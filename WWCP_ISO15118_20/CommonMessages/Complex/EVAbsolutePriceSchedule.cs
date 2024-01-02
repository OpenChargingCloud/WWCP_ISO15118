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
    /// The EV absolute price schedule.
    /// </summary>
    public class EVAbsolutePriceSchedule : IEquatable<EVAbsolutePriceSchedule>
    {

        #region Properties

        /// <summary>
        /// The time anchor.
        /// </summary>
        [Mandatory]
        public DateTime                       TimeAnchor           { get; }

        /// <summary>
        /// The currency.
        /// </summary>
        [Mandatory]
        public Currency                       Currency             { get; }

        /// <summary>
        /// The price algorithm.
        /// </summary>
        [Mandatory]
        public PriceAlgorithm_Id              PriceAlgorithm       { get; }

        /// <summary>
        /// The enumeration of EV price rule stacks.
        /// </summary>
        [Mandatory]
        public IEnumerable<EVPriceRuleStack>  EVPriceRuleStacks    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new EV absolute price schedule.
        /// </summary>
        /// <param name="TimeAnchor">A time anchor.</param>
        /// <param name="Currency">A currency.</param>
        /// <param name="PriceAlgorithm">A price algorithm.</param>
        /// <param name="EVPriceRuleStacks">An enumeration of EV price rule stacks (max 1024).</param>
        public EVAbsolutePriceSchedule(DateTime                       TimeAnchor,
                                       Currency                       Currency,
                                       PriceAlgorithm_Id              PriceAlgorithm,
                                       IEnumerable<EVPriceRuleStack>  EVPriceRuleStacks)
        {

            this.TimeAnchor         = TimeAnchor;
            this.Currency           = Currency;
            this.PriceAlgorithm     = PriceAlgorithm;
            this.EVPriceRuleStacks  = EVPriceRuleStacks;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="EVAbsolutePriceScheduleType">
        //     <xs:sequence>
        //         <xs:element name="TimeAnchor"        type="xs:unsignedLong"/>
        //         <xs:element name="Currency"          type="currencyType"/>
        //         <xs:element name="PriceAlgorithm"    type="v2gci_ct:identifierType"/>
        //         <xs:element name="EVPriceRuleStacks" type="EVPriceRuleStackListType"/>
        //     </xs:sequence>
        // </xs:complexType>


        // <xs:complexType name="EVPriceRuleStackListType">
        //     <xs:sequence>
        //         <xs:element name="EVPriceRuleStack" type="EVPriceRuleStackType" maxOccurs="1024"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomEVAbsolutePriceScheduleParser = null)

        /// <summary>
        /// Parse the given JSON representation of an EV absolute price schedule.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomEVAbsolutePriceScheduleParser">A delegate to parse custom EV absolute price schedules.</param>
        public static EVAbsolutePriceSchedule Parse(JObject                                                JSON,
                                                    CustomJObjectParserDelegate<EVAbsolutePriceSchedule>?  CustomEVAbsolutePriceScheduleParser   = null)
        {

            if (TryParse(JSON,
                         out var evAbsolutePriceSchedule,
                         out var errorResponse,
                         CustomEVAbsolutePriceScheduleParser))
            {
                return evAbsolutePriceSchedule!;
            }

            throw new ArgumentException("The given JSON representation of an EV absolute price schedule is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out EVAbsolutePriceSchedule, out ErrorResponse, CustomEVAbsolutePriceScheduleParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of an EV absolute price schedule.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EVAbsolutePriceSchedule">The parsed EV absolute price schedule.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                       JSON,
                                       out EVAbsolutePriceSchedule?  EVAbsolutePriceSchedule,
                                       out String?                   ErrorResponse)

            => TryParse(JSON,
                        out EVAbsolutePriceSchedule,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an EV absolute price schedule.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EVAbsolutePriceSchedule">The parsed EV absolute price schedule.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomEVAbsolutePriceScheduleParser">A delegate to parse custom EV absolute price schedules.</param>
        public static Boolean TryParse(JObject                                                JSON,
                                       out EVAbsolutePriceSchedule?                           EVAbsolutePriceSchedule,
                                       out String?                                            ErrorResponse,
                                       CustomJObjectParserDelegate<EVAbsolutePriceSchedule>?  CustomEVAbsolutePriceScheduleParser)
        {

            try
            {

                EVAbsolutePriceSchedule = null;

                #region TimeAnchor           [mandatory]

                if (!JSON.ParseMandatory("timeAnchor",
                                         "time anchor",
                                         out DateTime TimeAnchor,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Currency             [mandatory]

                if (!JSON.ParseMandatory("currency",
                                         "currency",
                                         org.GraphDefined.Vanaheimr.Illias.Currency.TryParse,
                                         out Currency? Currency,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (Currency is null)
                    return false;

                #endregion

                #region PriceAlgorithm       [mandatory]

                if (!JSON.ParseMandatory("priceAlgorithm",
                                         "price lgorithm",
                                         PriceAlgorithm_Id.TryParse,
                                         out PriceAlgorithm_Id PriceAlgorithm,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region EVPriceRuleStacks    [mandatory]

                if (!JSON.ParseMandatoryJSON("evPriceRuleStack",
                                             "EV price rule stack",
                                             EVPriceRuleStack.TryParse,
                                             out IEnumerable<EVPriceRuleStack>? EVPriceRuleStacks,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion


                EVAbsolutePriceSchedule = new EVAbsolutePriceSchedule(TimeAnchor,
                                                                      Currency,
                                                                      PriceAlgorithm,
                                                                      EVPriceRuleStacks);

                if (CustomEVAbsolutePriceScheduleParser is not null)
                    EVAbsolutePriceSchedule = CustomEVAbsolutePriceScheduleParser(JSON,
                                                                                  EVAbsolutePriceSchedule);

                return true;

            }
            catch (Exception e)
            {
                EVAbsolutePriceSchedule  = null;
                ErrorResponse            = "The given JSON representation of an EV absolute price schedule is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomEVAbsolutePriceScheduleSerializer = null, CustomEVPriceRuleStackSerializer = null, ...)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomEVAbsolutePriceScheduleSerializer">A delegate to serialize custom EV absolute price schedules.</param>
        /// <param name="CustomEVPriceRuleStackSerializer">A delegate to serialize custom EV price rule stacks.</param>
        /// <param name="CustomEVPriceRuleSerializer">A delegate to serialize custom EV price rules.</param>
        /// <param name="CustomRationalNumberSerializer">A delegate to serialize custom rational numbers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<EVAbsolutePriceSchedule>?  CustomEVAbsolutePriceScheduleSerializer   = null,
                              CustomJObjectSerializerDelegate<EVPriceRuleStack>?         CustomEVPriceRuleStackSerializer          = null,
                              CustomJObjectSerializerDelegate<EVPriceRule>?              CustomEVPriceRuleSerializer               = null,
                              CustomJObjectSerializerDelegate<RationalNumber>?           CustomRationalNumberSerializer            = null)
        {

            var json = JSONObject.Create(

                           new JProperty("timeAnchor",         TimeAnchor.ToIso8601()),
                           new JProperty("currency",           Currency.ISOCode),
                           new JProperty("priceAlgorithm",     PriceAlgorithm.ToString()),
                           new JProperty("evPriceRuleStacks",  new JArray(EVPriceRuleStacks.Select(evPriceRuleStack => evPriceRuleStack.ToJSON(CustomEVPriceRuleStackSerializer,
                                                                                                                                               CustomEVPriceRuleSerializer,
                                                                                                                                               CustomRationalNumberSerializer))))

                       );

            return CustomEVAbsolutePriceScheduleSerializer is not null
                       ? CustomEVAbsolutePriceScheduleSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (EVAbsolutePriceSchedule1, EVAbsolutePriceSchedule2)

        /// <summary>
        /// Compares two EV absolute price schedules for equality.
        /// </summary>
        /// <param name="EVAbsolutePriceSchedule1">An EV absolute price schedule.</param>
        /// <param name="EVAbsolutePriceSchedule2">Another EV absolute price schedule.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EVAbsolutePriceSchedule? EVAbsolutePriceSchedule1,
                                           EVAbsolutePriceSchedule? EVAbsolutePriceSchedule2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(EVAbsolutePriceSchedule1, EVAbsolutePriceSchedule2))
                return true;

            // If one is null, but not both, return false.
            if (EVAbsolutePriceSchedule1 is null || EVAbsolutePriceSchedule2 is null)
                return false;

            return EVAbsolutePriceSchedule1.Equals(EVAbsolutePriceSchedule2);

        }

        #endregion

        #region Operator != (EVAbsolutePriceSchedule1, EVAbsolutePriceSchedule2)

        /// <summary>
        /// Compares two EV absolute price schedules for inequality.
        /// </summary>
        /// <param name="EVAbsolutePriceSchedule1">An EV absolute price schedule.</param>
        /// <param name="EVAbsolutePriceSchedule2">Another EV absolute price schedule.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EVAbsolutePriceSchedule? EVAbsolutePriceSchedule1,
                                           EVAbsolutePriceSchedule? EVAbsolutePriceSchedule2)

            => !(EVAbsolutePriceSchedule1 == EVAbsolutePriceSchedule2);

        #endregion

        #endregion

        #region IEquatable<EVAbsolutePriceSchedule> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two EV absolute price schedules for equality.
        /// </summary>
        /// <param name="Object">An EV absolute price schedule to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EVAbsolutePriceSchedule evAbsolutePriceSchedule &&
                   Equals(evAbsolutePriceSchedule);

        #endregion

        #region Equals(EVAbsolutePriceSchedule)

        /// <summary>
        /// Compares two EV absolute price schedules for equality.
        /// </summary>
        /// <param name="EVAbsolutePriceSchedule">An EV absolute price schedule to compare with.</param>
        public Boolean Equals(EVAbsolutePriceSchedule? EVAbsolutePriceSchedule)

            => EVAbsolutePriceSchedule is not null &&

               TimeAnchor.    Equals(EVAbsolutePriceSchedule.TimeAnchor)     &&
               Currency.      Equals(EVAbsolutePriceSchedule.Currency)       &&
               PriceAlgorithm.Equals(EVAbsolutePriceSchedule.PriceAlgorithm) &&

               EVPriceRuleStacks.Count().Equals(EVAbsolutePriceSchedule.EVPriceRuleStacks.Count()) &&
               EVPriceRuleStacks.All(evPriceRuleStack => EVAbsolutePriceSchedule.EVPriceRuleStacks.Contains(evPriceRuleStack));

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

                return TimeAnchor.       GetHashCode()  * 11 ^
                       Currency.         GetHashCode()  *  7 ^
                       PriceAlgorithm.   GetHashCode()  *  5 ^
                       EVPriceRuleStacks.CalcHashCode() *  3 ^

                       base.             GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"{TimeAnchor}, {Currency.ISOCode}, {PriceAlgorithm}, {EVPriceRuleStacks.Count()} EV price rule stack(s)";

        #endregion


    }

}
