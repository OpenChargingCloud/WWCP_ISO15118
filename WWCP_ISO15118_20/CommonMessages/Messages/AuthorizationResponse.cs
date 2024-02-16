/*
 * Copyright (c) 2021-2024 GraphDefined GmbH <achim.friedland@graphdefined.com>
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
    /// The authorization response.
    /// </summary>
    public class AuthorizationResponse : AResponse<AuthorizationRequest,
                                                   AuthorizationResponse>
    {

        #region Properties

        /// <summary>
        /// The EVSE processing type.
        /// </summary>
        public ProcessingTypes  EVSEProcessing    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new authorization response.
        /// </summary>
        /// <param name="Request">The authorization setup request leading to this response.</param>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        /// <param name="EVSEProcessing">An EVSE processing type.</param>
        public AuthorizationResponse(AuthorizationRequest  Request,
                                     MessageHeader         MessageHeader,
                                     ResponseCodes         ResponseCode,
                                     ProcessingTypes       EVSEProcessing)

            : base(Request,
                   MessageHeader,
                   ResponseCode)

        {

            this.EVSEProcessing = EVSEProcessing;

        }

        #endregion


        #region Documentation

        // <xs:element name = "AuthorizationRes" type="AuthorizationResType"/>
        //
        // <xs:complexType name = "AuthorizationResType" >
        //     < xs:complexContent>
        //         <xs:extension base="v2gci_ct:V2GResponseType">
        //             <xs:sequence>
        //                 <xs:element name = "EVSEProcessing" type="v2gci_ct:processingType"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (Request, JSON, CustomAuthorizationResponseParser = null)

        /// <summary>
        /// Parse the given JSON representation of an authorization response.
        /// </summary>
        /// <param name="Request">The authorization request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomAuthorizationResponseParser">An optional delegate to parse custom authorization responses.</param>
        public static AuthorizationResponse Parse(AuthorizationRequest                                 Request,
                                                  JObject                                              JSON,
                                                  CustomJObjectParserDelegate<AuthorizationResponse>?  CustomAuthorizationResponseParser   = null)
        {

            if (TryParse(Request,
                         JSON,
                         out var authorizationResponse,
                         out var errorResponse,
                         CustomAuthorizationResponseParser))
            {
                return authorizationResponse!;
            }

            throw new ArgumentException("The given JSON representation of an authorization response is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(Request, JSON, out AuthorizationResponse, out ErrorResponse, CustomAuthorizationResponseParser = null)

        /// <summary>
        /// Try to parse the given JSON representation of an authorization response.
        /// </summary>
        /// <param name="Request">The authorization request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="AuthorizationResponse">The parsed authorization response.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomAuthorizationResponseParser">An optional delegate to parse custom authorization responses.</param>
        public static Boolean TryParse(AuthorizationRequest                                 Request,
                                       JObject                                              JSON,
                                       out AuthorizationResponse?                           AuthorizationResponse,
                                       out String?                                          ErrorResponse,
                                       CustomJObjectParserDelegate<AuthorizationResponse>?  CustomAuthorizationResponseParser   = null)
        {

            ErrorResponse = null;

            try
            {

                AuthorizationResponse = null;

                #region MessageHeader     [mandatory]

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

                #region ResponseCode      [mandatory]

                if (!JSON.ParseMandatory("responseCode",
                                         "response code",
                                         ResponseCodesExtensions.TryParse,
                                         out ResponseCodes ResponseCode,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region EVSEProcessing    [mandatory]

                if (!JSON.ParseMandatory("evseProcessing",
                                         "EVSE processing",
                                         ProcessingTypesExtensions.TryParse,
                                         out ProcessingTypes EVSEProcessing,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion


                AuthorizationResponse = new AuthorizationResponse(Request,
                                                                  MessageHeader,
                                                                  ResponseCode,
                                                                  EVSEProcessing);

                if (CustomAuthorizationResponseParser is not null)
                    AuthorizationResponse = CustomAuthorizationResponseParser(JSON,
                                                                              AuthorizationResponse);

                return true;

            }
            catch (Exception e)
            {
                AuthorizationResponse  = null;
                ErrorResponse          = "The given JSON representation of an authorization response is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomAuthorizationResponseSerializer = null, CustomMessageHeaderSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomAuthorizationResponseSerializer">A delegate to serialize custom authorization responses.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<AuthorizationResponse>?  CustomAuthorizationResponseSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?          CustomMessageHeaderSerializer           = null)
        {

            var json = JSONObject.Create(

                           new JProperty("messageHeader",   MessageHeader. ToJSON(CustomMessageHeaderSerializer)),
                           new JProperty("responseCode",    ResponseCode.  AsText()),

                           new JProperty("evseProcessing",  EVSEProcessing.AsText())

                       );

            return CustomAuthorizationResponseSerializer is not null
                       ? CustomAuthorizationResponseSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (AuthorizationResponse1, AuthorizationResponse2)

        /// <summary>
        /// Compares two authorization responses for equality.
        /// </summary>
        /// <param name="AuthorizationResponse1">An authorization response.</param>
        /// <param name="AuthorizationResponse2">Another authorization response.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (AuthorizationResponse? AuthorizationResponse1,
                                           AuthorizationResponse? AuthorizationResponse2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(AuthorizationResponse1, AuthorizationResponse2))
                return true;

            // If one is null, but not both, return false.
            if (AuthorizationResponse1 is null || AuthorizationResponse2 is null)
                return false;

            return AuthorizationResponse1.Equals(AuthorizationResponse2);

        }

        #endregion

        #region Operator != (AuthorizationResponse1, AuthorizationResponse2)

        /// <summary>
        /// Compares two authorization responses for inequality.
        /// </summary>
        /// <param name="AuthorizationResponse1">An authorization response.</param>
        /// <param name="AuthorizationResponse2">Another authorization response.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (AuthorizationResponse? AuthorizationResponse1,
                                           AuthorizationResponse? AuthorizationResponse2)

            => !(AuthorizationResponse1 == AuthorizationResponse2);

        #endregion

        #endregion

        #region IEquatable<AuthorizationResponse> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two authorization responses for equality.
        /// </summary>
        /// <param name="Object">An authorization response to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is AuthorizationResponse authorizationResponse &&
                   Equals(authorizationResponse);

        #endregion

        #region Equals(AuthorizationResponse)

        /// <summary>
        /// Compares two authorization responses for equality.
        /// </summary>
        /// <param name="AuthorizationResponse">An authorization response to compare with.</param>
        public override Boolean Equals(AuthorizationResponse? AuthorizationResponse)

            => AuthorizationResponse is not null &&

               EVSEProcessing.Equals(AuthorizationResponse.EVSEProcessing) &&

               base.GenericEquals(AuthorizationResponse);

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

                return EVSEProcessing.GetHashCode() * 3 ^
                       base.          GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => EVSEProcessing.AsText();

        #endregion

    }

}
