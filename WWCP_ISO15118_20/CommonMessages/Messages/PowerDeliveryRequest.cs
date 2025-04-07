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
    /// The power delivery request.
    /// </summary>
    public class PowerDeliveryRequest : ARequest<PowerDeliveryRequest>
    {

        #region Properties

        /// <summary>
        /// The EV processing state.
        /// </summary>
        [Mandatory]
        public ProcessingTypes         EVProcessing           { get; }

        /// <summary>
        /// The charge progress state.
        /// </summary>
        [Mandatory]
        public ChargeProgressTypes     ChargeProgress         { get; }

        /// <summary>
        /// The EV power profile.
        /// </summary>
        [Optional]
        public EVPowerProfile?         EVPowerProfile         { get; }

        /// <summary>
        /// The optional bidirectional power transfer channel.
        /// </summary>
        [Optional]
        public ChannelSelectionTypes?  BPTChannelSelection    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new power delivery request.
        /// </summary>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="EVProcessing">An EV processing state.</param>
        /// <param name="ChargeProgress">A charge progress state.</param>
        /// <param name="EVPowerProfile">An EV power profile.</param>
        /// <param name="BPTChannelSelection">An optional bidirectional power transfer channel.</param>
        public PowerDeliveryRequest(MessageHeader           MessageHeader,
                                    ProcessingTypes         EVProcessing,
                                    ChargeProgressTypes     ChargeProgress,
                                    EVPowerProfile?         EVPowerProfile        = null,
                                    ChannelSelectionTypes?  BPTChannelSelection   = null)

            : base(MessageHeader)

        {

            this.EVProcessing         = EVProcessing;
            this.ChargeProgress       = ChargeProgress;
            this.EVPowerProfile       = EVPowerProfile;
            this.BPTChannelSelection  = BPTChannelSelection;

        }

        #endregion


        #region Documentation

        // <xs:element name="PowerDeliveryReq" type="PowerDeliveryReqType"/>
        //
        // <xs:complexType name="PowerDeliveryReqType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:V2GRequestType">
        //             <xs:sequence>
        //                 <xs:element name="EVProcessing"         type="v2gci_ct:processingType"/>
        //                 <xs:element name="ChargeProgress"       type="chargeProgressType"/>
        //                 <xs:element name="EVPowerProfile"       type="EVPowerProfileType"   minOccurs="0"/>
        //                 <xs:element name="BPT_ChannelSelection" type="channelSelectionType" minOccurs="0"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomPowerDeliveryRequestParser = null)

        /// <summary>
        /// Parse the given JSON representation of a power delivery request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomPowerDeliveryRequestParser">An optional delegate to parse custom power delivery requests.</param>
        public static PowerDeliveryRequest Parse(JObject                                             JSON,
                                                 CustomJObjectParserDelegate<PowerDeliveryRequest>?  CustomPowerDeliveryRequestParser   = null)
        {

            if (TryParse(JSON,
                         out var powerDeliveryRequest,
                         out var errorResponse,
                         CustomPowerDeliveryRequestParser))
            {
                return powerDeliveryRequest!;
            }

            throw new ArgumentException("The given JSON representation of a power delivery request is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out PowerDeliveryRequest, out ErrorResponse, CustomPowerDeliveryRequestParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a power delivery request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerDeliveryRequest">The parsed power delivery request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                    JSON,
                                       out PowerDeliveryRequest?  PowerDeliveryRequest,
                                       out String?                ErrorResponse)

            => TryParse(JSON,
                        out PowerDeliveryRequest,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a power delivery request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerDeliveryRequest">The parsed power delivery request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomPowerDeliveryRequestParser">An optional delegate to parse custom power delivery requests.</param>
        public static Boolean TryParse(JObject                                             JSON,
                                       out PowerDeliveryRequest?                           PowerDeliveryRequest,
                                       out String?                                         ErrorResponse,
                                       CustomJObjectParserDelegate<PowerDeliveryRequest>?  CustomPowerDeliveryRequestParser)
        {

            try
            {

                PowerDeliveryRequest = null;

                #region MessageHeader          [mandatory]

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

                #region EVProcessing           [mandatory]

                if (!JSON.ParseMandatory("evProcessing",
                                         "EV processing state",
                                         ProcessingTypesExtensions.TryParse,
                                         out ProcessingTypes EVProcessing,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region ChargeProgress         [mandatory]

                if (!JSON.ParseMandatory("chargeProgress",
                                         "charge progress state",
                                         ChargeProgressTypesExtensions.TryParse,
                                         out ChargeProgressTypes ChargeProgress,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region EVPowerProfile         [optional]

                if (JSON.ParseOptionalJSON("chargeProgress",
                                           "charge progress state",
                                           CommonMessages.EVPowerProfile.TryParse,
                                           out EVPowerProfile? EVPowerProfile,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region BPTChannelSelection    [optional]

                if (JSON.ParseOptional("bptChannelSelection",
                                       "bidirectional power transfer channel selection",
                                       ChannelSelectionTypesExtensions.TryParse,
                                       out ChannelSelectionTypes? BPTChannelSelection,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                PowerDeliveryRequest = new PowerDeliveryRequest(MessageHeader,
                                                                EVProcessing,
                                                                ChargeProgress,
                                                                EVPowerProfile,
                                                                BPTChannelSelection);

                if (CustomPowerDeliveryRequestParser is not null)
                    PowerDeliveryRequest = CustomPowerDeliveryRequestParser(JSON,
                                                                            PowerDeliveryRequest);

                return true;

            }
            catch (Exception e)
            {
                PowerDeliveryRequest  = null;
                ErrorResponse         = "The given JSON representation of a power delivery request is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomPowerDeliveryRequestSerializer = null, CustomMessageHeaderSerializer = null, ...)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomPowerDeliveryRequestSerializer">A delegate to serialize custom power delivery requests.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        /// <param name="CustomEVPowerProfileSerializer">A delegate to serialize custom EV power profiles.</param>
        /// <param name="CustomDynamicEVPowerProfileSerializer">A delegate to serialize custom dynamic EV power profiles.</param>
        /// <param name="CustomPowerScheduleEntrySerializer">A delegate to serialize custom power schedule entries.</param>
        /// <param name="CustomRationalNumberSerializer">A delegate to serialize custom rational numbers.</param>
        /// <param name="CustomScheduledEVPowerProfileSerializer">A delegate to serialize custom scheduled EV power profiles.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PowerDeliveryRequest>?     CustomPowerDeliveryRequestSerializer      = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?            CustomMessageHeaderSerializer             = null,
                              CustomJObjectSerializerDelegate<EVPowerProfile>?           CustomEVPowerProfileSerializer            = null,
                              CustomJObjectSerializerDelegate<DynamicEVPowerProfile>?    CustomDynamicEVPowerProfileSerializer     = null,
                              CustomJObjectSerializerDelegate<PowerScheduleEntry>?       CustomPowerScheduleEntrySerializer        = null,
                              CustomJObjectSerializerDelegate<RationalNumber>?           CustomRationalNumberSerializer            = null,
                              CustomJObjectSerializerDelegate<ScheduledEVPowerProfile>?  CustomScheduledEVPowerProfileSerializer   = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("messageHeader",        MessageHeader. ToJSON(CustomMessageHeaderSerializer)),
                                 new JProperty("evProcessing",         EVProcessing.  AsText()),
                                 new JProperty("chargeProgress",       ChargeProgress.AsText()),

                           EVPowerProfile is not null
                               ? new JProperty("EVPowerProfile",       EVPowerProfile.ToJSON(CustomEVPowerProfileSerializer,
                                                                                             CustomDynamicEVPowerProfileSerializer,
                                                                                             CustomPowerScheduleEntrySerializer,
                                                                                             CustomRationalNumberSerializer,
                                                                                             CustomScheduledEVPowerProfileSerializer))
                               : null,

                           BPTChannelSelection.HasValue
                               ? new JProperty("bptChannelSelection",  EVProcessing.  AsText())
                               : null

                       );

            return CustomPowerDeliveryRequestSerializer is not null
                       ? CustomPowerDeliveryRequestSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (PowerDeliveryRequest1, PowerDeliveryRequest2)

        /// <summary>
        /// Compares two power delivery requests for equality.
        /// </summary>
        /// <param name="PowerDeliveryRequest1">A power delivery request.</param>
        /// <param name="PowerDeliveryRequest2">Another power delivery request.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (PowerDeliveryRequest? PowerDeliveryRequest1,
                                           PowerDeliveryRequest? PowerDeliveryRequest2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(PowerDeliveryRequest1, PowerDeliveryRequest2))
                return true;

            // If one is null, but not both, return false.
            if (PowerDeliveryRequest1 is null || PowerDeliveryRequest2 is null)
                return false;

            return PowerDeliveryRequest1.Equals(PowerDeliveryRequest2);

        }

        #endregion

        #region Operator != (PowerDeliveryRequest1, PowerDeliveryRequest2)

        /// <summary>
        /// Compares two power delivery requests for inequality.
        /// </summary>
        /// <param name="PowerDeliveryRequest1">A power delivery request.</param>
        /// <param name="PowerDeliveryRequest2">Another power delivery request.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (PowerDeliveryRequest? PowerDeliveryRequest1,
                                           PowerDeliveryRequest? PowerDeliveryRequest2)

            => !(PowerDeliveryRequest1 == PowerDeliveryRequest2);

        #endregion

        #endregion

        #region IEquatable<PowerDeliveryRequest> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two power delivery requests for equality.
        /// </summary>
        /// <param name="Object">A power delivery request to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is PowerDeliveryRequest powerDeliveryRequest &&
                   Equals(powerDeliveryRequest);

        #endregion

        #region Equals(PowerDeliveryRequest)

        /// <summary>
        /// Compares two power delivery requests for equality.
        /// </summary>
        /// <param name="PowerDeliveryRequest">A power delivery request to compare with.</param>
        public override Boolean Equals(PowerDeliveryRequest? PowerDeliveryRequest)

            => PowerDeliveryRequest is not null &&

               EVProcessing.  Equals(PowerDeliveryRequest.EVProcessing)   &&
               ChargeProgress.Equals(PowerDeliveryRequest.ChargeProgress) &&

             ((EVPowerProfile      is     null &&  PowerDeliveryRequest.EVPowerProfile      is     null) ||
              (EVPowerProfile      is not null &&  PowerDeliveryRequest.EVPowerProfile      is not null && EVPowerProfile.           Equals(PowerDeliveryRequest.EVPowerProfile)))              &&

            ((!BPTChannelSelection.HasValue    && !PowerDeliveryRequest.BPTChannelSelection.HasValue)    ||
              (BPTChannelSelection.HasValue    &&  PowerDeliveryRequest.BPTChannelSelection.HasValue    && BPTChannelSelection.Value.Equals(PowerDeliveryRequest.BPTChannelSelection.  Value))) &&

               base.          Equals(PowerDeliveryRequest);

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

                return EVProcessing.        GetHashCode()       * 11 ^
                       ChargeProgress.      GetHashCode()       *  7 ^
                      (EVPowerProfile?.     GetHashCode() ?? 0) *  5 ^
                      (BPTChannelSelection?.GetHashCode() ?? 0) *  3 ^

                       base.                GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   EVProcessing.AsText(),
                   ", ",
                   ChargeProgress.AsText(),

                   EVPowerProfile is not null
                       ? ", " + EVPowerProfile
                       : "",

                   BPTChannelSelection.HasValue
                       ? ", " + BPTChannelSelection.Value.AsText()
                       : ""

               );

        #endregion

    }

}
