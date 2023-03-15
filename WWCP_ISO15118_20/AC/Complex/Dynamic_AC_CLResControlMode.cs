/*
 * Copyright (c) 2021-2023 GraphDefined GmbH
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

namespace cloud.charging.open.protocols.ISO15118_20.AC
{

    public class Dynamic_AC_CLResControlMode : ADynamic_CLResControlMode
    {

        public RationalNumber   EVSETargetActivePower         { get; }
        public RationalNumber?  EVSETargetActivePower_L2      { get; }
        public RationalNumber?  EVSETargetActivePower_L3      { get; }

        public RationalNumber?  EVSETargetReactivePower       { get; }
        public RationalNumber?  EVSETargetReactivePower_L2    { get; }
        public RationalNumber?  EVSETargetReactivePower_L3    { get; }

        public RationalNumber?  EVSEPresentActivePower        { get; }
        public RationalNumber?  EVSEPresentActivePower_L2     { get; }
        public RationalNumber?  EVSEPresentActivePower_L3     { get; }


        #region Documentation

        // <xs:complexType name="Dynamic_AC_CLResControlModeType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:Dynamic_CLResControlModeType">
        //             <xs:sequence>
        //                 <xs:element name="EVSETargetActivePower"      type="v2gci_ct:RationalNumberType"/>
        //                 <xs:element name="EVSETargetActivePower_L2"   type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVSETargetActivePower_L3"   type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVSETargetReactivePower"    type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVSETargetReactivePower_L2" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVSETargetReactivePower_L3" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVSEPresentActivePower"     type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVSEPresentActivePower_L2"  type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVSEPresentActivePower_L3"  type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

    }

}
