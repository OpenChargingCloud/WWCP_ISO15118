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
    /// The session setup request.
    /// </summary>
    public class SessionSetupRequest : AV2GRequest<SessionSetupRequest>
    {

        #region Properties

        /// <summary>
        /// The EV charge controller identification.
        /// </summary>
        [Mandatory]
        public EVCC_Id  EVCCId    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new session setup request.
        /// </summary>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="EVCCID">An EV charge controller identification.</param>
        public SessionSetupRequest(MessageHeader  MessageHeader,
                                   EVCC_Id        EVCCID)

            : base(MessageHeader)

        {

            this.EVCCId = EVCCID;

        }

        #endregion


        #region (static) Parse   (JSON, CustomSessionSetupRequestParser = null)

        /// <summary>
        /// Parse the given JSON representation of a session setup request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomSessionSetupRequestParser">A delegate to parse custom session setup requests.</param>
        public static SessionSetupRequest Parse(JObject                                            JSON,
                                                CustomJObjectParserDelegate<SessionSetupRequest>?  CustomSessionSetupRequestParser   = null)
        {

            if (TryParse(JSON,
                         out var sessionSetupRequest,
                         out var errorResponse,
                         CustomSessionSetupRequestParser))
            {
                return sessionSetupRequest!;
            }

            throw new ArgumentException("The given JSON representation of a session setup request is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out SessionSetupRequest, out ErrorResponse, CustomSessionSetupRequestParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a session setup request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="SessionSetupRequest">The parsed session setup request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                   JSON,
                                       out SessionSetupRequest?  SessionSetupRequest,
                                       out String?               ErrorResponse)

            => TryParse(JSON,
                        out SessionSetupRequest,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a session setup request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="SessionSetupRequest">The parsed session setup request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomSessionSetupRequestParser">A delegate to parse custom BootNotification requests.</param>
        public static Boolean TryParse(JObject                                            JSON,
                                       out SessionSetupRequest?                           SessionSetupRequest,
                                       out String?                                        ErrorResponse,
                                       CustomJObjectParserDelegate<SessionSetupRequest>?  CustomSessionSetupRequestParser)
        {

            try
            {

                SessionSetupRequest = null;

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

                #region EVCCId           [mandatory]

                if (!JSON.ParseMandatory("EVCCId",
                                         "EVCC identification",
                                         EVCC_Id.TryParse,
                                         out EVCC_Id EVCCId,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion


                SessionSetupRequest = new SessionSetupRequest(MessageHeader,
                                                              EVCCId);

                if (CustomSessionSetupRequestParser is not null)
                    SessionSetupRequest = CustomSessionSetupRequestParser(JSON,
                                                                          SessionSetupRequest);

                return true;

            }
            catch (Exception e)
            {
                SessionSetupRequest  = null;
                ErrorResponse        = "The given JSON representation of a session setup request is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomSessionSetupRequestSerializer = null, CustomMessageHeaderSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomSessionSetupRequestSerializer">A delegate to serialize custom session setup requests.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<SessionSetupRequest>?  CustomSessionSetupRequestSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?        CustomMessageHeaderSerializer         = null)
        {

            var json = JSONObject.Create(

                           new JProperty("messageHeader",  MessageHeader.ToJSON  (CustomMessageHeaderSerializer)),
                           new JProperty("evccId",         EVCCId.       ToString())

                       );

            return CustomSessionSetupRequestSerializer is not null
                       ? CustomSessionSetupRequestSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (SessionSetupRequest1, SessionSetupRequest2)

        /// <summary>
        /// Compares two session setup requests for equality.
        /// </summary>
        /// <param name="SessionSetupRequest1">A session setup request.</param>
        /// <param name="SessionSetupRequest2">Another session setup request.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (SessionSetupRequest? SessionSetupRequest1,
                                           SessionSetupRequest? SessionSetupRequest2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(SessionSetupRequest1, SessionSetupRequest2))
                return true;

            // If one is null, but not both, return false.
            if (SessionSetupRequest1 is null || SessionSetupRequest2 is null)
                return false;

            return SessionSetupRequest1.Equals(SessionSetupRequest2);

        }

        #endregion

        #region Operator != (SessionSetupRequest1, SessionSetupRequest2)

        /// <summary>
        /// Compares two session setup requests for inequality.
        /// </summary>
        /// <param name="SessionSetupRequest1">A session setup request.</param>
        /// <param name="SessionSetupRequest2">Another session setup request.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (SessionSetupRequest? SessionSetupRequest1,
                                           SessionSetupRequest? SessionSetupRequest2)

            => !(SessionSetupRequest1 == SessionSetupRequest2);

        #endregion

        #endregion

        #region IEquatable<SessionSetupRequest> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two session setup requests for equality.
        /// </summary>
        /// <param name="Object">A session setup request to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is SessionSetupRequest sessionSetupRequest &&
                   Equals(sessionSetupRequest);

        #endregion

        #region Equals(SessionSetupRequest)

        /// <summary>
        /// Compares two session setup requests for equality.
        /// </summary>
        /// <param name="SessionSetupRequest">A session setup request to compare with.</param>
        public override Boolean Equals(SessionSetupRequest? SessionSetupRequest)

            => SessionSetupRequest is not null &&

               EVCCId.     Equals(SessionSetupRequest.EVCCId) &&

               base.GenericEquals(SessionSetupRequest);

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

                return EVCCId.GetHashCode() * 3 ^
                       base.  GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => EVCCId.ToString();

        #endregion

    }

}
