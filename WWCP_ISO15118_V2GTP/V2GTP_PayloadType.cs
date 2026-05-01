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
    /// V2GTP payload types, big-endian uint16 in octets 2-3 of the header.
    ///
    /// ISO 15118-2:
    ///   0x8001  EXI Encoded V2G Message (SAP / V2G_CI_MsgDef)
    ///   0x9000  SDP Request
    ///   0x9001  SDP Response
    ///
    /// ISO 15118-20 (Edition 1, 2022) extends this:
    ///   0x8001  EXI: Mainstream V2G Message (V2G_CI_CommonMessages, AC, DC, ...)
    ///   0x8002  EXI: AC stream
    ///   0x8003  EXI: DC stream
    ///   0x8004  EXI: ACDP (Automatic Connection Device, Pantograph) stream
    ///   0x8005  EXI: WPT (Wireless Power Transfer) stream
    ///   0x9000  SDP Request                   (compatible with -2)
    ///   0x9001  SDP Response                  (compatible with -2)
    ///   0x9002  SDP Request Wireless          (extended discovery)
    ///   0x9003  SDP Response Wireless         (extended discovery)
    ///
    /// Payload types in 0x0000-0x7FFF are reserved, 0xA000-0xFFFF are
    /// manufacturer specific (used here for pentest test cases).
    /// </summary>
    public enum V2GTP_PayloadType : UInt16
    {

        // Reserved range starts at 0x0000

        /// <summary>EXI-encoded V2G message stream (mainstream / -2 / -20 common).</summary>
        ExiMainstream             = 0x8001,

        /// <summary>EXI: AC stream (15118-20).</summary>
        ExiAC                     = 0x8002,

        /// <summary>EXI: DC stream (15118-20).</summary>
        ExiDC                     = 0x8003,

        /// <summary>EXI: ACD Pantograph (ACDP) stream (15118-20).</summary>
        ExiACDP                   = 0x8004,

        /// <summary>EXI: Wireless Power Transfer (WPT) stream (15118-20).</summary>
        ExiWPT                    = 0x8005,

        /// <summary>SDP_Request – EVCC asks "where is the SECC?".</summary>
        SdpRequest                = 0x9000,

        /// <summary>SDP_Response – SECC announces its TCP/TLS endpoint.</summary>
        SdpResponse               = 0x9001,

        /// <summary>SDP_RequestWireless – EVCC asks "is there a SECC on this link?" for wireless charging / ACDP.</summary>
        SdpRequestWireless        = 0x9002,

        /// <summary>SDP_ResponseWireless – SECC announces its TCP/TLS endpoint + diagnostic info for wireless pairing.</summary>
        SdpResponseWireless       = 0x9003

    }

    public static class V2GTPPayloadTypeExtensions
    {
        public static Boolean IsKnown(this V2GTP_PayloadType V2GTPPayloadType)

            => V2GTPPayloadType switch {
                      V2GTP_PayloadType.ExiMainstream        => true,
                      V2GTP_PayloadType.ExiAC                => true,
                      V2GTP_PayloadType.ExiDC                => true,
                      V2GTP_PayloadType.ExiACDP              => true,
                      V2GTP_PayloadType.ExiWPT               => true,
                      V2GTP_PayloadType.SdpRequest           => true,
                      V2GTP_PayloadType.SdpResponse          => true,
                      V2GTP_PayloadType.SdpRequestWireless   => true,
                      V2GTP_PayloadType.SdpResponseWireless  => true,
                      _                                      => false,
                  };

        public static Boolean IsSDP(this V2GTP_PayloadType V2GTPPayloadType)

            => V2GTPPayloadType is V2GTP_PayloadType.SdpRequest         or
                                   V2GTP_PayloadType.SdpResponse        or
                                   V2GTP_PayloadType.SdpRequestWireless or
                                   V2GTP_PayloadType.SdpResponseWireless;

    }

}
