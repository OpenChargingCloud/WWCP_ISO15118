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
    /// The power schedule entry.
    /// </summary>
    public class PowerScheduleEntry : IEquatable<PowerScheduleEntry>
    {

        #region Properties

        /// <summary>
        /// The duration.
        /// </summary>
        [Mandatory]
        public TimeSpan         Duration    { get; }

        /// <summary>
        /// The power.
        /// </summary>
        [Mandatory]
        public RationalNumber   Power       { get; }

        /// <summary>
        /// The power on L2.
        /// </summary>
        [Optional]
        public RationalNumber?  PowerL2     { get; }

        /// <summary>
        /// The power on L3.
        /// </summary>
        [Optional]
        public RationalNumber?  PowerL3     { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new power schedule entry.
        /// </summary>
        /// <param name="Duration">The duration.</param>
        /// <param name="Power">The power.</param>
        /// <param name="PowerL2">The power on L2.</param>
        /// <param name="PowerL3">The power on L3.</param>
        public PowerScheduleEntry(TimeSpan         Duration,
                                  RationalNumber   Power,
                                  RationalNumber?  PowerL2   = null,
                                  RationalNumber?  PowerL3   = null)
        {

            this.Duration  = Duration;
            this.Power     = Power;
            this.PowerL2   = PowerL2;
            this.PowerL3   = PowerL3;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="PowerScheduleEntryType">
        //     <xs:sequence>
        //         <xs:element name="Duration" type="xs:unsignedInt"/>
        //         <xs:element name="Power"    type="v2gci_ct:RationalNumberType"/>
        //         <xs:element name="Power_L2" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //         <xs:element name="Power_L3" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomPowerScheduleEntryParser = null)

        /// <summary>
        /// Parse the given JSON representation of a power schedule entry.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomPowerScheduleEntryParser">A delegate to parse custom power schedule entries.</param>
        public static PowerScheduleEntry Parse(JObject                                           JSON,
                                               CustomJObjectParserDelegate<PowerScheduleEntry>?  CustomPowerScheduleEntryParser   = null)
        {

            if (TryParse(JSON,
                         out var powerScheduleEntry,
                         out var errorResponse,
                         CustomPowerScheduleEntryParser))
            {
                return powerScheduleEntry!;
            }

            throw new ArgumentException("The given JSON representation of a power schedule entry is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out PowerScheduleEntry, out ErrorResponse, CustomPowerScheduleEntryParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a power schedule entry.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerScheduleEntry">The parsed power schedule entry.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                  JSON,
                                       out PowerScheduleEntry?  PowerScheduleEntry,
                                       out String?              ErrorResponse)

            => TryParse(JSON,
                        out PowerScheduleEntry,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a power schedule entry.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerScheduleEntry">The parsed power schedule entry.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomPowerScheduleEntryParser">A delegate to parse custom power schedule entries.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       out PowerScheduleEntry?                           PowerScheduleEntry,
                                       out String?                                       ErrorResponse,
                                       CustomJObjectParserDelegate<PowerScheduleEntry>?  CustomPowerScheduleEntryParser)
        {

            try
            {

                PowerScheduleEntry = null;

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

                #region PowerL2     [optional]

                if (JSON.ParseOptionalJSON("powerL2",
                                           "power L2",
                                           RationalNumber.TryParse,
                                           out RationalNumber? PowerL2,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region PowerL3     [optional]

                if (JSON.ParseOptionalJSON("powerL3",
                                           "power L3",
                                           RationalNumber.TryParse,
                                           out RationalNumber? PowerL3,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                PowerScheduleEntry = new PowerScheduleEntry(Duration,
                                                            Power,
                                                            PowerL2,
                                                            PowerL3);

                if (CustomPowerScheduleEntryParser is not null)
                    PowerScheduleEntry = CustomPowerScheduleEntryParser(JSON,
                                                                        PowerScheduleEntry);

                return true;

            }
            catch (Exception e)
            {
                PowerScheduleEntry  = null;
                ErrorResponse       = "The given JSON representation of a power schedule entry is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomPowerScheduleEntrySerializer = null, CustomRationalNumberSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomPowerScheduleEntrySerializer">A delegate to serialize custom power schedule entries.</param>
        /// <param name="CustomRationalNumberSerializer">A delegate to serialize custom rational numbers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PowerScheduleEntry>?  CustomPowerScheduleEntrySerializer   = null,
                              CustomJObjectSerializerDelegate<RationalNumber>?      CustomRationalNumberSerializer       = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("duration",  (UInt64) Math.Round(Duration.TotalSeconds, 0)),

                                 new JProperty("power",     Power.        ToJSON(CustomRationalNumberSerializer)),

                           PowerL2 is not null
                               ? new JProperty("powerL2",   PowerL2.Value.ToJSON(CustomRationalNumberSerializer))
                               : null,

                           PowerL3 is not null
                               ? new JProperty("powerL3",   PowerL3.Value.ToJSON(CustomRationalNumberSerializer))
                               : null

                       );

            return CustomPowerScheduleEntrySerializer is not null
                       ? CustomPowerScheduleEntrySerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (PowerScheduleEntry1, PowerScheduleEntry2)

        /// <summary>
        /// Compares two power schedule entries for equality.
        /// </summary>
        /// <param name="PowerScheduleEntry1">A power schedule entry.</param>
        /// <param name="PowerScheduleEntry2">Another power schedule entry.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (PowerScheduleEntry? PowerScheduleEntry1,
                                           PowerScheduleEntry? PowerScheduleEntry2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(PowerScheduleEntry1, PowerScheduleEntry2))
                return true;

            // If one is null, but not both, return false.
            if (PowerScheduleEntry1 is null || PowerScheduleEntry2 is null)
                return false;

            return PowerScheduleEntry1.Equals(PowerScheduleEntry2);

        }

        #endregion

        #region Operator != (PowerScheduleEntry1, PowerScheduleEntry2)

        /// <summary>
        /// Compares two power schedule entries for inequality.
        /// </summary>
        /// <param name="PowerScheduleEntry1">A power schedule entry.</param>
        /// <param name="PowerScheduleEntry2">Another power schedule entry.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (PowerScheduleEntry? PowerScheduleEntry1,
                                           PowerScheduleEntry? PowerScheduleEntry2)

            => !(PowerScheduleEntry1 == PowerScheduleEntry2);

        #endregion

        #endregion

        #region IEquatable<PowerScheduleEntry> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two power schedule entries for equality.
        /// </summary>
        /// <param name="Object">A power schedule entry to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is PowerScheduleEntry powerScheduleEntry &&
                   Equals(powerScheduleEntry);

        #endregion

        #region Equals(PowerScheduleEntry)

        /// <summary>
        /// Compares two power schedule entries for equality.
        /// </summary>
        /// <param name="PowerScheduleEntry">A power schedule entry to compare with.</param>
        public Boolean Equals(PowerScheduleEntry? PowerScheduleEntry)

            => PowerScheduleEntry is not null &&

               Duration.Equals(PowerScheduleEntry.Duration) &&
               Power.   Equals(PowerScheduleEntry.Power)    &&

             ((PowerL2 is     null && PowerScheduleEntry.PowerL2 is     null) ||
              (PowerL2 is not null && PowerScheduleEntry.PowerL2 is not null && PowerL2.Equals(PowerScheduleEntry.PowerL2))) &&

             ((PowerL3 is     null && PowerScheduleEntry.PowerL3 is     null) ||
              (PowerL3 is not null && PowerScheduleEntry.PowerL3 is not null && PowerL3.Equals(PowerScheduleEntry.PowerL3)));

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

                return Duration.GetHashCode()       * 11 ^
                       Power.   GetHashCode()       *  7 ^
                      (PowerL2?.GetHashCode() ?? 0) *  5 ^
                      (PowerL3?.GetHashCode() ?? 0) *  3 ^

                       base.    GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   Duration.TotalSeconds,
                   " second(s), ",

                   Power.ToString(),
                   " kW",
                   PowerL2 is not null || PowerL3 is not null
                       ? " (L1)"
                       : " (common)",

                   PowerL2 is not null
                       ? $", {PowerL2.Value} kW (L2)"
                       : "",

                   PowerL3 is not null
                       ? $", {PowerL3.Value} kW (L3)"
                       : ""

               );

        #endregion

    }

}
