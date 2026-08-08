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
        int ListenPort, ProtocolVariant Protocol, bool OfferBoth, PowerMode Mode, bool Mcs,
        TlsStack TlsStack, bool UseSdp, string? Interface, bool PreferDynamic, bool NoPnc,
        bool UseSlac, int SlacListenPort, string? PkiDir,
        string? ServerCertPath, string? ServerCertPass, bool RequireClientCert, string? TrustRootsPath,
        bool Renegotiate, string? TariffCertPath, string? TariffCertPass)
    {

        /// <summary>The IANA-registered V2G port, so a bare run needs no flag at all.</summary>
        public const int DefaultListenPort = 15118;

        public static SeccOptions Parse(string[] args)
        {
            // A station that speaks only one protocol is the exception, not the rule: a real SECC takes
            // whatever drives up. So "both" is the default and each connection's SupportedAppProtocol
            // handshake settles it — ask for less with --protocol 2 or --protocol 20.
            int listenPort = DefaultListenPort, slacListenPort = 0;
            var protocol = ProtocolVariant.Iso15118_20;
            var offerBoth = true;
            var mode = PowerMode.Dc;
            var mcs = false;
            var backend = TlsStack.None;
            bool tls = false, useSdp = false, useSlac = false;
            bool preferDynamic = false, noPnc = false, requireClientCert = false, renegotiate = false;
            string? iface = null, pkiDir = null, serverCertPath = null, serverCertPass = null, trustRootsPath = null;
            string? tariffCertPath = null, tariffCertPass = null;

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
                    case "--help" or "-h":
                        throw new ArgumentException(Usage);
                    default:
                        throw new ArgumentException($"unknown argument '{args[i]}'.\n{Usage}");
                }
            }

            // --tls is shorthand for the .NET backend; --tls-backend wins if both are given.
            if (backend == TlsStack.None && tls)
                backend = TlsStack.Dotnet;

            Validate(listenPort, backend, useSdp, iface, useSlac, slacListenPort, pkiDir, mcs, protocol, offerBoth, trustRootsPath);

            return new SeccOptions(listenPort, protocol, offerBoth, mode, mcs, backend,
                                   useSdp, iface, preferDynamic, noPnc, useSlac, slacListenPort, pkiDir,
                                   serverCertPath, serverCertPass, requireClientCert, trustRootsPath,
                                   renegotiate, tariffCertPath, tariffCertPass);
        }

        private static void Validate(int listenPort, TlsStack backend, bool useSdp, string? iface,
                                     bool useSlac, int slacListenPort, string? pkiDir,
                                     bool mcs, ProtocolVariant protocol, bool offerBoth, string? trustRootsPath)
        {
            if (listenPort is <= 0 or > 65535)
                throw new ArgumentException($"--listen expects a port between 1 and 65535, got {listenPort}.");

            if (backend == TlsStack.BouncyCastle && pkiDir is null)
                throw new ArgumentException("--tls-backend bc requires --pki-dir <dir> (shared V2G certificate material).");

            if (useSdp && iface is null)
                throw new ArgumentException("--sdp requires --interface <name> (the V2G network interface).");

            if (useSlac && slacListenPort == 0)
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

        private static int ParsePort(string value, string flag)
            => int.TryParse(value, out var port) ? port : throw new ArgumentException($"{flag} expects a port number, got '{value}'.");

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
            "                                      mutual); needs --pki-dir <dir>\n" +
            "          --server-cert <pfx> [--server-cert-pass <pw>]   present this chain instead\n" +
            "          --require-client-cert       demand the car's certificate (mutual TLS)\n" +
            "          --trust-roots <file|dir>    validate the car's chains against these V2G root(s) —\n" +
            "                                      its TLS chain, its contract chain and its OEM\n" +
            "                                      provisioning chain. Without it none of them is checked.\n" +
            "                                      A directory holds several roots. Put any intermediates\n" +
            "                                      the car does not send itself in here too; they can only\n" +
            "                                      act as intermediates, never as trust anchors.\n" +
            "  SDP:    --sdp --interface <name>    advertise the endpoint instead of a fixed one\n" +
            "  Auth:   --no-pnc                    -20: advertise EIM only, not EIM + Plug & Charge\n" +
            "  Mode:   --dynamic                   -20: offer the Dynamic control-mode set first\n" +
            "  Tariff: --tariff-cert <pfx> [--tariff-cert-pass <pw>]   sign the SalesTariff / -20\n" +
            "                                      AbsolutePriceSchedule offer with this key\n" +
            "  Reneg:  --renegotiate               notify ReNegotiation/ServiceRenegotiation once mid-loop\n" +
            "  SLAC:   --slac --slac-listen <port> run a SLAC pairing stage first\n" +
            "\n" +
            "The car is the other program: WWCP_ISO15118_EVCC.";
    }
}
