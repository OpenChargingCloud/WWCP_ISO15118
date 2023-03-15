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
    /// The scheduled schedule exchange request.
    /// </summary>
    public class ScheduledScheduleExchangeRequest : ScheduleExchangeRequest,
                                                    IEquatable<ScheduledScheduleExchangeRequest>
    {

        #region Properties

        /// <summary>
        /// The optional (expected) departure time.
        /// </summary>
        [Optional]
        public DateTime?        DepartureTime             { get; }

        /// <summary>
        /// The optional EV target energy requested.
        /// </summary>
        [Optional]
        public RationalNumber?  EVTargetEnergyRequest     { get; }

        /// <summary>
        /// The optional EV maximum energy requested.
        /// </summary>
        [Optional]
        public RationalNumber?  EVMaximumEnergyRequest    { get; }

        /// <summary>
        /// The optional EV minimum energy requested.
        /// </summary>
        [Optional]
        public RationalNumber?  EVMinimumEnergyRequest    { get; }

        /// <summary>
        /// The optional EV energy offer.
        /// </summary>
        [Optional]
        public EVEnergyOffer?   EVEnergyOffer             { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new scheduled schedule exchange request.
        /// </summary>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="MaximumSupportingPoints"></param>
        /// 
        /// <param name="DepartureTime">An optional (expected) departure time.</param>
        /// <param name="EVTargetEnergyRequest">An optional EV target energy requested.</param>
        /// <param name="EVMaximumEnergyRequest">An optional EV maximum energy requested.</param>
        /// <param name="EVMinimumEnergyRequest">An optional EV minimum energy requested.</param>
        /// <param name="EVEnergyOffer">An optional EV energy offer.</param>
        public ScheduledScheduleExchangeRequest(MessageHeader    MessageHeader,
                                                UInt16           MaximumSupportingPoints,

                                                DateTime?        DepartureTime,
                                                RationalNumber?  EVTargetEnergyRequest,
                                                RationalNumber?  EVMaximumEnergyRequest,
                                                RationalNumber?  EVMinimumEnergyRequest,
                                                EVEnergyOffer?   EVEnergyOffer)

            : base(MessageHeader,
                   MaximumSupportingPoints)

        {

            this.DepartureTime           = DepartureTime;
            this.EVTargetEnergyRequest   = EVTargetEnergyRequest;
            this.EVMaximumEnergyRequest  = EVMaximumEnergyRequest;
            this.EVMinimumEnergyRequest  = EVMinimumEnergyRequest;
            this.EVEnergyOffer           = EVEnergyOffer;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="Scheduled_SEReqControlModeType">
        //     <xs:sequence>
        //         <xs:element name="DepartureTime"          type="xs:unsignedInt"              minOccurs="0"/>
        //         <xs:element name="EVTargetEnergyRequest"  type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //         <xs:element name="EVMaximumEnergyRequest" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //         <xs:element name="EVMinimumEnergyRequest" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //         <xs:element name="EVEnergyOffer"          type="EVEnergyOfferType"           minOccurs="0"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomScheduledScheduleExchangeRequestParser = null)

        /// <summary>
        /// Parse the given JSON representation of a scheduled schedule exchange request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomScheduledScheduleExchangeRequestParser">A delegate to parse custom scheduled schedule exchange requests.</param>
        public static ScheduledScheduleExchangeRequest Parse(JObject                                                         JSON,
                                                             CustomJObjectParserDelegate<ScheduledScheduleExchangeRequest>?  CustomScheduledScheduleExchangeRequestParser   = null)
        {

            if (TryParse(JSON,
                         out var scheduledScheduleExchangeRequest,
                         out var errorResponse,
                         CustomScheduledScheduleExchangeRequestParser))
            {
                return scheduledScheduleExchangeRequest!;
            }

            throw new ArgumentException("The given JSON representation of a scheduled schedule exchange request is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out ScheduledScheduleExchangeRequest, out ErrorResponse, CustomScheduledScheduleExchangeRequestParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a scheduled schedule exchange request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ScheduledScheduleExchangeRequest">The parsed scheduled schedule exchange request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                JSON,
                                       out ScheduledScheduleExchangeRequest?  ScheduledScheduleExchangeRequest,
                                       out String?                            ErrorResponse)

            => TryParse(JSON,
                        out ScheduledScheduleExchangeRequest,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a scheduled schedule exchange request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ScheduledScheduleExchangeRequest">The parsed scheduled schedule exchange request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomScheduledScheduleExchangeRequestParser">A delegate to parse custom scheduled schedule exchange requests.</param>
        public static Boolean TryParse(JObject                                                         JSON,
                                       out ScheduledScheduleExchangeRequest?                           ScheduledScheduleExchangeRequest,
                                       out String?                                                     ErrorResponse,
                                       CustomJObjectParserDelegate<ScheduledScheduleExchangeRequest>?  CustomScheduledScheduleExchangeRequestParser)
        {

            try
            {

                ScheduledScheduleExchangeRequest = null;

                #region MessageHeader                [mandatory]

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

                #region MaximumSupportingPoints      [mandatory]

                if (!JSON.ParseMandatory("maximumSupportingPoints",
                                         "maximum supporting points",
                                         out UInt16 MaximumSupportingPoints,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion


                #region DepartureTime                [optional]

                if (JSON.ParseOptional("departureTime",
                                       "departure time",
                                       out DateTime? DepartureTime,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region EVTargetEnergyRequest        [optional]

                if (JSON.ParseOptionalJSON("evTargetEnergyRequest",
                                           "EV target energy request",
                                           RationalNumber.TryParse,
                                           out RationalNumber? EVTargetEnergyRequest,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region EVMaximumEnergyRequest       [optional]

                if (JSON.ParseOptionalJSON("evMaximumEnergyRequest",
                                           "EV maximum energy request",
                                           RationalNumber.TryParse,
                                           out RationalNumber? EVMaximumEnergyRequest,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region EVMinimumEnergyRequest       [optional]

                if (JSON.ParseOptionalJSON("evMinimumEnergyRequest",
                                           "EV minimum energy request",
                                           RationalNumber.TryParse,
                                           out RationalNumber? EVMinimumEnergyRequest,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region EVEnergyOffer                [optional]

                if (JSON.ParseOptionalJSON("evEnergyOffer",
                                           "EV energy offer",
                                           CommonMessages.EVEnergyOffer.TryParse,
                                           out EVEnergyOffer? EVEnergyOffer,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                ScheduledScheduleExchangeRequest = new ScheduledScheduleExchangeRequest(MessageHeader,
                                                                                        MaximumSupportingPoints,

                                                                                        DepartureTime,
                                                                                        EVTargetEnergyRequest,
                                                                                        EVMaximumEnergyRequest,
                                                                                        EVMinimumEnergyRequest,
                                                                                        EVEnergyOffer);

                if (CustomScheduledScheduleExchangeRequestParser is not null)
                    ScheduledScheduleExchangeRequest = CustomScheduledScheduleExchangeRequestParser(JSON,
                                                                                                    ScheduledScheduleExchangeRequest);

                return true;

            }
            catch (Exception e)
            {
                ScheduledScheduleExchangeRequest  = null;
                ErrorResponse                     = "The given JSON representation of a scheduled schedule exchange request is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomScheduledScheduleExchangeRequestSerializer = null, CustomMessageHeaderSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomScheduledScheduleExchangeRequestSerializer">A delegate to serialize custom scheduled schedule exchange requests.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        /// <param name="CustomRationalNumberSerializer">A delegate to serialize custom rational numbers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<ScheduledScheduleExchangeRequest>?  CustomScheduledScheduleExchangeRequestSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?                     CustomMessageHeaderSerializer                      = null,
                              CustomJObjectSerializerDelegate<RationalNumber>?                    CustomRationalNumberSerializer                     = null,
                              CustomJObjectSerializerDelegate<EVEnergyOffer>?                     CustomEVEnergyOfferSerializer                      = null,
                              CustomJObjectSerializerDelegate<EVPowerSchedule>?                   CustomEVPowerScheduleSerializer                    = null,
                              CustomJObjectSerializerDelegate<EVPowerScheduleEntry>?              CustomEVPowerScheduleEntrySerializer               = null,
                              CustomJObjectSerializerDelegate<EVAbsolutePriceSchedule>?           CustomEVAbsolutePriceScheduleSerializer            = null,
                              CustomJObjectSerializerDelegate<EVPriceRuleStack>?                  CustomEVPriceRuleStackSerializer                   = null,
                              CustomJObjectSerializerDelegate<EVPriceRule>?                       CustomEVPriceRuleSerializer                        = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("messageHeader",            MessageHeader.               ToJSON(CustomMessageHeaderSerializer)),
                                 new JProperty("maximumSupportingPoints",  MaximumSupportingPoints),

                           DepartureTime.HasValue
                               ? new JProperty("departureTime",            DepartureTime.Value.         ToIso8601())
                               : null,

                           EVTargetEnergyRequest  is not null
                               ? new JProperty("evTargetEnergyRequest",    EVTargetEnergyRequest. Value.ToJSON(CustomRationalNumberSerializer))
                               : null,

                           EVMaximumEnergyRequest is not null
                               ? new JProperty("evMaximumEnergyRequest",   EVMaximumEnergyRequest.Value.ToJSON(CustomRationalNumberSerializer))
                               : null,

                           EVMinimumEnergyRequest is not null
                               ? new JProperty("evMinimumEnergyRequest",   EVMinimumEnergyRequest.Value.ToJSON(CustomRationalNumberSerializer))
                               : null,

                           EVEnergyOffer          is not null
                               ? new JProperty("evEnergyOffer",            EVEnergyOffer.               ToJSON(CustomEVEnergyOfferSerializer,
                                                                                                               CustomEVPowerScheduleSerializer,
                                                                                                               CustomEVPowerScheduleEntrySerializer,
                                                                                                               CustomRationalNumberSerializer,
                                                                                                               CustomEVAbsolutePriceScheduleSerializer,
                                                                                                               CustomEVPriceRuleStackSerializer,
                                                                                                               CustomEVPriceRuleSerializer))
                               : null

                       );

            return CustomScheduledScheduleExchangeRequestSerializer is not null
                       ? CustomScheduledScheduleExchangeRequestSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (ScheduledScheduleExchangeRequest1, ScheduledScheduleExchangeRequest2)

        /// <summary>
        /// Compares two scheduled schedule exchange requests for equality.
        /// </summary>
        /// <param name="ScheduledScheduleExchangeRequest1">A scheduled schedule exchange request.</param>
        /// <param name="ScheduledScheduleExchangeRequest2">Another scheduled schedule exchange request.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ScheduledScheduleExchangeRequest? ScheduledScheduleExchangeRequest1,
                                           ScheduledScheduleExchangeRequest? ScheduledScheduleExchangeRequest2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(ScheduledScheduleExchangeRequest1, ScheduledScheduleExchangeRequest2))
                return true;

            // If one is null, but not both, return false.
            if (ScheduledScheduleExchangeRequest1 is null || ScheduledScheduleExchangeRequest2 is null)
                return false;

            return ScheduledScheduleExchangeRequest1.Equals(ScheduledScheduleExchangeRequest2);

        }

        #endregion

        #region Operator != (ScheduledScheduleExchangeRequest1, ScheduledScheduleExchangeRequest2)

        /// <summary>
        /// Compares two scheduled schedule exchange requests for inequality.
        /// </summary>
        /// <param name="ScheduledScheduleExchangeRequest1">A scheduled schedule exchange request.</param>
        /// <param name="ScheduledScheduleExchangeRequest2">Another scheduled schedule exchange request.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ScheduledScheduleExchangeRequest? ScheduledScheduleExchangeRequest1,
                                           ScheduledScheduleExchangeRequest? ScheduledScheduleExchangeRequest2)

            => !(ScheduledScheduleExchangeRequest1 == ScheduledScheduleExchangeRequest2);

        #endregion

        #endregion

        #region IEquatable<ScheduledScheduleExchangeRequest> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two scheduled schedule exchange requests for equality.
        /// </summary>
        /// <param name="Object">A scheduled schedule exchange request to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ScheduledScheduleExchangeRequest scheduledScheduleExchangeRequest &&
                   Equals(scheduledScheduleExchangeRequest);

        #endregion

        #region Equals(ScheduledScheduleExchangeRequest)

        /// <summary>
        /// Compares two scheduled schedule exchange requests for equality.
        /// </summary>
        /// <param name="ScheduledScheduleExchangeRequest">A scheduled schedule exchange request to compare with.</param>
        public Boolean Equals(ScheduledScheduleExchangeRequest? ScheduledScheduleExchangeRequest)

            => ScheduledScheduleExchangeRequest is not null &&

            ((!DepartureTime.         HasValue    && !ScheduledScheduleExchangeRequest.DepartureTime.         HasValue) ||
              (DepartureTime.         HasValue    &&  ScheduledScheduleExchangeRequest.DepartureTime.         HasValue    && DepartureTime.   Value.Equals(ScheduledScheduleExchangeRequest.DepartureTime.Value)))    &&

             ((EVTargetEnergyRequest  is     null &&  ScheduledScheduleExchangeRequest.EVTargetEnergyRequest  is     null) ||
              (EVTargetEnergyRequest  is not null &&  ScheduledScheduleExchangeRequest.EVTargetEnergyRequest  is not null && EVTargetEnergyRequest. Equals(ScheduledScheduleExchangeRequest.EVTargetEnergyRequest)))  &&

             ((EVMaximumEnergyRequest is     null &&  ScheduledScheduleExchangeRequest.EVMaximumEnergyRequest is     null) ||
              (EVMaximumEnergyRequest is not null &&  ScheduledScheduleExchangeRequest.EVMaximumEnergyRequest is not null && EVMaximumEnergyRequest.Equals(ScheduledScheduleExchangeRequest.EVMaximumEnergyRequest))) &&

             ((EVMinimumEnergyRequest is     null &&  ScheduledScheduleExchangeRequest.EVMinimumEnergyRequest is     null) ||
              (EVMinimumEnergyRequest is not null &&  ScheduledScheduleExchangeRequest.EVMinimumEnergyRequest is not null && EVMinimumEnergyRequest.Equals(ScheduledScheduleExchangeRequest.EVMinimumEnergyRequest))) &&

             ((EVEnergyOffer          is     null &&  ScheduledScheduleExchangeRequest.EVEnergyOffer          is     null) ||
              (EVEnergyOffer          is not null &&  ScheduledScheduleExchangeRequest.EVEnergyOffer          is not null && EVEnergyOffer.         Equals(ScheduledScheduleExchangeRequest.EVEnergyOffer)))          &&

               base.Equals(ScheduledScheduleExchangeRequest);

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

                return (DepartureTime?.         GetHashCode() ?? 0) * 13 ^
                       (EVTargetEnergyRequest?. GetHashCode() ?? 0) * 11 ^
                       (EVMaximumEnergyRequest?.GetHashCode() ?? 0) *  7 ^
                       (EVMinimumEnergyRequest?.GetHashCode() ?? 0) *  5 ^
                       (EVEnergyOffer?.         GetHashCode() ?? 0) *  3 ^

                        base.                   GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => new String?[] {

                   DepartureTime.HasValue
                       ? "departure time: " + DepartureTime.Value.ToIso8601()
                       : null,

                   EVTargetEnergyRequest  is not null
                       ? "target energy: "  + EVTargetEnergyRequest
                       : null,

                   EVMaximumEnergyRequest is not null
                       ? "maximum energy: " + EVMaximumEnergyRequest
                       : null,

                   EVMinimumEnergyRequest is not null
                       ? "minimum energy: " + EVMinimumEnergyRequest
                       : null,

                   EVEnergyOffer          is not null
                       ? "energy offer: " + EVEnergyOffer
                       : null

               }.Where(text => text is not null).
                 AggregateWith(", ");

        #endregion

    }

}
