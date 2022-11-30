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

using org.GraphDefined.Vanaheimr.Illias;

using cloud.charging.open.protocols.ISO15118_20.CommonTypes;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// The authorization setup response message.
    /// </summary>
    public class AuthorizationSetupResponse : AV2GResponse
    {

        #region Properties

        /// <summary>
        /// The enumeration of 1 or 2 authorization services.
        /// </summary>
        [Mandatory]
        public IEnumerable<AuthorizationTypes>  AuthorizationServices             { get; }

        /// <summary>
        /// 
        /// </summary>
        [Mandatory]
        public Boolean                          CertificateInstallationService    { get; }


        // Choose one of the following...

        /// <summary>
        /// 
        /// </summary>
        [MandatoryChoice("ASResAuthorizationMode")]
        public EIM_ASResAuthorizationModeType?  EIM_ASResAuthorizationMode        { get; }

        /// <summary>
        /// 
        /// </summary>
        [MandatoryChoice("ASResAuthorizationMode")]
        public PnC_ASResAuthorizationModeType?  PnC_ASResAuthorizationMode        { get; }

        #endregion

        #region Constructor(s)

        #region (private) AuthorizationSetupResponse(..., EIM_ASResAuthorizationMode, PnC_ASResAuthorizationMode)

        /// <summary>
        /// Create a new authorization response message.
        /// </summary>
        /// <param name="Header">A message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        /// <param name="AuthorizationServices">An enumeration of 1 or 2 authorization services.</param>
        /// <param name="CertificateInstallationService"></param>
        /// <param name="EIM_ASResAuthorizationMode"></param>
        /// <param name="PnC_ASResAuthorizationMode"></param>
        private AuthorizationSetupResponse(MessageHeaderType                Header,
                                           ResponseCodeTypes                ResponseCode,
                                           IEnumerable<AuthorizationTypes>  AuthorizationServices,
                                           Boolean                          CertificateInstallationService,
                                           EIM_ASResAuthorizationModeType?  EIM_ASResAuthorizationMode,
                                           PnC_ASResAuthorizationModeType?  PnC_ASResAuthorizationMode)

            : base(Header,
                   ResponseCode)

        {

            if (AuthorizationServices.Any())
                throw new ArgumentException("The given enumeration of authorization services must not be empty!",
                                            nameof(AuthorizationServices));

            if (AuthorizationServices.Count() > 2)
                throw new ArgumentException("The given enumeration of authorization services must not have more than 2 elements!",
                                            nameof(AuthorizationServices));

            this.AuthorizationServices           = AuthorizationServices.Distinct();
            this.CertificateInstallationService  = CertificateInstallationService;
            this.EIM_ASResAuthorizationMode      = EIM_ASResAuthorizationMode;
            this.PnC_ASResAuthorizationMode      = PnC_ASResAuthorizationMode;

        }

        #endregion

        #region AuthorizationSetupResponse(..., EIM_ASResAuthorizationMode)

        /// <summary>
        /// Create a new authorization response message.
        /// </summary>
        /// <param name="Header">A message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        /// <param name="AuthorizationServices">An enumeration of 1 or 2 authorization services.</param>
        /// <param name="CertificateInstallationService"></param>
        /// <param name="EIM_ASResAuthorizationMode"></param>
        public AuthorizationSetupResponse(MessageHeaderType                Header,
                                          ResponseCodeTypes                ResponseCode,
                                          IEnumerable<AuthorizationTypes>  AuthorizationServices,
                                          Boolean                          CertificateInstallationService,
                                          EIM_ASResAuthorizationModeType   EIM_ASResAuthorizationMode)

            : this(Header,
                   ResponseCode,
                   AuthorizationServices,
                   CertificateInstallationService,
                   EIM_ASResAuthorizationMode,
                   null)

        { }

        #endregion

        #region AuthorizationSetupResponse(..., PnC_ASResAuthorizationMode)

        /// <summary>
        /// Create a new authorization response message.
        /// </summary>
        /// <param name="Header">A message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        /// <param name="AuthorizationServices">An enumeration of 1 or 2 authorization services.</param>
        /// <param name="CertificateInstallationService"></param>
        /// <param name="PnC_ASResAuthorizationMode"></param>
        public AuthorizationSetupResponse(MessageHeaderType                Header,
                                          ResponseCodeTypes                ResponseCode,
                                          IEnumerable<AuthorizationTypes>  AuthorizationServices,
                                          Boolean                          CertificateInstallationService,
                                          PnC_ASResAuthorizationModeType   PnC_ASResAuthorizationMode)

            : this(Header,
                   ResponseCode,
                   AuthorizationServices,
                   CertificateInstallationService,
                   null,
                   PnC_ASResAuthorizationMode)

        { }

        #endregion

        #endregion

    }

}
