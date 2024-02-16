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
    /// An energy offer.
    /// </summary>
    public class EVEnergyOffer
    {

        #region Properties

        /// <summary>
        /// The EV power schedule.
        /// </summary>
        [Mandatory]
        public EVPowerSchedule          EVPowerSchedule            { get; }

        /// <summary>
        /// The EV absolute price schedule.
        /// </summary>
        [Mandatory]
        public EVAbsolutePriceSchedule  EVAbsolutePriceSchedule    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new energy offer.
        /// </summary>
        /// <param name="EVPowerSchedule">An EV power schedule.</param>
        /// <param name="EVAbsolutePriceSchedule">An absolute EV price schedule.</param>
        public EVEnergyOffer(EVPowerSchedule          EVPowerSchedule,
                             EVAbsolutePriceSchedule  EVAbsolutePriceSchedule)
        {

            this.EVPowerSchedule          = EVPowerSchedule;
            this.EVAbsolutePriceSchedule  = EVAbsolutePriceSchedule;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="EVEnergyOfferType">
        //     <xs:sequence>
        //         <xs:element name="EVPowerSchedule"         type="EVPowerScheduleType"/>
        //         <xs:element name="EVAbsolutePriceSchedule" type="EVAbsolutePriceScheduleType"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomEVEnergyOfferParser = null)

        /// <summary>
        /// Parse the given JSON representation of an energy offer.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomEVEnergyOfferParser">An optional delegate to parse custom energy offers.</param>
        public static EVEnergyOffer Parse(JObject                                      JSON,
                                          CustomJObjectParserDelegate<EVEnergyOffer>?  CustomEVEnergyOfferParser   = null)
        {

            if (TryParse(JSON,
                         out var evEnergyOffer,
                         out var errorResponse,
                         CustomEVEnergyOfferParser))
            {
                return evEnergyOffer!;
            }

            throw new ArgumentException("The given JSON representation of an energy offer is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out EVEnergyOffer, out ErrorResponse, CustomEVEnergyOfferParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of an energy offer.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EVEnergyOffer">The parsed energy offer.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject             JSON,
                                       out EVEnergyOffer?  EVEnergyOffer,
                                       out String?         ErrorResponse)

            => TryParse(JSON,
                        out EVEnergyOffer,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an energy offer.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EVEnergyOffer">The parsed energy offer.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomEVEnergyOfferParser">An optional delegate to parse custom contract certificates.</param>
        public static Boolean TryParse(JObject                                      JSON,
                                       out EVEnergyOffer?                           EVEnergyOffer,
                                       out String?                                  ErrorResponse,
                                       CustomJObjectParserDelegate<EVEnergyOffer>?  CustomEVEnergyOfferParser)
        {

            try
            {

                EVEnergyOffer = null;

                #region EVPowerSchedule            [mandatory]

                if (!JSON.ParseMandatoryJSON("evPowerSchedule",
                                             "EV power schedule",
                                             CommonMessages.EVPowerSchedule.TryParse,
                                             out EVPowerSchedule? EVPowerSchedule,
                                             out ErrorResponse) ||
                    EVPowerSchedule is null)
                {
                    return false;
                }

                #endregion

                #region EVAbsolutePriceSchedule    [mandatory]

                if (!JSON.ParseMandatoryJSON("evAbsolutePriceSchedule",
                                             "EV absolute price schedule",
                                             CommonMessages.EVAbsolutePriceSchedule.TryParse,
                                             out EVAbsolutePriceSchedule? EVAbsolutePriceSchedule,
                                             out ErrorResponse) ||
                    EVAbsolutePriceSchedule is null)
                {
                    return false;
                }

                #endregion


                EVEnergyOffer = new EVEnergyOffer(EVPowerSchedule,
                                                  EVAbsolutePriceSchedule);

                if (CustomEVEnergyOfferParser is not null)
                    EVEnergyOffer = CustomEVEnergyOfferParser(JSON,
                                                              EVEnergyOffer);

                return true;

            }
            catch (Exception e)
            {
                EVEnergyOffer  = null;
                ErrorResponse  = "The given JSON representation of an energy offer is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomEVEnergyOfferSerializer = null, CustomEVPowerScheduleSerializer = null, ...)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomEVEnergyOfferSerializer">A delegate to serialize custom energy offers.</param>
        /// <param name="CustomEVPowerScheduleSerializer">A delegate to serialize custom EV power schedules.</param>
        /// <param name="CustomEVPowerScheduleEntrySerializer">A delegate to serialize custom EV power schedule entries.</param>
        /// <param name="CustomRationalNumberSerializer">A delegate to serialize custom rational numbers.</param>
        /// <param name="CustomEVAbsolutePriceScheduleSerializer">A delegate to serialize custom EV absolute price schedules.</param>
        /// <param name="CustomEVPriceRuleStackSerializer">A delegate to serialize custom EV price rule stacks.</param>
        /// <param name="CustomEVPriceRuleSerializer">A delegate to serialize custom EV price rules.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<EVEnergyOffer>?            CustomEVEnergyOfferSerializer             = null,
                              CustomJObjectSerializerDelegate<EVPowerSchedule>?          CustomEVPowerScheduleSerializer           = null,
                              CustomJObjectSerializerDelegate<EVPowerScheduleEntry>?     CustomEVPowerScheduleEntrySerializer      = null,
                              CustomJObjectSerializerDelegate<RationalNumber>?           CustomRationalNumberSerializer            = null,
                              CustomJObjectSerializerDelegate<EVAbsolutePriceSchedule>?  CustomEVAbsolutePriceScheduleSerializer   = null,
                              CustomJObjectSerializerDelegate<EVPriceRuleStack>?         CustomEVPriceRuleStackSerializer          = null,
                              CustomJObjectSerializerDelegate<EVPriceRule>?              CustomEVPriceRuleSerializer               = null)
        {

            var json = JSONObject.Create(

                           new JProperty("evPowerSchedule",          EVPowerSchedule.        ToJSON(CustomEVPowerScheduleSerializer,
                                                                                                    CustomEVPowerScheduleEntrySerializer,
                                                                                                    CustomRationalNumberSerializer)),

                           new JProperty("evAbsolutePriceSchedule",  EVAbsolutePriceSchedule.ToJSON(CustomEVAbsolutePriceScheduleSerializer,
                                                                                                    CustomEVPriceRuleStackSerializer,
                                                                                                    CustomEVPriceRuleSerializer,
                                                                                                    CustomRationalNumberSerializer))

                       );

            return CustomEVEnergyOfferSerializer is not null
                       ? CustomEVEnergyOfferSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (EVEnergyOffer1, EVEnergyOffer2)

        /// <summary>
        /// Compares two energy offers for equality.
        /// </summary>
        /// <param name="EVEnergyOffer1">An energy offer.</param>
        /// <param name="EVEnergyOffer2">Another energy offer.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EVEnergyOffer? EVEnergyOffer1,
                                           EVEnergyOffer? EVEnergyOffer2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(EVEnergyOffer1, EVEnergyOffer2))
                return true;

            // If one is null, but not both, return false.
            if (EVEnergyOffer1 is null || EVEnergyOffer2 is null)
                return false;

            return EVEnergyOffer1.Equals(EVEnergyOffer2);

        }

        #endregion

        #region Operator != (EVEnergyOffer1, EVEnergyOffer2)

        /// <summary>
        /// Compares two energy offers for inequality.
        /// </summary>
        /// <param name="EVEnergyOffer1">An energy offer.</param>
        /// <param name="EVEnergyOffer2">Another energy offer.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EVEnergyOffer? EVEnergyOffer1,
                                           EVEnergyOffer? EVEnergyOffer2)

            => !(EVEnergyOffer1 == EVEnergyOffer2);

        #endregion

        #endregion

        #region IEquatable<EVEnergyOffer> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two energy offers for equality.
        /// </summary>
        /// <param name="Object">An energy offer to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EVEnergyOffer evEnergyOffer &&
                   Equals(evEnergyOffer);

        #endregion

        #region Equals(EVEnergyOffer)

        /// <summary>
        /// Compares two energy offers for equality.
        /// </summary>
        /// <param name="EVEnergyOffer">An energy offer to compare with.</param>
        public Boolean Equals(EVEnergyOffer? EVEnergyOffer)

            => EVEnergyOffer is not null &&

               EVPowerSchedule.        Equals(EVEnergyOffer.EVPowerSchedule) &&
               EVAbsolutePriceSchedule.Equals(EVEnergyOffer.EVAbsolutePriceSchedule);

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

                return EVPowerSchedule.        GetHashCode() * 5 ^
                       EVAbsolutePriceSchedule.GetHashCode() * 3 ^

                       base.                   GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"{EVPowerSchedule}, {EVAbsolutePriceSchedule}";

        #endregion


    }

}
