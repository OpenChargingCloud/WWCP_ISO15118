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

namespace cloud.charging.open.protocols.ISO15118_20.AC
{

    public class BPT_AC_CPDReqEnergyTransferMode : AC_CPDReqEnergyTransferMode
    {

        public RationalNumber   EVMaximumDischargePower       { get; }
        public RationalNumber?  EVMaximumDischargePower_L2    { get; }
        public RationalNumber?  EVMaximumDischargePower_L3    { get; }

        public RationalNumber   EVMinimumDischargePower       { get; }
        public RationalNumber?  EVMinimumDischargePower_L2    { get; }
        public RationalNumber?  EVMinimumDischargePower_L3    { get; }


        public BPT_AC_CPDReqEnergyTransferMode(RationalNumber   EVMaximumChargePower,
                                               RationalNumber?  EVMaximumChargePower_L2,
                                               RationalNumber?  EVMaximumChargePower_L3,

                                               RationalNumber   EVMinimumChargePower,
                                               RationalNumber?  EVMinimumChargePower_L2,
                                               RationalNumber?  EVMinimumChargePower_L3,

                                               RationalNumber   eVMaximumDischargePower,
                                               RationalNumber?  eVMaximumDischargePower_L2,
                                               RationalNumber?  eVMaximumDischargePower_L3,

                                               RationalNumber   eVMinimumDischargePower,
                                               RationalNumber?  eVMinimumDischargePower_L2,
                                               RationalNumber?  eVMinimumDischargePower_L3)

            : base(EVMaximumChargePower,
                   EVMaximumChargePower_L2,
                   EVMaximumChargePower_L3,

                   EVMinimumChargePower,
                   EVMinimumChargePower_L2,
                   EVMinimumChargePower_L3)

        {

            this.EVMaximumDischargePower     = eVMaximumDischargePower;
            this.EVMaximumDischargePower_L2  = eVMaximumDischargePower_L2;
            this.EVMaximumDischargePower_L3  = eVMaximumDischargePower_L3;

            this.EVMinimumDischargePower     = eVMinimumDischargePower;
            this.EVMinimumDischargePower_L2  = eVMinimumDischargePower_L2;
            this.EVMinimumDischargePower_L3  = eVMinimumDischargePower_L3;

        }





        #region Documentation

        // <xs:complexType name="BPT_AC_CPDReqEnergyTransferModeType">
        //     <xs:complexContent>
        //         <xs:extension base="AC_CPDReqEnergyTransferModeType">
        //             <xs:sequence>
        //                 <xs:element name="EVMaximumDischargePower"    type="v2gci_ct:RationalNumberType"/>
        //                 <xs:element name="EVMaximumDischargePower_L2" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVMaximumDischargePower_L3" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVMinimumDischargePower"    type="v2gci_ct:RationalNumberType"/>
        //                 <xs:element name="EVMinimumDischargePower_L2" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVMinimumDischargePower_L3" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion


    }

}
