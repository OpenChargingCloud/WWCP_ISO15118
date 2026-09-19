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

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

using cloud.charging.open.protocols.ISO15118.T1S.Messages;

#endregion

namespace cloud.charging.open.protocols.ISO15118.T1S
{

    /// <summary>
    /// A frame off the medium that was one of ours: who sent it, to whom, what
    /// it says, and when it arrived.
    /// </summary>
    /// <param name="Destination">Where it was sent - a node, or the broadcast address.</param>
    /// <param name="Source">Who sent it.</param>
    /// <param name="Message">What it says.</param>
    /// <param name="ReceivedAt">When this node saw it, by this node's clock.</param>
    public sealed record DecodedT1SFrame(MACAddress      Destination,
                                         MACAddress      Source,
                                         IT1SMessage     Message,
                                         DateTimeOffset  ReceivedAt);

}
