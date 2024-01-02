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
    /// The abstract scheduled charge loop control mode.
    /// </summary>
    public abstract class AScheduled_CLReqControlMode : ACLReqControlMode
    {

        #region Properties

        /// <summary>
        /// The optional EV target energy request.
        /// </summary>
        [Optional]
        public RationalNumber?  EVTargetEnergyRequest     { get; }

        /// <summary>
        /// The optional EV maximum energy request.
        /// </summary>
        [Optional]
        public RationalNumber?  EVMaximumEnergyRequest    { get; }

        /// <summary>
        /// The optional EV minimum energy request.
        /// </summary>
        [Optional]
        public RationalNumber?  EVMinimumEnergyRequest    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new abstract scheduled charge loop control mode.
        /// </summary>
        /// <param name="EVTargetEnergyRequest">An optional EV target energy request.</param>
        /// <param name="EVMaximumEnergyRequest">An optional EV maximum energy request.</param>
        /// <param name="EVMinimumEnergyRequest">An optional EV minimum energy request.</param>
        public AScheduled_CLReqControlMode(RationalNumber?  EVTargetEnergyRequest,
                                           RationalNumber?  EVMaximumEnergyRequest,
                                           RationalNumber?  EVMinimumEnergyRequest)
        {

            this.EVTargetEnergyRequest   = EVTargetEnergyRequest;
            this.EVMaximumEnergyRequest  = EVMaximumEnergyRequest;
            this.EVMinimumEnergyRequest  = EVMinimumEnergyRequest;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="Scheduled_CLReqControlModeType" abstract="true">
        //     <xs:complexContent>
        //         <xs:extension base="CLReqControlModeType">
        //             <xs:sequence>
        //                 <xs:element name="EVTargetEnergyRequest"  type="RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVMaximumEnergyRequest" type="RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVMinimumEnergyRequest" type="RationalNumberType" minOccurs="0"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion


    }

}
