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
    /// The AC CPD request energy transfer mode.
    /// </summary>
    public class AC_CPDReqEnergyTransferMode
    {

        #region Properties

        /// <summary>
        /// The EV maximum charge power.
        /// </summary>
        [Mandatory]
        public RationalNumber   EVMaximumChargePower       { get; }

        /// <summary>
        /// The optional EV maximum charge power on phase 2.
        /// </summary>
        [Optional]
        public RationalNumber?  EVMaximumChargePower_L2    { get; }

        /// <summary>
        /// The optional EV maximum charge power on phase 3.
        /// </summary>
        [Optional]
        public RationalNumber?  EVMaximumChargePower_L3    { get; }


        /// <summary>
        /// The EV minimum charge power.
        /// </summary>
        [Mandatory]
        public RationalNumber   EVMinimumChargePower       { get; }

        /// <summary>
        /// The optional EV minimum charge power on phase 2.
        /// </summary>
        [Optional]
        public RationalNumber?  EVMinimumChargePower_L2    { get; }

        /// <summary>
        /// The optional EV minimum charge power on phase 3.
        /// </summary>
        [Optional]
        public RationalNumber?  EVMinimumChargePower_L3    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new AC CPD request energy transfer mode.
        /// </summary>
        /// <param name="eVMaximumChargePower">An EV maximum charge power.</param>
        /// <param name="eVMaximumChargePower_L2">An optional EV maximum charge power on phase 2.</param>
        /// <param name="eVMaximumChargePower_L3">An optional EV maximum charge power on phase 3.</param>
        /// 
        /// <param name="eVMinimumChargePower">An EV minimum charge power.</param>
        /// <param name="eVMinimumChargePower_L2">An optional EV minimum charge power on phase 2.</param>
        /// <param name="eVMinimumChargePower_L3">An optional EV minimum charge power on phase 3.</param>
        public AC_CPDReqEnergyTransferMode(RationalNumber   EVMaximumChargePower,
                                           RationalNumber?  EVMaximumChargePower_L2,
                                           RationalNumber?  EVMaximumChargePower_L3,

                                           RationalNumber   EVMinimumChargePower,
                                           RationalNumber?  EVMinimumChargePower_L2,
                                           RationalNumber?  EVMinimumChargePower_L3)
        {

            this.EVMaximumChargePower     = EVMaximumChargePower;
            this.EVMaximumChargePower_L2  = EVMaximumChargePower_L2;
            this.EVMaximumChargePower_L3  = EVMaximumChargePower_L3;

            this.EVMinimumChargePower     = EVMinimumChargePower;
            this.EVMinimumChargePower_L2  = EVMinimumChargePower_L2;
            this.EVMinimumChargePower_L3  = EVMinimumChargePower_L3;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="AC_CPDReqEnergyTransferModeType">
        //     <xs:sequence>
        //         <xs:element name="EVMaximumChargePower"    type="v2gci_ct:RationalNumberType"/>
        //         <xs:element name="EVMaximumChargePower_L2" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //         <xs:element name="EVMaximumChargePower_L3" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //         <xs:element name="EVMinimumChargePower"    type="v2gci_ct:RationalNumberType"/>
        //         <xs:element name="EVMinimumChargePower_L2" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //         <xs:element name="EVMinimumChargePower_L3" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

    }

}
