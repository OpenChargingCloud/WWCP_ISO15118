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

using IPPort = org.GraphDefined.Vanaheimr.Hermod.IPPort;

using cloud.charging.open.protocols.ISO15118.StateMachines;
using cloud.charging.open.protocols.ISO15118.SharedCC;

namespace cloud.charging.open.protocols.ISO15118.SECC
{
    /// <summary>
    /// The station's command line. Hand-rolled parsing (no arg-parsing package anywhere in this repo).
    /// </summary>
    /// <remarks>
    /// There is no role subcommand: this program <i>is</i> the station, so every flag here is a
    /// station's flag. The car's flags — <c>--connect</c>, <c>--contract-cert</c>, <c>--pause-resume</c>
    /// — are not accepted and not documented here, which is the point of the split: <c>--help</c> now
    /// shows what this side can do rather than the union of two roles.
    /// </remarks>
    public sealed record SeccOptions(
        IPPort ListenPort, ProtocolVariant Protocol, bool OfferBoth, PowerMode Mode, bool Mcs,
        TlsStack TlsStack, bool UseSdp, string? Interface, bool SdpLoopback, bool PreferDynamic, bool NoPnc,
        bool UseSlac, IPPort SlacListenPort, string? PkiDir,
        string? ServerCertPath, string? ServerCertPass, bool RequireClientCert, string? TrustRootsPath,
        bool Renegotiate, string? TariffCertPath, string? TariffCertPass,
        string? MeterId = null, int ClockSkewSeconds = 0,
        uint MeteringConfirmationEvery = 0, bool RequireMeteringConfirmation = false)
    {

        /// <summary>The IANA-registered V2G port, so a bare run needs no flag at all.</summary>
        public static readonly IPPort DefaultListenPort = IPPort.Parse(15118);

        public static SeccOptions Parse(string[] args)
        {
            // A station that speaks only one protocol is the exception, not the rule: a real SECC takes
            // whatever drives up. So "both" is the default and each connection's SupportedAppProtocol
            // handshake settles it — ask for less with --protocol 2 or --protocol 20.
            IPPort listenPort = DefaultListenPort, slacListenPort = IPPort.Zero;
            var protocol = ProtocolVariant.Iso15118_20;
            var offerBoth = true;
            var mode = PowerMode.Dc;
            var mcs = false;
            var backend = TlsStack.None;
            bool tls = false, useSdp = false, useSlac = false, sdpLoopback = false;
            bool preferDynamic = false, noPnc = false, requireClientCert = false, renegotiate = false;
            string? iface = null, pkiDir = null, serverCertPath = null, serverCertPass = null, trustRootsPath = null;
            string? tariffCertPath = null, tariffCertPass = null;
            string? meterId = null;
            int clockSkewSeconds = 0;
            uint meteringConfirmationEvery = 0;
            bool requireMeteringConfirmation = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--listen":
                        listenPort = ParsePort(args[++i], "--listen");
                        break;
                    // "both": accept an offer naming either protocol and run the state machine the
                    // handshake settled on. Protocol keeps the top preference, which is what everything
                    // decided before the handshake reads — the banner, and the TLS profile.
                    case "--protocol":
                        (protocol, offerBoth) = args[++i] switch
                        {
                            "2"    => (ProtocolVariant.Iso15118_2,  false),
                            "20"   => (ProtocolVariant.Iso15118_20, false),
                            "both" => (ProtocolVariant.Iso15118_20, true),
                            var v  => throw new ArgumentException($"--protocol expects 2, 20 or both, got '{v}'."),
                        };
                        break;
                    // "mcs" is not a third power mode: MCS rides the DC message set under
                    // energy-transfer services 8 / 9 with a megawatt envelope, so it sets Dc and
                    // raises a flag beside it. Only -20 has those services; Validate() says so.
                    case "--mode":
                        (mode, mcs) = args[++i] switch
                        {
                            "ac"  => (PowerMode.Ac, false),
                            "dc"  => (PowerMode.Dc, false),
                            "mcs" => (PowerMode.Dc, true),
                            var v => throw new ArgumentException($"--mode expects ac, dc or mcs, got '{v}'."),
                        };
                        break;
                    case "--tls":
                        tls = true;
                        break;
                    case "--tls-backend":
                        backend = args[++i] switch
                        {
                            "dotnet" => TlsStack.Dotnet,
                            "bc" or "bouncycastle" => TlsStack.BouncyCastle,
                            var v => throw new ArgumentException($"--tls-backend expects dotnet or bc, got '{v}'."),
                        };
                        break;
                    case "--sdp":
                        useSdp = true;
                        break;
                    // A station and a car on one machine. See
                    // SECC_SDPServerOptions.MulticastLoopback for why this is
                    // the socket that decides on Windows, and why the car's own
                    // switch cannot stand in for it.
                    case "--sdp-loopback":
                        useSdp      = true;
                        sdpLoopback = true;
                        break;
                    case "--interface":
                        iface = args[++i];
                        break;
                    case "--dynamic":
                        preferDynamic = true;
                        break;
                    // Offer EIM only. Legal either way; some EVs cannot cope with a list containing a
                    // service they do not support — see Secc20Base.OfferPlugAndCharge.
                    case "--no-pnc":
                        noPnc = true;
                        break;
                    case "--slac":
                        useSlac = true;
                        break;
                    case "--slac-listen":
                        slacListenPort = ParsePort(args[++i], "--slac-listen");
                        break;
                    case "--pki-dir":
                        pkiDir = args[++i];
                        break;
                    case "--server-cert":
                        serverCertPath = args[++i];
                        break;
                    case "--server-cert-pass":
                        serverCertPass = args[++i];
                        break;
                    case "--require-client-cert":
                        requireClientCert = true;
                        break;
                    // The V2G root(s) a car's certificates must chain to — its TLS chain, its contract
                    // chain and its OEM provisioning chain. One file, or a directory of them.
                    case "--trust-roots":
                        trustRootsPath = args[++i];
                        break;
                    case "--renegotiate":
                        renegotiate = true;
                        break;
                    case "--tariff-cert":
                        tariffCertPath = args[++i];
                        break;
                    case "--tariff-cert-pass":
                        tariffCertPass = args[++i];
                        break;
                    // A station reports a meter reading only when it has a meter, and this one signs.
                    // Without the flag there is no MeterInfo at all, whatever the car asks for.
                    case "--meter":
                        meterId = args[++i];
                        break;
                    // The first fault switch: a controller whose clock is wrong by this much. It moves
                    // the header TimeStamp of every -20 response and nothing else — the meter keeps the
                    // system's time, so the two clocks of one station can be seen to disagree.
                    case "--clock-skew":
                        if (!int.TryParse(args[++i], out clockSkewSeconds))
                            throw new ArgumentException($"--clock-skew expects whole seconds, got '{args[i]}'.");
                        break;
                    // -20: ask a Plug & Charge car to sign the meter reading every n charge-loop responses
                    // (EVSENotification MeteringConfirmation). Validate() refuses the combinations under
                    // which it would never ask.
                    case "--metering-confirmation":
                        if (!uint.TryParse(args[++i], out meteringConfirmationEvery) || meteringConfirmationEvery == 0)
                            throw new ArgumentException($"--metering-confirmation expects a count of charge-loop responses of 1 or more, got '{args[i]}'.");
                        break;
                    // Answer a confirmation that does not hold with FAILED_MeteringSignatureNotValid, which
                    // ends the session, instead of recording it and answering OK.
                    case "--require-metering-confirmation":
                        requireMeteringConfirmation = true;
                        break;
                    case "--help" or "-h":
                        throw new ArgumentException(Usage);
                    default:
                        throw new ArgumentException($"unknown argument '{args[i]}'.\n{Usage}");
                }
            }

            // --tls is shorthand for the .NET backend; --tls-backend wins if both are given.
            if (backend == TlsStack.None && tls)
                backend = TlsStack.Dotnet;

            Validate(listenPort, backend, useSdp, iface, useSlac, slacListenPort, pkiDir, mcs, protocol, offerBoth, trustRootsPath, serverCertPath);
            ValidateMeteringConfirmation(meteringConfirmationEvery, requireMeteringConfirmation, meterId, noPnc, protocol, offerBoth);

            return new SeccOptions(listenPort, protocol, offerBoth, mode, mcs, backend,
                                   useSdp, iface, sdpLoopback, preferDynamic, noPnc, useSlac, slacListenPort, pkiDir,
                                   serverCertPath, serverCertPass, requireClientCert, trustRootsPath,
                                   renegotiate, tariffCertPath, tariffCertPass,
                                   meterId, clockSkewSeconds,
                                   meteringConfirmationEvery, requireMeteringConfirmation);
        }

        /// <summary>
        /// Refuses a metering-confirmation request the station could never act on, for the reason --mode mcs
        /// against -2 is refused: a run that quietly asks for nothing would read as a car that never answered.
        /// </summary>
        private static void ValidateMeteringConfirmation(uint every, bool require, string? meterId, bool noPnc,
                                                         ProtocolVariant protocol, bool offerBoth)
        {
            if (require && every == 0)
                throw new ArgumentException("--require-metering-confirmation needs --metering-confirmation <n>.");

            if (every == 0)
                return;

            // [V2G20-1083]: asking carries the reading, and without a meter there is none.
            if (meterId is null)
                throw new ArgumentException("--metering-confirmation needs --meter <id>: the request carries the meter reading.");

            // [V2G20-2704]: the car signs with its contract key, so only a Plug & Charge session can be asked.
            if (noPnc)
                throw new ArgumentException("--metering-confirmation needs Plug & Charge; drop --no-pnc.");

            if (protocol != ProtocolVariant.Iso15118_20 && !offerBoth)
                throw new ArgumentException("--metering-confirmation is an ISO 15118-20 exchange; drop --protocol 2.");
        }

        private static void Validate(IPPort listenPort, TlsStack backend, bool useSdp, string? iface,
                                     bool useSlac, IPPort slacListenPort, string? pkiDir,
                                     bool mcs, ProtocolVariant protocol, bool offerBoth, string? trustRootsPath,
                                     string? serverCertPath)
        {
            if (listenPort.IsZero)
                throw new ArgumentException($"--listen expects a port between 1 and 65535, got {listenPort}.");

            // Two ways to give this backend a server identity, and it needs exactly one of them: --pki-dir
            // mints a dev hierarchy and pins the car it minted, --server-cert brings a real chain for a car
            // whose PKI is not ours. Neither leaves the station with nothing to present.
            if (backend == TlsStack.BouncyCastle && pkiDir is null && serverCertPath is null)
                throw new ArgumentException(
                    "--tls-backend bc needs a server identity: --pki-dir <dir> to mint a dev hierarchy, " +
                    "or --server-cert <pfx> to present a real one.");

            if (useSdp && iface is null)
                throw new ArgumentException("--sdp requires --interface <name> (the V2G network interface).");

            if (useSlac && slacListenPort.IsZero)
                throw new ArgumentException("--slac requires --slac-listen <port>.");

            // A file or a directory, so this only asks that it is one of them.
            if (trustRootsPath is not null && !File.Exists(trustRootsPath) && !Directory.Exists(trustRootsPath))
                throw new ArgumentException($"--trust-roots: no such file or directory '{trustRootsPath}'.");

            // Energy-transfer services 8 / 9 exist in no other catalogue, so --mode mcs against -2 is a
            // request that cannot be met. Refused here rather than quietly running plain DC, because a
            // session that silently degrades is the one failure an MCS run must not produce. Offering
            // both is fine — the handshake can still settle on -20, and only the -2 half cannot carry it.
            if (mcs && protocol != ProtocolVariant.Iso15118_20 && !offerBoth)
                throw new ArgumentException("--mode mcs is an ISO 15118-20 session; drop --protocol 2.");
        }

        private static IPPort ParsePort(string value, string flag)
            => IPPort.TryParse(value, out var port) ? port : throw new ArgumentException($"{flag} expects a port number, got '{value}'.");

        public const string Usage =
            "The ISO 15118 charging station (SECC): accept an EV and run a charging session.\n" +
            "\n" +
            "usage: WWCP_ISO15118_SECC [options]\n" +
            "\n" +
            "  --listen <port>        TCP port to accept V2G connections on (default: 15118)\n" +
            "  --protocol 2|20|both   which protocol to accept (default: both, -20 preferred)\n" +
            "                         both = accept an offer naming either and run whichever the car\n" +
            "                         settles on, decided per connection\n" +
            "  --mode ac|dc|mcs       energy transfer mode (default: ac)\n" +
            "                         mcs = the DC message set under energy-transfer services 8/9 with\n" +
            "                         a megawatt envelope; -20 only\n" +
            "\n" +
            "  TLS:    --tls                       .NET SslStream with a fresh self-signed dev certificate\n" +
            "          --tls-backend dotnet|bc     bc = the -20-faithful profile (TLS 1.3, secp521r1,\n" +
            "                                      mutual); needs --pki-dir <dir> to mint a dev hierarchy\n" +
            "                                      or --server-cert <pfx> to present a real one. Its\n" +
            "                                      CertificateRequest asks for ecdsa_secp521r1_sha512 or\n" +
            "                                      ed448, so a car with a P-256 client certificate cannot\n" +
            "                                      answer it — that is the profile, use dotnet for those.\n" +
            "          --server-cert <pfx> [--server-cert-pass <pw>]   present this chain instead\n" +
            "          --require-client-cert       demand the car's certificate (mutual TLS)\n" +
            "          --trust-roots <file|dir>    validate the car's chains against these V2G root(s) —\n" +
            "                                      its TLS chain, its contract chain and its OEM\n" +
            "                                      provisioning chain. Without it none of them is checked.\n" +
            "                                      A directory holds several roots. Put any intermediates\n" +
            "                                      the car does not send itself in here too; they can only\n" +
            "                                      act as intermediates, never as trust anchors.\n" +
            "  SDP:    --sdp --interface <name>    advertise the endpoint instead of a fixed one\n" +
            "          --sdp-loopback             also hear a car running on this same machine\n" +
            "  Auth:   --no-pnc                    -20: advertise EIM only, not EIM + Plug & Charge\n" +
            "  Mode:   --dynamic                   -20: offer the Dynamic control-mode set first\n" +
            "  Tariff: --tariff-cert <pfx> [--tariff-cert-pass <pw>]   sign the SalesTariff / -20\n" +
            "                                      AbsolutePriceSchedule offer with this key\n" +
            "  Reneg:  --renegotiate               notify ReNegotiation/ServiceRenegotiation once mid-loop\n" +
            "  SLAC:   --slac --slac-listen <port> run a SLAC pairing stage first\n" +
            "  Meter:  --meter <id>                install a signing meter with this id; without it the\n" +
            "                                      station reports no MeterInfo at all\n" +
            "          --metering-confirmation <n> -20: ask a Plug & Charge car to sign the meter reading\n" +
            "                                      every n charge-loop responses (never the first); needs\n" +
            "                                      --meter. The verdict is printed and the answer is OK.\n" +
            "          --require-metering-confirmation\n" +
            "                                      answer a confirmation that does not hold with\n" +
            "                                      FAILED_MeteringSignatureNotValid, ending the session\n" +
            "  Faults: --clock-skew <seconds>      run the controller's clock this far ahead (negative:\n" +
            "                                      behind). The meter keeps the system's time.\n" +
            "\n" +
            "The car is the other program: WWCP_ISO15118_EVCC.";
    }
}
