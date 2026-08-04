/*
 * Copyright (c) 2021-2025 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP ISO/IEC 15118 <https://github.com/OpenChargingCloud/WWCP_ISO15118>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

using cloud.charging.open.protocols.ISO15118.NetworkInterfaces;
using cloud.charging.open.protocols.ISO15118.SDP.Client;
using cloud.charging.open.protocols.ISO15118.SDP.Messages;
using cloud.charging.open.protocols.ISO15118.SDP.Server;
using cloud.charging.open.protocols.ISO15118.SLAC.StateMachine;
using cloud.charging.open.protocols.ISO15118.SLAC.Transport;

using Vanaheimr.V2G.Simulation.Discovery;
using Vanaheimr.V2G.Simulation.Sap;
using Vanaheimr.V2G.Simulation.Slac;
using Vanaheimr.V2G.Simulation.StateMachines;
using Vanaheimr.V2G.Simulation.StateMachines.Iso2;
using Vanaheimr.V2G.Simulation.StateMachines.Iso20;
using Vanaheimr.V2G.Simulation.Timing;
using Vanaheimr.V2G.Simulation.Transport;
using Vanaheimr.V2G.Simulation.Transport.BouncyCastle;

namespace Vanaheimr.V2G.Simulation.Cli
{
    /// <summary>
    /// One-shot EVCC/SECC session runner with selectable front stages (SLAC, SDP) and TLS backend
    /// (.NET SslStream or BouncyCastle). See <see cref="CliArgs.Usage"/>.
    /// </summary>
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            CliArgs cliArgs;
            try { cliArgs = CliArgs.Parse(args); }
            catch (ArgumentException ex) { Console.Error.WriteLine(ex.Message); return 2; }

            var sw = Stopwatch.StartNew();
            try
            {
                if (cliArgs.Role == Role.Secc) await RunSeccAsync(cliArgs);
                else                            await RunEvccAsync(cliArgs);

                Console.WriteLine($"\n✓ Session complete in {sw.ElapsedMilliseconds} ms.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"\n✗ Session aborted: {ex.Message}");
                return 1;
            }
        }

        // ── SECC ───────────────────────────────────────────────────────────────────────────────────

        private static async Task RunSeccAsync(CliArgs args)
        {
            if (args.UseSlac)
                await RunSeccSlacAsync(args);

            // .NET backend: a supplied --server-cert (e.g. a CPO/SECC leaf chain a real EVCC's trust anchor
            // accepts) takes precedence over the fresh self-signed dev cert. With --require-client-cert the SECC
            // requires + (dev: accepts any) the EVCC's client certificate for mutual TLS.
            var (serverLeaf, serverChain) = LoadCertificateWithChain(args.ServerCertPath, args.ServerCertPass);
            using var devCert = args.TlsBackend == TlsBackend.Dotnet && serverLeaf is null ? CreateDevCertificate() : null;
            var dotnetTls = args.TlsBackend != TlsBackend.Dotnet ? null : new TlsOptions
            {
                ServerCertificate         = serverLeaf ?? devCert,
                ServerCertificateChain    = serverChain,
                EnabledSslProtocols       = SslProtocols.Tls12 | SslProtocols.Tls13,
                RequireClientCertificate  = args.RequireClientCert,
                ClientCertificateValidation = args.RequireClientCert ? (_, _, _, _) => true : null,
            };
            if (serverLeaf is not null)
                Console.WriteLine($"Presenting server certificate: {serverLeaf.Subject} (+{serverChain?.Count ?? 0} intermediate(s))"
                                  + (args.RequireClientCert ? "; requiring a client certificate (mutual TLS, dev: accept-any)" : ""));
            var bcTls     = args.TlsBackend == TlsBackend.BouncyCastle ? CliPki.GenerateSeccOptions(args.PkiDir!) : null;

            using var listener = bcTls is not null
                                     ? new TcpV2GListener(new IPEndPoint(IPAddress.IPv6Any, args.ListenPort), bcTls)
                                     : new TcpV2GListener(new IPEndPoint(IPAddress.IPv6Any, args.ListenPort), dotnetTls);

            Console.WriteLine($"SECC listening on {listener.LocalEndpoint} " +
                              $"(protocol {(args.OfferBoth ? "both (-20 preferred)" : ProtocolName(args.Protocol))}, " +
                              $"{ModeName(args.Mode)}, TLS {args.TlsBackend})...");

            await using var sdp = args.UseSdp ? await StartSeccSdpAsync(args, listener.LocalEndpoint.Port) : null;

            // Pause/resume: a session that ends with ChargingSession.Pause hands back its session id — keep
            // accepting connections and offer it, so the EV can rejoin (OK_OldSessionJoined) after reconnecting.
            byte[]? resumeId = null;
            do
            {
                using var stream = await listener.AcceptAsync();
                CliArgs sessionArgs;
                if (args.OfferBoth)
                {
                    // A mini-IsoMux: accept both protocols, follow the EV's priority, and run the state
                    // machine the handshake settled on rather than one chosen before it ran.
                    var settled = await SapHandshake.RunSeccSideAsync(stream, BothOffers(args.Mode));
                    Console.WriteLine($"SAP: the EV's offer settled on {ProtocolName(settled.Protocol)}.");
                    sessionArgs = args with { Protocol = settled.Protocol };
                }
                else
                {
                    await SapHandshake.RunSeccSideAsync(stream, args.Protocol, mode: args.Mode);
                    sessionArgs = args;
                }
                resumeId = await RunSeccSessionAsync(stream, sessionArgs, resumeId);
                if (resumeId is not null)
                    Console.WriteLine($"Session paused (id {Convert.ToHexString(resumeId)}) — awaiting resume...");
            }
            while (resumeId is not null);
        }

        /// <summary>-20 first, -2 second: a real car's preference order, and the SECC list a
        /// multiplexer supports. The mode applies to both entries.</summary>
        private static SapOffer[] BothOffers(PowerMode mode) =>
            [new(ProtocolVariant.Iso15118_20, mode), new(ProtocolVariant.Iso15118_2, mode)];

        /// <summary>Runs one session; returns the session id when it ended <b>paused</b> (offer it to the
        /// next session as the resume id), else <c>null</c>.</summary>
        private static async Task<byte[]?> RunSeccSessionAsync(Stream stream, CliArgs args, byte[]? resumeId = null)
        {
            if (args.Protocol == ProtocolVariant.Iso15118_2)
            {
                var secc2 = new Secc2(args.Mode, TimeSpan.FromSeconds(60), TimeProvider.System)
                {
                    ResumeSessionId = resumeId,
                    RequestRenegotiation = args.Renegotiate,
                    TariffSignKey = args.TariffCertPath is not null
                        ? LoadTariffKey(args.TariffCertPath, args.TariffCertPass, signing: true) : null,
                };
                try { await secc2.RunAsync(stream); }
                finally
                {
                    if (secc2.PnCAuth is { } pnc2)
                        Console.WriteLine($"-2 Plug & Charge: contract {pnc2.ContractSubject}; challenge {(pnc2.ChallengeOk ? "OK" : "MISMATCH")}, " +
                                          $"digest {(pnc2.DigestOk ? "OK" : "FAIL")}, signature {(pnc2.SignatureOk ? "OK" : "FAIL")}" +
                                          $"{(pnc2.SignatureOk ? $" (grammar={pnc2.SignatureGrammar})" : "")}.");
                    foreach (var r in secc2.MeteringReceipts)
                        Console.WriteLine($"-2 MeteringReceipt: digest {(r.DigestOk ? "OK" : "FAIL")}, " +
                                          $"signature {(r.SignatureOk ? "OK" : "FAIL")}{(r.SignatureOk ? $" (grammar={r.SignatureGrammar})" : "")}.");
                    if (secc2.ChargingProfileCheck is { } cp)
                        Console.WriteLine($"-2 SmartCharging: EV chose tuple {cp.TupleId} " +
                                          $"({(cp.TupleIdOk ? "offered" : "NOT OFFERED")}), ChargingProfile {cp.ProfileEntries} " +
                                          $"entr{(cp.ProfileEntries == 1 ? "y" : "ies")}, within PMax: {(cp.WithinPMax ? "OK" : "VIOLATED")}.");
                }
                if (secc2.Renegotiations > 0)
                    Console.WriteLine($"-2 Renegotiation cycles: {secc2.Renegotiations}.");
                return secc2.Paused ? secc2.SessionId : null;
            }
            else
            {
                Secc20Base secc = args.Mode == PowerMode.Dc
                    ? new Secc20Dc(TimeSpan.FromSeconds(60), TimeProvider.System)
                    : new Secc20Ac(TimeSpan.FromSeconds(60), TimeProvider.System);
                secc.PreferDynamicControlMode = args.PreferDynamic;
                secc.OfferPlugAndCharge       = !args.NoPnc;
                secc.ResumeSessionId = resumeId;
                secc.RequestRenegotiation = args.Renegotiate;
                if (args.TariffCertPath is not null)
                    secc.TariffSignKey = LoadTariffKey(args.TariffCertPath, args.TariffCertPass, signing: true);
                // finally: the PnC/cert-install verdicts are the run's evidence — print them even when the
                // peer aborts mid-session (e.g. Josev's EVCC crashes on its own unimplemented cert-install res).
                try { await secc.RunAsync(stream); }
                finally
                {
                    PrintSeccVerdicts(secc);
                    if (secc.Renegotiations > 0)
                        Console.WriteLine($"-20 ServiceRenegotiation cycles: {secc.Renegotiations}.");
                }
                return secc.Paused ? secc.SessionId : null;
            }
        }

        private static void PrintSeccVerdicts(Secc20Base secc)
        {
                if (secc.PnCAuth is { } pnc)
                    Console.WriteLine($"Plug & Charge: contract {pnc.ContractSubject}; challenge {(pnc.ChallengeOk ? "OK" : "MISMATCH")}, " +
                                      $"digest {(pnc.DigestOk ? "OK" : "FAIL")}, signature {(pnc.SignatureOk ? "OK" : "FAIL")} " +
                                      $"({pnc.SignatureMethod}{(pnc.SignatureOk ? $", grammar={pnc.SignatureGrammar}" : "")}).");
                if (secc.CertInstall is { } ci)
                    Console.WriteLine($"CertificateInstallation: OEM {ci.OemSubject}; digest {(ci.DigestOk ? "OK" : "FAIL")}, " +
                                      $"signature {(ci.SignatureOk ? "OK" : "FAIL")}{(ci.SignatureOk ? $" (grammar={ci.SignatureGrammar})" : "")}, " +
                                      $"contract issued ({(ci.EncryptedForOem ? "key wrapped for OEM key" : "OEM key not P-521 — blob undecryptable for EV")}).");
        }

        private static async Task RunSeccSlacAsync(CliArgs args)
        {
            await using var transport = new UdpSlacTransport(RandomMac(), new IPEndPoint(IPAddress.Any, args.SlacListenPort));
            await using var slac = new SlacEvseStage(transport,
                new EvseSlacOptions { EvseId = new byte[17], Nid = RandomNumberGenerator.GetBytes(7), Nmk = RandomNumberGenerator.GetBytes(16) });
            await slac.StartAsync();
            Console.WriteLine($"SLAC: EVSE listening on UDP :{args.SlacListenPort} for a PEV...");
            var result = await slac.WaitForMatchAsync();
            Console.WriteLine($"SLAC: paired (NID {Convert.ToHexString(result.Nid)}).");
        }

        private static async Task<SeccSdpAdvertiser> StartSeccSdpAsync(CliArgs args, int tcpPort)
        {
            var iface   = ResolveInterface(args.Interface!);
            var noTls   = args.TlsBackend == TlsBackend.None;
            var server  = new SECC_SDPServer(BuildSeccSdpOptions(iface, tcpPort, noTls));
            // iface.LinkLocalIPAddress already carries the interface ScopeId on Linux; re-derive a scoped
            // address so the display shows the scope exactly once (not "…%2%2").
            var scoped  = new IPAddress(iface.LinkLocalIPAddress.GetAddressBytes(), iface.Index);
            Console.WriteLine($"SDP: advertising [{scoped}]:{tcpPort} ({(noTls ? "NoTLS" : "TLS")}) on {iface.Name}...");
            var advertiser = new SeccSdpAdvertiser(server);
            await advertiser.StartAsync();
            return advertiser;
        }

        /// <summary>
        /// Builds the SECC SDP-server options for the CLI. A <b>plaintext</b> SECC (<paramref name="noTls"/>)
        /// advertises <see cref="SDP_Security.NoTLS"/> and — crucially — sets
        /// <see cref="SECC_SDPServerOptions.RejectNoTlsRequests"/> to <c>false</c> so it actually answers a
        /// plaintext EVCC's SDP_Request; the option's TLS-deployment-oriented default (<c>true</c>) would
        /// otherwise silently drop it and make <c>--sdp</c> discovery appear broken. A TLS SECC advertises
        /// <see cref="SDP_Security.TLS"/> and keeps rejecting no-TLS downgrade requests.
        /// </summary>
        internal static SECC_SDPServerOptions BuildSeccSdpOptions(V2GNetworkInterface iface, int tcpPort, bool noTls)
            => new()
            {
                Interface           = iface,
                SeccPort            = (ushort) tcpPort,
                AcceptedVersions    = new HashSet<SDP_Version> { SDP_Version.ISO_15118_2, SDP_Version.ISO_15118_20 },
                OfferedSecurity     = noTls ? SDP_Security.NoTLS : SDP_Security.TLS,
                RejectNoTlsRequests = !noTls,
            };

        // ── EVCC ───────────────────────────────────────────────────────────────────────────────────

        private static async Task RunEvccAsync(CliArgs args)
        {
            if (args.UseSlac)
                await RunEvccSlacAsync(args);

            if (!args.PauseResume)
            {
                // --pause / --resume <hex>: the two pause/resume halves as separate invocations, for
                // orchestration by an outer script (e.g. when the SECC moves ports between sessions).
                var oneShotResume = args.ResumeSessionIdHex is null ? null : Convert.FromHexString(args.ResumeSessionIdHex);
                var sid = await RunOneEvccConnectionAsync(args, pause: args.EndPaused, resumeId: oneShotResume);
                if (args.EndPaused)
                    Console.WriteLine($"Paused session id: {Convert.ToHexString(sid)}");
                return;
            }

            // Pause/resume: session 1 ends with ChargingSession.Pause; then a completely fresh connection
            // (incl. SDP re-discovery when --sdp) rejoins the paused session by its id — the SECC must
            // answer SessionSetup with OK_OldSessionJoined.
            var sessionId = await RunOneEvccConnectionAsync(args, pause: true, resumeId: null);
            Console.WriteLine($"\n— Paused (session {Convert.ToHexString(sessionId)}); reconnecting to resume —\n");
            await Task.Delay(TimeSpan.FromSeconds(2));
            await RunOneEvccConnectionAsync(args, pause: false, resumeId: sessionId);
        }

        private static async Task<byte[]> RunOneEvccConnectionAsync(CliArgs args, bool pause, byte[]? resumeId)
        {
            var (host, port) = await ResolveEvccEndpointAsync(args);
            using var stream = await ConnectEvccAsync(args, host, port);

            if (args.OfferBoth)
            {
                // The state machine is chosen AFTER the handshake: offer both, run whichever the
                // station picked — the case a multiplexing station (EVerest's IsoMux) exists for.
                var accepted = await SapHandshake.RunEvccSideAsync(stream, BothOffers(args.Mode));
                Console.WriteLine($"SAP: offered -20 (priority 1) and -2 (priority 2); " +
                                  $"the station picked {ProtocolName(accepted.Protocol)}.");
                return await RunEvccSessionAsync(stream, args with { Protocol = accepted.Protocol }, pause, resumeId);
            }

            await SapHandshake.RunEvccSideAsync(stream, args.Protocol, mode: args.Mode);
            return await RunEvccSessionAsync(stream, args, pause, resumeId);
        }

        private static async Task<Stream> ConnectEvccAsync(CliArgs args, string host, int port)
        {
            switch (args.TlsBackend)
            {
                case TlsBackend.BouncyCastle:
                    return await TcpV2GClient.ConnectAsync(host, port, CliPki.LoadEvccOptions(args.PkiDir!));

                case TlsBackend.Dotnet:
                    // Dev CLI only: no out-of-band way to learn the SECC dev-cert thumbprint, so accept any.
                    Console.WriteLine("WARNING: accepting any TLS server certificate — dev CLI only, never against a real SECC.");
                    var (clientLeaf, clientChain) = LoadCertificateWithChain(args.ClientCertPath, args.ClientCertPass);
                    var tlsOptions = new TlsOptions
                    {
                        ServerCertificateValidation = (_, _, _, _) => true,
                        // Negotiate TLS 1.2 or 1.3 so this interoperates with a peer in either mode (Josev's
                        // SECC serves TLS 1.2 unilateral by default, TLS 1.3 mutual with ENABLE_TLS_1_3=True).
                        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                        ClientCertificate = clientLeaf,
                        ClientCertificateChain = clientChain,
                    };
                    if (clientLeaf is not null)
                        Console.WriteLine($"Presenting client certificate for mutual TLS: {clientLeaf.Subject} (+{clientChain?.Count ?? 0} intermediate(s))");
                    return await TcpV2GClient.ConnectAsync(host, port, tlsOptions);

                default:
                    return await TcpV2GClient.ConnectAsync(host, port);
            }
        }

        private static async Task<byte[]> RunEvccSessionAsync(Stream stream, CliArgs args, bool pause = false, byte[]? resumeId = null)
        {
            if (args.Protocol == ProtocolVariant.Iso15118_2)
            {
                var evcc = new Evcc2(stream, args.Mode, TimeProvider.System, new TaskAsyncDelay(), TimeSpan.FromSeconds(2))
                {
                    StopMode = pause ? cloud.charging.open.protocols.ISO15118_2.Generated.ChargingSession.Pause
                                     : cloud.charging.open.protocols.ISO15118_2.Generated.ChargingSession.Terminate,
                    ResumeSessionId = resumeId,
                    Renegotiate = args.Renegotiate,
                };
                if (args.ContractCertPath is not null)
                    evcc.Pnc = LoadContractCredentials(args.ContractCertPath, args.ContractCertPass);
                if (args.TariffCertPath is not null)
                    evcc.TariffVerifyKey = LoadTariffKey(args.TariffCertPath, args.TariffCertPass, signing: false);
                await evcc.RunAsync();
                Console.WriteLine($"  {evcc.Exchanges} exchanges, {evcc.BytesOnWire} bytes on the wire (request side), " +
                                  $"auth: {evcc.AuthorizationMode}, metering receipts sent: {evcc.MeteringReceiptsSent}, " +
                                  $"renegotiations: {evcc.Renegotiations}, session setup: {evcc.SessionSetupCode}.");
                if (evcc.Tariff is { } t2)
                    Console.WriteLine($"-2 Tariff: {t2.TuplesOffered} tuple(s), signature " +
                                      $"{(t2.SignaturePresent ? $"present, digests {(t2.DigestOk ? "OK" : "FAIL")}, " +
                                        $"ECDSA {(t2.SignatureOk ? $"OK (grammar={t2.SignatureGrammar})" : "FAIL/unverified")}" : "absent")}; " +
                                      $"chose tuple {t2.ChosenTupleId}, profile {t2.ProfileEntries} entr{(t2.ProfileEntries == 1 ? "y" : "ies")}.");
                return evcc.SessionId;
            }
            else
            {
                Evcc20Base evcc = args.Mode == PowerMode.Dc
                    ? new Evcc20Dc(stream, TimeProvider.System, new TaskAsyncDelay(), TimeSpan.FromSeconds(2))
                    : new Evcc20Ac(stream, TimeProvider.System, new TaskAsyncDelay(), TimeSpan.FromSeconds(2));
                evcc.StopMode = pause ? cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated.ChargingSession.Pause
                                      : cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated.ChargingSession.Terminate;
                evcc.ResumeSessionId = resumeId;
                if (args.ContractCertPath is not null)
                    evcc.Pnc = LoadContractCredentials(args.ContractCertPath, args.ContractCertPass);
                if (args.TariffCertPath is not null)
                    evcc.TariffVerifyKey = LoadTariffKey(args.TariffCertPath, args.TariffCertPass, signing: false);
                await evcc.RunAsync();
                Console.WriteLine($"  {evcc.Exchanges} exchanges, {evcc.BytesOnWire} bytes on the wire (request side), " +
                                  $"auth: {evcc.AuthorizationMode}, session setup: {evcc.SessionSetupCode}.");
                if (evcc.Tariff is { } t20)
                    Console.WriteLine($"-20 AbsolutePriceSchedule: signature {(t20.SignaturePresent ? "present" : "absent")}, " +
                                      $"digest {(t20.DigestOk ? "OK" : "FAIL")}, ECDSA-P521/SHA-512 {(t20.SignatureOk ? "OK" : "FAIL/unverified")}.");
                return evcc.SessionId;
            }
        }

        /// <summary>Loads the Plug &amp; Charge contract credentials from a PKCS#12: the cert with a private key
        /// is the contract leaf (its ECDSA key signs), every other cert goes into SubCertificates in file order
        /// (the MO sub-CAs). Fails loud if no cert carries an EC private key.</summary>
        private static PncEvccOptions LoadContractCredentials(string path, string? password)
        {
            var collection = X509CertificateLoader.LoadPkcs12CollectionFromFile(path, password,
                X509KeyStorageFlags.EphemeralKeySet);
            var leaf = collection.FirstOrDefault(c => c.HasPrivateKey)
                ?? throw new ArgumentException($"--contract-cert: no certificate in '{path}' carries a private key.");
            var key = leaf.GetECDsaPrivateKey()
                ?? throw new ArgumentException($"--contract-cert: the contract leaf's private key is not ECDSA.");
            var subCerts = collection.Where(c => !c.HasPrivateKey).Select(c => c.RawData).ToArray();
            Console.WriteLine($"PnC: contract cert {leaf.Subject} (+{subCerts.Length} sub-CA(s)), key {key.KeySize}-bit EC.");
            return new PncEvccOptions(leaf.RawData, subCerts, key);
        }

        /// <summary>Loads the tariff key from a PKCS#12: SECC (<paramref name="signing"/>) → the leaf's
        /// ECDSA private key, which signs the -2 SalesTariffs / -20 AbsolutePriceSchedule offer; EVCC →
        /// the leaf's public key, which verifies that signature.</summary>
        private static ECDsa LoadTariffKey(string path, string? password, bool signing)
        {
            var collection = X509CertificateLoader.LoadPkcs12CollectionFromFile(path, password,
                X509KeyStorageFlags.EphemeralKeySet);
            var leaf = collection.FirstOrDefault(c => c.HasPrivateKey) ?? collection.FirstOrDefault()
                ?? throw new ArgumentException($"--tariff-cert: '{path}' contains no certificate.");
            var key = signing
                ? leaf.GetECDsaPrivateKey() ?? throw new ArgumentException("--tariff-cert: the SECC side needs an ECDSA private key to sign.")
                : leaf.GetECDsaPublicKey() ?? throw new ArgumentException("--tariff-cert: the leaf's key is not ECDSA.");
            Console.WriteLine($"Tariff: {(signing ? "signing" : "verifying")} with {leaf.Subject}, {key.KeySize}-bit EC.");
            return key;
        }

        private static async Task<(string Host, int Port)> ResolveEvccEndpointAsync(CliArgs args)
        {
            if (!args.UseSdp)
                return (args.ConnectHost!, args.ConnectPort);

            var iface = ResolveInterface(args.Interface!);
            var discovery = new SdpSeccDiscovery(new EVCC_SDPClientOptions
            {
                Interface            = iface,
                RequestedSecurity    = args.TlsBackend == TlsBackend.None ? SDP_Security.NoTLS : SDP_Security.TLS,
                // Single-host interop (the SECC on the same machine, e.g. Josev in Docker/WSL): the
                // ff02::1 SDP_Request only reaches a LOCAL SECC via multicast loopback — with the
                // client's real-hardware default (off) the discovery times out against a healthy SECC
                // (root-caused live 2026-07-23). Harmless on real networks: the client discards its
                // own looped-back request by V2GTP payload type.
                MulticastLoopback    = true,
                // A plaintext run must accept the NoTLS response it asked for — the client's CRA/NIS2
                // default rejects it (the EVCC-side mirror of the SECC's RejectNoTlsRequests policy).
                RejectNoTlsResponses = args.TlsBackend != TlsBackend.None,
            });
            Console.WriteLine($"SDP: discovering the SECC on {iface.Name}...");
            var endpoint = await discovery.DiscoverAsync();
            Console.WriteLine($"SDP: found SECC at [{endpoint.Host}]:{endpoint.Port} (TLS {endpoint.Tls}).");
            return (endpoint.Host, endpoint.Port);
        }

        private static async Task RunEvccSlacAsync(CliArgs args)
        {
            var peer = new IPEndPoint(IPAddress.Parse(args.SlacPeerHost!), args.SlacPeerPort);
            await using var transport = new UdpSlacTransport(RandomMac(), new IPEndPoint(IPAddress.Any, 0), bootstrapPeers: [peer]);
            var slac = new SlacEvStage(transport, new EvSlacOptions { PevId = new byte[17] });
            Console.WriteLine($"SLAC: pairing with EVSE at {peer}...");
            var result = await slac.PairAsync();
            Console.WriteLine($"SLAC: paired (NID {Convert.ToHexString(result.Nid)}).");
        }

        // ── helpers ──────────────────────────────────────────────────────────────────────────────────

        private static V2GNetworkInterface ResolveInterface(string name)
            => new SystemV2GNetworkInterfaceProvider().FindByName(name)
               ?? throw new ArgumentException($"no V2G-capable network interface named '{name}' found.");

        private static MACAddress RandomMac() => MACAddress.FromPhysicalAddress(new PhysicalAddress(RandomNumberGenerator.GetBytes(6)));

        private static string ProtocolName(ProtocolVariant p) => p == ProtocolVariant.Iso15118_2 ? "-2" : "-20";
        private static string ModeName(PowerMode m) => m == PowerMode.Ac ? "AC" : "DC";

        /// <summary>Loads a PKCS#12 certificate for TLS (client or server), splitting the private-key leaf from its
        /// intermediate CA chain so <c>SslStream</c> can send both. Returns (null, null) when <paramref name="path"/> is null.</summary>
        private static (X509Certificate2? Leaf, X509Certificate2Collection? Chain) LoadCertificateWithChain(string? path, string? password)
        {
            if (path is null)
                return (null, null);

            var all = X509CertificateLoader.LoadPkcs12CollectionFromFile(path, password, X509KeyStorageFlags.Exportable);
            var leaf = all.FirstOrDefault(c => c.HasPrivateKey) ?? all[0];
            var chain = new X509Certificate2Collection(all.Where(c => !ReferenceEquals(c, leaf)).ToArray());
            return (leaf, chain);
        }

        private static X509Certificate2 CreateDevCertificate()
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var req = new CertificateRequest("CN=localhost", ecdsa, HashAlgorithmName.SHA256);
            var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddHours(1));
            Console.WriteLine("Using a fresh self-signed DEV certificate — not for production use.");
            return X509CertificateLoader.LoadPkcs12(cert.Export(X509ContentType.Pfx), password: null, X509KeyStorageFlags.Exportable);
        }
    }
}
