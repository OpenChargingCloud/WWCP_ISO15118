﻿/*
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

namespace cloud.charging.open.protocols.ISO15118_20.CommonTypes
{

    /// <summary>
    /// The abstract charge parameter discovery request.
    /// </summary>
    public abstract class AChargeParameterDiscoveryRequest : ARequest<AChargeParameterDiscoveryRequest>
    {

        #region Constructor(s)

        /// <summary>
        /// Create a new abstract charge parameter discovery request.
        /// </summary>
        /// <param name="MessageHeader">A message header.</param>
        protected AChargeParameterDiscoveryRequest(MessageHeader MessageHeader)

            : base(MessageHeader)

        { }

        #endregion


        #region Documentation

        // <xs:complexType name="ChargeParameterDiscoveryReqType" abstract="true">
        //     <xs:complexContent>
        //         <xs:extension base="V2GRequestType"/>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

    }

}
