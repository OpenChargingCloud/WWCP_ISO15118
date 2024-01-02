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
    /// The dynamic schedule exchange response.
    /// </summary>
    public class DynamicScheduleExchangeResponse : ScheduleExchangeResponse,
                                                   IEquatable<DynamicScheduleExchangeResponse>
    {

        #region Properties

        /// <summary>
        /// The optional departure time.
        /// </summary>
        [Optional]
        public DateTime?               DepartureTime            { get; }

        /// <summary>
        /// The optional minimum state-of-charge.
        /// </summary>
        [Optional]
        public PercentValue?           MinimumSOC               { get; }

        /// <summary>
        /// The optional target state-of-charge.
        /// </summary>
        [Optional]
        public PercentValue?           TargetSOC                { get; }

        /// <summary>
        /// The optional absolute price schedule.
        /// </summary>
        [OptionalChoice("priceSchedule")]
        public AbsolutePriceSchedule?  AbsolutePriceSchedule    { get; }

        /// <summary>
        /// The optional price level schedule.
        /// </summary>
        [OptionalChoice("priceSchedule")]
        public PriceLevelSchedule?     PriceLevelSchedule       { get; }

        #endregion

        #region Constructor(s)

        #region (private) DynamicScheduleExchangeResponse(Request, ..., DepartureTime, MinimumSOC, TargetSOC, AbsolutePriceSchedule, PriceLevelSchedule)

        /// <summary>
        /// Create a new dynamic schedule exchange response.
        /// </summary>
        /// <param name="Request">The schedule exchange request leading to this response.</param>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        /// <param name="EVSEProcessing">An EVSE processing type.</param>
        /// <param name="GoToPause">An optional indication whether to pause charging.</param>
        /// <param name="DepartureTime">An optional departure time.</param>
        /// <param name="MinimumSOC">An optional minimum state-of-charge.</param>
        /// <param name="TargetSOC">An optional target state-of-charge.</param>
        /// <param name="AbsolutePriceSchedule">An optional absolute price schedule.</param>
        /// <param name="PriceLevelSchedule">An optional price level schedule.</param>
        private DynamicScheduleExchangeResponse(ScheduleExchangeRequest  Request,
                                                MessageHeader            MessageHeader,
                                                ResponseCodes            ResponseCode,
                                                ProcessingTypes          EVSEProcessing,
                                                Boolean?                 GoToPause               = null,

                                                DateTime?                DepartureTime           = null,
                                                PercentValue?            MinimumSOC              = null,
                                                PercentValue?            TargetSOC               = null,
                                                AbsolutePriceSchedule?   AbsolutePriceSchedule   = null,
                                                PriceLevelSchedule?      PriceLevelSchedule      = null)

            : base(Request,
                   MessageHeader,
                   ResponseCode,
                   EVSEProcessing,
                   GoToPause)

        {

            this.DepartureTime          = DepartureTime;
            this.MinimumSOC             = MinimumSOC;
            this.TargetSOC              = TargetSOC;
            this.AbsolutePriceSchedule  = AbsolutePriceSchedule;
            this.PriceLevelSchedule     = PriceLevelSchedule;

        }

        #endregion

        #region (public)  DynamicScheduleExchangeResponse(Request, ...,                        DepartureTime = null, MinimumSOC = null, TargetSOC = null)

        /// <summary>
        /// Create a new dynamic schedule exchange response.
        /// </summary>
        /// <param name="Request">The schedule exchange request leading to this response.</param>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        /// <param name="EVSEProcessing">An EVSE processing type.</param>
        /// <param name="GoToPause">An optional indication whether to pause charging.</param>
        /// <param name="DepartureTime">An optional departure time.</param>
        /// <param name="MinimumSOC">An optional minimum state-of-charge.</param>
        /// <param name="TargetSOC">An optional target state-of-charge.</param>
        public DynamicScheduleExchangeResponse(ScheduleExchangeRequest  Request,
                                               MessageHeader            MessageHeader,
                                               ResponseCodes            ResponseCode,
                                               ProcessingTypes          EVSEProcessing,
                                               Boolean?                 GoToPause       = null,

                                               DateTime?                DepartureTime   = null,
                                               PercentValue?            MinimumSOC      = null,
                                               PercentValue?            TargetSOC       = null)

            : this(Request,
                   MessageHeader,
                   ResponseCode,
                   EVSEProcessing,
                   GoToPause,

                   DepartureTime,
                   MinimumSOC,
                   TargetSOC,
                   null,
                   null)

        {

            this.DepartureTime  = DepartureTime;
            this.MinimumSOC     = MinimumSOC;
            this.TargetSOC      = TargetSOC;

        }

        #endregion

        #region (public)  DynamicScheduleExchangeResponse(Request, ..., AbsolutePriceSchedule, DepartureTime = null, MinimumSOC = null, TargetSOC = null)

        /// <summary>
        /// Create a new dynamic schedule exchange response having an absolute price schedule.
        /// </summary>
        /// <param name="Request">The schedule exchange request leading to this response.</param>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        /// <param name="EVSEProcessing">An EVSE processing type.</param>
        /// <param name="AbsolutePriceSchedule">An absolute price schedule.</param>
        /// <param name="GoToPause">An optional indication whether to pause charging.</param>
        /// <param name="DepartureTime">An optional departure time.</param>
        /// <param name="MinimumSOC">An optional minimum state-of-charge.</param>
        /// <param name="TargetSOC">An optional target state-of-charge.</param>
        public DynamicScheduleExchangeResponse(ScheduleExchangeRequest  Request,
                                               MessageHeader            MessageHeader,
                                               ResponseCodes            ResponseCode,
                                               ProcessingTypes          EVSEProcessing,
                                               AbsolutePriceSchedule    AbsolutePriceSchedule,
                                               Boolean?                 GoToPause       = null,

                                               DateTime?                DepartureTime   = null,
                                               PercentValue?            MinimumSOC      = null,
                                               PercentValue?            TargetSOC       = null)

            : this(Request,
                   MessageHeader,
                   ResponseCode,
                   EVSEProcessing,
                   GoToPause,

                   DepartureTime,
                   MinimumSOC,
                   TargetSOC,
                   AbsolutePriceSchedule,
                   null)

        { }

        #endregion

        #region (public)  DynamicScheduleExchangeResponse(Request, ..., PriceLevelSchedule,    DepartureTime = null, MinimumSOC = null, TargetSOC = null)

        /// <summary>
        /// Create a new dynamic schedule exchange response having a price level schedule.
        /// </summary>
        /// <param name="Request">The schedule exchange request leading to this response.</param>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        /// <param name="EVSEProcessing">An EVSE processing type.</param>
        /// <param name="PriceLevelSchedule">A price level schedule.</param>
        /// <param name="GoToPause">An optional indication whether to pause charging.</param>
        /// <param name="DepartureTime">An optional departure time.</param>
        /// <param name="MinimumSOC">An optional minimum state-of-charge.</param>
        /// <param name="TargetSOC">An optional target state-of-charge.</param>
        public DynamicScheduleExchangeResponse(ScheduleExchangeRequest  Request,
                                               MessageHeader            MessageHeader,
                                               ResponseCodes            ResponseCode,
                                               ProcessingTypes          EVSEProcessing,
                                               PriceLevelSchedule       PriceLevelSchedule,
                                               Boolean?                 GoToPause       = null,

                                               DateTime?                DepartureTime   = null,
                                               PercentValue?            MinimumSOC      = null,
                                               PercentValue?            TargetSOC       = null)

            : this(Request,
                   MessageHeader,
                   ResponseCode,
                   EVSEProcessing,
                   GoToPause,

                   DepartureTime,
                   MinimumSOC,
                   TargetSOC,
                   null,
                   PriceLevelSchedule)

        { }

        #endregion

        #endregion


        #region Documentation

        // <xs:complexType name="Dynamic_SEResControlModeType">
        //     <xs:sequence>
        //
        //         <xs:element name="DepartureTime" type="xs:unsignedInt"            minOccurs="0"/>
        //         <xs:element name="MinimumSOC"    type="v2gci_ct:percentValueType" minOccurs="0"/>
        //         <xs:element name="TargetSOC"     type="v2gci_ct:percentValueType" minOccurs="0"/>
        //
        //         <xs:choice minOccurs="0">
        //             <xs:element name="AbsolutePriceSchedule" type="AbsolutePriceScheduleType"/>
        //             <xs:element name="PriceLevelSchedule" type="PriceLevelScheduleType"/>
        //         </xs:choice>
        //
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (Request, JSON, CustomDynamicScheduleExchangeResponseParser = null)

        /// <summary>
        /// Parse the given JSON representation of a dynamic schedule exchange response.
        /// </summary>
        /// <param name="Request">The schedule exchange request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomDynamicScheduleExchangeResponseParser">A delegate to parse custom dynamic schedule exchange responses.</param>
        public static DynamicScheduleExchangeResponse Parse(ScheduleExchangeRequest                                        Request,
                                                            JObject                                                        JSON,
                                                            CustomJObjectParserDelegate<DynamicScheduleExchangeResponse>?  CustomDynamicScheduleExchangeResponseParser   = null)
        {

            if (TryParse(Request,
                         JSON,
                         out var dynamicScheduleExchangeResponse,
                         out var errorResponse,
                         CustomDynamicScheduleExchangeResponseParser))
            {
                return dynamicScheduleExchangeResponse!;
            }

            throw new ArgumentException("The given JSON representation of a dynamic schedule exchange response is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(Request, JSON, out DynamicScheduleExchangeResponse, out ErrorResponse, CustomDynamicScheduleExchangeResponseParser = null)

        /// <summary>
        /// Try to parse the given JSON representation of a dynamic schedule exchange response.
        /// </summary>
        /// <param name="Request">The schedule exchange request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DynamicScheduleExchangeResponse">The parsed dynamic schedule exchange response.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomDynamicScheduleExchangeResponseParser">A delegate to parse custom dynamic schedule exchange responses.</param>
        public static Boolean TryParse(ScheduleExchangeRequest                                        Request,
                                       JObject                                                        JSON,
                                       out DynamicScheduleExchangeResponse?                           DynamicScheduleExchangeResponse,
                                       out String?                                                    ErrorResponse,
                                       CustomJObjectParserDelegate<DynamicScheduleExchangeResponse>?  CustomDynamicScheduleExchangeResponseParser   = null)
        {

            ErrorResponse = null;

            try
            {

                DynamicScheduleExchangeResponse = null;

                #region MessageHeader            [mandatory]

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

                #region ResponseCode             [mandatory]

                if (!JSON.ParseMandatory("responseCode",
                                         "response code",
                                         ResponseCodesExtensions.TryParse,
                                         out ResponseCodes ResponseCode,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region EVSEProcessing           [mandatory]

                if (!JSON.ParseMandatory("evseProcessing",
                                         "evse processing",
                                         ProcessingTypesExtensions.TryParse,
                                         out ProcessingTypes EVSEProcessing,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region GoToPause                [optional]

                if (JSON.ParseOptional("goToPause",
                                       "goto pause",
                                       out Boolean? GoToPause,
                                       out ErrorResponse))
                {
                    return false;
                }

                #endregion


                #region DepartureTime            [optional]

                if (JSON.ParseOptional("departureTime",
                                       "departure time",
                                       out DateTime? DepartureTime,
                                       out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region MinimumSOC               [optional]

                if (JSON.ParseOptional("minimumSOC",
                                       "minimum state-of-charge",
                                       PercentValue.TryParse,
                                       out PercentValue? MinimumSOC,
                                       out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region TargetSOC                [optional]

                if (JSON.ParseOptional("targetSOC",
                                       "target state-of-charge",
                                       PercentValue.TryParse,
                                       out PercentValue? TargetSOC,
                                       out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region AbsolutePriceSchedule    [optional]

                if (JSON.ParseOptionalJSON("absolutePriceSchedule",
                                           "absolute price schedule",
                                           CommonMessages.AbsolutePriceSchedule.TryParse,
                                           out AbsolutePriceSchedule? AbsolutePriceSchedule,
                                           out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region PriceLevelSchedule       [optional]

                if (JSON.ParseOptionalJSON("priceLevelSchedule",
                                           "price level schedule",
                                           CommonMessages.PriceLevelSchedule.TryParse,
                                           out PriceLevelSchedule? PriceLevelSchedule,
                                           out ErrorResponse))
                {
                    return false;
                }

                #endregion


                DynamicScheduleExchangeResponse = new DynamicScheduleExchangeResponse(Request,
                                                                                      MessageHeader,
                                                                                      ResponseCode,
                                                                                      EVSEProcessing,
                                                                                      GoToPause,

                                                                                      DepartureTime,
                                                                                      MinimumSOC,
                                                                                      TargetSOC,
                                                                                      AbsolutePriceSchedule,
                                                                                      PriceLevelSchedule);

                if (CustomDynamicScheduleExchangeResponseParser is not null)
                    DynamicScheduleExchangeResponse = CustomDynamicScheduleExchangeResponseParser(JSON,
                                                                                                  DynamicScheduleExchangeResponse);

                return true;

            }
            catch (Exception e)
            {
                DynamicScheduleExchangeResponse  = null;
                ErrorResponse                    = "The given JSON representation of a dynamic schedule exchange response is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomDynamicScheduleExchangeResponseSerializer = null, CustomMessageHeaderSerializer = null, ...)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomDynamicScheduleExchangeResponseSerializer">A delegate to serialize custom dynamic schedule exchange responses.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        /// <param name="CustomAbsolutePriceScheduleSerializer">A delegate to serialize custom absolute price schedules.</param>
        /// <param name="CustomPriceRuleStackSerializer">A delegate to serialize custom price rule stacks.</param>
        /// <param name="CustomPriceRuleSerializer">A delegate to serialize custom price rules.</param>
        /// <param name="CustomRationalNumberSerializer">A delegate to serialize custom rational numbers.</param>
        /// <param name="CustomTaxRuleSerializer">A delegate to serialize custom tax rules.</param>
        /// <param name="CustomOverstayRuleListSerializer">A delegate to serialize custom overstay rule lists.</param>
        /// <param name="CustomOverstayRuleSerializer">A delegate to serialize custom overstay rules.</param>
        /// <param name="CustomAdditionalServiceSerializer">A delegate to serialize custom additional services.</param>
        /// <param name="CustomPriceLevelScheduleSerializer">A delegate to serialize custom price level schedules.</param>
        /// <param name="CustomPriceLevelScheduleEntrySerializer">A delegate to serialize custom price level schedule entries.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<DynamicScheduleExchangeResponse>?  CustomDynamicScheduleExchangeResponseSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?                    CustomMessageHeaderSerializer                     = null,
                              CustomJObjectSerializerDelegate<AbsolutePriceSchedule>?            CustomAbsolutePriceScheduleSerializer             = null,
                              CustomJObjectSerializerDelegate<PriceRuleStack>?                   CustomPriceRuleStackSerializer                    = null,
                              CustomJObjectSerializerDelegate<PriceRule>?                        CustomPriceRuleSerializer                         = null,
                              CustomJObjectSerializerDelegate<RationalNumber>?                   CustomRationalNumberSerializer                    = null,
                              CustomJObjectSerializerDelegate<TaxRule>?                          CustomTaxRuleSerializer                           = null,
                              CustomJObjectSerializerDelegate<OverstayRuleList>?                 CustomOverstayRuleListSerializer                  = null,
                              CustomJObjectSerializerDelegate<OverstayRule>?                     CustomOverstayRuleSerializer                      = null,
                              CustomJObjectSerializerDelegate<AdditionalService>?                CustomAdditionalServiceSerializer                 = null,
                              CustomJObjectSerializerDelegate<PriceLevelSchedule>?               CustomPriceLevelScheduleSerializer                = null,
                              CustomJObjectSerializerDelegate<PriceLevelScheduleEntry>?          CustomPriceLevelScheduleEntrySerializer           = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("messageHeader",           MessageHeader. ToJSON(CustomMessageHeaderSerializer)),
                                 new JProperty("responseCode",            ResponseCode.  AsText()),
                                 new JProperty("evseProcessing",          EVSEProcessing.AsText()),

                           GoToPause.HasValue
                               ? new JProperty("goToPause",               GoToPause.    Value)
                               : null,

                           DepartureTime.HasValue
                               ? new JProperty("departureTime",           DepartureTime.Value.ToIso8601())
                               : null,

                           MinimumSOC.HasValue
                               ? new JProperty("minimumSOC",              MinimumSOC.   Value.Value)
                               : null,

                           TargetSOC.HasValue
                               ? new JProperty("targetSOC",               TargetSOC.    Value.Value)
                               : null,

                           AbsolutePriceSchedule is not null
                               ? new JProperty("absolutePriceSchedule",   AbsolutePriceSchedule.ToJSON(CustomAbsolutePriceScheduleSerializer,
                                                                                                       CustomPriceRuleStackSerializer,
                                                                                                       CustomPriceRuleSerializer,
                                                                                                       CustomRationalNumberSerializer,
                                                                                                       CustomTaxRuleSerializer,
                                                                                                       CustomOverstayRuleListSerializer,
                                                                                                       CustomOverstayRuleSerializer,
                                                                                                       CustomAdditionalServiceSerializer))
                               : null,

                           PriceLevelSchedule is not null
                               ? new JProperty("priceLevelSchedule",      PriceLevelSchedule.   ToJSON(CustomPriceLevelScheduleSerializer,
                                                                                                       CustomPriceLevelScheduleEntrySerializer))
                               : null

                       );

            return CustomDynamicScheduleExchangeResponseSerializer is not null
                       ? CustomDynamicScheduleExchangeResponseSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (DynamicScheduleExchangeResponse1, DynamicScheduleExchangeResponse2)

        /// <summary>
        /// Compares two dynamic schedule exchange responses for equality.
        /// </summary>
        /// <param name="DynamicScheduleExchangeResponse1">A dynamic schedule exchange response.</param>
        /// <param name="DynamicScheduleExchangeResponse2">Another dynamic schedule exchange response.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (DynamicScheduleExchangeResponse? DynamicScheduleExchangeResponse1,
                                           DynamicScheduleExchangeResponse? DynamicScheduleExchangeResponse2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(DynamicScheduleExchangeResponse1, DynamicScheduleExchangeResponse2))
                return true;

            // If one is null, but not both, return false.
            if (DynamicScheduleExchangeResponse1 is null || DynamicScheduleExchangeResponse2 is null)
                return false;

            return DynamicScheduleExchangeResponse1.Equals(DynamicScheduleExchangeResponse2);

        }

        #endregion

        #region Operator != (DynamicScheduleExchangeResponse1, DynamicScheduleExchangeResponse2)

        /// <summary>
        /// Compares two dynamic schedule exchange responses for inequality.
        /// </summary>
        /// <param name="DynamicScheduleExchangeResponse1">A dynamic schedule exchange response.</param>
        /// <param name="DynamicScheduleExchangeResponse2">Another dynamic schedule exchange response.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (DynamicScheduleExchangeResponse? DynamicScheduleExchangeResponse1,
                                           DynamicScheduleExchangeResponse? DynamicScheduleExchangeResponse2)

            => !(DynamicScheduleExchangeResponse1 == DynamicScheduleExchangeResponse2);

        #endregion

        #endregion

        #region IEquatable<DynamicScheduleExchangeResponse> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two dynamic schedule exchange responses for equality.
        /// </summary>
        /// <param name="Object">A dynamic schedule exchange response to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is DynamicScheduleExchangeResponse dnamicScheduleExchangeResponse &&
                   Equals(dnamicScheduleExchangeResponse);

        #endregion

        #region Equals(DynamicScheduleExchangeResponse)

        /// <summary>
        /// Compares two dynamic schedule exchange responses for equality.
        /// </summary>
        /// <param name="DynamicScheduleExchangeResponse">A dynamic schedule exchange response to compare with.</param>
        public Boolean Equals(DynamicScheduleExchangeResponse? DynamicScheduleExchangeResponse)

            => DynamicScheduleExchangeResponse is not null &&

            ((!DepartureTime.        HasValue    && !DynamicScheduleExchangeResponse.DepartureTime.        HasValue)    ||
              (DepartureTime.        HasValue    &&  DynamicScheduleExchangeResponse.DepartureTime.        HasValue    && DepartureTime.  Value.Equals(DynamicScheduleExchangeResponse.DepartureTime.Value)))   &&

            ((!MinimumSOC.           HasValue    && !DynamicScheduleExchangeResponse.MinimumSOC.           HasValue)    ||
              (MinimumSOC.           HasValue    &&  DynamicScheduleExchangeResponse.MinimumSOC.           HasValue    && MinimumSOC.     Value.Equals(DynamicScheduleExchangeResponse.MinimumSOC.Value)))      &&

            ((!TargetSOC.            HasValue    && !DynamicScheduleExchangeResponse.TargetSOC.            HasValue)    ||
              (TargetSOC.            HasValue    &&  DynamicScheduleExchangeResponse.TargetSOC.            HasValue    && TargetSOC.      Value.Equals(DynamicScheduleExchangeResponse.TargetSOC.Value)))       &&

             ((AbsolutePriceSchedule is     null &&  DynamicScheduleExchangeResponse.AbsolutePriceSchedule is     null) ||
              (AbsolutePriceSchedule is not null &&  DynamicScheduleExchangeResponse.AbsolutePriceSchedule is not null && AbsolutePriceSchedule.Equals(DynamicScheduleExchangeResponse.AbsolutePriceSchedule))) &&

             ((PriceLevelSchedule    is     null &&  DynamicScheduleExchangeResponse.PriceLevelSchedule    is     null) ||
              (PriceLevelSchedule    is not null &&  DynamicScheduleExchangeResponse.PriceLevelSchedule    is not null && PriceLevelSchedule.   Equals(DynamicScheduleExchangeResponse.PriceLevelSchedule)))    &&

               base.GenericEquals(DynamicScheduleExchangeResponse);

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

                return (DepartureTime?.        GetHashCode() ?? 0) * 13 ^
                       (MinimumSOC?.           GetHashCode() ?? 0) * 11 ^
                       (TargetSOC?.            GetHashCode() ?? 0) *  7 ^
                       (AbsolutePriceSchedule?.GetHashCode() ?? 0) *  5 ^
                       (PriceLevelSchedule?.   GetHashCode() ?? 0) *  3 ^

                        base.                  GetHashCode();

            }
        }

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
