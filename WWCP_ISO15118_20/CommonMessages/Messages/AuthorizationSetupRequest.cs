/*
 * Copyright (c) 2021-2023 GraphDefined GmbH
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
    /// The authorization setup request.
    /// </summary>
    public class AuthorizationSetupRequest : ARequest<AuthorizationSetupRequest>
    {

        #region Constructor(s)

        /// <summary>
        /// Create a new authorization setup request.
        /// </summary>
        /// <param name="Header">A message header.</param>
        public AuthorizationSetupRequest(MessageHeader  Header)

            : base(Header)

        { }

        #endregion


        #region Documentation

        // <xs:element name = "AuthorizationSetupReq" type="AuthorizationSetupReqType"/>
        //
        // <xs:complexType name = "AuthorizationSetupReqType" >
        //     < xs:complexContent>
        //         <xs:extension base="v2gci_ct:V2GRequestType"/>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomAuthorizationSetupRequestParser = null)

        /// <summary>
        /// Parse the given JSON representation of an authorization setup request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomAuthorizationSetupRequestParser">A delegate to parse custom authorization setup requests.</param>
        public static AuthorizationSetupRequest Parse(JObject                                                  JSON,
                                                      CustomJObjectParserDelegate<AuthorizationSetupRequest>?  CustomAuthorizationSetupRequestParser   = null)
        {

            if (TryParse(JSON,
                         out var authorizationSetupRequest,
                         out var errorResponse,
                         CustomAuthorizationSetupRequestParser))
            {
                return authorizationSetupRequest!;
            }

            throw new ArgumentException("The given JSON representation of an authorization setup request is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out AuthorizationSetupRequest, out ErrorResponse, CustomAuthorizationSetupRequestParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of an authorization setup request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="AuthorizationSetupRequest">The parsed authorization setup request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                         JSON,
                                       out AuthorizationSetupRequest?  AuthorizationSetupRequest,
                                       out String?                     ErrorResponse)

            => TryParse(JSON,
                        out AuthorizationSetupRequest,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an authorization setup request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="AuthorizationSetupRequest">The parsed authorization setup request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomAuthorizationSetupRequestParser">A delegate to parse custom authorization setup requests.</param>
        public static Boolean TryParse(JObject                                                  JSON,
                                       out AuthorizationSetupRequest?                           AuthorizationSetupRequest,
                                       out String?                                              ErrorResponse,
                                       CustomJObjectParserDelegate<AuthorizationSetupRequest>?  CustomAuthorizationSetupRequestParser)
        {

            try
            {

                AuthorizationSetupRequest = null;

                #region MessageHeader    [mandatory]

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


                AuthorizationSetupRequest = new AuthorizationSetupRequest(MessageHeader);

                if (CustomAuthorizationSetupRequestParser is not null)
                    AuthorizationSetupRequest = CustomAuthorizationSetupRequestParser(JSON,
                                                                                      AuthorizationSetupRequest);

                return true;

            }
            catch (Exception e)
            {
                AuthorizationSetupRequest  = null;
                ErrorResponse              = "The given JSON representation of an authorization setup request is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomAuthorizationSetupRequestSerializer = null, CustomMessageHeaderSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomAuthorizationSetupRequestSerializer">A delegate to serialize custom authorization setup requests.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<AuthorizationSetupRequest>?  CustomAuthorizationSetupRequestSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?              CustomMessageHeaderSerializer               = null)
        {

            var json = JSONObject.Create(

                           new JProperty("messageHeader",  MessageHeader.ToJSON(CustomMessageHeaderSerializer))

                       );

            return CustomAuthorizationSetupRequestSerializer is not null
                       ? CustomAuthorizationSetupRequestSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (AuthorizationSetupRequest1, AuthorizationSetupRequest2)

        /// <summary>
        /// Compares two authorization setup requests for equality.
        /// </summary>
        /// <param name="AuthorizationSetupRequest1">An authorization setup request.</param>
        /// <param name="AuthorizationSetupRequest2">Another authorization setup request.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (AuthorizationSetupRequest? AuthorizationSetupRequest1,
                                           AuthorizationSetupRequest? AuthorizationSetupRequest2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(AuthorizationSetupRequest1, AuthorizationSetupRequest2))
                return true;

            // If one is null, but not both, return false.
            if (AuthorizationSetupRequest1 is null || AuthorizationSetupRequest2 is null)
                return false;

            return AuthorizationSetupRequest1.Equals(AuthorizationSetupRequest2);

        }

        #endregion

        #region Operator != (AuthorizationSetupRequest1, AuthorizationSetupRequest2)

        /// <summary>
        /// Compares two authorization setup requests for inequality.
        /// </summary>
        /// <param name="AuthorizationSetupRequest1">An authorization setup request.</param>
        /// <param name="AuthorizationSetupRequest2">Another authorization setup request.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (AuthorizationSetupRequest? AuthorizationSetupRequest1,
                                           AuthorizationSetupRequest? AuthorizationSetupRequest2)

            => !(AuthorizationSetupRequest1 == AuthorizationSetupRequest2);

        #endregion

        #endregion

        #region IEquatable<AuthorizationSetupRequest> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two authorization setup requests for equality.
        /// </summary>
        /// <param name="Object">An authorization setup request to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is AuthorizationSetupRequest authorizationSetupRequest &&
                   Equals(authorizationSetupRequest);

        #endregion

        #region Equals(AuthorizationSetupRequest)

        /// <summary>
        /// Compares two authorization setup requests for equality.
        /// </summary>
        /// <param name="AuthorizationSetupRequest">An authorization setup request to compare with.</param>
        public override Boolean Equals(AuthorizationSetupRequest? AuthorizationSetupRequest)

            => AuthorizationSetupRequest is not null &&

               base.Equals(AuthorizationSetupRequest);

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

                return base.GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => nameof(AuthorizationSetupRequest);

        #endregion

    }

}
