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
    /// The session setup response.
    /// </summary>
    public class SessionSetupResponse : AResponse<SessionSetupRequest,
                                                  SessionSetupResponse>
    {

        #region Properties

        /// <summary>
        /// The EVSE identification.
        /// </summary>
        [Mandatory]
        public EVSE_Id  EVSEId    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new session setup response.
        /// </summary>
        /// <param name="Request">The session setup request leading to this response.</param>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        /// <param name="EVSEId">An EVSE identification.</param>
        public SessionSetupResponse(SessionSetupRequest  Request,
                                    MessageHeader        MessageHeader,
                                    ResponseCodes        ResponseCode,
                                    EVSE_Id              EVSEId)

            : base(Request,
                   MessageHeader,
                   ResponseCode)

        {

            this.EVSEId = EVSEId;

        }

        #endregion


        #region Documentation

        // <xs:element name = "SessionSetupRes" type="SessionSetupResType"/>
        //
        // <xs:complexType name = "SessionSetupResType" >
        //     < xs:complexContent>
        //         <xs:extension base="v2gci_ct:V2GResponseType">
        //             <xs:sequence>
        //                 <xs:element name = "EVSEID" type="v2gci_ct:identifierType"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (Request, JSON, CustomSessionSetupResponseParser = null)

        /// <summary>
        /// Parse the given JSON representation of a session setup response.
        /// </summary>
        /// <param name="Request">The session setup request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomSessionSetupResponseParser">An optional delegate to parse custom session setup responses.</param>
        public static SessionSetupResponse Parse(SessionSetupRequest                                 Request,
                                                 JObject                                             JSON,
                                                 CustomJObjectParserDelegate<SessionSetupResponse>?  CustomSessionSetupResponseParser   = null)
        {

            if (TryParse(Request,
                         JSON,
                         out var notifyDisplayMessagesResponse,
                         out var errorResponse,
                         CustomSessionSetupResponseParser))
            {
                return notifyDisplayMessagesResponse!;
            }

            throw new ArgumentException("The given JSON representation of a session setup response is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(Request, JSON, out SessionSetupResponse, out ErrorResponse, CustomSessionSetupResponseParser = null)

        /// <summary>
        /// Try to parse the given JSON representation of a session setup response.
        /// </summary>
        /// <param name="Request">The session setup request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="SessionSetupResponse">The parsed session setup response.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomSessionSetupResponseParser">An optional delegate to parse custom session setup responses.</param>
        public static Boolean TryParse(SessionSetupRequest                                 Request,
                                       JObject                                             JSON,
                                       out SessionSetupResponse?                           SessionSetupResponse,
                                       out String?                                         ErrorResponse,
                                       CustomJObjectParserDelegate<SessionSetupResponse>?  CustomSessionSetupResponseParser   = null)
        {

            ErrorResponse = null;

            try
            {

                SessionSetupResponse = null;

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

                #region ResponseCode     [mandatory]

                if (!JSON.ParseMandatory("responseCode",
                                         "response code",
                                         ResponseCodesExtensions.TryParse,
                                         out ResponseCodes ResponseCode,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region EVSEId           [mandatory]

                if (!JSON.ParseMandatory("evseId",
                                         "EVSE identification",
                                         EVSE_Id.TryParse,
                                         out EVSE_Id EVSEId,
                                         out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                SessionSetupResponse = new SessionSetupResponse(Request,
                                                                MessageHeader,
                                                                ResponseCode,
                                                                EVSEId);

                if (CustomSessionSetupResponseParser is not null)
                    SessionSetupResponse = CustomSessionSetupResponseParser(JSON,
                                                                            SessionSetupResponse);

                return true;

            }
            catch (Exception e)
            {
                SessionSetupResponse  = null;
                ErrorResponse         = "The given JSON representation of a session setup response is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomSessionSetupResponseSerializer = null, CustomMessageHeaderSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomSessionSetupResponseSerializer">A delegate to serialize custom session setup responses.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<SessionSetupResponse>?  CustomSessionSetupResponseSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?         CustomMessageHeaderSerializer          = null)
        {

            var json = JSONObject.Create(

                           new JProperty("messageHeader",  MessageHeader.ToJSON  (CustomMessageHeaderSerializer)),
                           new JProperty("responseCode",   ResponseCode. AsText  ()),
                           new JProperty("evseId",         EVSEId.       ToString())

                       );

            return CustomSessionSetupResponseSerializer is not null
                       ? CustomSessionSetupResponseSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (SessionSetupResponse1, SessionSetupResponse2)

        /// <summary>
        /// Compares two session setup responses for equality.
        /// </summary>
        /// <param name="SessionSetupResponse1">A session setup response.</param>
        /// <param name="SessionSetupResponse2">Another session setup response.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (SessionSetupResponse? SessionSetupResponse1,
                                           SessionSetupResponse? SessionSetupResponse2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(SessionSetupResponse1, SessionSetupResponse2))
                return true;

            // If one is null, but not both, return false.
            if (SessionSetupResponse1 is null || SessionSetupResponse2 is null)
                return false;

            return SessionSetupResponse1.Equals(SessionSetupResponse2);

        }

        #endregion

        #region Operator != (SessionSetupResponse1, SessionSetupResponse2)

        /// <summary>
        /// Compares two session setup responses for inequality.
        /// </summary>
        /// <param name="SessionSetupResponse1">A session setup response.</param>
        /// <param name="SessionSetupResponse2">Another session setup response.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (SessionSetupResponse? SessionSetupResponse1,
                                           SessionSetupResponse? SessionSetupResponse2)

            => !(SessionSetupResponse1 == SessionSetupResponse2);

        #endregion

        #endregion

        #region IEquatable<SessionSetupResponse> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two session setup responses for equality.
        /// </summary>
        /// <param name="Object">A session setup response to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is SessionSetupResponse sessionSetupResponse &&
                   Equals(sessionSetupResponse);

        #endregion

        #region Equals(SessionSetupResponse)

        /// <summary>
        /// Compares two session setup responses for equality.
        /// </summary>
        /// <param name="SessionSetupResponse">A session setup response to compare with.</param>
        public override Boolean Equals(SessionSetupResponse? SessionSetupResponse)

            => SessionSetupResponse is not null &&

               EVSEId.     Equals(SessionSetupResponse.EVSEId) &&

               base.GenericEquals(SessionSetupResponse);

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the HashCode of this object.
        /// </summary>
        public override Int32 GetHashCode()
        {
            unchecked
            {

                return EVSEId.GetHashCode() * 3 ^
                       base.  GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => EVSEId.ToString();

        #endregion

    }

}
