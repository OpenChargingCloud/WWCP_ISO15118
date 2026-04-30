/*
 * Copyright (c) 2021-2025 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP ISO/IEC 15118 <https://github.com/OpenChargingCloud/WWCP_ISO15118>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace cloud.charging.open.protocols.ISO15118.SLAC
{

    /// <summary>
    /// HomePlug AV Management Message Type codes. The lower two bits encode
    /// the message subtype: 00=REQ, 01=CNF, 10=IND, 11=RSP.
    /// </summary>
    public enum ManagementMessageType : UInt16
    {

        CM_SLAC_PARM_REQ         = 0x6064,
        CM_SLAC_PARM_CNF         = 0x6065,

        CM_START_ATTEN_CHAR_IND  = 0x606A,

        CM_ATTEN_CHAR_IND        = 0x606E,
        CM_ATTEN_CHAR_RSP        = 0x606F,

        CM_MNBC_SOUND_IND        = 0x6076,

        CM_VALIDATE_REQ          = 0x6078,
        CM_VALIDATE_CNF          = 0x6079,

        CM_SLAC_MATCH_REQ        = 0x607C,
        CM_SLAC_MATCH_CNF        = 0x607D,

        CM_SLAC_USER_DATA_REQ    = 0x6080,
        CM_SLAC_USER_DATA_CNF    = 0x6081,
        CM_SLAC_USER_DATA_IND    = 0x6082,
        CM_SLAC_USER_DATA_RSP    = 0x6083,

        // ----- HomePlug AV standard (not SLAC-specific) -----
        // Used after a successful SLAC match to program the negotiated NMK
        // into the local PLC chip so that EV and EVSE end up in the same AVLN.
        CM_SET_KEY_REQ           = 0x6008,
        CM_SET_KEY_CNF           = 0x6009

    }

}
