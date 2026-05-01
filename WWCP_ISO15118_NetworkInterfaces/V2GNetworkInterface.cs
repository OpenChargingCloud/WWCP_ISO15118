/*
 * Copyright (c) 2014-2025 GraphDefined GmbH <achim.friedland@graphdefined.com>
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
using System.Net.NetworkInformation;

#endregion

namespace cloud.charging.open.protocols.ISO15118.NetworkInterfaces
{

    /// <summary>
    /// A network interface usable for V2G traffic (typically the PLC interface
    /// established after SLAC matching). Wraps the OS-level NIC info with the
    /// pieces V2GTP / SDP / TCP-V2G actually need: a stable index for the IPv6
    /// scope-id, the link-local address to send/respond from, and the MAC.
    /// </summary>
    public sealed record V2GNetworkInterface(Int32      Index,
                                             String     Name,
                                             IPAddress  LinkLocalIPAddress,
                                             Byte[]     MACAddress)
    {

        /// <summary>
        /// IPv6 socket address of <see cref="LinkLocalIPAddress"/> with the correct
        /// scope-id baked in. Required for any send/bind on link-local IPv6 –
        /// without scope-id the kernel cannot pick the right interface when more
        /// than one link-local route exists.
        /// </summary>
        public IPEndPoint LinkLocalEndpoint(UInt16 IPPort)

            => new (
                   new IPAddress(
                       LinkLocalIPAddress.GetAddressBytes(),
                       Index
                   ),
                   IPPort
               );


        /// <summary>
        /// Returns true if this interface looks like a candidate for V2G traffic:
        /// up, has at least one IPv6 link-local address, and has a MAC.
        /// </summary>
        public static Boolean LooksLikeV2GCandidate(NetworkInterface NetworkInterface)
        {

            if (NetworkInterface.OperationalStatus    != OperationalStatus.Up)
                return false;

            if (NetworkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                return false;

            if (NetworkInterface.GetPhysicalAddress().GetAddressBytes().Length != 6)
                return false;

            return NetworkInterface.
                       GetIPProperties().
                       UnicastAddresses.Any(unicastIPAddressInformation => unicastIPAddressInformation.Address.IsIPv6LinkLocal);

        }

        public override String ToString()
            => $"{Name} (idx={Index}) {LinkLocalIPAddress}%{Index} mac={Convert.ToHexString(MACAddress)}";

    }

}
