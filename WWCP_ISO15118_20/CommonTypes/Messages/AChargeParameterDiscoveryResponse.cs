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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonTypes
{

    /// <summary>
    /// The abstract charge parameter discovery response.
    /// </summary>
    public abstract class AChargeParameterDiscoveryResponse : AResponse<AChargeParameterDiscoveryRequest,
                                                                        AChargeParameterDiscoveryResponse>
    {

        #region Constructor(s)

        /// <summary>
        /// Create a new abstract charge parameter discovery response.
        /// </summary>
        /// <param name="Request">The abstract charge parameter discovery request leading to this result.</param>
        /// <param name="MessageHeader">An ISO 15118-20 V2G common message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        public AChargeParameterDiscoveryResponse(AChargeParameterDiscoveryRequest  Request,
                                                 MessageHeader                     MessageHeader,
                                                 ResponseCodes                     ResponseCode)

            : base(Request,
                   MessageHeader,
                   ResponseCode)

        { }

        #endregion

    }

}
