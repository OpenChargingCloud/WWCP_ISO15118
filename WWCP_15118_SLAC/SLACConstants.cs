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
    /// HomePlug AV / GreenPHY constants used by the SLAC matching profile
    /// for ISO 15118-3. Values are taken from the HomePlug AV 2.1 spec and
    /// HomePlug GreenPHY Specification, Annex A (PEV-EVSE matching).
    /// </summary>
    public static class SLACConstants
    {

        /// <summary>EtherType for HomePlug AV management messages (used on the real PLC link).</summary>
        public const ushort HomePlugAvEtherType = 0x88E1;

        /// <summary>Management Message Version. 0x01 for HPAV-1.1+ MMEs (3-byte MM header).</summary>
        public const byte Mmv = 0x01;

        /// <summary>APPLICATION_TYPE = 0x00: PEV-EVSE matching profile.</summary>
        public const byte ApplicationType_PevEvseMatching = 0x00;

        /// <summary>SECURITY_TYPE = 0x00: no security (NMK exchanged in clear, per ISO 15118-3).</summary>
        public const byte SecurityType_None = 0x00;

        /// <summary>Default number of M-Sounds the EV transmits (typical: 10).</summary>
        public const byte DefaultNumSounds = 10;

        /// <summary>Default sounding timeout in 100 ms units (TT_EVSE_match_MNBC).</summary>
        public const byte DefaultTimeOut100ms = 6;

        /// <summary>Number of attenuation groups in the AAG profile (HPGP fixed).</summary>
        public const byte NumAttenGroups = 58;

        /// <summary>Length of a HomePlug AV Network ID (NID) in bytes.</summary>
        public const int NidLength = 7;

        /// <summary>Length of a HomePlug AV Network Membership Key (NMK) in bytes.</summary>
        public const int NmkLength = 16;

        /// <summary>Length of a RunID in bytes.</summary>
        public const int RunIdLength = 8;

        /// <summary>Length of a station identifier (e.g. EVSE_ID, PEV_ID) in bytes.</summary>
        public const int StationIdLength = 17;

    }

}
