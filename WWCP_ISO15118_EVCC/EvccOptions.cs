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
using cloud.charging.open.protocols.ISO15118.Transport;

namespace cloud.charging.open.protocols.ISO15118.EVCC
{
    /// <summary>
    /// The car's command line. Hand-rolled parsing (no arg-parsing package anywhere in this repo).
    /// </summary>
    /// <remarks>
    /// There is no role subcommand: this program <i>is</i> the car, so every flag here is a car's flag.
    /// The station's flags — <c>--listen</c>, <c>--no-pnc</c>, <c>--dynamic</c>, <c>--server-cert</c> —
    /// are not accepted and not documented here, which is the point of the split: <c>--help</c> now
    /// shows what this side can do rather than the union of two roles.
    /// </remarks>
    public sealed record EvccOptions(
        string? ConnectHost, int ConnectPort,
        ProtocolVariant Protocol, bool OfferBoth, PowerMode Mode, bool Mcs,
        TlsStack TlsStack, bool UseSdp, string? Interface,
        bool UseSlac, string? SlacPeerHost, int SlacPeerPort, string? PkiDir,
        string? VehicleCertPath, string? VehicleCertPass, string? TrustRootsPath,
        string? ContractCertPath, string? ContractCertPass,
        string? OemCertPath, string? OemCertPass,
        bool PauseResume, bool EndPaused, string? ResumeSessionIdHex,
        bool Renegotiate, string? TariffCertPath, string? TariffCertPass,
        double? BatteryKWh, double? StartSoC, double? TargetSoC, double? TargetEnergyKWh,
        TimeSpan? MaxChargingTime, double? PowerKW,
        TimeSpan? DepartureIn, double? MinimumSoC)
    {

        /// <summary>
        /// Whether this run simulates a battery filling up rather than a fixed three-iteration loop.
        /// Any of the battery flags turns it on; none of them keeps every recorded interop run's shape.
        /// </summary>
        public bool HasBattery => BatteryKWh is not null || StartSoC is not null || TargetSoC is not null
                               || TargetEnergyKWh is not null || MaxChargingTime is not null || PowerKW is not null
                               || DepartureIn is not null || MinimumSoC is not null;

        public static EvccOptions Parse(string[] args)
        {
            // A real car offers what it speaks and lets the station choose. Offering both with -20 at
            // priority 1 is what modern vehicles do, so it is the default here: against a -20 station the
            // session runs -20, against a -2-only station it falls back without a second attempt. Pin one
            // with --protocol 2 or --protocol 20 when the point of the run is that protocol.
            int connectPort = 0, slacPeerPort = 0;
            var protocol = ProtocolVariant.Iso15118_20;
            var offerBoth = true;
            var mode = PowerMode.Dc;
            var mcs = false;
            var backend = TlsStack.None;
            bool tls = false, useSdp = false, useSlac = false;
            bool pauseResume = false, endPaused = false, renegotiate = false;
            string? connectHost = null, iface = null, slacPeerHost = null, pkiDir = null;
            string? vehicleCertPath = null, vehicleCertPass = null, trustRootsPath = null;
            string? contractCertPath = null, contractCertPass = null;
            string? oemCertPath = null, oemCertPass = null;
            string? resumeSessionIdHex = null, tariffCertPath = null, tariffCertPass = null;
            double? batteryKWh = null, startSoC = null, targetSoC = null, targetEnergyKWh = null, powerKW = null;
            TimeSpan? maxChargingTime = null, departureIn = null;
            double? minimumSoC = null;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--connect":
                        (connectHost, connectPort) = SplitHostPort(args[++i], "--connect");
                        break;
                    // "both": one SupportedAppProtocol offer carrying -20 at priority 1 and -2 at
                    // priority 2; the session runs whichever the station picks. Protocol keeps the top
                    // preference for everything decided before the handshake — the banner, and TLS.
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
                    case "--slac":
                        useSlac = true;
                        break;
                    case "--slac-peer":
                        (slacPeerHost, slacPeerPort) = SplitHostPort(args[++i], "--slac-peer");
                        break;
                    case "--pki-dir":
                        pkiDir = args[++i];
                        break;
                    // The car's own identity in the V2G PKI — the "Vehicle" certificate, which for
                    // -20 is what a station's mutual-TLS handshake asks for and what its resume binding is
                    // computed over.
                    case "--vehicle-cert":
                        vehicleCertPath = args[++i];
                        break;
                    case "--vehicle-cert-pass":
                        vehicleCertPass = args[++i];
                        break;
                    // The V2G root(s) the station's certificate must chain to. One file, or a directory of
                    // them when several counterparties' hierarchies have to be trusted at once.
                    case "--trust-roots":
                        trustRootsPath = args[++i];
                        break;
                    // The OEM provisioning chain, which is a different certificate from both of the above:
                    // it is what the car was born with, and the only thing it can prove before it holds a
                    // contract. -20 uses it for CertificateInstallation; see Validate() for -2.
                    case "--oem-cert":
                        oemCertPath = args[++i];
                        break;
                    case "--oem-cert-pass":
                        oemCertPass = args[++i];
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
                    // The battery. Any one of these turns the charge loop from three iterations into a
                    // session that ends when the car is done; see EvBattery for why that is opt-in.
                    case "--battery":
                        batteryKWh = ParsePositive(args[++i], "--battery", "kWh");
                        break;
                    case "--soc":
                        startSoC = ParsePercent(args[++i], "--soc");
                        break;
                    case "--target-soc":
                        targetSoC = ParsePercent(args[++i], "--target-soc");
                        break;
                    case "--target-energy":
                        targetEnergyKWh = ParsePositive(args[++i], "--target-energy", "kWh");
                        break;
                    case "--max-charging-time":
                        maxChargingTime = ParseDuration(args[++i], "--max-charging-time");
                        break;
                    // When the car leaves. Relative, because that is what the wire carries: -20's
                    // DepartureTime is seconds from the session's own time anchor.
                    case "--departure-time":
                        departureIn = ParseDuration(args[++i], "--departure-time");
                        break;
                    // What the driver needs to have by then. A floor, not a goal — see EvBattery.
                    case "--min-soc":
                        minimumSoC = ParsePercent(args[++i], "--min-soc");
                        break;
                    case "--power":
                        powerKW = ParsePositive(args[++i], "--power", "kW");
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

            Validate(connectHost, backend, useSdp, iface, useSlac, slacPeerHost, pkiDir, mcs, protocol,
                     offerBoth, oemCertPath, vehicleCertPath, trustRootsPath);

            return new EvccOptions(connectHost, connectPort, protocol, offerBoth, mode, mcs, backend,
                                   useSdp, iface, useSlac, slacPeerHost, slacPeerPort, pkiDir,
                                   vehicleCertPath, vehicleCertPass, trustRootsPath,
                                   contractCertPath, contractCertPass,
                                   oemCertPath, oemCertPass,
                                   pauseResume, endPaused, resumeSessionIdHex,
                                   renegotiate, tariffCertPath, tariffCertPass,
                                   batteryKWh, startSoC, targetSoC, targetEnergyKWh, maxChargingTime, powerKW,
                                   departureIn, minimumSoC);
        }

        private static double ParsePositive(string value, string flag, string unit)
            => double.TryParse(value, System.Globalization.NumberStyles.Float,
                               System.Globalization.CultureInfo.InvariantCulture, out var d) && d > 0
                   ? d
                   : throw new ArgumentException($"{flag} expects a positive number of {unit}, got '{value}'.");

        private static double ParsePercent(string value, string flag)
            => double.TryParse(value.TrimEnd('%'), System.Globalization.NumberStyles.Float,
                               System.Globalization.CultureInfo.InvariantCulture, out var d) && d is >= 0 and <= 100
                   ? d
                   : throw new ArgumentException($"{flag} expects a state of charge between 0 and 100 %, got '{value}'.");

        /// <summary>
        /// <c>90</c>, <c>90m</c>, <c>2h</c>, <c>45s</c>, or <c>1h30m</c>. A bare number is minutes, because
        /// that is the unit a charging session is discussed in and the one a charge-loop iteration stands for.
        /// </summary>
        private static TimeSpan ParseDuration(string value, string flag)
        {
            var text  = value.Trim().ToLowerInvariant();
            var total = TimeSpan.Zero;
            var seen  = false;

            for (int i = 0; i < text.Length;)
            {
                int start = i;
                while (i < text.Length && (char.IsDigit(text[i]) || text[i] == '.')) i++;
                if (i == start) throw new ArgumentException($"{flag}: cannot read a duration from '{value}' (try 90m, 2h, 1h30m).");

                if (!double.TryParse(text[start..i], System.Globalization.NumberStyles.Float,
                                     System.Globalization.CultureInfo.InvariantCulture, out var n))
                    throw new ArgumentException($"{flag}: cannot read a duration from '{value}' (try 90m, 2h, 1h30m).");

                var unit = i < text.Length ? text[i++] : 'm';
                total += unit switch
                {
                    's' => TimeSpan.FromSeconds(n),
                    'm' => TimeSpan.FromMinutes(n),
                    'h' => TimeSpan.FromHours(n),
                    _   => throw new ArgumentException($"{flag}: unknown unit '{unit}' in '{value}' — use s, m or h."),
                };
                seen = true;
            }

            return seen && total > TimeSpan.Zero
                       ? total
                       : throw new ArgumentException($"{flag} expects a duration greater than zero, got '{value}'.");
        }

        private static void Validate(string? connectHost, TlsStack backend, bool useSdp, string? iface,
                                     bool useSlac, string? slacPeerHost, string? pkiDir,
                                     bool mcs, ProtocolVariant protocol, bool offerBoth,
                                     string? oemCertPath, string? vehicleCertPath, string? trustRootsPath)
        {
            if (connectHost is null && !useSdp)
                throw new ArgumentException($"a car needs somewhere to drive to: --connect host:port (or --sdp --interface <name>).\n{Usage}");

            // --pki-dir is how the dev loopback shares material: the station mints a hierarchy and the car
            // reads its Vehicle chain back out. --vehicle-cert is the other way in, for a run against a
            // foreign station whose PKI we do not mint — one of the two has to supply the car's identity.
            if (backend == TlsStack.BouncyCastle && pkiDir is null && vehicleCertPath is null)
                throw new ArgumentException(
                    "--tls-backend bc needs the car's own credentials: either --pki-dir <dir> (the dev " +
                    "hierarchy the station minted) or --vehicle-cert <pfx> (your own Vehicle chain).");

            if (useSdp && iface is null)
                throw new ArgumentException("--sdp requires --interface <name> (the V2G network interface).");

            if (useSlac && slacPeerHost is null)
                throw new ArgumentException("--slac requires --slac-peer <host:port> (the EVSE's SLAC endpoint).");

            // Existence only — the contents are read much later, some of them after the socket is open, and
            // a mistyped path should not first show up as a station that appears to have hung up on us.
            foreach (var (flag, path) in new[] { ("--vehicle-cert", vehicleCertPath), ("--oem-cert", oemCertPath) })
                if (path is not null && !File.Exists(path))
                    throw new ArgumentException($"{flag}: no such file '{path}'.");

            // Roots may be a file or a directory, so this one only asks that it is *something*.
            if (trustRootsPath is not null && !File.Exists(trustRootsPath) && !Directory.Exists(trustRootsPath))
                throw new ArgumentException($"--trust-roots: no such file or directory '{trustRootsPath}'.");

            // Energy-transfer services 8 / 9 exist in no other catalogue, so --mode mcs against -2 is a
            // request that cannot be met. Refused here rather than quietly running plain DC, because a
            // session that silently degrades is the one failure an MCS run must not produce. Offering
            // both is fine — the handshake can still settle on -20, and only the -2 half cannot carry it.
            if (mcs && protocol != ProtocolVariant.Iso15118_20 && !offerBoth)
                throw new ArgumentException("--mode mcs is an ISO 15118-20 session; drop --protocol 2.");
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

        public const string Usage =
            "The ISO 15118 vehicle (EVCC): drive up to a station and run a charging session.\n" +
            "\n" +
            "usage: WWCP_ISO15118_EVCC --connect <host:port> [options]\n" +
            "       WWCP_ISO15118_EVCC --sdp --interface <name> [options]\n" +
            "\n" +
            "  --connect <host:port>  the station. IPv6 literals must be bracketed:\n" +
            "                         [fe80::1%eth0]:15118\n" +
            "  --sdp --interface <n>  find the station on the link instead of naming it\n" +
            "  --protocol 2|20|both   what to offer (default: both, -20 at priority 1)\n" +
            "                         both = offer -20 and -2 in one handshake and run whichever the\n" +
            "                         station picks; against a -20 station that is -20\n" +
            "  --mode ac|dc|mcs       energy transfer mode (default: dc)\n" +
            "                         mcs = the DC message set under energy-transfer services 8/9 with\n" +
            "                         a megawatt envelope; -20 only\n" +
            "\n" +
            "  TLS:    --tls                       .NET SslStream (accepts any server certificate — dev only)\n" +
            "          --tls-backend dotnet|bc     bc = the -20-faithful profile (TLS 1.3, secp521r1,\n" +
            "                                      mutual). Required on Windows and macOS for a real -20\n" +
            "                                      session; see the README.\n" +
            "          --pki-dir <dir>             the dev hierarchy the station minted\n" +
            "          --trust-roots <file|dir>    validate the station's chain against these V2G root(s).\n" +
            "                                      Without it no station certificate is checked at all.\n" +
            "                                      A directory holds several, for a run that must accept\n" +
            "                                      more than one hierarchy. Put any intermediates the\n" +
            "                                      station does not send itself in here too — EVerest's\n" +
            "                                      sends only its leaf, so root-alone is refused. They can\n" +
            "                                      only ever act as intermediates, never as trust anchors.\n" +
            "\n" +
            "  The car carries up to three different certificates. They are not interchangeable:\n" +
            "          --vehicle-cert <pfx> [--vehicle-cert-pass <pw>]\n" +
            "                                      the *Vehicle* certificate — who this car is, in\n" +
            "                                      the V2G PKI. Presented in the TLS handshake; for -20 the\n" +
            "                                      station's resume binding is computed over it. Works on\n" +
            "                                      both backends.\n" +
            "          --contract-cert <pfx> [--contract-cert-pass <pw>]\n" +
            "                                      the *contract* certificate — who pays. -2/-20 Plug &\n" +
            "                                      Charge: signs the authorization instead of paying\n" +
            "                                      externally.\n" +
            "          --oem-cert <pfx> [--oem-cert-pass <pw>]\n" +
            "                                      the *OEM provisioning* certificate — what the car was\n" +
            "                                      born with, and all it can prove before it holds a\n" +
            "                                      contract. -20: requests CertificateInstallation and\n" +
            "                                      unwraps the issued contract key (needs a P-521 key).\n" +
            "                                      Accepted for -2, where nothing uses it yet.\n" +
            "  Battery: any one of these turns the charge loop from three iterations into a session that\n" +
            "           ends when the car is done. One iteration is one simulated minute, so a full charge\n" +
            "           is hundreds of exchanges — give --max-charging-time when driving a live station.\n" +
            "          --battery <kWh>             usable capacity (default: 60)\n" +
            "          --soc <percent>             state of charge at plug-in (default: random 10–60 %)\n" +
            "          --target-soc <percent>      charge until this state of charge\n" +
            "          --target-energy <kWh>       charge until this much has been delivered\n" +
            "                                      (EAmount in -2, EVTargetEnergyRequest in -20)\n" +
            "          --max-charging-time <dur>   stop after this much simulated time: 90, 90m, 2h, 1h30m\n" +
            "          --departure-time <dur>      when the car leaves. Goes on the wire as -20's\n" +
            "                                      DepartureTime, and ends the session when it arrives.\n" +
            "          --min-soc <percent>         what the driver needs by then. A floor, not a goal: it\n" +
            "                                      cannot prolong a session, and the run reports whether\n" +
            "                                      the car left with enough.\n" +
            "          --power <kW>                what the car asks for. -20 DC: its EVMaximum* limits.\n" +
            "                                      The station decides what it actually delivers, and the\n" +
            "                                      battery fills with what the meter counted.\n" +
            "           With no goal named at all, the goal is 100 %.\n" +
            "  Tariff: --tariff-cert <pfx> [--tariff-cert-pass <pw>]   verify the station's signed\n" +
            "                                      SalesTariff / AbsolutePriceSchedule with this public key\n" +
            "  Pause:  --pause-resume              pause after the charge loop, reconnect, rejoin the session\n" +
            "          --pause | --resume <hex>    the same two halves as separate runs\n" +
            "  Reneg:  --renegotiate               -2: PowerDelivery(Renegotiate) after the first cycle\n" +
            "  SLAC:   --slac --slac-peer <host:port>   run a SLAC pairing stage first\n" +
            "\n" +
            "The station is the other program: WWCP_ISO15118_SECC.";
    }
}
