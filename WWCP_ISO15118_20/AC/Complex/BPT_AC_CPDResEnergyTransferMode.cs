/*
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

    public class BPT_AC_CPDResEnergyTransferMode : AC_CPDResEnergyTransferMode
    {

        public RationalNumber   EVSEMaximumDischargePower       { get; }
        public RationalNumber?  EVSEMaximumDischargePower_L2    { get; }
        public RationalNumber?  EVSEMaximumDischargePower_L3    { get; }

        public RationalNumber   EVSEMinimumDischargePower       { get; }
        public RationalNumber?  EVSEMinimumDischargePower_L2    { get; }
        public RationalNumber?  EVSEMinimumDischargePower_L3    { get; }


        public BPT_AC_CPDResEnergyTransferMode(RationalNumber   EVSEMaximumChargePower,
                                               RationalNumber   EVSEMinimumChargePower,
                                               RationalNumber   EVSENominalFrequency,

                                               RationalNumber   EVSEMaximumDischargePower,
                                               RationalNumber?  EVSEMaximumDischargePower_L2,
                                               RationalNumber?  EVSEMaximumDischargePower_L3,

                                               RationalNumber   EVSEMinimumDischargePower,
                                               RationalNumber?  EVSEMinimumDischargePower_L2,
                                               RationalNumber?  EVSEMinimumDischargePower_L3,

                                               RationalNumber?  EVSEMaximumChargePowerL2   = null,
                                               RationalNumber?  EVSEMaximumChargePowerL3   = null,

                                               RationalNumber?  EVSEMinimumChargePowerL2   = null,
                                               RationalNumber?  EVSEMinimumChargePowerL3   = null,

                                               RationalNumber?  EVSEPresentActivePower     = null,
                                               RationalNumber?  EVSEPresentActivePowerL2   = null,
                                               RationalNumber?  EVSEPresentActivePowerL3   = null,

                                               RationalNumber?  MaximumPowerAsymmetry      = null,
                                               RationalNumber?  EVSEPowerRampLimitation    = null)

            : base(EVSEMaximumChargePower,
                   EVSEMinimumChargePower,
                   EVSENominalFrequency,

                   EVSEMaximumChargePowerL2,
                   EVSEMaximumChargePowerL3,

                   EVSEMinimumChargePowerL2,
                   EVSEMinimumChargePowerL3,

                   EVSEPresentActivePower,
                   EVSEPresentActivePowerL2,
                   EVSEPresentActivePowerL3,

                   MaximumPowerAsymmetry,
                   EVSEPowerRampLimitation)

        {

            this.EVSEMaximumDischargePower     = EVSEMaximumDischargePower;
            this.EVSEMaximumDischargePower_L2  = EVSEMaximumDischargePower_L2;
            this.EVSEMaximumDischargePower_L3  = EVSEMaximumDischargePower_L3;

            this.EVSEMinimumDischargePower     = EVSEMinimumDischargePower;
            this.EVSEMinimumDischargePower_L2  = EVSEMinimumDischargePower_L2;
            this.EVSEMinimumDischargePower_L3  = EVSEMinimumDischargePower_L3;

        }


        #region Documentation

        // <xs:complexType name="BPT_AC_CPDResEnergyTransferModeType">
        //     <xs:complexContent>
        //         <xs:extension base="AC_CPDResEnergyTransferModeType">
        //             <xs:sequence>
        //                 <xs:element name="EVSEMaximumDischargePower"    type="v2gci_ct:RationalNumberType"/>
        //                 <xs:element name="EVSEMaximumDischargePower_L2" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVSEMaximumDischargePower_L3" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVSEMinimumDischargePower"    type="v2gci_ct:RationalNumberType"/>
        //                 <xs:element name="EVSEMinimumDischargePower_L2" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVSEMinimumDischargePower_L3" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion


    }

}
