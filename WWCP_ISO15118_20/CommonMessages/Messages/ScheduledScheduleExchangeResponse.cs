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
    /// The scheduled schedule exchange response.
    /// </summary>
    public class ScheduledScheduleExchangeResponse : ScheduleExchangeResponse,
                                                     IEquatable<ScheduledScheduleExchangeResponse>
    {

        #region Properties

        /// <summary>
        /// The enumeration of schedule tuples.
        /// [max 3]
        /// </summary>
        [Mandatory]
        public IEnumerable<ScheduleTuple>  ScheduleTuples    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new scheduled schedule exchange response.
        /// </summary>
        /// <param name="Request">The schedule exchange request leading to this response.</param>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        /// <param name="EVSEProcessing">An EVSE processing type.</param>
        /// <param name="ScheduleTuples">An enumeration of schedule tuples [max 3].</param>
        /// <param name="GoToPause">An optional indication whether to pause charging.</param>
        public ScheduledScheduleExchangeResponse(ScheduleExchangeRequest     Request,
                                                 MessageHeader               MessageHeader,
                                                 ResponseCodes               ResponseCode,

                                                 ProcessingTypes             EVSEProcessing,
                                                 IEnumerable<ScheduleTuple>  ScheduleTuples,
                                                 Boolean?                    GoToPause   = null)

            : base(Request,
                   MessageHeader,
                   ResponseCode,
                   EVSEProcessing,
                   GoToPause)

        {

            this.ScheduleTuples = ScheduleTuples.Distinct();

        }

        #endregion


        #region Documentation

        // <xs:complexType name="Scheduled_SEResControlModeType">
        //     <xs:sequence>
        //         <xs:element name="ScheduleTuple" type="ScheduleTupleType" maxOccurs="3"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (Request, JSON, CustomScheduledScheduleExchangeResponseParser = null)

        /// <summary>
        /// Parse the given JSON representation of a scheduled schedule exchange response.
        /// </summary>
        /// <param name="Request">The schedule exchange request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomScheduledScheduleExchangeResponseParser">An optional delegate to parse custom scheduled schedule exchange responses.</param>
        public static ScheduledScheduleExchangeResponse Parse(ScheduleExchangeRequest                                          Request,
                                                              JObject                                                          JSON,
                                                              CustomJObjectParserDelegate<ScheduledScheduleExchangeResponse>?  CustomScheduledScheduleExchangeResponseParser   = null)
        {

            if (TryParse(Request,
                         JSON,
                         out var scheduledScheduleExchangeResponse,
                         out var errorResponse,
                         CustomScheduledScheduleExchangeResponseParser))
            {
                return scheduledScheduleExchangeResponse!;
            }

            throw new ArgumentException("The given JSON representation of a scheduled schedule exchange response is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(Request, JSON, out ScheduledScheduleExchangeResponse, out ErrorResponse, CustomScheduledScheduleExchangeResponseParser = null)

        /// <summary>
        /// Try to parse the given JSON representation of a scheduled schedule exchange response.
        /// </summary>
        /// <param name="Request">The schedule exchange request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ScheduledScheduleExchangeResponse">The parsed scheduled schedule exchange response.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomScheduledScheduleExchangeResponseParser">An optional delegate to parse custom scheduled schedule exchange responses.</param>
        public static Boolean TryParse(ScheduleExchangeRequest                                          Request,
                                       JObject                                                          JSON,
                                       out ScheduledScheduleExchangeResponse?                           ScheduledScheduleExchangeResponse,
                                       out String?                                                      ErrorResponse,
                                       CustomJObjectParserDelegate<ScheduledScheduleExchangeResponse>?  CustomScheduledScheduleExchangeResponseParser   = null)
        {

            ErrorResponse = null;

            try
            {

                ScheduledScheduleExchangeResponse = null;

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
                                         "evse processing",
                                         ProcessingTypesExtensions.TryParse,
                                         out ProcessingTypes EVSEProcessing,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region ScheduleTuples    [mandatory]

                if (!JSON.ParseMandatoryHashSet("scheduleTuples",
                                                "schedule tuples",
                                                ScheduleTuple.TryParse,
                                                out HashSet<ScheduleTuple> ScheduleTuples,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region GoToPause          [optional]

                if (JSON.ParseOptional("goToPause",
                                       "goto pause",
                                       out Boolean? GoToPause,
                                       out ErrorResponse))
                {
                    return false;
                }

                #endregion


                ScheduledScheduleExchangeResponse = new ScheduledScheduleExchangeResponse(Request,
                                                                                          MessageHeader,
                                                                                          ResponseCode,

                                                                                          EVSEProcessing,
                                                                                          ScheduleTuples,
                                                                                          GoToPause);

                if (CustomScheduledScheduleExchangeResponseParser is not null)
                    ScheduledScheduleExchangeResponse = CustomScheduledScheduleExchangeResponseParser(JSON,
                                                                                                      ScheduledScheduleExchangeResponse);

                return true;

            }
            catch (Exception e)
            {
                ScheduledScheduleExchangeResponse  = null;
                ErrorResponse                      = "The given JSON representation of a scheduled schedule exchange response is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomScheduledScheduleExchangeResponseSerializer = null, CustomMessageHeaderSerializer = null, ...)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomScheduledScheduleExchangeResponseSerializer">A delegate to serialize custom scheduled schedule exchange responses.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<ScheduledScheduleExchangeResponse>?  CustomScheduledScheduleExchangeResponseSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?                      CustomMessageHeaderSerializer                       = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("messageHeader",   MessageHeader. ToJSON(CustomMessageHeaderSerializer)),
                                 new JProperty("responseCode",    ResponseCode.  AsText()),

                                 new JProperty("evseProcessing",  EVSEProcessing.AsText()),
                                 new JProperty("scheduleTuples",  new JArray(ScheduleTuples.Select(scheduleTuple => scheduleTuple.ToJSON()))),

                           GoToPause.HasValue
                               ? new JProperty("goToPause",       GoToPause.Value)
                               : null

                       );

            return CustomScheduledScheduleExchangeResponseSerializer is not null
                       ? CustomScheduledScheduleExchangeResponseSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (ScheduledScheduleExchangeResponse1, ScheduledScheduleExchangeResponse2)

        /// <summary>
        /// Compares two scheduled schedule exchange responses for equality.
        /// </summary>
        /// <param name="ScheduledScheduleExchangeResponse1">A scheduled schedule exchange response.</param>
        /// <param name="ScheduledScheduleExchangeResponse2">Another scheduled schedule exchange response.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ScheduledScheduleExchangeResponse? ScheduledScheduleExchangeResponse1,
                                           ScheduledScheduleExchangeResponse? ScheduledScheduleExchangeResponse2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(ScheduledScheduleExchangeResponse1, ScheduledScheduleExchangeResponse2))
                return true;

            // If one is null, but not both, return false.
            if (ScheduledScheduleExchangeResponse1 is null || ScheduledScheduleExchangeResponse2 is null)
                return false;

            return ScheduledScheduleExchangeResponse1.Equals(ScheduledScheduleExchangeResponse2);

        }

        #endregion

        #region Operator != (ScheduledScheduleExchangeResponse1, ScheduledScheduleExchangeResponse2)

        /// <summary>
        /// Compares two scheduled schedule exchange responses for inequality.
        /// </summary>
        /// <param name="ScheduledScheduleExchangeResponse1">A scheduled schedule exchange response.</param>
        /// <param name="ScheduledScheduleExchangeResponse2">Another scheduled schedule exchange response.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ScheduledScheduleExchangeResponse? ScheduledScheduleExchangeResponse1,
                                           ScheduledScheduleExchangeResponse? ScheduledScheduleExchangeResponse2)

            => !(ScheduledScheduleExchangeResponse1 == ScheduledScheduleExchangeResponse2);

        #endregion

        #endregion

        #region IEquatable<ScheduledScheduleExchangeResponse> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two scheduled schedule exchange responses for equality.
        /// </summary>
        /// <param name="Object">A scheduled schedule exchange response to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ScheduledScheduleExchangeResponse scheduledScheduleExchangeResponse &&
                   Equals(scheduledScheduleExchangeResponse);

        #endregion

        #region Equals(ScheduledScheduleExchangeResponse)

        /// <summary>
        /// Compares two scheduled schedule exchange responses for equality.
        /// </summary>
        /// <param name="ScheduledScheduleExchangeResponse">A scheduled schedule exchange response to compare with.</param>
        public Boolean Equals(ScheduledScheduleExchangeResponse? ScheduledScheduleExchangeResponse)

            => ScheduledScheduleExchangeResponse is not null &&

               ScheduleTuples.Count().Equals(ScheduledScheduleExchangeResponse.ScheduleTuples.Count()) &&
               ScheduleTuples.All(scheduleTuple => ScheduledScheduleExchangeResponse.ScheduleTuples.Contains(scheduleTuple)) &&

               base.GenericEquals(ScheduledScheduleExchangeResponse);

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the HashCode of this object.
        /// </summary>
        public override Int32 GetHashCode()

            => base.GetHashCode();

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => base.ToString();

        #endregion

    }

}
