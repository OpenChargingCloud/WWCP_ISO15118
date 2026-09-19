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

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

#endregion

namespace cloud.charging.open.protocols.ISO15118.T1S.Transport
{

    /// <summary>
    /// What a node needs to know to attach to a medium: which kind, on which
    /// interface, and - for the emulated one - which group.
    /// </summary>
    /// <remarks>
    /// One record for both media rather than one per medium, because that is
    /// what a configuration file holds: four fields a vehicle or a station is
    /// given, and one decision made from them at a start. Which of the four
    /// matter depends on the kind, and <see cref="T1STransports.Open"/> says
    /// so when one that matters is missing.
    /// </remarks>
    /// <param name="Kind">Which medium. Auto takes the real adapter where there is one and nothing anywhere else.</param>
    /// <param name="InterfaceName">The adapter, for AF_PACKET; the interface to join the group on, for UDP, or null for the one the operating system picks.</param>
    /// <param name="Group">The multicast group and port that are the emulated medium; the library's default when null. Ignored by AF_PACKET.</param>
    /// <param name="LocalMac">This node's address on the medium. Random and locally administered when null on UDP; the adapter's own on AF_PACKET, where a value here overrides it - for a test, not for a vehicle.</param>
    public sealed record T1STransportOptions(T1STransportKind  Kind           = T1STransportKind.Auto,
                                             String?           InterfaceName  = null,
                                             IPEndPoint?       Group          = null,
                                             MACAddress?       LocalMac       = null);

}
