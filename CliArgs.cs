/*
 * Copyright (c) 2021-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of EVSimulatorApp
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

using Vanaheimr.V2G.Simulation.StateMachines;
using Vanaheimr.V2G.Simulation.Transport;

namespace Vanaheimr.V2G.Simulation.Cli
{
    /// <summary>
    /// Hand-rolled flag parsing (no arg-parsing package anywhere in this repo) for the two subcommands.
    /// Beyond protocol/mode/TLS it selects the optional front stages and the TLS backend:
    /// <list type="bullet">
    ///   <item><c>--tls-backend dotnet|bc</c> — .NET SslStream (default when <c>--tls</c>) or BouncyCastle
    ///         (-20-faithful mutual TLS; needs <c>--pki-dir</c>).</item>
    ///   <item><c>--slac</c> — run a SLAC pairing stage first (SECC <c>--slac-listen</c>, EVCC <c>--slac-peer</c>).</item>
    ///   <item><c>--sdp</c> — discover/advertise the endpoint via SDP on <c>--interface</c> instead of a fixed one.</item>
    ///   <item><c>--dynamic</c> — SECC, -20 only: advertise the Dynamic (ControlMode=2) parameter set first,
    ///         so an EV that takes the first offered set (e.g. Josev) runs a Dynamic-mode session.</item>
    ///   <item><c>--no-pnc</c> — SECC, -20 only: advertise EIM only instead of EIM + Plug &amp; Charge, for
    ///         an EV that cannot ignore a service it does not support (see <c>Secc20Base.OfferPlugAndCharge</c>).</item>
    /// </list>
    /// </summary>
    public sealed record CliArgs(
        Role Role, string? ConnectHost, int ConnectPort, int ListenPort,
        ProtocolVariant Protocol, PowerMode Mode, TlsBackend TlsBackend,
        bool UseSdp, string? Interface, bool PreferDynamic, bool NoPnc,
        bool UseSlac, int SlacListenPort, string? SlacPeerHost, int SlacPeerPort,
        string? PkiDir, string? ClientCertPath, string? ClientCertPass,
        string? ServerCertPath, string? ServerCertPass, bool RequireClientCert,
        string? ContractCertPath, string? ContractCertPass, bool PauseResume,
        bool EndPaused, string? ResumeSessionIdHex, bool Renegotiate,
        string? TariffCertPath, string? TariffCertPass,
        bool OfferBoth = false,
        bool Mcs = false)
    {
        public static CliArgs Parse(string[] args)
        {
            if (args.Length == 0 || (args[0] != "evcc" && args[0] != "secc"))
                throw new ArgumentException(Usage);

            var role = args[0] == "evcc" ? Role.Evcc : Role.Secc;
            string? connectHost = null;
            int connectPort = 0, listenPort = 0;
            var protocol = ProtocolVariant.Iso15118_2;
            var offerBoth = false;
            var mode = PowerMode.Ac;
            var mcs  = false;
            var backend = TlsBackend.None;
            bool tls = false;
            bool noPnc = false;
            bool useSdp = false, useSlac = false, requireClientCert = false, preferDynamic = false, pauseResume = false, endPaused = false, renegotiate = false;
            string? resumeSessionIdHex = null;
            string? iface = null, slacPeerHost = null, pkiDir = null, clientCertPath = null, clientCertPass = null;
            string? serverCertPath = null, serverCertPass = null, contractCertPath = null, contractCertPass = null;
            string? tariffCertPath = null, tariffCertPass = null;
            int slacListenPort = 0, slacPeerPort = 0;

            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--connect":
                        (connectHost, connectPort) = SplitHostPort(args[++i], "--connect");
                        break;
                    case "--listen":
                        listenPort = ParsePort(args[++i], "--listen");
                        break;
                    case "--protocol":
                        // "both": one SupportedAppProtocol offer carrying -20 at priority 1 and -2 at
                        // priority 2; the session runs whichever the peer settles on. Protocol keeps the
                        // top preference for everything decided before the handshake (banner, TLS).
                        (protocol, offerBoth) = args[++i] switch
                        {
                            "2"    => (ProtocolVariant.Iso15118_2,  false),
                            "20"   => (ProtocolVariant.Iso15118_20, false),
                            "both" => (ProtocolVariant.Iso15118_20, true),
                            var v  => throw new ArgumentException($"--protocol expects 2, 20 or both, got '{v}'."),
                        };
                        break;
                    case "--mode":
                        // "mcs" is not a third power mode: MCS rides the DC message set under
                        // energy-transfer services 8 / 9 with a megawatt envelope, so it sets Dc and
                        // raises a flag beside it. Only -20 has those services; Validate() says so.
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
                            "dotnet" => TlsBackend.Dotnet,
                            "bc" or "bouncycastle" => TlsBackend.BouncyCastle,
                            var v => throw new ArgumentException($"--tls-backend expects dotnet or bc, got '{v}'."),
                        };
                        break;
                    case "--sdp":
                        useSdp = true;
                        break;
                    case "--dynamic":
                        preferDynamic = true;
                        break;
                    // Offer EIM only. Legal either way; some EVs cannot cope with a list containing a
                    // service they do not support — see Secc20Base.OfferPlugAndCharge.
                    case "--no-pnc":
                        noPnc = true;
                        break;
                    case "--interface":
                        iface = args[++i];
                        break;
                    case "--slac":
                        useSlac = true;
                        break;
                    case "--slac-listen":
                        slacListenPort = ParsePort(args[++i], "--slac-listen");
                        break;
                    case "--slac-peer":
                        (slacPeerHost, slacPeerPort) = SplitHostPort(args[++i], "--slac-peer");
                        break;
                    case "--pki-dir":
                        pkiDir = args[++i];
                        break;
                    case "--client-cert":
                        clientCertPath = args[++i];
                        break;
                    case "--client-cert-pass":
                        clientCertPass = args[++i];
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
                    case "--contract-cert":
                        contractCertPath = args[++i];
                        break;
                    case "--contract-cert-pass":
                        contractCertPass = args[++i];
                        break;
                    case "--pause-resume":
                        pauseResume = true;
                        break;
                    case "--pause":
                        endPaused = true;
                        break;
                    case "--resume":
                        resumeSessionIdHex = args[++i];
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
                    default:
                        throw new ArgumentException($"unknown argument '{args[i]}'.\n{Usage}");
                }
            }

            // --tls is shorthand for the .NET backend; --tls-backend wins if both are given.
            if (backend == TlsBackend.None && tls)
                backend = TlsBackend.Dotnet;

            Validate(role, connectHost, listenPort, backend, useSdp, iface, useSlac, slacListenPort, slacPeerHost, pkiDir);

            // Energy-transfer services 8 / 9 exist in no other catalogue, so --mode mcs against -2 is a
            // request that cannot be met. Refused here rather than quietly running plain DC, because a
            // session that silently degrades is the one failure an MCS run must not produce.
            if (mcs && protocol != ProtocolVariant.Iso15118_20)
                throw new ArgumentException("--mode mcs is an ISO 15118-20 session; add --protocol 20.");

            return new CliArgs(role, connectHost, connectPort, listenPort, protocol, mode, backend,
                               useSdp, iface, preferDynamic, noPnc, useSlac, slacListenPort, slacPeerHost, slacPeerPort, pkiDir,
                               clientCertPath, clientCertPass, serverCertPath, serverCertPass, requireClientCert,
                               contractCertPath, contractCertPass, pauseResume, endPaused, resumeSessionIdHex, renegotiate,
                               tariffCertPath, tariffCertPass, offerBoth, mcs);
        }

        private static void Validate(Role role, string? connectHost, int listenPort, TlsBackend backend,
                                     bool useSdp, string? iface, bool useSlac, int slacListenPort,
                                     string? slacPeerHost, string? pkiDir)
        {
            if (role == Role.Evcc && connectHost is null && !useSdp)
                throw new ArgumentException($"evcc requires --connect host:port (or --sdp --interface <name>).\n{Usage}");
            if (role == Role.Secc && listenPort == 0)
                throw new ArgumentException($"secc requires --listen port.\n{Usage}");

            if (backend == TlsBackend.BouncyCastle && pkiDir is null)
                throw new ArgumentException("--tls-backend bc requires --pki-dir <dir> (shared V2G certificate material).");

            if (useSdp && iface is null)
                throw new ArgumentException("--sdp requires --interface <name> (the V2G network interface).");

            if (useSlac && role == Role.Evcc && slacPeerHost is null)
                throw new ArgumentException("evcc --slac requires --slac-peer <host:port> (the EVSE's SLAC endpoint).");
            if (useSlac && role == Role.Secc && slacListenPort == 0)
                throw new ArgumentException("secc --slac requires --slac-listen <port>.");
        }

        /// <summary>
        /// An IPv6 literal must be bracketed — <c>[fe80::1%eth0]:9000</c>, the form an SDP-less connect
        /// to a station uses, and the form every recorded interop run wrote.
        /// </summary>
        /// <remarks>
        /// This used to split an unbracketed value at its last colon and hand the zone on "to the socket
        /// layer". Both halves of that were wrong, and <see cref="V2GEndpoint"/> now owns the details:
        /// <c>::1:8080</c> is an address in its own right (<c>::0.1.128.128</c>), so splitting it at the
        /// last colon silently connects somewhere else; and a zone whose interface name this machine does
        /// not have is discarded by the platform's parsers without a word.
        /// </remarks>
        private static (string Host, int Port) SplitHostPort(string value, string flag)
        {
            var endpoint = V2GEndpoint.Parse(value, flag);

            // ConnectHost, not Host: for a literal it carries the zone as a *number*, which is the one
            // form nothing downstream can lose.
            return (endpoint.ConnectHost, endpoint.Port);
        }

        private static int ParsePort(string value, string flag)
            => int.TryParse(value, out var port) ? port : throw new ArgumentException($"{flag} expects a port number, got '{value}'.");

        public const string Usage =
            "usage: evcc --connect <host:port> --protocol 2|20|both --mode ac|dc|mcs [tls/stage options]\n" +
            "       secc --listen  <port>      --protocol 2|20|both --mode ac|dc|mcs [tls/stage options]\n" +
            "         (both: offer/accept -20 at priority 1 and -2 at priority 2 in one handshake,\n" +
            "          then run whichever the peer settles on)\n" +
            "         (mcs: the DC message set under energy-transfer services 8/9 with a megawatt\n" +
            "          envelope — implies --mode dc and requires --protocol 20)\n" +
            "  TLS:   --tls | --tls-backend dotnet|bc   (bc = -20-faithful mutual TLS, needs --pki-dir <dir>)\n" +
            "         evcc --client-cert <pfx> [--client-cert-pass <pw>]  (mutual TLS on the .NET backend)\n" +
            "         secc --server-cert <pfx> [--server-cert-pass <pw>] [--require-client-cert]  (.NET backend)\n" +
            "  SDP:   --sdp --interface <name>          (discover/advertise instead of a fixed endpoint)\n" +
            "  Auth:  secc --no-pnc                     (-20: advertise EIM only, not EIM + Plug & Charge)\n" +
            "  Mode:  secc --dynamic                    (-20: offer the Dynamic control-mode parameter set first)\n" +
            "  PnC:   evcc --contract-cert <pfx> [--contract-cert-pass <pw>]  (-2/-20: signed Plug & Charge auth)\n" +
            "  Pause: evcc --pause-resume    (pause after the charge loop, reconnect, rejoin the old session)\n" +
            "         evcc --pause | --resume <hex-session-id>   (the two halves as separate invocations)\n" +
            "  Tariff: secc --tariff-cert <pfx> [--tariff-cert-pass <pw>]  (signed SalesTariff/-20 AbsolutePriceSchedule offer)\n" +
            "          evcc --tariff-cert <pfx>   (verify the tariff signature with the cert's public key)\n" +
            "  Reneg: secc --renegotiate  (notify ReNegotiation/ServiceRenegotiation once mid-loop)\n" +
            "         evcc --renegotiate  (-2: EV-initiated PowerDelivery(Renegotiate) after the first cycle)\n" +
            "  SLAC:  --slac  (secc: --slac-listen <port>; evcc: --slac-peer <host:port>)";
    }
}
