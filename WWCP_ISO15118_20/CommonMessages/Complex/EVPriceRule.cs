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
    /// The EV price rule.
    /// </summary>
    public class EVPriceRule : IEquatable<EVPriceRule>
    {

        #region Properties

        /// <summary>
        /// The energy fee.
        /// </summary>
        [Mandatory]
        public RationalNumber  EnergyFee          { get; }

        /// <summary>
        /// The power range start.
        /// </summary>
        [Mandatory]
        public RationalNumber  PowerRangeStart    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new EV price rule.
        /// </summary>
        /// <param name="EnergyFee">The energy fee.</param>
        /// <param name="PowerRangeStart">The power range start.</param>
        public EVPriceRule(RationalNumber  EnergyFee,
                           RationalNumber  PowerRangeStart)
        {

            this.EnergyFee        = EnergyFee;
            this.PowerRangeStart  = PowerRangeStart;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="EVPriceRuleType">
        //     <xs:sequence>
        //         <xs:element name="EnergyFee"       type="v2gci_ct:RationalNumberType"/>
        //         <xs:element name="PowerRangeStart" type="v2gci_ct:RationalNumberType"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomEVPriceRuleParser = null)

        /// <summary>
        /// Parse the given JSON representation of an EV price rule.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomEVPriceRuleParser">An optional delegate to parse custom EV price rules.</param>
        public static EVPriceRule Parse(JObject                                    JSON,
                                        CustomJObjectParserDelegate<EVPriceRule>?  CustomEVPriceRuleParser   = null)
        {

            if (TryParse(JSON,
                         out var evPriceRule,
                         out var errorResponse,
                         CustomEVPriceRuleParser))
            {
                return evPriceRule!;
            }

            throw new ArgumentException("The given JSON representation of an EV price rule is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out EVPriceRule, out ErrorResponse, CustomEVPriceRuleParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of an EV price rule.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EVPriceRule">The parsed EV price rule.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject           JSON,
                                       out EVPriceRule?  EVPriceRule,
                                       out String?       ErrorResponse)

            => TryParse(JSON,
                        out EVPriceRule,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an EV price rule.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EVPriceRule">The parsed EV price rule.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomEVPriceRuleParser">An optional delegate to parse custom EV price rules.</param>
        public static Boolean TryParse(JObject                                    JSON,
                                       out EVPriceRule?                           EVPriceRule,
                                       out String?                                ErrorResponse,
                                       CustomJObjectParserDelegate<EVPriceRule>?  CustomEVPriceRuleParser)
        {

            try
            {

                EVPriceRule = null;

                #region EnergyFee          [mandatory]

                if (!JSON.ParseMandatoryJSON("EnergyFee",
                                             "energy fee",
                                             RationalNumber.TryParse,
                                             out RationalNumber EnergyFee,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region PowerRangeStart    [mandatory]

                if (!JSON.ParseMandatoryJSON("PowerRangeStart",
                                             "power range start",
                                             RationalNumber.TryParse,
                                             out RationalNumber PowerRangeStart,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion


                EVPriceRule = new EVPriceRule(EnergyFee,
                                              PowerRangeStart);

                if (CustomEVPriceRuleParser is not null)
                    EVPriceRule = CustomEVPriceRuleParser(JSON,
                                                          EVPriceRule);

                return true;

            }
            catch (Exception e)
            {
                EVPriceRule    = null;
                ErrorResponse  = "The given JSON representation of an EV price rule is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomEVPriceRuleSerializer = null, CustomRationalNumberSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomEVPriceRuleSerializer">A delegate to serialize custom EV price rules.</param>
        /// <param name="CustomRationalNumberSerializer">A delegate to serialize custom rational numbers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<EVPriceRule>?     CustomEVPriceRuleSerializer      = null,
                              CustomJObjectSerializerDelegate<RationalNumber>?  CustomRationalNumberSerializer   = null)
        {

            var json = JSONObject.Create(

                           new JProperty("energyFee",        EnergyFee.      ToJSON(CustomRationalNumberSerializer)),
                           new JProperty("powerRangeStart",  PowerRangeStart.ToJSON(CustomRationalNumberSerializer))

                       );

            return CustomEVPriceRuleSerializer is not null
                       ? CustomEVPriceRuleSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (EVPriceRule1, EVPriceRule2)

        /// <summary>
        /// Compares two EV price rules for equality.
        /// </summary>
        /// <param name="EVPriceRule1">An EV price rule.</param>
        /// <param name="EVPriceRule2">Another EV price rule.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EVPriceRule? EVPriceRule1,
                                           EVPriceRule? EVPriceRule2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(EVPriceRule1, EVPriceRule2))
                return true;

            // If one is null, but not both, return false.
            if (EVPriceRule1 is null || EVPriceRule2 is null)
                return false;

            return EVPriceRule1.Equals(EVPriceRule2);

        }

        #endregion

        #region Operator != (EVPriceRule1, EVPriceRule2)

        /// <summary>
        /// Compares two EV price rules for inequality.
        /// </summary>
        /// <param name="EVPriceRule1">An EV price rule.</param>
        /// <param name="EVPriceRule2">Another EV price rule.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EVPriceRule? EVPriceRule1,
                                           EVPriceRule? EVPriceRule2)

            => !(EVPriceRule1 == EVPriceRule2);

        #endregion

        #endregion

        #region IEquatable<EVPriceRule> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two EV price rules for equality.
        /// </summary>
        /// <param name="Object">An EV price rule to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EVPriceRule evPriceRule &&
                   Equals(evPriceRule);

        #endregion

        #region Equals(EVPriceRule)

        /// <summary>
        /// Compares two EV price rules for equality.
        /// </summary>
        /// <param name="EVPriceRule">An EV price rule to compare with.</param>
        public Boolean Equals(EVPriceRule? EVPriceRule)

            => EVPriceRule is not null &&

               EnergyFee.      Equals(EVPriceRule.EnergyFee) &&
               PowerRangeStart.Equals(EVPriceRule.PowerRangeStart);

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

                return EnergyFee.      GetHashCode() * 5 ^
                       PowerRangeStart.GetHashCode() * 3 ^

                       base.           GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"{EnergyFee} >= {PowerRangeStart} kW";

        #endregion

    }

}
