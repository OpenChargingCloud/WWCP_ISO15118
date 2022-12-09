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
    /// The dynamic schedule exchange request.
    /// </summary>
    public class DynamicScheduleExchangeRequest : ScheduleExchangeRequest,
                                                  IEquatable<DynamicScheduleExchangeRequest>
    {

        #region Properties

        /// <summary>
        /// The (expected) departure time.
        /// </summary>
        [Mandatory]
        public DateTime         DepartureTime                { get; }

        /// <summary>
        /// The EV target energy requested.
        /// </summary>
        [Mandatory]
        public RationalNumber   EVTargetEnergyRequest        { get; }

        /// <summary>
        /// The EV maximum energy requested.
        /// </summary>
        [Mandatory]
        public RationalNumber   EVMaximumEnergyRequest       { get; }

        /// <summary>
        /// The EV minimum energy requested.
        /// </summary>
        [Mandatory]
        public RationalNumber   EVMinimumEnergyRequest       { get; }

        /// <summary>
        /// The optional minimum state-of-charge.
        /// </summary>
        [Optional]
        public PercentValue?    MinimumSOC                   { get; }

        /// <summary>
        /// The optional target state-of-charge.
        /// </summary>
        [Optional]
        public PercentValue?    TargetSOC                    { get; }

        /// <summary>
        /// The optional EV maximum V2X energy requested.
        /// </summary>
        [Optional]
        public RationalNumber?  EVMaximumV2XEnergyRequest    { get; }

        /// <summary>
        /// The optional EV minimum V2X energy requested.
        /// </summary>
        [Optional]
        public RationalNumber?  EVMinimumV2XEnergyRequest    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new dynamic schedule exchange request.
        /// </summary>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="MaximumSupportingPoints">The maximum number of supporting points of the energy schedule [min/max 12...1024].</param>
        /// 
        /// <param name="DepartureTime">An (expected) departure time.</param>
        /// <param name="EVTargetEnergyRequest">An EV target energy requested.</param>
        /// <param name="EVMaximumEnergyRequest">An EV maximum energy requested.</param>
        /// <param name="EVMinimumEnergyRequest">An EV minimum energy requested.</param>
        /// 
        /// <param name="MinimumSOC">An optional minimum state-of-charge.</param>
        /// <param name="TargetSOC">An optional target state-of-charge.</param>
        /// <param name="EVMaximumV2XEnergyRequest">An optional EV maximum V2X energy requested.</param>
        /// <param name="EVMinimumV2XEnergyRequest">An optional EV minimum V2X energy requested.</param>
        public DynamicScheduleExchangeRequest(MessageHeader    MessageHeader,
                                              UInt16           MaximumSupportingPoints,

                                              DateTime         DepartureTime,
                                              RationalNumber   EVTargetEnergyRequest,
                                              RationalNumber   EVMaximumEnergyRequest,
                                              RationalNumber   EVMinimumEnergyRequest,

                                              PercentValue?    MinimumSOC                  = null,
                                              PercentValue?    TargetSOC                   = null,
                                              RationalNumber?  EVMaximumV2XEnergyRequest   = null,
                                              RationalNumber?  EVMinimumV2XEnergyRequest   = null)

            : base(MessageHeader,
                   MaximumSupportingPoints)

        {

            this.DepartureTime              = DepartureTime;
            this.EVTargetEnergyRequest      = EVTargetEnergyRequest;
            this.EVMaximumEnergyRequest     = EVMaximumEnergyRequest;
            this.EVMinimumEnergyRequest     = EVMinimumEnergyRequest;

            this.MinimumSOC                 = MinimumSOC;
            this.TargetSOC                  = TargetSOC;
            this.EVMaximumV2XEnergyRequest  = EVMaximumV2XEnergyRequest;
            this.EVMinimumV2XEnergyRequest  = EVMinimumV2XEnergyRequest;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="Dynamic_SEReqControlModeType">
        //     <xs:sequence>
        //         <xs:element name="DepartureTime"             type="xs:unsignedInt"/>
        //         <xs:element name="MinimumSOC"                type="v2gci_ct:percentValueType" minOccurs="0"/>
        //         <xs:element name="TargetSOC"                 type="v2gci_ct:percentValueType" minOccurs="0"/>
        //         <xs:element name="EVTargetEnergyRequest"     type="v2gci_ct:RationalNumberType"/>
        //         <xs:element name="EVMaximumEnergyRequest"    type="v2gci_ct:RationalNumberType"/>
        //         <xs:element name="EVMinimumEnergyRequest"    type="v2gci_ct:RationalNumberType"/>
        //         <xs:element name="EVMaximumV2XEnergyRequest" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //         <xs:element name="EVMinimumV2XEnergyRequest" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomDynamicScheduleExchangeRequestParser = null)

        /// <summary>
        /// Parse the given JSON representation of a dynamic schedule exchange request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomDynamicScheduleExchangeRequestParser">A delegate to parse custom dynamic schedule exchange requests.</param>
        public static DynamicScheduleExchangeRequest Parse(JObject                                                       JSON,
                                                           CustomJObjectParserDelegate<DynamicScheduleExchangeRequest>?  CustomDynamicScheduleExchangeRequestParser   = null)
        {

            if (TryParse(JSON,
                         out var dynamicScheduleExchangeRequest,
                         out var errorResponse,
                         CustomDynamicScheduleExchangeRequestParser))
            {
                return dynamicScheduleExchangeRequest!;
            }

            throw new ArgumentException("The given JSON representation of a dynamic schedule exchange request is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out DynamicScheduleExchangeRequest, out ErrorResponse, CustomDynamicScheduleExchangeRequestParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a dynamic schedule exchange request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DynamicScheduleExchangeRequest">The parsed dynamic schedule exchange request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                              JSON,
                                       out DynamicScheduleExchangeRequest?  DynamicScheduleExchangeRequest,
                                       out String?                          ErrorResponse)

            => TryParse(JSON,
                        out DynamicScheduleExchangeRequest,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a dynamic schedule exchange request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DynamicScheduleExchangeRequest">The parsed dynamic schedule exchange request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomDynamicScheduleExchangeRequestParser">A delegate to parse custom dynamic schedule exchange requests.</param>
        public static Boolean TryParse(JObject                                                       JSON,
                                       out DynamicScheduleExchangeRequest?                           DynamicScheduleExchangeRequest,
                                       out String?                                                   ErrorResponse,
                                       CustomJObjectParserDelegate<DynamicScheduleExchangeRequest>?  CustomDynamicScheduleExchangeRequestParser)
        {

            try
            {

                DynamicScheduleExchangeRequest = null;

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


                #region DepartureTime                [mandatory]

                if (!JSON.ParseMandatory("departureTime",
                                         "departure time",
                                         out DateTime DepartureTime,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region EVTargetEnergyRequest        [mandatory]

                if (!JSON.ParseMandatoryJSON("evTargetEnergyRequest",
                                             "EV target energy request",
                                             RationalNumber.TryParse,
                                             out RationalNumber EVTargetEnergyRequest,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region EVMaximumEnergyRequest       [mandatory]

                if (!JSON.ParseMandatoryJSON("evMaximumEnergyRequest",
                                             "EV maximum energy request",
                                             RationalNumber.TryParse,
                                             out RationalNumber EVMaximumEnergyRequest,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region EVMinimumEnergyRequest       [mandatory]

                if (!JSON.ParseMandatoryJSON("evMinimumEnergyRequest",
                                             "EV minimum energy request",
                                             RationalNumber.TryParse,
                                             out RationalNumber EVMinimumEnergyRequest,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion


                #region MinimumSOC                   [optional]

                if (JSON.ParseOptional("minimumSOC",
                                       "minimum state-of-charge",
                                       PercentValue.TryParse,
                                       out PercentValue? MinimumSOC,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region TargetSOC                    [optional]

                if (JSON.ParseOptional("targetSOC",
                                       "target state-of-charge",
                                       PercentValue.TryParse,
                                       out PercentValue? TargetSOC,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region EVMaximumV2XEnergyRequest    [optional]

                if (JSON.ParseOptionalJSON("evMaximumV2XEnergyRequest",
                                           "EV maximum V2X energy request",
                                           RationalNumber.TryParse,
                                           out RationalNumber? EVMaximumV2XEnergyRequest,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region EVMinimumV2XEnergyRequest    [optional]

                if (JSON.ParseOptionalJSON("evMinimumV2XEnergyRequest",
                                           "EV minimum V2X energy request",
                                           RationalNumber.TryParse,
                                           out RationalNumber? EVMinimumV2XEnergyRequest,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                DynamicScheduleExchangeRequest = new DynamicScheduleExchangeRequest(MessageHeader,
                                                                                    MaximumSupportingPoints,

                                                                                    DepartureTime,
                                                                                    EVTargetEnergyRequest,
                                                                                    EVMaximumEnergyRequest,
                                                                                    EVMinimumEnergyRequest,

                                                                                    MinimumSOC,
                                                                                    TargetSOC,
                                                                                    EVMaximumV2XEnergyRequest,
                                                                                    EVMinimumV2XEnergyRequest);

                if (CustomDynamicScheduleExchangeRequestParser is not null)
                    DynamicScheduleExchangeRequest = CustomDynamicScheduleExchangeRequestParser(JSON,
                                                                                                DynamicScheduleExchangeRequest);

                return true;

            }
            catch (Exception e)
            {
                DynamicScheduleExchangeRequest  = null;
                ErrorResponse                   = "The given JSON representation of a dynamic schedule exchange request is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomDynamicScheduleExchangeRequestSerializer = null, CustomMessageHeaderSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomDynamicScheduleExchangeRequestSerializer">A delegate to serialize custom dynamic schedule exchange requests.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        /// <param name="CustomRationalNumberSerializer">A delegate to serialize custom rational numbers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<DynamicScheduleExchangeRequest>?  CustomDynamicScheduleExchangeRequestSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?                   CustomMessageHeaderSerializer                    = null,
                              CustomJObjectSerializerDelegate<RationalNumber>?                  CustomRationalNumberSerializer                   = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("messageHeader",               MessageHeader.                  ToJSON(CustomMessageHeaderSerializer)),
                                 new JProperty("maximumSupportingPoints",     MaximumSupportingPoints),

                                 new JProperty("departureTime",               DepartureTime.                  ToIso8601()),
                                 new JProperty("evTargetEnergyRequest",       EVTargetEnergyRequest.          ToJSON(CustomRationalNumberSerializer)),
                                 new JProperty("evMaximumEnergyRequest",      EVMaximumEnergyRequest.         ToJSON(CustomRationalNumberSerializer)),
                                 new JProperty("evMinimumEnergyRequest",      EVMinimumEnergyRequest.         ToJSON(CustomRationalNumberSerializer)),

                           MinimumSOC.HasValue
                               ? new JProperty("minimumSOC",                  MinimumSOC.Value.               ToString())
                               : null,

                           TargetSOC.HasValue
                               ? new JProperty("targetSOC",                   TargetSOC.Value.                ToString())
                               : null,

                           EVMaximumV2XEnergyRequest is not null
                               ? new JProperty("evMaximumV2XEnergyRequest",   EVMaximumV2XEnergyRequest.Value.ToJSON(CustomRationalNumberSerializer))
                               : null,

                           EVMinimumV2XEnergyRequest is not null
                               ? new JProperty("evMinimumV2XEnergyRequest",   EVMinimumV2XEnergyRequest.Value.ToJSON(CustomRationalNumberSerializer))
                               : null

                       );

            return CustomDynamicScheduleExchangeRequestSerializer is not null
                       ? CustomDynamicScheduleExchangeRequestSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (DynamicScheduleExchangeRequest1, DynamicScheduleExchangeRequest2)

        /// <summary>
        /// Compares two dynamic schedule exchange requests for equality.
        /// </summary>
        /// <param name="DynamicScheduleExchangeRequest1">A dynamic schedule exchange request.</param>
        /// <param name="DynamicScheduleExchangeRequest2">Another dynamic schedule exchange request.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (DynamicScheduleExchangeRequest? DynamicScheduleExchangeRequest1,
                                           DynamicScheduleExchangeRequest? DynamicScheduleExchangeRequest2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(DynamicScheduleExchangeRequest1, DynamicScheduleExchangeRequest2))
                return true;

            // If one is null, but not both, return false.
            if (DynamicScheduleExchangeRequest1 is null || DynamicScheduleExchangeRequest2 is null)
                return false;

            return DynamicScheduleExchangeRequest1.Equals(DynamicScheduleExchangeRequest2);

        }

        #endregion

        #region Operator != (DynamicScheduleExchangeRequest1, DynamicScheduleExchangeRequest2)

        /// <summary>
        /// Compares two dynamic schedule exchange requests for inequality.
        /// </summary>
        /// <param name="DynamicScheduleExchangeRequest1">A dynamic schedule exchange request.</param>
        /// <param name="DynamicScheduleExchangeRequest2">Another dynamic schedule exchange request.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (DynamicScheduleExchangeRequest? DynamicScheduleExchangeRequest1,
                                           DynamicScheduleExchangeRequest? DynamicScheduleExchangeRequest2)

            => !(DynamicScheduleExchangeRequest1 == DynamicScheduleExchangeRequest2);

        #endregion

        #endregion

        #region IEquatable<DynamicScheduleExchangeRequest> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two dynamic schedule exchange requests for equality.
        /// </summary>
        /// <param name="Object">A dynamic schedule exchange request to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is DynamicScheduleExchangeRequest dynamicScheduleExchangeRequest &&
                   Equals(dynamicScheduleExchangeRequest);

        #endregion

        #region Equals(DynamicScheduleExchangeRequest)

        /// <summary>
        /// Compares two dynamic schedule exchange requests for equality.
        /// </summary>
        /// <param name="DynamicScheduleExchangeRequest">A dynamic schedule exchange request to compare with.</param>
        public Boolean Equals(DynamicScheduleExchangeRequest? DynamicScheduleExchangeRequest)

            => DynamicScheduleExchangeRequest is not null &&

               DepartureTime.          Equals(DynamicScheduleExchangeRequest.DepartureTime)           &&
               EVTargetEnergyRequest.  Equals(DynamicScheduleExchangeRequest.EVTargetEnergyRequest)   &&
               EVMaximumEnergyRequest. Equals(DynamicScheduleExchangeRequest.EVMaximumEnergyRequest)  &&
               EVMinimumEnergyRequest. Equals(DynamicScheduleExchangeRequest.EVMinimumEnergyRequest)  &&

            ((!MinimumSOC.               HasValue    && !DynamicScheduleExchangeRequest.MinimumSOC.               HasValue)    ||
              (MinimumSOC.               HasValue    &&  DynamicScheduleExchangeRequest.MinimumSOC.               HasValue    && MinimumSOC.         Value.Equals(DynamicScheduleExchangeRequest.MinimumSOC.Value)))          &&

            ((!TargetSOC.                HasValue    && !DynamicScheduleExchangeRequest.TargetSOC.                HasValue)    ||
              (TargetSOC.                HasValue    &&  DynamicScheduleExchangeRequest.TargetSOC.                HasValue    && TargetSOC.          Value.Equals(DynamicScheduleExchangeRequest.TargetSOC. Value)))          &&

             ((EVMaximumV2XEnergyRequest is     null &&  DynamicScheduleExchangeRequest.EVMaximumV2XEnergyRequest is     null) ||
              (EVMaximumV2XEnergyRequest is not null &&  DynamicScheduleExchangeRequest.EVMaximumV2XEnergyRequest is not null && EVMaximumV2XEnergyRequest.Equals(DynamicScheduleExchangeRequest.EVMaximumV2XEnergyRequest))) &&

             ((EVMinimumV2XEnergyRequest is     null &&  DynamicScheduleExchangeRequest.EVMinimumV2XEnergyRequest is     null) ||
              (EVMinimumV2XEnergyRequest is not null &&  DynamicScheduleExchangeRequest.EVMinimumV2XEnergyRequest is not null && EVMinimumV2XEnergyRequest.Equals(DynamicScheduleExchangeRequest.EVMinimumV2XEnergyRequest))) &&

               base.Equals(DynamicScheduleExchangeRequest);

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

                return DepartureTime.             GetHashCode()       * 23 ^
                       EVTargetEnergyRequest.     GetHashCode()       * 19 ^
                       EVMaximumEnergyRequest.    GetHashCode()       * 17 ^
                       EVMinimumEnergyRequest.    GetHashCode()       * 13 ^

                      (MinimumSOC?.               GetHashCode() ?? 0) * 11 ^
                      (TargetSOC?.                GetHashCode() ?? 0) *  7 ^
                      (EVMaximumV2XEnergyRequest?.GetHashCode() ?? 0) *  5 ^
                      (EVMinimumV2XEnergyRequest?.GetHashCode() ?? 0) *  3 ^

                       base.                      GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   DepartureTime.ToIso8601(),  ", ",

                   EVTargetEnergyRequest,  " kWh, ",
                   EVMaximumEnergyRequest, " kWh, ",
                   EVMinimumEnergyRequest, " kWh",

                   MinimumSOC.HasValue
                       ? ", minimum SOC: "           + MinimumSOC
                       : "",

                   TargetSOC.HasValue
                       ? ", target SOC: "            + TargetSOC
                       : "",

                   EVMaximumV2XEnergyRequest is not null
                       ? ", EV maximum V2X energy: " + EVMaximumV2XEnergyRequest
                       : "",

                   EVMinimumV2XEnergyRequest is not null
                       ? ", EV minimum V2X energy: " + EVMinimumV2XEnergyRequest
                       : ""

               );

        #endregion

    }

}
