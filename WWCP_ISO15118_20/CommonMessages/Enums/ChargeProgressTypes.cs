/*
 * Copyright (c) 2021-2022 GraphDefined GmbH
 * This file is part of WWCP ISO 15118-20 <https://github.com/OpenChargingCloud/WWCP_ISO15118_20>
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

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    #region Documentation

    // <xs:simpleType name="chargeProgressType">
    //     <xs:restriction base="xs:string">
    //         <xs:enumeration value="Start"/>
    //         <xs:enumeration value="Stop"/>
    //         <xs:enumeration value="Standby"/>
    //         <xs:enumeration value="ScheduleRenegotiation"/>
    //     </xs:restriction>
    // </xs:simpleType>

    #endregion

    public enum ChargeProgressTypes
    {

        /// <summary>
        /// Start charging.
        /// </summary>
        Start,

        /// <summary>
        /// Stop charging.
        /// </summary>
        Stop,

        /// <summary>
        /// Standby.
        /// </summary>
        Standby,

        /// <summary>
        /// Schedule renegotiation.
        /// </summary>
        ScheduleRenegotiation

    }

}
