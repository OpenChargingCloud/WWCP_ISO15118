/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

#region Usings

using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

using cloud.charging.open.protocols.ISO15118.T1S.Transport.Linux;

#endregion

namespace cloud.charging.open.protocols.ISO15118.T1S.Transport
{

    /// <summary>
    /// What came of asking for a medium: the medium, or the one sentence
    /// that says why there is none.
    /// </summary>
    /// <remarks>
    /// Three outcomes and not two, because "no bus" is not always a failure.
    /// A vehicle configured with <see cref="T1STransportKind.None"/> has no
    /// bus and nothing is wrong; one configured with
    /// <see cref="T1STransportKind.Auto"/> on a laptop has no bus and that is
    /// worth a sentence in the log; one configured with
    /// <see cref="T1STransportKind.AfPacket"/> on Windows has no bus and that
    /// is a mistake somebody should be told about. The first two have a
    /// <see cref="Reason"/>; only the third has an <see cref="Error"/>.
    /// </remarks>
    /// <param name="Kind">What was decided on - the kind asked for, or what Auto resolved to.</param>
    /// <param name="Transport">The medium, attached but not yet started; null where there is none.</param>
    /// <param name="Reason">What this is, or why it is nothing, for a log.</param>
    /// <param name="Error">Why there is no medium although one was asked for; null otherwise.</param>
    public sealed record T1SMedium(T1STransportKind  Kind,
                                   IT1STransport?    Transport,
                                   String            Reason,
                                   String?           Error  = null)
    {

        /// <summary>There is a medium.</summary>
        public Boolean  IsOpen
            => Transport is not null;

        /// <summary>A medium was asked for and could not be had.</summary>
        public Boolean  IsFailed
            => Error is not null;

    }


    /// <summary>
    /// The one place a configuration is turned into a medium.
    /// </summary>
    /// <remarks>
    /// The vehicle, the station and a bench all make the same decision from
    /// the same four fields, so it is made here once - including the two
    /// rules worth stating: the emulated medium is never chosen by itself,
    /// and a real adapter is Linux and CAP_NET_RAW or nothing.
    /// </remarks>
    public static class T1STransports
    {

        #region Open(Options, Clock = null)

        /// <summary>
        /// The medium these options ask for, not yet started; or why there is
        /// none.
        /// </summary>
        /// <param name="Options">What was asked for.</param>
        /// <param name="Clock">Where received frames get their timestamps from.</param>
        public static T1SMedium Open(T1STransportOptions  Options,
                                     TimeProvider?        Clock   = null)
        {

            switch (Options.Kind)
            {

                case T1STransportKind.None:
                    return new T1SMedium(T1STransportKind.None, null, "no bus: none was asked for.");

                case T1STransportKind.Auto:

                    // Only the real medium is ever chosen by itself. The
                    // emulated one has to be asked for, because a node that
                    // joined a multicast group on a machine without an adapter
                    // would look attached and be talking to nothing a coupler
                    // has.
                    if (!OperatingSystem.IsLinux())
                        return new T1SMedium(T1STransportKind.None, null,
                                             "no bus: a real 10BASE-T1S adapter needs AF_PACKET, which is Linux only. " +
                                             "Ask for the emulated medium (\"udp\") explicitly to run without one.");

                    if (String.IsNullOrWhiteSpace(Options.InterfaceName))
                        return new T1SMedium(T1STransportKind.None, null,
                                             "no bus: name the interface the adapter is, and AF_PACKET is used on it.");

                    return OpenAfPacket(Options, Clock);

                case T1STransportKind.AfPacket:
                    return OpenAfPacket(Options, Clock);

                case T1STransportKind.UDP:
                    return OpenUDP(Options, Clock);

                default:
                    return Failed(Options.Kind, $"'{Options.Kind}' is not a transport kind this library knows.");

            }

        }

        #endregion

        #region IPv4AddressOf(InterfaceName, out Address, out Error)

        /// <summary>
        /// The first IPv4 address of the named interface, which is what the
        /// emulated medium joins its group on.
        /// </summary>
        public static Boolean TryFindIPv4Address(String                                     InterfaceName,
                                                 [System.Diagnostics.CodeAnalysis.NotNullWhen(true)]  out IPAddress?  Address,
                                                 [System.Diagnostics.CodeAnalysis.NotNullWhen(false)] out String?     Error)
        {

            Address  = null;
            Error    = null;

            NetworkInterface? found;

            try
            {
                found = NetworkInterface.GetAllNetworkInterfaces()
                                        .FirstOrDefault(nic => String.Equals(nic.Name, InterfaceName, StringComparison.OrdinalIgnoreCase));
            }
            catch (NetworkInformationException e)
            {
                Error = $"The network interfaces of this machine could not be listed: {e.Message}";
                return false;
            }

            if (found is null)
            {
                Error = $"There is no network interface '{InterfaceName}' on this machine.";
                return false;
            }

            Address = found.GetIPProperties()
                           .UnicastAddresses
                           .Select(unicast => unicast.Address)
                           .FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork);

            if (Address is null)
            {
                Error = $"'{InterfaceName}' has no IPv4 address, and the emulated medium is IPv4 multicast.";
                return false;
            }

            return true;

        }

        #endregion


        #region (private) OpenAfPacket(Options, Clock)

        private static T1SMedium OpenAfPacket(T1STransportOptions  Options,
                                              TimeProvider?        Clock)
        {

            if (!OperatingSystem.IsLinux())
                return Failed(T1STransportKind.AfPacket,
                              "AF_PACKET is Linux only; on this operating system there is no raw access to an adapter. " +
                              "Use \"udp\" for the emulated medium.");

            if (String.IsNullOrWhiteSpace(Options.InterfaceName))
                return Failed(T1STransportKind.AfPacket,
                              "AF_PACKET needs the name of the interface the adapter is - 'eth1', say.");

            try
            {

                var transport = new AfPacketT1STransport(
                                    Options.InterfaceName.Trim(),
                                    Options.LocalMac,
                                    Clock
                                );

                return new T1SMedium(T1STransportKind.AfPacket, transport, $"{transport.Description} as {transport.LocalMac}");

            }
            catch (Exception e) when (e is IOException or PlatformNotSupportedException or UnauthorizedAccessException)
            {
                return Failed(T1STransportKind.AfPacket, e.Message);
            }

        }

        #endregion

        #region (private) OpenUDP(Options, Clock)

        private static T1SMedium OpenUDP(T1STransportOptions  Options,
                                         TimeProvider?        Clock)
        {

            IPAddress? via = null;

            if (!String.IsNullOrWhiteSpace(Options.InterfaceName))
            {

                if (!TryFindIPv4Address(Options.InterfaceName.Trim(), out via, out var error))
                    return Failed(T1STransportKind.UDP, error);

            }

            try
            {

                var transport = new UdpMulticastT1STransport(
                                    Options.LocalMac ?? T1SConstants.RandomLocalMac(),
                                    Options.Group,
                                    via,
                                    Clock
                                );

                return new T1SMedium(T1STransportKind.UDP, transport, $"{transport.Description} as {transport.LocalMac}");

            }
            catch (Exception e) when (e is SocketException or ArgumentException)
            {
                return Failed(T1STransportKind.UDP, $"The emulated medium could not be opened: {e.Message}");
            }

        }

        #endregion

        #region (private) Failed(Kind, Error)

        private static T1SMedium Failed(T1STransportKind  Kind,
                                        String            Error)

            => new (Kind, null, $"no bus: {Error}", Error);

        #endregion

    }

}
