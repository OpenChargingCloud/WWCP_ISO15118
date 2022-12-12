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
using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.AC
{

    /// <summary>
    /// The AC CPD response energy transfer mode.
    /// </summary>
    public class AC_CPDResEnergyTransferMode
    {

        #region Properties

        /// <summary>
        /// The EVSE maximum charge power.
        /// </summary>
        [Mandatory]
        public RationalNumber   EVSEMaximumChargePower       { get; }

        /// <summary>
        /// The optional EVSE maximum charge power on phase 2.
        /// </summary>
        [Optional]
        public RationalNumber?  EVSEMaximumChargePower_L2    { get; }

        /// <summary>
        /// The optional EVSE maximum charge power on phase 3.
        /// </summary>
        [Optional]
        public RationalNumber?  EVSEMaximumChargePower_L3    { get; }


        /// <summary>
        /// The EVSE minimum charge power.
        /// </summary>
        [Mandatory]
        public RationalNumber   EVSEMinimumChargePower       { get; }

        /// <summary>
        /// The optional EVSE minimum charge power on phase 2.
        /// </summary>
        [Optional]
        public RationalNumber?  EVSEMinimumChargePower_L2    { get; }

        /// <summary>
        /// The optional EVSE minimum charge power on phase 3.
        /// </summary>
        [Optional]
        public RationalNumber?  EVSEMinimumChargePower_L3    { get; }


        /// <summary>
        /// 
        /// </summary>
        [Optional]
        public RationalNumber?  EVSEPresentActivePower       { get; }

        /// <summary>
        /// 
        /// </summary>
        [Optional]
        public RationalNumber?  EVSEPresentActivePower_L2    { get; }

        /// <summary>
        /// 
        /// </summary>
        [Optional]
        public RationalNumber?  EVSEPresentActivePower_L3    { get; }


        /// <summary>
        /// 
        /// </summary>
        [Optional]
        public RationalNumber   EVSENominalFrequency         { get; }

        /// <summary>
        /// 
        /// </summary>
        [Optional]
        public RationalNumber?  MaximumPowerAsymmetry        { get; }

        /// <summary>
        /// 
        /// </summary>
        [Optional]
        public RationalNumber?  EVSEPowerRampLimitation      { get; }

        #endregion

        #region Constructor(s)

        public AC_CPDResEnergyTransferMode(RationalNumber   EVSEMaximumChargePower,
                                           RationalNumber?  EVSEMaximumChargePower_L2,
                                           RationalNumber?  EVSEMaximumChargePower_L3,

                                           RationalNumber   EVSEMinimumChargePower,
                                           RationalNumber?  EVSEMinimumChargePower_L2,
                                           RationalNumber?  EVSEMinimumChargePower_L3,

                                           RationalNumber?  EVSEPresentActivePower,
                                           RationalNumber?  EVSEPresentActivePower_L2,
                                           RationalNumber?  EVSEPresentActivePower_L3,

                                           RationalNumber   EVSENominalFrequency,
                                           RationalNumber?  MaximumPowerAsymmetry,
                                           RationalNumber?  EVSEPowerRampLimitation)

        {

            this.EVSEMaximumChargePower     = EVSEMaximumChargePower;
            this.EVSEMaximumChargePower_L2  = EVSEMaximumChargePower_L2;
            this.EVSEMaximumChargePower_L3  = EVSEMaximumChargePower_L3

            this.EVSEMinimumChargePower     = EVSEMinimumChargePower;
            this.EVSEMinimumChargePower_L2  = EVSEMinimumChargePower_L2;
            this.EVSEMinimumChargePower_L3  = EVSEMinimumChargePower_L3;

            this.EVSEPresentActivePower     = EVSEPresentActivePower;
            this.EVSEPresentActivePower_L2  = EVSEPresentActivePower_L2;
            this.EVSEPresentActivePower_L3  = EVSEPresentActivePower_L3;

            this.EVSENominalFrequency       = EVSENominalFrequency;
            this.MaximumPowerAsymmetry      = MaximumPowerAsymmetry;
            this.EVSEPowerRampLimitation    = EVSEPowerRampLimitation;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="AC_CPDResEnergyTransferModeType">
        //     <xs:sequence>
        //         <xs:element name="EVSEMaximumChargePower"    type="v2gci_ct:RationalNumberType"/>
        //         <xs:element name="EVSEMaximumChargePower_L2" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //         <xs:element name="EVSEMaximumChargePower_L3" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //
        //         <xs:element name="EVSEMinimumChargePower"    type="v2gci_ct:RationalNumberType"/>
        //         <xs:element name="EVSEMinimumChargePower_L2" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //         <xs:element name="EVSEMinimumChargePower_L3" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //
        //         <xs:element name="EVSENominalFrequency"      type="v2gci_ct:RationalNumberType"/>
        //         <xs:element name="MaximumPowerAsymmetry"     type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //         <xs:element name="EVSEPowerRampLimitation"   type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //
        //         <xs:element name="EVSEPresentActivePower"    type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //         <xs:element name="EVSEPresentActivePower_L2" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //         <xs:element name="EVSEPresentActivePower_L3" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

    }

}
