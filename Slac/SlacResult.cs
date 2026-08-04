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

namespace Vanaheimr.V2G.Simulation.Slac
{
    /// <summary>
    /// The outcome of a completed SLAC pairing: the PLC network credentials both sides agreed on
    /// (<c>NID</c>, 7 bytes; <c>NMK</c>, 16 bytes) that would program the local PLC chip to join the AVLN.
    /// In this loopback simulation the subsequent TCP/TLS session does not consume them — SLAC is the
    /// pairing stage that must simply complete before discovery.
    /// </summary>
    public sealed record SlacResult(byte[] Nid, byte[] Nmk);
}
