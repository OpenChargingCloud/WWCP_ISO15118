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
using cloud.charging.open.protocols.ISO15118.Security;
using cloud.charging.open.protocols.ISO15118.Session;
using cloud.charging.open.protocols.ISO15118.SharedCC;
using cloud.charging.open.protocols.ISO15118.Simulation;
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
            var trust = args.TrustRootsPath is null ? null : TrustRoots.Load(args.TrustRootsPath);
            if (trust is not null)
                Console.WriteLine($"Trust roots: {string.Join(", ", trust.RootSubjects)}");
            using var stream = await ConnectAsync(args, host, port, trust);

            if (args.OfferBoth)
            {
                // The state machine is chosen AFTER the handshake: offer both, run whichever the
                // station picked — the case a multiplexing station (EVerest's IsoMux) exists for.
                var accepted = await SapHandshake.RunEvccSideAsync(stream, BothOffers(args.Mode));
                Console.WriteLine($"SAP: offered -20 (priority 1) and -2 (priority 2); " +
                                  $"the station picked {V2GInterface.Name(accepted.Protocol)}.");
                return await RunSessionAsync(stream, args with { Protocol = accepted.Protocol }, pause, resume);
            }

            await SapHandshake.RunEvccSideAsync(stream, args.Protocol, mode: args.Mode);
            return await RunSessionAsync(stream, args, pause, resume);
        }

        /// <summary>
        /// Turns a chain verdict into the boolean a TLS callback needs, and says what it decided — a
        /// refused handshake is otherwise a bare connection reset with the reason known only in here.
        /// </summary>
        private static bool Report(string what, ChainResult result)
        {
            Console.WriteLine(result.Ok
                                  ? $"{what}: chain valid, anchored at {result.Anchor}."
                                  : $"{what}: chain REJECTED — {result.Reason}");
            return result.Ok;
        }

        /// <summary>-20 first, -2 second: what a dual-stack car offers, and the order the station reads.</summary>
        private static SapOffer[] BothOffers(PowerMode mode) =>
            [new(ProtocolVariant.Iso15118_20, mode), new(ProtocolVariant.Iso15118_2, mode)];

        private static async Task<Stream> ConnectAsync(EvccOptions args, string host, int port, V2GChainValidator? trust)
        {
            switch (args.TlsStack)
            {
                case TlsStack.BouncyCastle:
                {
                    // Two ways in, and the difference matters: --pki-dir is the dev loopback, where the
                    // station minted this car's Vehicle chain and we read it back; --vehicle-cert is a run
                    // against a station whose PKI is not ours, where the car brings its own identity and
                    // there is nothing to pin the peer against beyond what the caller trusts.
                    var bc = args.VehicleCertPath is not null
                                 ? EvccPki.WithVehicleCertificate(args.VehicleCertPath, args.VehicleCertPass, args.PkiDir)
                                 : EvccPki.Load(args.PkiDir!);
                    // Trust roots and pinning are not alternatives: pinning says "this exact station",
                    // chaining says "a station some V2G root vouches for". Whichever is configured runs,
                    // and both do when both are.
                    if (trust is not null)
                        bc = bc with { ValidatePeerChain = c => Report("TLS station", trust.Validate(c[0], c[1..])) };
                    return await TcpV2GClient.ConnectAsync(host, port, bc);
                }

                case TlsStack.Dotnet:
                {
                    if (trust is null)
                        Console.WriteLine("WARNING: accepting any TLS server certificate — no --trust-roots given. Dev tool only.");
                    var (vehicleLeaf, vehicleChain) = Credentials.LoadForTls(args.VehicleCertPath, args.VehicleCertPass, "--vehicle-cert");
                    var tlsOptions = new TlsOptions
                    {
                        ServerCertificateValidation = trust is null
                            ? (_, _, _, _) => true
                            // The chain argument carries what the station put on the wire beyond its leaf;
                            // without it a station that sends its Sub-CAs is judged as though it had sent none.
                            : (_, cert, chain, _) => cert is not null
                                  && Report("TLS station", trust.Validate(new X509Certificate2(cert), TrustRoots.PeerIntermediates(chain))),
                        // Negotiate TLS 1.2 or 1.3 so this interoperates with a peer in either mode (Josev's
                        // SECC serves TLS 1.2 unilateral by default, TLS 1.3 mutual with ENABLE_TLS_1_3=True).
                        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                        ClientCertificate = vehicleLeaf,
                        ClientCertificateChain = vehicleChain,
                    };
                    if (vehicleLeaf is not null)
                        Console.WriteLine($"Presenting Vehicle certificate for mutual TLS: {vehicleLeaf.Subject} (+{vehicleChain?.Count ?? 0} intermediate(s))");
                    return await TcpV2GClient.ConnectAsync(host, port, tlsOptions);
                }

                default:
                    return await TcpV2GClient.ConnectAsync(host, port);
            }
        }

        /// <summary>
        /// The car's battery, or null when this run is a message sequence rather than a charging session.
        /// </summary>
        /// <remarks>
        /// The defaults are the ones a driver would not have to think about: a 60 kWh pack, a state of
        /// charge somewhere between 10 and 60 % because a car that always arrives at exactly half full is
        /// a worse simulation than a random one, and — when no goal is named at all — full.
        /// </remarks>
        private static EvBattery? BuildBattery(EvccOptions args)
        {
            if (!args.HasBattery)
                return null;

            var capacity = args.BatteryKWh ?? EvBattery.DefaultCapacityKWh;
            var soc      = args.StartSoC   ?? Random.Shared.Next(10, 61);

            var battery = new EvBattery(capacity, soc)
            {
                // No goal named: full. The others are limits on top of it, whichever comes first.
                TargetSoC       = args.TargetSoC ?? (args.TargetEnergyKWh is null && args.MaxChargingTime is null
                                                     && args.DepartureIn is null ? 100.0 : null),
                TargetEnergyWh  = args.TargetEnergyKWh * 1000.0,
                MaxDuration     = args.MaxChargingTime,
                DepartureIn     = args.DepartureIn,
                MinimumSoC      = args.MinimumSoC,
                RequestedPowerW = (args.PowerKW ?? 0) * 1000.0,
            };

            var goals = new[]
            {
                battery.TargetSoC      is { } t   ? $"{t:F0} %" : null,
                battery.TargetEnergyWh is { } e   ? $"{e / 1000:F1} kWh delivered" : null,
                battery.MaxDuration    is { } d   ? $"{d.TotalMinutes:F0} min" : null,
                battery.DepartureIn    is { } dep ? $"departure in {dep.TotalMinutes:F0} min" : null,
            }.Where(x => x is not null);

            Console.WriteLine($"Battery: {capacity:F1} kWh at {soc:F0} %" +
                              (args.PowerKW is { } p ? $", asking for {p:F1} kW" : "") +
                              $" — charging until {string.Join(", ", goals)} (whichever comes first)." +
                              (battery.MinimumSoC is { } min ? $" The driver needs {min:F0} % by then." : ""));

            return battery;
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
                    Battery = BuildBattery(args),
                };
                if (args.ContractCertPath is not null)
                    evcc.Pnc = LoadContractCredentials(args.ContractCertPath, args.ContractCertPass);
                if (args.TariffCertPath is not null)
                    evcc.TariffVerifyKey = LoadTariffVerifyKey(args.TariffCertPath, args.TariffCertPass);
                // Accepted for -2 and refused nowhere, but there is no -2 path that uses it: our Evcc2 has
                // no CertificateInstallation. Said out loud rather than ignored, because a flag that is
                // quietly dropped is worse than one that is not offered.
                if (args.OemCertPath is not null)
                    Console.WriteLine("--oem-cert: ignored for -2 — this EVCC implements CertificateInstallation " +
                                      "only for -20. Run with --protocol 20 to use it.");
                await evcc.RunAsync();
                Console.WriteLine($"  {evcc.Exchanges} exchanges, {evcc.BytesOnWire} bytes on the wire (request side), " +
                                  $"auth: {evcc.AuthorizationMode}, metering receipts sent: {evcc.MeteringReceiptsSent}, " +
                                  $"renegotiations: {evcc.Renegotiations}, session setup: {evcc.SessionSetupCode}.");
                if (evcc.Battery is { } b2 && evcc.BatteryStop is { } s2)
                    Console.WriteLine("  " + b2.Describe(s2));
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
                evcc.Battery = BuildBattery(args);
                // The one battery goal that is also a protocol field: -20 carries DepartureTime as seconds
                // from the session's time anchor, and a Dynamic station schedules against it.
                if (args.DepartureIn is { } departure)
                    evcc.DepartureTime = (uint) Math.Max(1, Math.Round(departure.TotalSeconds));
                if (args.ContractCertPath is not null)
                    evcc.Pnc = LoadContractCredentials(args.ContractCertPath, args.ContractCertPass);
                if (args.OemCertPath is not null)
                    evcc.CertInstallRequest = LoadOemCredentials(args.OemCertPath, args.OemCertPass);
                if (args.TariffCertPath is not null)
                    evcc.TariffVerifyKey = LoadTariffVerifyKey(args.TariffCertPath, args.TariffCertPass);
                await evcc.RunAsync();
                Console.WriteLine($"  {evcc.Exchanges} exchanges, {evcc.BytesOnWire} bytes on the wire (request side), " +
                                  $"auth: {evcc.AuthorizationMode}, session setup: {evcc.SessionSetupCode}.");
                if (evcc.Battery is { } b20 && evcc.BatteryStop is { } s20)
                    Console.WriteLine("  " + b20.Describe(s20));
                if (evcc.InstalledContractCertificate is not null)
                    Console.WriteLine("  CertificateInstallation: a contract certificate was issued and its private " +
                                      "key unwrapped — the ECDH/AES-GCM round trip closed.");
                else if (args.OemCertPath is not null)
                    Console.WriteLine("  CertificateInstallation: not completed — the station either did not offer " +
                                      "the service or refused the request.");
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

            var iface = V2GInterface.Resolve(args.Interface!);
            var discovery = new SdpSeccDiscovery(new EVCC_SDPClientOptions
            {
                Interface            = iface,
                RequestedSecurity    = args.TlsStack == TlsStack.None ? SDP_Security.NoTLS : SDP_Security.TLS,
                // Single-host interop (the SECC on the same machine, e.g. Josev in Docker/WSL): the
                // ff02::1 SDP_Request only reaches a LOCAL SECC via multicast loopback — with the
                // client's real-hardware default (off) the discovery times out against a healthy SECC
                // (root-caused live 2026-07-23). Harmless on real networks: the client discards its
                // own looped-back request by V2GTP payload type.
                MulticastLoopback    = true,
                // A plaintext run must accept the NoTLS response it asked for — the client's CRA/NIS2
                // default rejects it (the EVCC-side mirror of the SECC's RejectNoTlsRequests policy).
                RejectNoTlsResponses = args.TlsStack != TlsStack.None,
            });
            Console.WriteLine($"SDP: discovering the SECC on {iface.Name}...");
            var endpoint = await discovery.DiscoverAsync();
            Console.WriteLine($"SDP: found SECC at [{endpoint.Host}]:{endpoint.Port} (TLS {endpoint.Tls}).");
            return (endpoint.Host, endpoint.Port);
        }

        private static async Task RunSlacAsync(EvccOptions args)
        {
            var peer = new IPEndPoint(IPAddress.Parse(args.SlacPeerHost!), args.SlacPeerPort);
            await using var transport = new UdpSlacTransport(V2GInterface.RandomMac(), new IPEndPoint(IPAddress.Any, 0), bootstrapPeers: [peer]);
            var slac = new SlacEvStage(transport, new EvSlacOptions { PevId = new byte[17] });
            Console.WriteLine($"SLAC: pairing with EVSE at {peer}...");
            var result = await slac.PairAsync();
            Console.WriteLine($"SLAC: paired (NID {Convert.ToHexString(result.Nid)}).");
        }

        // ── helpers ──────────────────────────────────────────────────────────────────────────────────
        // The PKCS#12 mechanics live in WWCP_ISO15118_SharedCC; what is left here is turning them into
        // the shapes only a car has.

        /// <summary>The Plug &amp; Charge <b>contract</b> credentials: who pays.</summary>
        private static PncEvccOptions LoadContractCredentials(string path, string? password)
        {
            var (leaf, subCerts, key, subject) = Credentials.LoadChain(path, password, "--contract-cert");
            Console.WriteLine($"PnC: contract cert {subject} (+{subCerts.Length} sub-CA(s)), key {key.KeySize}-bit EC.");
            return new PncEvccOptions(leaf, subCerts, key);
        }

        /// <summary>
        /// The <b>OEM provisioning</b> credentials: what the car was born with. Set on the -20 EVCC they
        /// make it request CertificateInstallation before authorizing — the chain signed over its own EXI
        /// fragment, and the issued contract key ECDH-unwrapped from the response.
        /// </summary>
        /// <remarks>
        /// The key has to be <b>P-521</b>: the unwrap is an ECDH against the station's ephemeral secp521r1
        /// key, and a -2-era P-256 OEM certificate — which is what Josev ships — cannot take part. The
        /// station answers such a request with a well-formed response the car then cannot decrypt, so the
        /// curve is checked here where it can still be explained rather than at the failure.
        /// </remarks>
        private static CertInstallEvccOptions LoadOemCredentials(string path, string? password)
        {
            var (leaf, subCerts, key, subject) = Credentials.LoadChain(path, password, "--oem-cert", exportable: true);

            if (key.KeySize != 521)
                Console.WriteLine($"WARNING: --oem-cert key is {key.KeySize}-bit, and -20 contract provisioning " +
                                  "agrees on secp521r1. The station's response will be well-formed and " +
                                  "undecryptable for this car.");

            // The same private key twice: once to sign the request, once as an ECDH handle to unwrap the
            // issued contract key. Re-imported rather than cast, because ECDsa and ECDiffieHellman are
            // separate handle types over one keypair.
            var agreement = ECDiffieHellman.Create();
            agreement.ImportECPrivateKey(key.ExportECPrivateKey(), out _);

            Console.WriteLine($"CertificateInstallation: OEM cert {subject} (+{subCerts.Length} sub-CA(s)), key {key.KeySize}-bit EC.");
            return new CertInstallEvccOptions(leaf, subCerts, key, agreement);
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
