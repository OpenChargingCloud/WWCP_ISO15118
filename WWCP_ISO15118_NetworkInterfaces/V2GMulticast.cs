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

#endregion

namespace cloud.charging.open.protocols.ISO15118.NetworkInterfaces
{

    /// <summary>
    /// IPv6 multicast addresses and ports used by the V2G stack.
    /// </summary>
    public static class V2GMulticast
    {

        /// <summary>
        /// Per ISO 15118-2 §8.4.2 / ISO 15118-20 §8.3.6, the EVCC sends the
        /// SDP_Request to the All_Nodes link-local IPv6 multicast address
        /// (FF02::1), UDP port 15118. The standard does *not* allocate a
        /// dedicated multicast group for SDP – it really is the all-nodes group.
        /// </summary>
        public static readonly IPAddress SDPMulticastGroup
            = IPAddress.Parse("FF02::1");

        /// <summary>
        /// UDP/TCP port reserved for V2G – ISO 15118 / IANA.
        /// </summary>
        public const UInt16 V2GUdpPort = 15118;

        /// <summary>
        /// Endpoint a SECC must bind/listen on to catch SDP_Request multicasts.
        /// The ANY address is used here intentionally – the multicast join
        /// happens explicitly per interface via <c>AddMembership</c>.
        /// </summary>
        public static IPEndPoint SDPListenEndpoint

            => new (
                   IPAddress.IPv6Any,
                   V2GUdpPort
               );

        /// <summary>
        /// Endpoint an EVCC must send the SDP_Request to. The scope-id of the
        /// outgoing interface is set on the socket via
        /// <c>IPv6MulticastOption(addr, ifindex)</c>, not in the address itself.
        /// </summary>
        public static IPEndPoint SDPSendEndpoint

            => new (
                   SDPMulticastGroup,
                   V2GUdpPort
               );

    }

}
