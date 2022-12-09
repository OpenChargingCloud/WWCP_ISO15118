﻿/*
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
    /// The overstay rule.
    /// </summary>
    public class OverstayRule : IEquatable<OverstayRule>
    {

        #region Properties

        /// <summary>
        /// The start time.
        /// </summary>
        [Mandatory]
        public DateTime        StartTime      { get; }

        /// <summary>
        /// The overstay fee period.
        /// </summary>
        [Mandatory]
        public TimeSpan        Period         { get; }

        /// <summary>
        /// The overstay fee.
        /// </summary>
        [Mandatory]
        public RationalNumber  Fee            { get; }

        /// <summary>
        /// The optional description.
        /// </summary>
        [Optional]
        public Description?    Description    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new overstay rule.
        /// </summary>
        /// <param name="StartTime">A start time.</param>
        /// <param name="Period">An overstay fee period.</param>
        /// <param name="Fee">An overstay fee.</param>
        /// <param name="Description">An optional description.</param>
        public OverstayRule(DateTime        StartTime,
                            TimeSpan        Period,
                            RationalNumber  Fee,
                            Description?    Description   = null)
        {

            this.StartTime    = StartTime;
            this.Period       = Period;
            this.Fee          = Fee;
            this.Description  = Description;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="OverstayRuleType">
        //     <xs:sequence>
        //         <xs:element name="OverstayRuleDescription" type="v2gci_ct:descriptionType" minOccurs="0"/>
        //         <xs:element name="StartTime"               type="xs:unsignedInt"/>
        //         <xs:element name="OverstayFee"             type="v2gci_ct:RationalNumberType"/>
        //         <xs:element name="OverstayFeePeriod"       type="xs:unsignedInt"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomOverstayRuleParser = null)

        /// <summary>
        /// Parse the given JSON representation of an overstay rule.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomOverstayRuleParser">A delegate to parse custom overstay rules.</param>
        public static OverstayRule Parse(JObject                                     JSON,
                                         CustomJObjectParserDelegate<OverstayRule>?  CustomOverstayRuleParser   = null)
        {

            if (TryParse(JSON,
                         out var overstayRule,
                         out var errorResponse,
                         CustomOverstayRuleParser))
            {
                return overstayRule!;
            }

            throw new ArgumentException("The given JSON representation of an overstay rule is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out OverstayRule, out ErrorResponse, CustomOverstayRuleParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of an overstay rule.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="OverstayRule">The parsed overstay rule.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject            JSON,
                                       out OverstayRule?  OverstayRule,
                                       out String?        ErrorResponse)

            => TryParse(JSON,
                        out OverstayRule,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an overstay rule.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="OverstayRule">The parsed overstay rule.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomOverstayRuleParser">A delegate to parse custom contract certificates.</param>
        public static Boolean TryParse(JObject                                     JSON,
                                       out OverstayRule?                           OverstayRule,
                                       out String?                                 ErrorResponse,
                                       CustomJObjectParserDelegate<OverstayRule>?  CustomOverstayRuleParser)
        {

            try
            {

                OverstayRule = null;

                #region StartTime      [mandatory]

                if (!JSON.ParseMandatory("startTime",
                                         "start time",
                                         out DateTime StartTime,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Period         [mandatory]

                if (!JSON.ParseMandatory("period",
                                         "overstay fee period",
                                         out UInt64 period,
                                         out ErrorResponse))
                {
                    return false;
                }

                var Period = TimeSpan.FromSeconds(period);

                #endregion

                #region Fee            [mandatory]

                if (!JSON.ParseMandatoryJSON("fee",
                                             "overstay fee",
                                             RationalNumber.TryParse,
                                             out RationalNumber Fee,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Description    [optional]

                if (JSON.ParseOptional("description",
                                       "overstay fee description",
                                       CommonTypes.Description.TryParse,
                                       out Description Description,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                OverstayRule = new OverstayRule(StartTime,
                                                Period,
                                                Fee,
                                                Description);

                if (CustomOverstayRuleParser is not null)
                    OverstayRule = CustomOverstayRuleParser(JSON,
                                                            OverstayRule);

                return true;

            }
            catch (Exception e)
            {
                OverstayRule   = null;
                ErrorResponse  = "The given JSON representation of an overstay rule is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomOverstayRuleSerializer = null,CustomRationalNumberSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomOverstayRuleSerializer">A delegate to serialize custom overstay rules.</param>
        /// <param name="CustomRationalNumberSerializer">A delegate to serialize custom rational numbers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<OverstayRule>?    CustomOverstayRuleSerializer     = null,
                              CustomJObjectSerializerDelegate<RationalNumber>?  CustomRationalNumberSerializer   = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("startTime",    StartTime.ToIso8601()),
                                 new JProperty("period",       (UInt64) Math.Round(Period.TotalSeconds, 0)),
                                 new JProperty("fee",          Fee.ToJSON(CustomRationalNumberSerializer)),

                           Description.HasValue
                               ? new JProperty("description",  Description.ToString())
                               : null

                       );

            return CustomOverstayRuleSerializer is not null
                       ? CustomOverstayRuleSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (OverstayRule1, OverstayRule2)

        /// <summary>
        /// Compares two overstay rules for equality.
        /// </summary>
        /// <param name="OverstayRule1">An overstay rule.</param>
        /// <param name="OverstayRule2">Another overstay rule.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (OverstayRule? OverstayRule1,
                                           OverstayRule? OverstayRule2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(OverstayRule1, OverstayRule2))
                return true;

            // If one is null, but not both, return false.
            if (OverstayRule1 is null || OverstayRule2 is null)
                return false;

            return OverstayRule1.Equals(OverstayRule2);

        }

        #endregion

        #region Operator != (OverstayRule1, OverstayRule2)

        /// <summary>
        /// Compares two overstay rules for inequality.
        /// </summary>
        /// <param name="OverstayRule1">An overstay rule.</param>
        /// <param name="OverstayRule2">Another overstay rule.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (OverstayRule? OverstayRule1,
                                           OverstayRule? OverstayRule2)

            => !(OverstayRule1 == OverstayRule2);

        #endregion

        #endregion

        #region IEquatable<OverstayRule> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two overstay rules for equality.
        /// </summary>
        /// <param name="Object">An overstay rule to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is OverstayRule overstayRule &&
                   Equals(overstayRule);

        #endregion

        #region Equals(OverstayRule)

        /// <summary>
        /// Compares two overstay rules for equality.
        /// </summary>
        /// <param name="OverstayRule">An overstay rule to compare with.</param>
        public Boolean Equals(OverstayRule? OverstayRule)

            => OverstayRule is not null &&

               StartTime.Equals(OverstayRule.StartTime) &&
               Period.   Equals(OverstayRule.Period)    &&
               Fee.      Equals(OverstayRule.Fee)       &&

            ((!Description.HasValue && !OverstayRule.Description.HasValue) ||
              (Description.HasValue &&  OverstayRule.Description.HasValue && Description.Value.Equals(OverstayRule.Description.Value)));

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

                return StartTime.   GetHashCode()       * 11 ^
                       Period.      GetHashCode()       *  7 ^
                       Fee.         GetHashCode()       *  5 ^
                      (Description?.GetHashCode() ?? 0) *  3 ^

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

                   StartTime.ToIso8601(),
                   ", ",
                   Period.TotalSeconds,
                   " second(s), ",
                   Fee,
                   " fee",

                   Description.HasValue
                       ? ", description: \"" + Description.Value + "\""
                       : ""

               );

        #endregion

    }

}
