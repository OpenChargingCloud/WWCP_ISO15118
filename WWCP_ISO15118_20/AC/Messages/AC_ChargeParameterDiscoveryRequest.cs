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

    /// <summary>
    /// The AC charge parameter discovery request.
    /// </summary>
    public class AC_ChargeParameterDiscoveryRequest : AChargeParameterDiscoveryRequest
    {

        #region Properties

        /// <summary>
        /// The AC CPD request energy transfer mode.
        /// </summary>
        public AC_CPDReqEnergyTransferMode?  AC_CPDReqEnergyTransferMode    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new AC charge parameter discovery request.
        /// </summary>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="AC_CPDReqEnergyTransferMode">An AC CPD request energy transfer mode.</param>
        public AC_ChargeParameterDiscoveryRequest(MessageHeader                 MessageHeader,
                                                  AC_CPDReqEnergyTransferMode?  AC_CPDReqEnergyTransferMode   = null)

            : base(MessageHeader)

        {

            this.AC_CPDReqEnergyTransferMode = AC_CPDReqEnergyTransferMode;

        }

        #endregion


        #region Documentation

        // <xs:element name="AC_ChargeParameterDiscoveryReq" type="AC_ChargeParameterDiscoveryReqType"/>
        //
        // <xs:complexType name="AC_ChargeParameterDiscoveryReqType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:ChargeParameterDiscoveryReqType">
        //             <xs:sequence>
        //                 <xs:element ref="AC_CPDReqEnergyTransferMode"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

    }

}
