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

namespace cloud.charging.open.protocols.ISO15118.V2GTP
{

    /// <summary>
    /// V2GTP protocol version constants.
    ///
    /// ISO 15118-2 §7.8.2 and ISO 15118-20 §A.1 both define the V2GTP header as:
    ///
    ///   octet 0: ProtocolVersion         (0x01)
    ///   octet 1: InverseProtocolVersion  (0xFE = ~0x01)
    ///   octets 2-3: PayloadType          (uint16, big-endian)
    ///   octets 4-7: PayloadLength        (uint32, big-endian)
    ///
    /// Both protocol revisions use ProtocolVersion = 0x01. They are *not*
    /// distinguished on the V2GTP layer – the EVCC and SECC must agree on
    /// which 15118 revision they speak via SAP (Supported App Protocol)
    /// negotiation, which itself runs over V2GTP.
    /// </summary>
    public static class V2GTP_ProtocolVersion
    {
        public const Byte  Current  = 0x01;
        public const Byte  Inverse  = 0xFE;
    }

}
