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

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// The metering confirmation request.
    /// </summary>
    public class MeteringConfirmationRequest : ARequest<MeteringConfirmationRequest>
    {

        #region Properties

        /// <summary>
        /// Signed metering data.
        /// </summary>
        [Mandatory]
        public SignedMeteringDataType  SignedMeteringData    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new service detail request.
        /// </summary>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="SignedMeteringData">Signed metering data.</param>
        public MeteringConfirmationRequest(MessageHeader           MessageHeader,
                                           SignedMeteringDataType  SignedMeteringData)

            : base(MessageHeader)

        {

            this.SignedMeteringData = SignedMeteringData;

        }

        #endregion


        #region Documentation

        // <xs:element name="MeteringConfirmationReq" type="MeteringConfirmationReqType"/>
        //
        // <xs:complexType name="MeteringConfirmationReqType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:V2GRequestType">
        //             <xs:sequence>
        //                 <xs:element name="SignedMeteringData" type="SignedMeteringDataType"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion



    }

}
