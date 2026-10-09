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

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

using cloud.charging.open.protocols.ISO15118.SLAC.Selection;

namespace cloud.charging.open.protocols.ISO15118.Slac
{
    /// <summary>
    /// The outcome of a completed SLAC pairing: the PLC network credentials both sides agreed on
    /// (<c>NID</c>, 7 bytes; <c>NMK</c>, 16 bytes) that would program the local PLC chip to join the AVLN,
    /// and with whom. In this loopback simulation the subsequent TCP/TLS session does not consume the
    /// credentials — SLAC is the pairing stage that must simply complete before discovery.
    /// </summary>
    /// <remarks>
    /// Who the other side is was thrown away before: a vehicle that sounded several stations could not say
    /// which of them it had paired with, nor how loudly each had heard it - which is what somebody plugging
    /// in wants to see, and why SLAC is run at all.
    /// </remarks>
    /// <param name="Nid">The network identifier, 7 bytes.</param>
    /// <param name="Nmk">The network membership key, 16 bytes.</param>
    /// <param name="Peer">The MAC address of the other side: for a vehicle the station at the end of its cable, for a station the vehicle.</param>
    /// <param name="Station">A vehicle's side only: the station it chose - the one with the lowest average attenuation - with its attenuation profile. Null on a station's side.</param>
    /// <param name="Candidates">A vehicle's side only: every station that answered the sounding, the chosen one among them. Empty on a station's side.</param>
    public sealed record SlacResult(byte[]                        Nid,
                                    byte[]                        Nmk,
                                    MACAddress                    Peer,
                                    EVSECandidate?                Station,
                                    IReadOnlyList<EVSECandidate>  Candidates);
}
