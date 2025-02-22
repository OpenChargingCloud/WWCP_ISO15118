﻿/*
 * Copyright (c) 2021-2025 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

#region Usings

using cloud.charging.open.protocols.ISO15118_20.CommonTypes;


#endregion

namespace cloud.charging.open.protocols.ISO15118_20.DC
{

    public class DC_ChargeLoopResponse : AResponse
    {

        public RationalNumber      EVSEPresentCurrent          { get; }
        public RationalNumber      EVSEPresentVoltage          { get; }
        public Boolean             EVSEPowerLimitAchieved      { get; }
        public Boolean             EVSECurrentLimitAchieved    { get; }
        public Boolean             EVSEVoltageLimitAchieved    { get; }
        public ACLResControlMode?  CLResControlMode            { get; }


        #region Documentation

        // <xs:complexType name="DC_ChargeLoopResType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:ChargeLoopResType">
        //             <xs:sequence>
        //                 <xs:element name="EVSEPresentCurrent"       type="v2gci_ct:RationalNumberType"/>
        //                 <xs:element name="EVSEPresentVoltage"       type="v2gci_ct:RationalNumberType"/>
        //                 <xs:element name="EVSEPowerLimitAchieved"   type="xs:boolean"/>
        //                 <xs:element name="EVSECurrentLimitAchieved" type="xs:boolean"/>
        //                 <xs:element name="EVSEVoltageLimitAchieved" type="xs:boolean"/>
        //                 <xs:element ref="v2gci_ct:CLResControlMode"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

    }

}
