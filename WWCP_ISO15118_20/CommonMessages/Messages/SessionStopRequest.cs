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
    /// The session stop request.
    /// </summary>
    public class SessionStopRequest : ARequest<SessionStopRequest>
    {

        #region Properties

        /// <summary>
        /// The charging session type.
        /// </summary>
        [Mandatory]
        public ChargingSessionTypes  ChargingSession             { get; }

        /// <summary>
        /// The optional EV termination code.
        /// </summary>
        [Optional]
        public Name?                 EVTerminationCode           { get; }

        /// <summary>
        /// The optional EV termination explanation.
        /// </summary>
        [Optional]
        public Description?          EVTerminationExplanation    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new session stop request.
        /// </summary>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="ChargingSession">A charging session type.</param>
        /// <param name="EVTerminationCode">An optional EV termination code.</param>
        /// <param name="EVTerminationExplanation">An optional EV termination explanation.</param>
        public SessionStopRequest(MessageHeader         MessageHeader,
                                  ChargingSessionTypes  ChargingSession,
                                  Name?                 EVTerminationCode          = null,
                                  Description?          EVTerminationExplanation   = null)

            : base(MessageHeader)

        {

            this.ChargingSession           = ChargingSession;
            this.EVTerminationCode         = EVTerminationCode;
            this.EVTerminationExplanation  = EVTerminationExplanation;

        }

        #endregion


        #region Documentation

        // <xs:element name="SessionStopReq" type="SessionStopReqType"/>
        //
        // <xs:complexType name="SessionStopReqType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:V2GRequestType">
        //             <xs:sequence>
        //                 <xs:element name="ChargingSession"          type="chargingSessionType"/>
        //                 <xs:element name="EVTerminationCode"        type="v2gci_ct:nameType"        minOccurs="0"/>
        //                 <xs:element name="EVTerminationExplanation" type="v2gci_ct:descriptionType" minOccurs="0"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomSessionStopRequestParser = null)

        /// <summary>
        /// Parse the given JSON representation of a session stop request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomSessionStopRequestParser">An optional delegate to parse custom session stop requests.</param>
        public static SessionStopRequest Parse(JObject                                           JSON,
                                               CustomJObjectParserDelegate<SessionStopRequest>?  CustomSessionStopRequestParser   = null)
        {

            if (TryParse(JSON,
                         out var sessionStopRequest,
                         out var errorResponse,
                         CustomSessionStopRequestParser))
            {
                return sessionStopRequest!;
            }

            throw new ArgumentException("The given JSON representation of a session stop request is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out SessionStopRequest, out ErrorResponse, CustomSessionStopRequestParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a session stop request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="SessionStopRequest">The parsed session stop request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                  JSON,
                                       out SessionStopRequest?  SessionStopRequest,
                                       out String?              ErrorResponse)

            => TryParse(JSON,
                        out SessionStopRequest,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a session stop request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="SessionStopRequest">The parsed session stop request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomSessionStopRequestParser">An optional delegate to parse custom session stop requests.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       out SessionStopRequest?                           SessionStopRequest,
                                       out String?                                       ErrorResponse,
                                       CustomJObjectParserDelegate<SessionStopRequest>?  CustomSessionStopRequestParser)
        {

            try
            {

                SessionStopRequest = null;

                #region MessageHeader               [mandatory]

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

                #region ChargingSession             [mandatory]

                if (!JSON.ParseMandatory("chargingSession",
                                         "charging session",
                                         ChargingSessionTypesExtensions.TryParse,
                                         out ChargingSessionTypes ChargingSession,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region EVTerminationCode           [optional]

                if (JSON.ParseOptional("evTerminationCode",
                                       "EV termination code",
                                       Name.TryParse,
                                       out Name EVTerminationCode,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region EVTerminationExplanation    [optional]

                if (JSON.ParseOptional("evTerminationExplanation",
                                       "EV termination explanation",
                                       Description.TryParse,
                                       out Description EVTerminationExplanation,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                SessionStopRequest = new SessionStopRequest(MessageHeader,
                                                            ChargingSession,
                                                            EVTerminationCode,
                                                            EVTerminationExplanation);

                if (CustomSessionStopRequestParser is not null)
                    SessionStopRequest = CustomSessionStopRequestParser(JSON,
                                                                        SessionStopRequest);

                return true;

            }
            catch (Exception e)
            {
                SessionStopRequest  = null;
                ErrorResponse       = "The given JSON representation of a session stop request is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomSessionStopRequestSerializer = null, CustomMessageHeaderSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomSessionStopRequestSerializer">A delegate to serialize custom session stop requests.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<SessionStopRequest>?  CustomSessionStopRequestSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?       CustomMessageHeaderSerializer        = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("messageHeader",             MessageHeader.                 ToJSON(CustomMessageHeaderSerializer)),
                                 new JProperty("ChargingSession",           ChargingSession.               ToString()),

                           EVTerminationCode.HasValue
                               ? new JProperty("evTerminationCode",         EVTerminationCode.       Value.ToString())
                               : null,

                           EVTerminationExplanation.HasValue
                               ? new JProperty("evTerminationExplanation",  EVTerminationExplanation.Value.ToString())
                               : null


                       );

            return CustomSessionStopRequestSerializer is not null
                       ? CustomSessionStopRequestSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (SessionStopRequest1, SessionStopRequest2)

        /// <summary>
        /// Compares two session stop requests for equality.
        /// </summary>
        /// <param name="SessionStopRequest1">A session stop request.</param>
        /// <param name="SessionStopRequest2">Another session stop request.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (SessionStopRequest? SessionStopRequest1,
                                           SessionStopRequest? SessionStopRequest2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(SessionStopRequest1, SessionStopRequest2))
                return true;

            // If one is null, but not both, return false.
            if (SessionStopRequest1 is null || SessionStopRequest2 is null)
                return false;

            return SessionStopRequest1.Equals(SessionStopRequest2);

        }

        #endregion

        #region Operator != (SessionStopRequest1, SessionStopRequest2)

        /// <summary>
        /// Compares two session stop requests for inequality.
        /// </summary>
        /// <param name="SessionStopRequest1">A session stop request.</param>
        /// <param name="SessionStopRequest2">Another session stop request.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (SessionStopRequest? SessionStopRequest1,
                                           SessionStopRequest? SessionStopRequest2)

            => !(SessionStopRequest1 == SessionStopRequest2);

        #endregion

        #endregion

        #region IEquatable<SessionStopRequest> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two session stop requests for equality.
        /// </summary>
        /// <param name="Object">A session stop request to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is SessionStopRequest sessionStopRequest &&
                   Equals(sessionStopRequest);

        #endregion

        #region Equals(SessionStopRequest)

        /// <summary>
        /// Compares two session stop requests for equality.
        /// </summary>
        /// <param name="SessionStopRequest">A session stop request to compare with.</param>
        public override Boolean Equals(SessionStopRequest? SessionStopRequest)

            => SessionStopRequest is not null &&

               ChargingSession.Equals(SessionStopRequest.ChargingSession) &&

            ((!EVTerminationCode.       HasValue && !SessionStopRequest.EVTerminationCode.       HasValue) ||
              (EVTerminationCode.       HasValue &&  SessionStopRequest.EVTerminationCode.       HasValue && EVTerminationCode.       Value.Equals(SessionStopRequest.EVTerminationCode.       Value))) &&

            ((!EVTerminationExplanation.HasValue && !SessionStopRequest.EVTerminationExplanation.HasValue) ||
              (EVTerminationExplanation.HasValue &&  SessionStopRequest.EVTerminationExplanation.HasValue && EVTerminationExplanation.Value.Equals(SessionStopRequest.EVTerminationExplanation.Value))) &&

               base.           Equals(SessionStopRequest);

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

                return ChargingSession.          GetHashCode()       * 7 ^
                      (EVTerminationCode?.       GetHashCode() ?? 0) * 5 ^
                      (EVTerminationExplanation?.GetHashCode() ?? 0) * 3 ^

                       base.     GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   ChargingSession,

                   EVTerminationCode.HasValue
                       ? ", code: "        + EVTerminationCode
                       : "",

                   EVTerminationExplanation.HasValue
                       ? ", explanation: " + EVTerminationExplanation
                       : ""

               );

        #endregion

    }

}
