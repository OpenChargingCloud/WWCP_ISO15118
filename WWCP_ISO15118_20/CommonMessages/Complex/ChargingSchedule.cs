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
    /// The charging schedule.
    /// </summary>
    public class ChargingSchedule : IEquatable<ChargingSchedule>
    {

        #region Properties

        /// <summary>
        /// The power schedule.
        /// </summary>
        [Mandatory]
        public PowerSchedule           PowerSchedule            { get; }

        /// <summary>
        /// The optional absolute price schedule.
        /// </summary>
        [OptionalChoice("priceSchedules")]
        public AbsolutePriceSchedule?  AbsolutePriceSchedule    { get; }

        /// <summary>
        /// The optional price level schedule.
        /// </summary>
        [OptionalChoice("priceSchedules")]
        public PriceLevelSchedule?     PriceLevelSchedule       { get; }

        #endregion

        #region Constructor(s)

        #region (private) ChargingSchedule(PowerSchedule, AbsolutePriceSchedule, PriceLevelSchedule)

        /// <summary>
        /// Create a new charging schedule.
        /// </summary>
        /// <param name="PowerSchedule">A power schedule.</param>
        /// <param name="AbsolutePriceSchedule">An optional absolute price schedule.</param>
        /// <param name="PriceLevelSchedule">An optional price level schedule.</param>
        private ChargingSchedule(PowerSchedule           PowerSchedule,
                                 AbsolutePriceSchedule?  AbsolutePriceSchedule,
                                 PriceLevelSchedule?     PriceLevelSchedule)
        {

            if (AbsolutePriceSchedule is not null && PriceLevelSchedule is not null)
                throw new ArgumentException("Please choose between AbsolutePriceSchedule and PriceLevelSchedule!");

            this.PowerSchedule          = PowerSchedule;
            this.AbsolutePriceSchedule  = AbsolutePriceSchedule;
            this.PriceLevelSchedule     = PriceLevelSchedule;

        }

        #endregion

        #region ChargingSchedule(PowerSchedule)

        /// <summary>
        /// Create a new charging schedule.
        /// </summary>
        /// <param name="PowerSchedule">A power schedule.</param>
        public ChargingSchedule(PowerSchedule PowerSchedule)

            : this(PowerSchedule,
                   null,
                   null)

        { }

        #endregion

        #region ChargingSchedule(PowerSchedule, AbsolutePriceSchedule)

        /// <summary>
        /// Create a new charging schedule.
        /// </summary>
        /// <param name="PowerSchedule">A power schedule.</param>
        /// <param name="AbsolutePriceSchedule">An absolute price schedule.</param>
        public ChargingSchedule(PowerSchedule           PowerSchedule,
                                AbsolutePriceSchedule?  AbsolutePriceSchedule)

            : this(PowerSchedule,
                   AbsolutePriceSchedule,
                   null)

        { }

        #endregion

        #region ChargingSchedule(PowerSchedule, PriceLevelSchedule)

        /// <summary>
        /// Create a new charging schedule.
        /// </summary>
        /// <param name="PowerSchedule">A power schedule.</param>
        /// <param name="PriceLevelSchedule">A price level schedule.</param>
        public ChargingSchedule(PowerSchedule           PowerSchedule,
                                PriceLevelSchedule?     PriceLevelSchedule)

            : this(PowerSchedule,
                   null,
                   PriceLevelSchedule)

        { }

        #endregion

        #endregion


        #region Documentation

        // <xs:complexType name="ChargingScheduleType">
        //     <xs:sequence>
        //
        //         <xs:element name="PowerSchedule" type="PowerScheduleType"/>
        //
        //         <xs:choice minOccurs="0">
        //             <xs:element name="AbsolutePriceSchedule" type="AbsolutePriceScheduleType"/>
        //             <xs:element name="PriceLevelSchedule"    type="PriceLevelScheduleType"/>
        //         </xs:choice>
        //
        //     </xs:sequence>
        // </xs:complexType>

         #endregion

        #region (static) Parse   (JSON, CustomChargingScheduleParser = null)

        /// <summary>
        /// Parse the given JSON representation of a charging schedule.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomChargingScheduleParser">A delegate to parse custom charging schedules.</param>
        public static ChargingSchedule Parse(JObject                                         JSON,
                                             CustomJObjectParserDelegate<ChargingSchedule>?  CustomChargingScheduleParser   = null)
        {

            if (TryParse(JSON,
                         out var chargingSchedule,
                         out var errorResponse,
                         CustomChargingScheduleParser))
            {
                return chargingSchedule!;
            }

            throw new ArgumentException("The given JSON representation of a charging schedule is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out ChargingSchedule, out ErrorResponse, CustomChargingScheduleParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a charging schedule.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ChargingSchedule">The parsed charging schedule.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                JSON,
                                       out ChargingSchedule?  ChargingSchedule,
                                       out String?            ErrorResponse)

            => TryParse(JSON,
                        out ChargingSchedule,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a charging schedule.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ChargingSchedule">The parsed charging schedule.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomChargingScheduleParser">A delegate to parse custom contract certificates.</param>
        public static Boolean TryParse(JObject                                         JSON,
                                       out ChargingSchedule?                           ChargingSchedule,
                                       out String?                                     ErrorResponse,
                                       CustomJObjectParserDelegate<ChargingSchedule>?  CustomChargingScheduleParser)
        {

            try
            {

                ChargingSchedule = null;

                #region PowerSchedule            [mandatory]

                if (!JSON.ParseMandatoryJSON("powerSchedule",
                                             "power schedule",
                                             CommonMessages.PowerSchedule.TryParse,
                                             out PowerSchedule? PowerSchedule,
                                             out ErrorResponse))
                {
                    return false;
                }

                if (PowerSchedule is null)
                    return false;

                #endregion

                #region AbsolutePriceSchedule    [optional]

                if (JSON.ParseOptionalJSON("absolutePriceSchedule",
                                           "absolute price schedule",
                                           CommonMessages.AbsolutePriceSchedule.TryParse,
                                           out AbsolutePriceSchedule? AbsolutePriceSchedule,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
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
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                ChargingSchedule = new ChargingSchedule(PowerSchedule,
                                                        AbsolutePriceSchedule,
                                                        PriceLevelSchedule);

                if (CustomChargingScheduleParser is not null)
                    ChargingSchedule = CustomChargingScheduleParser(JSON,
                                                                    ChargingSchedule);

                return true;

            }
            catch (Exception e)
            {
                ChargingSchedule  = null;
                ErrorResponse     = "The given JSON representation of a charging schedule is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomChargingScheduleSerializer = null, CustomPowerScheduleSerializer = null, ...)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomChargingScheduleSerializer">A delegate to serialize custom charging schedules.</param>
        /// <param name="CustomPowerScheduleSerializer">A delegate to serialize custom power schedules.</param>
        /// <param name="CustomPowerScheduleEntrySerializer">A delegate to serialize custom power schedule entries.</param>
        /// <param name="CustomRationalNumberSerializer">A delegate to serialize custom rational numbers.</param>
        /// <param name="CustomAbsolutePriceScheduleSerializer">A delegate to serialize custom absolute price schedules.</param>
        /// <param name="CustomPriceRuleStackSerializer">A delegate to serialize custom price rule stacks.</param>
        /// <param name="CustomPriceRuleSerializer">A delegate to serialize custom price rules.</param>
        /// <param name="CustomTaxRuleSerializer">A delegate to serialize custom tax rules.</param>
        /// <param name="CustomOverstayRuleListSerializer">A delegate to serialize custom overstay rule lists.</param>
        /// <param name="CustomOverstayRuleSerializer">A delegate to serialize custom overstay rules.</param>
        /// <param name="CustomAdditionalServiceSerializer">A delegate to serialize custom additional services.</param>
        /// <param name="CustomPriceLevelScheduleSerializer">A delegate to serialize custom price level schedules.</param>
        /// <param name="CustomPriceLevelScheduleEntrySerializer">A delegate to serialize custom price level schedule entries.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<ChargingSchedule>?         CustomChargingScheduleSerializer          = null,
                              CustomJObjectSerializerDelegate<PowerSchedule>?            CustomPowerScheduleSerializer             = null,
                              CustomJObjectSerializerDelegate<PowerScheduleEntry>?       CustomPowerScheduleEntrySerializer        = null,
                              CustomJObjectSerializerDelegate<RationalNumber>?           CustomRationalNumberSerializer            = null,
                              CustomJObjectSerializerDelegate<AbsolutePriceSchedule>?    CustomAbsolutePriceScheduleSerializer     = null,
                              CustomJObjectSerializerDelegate<PriceRuleStack>?           CustomPriceRuleStackSerializer            = null,
                              CustomJObjectSerializerDelegate<PriceRule>?                CustomPriceRuleSerializer                 = null,
                              CustomJObjectSerializerDelegate<TaxRule>?                  CustomTaxRuleSerializer                   = null,
                              CustomJObjectSerializerDelegate<OverstayRuleList>?         CustomOverstayRuleListSerializer          = null,
                              CustomJObjectSerializerDelegate<OverstayRule>?             CustomOverstayRuleSerializer              = null,
                              CustomJObjectSerializerDelegate<AdditionalService>?        CustomAdditionalServiceSerializer         = null,
                              CustomJObjectSerializerDelegate<PriceLevelSchedule>?       CustomPriceLevelScheduleSerializer        = null,
                              CustomJObjectSerializerDelegate<PriceLevelScheduleEntry>?  CustomPriceLevelScheduleEntrySerializer   = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("powerSchedule",          PowerSchedule.        ToJSON(CustomPowerScheduleSerializer,
                                                                                                      CustomPowerScheduleEntrySerializer,
                                                                                                      CustomRationalNumberSerializer)),

                           AbsolutePriceSchedule is not null
                               ? new JProperty("absolutePriceSchedule",  AbsolutePriceSchedule.ToJSON(CustomAbsolutePriceScheduleSerializer,
                                                                                                      CustomPriceRuleStackSerializer,
                                                                                                      CustomPriceRuleSerializer,
                                                                                                      CustomRationalNumberSerializer,
                                                                                                      CustomTaxRuleSerializer,
                                                                                                      CustomOverstayRuleListSerializer,
                                                                                                      CustomOverstayRuleSerializer,
                                                                                                      CustomAdditionalServiceSerializer))
                               : null,

                           PriceLevelSchedule is not null
                               ? new JProperty("priceLevelSchedule",     PriceLevelSchedule.   ToJSON(CustomPriceLevelScheduleSerializer,
                                                                                                      CustomPriceLevelScheduleEntrySerializer))
                               : null

                       );

            return CustomChargingScheduleSerializer is not null
                       ? CustomChargingScheduleSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (ChargingSchedule1, ChargingSchedule2)

        /// <summary>
        /// Compares two charging schedules for equality.
        /// </summary>
        /// <param name="ChargingSchedule1">A charging schedule.</param>
        /// <param name="ChargingSchedule2">Another charging schedule.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ChargingSchedule? ChargingSchedule1,
                                           ChargingSchedule? ChargingSchedule2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(ChargingSchedule1, ChargingSchedule2))
                return true;

            // If one is null, but not both, return false.
            if (ChargingSchedule1 is null || ChargingSchedule2 is null)
                return false;

            return ChargingSchedule1.Equals(ChargingSchedule2);

        }

        #endregion

        #region Operator != (ChargingSchedule1, ChargingSchedule2)

        /// <summary>
        /// Compares two charging schedules for inequality.
        /// </summary>
        /// <param name="ChargingSchedule1">A charging schedule.</param>
        /// <param name="ChargingSchedule2">Another charging schedule.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ChargingSchedule? ChargingSchedule1,
                                           ChargingSchedule? ChargingSchedule2)

            => !(ChargingSchedule1 == ChargingSchedule2);

        #endregion

        #endregion

        #region IEquatable<ChargingSchedule> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two charging schedules for equality.
        /// </summary>
        /// <param name="Object">A charging schedule to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ChargingSchedule chargingSchedule &&
                   Equals(chargingSchedule);

        #endregion

        #region Equals(ChargingSchedule)

        /// <summary>
        /// Compares two charging schedules for equality.
        /// </summary>
        /// <param name="ChargingSchedule">A charging schedule to compare with.</param>
        public Boolean Equals(ChargingSchedule? ChargingSchedule)

            => ChargingSchedule is not null &&

               PowerSchedule.Equals(ChargingSchedule.PowerSchedule) &&

             ((AbsolutePriceSchedule is     null && ChargingSchedule.AbsolutePriceSchedule is     null) ||
              (AbsolutePriceSchedule is not null && ChargingSchedule.AbsolutePriceSchedule is not null && AbsolutePriceSchedule.Equals(ChargingSchedule.AbsolutePriceSchedule))) &&

             ((PriceLevelSchedule    is     null && ChargingSchedule.PriceLevelSchedule    is     null) ||
              (PriceLevelSchedule    is not null && ChargingSchedule.PriceLevelSchedule    is not null && PriceLevelSchedule.   Equals(ChargingSchedule.PriceLevelSchedule)));

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

                return PowerSchedule.         GetHashCode()       * 7 ^
                      (AbsolutePriceSchedule?.GetHashCode() ?? 0) * 5 ^
                      (PriceLevelSchedule?.   GetHashCode() ?? 0) * 3 ^

                       base.                  GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   PowerSchedule,

                   AbsolutePriceSchedule is not null
                       ? ", absolute price: " + AbsolutePriceSchedule
                       : "",

                   PriceLevelSchedule is not null
                       ? ", price level: " + PriceLevelSchedule
                       : ""

               );

        #endregion

    }

}
