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

using org.GraphDefined.Vanaheimr.Illias;

using cloud.charging.open.protocols.ISO15118_20.CommonTypes;
using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.AC
{

    /// <summary>
    /// The dynamic AC charge loop control mode.
    /// </summary>
    public class Dynamic_AC_CLReqControlMode : ADynamic_CLReqControlMode
    {

        #region Properties

        /// <summary>
        /// The EV maximum charge power.
        /// </summary>
        [Mandatory]
        public RationalNumber   EVMaximumChargePower         { get; }

        /// <summary>
        /// The optional EV maximum charge power on phase 2.
        /// </summary>
        [Optional]
        public RationalNumber?  EVMaximumChargePower_L2      { get; }

        /// <summary>
        /// The optional EV maximum charge power on phase 3.
        /// </summary>
        [Optional]
        public RationalNumber?  EVMaximumChargePower_L3      { get; }


        /// <summary>
        /// The EV minimum charge power.
        /// </summary>
        [Mandatory]
        public RationalNumber   EVMinimumChargePower         { get; }

        /// <summary>
        /// The optional EV minimum charge power on phase 2.
        /// </summary>
        [Optional]
        public RationalNumber?  EVMinimumChargePower_L2      { get; }

        /// <summary>
        /// The optional EV minimum charge power on phase 3.
        /// </summary>
        [Optional]
        public RationalNumber?  EVMinimumChargePower_L3      { get; }


        /// <summary>
        /// The EV present active power.
        /// </summary>
        [Mandatory]
        public RationalNumber   EVPresentActivePower         { get; }

        /// <summary>
        /// The optional EV present active power on phase 2.
        /// </summary>
        [Optional]
        public RationalNumber?  EVPresentActivePower_L2      { get; }

        /// <summary>
        /// The optional EV present active power on phase 2.
        /// </summary>
        [Optional]
        public RationalNumber?  EVPresentActivePower_L3      { get; }


        /// <summary>
        /// The EV present reactive power.
        /// </summary>
        [Mandatory]
        public RationalNumber   EVPresentReactivePower       { get; }

        /// <summary>
        /// The optional EV present reactive power on phase 2.
        /// </summary>
        [Optional]
        public RationalNumber?  EVPresentReactivePower_L2    { get; }

        /// <summary>
        /// The optional EV present reactive power on phase 3.
        /// </summary>
        [Optional]
        public RationalNumber?  EVPresentReactivePower_L3    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new dynamic AC charge loop control mode.
        /// </summary>
        /// <param name="EVTargetEnergyRequest">An EV target energy request.</param>
        /// <param name="EVMaximumEnergyRequest">An EV maximum energy request.</param>
        /// <param name="EVMinimumEnergyRequest">An EV minimum energy request.</param>
        /// <param name="DepartureTime">An optional departure time.</param>
        /// 
        /// <param name="EVMaximumChargePower">An EV maximum charge power.</param>
        /// <param name="EVMaximumChargePower_L2">An optional EV maximum charge power on phase 2.</param>
        /// <param name="EVMaximumChargePower_L3">An optional EV maximum charge power on phase 3.</param>
        /// 
        /// <param name="EVMinimumChargePower">An EV minimum charge power.</param>
        /// <param name="EVMinimumChargePower_L2">An optional EV minimum charge power on phase 2.</param>
        /// <param name="EVMinimumChargePower_L3">An optional EV minimum charge power on phase 3.</param>
        /// 
        /// <param name="EVPresentActivePower">An EV present active power.</param>
        /// <param name="EVPresentActivePower_L2">An optional EV present active power on phase 2.</param>
        /// <param name="EVPresentActivePower_L3">An optional EV present active power on phase 3.</param>
        /// 
        /// <param name="EVPresentReactivePower">An EV present reactive power.</param>
        /// <param name="EVPresentReactivePower_L2">An optional EV present reactive power on phase 2.</param>
        /// <param name="EVPresentReactivePower_L3">An optional EV present reactive power on phase 3.</param>
        public Dynamic_AC_CLReqControlMode(RationalNumber   EVTargetEnergyRequest,
                                           RationalNumber   EVMaximumEnergyRequest,
                                           RationalNumber   EVMinimumEnergyRequest,
                                           DateTime?        DepartureTime,

                                           RationalNumber   EVMaximumChargePower,
                                           RationalNumber?  EVMaximumChargePower_L2,
                                           RationalNumber?  EVMaximumChargePower_L3,

                                           RationalNumber   EVMinimumChargePower,
                                           RationalNumber?  EVMinimumChargePower_L2,
                                           RationalNumber?  EVMinimumChargePower_L3,

                                           RationalNumber   EVPresentActivePower,
                                           RationalNumber?  EVPresentActivePower_L2,
                                           RationalNumber?  EVPresentActivePower_L3,

                                           RationalNumber   EVPresentReactivePower,
                                           RationalNumber?  EVPresentReactivePower_L2,
                                           RationalNumber?  EVPresentReactivePower_L3)

            : base(EVTargetEnergyRequest,
                   EVMaximumEnergyRequest,
                   EVMinimumEnergyRequest,
                   DepartureTime)

        {

            this.EVMaximumChargePower       = EVMaximumChargePower;
            this.EVMaximumChargePower_L2    = EVMaximumChargePower_L2;
            this.EVMaximumChargePower_L3    = EVMaximumChargePower_L3;

            this.EVMinimumChargePower       = EVMinimumChargePower;
            this.EVMinimumChargePower_L2    = EVMinimumChargePower_L2;
            this.EVMinimumChargePower_L3    = EVMinimumChargePower_L3;

            this.EVPresentActivePower       = EVPresentActivePower;
            this.EVPresentActivePower_L2    = EVPresentActivePower_L2;
            this.EVPresentActivePower_L3    = EVPresentActivePower_L3;

            this.EVPresentReactivePower     = EVPresentReactivePower;
            this.EVPresentReactivePower_L2  = EVPresentReactivePower_L2;
            this.EVPresentReactivePower_L3  = EVPresentReactivePower_L3;

        }

        #endregion


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



        public static Boolean TryParse(JObject                           JSON,
                                       out Dynamic_AC_CLReqControlMode?  Dynamic_AC_CLReqControlMode,
                                       out String?                       ErrorResponse)
        {

            ErrorResponse                = null;
            Dynamic_AC_CLReqControlMode  = null;

            return false;

        }

        public override JObject ToJSON(CustomJObjectSerializerDelegate<RationalNumber>? CustomRationalNumberSerializer = null)
        {
            throw new NotImplementedException();
        }


    }

}
