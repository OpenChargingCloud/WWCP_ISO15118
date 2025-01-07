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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

using cloud.charging.open.protocols.ISO15118_20.CommonTypes;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// The external identification means authorization setup response.
    /// </summary>
    public class EIM_AuthorizationSetupResponse : AuthorizationSetupResponse,
                                                  IEquatable<EIM_AuthorizationSetupResponse>
    {

        #region Constructor(s)

        /// <summary>
        /// Create a new external identification means authorization response.
        /// </summary>
        /// <param name="Request">The authorization setup request leading to this response.</param>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        /// <param name="CertificateInstallationService">Whether this charging station will provide a certificate installation service.</param>
        public EIM_AuthorizationSetupResponse(AuthorizationSetupRequest  Request,
                                              MessageHeader              MessageHeader,
                                              ResponseCodes              ResponseCode,
                                              Boolean                    CertificateInstallationService)

            : base(Request,
                   MessageHeader,
                   ResponseCode,
                   new AuthorizationTypes[] { AuthorizationTypes.EIM },
                   CertificateInstallationService)

        { }

        #endregion


        #region Documentation

        // <xs:complexType name="EIM_ASResAuthorizationModeType"/>

        #endregion

        #region (static) Parse   (Request, JSON, CustomEIM_AuthorizationSetupResponseParser = null)

        /// <summary>
        /// Parse the given JSON representation of an external identification means authorization setup response.
        /// </summary>
        /// <param name="Request">The authorization setup request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomEIM_AuthorizationSetupResponseParser">An optional delegate to parse custom external identification means authorization setup responses.</param>
        public static EIM_AuthorizationSetupResponse Parse(AuthorizationSetupRequest                                     Request,
                                                           JObject                                                       JSON,
                                                           CustomJObjectParserDelegate<EIM_AuthorizationSetupResponse>?  CustomEIM_AuthorizationSetupResponseParser   = null)
        {

            if (TryParse(Request,
                         JSON,
                         out var eimAuthorizationSetupResponse,
                         out var errorResponse,
                         CustomEIM_AuthorizationSetupResponseParser))
            {
                return eimAuthorizationSetupResponse!;
            }

            throw new ArgumentException("The given JSON representation of an external identification means authorization setup response is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(Request, JSON, out AuthorizationSetupResponse, out ErrorResponse, CustomEIM_AuthorizationSetupResponseParser = null)

        /// <summary>
        /// Try to parse the given JSON representation of an external identification means authorization setup response.
        /// </summary>
        /// <param name="Request">The authorization setup request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="AuthorizationSetupResponse">The parsed external identification means authorization setup response.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomEIM_AuthorizationSetupResponseParser">An optional delegate to parse custom external identification means authorization setup responses.</param>
        public static Boolean TryParse(AuthorizationSetupRequest                                     Request,
                                       JObject                                                       JSON,
                                       out EIM_AuthorizationSetupResponse?                           AuthorizationSetupResponse,
                                       out String?                                                   ErrorResponse,
                                       CustomJObjectParserDelegate<EIM_AuthorizationSetupResponse>?  CustomEIM_AuthorizationSetupResponseParser   = null)
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

                if (!JSON.ParseMandatoryHashSet("authorizationServices",
                                                "authorization services",
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


                if (!AuthorizationServices.Contains(AuthorizationTypes.EIM))
                {
                    ErrorResponse = "The enumeration of authorization services does not contain 'external identification means'!";
                    return false;
                }


                AuthorizationSetupResponse = new EIM_AuthorizationSetupResponse(Request,
                                                                                MessageHeader,
                                                                                ResponseCode,
                                                                                CertificateInstallationService);

                if (CustomEIM_AuthorizationSetupResponseParser is not null)
                    AuthorizationSetupResponse = CustomEIM_AuthorizationSetupResponseParser(JSON,
                                                                                            AuthorizationSetupResponse);

                return true;

            }
            catch (Exception e)
            {
                AuthorizationSetupResponse  = null;
                ErrorResponse               = "The given JSON representation of an external identification means authorization setup response is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomEIM_AuthorizationSetupResponseSerializer = null, CustomMessageHeaderSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomEIM_AuthorizationSetupResponseSerializer">A delegate to serialize custom external identification means authorization setup responses.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<EIM_AuthorizationSetupResponse>?  CustomEIM_AuthorizationSetupResponseSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?                   CustomMessageHeaderSerializer                    = null)
        {

            var json = JSONObject.Create(

                           new JProperty("messageHeader",                   MessageHeader.ToJSON(CustomMessageHeaderSerializer)),
                           new JProperty("responseCode",                    ResponseCode. AsText()),
                           new JProperty("certificateInstallationService",  CertificateInstallationService)

                       );


            return CustomEIM_AuthorizationSetupResponseSerializer is not null
                       ? CustomEIM_AuthorizationSetupResponseSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (AuthorizationSetupResponse1, AuthorizationSetupResponse2)

        /// <summary>
        /// Compares two external identification means authorization setup responses for equality.
        /// </summary>
        /// <param name="AuthorizationSetupResponse1">An external identification means authorization setup response.</param>
        /// <param name="AuthorizationSetupResponse2">Another external identification means authorization setup response.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EIM_AuthorizationSetupResponse? AuthorizationSetupResponse1,
                                           EIM_AuthorizationSetupResponse? AuthorizationSetupResponse2)
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
        /// Compares two external identification means authorization setup responses for inequality.
        /// </summary>
        /// <param name="AuthorizationSetupResponse1">An external identification means authorization setup response.</param>
        /// <param name="AuthorizationSetupResponse2">Another external identification means authorization setup response.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EIM_AuthorizationSetupResponse? AuthorizationSetupResponse1,
                                           EIM_AuthorizationSetupResponse? AuthorizationSetupResponse2)

            => !(AuthorizationSetupResponse1 == AuthorizationSetupResponse2);

        #endregion

        #endregion

        #region IEquatable<AuthorizationSetupResponse> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two external identification means authorization setup responses for equality.
        /// </summary>
        /// <param name="Object">An external identification means authorization setup response to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EIM_AuthorizationSetupResponse eimAuthorizationSetupResponse &&
                   Equals(eimAuthorizationSetupResponse);

        #endregion

        #region Equals(AuthorizationSetupResponse)

        /// <summary>
        /// Compares two external identification means authorization setup responses for equality.
        /// </summary>
        /// <param name="AuthorizationSetupResponse">An external identification means authorization setup response to compare with.</param>
        public Boolean Equals(EIM_AuthorizationSetupResponse? AuthorizationSetupResponse)

            => AuthorizationSetupResponse is not null &&

               CertificateInstallationService.Equals(AuthorizationSetupResponse.CertificateInstallationService) &&

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

                return CertificateInstallationService.GetHashCode() * 3 ^
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
                       : "no"

               );

        #endregion

    }

}
