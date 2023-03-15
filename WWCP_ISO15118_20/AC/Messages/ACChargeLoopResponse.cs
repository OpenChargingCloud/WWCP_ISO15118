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

    /// <summary>
    /// The AC charge loop response.
    /// </summary>
    public class ACChargeLoopResponse : AChargeLoopResponse
    {

        #region Properties

        /// <summary>
        /// The EVSE target frequency.
        /// </summary>
        public RationalNumber?     EVSETargetFrequency    { get; }

        /// <summary>
        /// The abstract control mode.
        /// </summary>
        public ACLResControlMode?  CLResControlMode       { get; }

        #endregion

        #region Constructor(s)

        #region AC_ChargeLoopResponse(EVSETargetFrequency, Dynamic_AC_CLResControlMode)

        /// <summary>
        /// Create a new dynamic AC charge loop response.
        /// </summary>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="DisplayParameters">Optional display parameters.</param>
        /// <param name="MeterInfoRequested">Whether meter information is requested.</param>
        /// 
        /// <param name="EVSETargetFrequency">An EVSE target frequency.</param>
        /// <param name="Dynamic_AC_CLResControlMode">The dynamic control mode.</param>
        public ACChargeLoopResponse(RationalNumber?              EVSETargetFrequency,
                                    Dynamic_AC_CLResControlMode  Dynamic_AC_CLResControlMode)
        {

            this.EVSETargetFrequency  = EVSETargetFrequency;
            this.CLResControlMode     = Dynamic_AC_CLResControlMode;

        }

        #endregion

        #region AC_ChargeLoopResponse(EVSETargetFrequency, Scheduled_AC_CLResControlMode)

        /// <summary>
        /// Create a new scheduled AC charge loop response.
        /// </summary>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="DisplayParameters">Optional display parameters.</param>
        /// <param name="MeterInfoRequested">Whether meter information is requested.</param>
        /// 
        /// <param name="EVSETargetFrequency">An EVSE target frequency.</param>
        /// <param name="Scheduled_AC_CLResControlMode">The scheduled control mode.</param>
        public ACChargeLoopResponse(RationalNumber?                EVSETargetFrequency,
                                    Scheduled_AC_CLResControlMode  Scheduled_AC_CLResControlMode)
        {

            this.EVSETargetFrequency  = EVSETargetFrequency;
            this.CLResControlMode     = Scheduled_AC_CLResControlMode;

        }

        #endregion

        #endregion


        #region Documentation

        // <xs:element name="AC_ChargeLoopRes" type="AC_ChargeLoopResType"/>
        // 
        // <xs:complexType name="AC_ChargeLoopResType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:ChargeLoopResType">
        //             <xs:sequence>
        //                 <xs:element name="EVSETargetFrequency" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element ref="v2gci_ct:CLResControlMode"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

    }

}
