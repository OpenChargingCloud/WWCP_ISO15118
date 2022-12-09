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

namespace cloud.charging.open.protocols.ISO15118_20.AC
{

    public class Dynamic_AC_CLReqControlMode : ADynamic_CLReqControlMode
    {

        public RationalNumber   EVMaximumChargePower         { get; }
        public RationalNumber?  EVMaximumChargePower_L2      { get; }
        public RationalNumber?  EVMaximumChargePower_L3      { get; }

        public RationalNumber   EVMinimumChargePower         { get; }
        public RationalNumber?  EVMinimumChargePower_L2      { get; }
        public RationalNumber?  EVMinimumChargePower_L3      { get; }

        public RationalNumber   EVPresentActivePower         { get; }
        public RationalNumber?  EVPresentActivePower_L2      { get; }
        public RationalNumber?  EVPresentActivePower_L3      { get; }

        public RationalNumber   EVPresentReactivePower       { get; }
        public RationalNumber?  EVPresentReactivePower_L2    { get; }
        public RationalNumber?  EVPresentReactivePower_L3    { get; }


        #region Documentation

        // <xs:complexType name="Dynamic_AC_CLReqControlModeType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:Dynamic_CLReqControlModeType">
        //             <xs:sequence>
        //                 <xs:element name="EVMaximumChargePower"      type="v2gci_ct:RationalNumberType"/>
        //                 <xs:element name="EVMaximumChargePower_L2"   type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVMaximumChargePower_L3"   type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVMinimumChargePower"      type="v2gci_ct:RationalNumberType"/>
        //                 <xs:element name="EVMinimumChargePower_L2"   type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVMinimumChargePower_L3"   type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVPresentActivePower"      type="v2gci_ct:RationalNumberType"/>
        //                 <xs:element name="EVPresentActivePower_L2"   type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVPresentActivePower_L3"   type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVPresentReactivePower"    type="v2gci_ct:RationalNumberType"/>
        //                 <xs:element name="EVPresentReactivePower_L2" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVPresentReactivePower_L3" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

    }

}
