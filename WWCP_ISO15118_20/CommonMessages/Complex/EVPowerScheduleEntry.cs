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
    /// The EV power schedule entry.
    /// </summary>
    public class EVPowerScheduleEntry : IEquatable<EVPowerScheduleEntry>
    {

        #region Properties

        /// <summary>
        /// The duration.
        /// </summary>
        [Mandatory]
        public TimeSpan        Duration    { get; }

        /// <summary>
        /// The power.
        /// </summary>
        [Mandatory]
        public RationalNumber  Power       { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new EV power schedule entry.
        /// </summary>
        /// <param name="Duration"></param>
        /// <param name="Power"></param>
        public EVPowerScheduleEntry(TimeSpan        Duration,
                                    RationalNumber  Power)
        {

            this.Duration  = Duration;
            this.Power     = Power;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="EVPowerScheduleEntryType">
        //     <xs:sequence>
        //         <xs:element name="Duration" type="xs:unsignedInt"/>
        //         <xs:element name="Power"    type="v2gci_ct:RationalNumberType"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomEVPowerScheduleEntryParser = null)

        /// <summary>
        /// Parse the given JSON representation of an EV power schedule entry.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomEVPowerScheduleEntryParser">An optional delegate to parse custom EV power schedule entries.</param>
        public static EVPowerScheduleEntry Parse(JObject                                             JSON,
                                                 CustomJObjectParserDelegate<EVPowerScheduleEntry>?  CustomEVPowerScheduleEntryParser   = null)
        {

            if (TryParse(JSON,
                         out var evPowerScheduleEntry,
                         out var errorResponse,
                         CustomEVPowerScheduleEntryParser))
            {
                return evPowerScheduleEntry!;
            }

            throw new ArgumentException("The given JSON representation of an EV power schedule entry is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out EVPowerScheduleEntry, out ErrorResponse, CustomEVPowerScheduleEntryParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of an EV power schedule entry.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EVPowerScheduleEntry">The parsed EV power schedule entry.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                    JSON,
                                       out EVPowerScheduleEntry?  EVPowerScheduleEntry,
                                       out String?                ErrorResponse)

            => TryParse(JSON,
                        out EVPowerScheduleEntry,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an EV power schedule entry.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EVPowerScheduleEntry">The parsed EV power schedule entry.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomEVPowerScheduleEntryParser">An optional delegate to parse custom EV power schedule entries.</param>
        public static Boolean TryParse(JObject                                             JSON,
                                       out EVPowerScheduleEntry?                           EVPowerScheduleEntry,
                                       out String?                                         ErrorResponse,
                                       CustomJObjectParserDelegate<EVPowerScheduleEntry>?  CustomEVPowerScheduleEntryParser)
        {

            try
            {

                EVPowerScheduleEntry = null;

                #region Duration    [mandatory]

                if (!JSON.ParseMandatory("duration",
                                         "duration",
                                         out UInt64 duration,
                                         out ErrorResponse))
                {
                    return false;
                }

                var Duration = TimeSpan.FromSeconds(duration);

                #endregion

                #region Power       [mandatory]

                if (!JSON.ParseMandatoryJSON("power",
                                             "power",
                                             RationalNumber.TryParse,
                                             out RationalNumber Power,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion


                EVPowerScheduleEntry = new EVPowerScheduleEntry(Duration,
                                                                Power);

                if (CustomEVPowerScheduleEntryParser is not null)
                    EVPowerScheduleEntry = CustomEVPowerScheduleEntryParser(JSON,
                                                                            EVPowerScheduleEntry);

                return true;

            }
            catch (Exception e)
            {
                EVPowerScheduleEntry  = null;
                ErrorResponse           = "The given JSON representation of an EV power schedule entry is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomEVPowerScheduleEntrySerializer = null, CustomRationalNumberSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomEVPowerScheduleEntrySerializer">A delegate to serialize custom EV power schedule entries.</param>
        /// <param name="CustomRationalNumberSerializer">A delegate to serialize custom rational numbers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<EVPowerScheduleEntry>?  CustomEVPowerScheduleEntrySerializer   = null,
                              CustomJObjectSerializerDelegate<RationalNumber>?        CustomRationalNumberSerializer         = null)
        {

            var json = JSONObject.Create(

                           new JProperty("duration",  (UInt64) Math.Round(Duration.TotalSeconds, 0)),
                           new JProperty("power",     Power.ToJSON(CustomRationalNumberSerializer))

                       );

            return CustomEVPowerScheduleEntrySerializer is not null
                       ? CustomEVPowerScheduleEntrySerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (EVPowerScheduleEntry1, EVPowerScheduleEntry2)

        /// <summary>
        /// Compares two EV power schedule entries for equality.
        /// </summary>
        /// <param name="EVPowerScheduleEntry1">An EV power schedule entry.</param>
        /// <param name="EVPowerScheduleEntry2">Another EV power schedule entry.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EVPowerScheduleEntry? EVPowerScheduleEntry1,
                                           EVPowerScheduleEntry? EVPowerScheduleEntry2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(EVPowerScheduleEntry1, EVPowerScheduleEntry2))
                return true;

            // If one is null, but not both, return false.
            if (EVPowerScheduleEntry1 is null || EVPowerScheduleEntry2 is null)
                return false;

            return EVPowerScheduleEntry1.Equals(EVPowerScheduleEntry2);

        }

        #endregion

        #region Operator != (EVPowerScheduleEntry1, EVPowerScheduleEntry2)

        /// <summary>
        /// Compares two EV power schedule entries for inequality.
        /// </summary>
        /// <param name="EVPowerScheduleEntry1">An EV power schedule entry.</param>
        /// <param name="EVPowerScheduleEntry2">Another EV power schedule entry.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EVPowerScheduleEntry? EVPowerScheduleEntry1,
                                           EVPowerScheduleEntry? EVPowerScheduleEntry2)

            => !(EVPowerScheduleEntry1 == EVPowerScheduleEntry2);

        #endregion

        #endregion

        #region IEquatable<EVPowerScheduleEntry> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two EV power schedule entries for equality.
        /// </summary>
        /// <param name="Object">An EV power schedule entry to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EVPowerScheduleEntry evPowerScheduleEntry &&
                   Equals(evPowerScheduleEntry);

        #endregion

        #region Equals(EVPowerScheduleEntry)

        /// <summary>
        /// Compares two EV power schedule entries for equality.
        /// </summary>
        /// <param name="EVPowerScheduleEntry">An EV power schedule entry to compare with.</param>
        public Boolean Equals(EVPowerScheduleEntry? EVPowerScheduleEntry)

            => EVPowerScheduleEntry is not null &&

               Duration.Equals(EVPowerScheduleEntry.Duration) &&
               Power.   Equals(EVPowerScheduleEntry.Power);

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

                return Duration.GetHashCode() * 5 ^
                       Power.   GetHashCode() * 3 ^

                       base.    GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"{Power} kW for {Duration.TotalSeconds} second(s)";

        #endregion

    }

}
