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
    /// The EV price rule stack.
    /// </summary>
    public class EVPriceRuleStack : IEquatable<EVPriceRuleStack>
    {

        #region Properties

        /// <summary>
        /// The duration.
        /// </summary>
        [Mandatory]
        public TimeSpan                  Duration        { get; }

        /// <summary>
        /// The enumeration of EV price rules.
        /// [max 8]
        /// </summary>
        [Mandatory]
        public IEnumerable<EVPriceRule>  EVPriceRules    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new EV price rule stack.
        /// </summary>
        /// <param name="Duration">The duration.</param>
        /// <param name="EVPriceRules">An enumeration of EV price rules.</param>
        public EVPriceRuleStack(TimeSpan                  Duration,
                                IEnumerable<EVPriceRule>  EVPriceRules)
        {

            this.Duration      = Duration;
            this.EVPriceRules  = EVPriceRules.Distinct();

        }

        #endregion


        #region Documentation

        // <xs:complexType name="EVPriceRuleStackType">
        //     <xs:sequence>
        //         <xs:element name="Duration"    type="xs:unsignedInt"/>
        //         <xs:element name="EVPriceRule" type="EVPriceRuleType" maxOccurs="8"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomEVPriceRuleStackParser = null)

        /// <summary>
        /// Parse the given JSON representation of an EV price rule stack.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomEVPriceRuleStackParser">A delegate to parse custom EV price rule stacks.</param>
        public static EVPriceRuleStack Parse(JObject                                         JSON,
                                             CustomJObjectParserDelegate<EVPriceRuleStack>?  CustomEVPriceRuleStackParser   = null)
        {

            if (TryParse(JSON,
                         out var evPriceRuleStack,
                         out var errorResponse,
                         CustomEVPriceRuleStackParser))
            {
                return evPriceRuleStack!;
            }

            throw new ArgumentException("The given JSON representation of an EV price rule stack is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out EVPriceRuleStack, out ErrorResponse, CustomEVPriceRuleStackParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of an EV price rule stack.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EVPriceRuleStack">The parsed EV price rule stack.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                JSON,
                                       out EVPriceRuleStack?  EVPriceRuleStack,
                                       out String?            ErrorResponse)

            => TryParse(JSON,
                        out EVPriceRuleStack,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an EV price rule stack.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EVPriceRuleStack">The parsed EV price rule stack.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomEVPriceRuleStackParser">A delegate to parse custom EV price rule stacks.</param>
        public static Boolean TryParse(JObject                                         JSON,
                                       out EVPriceRuleStack?                           EVPriceRuleStack,
                                       out String?                                     ErrorResponse,
                                       CustomJObjectParserDelegate<EVPriceRuleStack>?  CustomEVPriceRuleStackParser)
        {

            try
            {

                EVPriceRuleStack = null;

                #region Duration        [mandatory]

                if (!JSON.ParseMandatory("duration",
                                         "duration",
                                         out UInt64 duration,
                                         out ErrorResponse))
                {
                    return false;
                }

                var Duration = TimeSpan.FromSeconds(duration);

                #endregion

                #region EVPriceRules    [optional]

                if (!JSON.ParseMandatoryHashSet("evPriceRules",
                                                "EV price rules",
                                                EVPriceRule.TryParse,
                                                out HashSet<EVPriceRule> EVPriceRules,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion


                EVPriceRuleStack = new EVPriceRuleStack(Duration,
                                                        EVPriceRules);

                if (CustomEVPriceRuleStackParser is not null)
                    EVPriceRuleStack = CustomEVPriceRuleStackParser(JSON,
                                                                    EVPriceRuleStack);

                return true;

            }
            catch (Exception e)
            {
                EVPriceRuleStack  = null;
                ErrorResponse     = "The given JSON representation of an EV price rule stack is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomEVPriceRuleStackSerializer = null, CustomEVPriceRuleSerializer = null, ...)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomEVPriceRuleStackSerializer">A delegate to serialize custom EV price rule stacks.</param>
        /// <param name="CustomEVPriceRuleSerializer">A delegate to serialize custom EV price rules.</param>
        /// <param name="CustomRationalNumberSerializer">A delegate to serialize custom rational numbers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<EVPriceRuleStack>?  CustomEVPriceRuleStackSerializer   = null,
                              CustomJObjectSerializerDelegate<EVPriceRule>?       CustomEVPriceRuleSerializer        = null,
                              CustomJObjectSerializerDelegate<RationalNumber>?    CustomRationalNumberSerializer     = null)
        {

            var json = JSONObject.Create(

                           new JProperty("duration",      (UInt64) Math.Round(Duration.TotalSeconds, 0)),
                           new JProperty("evPriceRules",  new JArray(EVPriceRules.Select(evPriceRule => evPriceRule.ToJSON(CustomEVPriceRuleSerializer,
                                                                                                                           CustomRationalNumberSerializer))))

                       );

            return CustomEVPriceRuleStackSerializer is not null
                       ? CustomEVPriceRuleStackSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (EVPriceRuleStack1, EVPriceRuleStack2)

        /// <summary>
        /// Compares two EV price rule stacks for equality.
        /// </summary>
        /// <param name="EVPriceRuleStack1">An EV price rule stack.</param>
        /// <param name="EVPriceRuleStack2">Another EV price rule stack.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EVPriceRuleStack? EVPriceRuleStack1,
                                           EVPriceRuleStack? EVPriceRuleStack2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(EVPriceRuleStack1, EVPriceRuleStack2))
                return true;

            // If one is null, but not both, return false.
            if (EVPriceRuleStack1 is null || EVPriceRuleStack2 is null)
                return false;

            return EVPriceRuleStack1.Equals(EVPriceRuleStack2);

        }

        #endregion

        #region Operator != (EVPriceRuleStack1, EVPriceRuleStack2)

        /// <summary>
        /// Compares two EV price rule stacks for inequality.
        /// </summary>
        /// <param name="EVPriceRuleStack1">An EV price rule stack.</param>
        /// <param name="EVPriceRuleStack2">Another EV price rule stack.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EVPriceRuleStack? EVPriceRuleStack1,
                                           EVPriceRuleStack? EVPriceRuleStack2)

            => !(EVPriceRuleStack1 == EVPriceRuleStack2);

        #endregion

        #endregion

        #region IEquatable<EVPriceRuleStack> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two EV price rule stacks for equality.
        /// </summary>
        /// <param name="Object">An EV price rule stack to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EVPriceRuleStack evPriceRuleStack &&
                   Equals(evPriceRuleStack);

        #endregion

        #region Equals(EVPriceRuleStack)

        /// <summary>
        /// Compares two EV price rule stacks for equality.
        /// </summary>
        /// <param name="EVPriceRuleStack">An EV price rule stack to compare with.</param>
        public Boolean Equals(EVPriceRuleStack? EVPriceRuleStack)

            => EVPriceRuleStack is not null &&

               Duration.Equals(EVPriceRuleStack.Duration) &&

               EVPriceRules.Count().Equals(EVPriceRuleStack.EVPriceRules.Count()) &&
               EVPriceRules.All(evPriceRule => EVPriceRuleStack.EVPriceRules.Contains(evPriceRule));

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

                return Duration.    GetHashCode()  * 5 ^
                       EVPriceRules.CalcHashCode() * 3 ^

                       base.        GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   Duration,
                   " second(s), ",

                   EVPriceRules.Count(),
                   " EV price rule(s)"

               );

        #endregion

    }

}
