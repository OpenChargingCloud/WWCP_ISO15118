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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

using cloud.charging.open.protocols.ISO15118_20.CommonTypes;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// The authorization setup response.
    /// </summary>
    public class AuthorizationSetupResponse : AV2GResponse<AuthorizationSetupRequest,
                                                           AuthorizationSetupResponse>
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
        /// Create a new authorization response.
        /// </summary>
        /// <param name="Request">The authorization setup request leading to this response.</param>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        /// <param name="AuthorizationServices">An enumeration of 1 or 2 authorization services.</param>
        /// <param name="CertificateInstallationService"></param>
        /// <param name="EIM_ASResAuthorizationMode"></param>
        /// <param name="PnC_ASResAuthorizationMode"></param>
        private AuthorizationSetupResponse(AuthorizationSetupRequest        Request,
                                           MessageHeader                    MessageHeader,
                                           ResponseCodes                    ResponseCode,
                                           IEnumerable<AuthorizationTypes>  AuthorizationServices,
                                           Boolean                          CertificateInstallationService,
                                           EIM_ASResAuthorizationModeType?  EIM_ASResAuthorizationMode,
                                           PnC_ASResAuthorizationModeType?  PnC_ASResAuthorizationMode)

            : base(Request,
                   MessageHeader,
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
        /// <param name="Request">The authorization setup request leading to this response.</param>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        /// <param name="AuthorizationServices">An enumeration of 1 or 2 authorization services.</param>
        /// <param name="CertificateInstallationService"></param>
        /// <param name="EIM_ASResAuthorizationMode"></param>
        public AuthorizationSetupResponse(AuthorizationSetupRequest        Request,
                                          MessageHeader                    MessageHeader,
                                          ResponseCodes                    ResponseCode,
                                          IEnumerable<AuthorizationTypes>  AuthorizationServices,
                                          Boolean                          CertificateInstallationService,
                                          EIM_ASResAuthorizationModeType   EIM_ASResAuthorizationMode)

            : this(Request,
                   MessageHeader,
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
        /// <param name="Request">The authorization setup request leading to this response.</param>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        /// <param name="AuthorizationServices">An enumeration of 1 or 2 authorization services.</param>
        /// <param name="CertificateInstallationService"></param>
        /// <param name="PnC_ASResAuthorizationMode"></param>
        public AuthorizationSetupResponse(AuthorizationSetupRequest        Request,
                                          MessageHeader                    MessageHeader,
                                          ResponseCodes                    ResponseCode,
                                          IEnumerable<AuthorizationTypes>  AuthorizationServices,
                                          Boolean                          CertificateInstallationService,
                                          PnC_ASResAuthorizationModeType   PnC_ASResAuthorizationMode)

            : this(Request,
                   MessageHeader,
                   ResponseCode,
                   AuthorizationServices,
                   CertificateInstallationService,
                   null,
                   PnC_ASResAuthorizationMode)

        { }

        #endregion

        #endregion


        #region Documentation

        // <xs:element name = "AuthorizationSetupRes" type="AuthorizationSetupResType"/>
        //
        // <xs:complexType name = "AuthorizationSetupResType" >
        //     < xs:complexContent>
        //         <xs:extension base="v2gci_ct:V2GResponseType">
        //             <xs:sequence>
        //                 <xs:element name = "AuthorizationServices" type="authorizationType" maxOccurs="2"/>
        //                 <xs:element name = "CertificateInstallationService" type="xs:boolean"/>
        //                 <xs:choice>
        //                     <xs:element name = "EIM_ASResAuthorizationMode" type="EIM_ASResAuthorizationModeType"/>
        //                     <xs:element name = "PnC_ASResAuthorizationMode" type="PnC_ASResAuthorizationModeType"/>
        //                 </xs:choice>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (Request, JSON, CustomAuthorizationSetupResponseParser = null)

        /// <summary>
        /// Parse the given JSON representation of an authorization setup response.
        /// </summary>
        /// <param name="Request">The authorization setup request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomAuthorizationSetupResponseParser">A delegate to parse custom authorization setup responses.</param>
        public static AuthorizationSetupResponse Parse(AuthorizationSetupRequest                                 Request,
                                                       JObject                                                   JSON,
                                                       CustomJObjectParserDelegate<AuthorizationSetupResponse>?  CustomAuthorizationSetupResponseParser   = null)
        {

            if (TryParse(Request,
                         JSON,
                         out var authorizationSetupResponse,
                         out var errorResponse,
                         CustomAuthorizationSetupResponseParser))
            {
                return authorizationSetupResponse!;
            }

            throw new ArgumentException("The given JSON representation of an authorization setup response is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(Request, JSON, out AuthorizationSetupResponse, out ErrorResponse, CustomAuthorizationSetupResponseParser = null)

        /// <summary>
        /// Try to parse the given JSON representation of an authorization setup response.
        /// </summary>
        /// <param name="Request">The authorization setup request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="AuthorizationSetupResponse">The parsed authorization setup response.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomAuthorizationSetupResponseParser">A delegate to parse custom authorization setup responses.</param>
        public static Boolean TryParse(AuthorizationSetupRequest                                 Request,
                                       JObject                                                   JSON,
                                       out AuthorizationSetupResponse?                           AuthorizationSetupResponse,
                                       out String?                                               ErrorResponse,
                                       CustomJObjectParserDelegate<AuthorizationSetupResponse>?  CustomAuthorizationSetupResponseParser   = null)
        {

            ErrorResponse = null;

            try
            {

                AuthorizationSetupResponse = null;

                #region MessageHeader                     [mandatory]

                if (!JSON.ParseMandatoryJSON("messageHeader",
                                             "message header",
                                             CommonTypes.MessageHeader.TryParse,
                                             out MessageHeader? MessageHeader,
                                             out ErrorResponse))
                {
                    return false;
                }

                if (MessageHeader is null)
                    return false;

                #endregion

                #region ResponseCode                      [mandatory]

                if (!JSON.ParseMandatory("responseCode",
                                         "response code",
                                         ResponseCodesExtensions.TryParse,
                                         out ResponseCodes ResponseCode,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region AuthorizationServices             [mandatory]

                if (!JSON.ParseMandatoryHashSet("certificateInstallationService",
                                                "certificate installation service",
                                                AuthorizationTypesExtensions.TryParse,
                                                out HashSet<AuthorizationTypes> AuthorizationServices,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region CertificateInstallationService    [mandatory]

                if (!JSON.ParseMandatory("certificateInstallationService",
                                         "certificate installation service",
                                         out Boolean CertificateInstallationService,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region EIM_ASResAuthorizationMode        [optional]

                if (JSON.ParseOptionalJSON("EIM_ASResAuthorizationMode",
                                           "EIM ASRes authorization mode",
                                           EIM_ASResAuthorizationModeType.TryParse,
                                           out EIM_ASResAuthorizationModeType? EIM_ASResAuthorizationMode,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region PnC_ASResAuthorizationMode        [optional]

                if (JSON.ParseOptionalJSON("PnC_AReqAuthorizationMode",
                                           "PnC AReq authorization mode",
                                           PnC_ASResAuthorizationModeType.TryParse,
                                           out PnC_ASResAuthorizationModeType? PnC_ASResAuthorizationMode,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                if (EIM_ASResAuthorizationMode is not null &&
                    PnC_ASResAuthorizationMode is not null)
                {
                    ErrorResponse = "Only wither EIM_ASResAuthorizationMode or PnC_ASResAuthorizationMode is allowed, but not both!";
                    return false;
                }

                AuthorizationSetupResponse = new AuthorizationSetupResponse(Request,
                                                                            MessageHeader,
                                                                            ResponseCode,
                                                                            AuthorizationServices,
                                                                            CertificateInstallationService,
                                                                            EIM_ASResAuthorizationMode,
                                                                            PnC_ASResAuthorizationMode);

                if (CustomAuthorizationSetupResponseParser is not null)
                    AuthorizationSetupResponse = CustomAuthorizationSetupResponseParser(JSON,
                                                                                        AuthorizationSetupResponse);

                return true;

            }
            catch (Exception e)
            {
                AuthorizationSetupResponse  = null;
                ErrorResponse               = "The given JSON representation of an authorization setup response is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomAuthorizationSetupResponseSerializer = null, CustomMessageHeaderSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomAuthorizationSetupResponseSerializer">A delegate to serialize custom authorization setup responses.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<AuthorizationSetupResponse>?  CustomAuthorizationSetupResponseSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?               CustomMessageHeaderSerializer                = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("messageHeader",               MessageHeader.             ToJSON  (CustomMessageHeaderSerializer)),
                                 new JProperty("responseCode",                ResponseCode.              AsText()),

                           EIM_ASResAuthorizationMode is not null
                               ? new JProperty("EIM_ASResAuthorizationMode",  EIM_ASResAuthorizationMode.ToJSON())
                               : null,

                           PnC_ASResAuthorizationMode is not null
                               ? new JProperty("PnC_ASResAuthorizationMode",  PnC_ASResAuthorizationMode.ToJSON())
                               : null

                       );

            return CustomAuthorizationSetupResponseSerializer is not null
                       ? CustomAuthorizationSetupResponseSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (AuthorizationSetupResponse1, AuthorizationSetupResponse2)

        /// <summary>
        /// Compares two authorization setup responses for equality.
        /// </summary>
        /// <param name="AuthorizationSetupResponse1">An authorization setup response.</param>
        /// <param name="AuthorizationSetupResponse2">Another authorization setup response.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (AuthorizationSetupResponse? AuthorizationSetupResponse1,
                                           AuthorizationSetupResponse? AuthorizationSetupResponse2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(AuthorizationSetupResponse1, AuthorizationSetupResponse2))
                return true;

            // If one is null, but not both, return false.
            if (AuthorizationSetupResponse1 is null || AuthorizationSetupResponse2 is null)
                return false;

            return AuthorizationSetupResponse1.Equals(AuthorizationSetupResponse2);

        }

        #endregion

        #region Operator != (AuthorizationSetupResponse1, AuthorizationSetupResponse2)

        /// <summary>
        /// Compares two authorization setup responses for inequality.
        /// </summary>
        /// <param name="AuthorizationSetupResponse1">An authorization setup response.</param>
        /// <param name="AuthorizationSetupResponse2">Another authorization setup response.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (AuthorizationSetupResponse? AuthorizationSetupResponse1,
                                           AuthorizationSetupResponse? AuthorizationSetupResponse2)

            => !(AuthorizationSetupResponse1 == AuthorizationSetupResponse2);

        #endregion

        #endregion

        #region IEquatable<AuthorizationSetupResponse> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two authorization setup responses for equality.
        /// </summary>
        /// <param name="Object">An authorization setup response to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is AuthorizationSetupResponse authorizationSetupResponse &&
                   Equals(authorizationSetupResponse);

        #endregion

        #region Equals(AuthorizationSetupResponse)

        /// <summary>
        /// Compares two authorization setup responses for equality.
        /// </summary>
        /// <param name="AuthorizationSetupResponse">An authorization setup response to compare with.</param>
        public override Boolean Equals(AuthorizationSetupResponse? AuthorizationSetupResponse)

            => AuthorizationSetupResponse is not null &&

               CertificateInstallationService.Equals(AuthorizationSetupResponse.CertificateInstallationService) &&

             ((EIM_ASResAuthorizationMode is     null && AuthorizationSetupResponse.EIM_ASResAuthorizationMode is     null) ||
              (EIM_ASResAuthorizationMode is not null && AuthorizationSetupResponse.EIM_ASResAuthorizationMode is not null && EIM_ASResAuthorizationMode.Equals(AuthorizationSetupResponse.EIM_ASResAuthorizationMode))) &&

             ((PnC_ASResAuthorizationMode is     null && AuthorizationSetupResponse.PnC_ASResAuthorizationMode is     null) ||
              (PnC_ASResAuthorizationMode is not null && AuthorizationSetupResponse.PnC_ASResAuthorizationMode is not null && PnC_ASResAuthorizationMode.Equals(AuthorizationSetupResponse.PnC_ASResAuthorizationMode))) &&

               base.GenericEquals(AuthorizationSetupResponse);

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the HashCode of this object.
        /// </summary>
        /// <returns>The HashCode of this object.</returns>
        public override Int32 GetHashCode()
        {
            unchecked
            {

                return CertificateInstallationService.GetHashCode()       * 7 ^
                      (EIM_ASResAuthorizationMode?.   GetHashCode() ?? 0) * 5 ^
                      (PnC_ASResAuthorizationMode?.   GetHashCode() ?? 0) * 3 ^
                       base.                          GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   CertificateInstallationService
                       ? "yes"
                       : "no",

                   ": ",

                   EIM_ASResAuthorizationMode is not null
                       ? EIM_ASResAuthorizationMode.ToString()
                       : "",

                   PnC_ASResAuthorizationMode is not null
                       ? PnC_ASResAuthorizationMode.ToString()
                       : ""

               );

        #endregion

    }

}
