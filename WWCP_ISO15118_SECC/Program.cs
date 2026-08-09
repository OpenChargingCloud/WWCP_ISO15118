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
using cloud.charging.open.protocols.ISO15118.Security;
using cloud.charging.open.protocols.ISO15118.Session;
using cloud.charging.open.protocols.ISO15118.SharedCC;
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
            var trust = args.TrustRootsPath is null ? null : TrustRoots.Load(args.TrustRootsPath);
            if (trust is not null)
                Console.WriteLine($"Trust roots: {string.Join(", ", trust.RootSubjects)}");

            var (serverLeaf, serverChain) = Credentials.LoadForTls(args.ServerCertPath, args.ServerCertPass, "--server-cert");
            using var devCert = args.TlsStack == TlsStack.Dotnet && serverLeaf is null ? CreateDevCertificate() : null;
            var dotnetTls = args.TlsStack != TlsStack.Dotnet ? null : new TlsOptions
            {
                ServerCertificate         = serverLeaf ?? devCert,
                ServerCertificateChain    = serverChain,
                EnabledSslProtocols       = SslProtocols.Tls12 | SslProtocols.Tls13,
                RequireClientCertificate  = args.RequireClientCert,
                // With roots configured the car's chain is actually checked; without them the old dev
                // behaviour stands and any presented certificate is accepted.
                ClientCertificateValidation = !args.RequireClientCert ? null
                    : trust is null
                          ? (_, _, _, _) => true
                          // The chain argument carries what the car put on the wire beyond its leaf; without
                          // it a car that sends its Sub-CAs is judged as though it had sent none.
                          : (_, cert, chain, _) => cert is not null
                                && Report("TLS client", trust.Validate(new X509Certificate2(cert), TrustRoots.PeerIntermediates(chain))),
            };
            if (serverLeaf is not null)
                Console.WriteLine($"Presenting server certificate: {serverLeaf.Subject} (+{serverChain?.Count ?? 0} intermediate(s))"
                                  + (args.RequireClientCert
                                         ? trust is not null
                                               ? "; requiring a client certificate and validating its chain"
                                               : "; requiring a client certificate (mutual TLS, dev: accept-any)"
                                         : ""));

            // Two ways into the -20-faithful backend, and the difference is whose PKI it is. --pki-dir is
            // the dev loopback: this side mints the hierarchy and pins the car it minted. --server-cert is
            // a run against a car whose material is not ours, where there is nothing to pin and the peer is
            // judged by --trust-roots or not at all.
            var bcTls = args.TlsStack != TlsStack.BouncyCastle
                            ? null
                            : args.ServerCertPath is not null
                                  ? SeccPki.WithServerCertificate(args.ServerCertPath, args.ServerCertPass, args.RequireClientCert)
                                  : SeccPki.Generate(args.PkiDir!);
            if (bcTls is not null && trust is not null)
                bcTls = bcTls with { ValidatePeerChain = c => Report("TLS client", trust.Validate(c[0], c[1..])) };
            if (bcTls is not null && trust is null && args.RequireClientCert && args.ServerCertPath is not null)
                Console.WriteLine("WARNING: requiring a client certificate and accepting ANY of them — " +
                                  "nothing to pin against a foreign PKI, and no --trust-roots given. Dev tool only.");

            using var listener = bcTls is not null
                                     ? new TcpV2GListener(new IPEndPoint(IPAddress.IPv6Any, args.ListenPort), bcTls)
                                     : new TcpV2GListener(new IPEndPoint(IPAddress.IPv6Any, args.ListenPort), dotnetTls);

            Console.WriteLine($"SECC listening on {listener.LocalEndpoint} " +
                              $"(protocol {(args.OfferBoth ? "both (-20 preferred)" : V2GInterface.Name(args.Protocol))}, " +
                              $"{V2GInterface.Name(args.Mode)}, TLS {args.TlsStack})...");

            await using var sdp = args.UseSdp ? await StartSdpAsync(args, listener.LocalEndpoint.Port) : null;

            // Pause/resume: a session that ends with ChargingSession.Pause hands back what the next
            // connection needs to rejoin it — keep accepting connections and offer that, so the EV can come
            // back (OK_OldSessionJoined). For -20 "the EV" is meant literally: the offer includes a binding
            // to the vehicle certificate, and a different car naming this session id gets a new session.
            ResumableSession? paused = null;
            do
            {
                using var stream = await listener.AcceptAsync();
                var transport = TransportOf(args, stream);
                SeccOptions sessionArgs;
                if (args.OfferBoth)
                {
                    // A mini-IsoMux: accept both protocols, follow the EV's priority, and run the state
                    // machine the handshake settled on rather than one chosen before it ran.
                    var settled = await SapHandshake.RunSeccSideAsync(stream, BothOffers(args.Mode), transport: transport);
                    Console.WriteLine($"SAP: the EV's offer settled on {V2GInterface.Name(settled.Protocol)}.");
                    sessionArgs = args with { Protocol = settled.Protocol };
                }
                else
                {
                    await SapHandshake.RunSeccSideAsync(stream, args.Protocol, mode: args.Mode, transport: transport);
                    sessionArgs = args;
                }
                paused = await RunSessionAsync(stream, sessionArgs, trust, paused);
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

        /// <summary>
        /// The station's half of the same question the car answers in <c>EVCC/Program.cs</c>:
        /// <c>[V2G20-2356]</c> forbids <i>selecting</i> ISO 15118-20 on plain TCP or TLS 1.2 and below,
        /// whatever the car offered.
        /// </summary>
        /// <remarks>
        /// Deliberately the same shape as the car's, including the standing down: this station answers most
        /// of the matrix over plain TCP on purpose. What it will not do any more is answer <c>-20</c> there
        /// without saying so — which is exactly the finding filed against EVerest's <c>IsoMux</c>
        /// (<c>ISO15118ConformanceTests/docs/reports/everest-isomux-iso20-over-tls12.md</c>), and the reason
        /// the obligation binds the station separately from the car is the case where the car is the one
        /// getting it wrong.
        /// </remarks>
        private static TransportSecurity TransportOf(SeccOptions args, Stream stream)
        {
            var actual = Iso20Transport.Of(stream);

            if (args.Protocol != ProtocolVariant.Iso15118_20 && !args.OfferBoth)
                return actual;

            if (Iso20Transport.MayCarryIso20(actual))
                return actual;

            Console.WriteLine($"SAP: prepared to select ISO 15118-20 on {Iso20Transport.Describe(actual)} — "
                            + "[V2G20-2356] says a station should not, and this one will anyway because that "
                            + "is what the run is for. Use TLS 1.3 for a conformant session.");
            return TransportSecurity.Unknown;
        }

        /// <summary>Runs one session; returns the session id when it ended <b>paused</b> (offer it to the
        /// next session as the resume id), else <c>null</c>.</summary>
        private static async Task<ResumableSession?> RunSessionAsync(Stream stream, SeccOptions args,
                                                                     V2GChainValidator? trust, ResumableSession? resume = null)
        {
            if (args.Protocol == ProtocolVariant.Iso15118_2)
            {
                var secc2 = new Secc2(args.Mode, TimeSpan.FromSeconds(60), TimeProvider.System)
                {
                    ContractChainValidator = trust,
                    ResumeSessionId = resume?.SessionId,
                    RequestRenegotiation = args.Renegotiate,
                    TariffSignKey = args.TariffCertPath is not null
                        ? LoadTariffKey(args.TariffCertPath, args.TariffCertPass) : null,
                };
                try { await secc2.RunAsync(stream); }
                finally
                {
                    if (secc2.PnCAuth is { } pnc2)
                        Console.WriteLine($"-2 Plug & Charge: contract {pnc2.ContractSubject}; challenge {(pnc2.ChallengeOk ? "OK" : "MISMATCH")}, " +
                                          $"digest {(pnc2.DigestOk ? "OK" : "FAIL")}, signature {(pnc2.SignatureOk ? "OK" : "FAIL")}" +
                                          $"{(pnc2.SignatureOk ? $" (grammar={pnc2.SignatureGrammar})" : "")}; " +
                                          $"chain {Describe(pnc2.Chain)}.");
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
                secc.ContractChainValidator = trust;
                secc.PreferDynamicControlMode = args.PreferDynamic;
                secc.OfferPlugAndCharge       = !args.NoPnc;
                secc.OfferResume(resume);
                secc.RequestRenegotiation = args.Renegotiate;
                if (args.TariffCertPath is not null)
                    secc.TariffSignKey = LoadTariffKey(args.TariffCertPath, args.TariffCertPass);
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

        /// <summary>
        /// A chain verdict as one phrase. "not checked" is spelled out rather than shown as a failure,
        /// because a conformance run must never let "we did not look" read like "we looked and it was bad".
        /// </summary>
        private static string Describe(ChainResult result)
            => result.Ok                        ? $"valid (anchored at {result.Anchor})"
             : result == ChainResult.NotConfigured ? "not checked — no --trust-roots"
             :                                    $"REJECTED — {result.Reason}";

        /// <summary>
        /// Turns a chain verdict into the boolean a TLS callback needs, and says out loud what it decided.
        /// A refused handshake otherwise surfaces as a bare connection reset, with the reason — expired,
        /// unknown root, broken signature — known here and printed nowhere.
        /// </summary>
        private static bool Report(string what, ChainResult result)
        {
            Console.WriteLine(result.Ok
                                  ? $"{what}: chain valid, anchored at {result.Anchor}."
                                  : $"{what}: chain REJECTED — {result.Reason}");
            return result.Ok;
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
                                  $"({pnc.SignatureMethod}{(pnc.SignatureOk ? $", grammar={pnc.SignatureGrammar}" : "")}); " +
                                  $"chain {Describe(pnc.Chain)}.");
            if (secc.CertInstall is { } ci)
                Console.WriteLine($"CertificateInstallation: OEM {ci.OemSubject}; digest {(ci.DigestOk ? "OK" : "FAIL")}, " +
                                  $"signature {(ci.SignatureOk ? "OK" : "FAIL")}{(ci.SignatureOk ? $" (grammar={ci.SignatureGrammar})" : "")}, " +
                                  $"contract issued ({(ci.EncryptedForOem ? "key wrapped for OEM key" : "OEM key not P-521 — blob undecryptable for EV")}); " +
                                  $"OEM chain {Describe(ci.Chain)}.");
        }

        private static async Task RunSlacAsync(SeccOptions args)
        {
            await using var transport = new UdpSlacTransport(V2GInterface.RandomMac(), new IPEndPoint(IPAddress.Any, args.SlacListenPort));
            await using var slac = new SlacEvseStage(transport,
                new EvseSlacOptions { EvseId = new byte[17], Nid = RandomNumberGenerator.GetBytes(7), Nmk = RandomNumberGenerator.GetBytes(16) });
            await slac.StartAsync();
            Console.WriteLine($"SLAC: EVSE listening on UDP :{args.SlacListenPort} for a PEV...");
            var result = await slac.WaitForMatchAsync();
            Console.WriteLine($"SLAC: paired (NID {Convert.ToHexString(result.Nid)}).");
        }

        private static async Task<SeccSdpAdvertiser> StartSdpAsync(SeccOptions args, int tcpPort)
        {
            var iface  = V2GInterface.Resolve(args.Interface!);
            var noTls  = args.TlsStack == TlsStack.None;
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
        // Everything a car would need in the same shape lives in WWCP_ISO15118_SharedCC; what is left
        // here is what only a station does.

        /// <summary>The tariff <b>signing</b> key: the leaf's ECDSA private key, which signs the -2
        /// SalesTariffs / -20 AbsolutePriceSchedule offer. The verifying half is the car's, and lives in
        /// the EVCC program — same file, opposite key.</summary>
        private static ECDsa LoadTariffKey(string path, string? password)
        {
            var (key, subject) = Credentials.LoadEcdsaKey(path, password, wantPrivate: true, "--tariff-cert");
            Console.WriteLine($"Tariff: signing with {subject}, {key.KeySize}-bit EC.");
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
