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
    /// The AC charge parameter discovery response.
    /// </summary>
    public class AC_ChargeParameterDiscoveryResponse : AChargeParameterDiscoveryResponse
    {

        #region Properties

        /// <summary>
        /// The AC CPD response energy transfer mode.
        /// </summary>
        public AC_CPDResEnergyTransferMode? AC_CPDResEnergyTransferMode { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new AC charge parameter discovery response.
        /// </summary>
        /// <param name="Request">The AC charge parameter discovery request leading to this result.</param>
        /// <param name="MessageHeader">An ISO 15118-20 V2G common message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        /// <param name="AC_CPDResEnergyTransferMode">An AC CPD response energy transfer mode.</param>
        public AC_ChargeParameterDiscoveryResponse(AC_ChargeParameterDiscoveryRequest  Request,
                                                   MessageHeader                       MessageHeader,
                                                   ResponseCodes                       ResponseCode,
                                                   AC_CPDResEnergyTransferMode?        AC_CPDResEnergyTransferMode)

            : base(Request,
                   MessageHeader,
                   ResponseCode)

        {

            this.AC_CPDResEnergyTransferMode = AC_CPDResEnergyTransferMode;

        }

        #endregion


        #region Documentation

        // <xs:element name="AC_ChargeParameterDiscoveryRes" type="AC_ChargeParameterDiscoveryResType"/>
        //
        // <xs:complexType name="AC_ChargeParameterDiscoveryResType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:ChargeParameterDiscoveryResType">
        //             <xs:sequence>
        //                 <xs:element ref="AC_CPDResEnergyTransferMode"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion



        public override bool Equals(AChargeParameterDiscoveryResponse? AV2GResponse)
        {
            throw new NotImplementedException();
        }

    }

}
