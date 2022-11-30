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

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// The authorization response message.
    /// </summary>
    public class AuthorizationResponse : AV2GResponse
    {

        #region Properties

        /// <summary>
        /// The EVSE processing type.
        /// </summary>
        public ProcessingTypes  EVSEProcessing    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new authorization response message.
        /// </summary>
        /// <param name="Header">A message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        /// <param name="EVSEProcessing">An EVSE processing type..</param>
        public AuthorizationResponse(MessageHeaderType  Header,
                                     ResponseCodeTypes  ResponseCode,
                                     ProcessingTypes    EVSEProcessing)

            : base(Header,
                   ResponseCode)

        {

            this.EVSEProcessing = EVSEProcessing;

        }

        #endregion

    }

}
