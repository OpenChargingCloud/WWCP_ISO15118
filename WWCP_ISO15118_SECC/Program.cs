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
using cloud.charging.open.protocols.ISO15118.SDP.Messages;
using cloud.charging.open.protocols.ISO15118.SDP.Server;
using cloud.charging.open.protocols.ISO15118.SLAC.StateMachine;
using cloud.charging.open.protocols.ISO15118.SLAC.Transport;

using cloud.charging.open.protocols.ISO15118.Discovery;
using cloud.charging.open.protocols.ISO15118.Sap;
using cloud.charging.open.protocols.ISO15118.Session;
using cloud.charging.open.protocols.ISO15118.Slac;
using cloud.charging.open.protocols.ISO15118.StateMachines;
using cloud.charging.open.protocols.ISO15118.StateMachines.Iso2;
using cloud.charging.open.protocols.ISO15118.StateMachines.Iso20;
using cloud.charging.open.protocols.ISO15118.Transport;

namespace cloud.charging.open.protocols.ISO15118.SECC
{
    /// <summary>
    /// The charging station. It listens, optionally advertises itself over SDP and pairs over SLAC first,
    /// then runs one ISO 15118 session per connection — <c>-2</c> or <c>-20</c>, whichever the car's
    /// SupportedAppProtocol offer settles on.
    /// </summary>
    /// <remarks>
    /// A station outlives its sessions, and this one does too: it keeps accepting for as long as a
    /// paused session is waiting to be rejoined. That is the shape a real SECC has and the reason the
    /// two roles are separate programs — the car's flow is one connection with a beginning and an end,
    /// the station's is a loop.
    /// </remarks>
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            SeccOptions options;
            try { options = SeccOptions.Parse(args); }
            catch (ArgumentException ex) { Console.Error.WriteLine(ex.Message); return 2; }

            var sw = Stopwatch.StartNew();
            try
            {
                await RunAsync(options);
                Console.WriteLine($"\n✓ Station stopped after {sw.ElapsedMilliseconds} ms.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"\n✗ Session aborted: {ex.Message}");
                return 1;
            }
        }

        private static async Task RunAsync(SeccOptions args)
        {
            if (args.UseSlac)
                await RunSlacAsync(args);

            // .NET backend: a supplied --server-cert (e.g. a CPO/SECC leaf chain a real EVCC's trust anchor
            // accepts) takes precedence over the fresh self-signed dev cert. With --require-client-cert the
            // station requires + (dev: accepts any) the car's client certificate for mutual TLS.
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
            var bcTls = args.TlsBackend == TlsBackend.BouncyCastle ? SeccPki.Generate(args.PkiDir!) : null;

            using var listener = bcTls is not null
                                     ? new TcpV2GListener(new IPEndPoint(IPAddress.IPv6Any, args.ListenPort), bcTls)
                                     : new TcpV2GListener(new IPEndPoint(IPAddress.IPv6Any, args.ListenPort), dotnetTls);

            Console.WriteLine($"SECC listening on {listener.LocalEndpoint} " +
                              $"(protocol {(args.OfferBoth ? "both (-20 preferred)" : ProtocolName(args.Protocol))}, " +
                              $"{ModeName(args.Mode)}, TLS {args.TlsBackend})...");

            await using var sdp = args.UseSdp ? await StartSdpAsync(args, listener.LocalEndpoint.Port) : null;

            // Pause/resume: a session that ends with ChargingSession.Pause hands back what the next
            // connection needs to rejoin it — keep accepting connections and offer that, so the EV can come
            // back (OK_OldSessionJoined). For -20 "the EV" is meant literally: the offer includes a binding
            // to the vehicle certificate, and a different car naming this session id gets a new session.
            ResumableSession? paused = null;
            do
            {
                using var stream = await listener.AcceptAsync();
                SeccOptions sessionArgs;
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
                paused = await RunSessionAsync(stream, sessionArgs, paused);
                if (paused is not null)
                    Console.WriteLine($"Session paused (id {Convert.ToHexString(paused.SessionId)}" +
                                      $"{(paused.Binding is null ? ", unbound — no peer certificate, so a resume cannot be verified" : "")})" +
                                      " — awaiting resume...");
            }
            while (paused is not null);
        }

        /// <summary>-20 first, -2 second: a real car's preference order, and the SECC list a
        /// multiplexer supports. The mode applies to both entries.</summary>
        private static SapOffer[] BothOffers(PowerMode mode) =>
            [new(ProtocolVariant.Iso15118_20, mode), new(ProtocolVariant.Iso15118_2, mode)];

        /// <summary>Runs one session; returns the session id when it ended <b>paused</b> (offer it to the
        /// next session as the resume id), else <c>null</c>.</summary>
        private static async Task<ResumableSession?> RunSessionAsync(Stream stream, SeccOptions args, ResumableSession? resume = null)
        {
            if (args.Protocol == ProtocolVariant.Iso15118_2)
            {
                var secc2 = new Secc2(args.Mode, TimeSpan.FromSeconds(60), TimeProvider.System)
                {
                    ResumeSessionId = resume?.SessionId,
                    RequestRenegotiation = args.Renegotiate,
                    TariffSignKey = args.TariffCertPath is not null
                        ? LoadTariffSigningKey(args.TariffCertPath, args.TariffCertPass) : null,
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
                return secc2.Paused ? new ResumableSession(secc2.SessionId, null, 0) : null;
            }
            else
            {
                Secc20Base secc = (args.Mode, args.Mcs) switch
                {
                    (PowerMode.Dc, true ) => new Secc20Mcs(TimeSpan.FromSeconds(60), TimeProvider.System),
                    (PowerMode.Dc, false) => new Secc20Dc (TimeSpan.FromSeconds(60), TimeProvider.System),
                    _                     => new Secc20Ac (TimeSpan.FromSeconds(60), TimeProvider.System),
                };
                secc.PreferDynamicControlMode = args.PreferDynamic;
                secc.OfferPlugAndCharge       = !args.NoPnc;
                secc.OfferResume(resume);
                secc.RequestRenegotiation = args.Renegotiate;
                if (args.TariffCertPath is not null)
                    secc.TariffSignKey = LoadTariffSigningKey(args.TariffCertPath, args.TariffCertPass);
                // finally: the PnC/cert-install verdicts are the run's evidence — print them even when the
                // peer aborts mid-session (e.g. Josev's EVCC crashes on its own unimplemented cert-install res).
                try { await secc.RunAsync(stream); }
                finally
                {
                    PrintVerdicts(secc);
                    // Which entry of our catalogue the EV picked. Only the station can report this in a
                    // reverse run, and MCS is otherwise indistinguishable from DC on the wire.
                    if (secc.SelectedEnergyServiceId != 0)
                        Console.WriteLine($"Energy transfer service: {secc.SelectedEnergyServiceId} " +
                                          $"({EnergyServiceName(secc.SelectedEnergyServiceId)}).");
                    if (secc.Renegotiations > 0)
                        Console.WriteLine($"-20 ServiceRenegotiation cycles: {secc.Renegotiations}.");
                    // A resume that was offered but not taken means the car naming the session id could not
                    // prove it was the car that paused it — worth saying out loud in a reverse run.
                    if (resume is not null && secc.SessionSetupCode !=
                            cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated.ResponseCode.OK_OldSessionJoined)
                        Console.WriteLine("Resume refused: the requester did not match the paused session's " +
                                          "binding — a new session was established instead.");
                }
                return secc.PausedSession;
            }
        }

        /// <summary>ISO 15118-20 Table 204, so the line above names the service instead of leaving a bare
        /// number to be looked up.</summary>
        private static string EnergyServiceName(ushort serviceId)
            => serviceId switch
               {
                   1 => "AC",         2 => "DC",          3 => "WPT",     4 => "DC_ACDP",
                   5 => "AC_BPT",     6 => "DC_BPT",      7 => "DC_ACDP_BPT",
                   8 => "MCS",        9 => "MCS_BPT",    10 => "AC_DER",
                   _ => "not in Table 204 as we read it",
               };

        private static void PrintVerdicts(Secc20Base secc)
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

        private static async Task RunSlacAsync(SeccOptions args)
        {
            await using var transport = new UdpSlacTransport(RandomMac(), new IPEndPoint(IPAddress.Any, args.SlacListenPort));
            await using var slac = new SlacEvseStage(transport,
                new EvseSlacOptions { EvseId = new byte[17], Nid = RandomNumberGenerator.GetBytes(7), Nmk = RandomNumberGenerator.GetBytes(16) });
            await slac.StartAsync();
            Console.WriteLine($"SLAC: EVSE listening on UDP :{args.SlacListenPort} for a PEV...");
            var result = await slac.WaitForMatchAsync();
            Console.WriteLine($"SLAC: paired (NID {Convert.ToHexString(result.Nid)}).");
        }

        private static async Task<SeccSdpAdvertiser> StartSdpAsync(SeccOptions args, int tcpPort)
        {
            var iface  = ResolveInterface(args.Interface!);
            var noTls  = args.TlsBackend == TlsBackend.None;
            var server = new SECC_SDPServer(BuildSdpOptions(iface, tcpPort, noTls));
            // iface.LinkLocalIPAddress already carries the interface ScopeId on Linux; re-derive a scoped
            // address so the display shows the scope exactly once (not "…%2%2").
            var scoped = new IPAddress(iface.LinkLocalIPAddress.GetAddressBytes(), iface.Index);
            Console.WriteLine($"SDP: advertising [{scoped}]:{tcpPort} ({(noTls ? "NoTLS" : "TLS")}) on {iface.Name}...");
            var advertiser = new SeccSdpAdvertiser(server);
            await advertiser.StartAsync();
            return advertiser;
        }

        /// <summary>
        /// Builds the SECC SDP-server options. A <b>plaintext</b> station (<paramref name="noTls"/>)
        /// advertises <see cref="SDP_Security.NoTLS"/> and — crucially — sets
        /// <see cref="SECC_SDPServerOptions.RejectNoTlsRequests"/> to <c>false</c> so it actually answers a
        /// plaintext EVCC's SDP_Request; the option's TLS-deployment-oriented default (<c>true</c>) would
        /// otherwise silently drop it and make <c>--sdp</c> discovery appear broken. A TLS station
        /// advertises <see cref="SDP_Security.TLS"/> and keeps rejecting no-TLS downgrade requests.
        /// </summary>
        internal static SECC_SDPServerOptions BuildSdpOptions(V2GNetworkInterface iface, int tcpPort, bool noTls)
            => new()
            {
                Interface           = iface,
                SeccPort            = (ushort) tcpPort,
                AcceptedVersions    = new HashSet<SDP_Version> { SDP_Version.ISO_15118_2, SDP_Version.ISO_15118_20 },
                OfferedSecurity     = noTls ? SDP_Security.NoTLS : SDP_Security.TLS,
                RejectNoTlsRequests = !noTls,
            };

        // ── helpers ──────────────────────────────────────────────────────────────────────────────────

        private static V2GNetworkInterface ResolveInterface(string name)
            => new SystemV2GNetworkInterfaceProvider().FindByName(name)
               ?? throw new ArgumentException($"no V2G-capable network interface named '{name}' found.");

        private static MACAddress RandomMac() => MACAddress.FromPhysicalAddress(new PhysicalAddress(RandomNumberGenerator.GetBytes(6)));

        private static string ProtocolName(ProtocolVariant p) => p == ProtocolVariant.Iso15118_2 ? "-2" : "-20";
        private static string ModeName(PowerMode m) => m == PowerMode.Ac ? "AC" : "DC";

        /// <summary>Loads a PKCS#12 certificate for TLS, splitting the private-key leaf from its intermediate
        /// CA chain so <c>SslStream</c> can send both. Returns (null, null) when <paramref name="path"/> is null.</summary>
        private static (X509Certificate2? Leaf, X509Certificate2Collection? Chain) LoadCertificateWithChain(string? path, string? password)
        {
            if (path is null)
                return (null, null);

            var all = X509CertificateLoader.LoadPkcs12CollectionFromFile(path, password, X509KeyStorageFlags.Exportable);
            var leaf = all.FirstOrDefault(c => c.HasPrivateKey) ?? all[0];
            var chain = new X509Certificate2Collection(all.Where(c => !ReferenceEquals(c, leaf)).ToArray());
            return (leaf, chain);
        }

        /// <summary>Loads the tariff <b>signing</b> key from a PKCS#12 — the leaf's ECDSA private key, which
        /// signs the -2 SalesTariffs / -20 AbsolutePriceSchedule offer. The car's side verifies with the
        /// matching public key; that half lives in the EVCC program.</summary>
        private static ECDsa LoadTariffSigningKey(string path, string? password)
        {
            var collection = X509CertificateLoader.LoadPkcs12CollectionFromFile(path, password,
                X509KeyStorageFlags.EphemeralKeySet);
            var leaf = collection.FirstOrDefault(c => c.HasPrivateKey) ?? collection.FirstOrDefault()
                ?? throw new ArgumentException($"--tariff-cert: '{path}' contains no certificate.");
            var key = leaf.GetECDsaPrivateKey()
                ?? throw new ArgumentException("--tariff-cert: the station needs an ECDSA private key to sign.");
            Console.WriteLine($"Tariff: signing with {leaf.Subject}, {key.KeySize}-bit EC.");
            return key;
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
