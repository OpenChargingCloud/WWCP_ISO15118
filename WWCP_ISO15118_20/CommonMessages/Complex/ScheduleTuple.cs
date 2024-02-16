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
    /// The (dis-)charging schedule tuple.
    /// </summary>
    public class ScheduleTuple
    {

        #region Properties

        /// <summary>
        /// The unique (dis-)charging schedule tuple identification.
        /// </summary>
        public ScheduleTuple_Id   Id                     { get; }

        /// <summary>
        /// The charging schedule.
        /// </summary>
        [Mandatory]
        public ChargingSchedule   ChargingSchedule       { get; }

        /// <summary>
        /// The optional discharging schedule.
        /// </summary>
        [Optional]
        public ChargingSchedule?  DischargingSchedule    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new (dis-)charging schedule tuple.
        /// </summary>
        /// <param name="Id">An unique (dis-)charging schedule tuple identification.</param>
        /// <param name="ChargingSchedule">A charging schedule.</param>
        /// <param name="DischargingSchedule">An optional discharging schedule.</param>
        public ScheduleTuple(ScheduleTuple_Id   Id,
                             ChargingSchedule   ChargingSchedule,
                             ChargingSchedule?  DischargingSchedule)
        {

            this.Id      = Id;
            this.ChargingSchedule     = ChargingSchedule;
            this.DischargingSchedule  = DischargingSchedule;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="ScheduleTupleType">
        //     <xs:sequence>
        //         <xs:element name="ScheduleTupleID"     type="v2gci_ct:numericIDType"/>
        //         <xs:element name="ChargingSchedule"    type="ChargingScheduleType"/>
        //         <xs:element name="DischargingSchedule" type="ChargingScheduleType" minOccurs="0"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomScheduleTupleParser = null)

        /// <summary>
        /// Parse the given JSON representation of a (dis-)charging schedule tuple.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomScheduleTupleParser">An optional delegate to parse custom (dis-)charging schedule tuples.</param>
        public static ScheduleTuple Parse(JObject                                      JSON,
                                          CustomJObjectParserDelegate<ScheduleTuple>?  CustomScheduleTupleParser   = null)
        {

            if (TryParse(JSON,
                         out var scheduleTuple,
                         out var errorResponse,
                         CustomScheduleTupleParser))
            {
                return scheduleTuple!;
            }

            throw new ArgumentException("The given JSON representation of a (dis-)charging schedule tuple is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out ScheduleTuple, out ErrorResponse, CustomScheduleTupleParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a (dis-)charging schedule tuple.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ScheduleTuple">The parsed (dis-)charging schedule tuple.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject             JSON,
                                       out ScheduleTuple?  ScheduleTuple,
                                       out String?         ErrorResponse)

            => TryParse(JSON,
                        out ScheduleTuple,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a (dis-)charging schedule tuple.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ScheduleTuple">The parsed (dis-)charging schedule tuple.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomScheduleTupleParser">An optional delegate to parse custom contract certificates.</param>
        public static Boolean TryParse(JObject                                      JSON,
                                       out ScheduleTuple?                           ScheduleTuple,
                                       out String?                                  ErrorResponse,
                                       CustomJObjectParserDelegate<ScheduleTuple>?  CustomScheduleTupleParser)
        {

            try
            {

                ScheduleTuple = null;

                #region Id                     [mandatory]

                if (!JSON.ParseMandatory("id",
                                         "(dis-)charging schedule tuple identification",
                                         ScheduleTuple_Id.TryParse,
                                         out ScheduleTuple_Id ScheduleTupleId,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region ChargingSchedule       [mandatory]

                if (!JSON.ParseMandatoryJSON("chargingSchedule",
                                             "charging schedule",
                                             CommonMessages.ChargingSchedule.TryParse,
                                             out ChargingSchedule? ChargingSchedule,
                                             out ErrorResponse))
                {
                    return false;
                }

                if (ChargingSchedule is null)
                    return false;

                #endregion

                #region DischargingSchedule    [optional]

                if (!JSON.ParseOptionalJSON("dischargingSchedule",
                                            "discharging schedule",
                                            CommonMessages.ChargingSchedule.TryParse,
                                            out ChargingSchedule? DischargingSchedule,
                                            out ErrorResponse))
                {
                    return false;
                }

                #endregion


                ScheduleTuple = new ScheduleTuple(ScheduleTupleId,
                                                  ChargingSchedule,
                                                  DischargingSchedule);

                if (CustomScheduleTupleParser is not null)
                    ScheduleTuple = CustomScheduleTupleParser(JSON,
                                                              ScheduleTuple);

                return true;

            }
            catch (Exception e)
            {
                ScheduleTuple  = null;
                ErrorResponse  = "The given JSON representation of a (dis-)charging schedule tuple is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomScheduleTupleSerializer = null, CustomChargingScheduleSerializer = null, ...)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomScheduleTupleSerializer">A delegate to serialize custom (dis-)charging schedule tuples.</param>
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
        public JObject ToJSON(CustomJObjectSerializerDelegate<ScheduleTuple>?            CustomScheduleTupleSerializer             = null,
                              CustomJObjectSerializerDelegate<ChargingSchedule>?         CustomChargingScheduleSerializer          = null,
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

                                 new JProperty("id",                   Id.                 ToString()),

                                 new JProperty("chargingSchedule",     ChargingSchedule.   ToJSON(CustomChargingScheduleSerializer,
                                                                                                  CustomPowerScheduleSerializer,
                                                                                                  CustomPowerScheduleEntrySerializer,
                                                                                                  CustomRationalNumberSerializer,
                                                                                                  CustomAbsolutePriceScheduleSerializer,
                                                                                                  CustomPriceRuleStackSerializer,
                                                                                                  CustomPriceRuleSerializer,
                                                                                                  CustomTaxRuleSerializer,
                                                                                                  CustomOverstayRuleListSerializer,
                                                                                                  CustomOverstayRuleSerializer,
                                                                                                  CustomAdditionalServiceSerializer,
                                                                                                  CustomPriceLevelScheduleSerializer,
                                                                                                  CustomPriceLevelScheduleEntrySerializer)),

                           DischargingSchedule is not null
                               ? new JProperty("dischargingSchedule",  DischargingSchedule.ToJSON(CustomChargingScheduleSerializer,
                                                                                                  CustomPowerScheduleSerializer,
                                                                                                  CustomPowerScheduleEntrySerializer,
                                                                                                  CustomRationalNumberSerializer,
                                                                                                  CustomAbsolutePriceScheduleSerializer,
                                                                                                  CustomPriceRuleStackSerializer,
                                                                                                  CustomPriceRuleSerializer,
                                                                                                  CustomTaxRuleSerializer,
                                                                                                  CustomOverstayRuleListSerializer,
                                                                                                  CustomOverstayRuleSerializer,
                                                                                                  CustomAdditionalServiceSerializer,
                                                                                                  CustomPriceLevelScheduleSerializer,
                                                                                                  CustomPriceLevelScheduleEntrySerializer))
                               : null

                       );

            return CustomScheduleTupleSerializer is not null
                       ? CustomScheduleTupleSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (ScheduleTuple1, ScheduleTuple2)

        /// <summary>
        /// Compares two (dis-)charging schedule tuples for equality.
        /// </summary>
        /// <param name="ScheduleTuple1">A (dis-)charging schedule tuple.</param>
        /// <param name="ScheduleTuple2">Another (dis-)charging schedule tuple.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ScheduleTuple? ScheduleTuple1,
                                           ScheduleTuple? ScheduleTuple2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(ScheduleTuple1, ScheduleTuple2))
                return true;

            // If one is null, but not both, return false.
            if (ScheduleTuple1 is null || ScheduleTuple2 is null)
                return false;

            return ScheduleTuple1.Equals(ScheduleTuple2);

        }

        #endregion

        #region Operator != (ScheduleTuple1, ScheduleTuple2)

        /// <summary>
        /// Compares two (dis-)charging schedule tuples for inequality.
        /// </summary>
        /// <param name="ScheduleTuple1">A (dis-)charging schedule tuple.</param>
        /// <param name="ScheduleTuple2">Another (dis-)charging schedule tuple.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ScheduleTuple? ScheduleTuple1,
                                           ScheduleTuple? ScheduleTuple2)

            => !(ScheduleTuple1 == ScheduleTuple2);

        #endregion

        #endregion

        #region IEquatable<ScheduleTuple> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two (dis-)charging schedule tuples for equality.
        /// </summary>
        /// <param name="Object">A (dis-)charging schedule tuple to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ScheduleTuple scheduleTuple &&
                   Equals(scheduleTuple);

        #endregion

        #region Equals(ScheduleTuple)

        /// <summary>
        /// Compares two (dis-)charging schedule tuples for equality.
        /// </summary>
        /// <param name="ScheduleTuple">A (dis-)charging schedule tuple to compare with.</param>
        public Boolean Equals(ScheduleTuple? ScheduleTuple)

            => ScheduleTuple is not null &&

               Id.              Equals(ScheduleTuple.Id)               &&
               ChargingSchedule.Equals(ScheduleTuple.ChargingSchedule) &&

             ((DischargingSchedule is     null &&  ScheduleTuple.DischargingSchedule is     null) ||
              (DischargingSchedule is not null &&  ScheduleTuple.DischargingSchedule is not null && DischargingSchedule.Equals(ScheduleTuple.DischargingSchedule)));

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

                return Id.                  GetHashCode()       * 7 ^
                       ChargingSchedule.    GetHashCode()       * 5 ^
                      (DischargingSchedule?.GetHashCode() ?? 0) * 3 ^

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

                   Id,
                   ": ",
                   ChargingSchedule,

                   DischargingSchedule is not null
                       ? ", discharge: " + DischargingSchedule
                       : ""

               );

        #endregion

    }

}
