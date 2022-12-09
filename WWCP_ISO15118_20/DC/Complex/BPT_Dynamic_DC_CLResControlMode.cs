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

#region Usings

using cloud.charging.open.protocols.ISO15118_20.CommonTypes;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.DC
{

    public class BPT_Dynamic_DC_CLResControlMode : Dynamic_DC_CLResControlMode
    {

        public RationalNumber  EVSEMaximumDischargePower      { get; }
        public RationalNumber  EVSEMinimumDischargePower      { get; }
        public RationalNumber  EVSEMaximumDischargeCurrent    { get; }
        public RationalNumber  EVSEMinimumVoltage             { get; }


        #region Documentation

        // <xs:complexType name="BPT_Dynamic_DC_CLResControlModeType">
        //     <xs:complexContent>
        //         <xs:extension base="Dynamic_DC_CLResControlModeType">
        //             <xs:sequence>
        //                 <xs:element name="EVSEMaximumDischargePower"   type="v2gci_ct:RationalNumberType"/>
        //                 <xs:element name="EVSEMinimumDischargePower"   type="v2gci_ct:RationalNumberType"/>
        //                 <xs:element name="EVSEMaximumDischargeCurrent" type="v2gci_ct:RationalNumberType"/>
        //                 <xs:element name="EVSEMinimumVoltage"          type="v2gci_ct:RationalNumberType"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

    }

}
