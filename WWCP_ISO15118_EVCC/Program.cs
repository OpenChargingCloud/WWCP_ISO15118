/*
 * Copyright (c) 2021-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP ISO/IEC 15118 <https://github.com/OpenChargingCloud/WWCP_ISO15118>
 *
 * Licensed under the Affero GPL license, Version 3.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.gnu.org/licenses/agpl.html
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
using cloud.charging.open.protocols.ISO15118.SLAC.StateMachine;
using cloud.charging.open.protocols.ISO15118.SLAC.Transport;

using cloud.charging.open.protocols.ISO15118.Discovery;
using cloud.charging.open.protocols.ISO15118.Sap;
using cloud.charging.open.protocols.ISO15118.Session;
using cloud.charging.open.protocols.ISO15118.Slac;
using cloud.charging.open.protocols.ISO15118.StateMachines;
using cloud.charging.open.protocols.ISO15118.StateMachines.Iso2;
using cloud.charging.open.protocols.ISO15118.StateMachines.Iso20;
using cloud.charging.open.protocols.ISO15118.Timing;
using cloud.charging.open.protocols.ISO15118.Transport;

namespace cloud.charging.open.protocols.ISO15118.EVCC
{
    /// <summary>
    /// The vehicle. It finds a station (or is told where one is), optionally pairs over SLAC first, then
    /// runs one ISO 15118 session to <c>SessionStop</c> — <c>-2</c> or <c>-20</c>, whichever the
    /// SupportedAppProtocol handshake settles on.
    /// </summary>
    /// <remarks>
    /// A car's flow has a beginning and an end: connect, charge, leave. That is why this is a separate
    /// program from the station, whose flow is a loop. The one exception is pause/resume, where the car
    /// deliberately comes back — <c>--pause-resume</c> makes both halves one run.
    /// </remarks>
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            EvccOptions options;
            try { options = EvccOptions.Parse(args); }
            catch (ArgumentException ex) { Console.Error.WriteLine(ex.Message); return 2; }

            var sw = Stopwatch.StartNew();
            try
            {
                await RunAsync(options);
                Console.WriteLine($"\n✓ Session complete in {sw.ElapsedMilliseconds} ms.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"\n✗ Session aborted: {ex.Message}");
                return 1;
            }
        }

        private static async Task RunAsync(EvccOptions args)
        {
            if (args.UseSlac)
                await RunSlacAsync(args);

            if (!args.PauseResume)
            {
                // --pause / --resume <hex>: the two pause/resume halves as separate invocations, for
                // orchestration by an outer script (e.g. when the SECC moves ports between sessions).
                // Two invocations cannot hand a station binding between them, so a --resume here carries the
                // session id alone and the EVCC's own same-station check has nothing to compare against.
                // Deliberate: see Evcc20Base.ResumeBinding for why the car may proceed where a station may not.
                var oneShotResume = args.ResumeSessionIdHex is null
                                        ? null
                                        : new ResumableSession(Convert.FromHexString(args.ResumeSessionIdHex), null, 0);
                var oneShot = await RunOneConnectionAsync(args, pause: args.EndPaused, resume: oneShotResume);
                if (args.EndPaused)
                    Console.WriteLine($"Paused session id: {Convert.ToHexString(oneShot.SessionId)}");
                return;
            }

            // Pause/resume: session 1 ends with ChargingSession.Pause; then a completely fresh connection
            // (incl. SDP re-discovery when --sdp) rejoins the paused session — the SECC must answer
            // SessionSetup with OK_OldSessionJoined, which for -20 also means proving we are the same car.
            var paused = await RunOneConnectionAsync(args, pause: true, resume: null);
            Console.WriteLine($"\n— Paused (session {Convert.ToHexString(paused.SessionId)}); reconnecting to resume —\n");
            await Task.Delay(TimeSpan.FromSeconds(2));
            await RunOneConnectionAsync(args, pause: false, resume: paused);
        }

        private static async Task<ResumableSession> RunOneConnectionAsync(EvccOptions args, bool pause, ResumableSession? resume)
        {
            var (host, port) = await ResolveEndpointAsync(args);
            using var stream = await ConnectAsync(args, host, port);

            if (args.OfferBoth)
            {
                // The state machine is chosen AFTER the handshake: offer both, run whichever the
                // station picked — the case a multiplexing station (EVerest's IsoMux) exists for.
                var accepted = await SapHandshake.RunEvccSideAsync(stream, BothOffers(args.Mode));
                Console.WriteLine($"SAP: offered -20 (priority 1) and -2 (priority 2); " +
                                  $"the station picked {ProtocolName(accepted.Protocol)}.");
                return await RunSessionAsync(stream, args with { Protocol = accepted.Protocol }, pause, resume);
            }

            await SapHandshake.RunEvccSideAsync(stream, args.Protocol, mode: args.Mode);
            return await RunSessionAsync(stream, args, pause, resume);
        }

        /// <summary>-20 first, -2 second: what a dual-stack car offers, and the order the station reads.</summary>
        private static SapOffer[] BothOffers(PowerMode mode) =>
            [new(ProtocolVariant.Iso15118_20, mode), new(ProtocolVariant.Iso15118_2, mode)];

        private static async Task<Stream> ConnectAsync(EvccOptions args, string host, int port)
        {
            switch (args.TlsBackend)
            {
                case TlsBackend.BouncyCastle:
                    return await TcpV2GClient.ConnectAsync(host, port, EvccPki.Load(args.PkiDir!));

                case TlsBackend.Dotnet:
                    // Dev tool only: no out-of-band way to learn the SECC dev-cert thumbprint, so accept any.
                    Console.WriteLine("WARNING: accepting any TLS server certificate — dev tool only, never against a real SECC.");
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

        private static async Task<ResumableSession> RunSessionAsync(Stream stream, EvccOptions args, bool pause = false, ResumableSession? resume = null)
        {
            if (args.Protocol == ProtocolVariant.Iso15118_2)
            {
                var evcc = new Evcc2(stream, args.Mode, TimeProvider.System, new TaskAsyncDelay(), TimeSpan.FromSeconds(2))
                {
                    StopMode = pause ? cloud.charging.open.protocols.ISO15118_2.Generated.ChargingSession.Pause
                                     : cloud.charging.open.protocols.ISO15118_2.Generated.ChargingSession.Terminate,
                    ResumeSessionId = resume?.SessionId,
                    Renegotiate = args.Renegotiate,
                };
                if (args.ContractCertPath is not null)
                    evcc.Pnc = LoadContractCredentials(args.ContractCertPath, args.ContractCertPass);
                if (args.TariffCertPath is not null)
                    evcc.TariffVerifyKey = LoadTariffVerifyKey(args.TariffCertPath, args.TariffCertPass);
                await evcc.RunAsync();
                Console.WriteLine($"  {evcc.Exchanges} exchanges, {evcc.BytesOnWire} bytes on the wire (request side), " +
                                  $"auth: {evcc.AuthorizationMode}, metering receipts sent: {evcc.MeteringReceiptsSent}, " +
                                  $"renegotiations: {evcc.Renegotiations}, session setup: {evcc.SessionSetupCode}.");
                if (evcc.Tariff is { } t2)
                    Console.WriteLine($"-2 Tariff: {t2.TuplesOffered} tuple(s), signature " +
                                      $"{(t2.SignaturePresent ? $"present, digests {(t2.DigestOk ? "OK" : "FAIL")}, " +
                                        $"ECDSA {(t2.SignatureOk ? $"OK (grammar={t2.SignatureGrammar})" : "FAIL/unverified")}" : "absent")}; " +
                                      $"chose tuple {t2.ChosenTupleId}, profile {t2.ProfileEntries} entr{(t2.ProfileEntries == 1 ? "y" : "ies")}.");
                // -2 binds nothing to the session and renegotiates the service on resume — both by design
                // there, and both changed in -20. See Session/ResumableSession.
                return new ResumableSession(evcc.SessionId, null, 0);
            }
            else
            {
                Evcc20Base evcc = (args.Mode, args.Mcs) switch
                {
                    (PowerMode.Dc, true ) => new Evcc20Mcs(stream, TimeProvider.System, new TaskAsyncDelay(), TimeSpan.FromSeconds(2)),
                    (PowerMode.Dc, false) => new Evcc20Dc (stream, TimeProvider.System, new TaskAsyncDelay(), TimeSpan.FromSeconds(2)),
                    _                     => new Evcc20Ac (stream, TimeProvider.System, new TaskAsyncDelay(), TimeSpan.FromSeconds(2)),
                };
                evcc.StopMode = pause ? cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated.ChargingSession.Pause
                                      : cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated.ChargingSession.Terminate;
                evcc.ResumeFrom(resume);
                if (args.ContractCertPath is not null)
                    evcc.Pnc = LoadContractCredentials(args.ContractCertPath, args.ContractCertPass);
                if (args.TariffCertPath is not null)
                    evcc.TariffVerifyKey = LoadTariffVerifyKey(args.TariffCertPath, args.TariffCertPass);
                await evcc.RunAsync();
                Console.WriteLine($"  {evcc.Exchanges} exchanges, {evcc.BytesOnWire} bytes on the wire (request side), " +
                                  $"auth: {evcc.AuthorizationMode}, session setup: {evcc.SessionSetupCode}.");
                if (evcc.Tariff is { } t20)
                    Console.WriteLine($"-20 AbsolutePriceSchedule: signature {(t20.SignaturePresent ? "present" : "absent")}, " +
                                      $"digest {(t20.DigestOk ? "OK" : "FAIL")}, ECDSA-P521/SHA-512 {(t20.SignatureOk ? "OK" : "FAIL/unverified")}.");
                if (evcc.ResumeRefused)
                    Console.WriteLine("  the station refused the resume and opened a new session — " +
                                      "everything the paused session carried, authorization included, was dropped.");
                else if (evcc.ResumedStationVerified == true)
                    Console.WriteLine("  resumed session confirmed to be with the same station (certificate binding).");
                return evcc.PausedSession;
            }
        }

        private static async Task<(string Host, int Port)> ResolveEndpointAsync(EvccOptions args)
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

        private static async Task RunSlacAsync(EvccOptions args)
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

        /// <summary>Loads a PKCS#12 certificate for mutual TLS, splitting the private-key leaf from its
        /// intermediate CA chain so <c>SslStream</c> can send both. Returns (null, null) when
        /// <paramref name="path"/> is null.</summary>
        private static (X509Certificate2? Leaf, X509Certificate2Collection? Chain) LoadCertificateWithChain(string? path, string? password)
        {
            if (path is null)
                return (null, null);

            var all = X509CertificateLoader.LoadPkcs12CollectionFromFile(path, password, X509KeyStorageFlags.Exportable);
            var leaf = all.FirstOrDefault(c => c.HasPrivateKey) ?? all[0];
            var chain = new X509Certificate2Collection(all.Where(c => !ReferenceEquals(c, leaf)).ToArray());
            return (leaf, chain);
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

        /// <summary>Loads the tariff <b>verification</b> key from a PKCS#12 — the leaf's ECDSA public key,
        /// which checks the station's signed SalesTariffs / -20 AbsolutePriceSchedule. The signing half
        /// lives in the SECC program.</summary>
        private static ECDsa LoadTariffVerifyKey(string path, string? password)
        {
            var collection = X509CertificateLoader.LoadPkcs12CollectionFromFile(path, password,
                X509KeyStorageFlags.EphemeralKeySet);
            var leaf = collection.FirstOrDefault(c => c.HasPrivateKey) ?? collection.FirstOrDefault()
                ?? throw new ArgumentException($"--tariff-cert: '{path}' contains no certificate.");
            var key = leaf.GetECDsaPublicKey()
                ?? throw new ArgumentException("--tariff-cert: the leaf's key is not ECDSA.");
            Console.WriteLine($"Tariff: verifying with {leaf.Subject}, {key.KeySize}-bit EC.");
            return key;
        }
    }
}
