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
    /// The session stop response.
    /// </summary>
    public class SessionStopResponse : AResponse<SessionStopRequest,
                                                 SessionStopResponse>
    {

        #region Constructor(s)

        /// <summary>
        /// Create a new session stop response.
        /// </summary>
        /// <param name="Request">The session stop request leading to this response.</param>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        public SessionStopResponse(SessionStopRequest  Request,
                                   MessageHeader       MessageHeader,
                                   ResponseCodes       ResponseCode)

            : base(Request,
                   MessageHeader,
                   ResponseCode)

        { }

        #endregion


        #region Documentation

        // <xs:element name="SessionStopRes" type="SessionStopResType"/>
        //
        // <xs:complexType name="SessionStopResType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:V2GResponseType"/>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (Request, JSON, CustomSessionStopResponseParser = null)

        /// <summary>
        /// Parse the given JSON representation of a session stop response.
        /// </summary>
        /// <param name="Request">The session stop request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomSessionStopResponseParser">An optional delegate to parse custom session stop responses.</param>
        public static SessionStopResponse Parse(SessionStopRequest                                 Request,
                                                JObject                                            JSON,
                                                CustomJObjectParserDelegate<SessionStopResponse>?  CustomSessionStopResponseParser   = null)
        {

            if (TryParse(Request,
                         JSON,
                         out var sessionStopResponse,
                         out var errorResponse,
                         CustomSessionStopResponseParser))
            {
                return sessionStopResponse!;
            }

            throw new ArgumentException("The given JSON representation of a session stop response is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(Request, JSON, out SessionStopResponse, out ErrorResponse, CustomSessionStopResponseParser = null)

        /// <summary>
        /// Try to parse the given JSON representation of a session stop response.
        /// </summary>
        /// <param name="Request">The session stop request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="SessionStopResponse">The parsed session stop response.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomSessionStopResponseParser">An optional delegate to parse custom session stop responses.</param>
        public static Boolean TryParse(SessionStopRequest                                 Request,
                                       JObject                                            JSON,
                                       out SessionStopResponse?                           SessionStopResponse,
                                       out String?                                        ErrorResponse,
                                       CustomJObjectParserDelegate<SessionStopResponse>?  CustomSessionStopResponseParser   = null)
        {

            ErrorResponse = null;

            try
            {

                SessionStopResponse = null;

                #region MessageHeader        [mandatory]

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

                #region ResponseCode         [mandatory]

                if (!JSON.ParseMandatory("responseCode",
                                         "response code",
                                         ResponseCodesExtensions.TryParse,
                                         out ResponseCodes ResponseCode,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion


                SessionStopResponse = new SessionStopResponse(Request,
                                                              MessageHeader,
                                                              ResponseCode);

                if (CustomSessionStopResponseParser is not null)
                    SessionStopResponse = CustomSessionStopResponseParser(JSON,
                                                                          SessionStopResponse);

                return true;

            }
            catch (Exception e)
            {
                SessionStopResponse  = null;
                ErrorResponse        = "The given JSON representation of a session stop response is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomSessionStopResponseSerializer = null, CustomMessageHeaderSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomSessionStopResponseSerializer">A delegate to serialize custom session stop responses.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<SessionStopResponse>?  CustomSessionStopResponseSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?        CustomMessageHeaderSerializer         = null)
        {

            var json = JSONObject.Create(

                           new JProperty("messageHeader",  MessageHeader.ToJSON(CustomMessageHeaderSerializer)),
                           new JProperty("responseCode",   ResponseCode. AsText())

                       );

            return CustomSessionStopResponseSerializer is not null
                       ? CustomSessionStopResponseSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (SessionStopResponse1, SessionStopResponse2)

        /// <summary>
        /// Compares two session stop responses for equality.
        /// </summary>
        /// <param name="SessionStopResponse1">A session stop response.</param>
        /// <param name="SessionStopResponse2">Another session stop response.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (SessionStopResponse? SessionStopResponse1,
                                           SessionStopResponse? SessionStopResponse2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(SessionStopResponse1, SessionStopResponse2))
                return true;

            // If one is null, but not both, return false.
            if (SessionStopResponse1 is null || SessionStopResponse2 is null)
                return false;

            return SessionStopResponse1.Equals(SessionStopResponse2);

        }

        #endregion

        #region Operator != (SessionStopResponse1, SessionStopResponse2)

        /// <summary>
        /// Compares two session stop responses for inequality.
        /// </summary>
        /// <param name="SessionStopResponse1">A session stop response.</param>
        /// <param name="SessionStopResponse2">Another session stop response.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (SessionStopResponse? SessionStopResponse1,
                                           SessionStopResponse? SessionStopResponse2)

            => !(SessionStopResponse1 == SessionStopResponse2);

        #endregion

        #endregion

        #region IEquatable<SessionStopResponse> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two session stop responses for equality.
        /// </summary>
        /// <param name="Object">A session stop response to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is SessionStopResponse sessionStopResponse &&
                   Equals(sessionStopResponse);

        #endregion

        #region Equals(SessionStopResponse)

        /// <summary>
        /// Compares two session stop responses for equality.
        /// </summary>
        /// <param name="SessionStopResponse">A session stop response to compare with.</param>
        public override Boolean Equals(SessionStopResponse? SessionStopResponse)

            => SessionStopResponse is not null &&

               base.GenericEquals(SessionStopResponse);

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the HashCode of this object.
        /// </summary>
        /// <returns>The HashCode of this object.</returns>
        public override Int32 GetHashCode()

            => base.GetHashCode();

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => nameof(SessionStopResponse);

        #endregion

    }

}
