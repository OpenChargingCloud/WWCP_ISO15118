/*
 * Copyright (c) 2021-2024 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonTypes
{

    /// <summary>
    /// The abstract dynamic charge loop control mode.
    /// </summary>
    public abstract class ADynamic_CLReqControlMode : ACLReqControlMode
    {

        #region Properties

        /// <summary>
        /// The EV target energy request.
        /// </summary>
        [Mandatory]
        public RationalNumber  EVTargetEnergyRequest     { get; }

        /// <summary>
        /// The EV maximum energy request.
        /// </summary>
        [Mandatory]
        public RationalNumber  EVMaximumEnergyRequest    { get; }

        /// <summary>
        /// The EV minimum energy request.
        /// </summary>
        [Mandatory]
        public RationalNumber  EVMinimumEnergyRequest    { get; }

        /// <summary>
        /// The optional departure time.
        /// </summary>
        [Optional]
        public DateTime?       DepartureTime             { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new abstract dynamic charge loop control mode.
        /// </summary>
        /// <param name="EVTargetEnergyRequest">An EV target energy request.</param>
        /// <param name="EVMaximumEnergyRequest">An EV maximum energy request.</param>
        /// <param name="EVMinimumEnergyRequest">An EV minimum energy request.</param>
        /// <param name="DepartureTime">An optional departure time.</param>
        public ADynamic_CLReqControlMode(RationalNumber  EVTargetEnergyRequest,
                                         RationalNumber  EVMaximumEnergyRequest,
                                         RationalNumber  EVMinimumEnergyRequest,
                                         DateTime?       DepartureTime)
        {

            this.EVTargetEnergyRequest   = EVTargetEnergyRequest;
            this.EVMaximumEnergyRequest  = EVMaximumEnergyRequest;
            this.EVMinimumEnergyRequest  = EVMinimumEnergyRequest;
            this.DepartureTime           = DepartureTime;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="Dynamic_CLReqControlModeType" abstract="true">
        //     <xs:complexContent>
        //         <xs:extension base="CLReqControlModeType">
        //             <xs:sequence>
        //                 <xs:element name="DepartureTime"          type="xs:unsignedInt" minOccurs="0"/>
        //                 <xs:element name="EVTargetEnergyRequest"  type="RationalNumberType"/>
        //                 <xs:element name="EVMaximumEnergyRequest" type="RationalNumberType"/>
        //                 <xs:element name="EVMinimumEnergyRequest" type="RationalNumberType"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion


    }

}
