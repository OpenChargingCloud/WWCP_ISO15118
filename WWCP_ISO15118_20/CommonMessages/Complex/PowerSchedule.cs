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
    /// The power schedule.
    /// </summary>
    public class PowerSchedule : IEquatable<PowerSchedule>
    {

        #region Properties

        /// <summary>
        /// The time anchor.
        /// </summary>
        public DateTime                         TimeAnchor              { get; }

        /// <summary>
        /// The optional available engery.
        /// </summary>
        public RationalNumber?                  AvailableEnergy         { get; }

        /// <summary>
        /// The optional power tolerance.
        /// </summary>
        public RationalNumber?                  PowerTolerance          { get; }

        /// <summary>
        /// The enumeration of power schedule entries.
        /// [max 1024]
        /// </summary>
        public IEnumerable<PowerScheduleEntry>  PowerScheduleEntries    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new power schedule.
        /// </summary>
        /// <param name="TimeAnchor">The time anchor.</param>
        /// <param name="AvailableEnergy">The optional available engery.</param>
        /// <param name="PowerTolerance">The optional power tolerance.</param>
        /// <param name="PowerScheduleEntries">The enumeration of power schedule entries.</param>
        public PowerSchedule(DateTime                         TimeAnchor,
                             RationalNumber?                  AvailableEnergy,
                             RationalNumber?                  PowerTolerance,
                             IEnumerable<PowerScheduleEntry>  PowerScheduleEntries)
        {

            this.TimeAnchor            = TimeAnchor;
            this.AvailableEnergy       = AvailableEnergy;
            this.PowerTolerance        = PowerTolerance;
            this.PowerScheduleEntries  = PowerScheduleEntries;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="PowerScheduleType">
        //     <xs:sequence>
        //         <xs:element name="TimeAnchor"           type="xs:unsignedLong"/>
        //         <xs:element name="AvailableEnergy"      type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //         <xs:element name="PowerTolerance"       type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //         <xs:element name="PowerScheduleEntries" type="PowerScheduleEntryListType"/>
        //     </xs:sequence>
        // </xs:complexType>


        // <xs:complexType name="PowerScheduleEntryListType">
        //     <xs:sequence>
        //         <xs:element name="PowerScheduleEntry" type="PowerScheduleEntryType" maxOccurs="1024"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomPowerScheduleParser = null)

        /// <summary>
        /// Parse the given JSON representation of a power schedule.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomPowerScheduleParser">An optional delegate to parse custom power schedules.</param>
        public static PowerSchedule Parse(JObject                                      JSON,
                                          CustomJObjectParserDelegate<PowerSchedule>?  CustomPowerScheduleParser   = null)
        {

            if (TryParse(JSON,
                         out var powerSchedule,
                         out var errorResponse,
                         CustomPowerScheduleParser))
            {
                return powerSchedule!;
            }

            throw new ArgumentException("The given JSON representation of a power schedule is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out PowerSchedule, out ErrorResponse, CustomPowerScheduleParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a power schedule.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerSchedule">The parsed power schedule.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject             JSON,
                                       out PowerSchedule?  PowerSchedule,
                                       out String?         ErrorResponse)

            => TryParse(JSON,
                        out PowerSchedule,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a power schedule.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerSchedule">The parsed power schedule.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomPowerScheduleParser">An optional delegate to parse custom contract certificates.</param>
        public static Boolean TryParse(JObject                                      JSON,
                                       out PowerSchedule?                           PowerSchedule,
                                       out String?                                  ErrorResponse,
                                       CustomJObjectParserDelegate<PowerSchedule>?  CustomPowerScheduleParser)
        {

            try
            {

                PowerSchedule = null;

                #region TimeAnchor              [mandatory]

                if (!JSON.ParseMandatory("timeAnchor",
                                         "time anchor",
                                         out DateTime TimeAnchor,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region AvailableEnergy         [optional]

                if (JSON.ParseOptionalJSON("availableEnergy",
                                           "available energy",
                                           RationalNumber.TryParse,
                                           out RationalNumber? AvailableEnergy,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region PowerTolerance          [optional]

                if (JSON.ParseOptionalJSON("PowerTolerance",
                                           "power tolerance",
                                           RationalNumber.TryParse,
                                           out RationalNumber? PowerTolerance,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region PowerScheduleEntries    [mandatory]

                if (!JSON.ParseMandatoryHashSet("powerScheduleEntries",
                                                "power schedule entries",
                                                PowerScheduleEntry.TryParse,
                                                out HashSet<PowerScheduleEntry> PowerScheduleEntries,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion


                PowerSchedule = new PowerSchedule(TimeAnchor,
                                                  AvailableEnergy,
                                                  PowerTolerance,
                                                  PowerScheduleEntries);

                if (CustomPowerScheduleParser is not null)
                    PowerSchedule = CustomPowerScheduleParser(JSON,
                                                              PowerSchedule);

                return true;

            }
            catch (Exception e)
            {
                PowerSchedule  = null;
                ErrorResponse  = "The given JSON representation of a power schedule is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomPowerScheduleSerializer = null, CustomPowerScheduleEntrySerializer = null, ...)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomPowerScheduleSerializer">A delegate to serialize custom power schedules.</param>
        /// <param name="CustomPowerScheduleEntrySerializer">A delegate to serialize custom power schedule entries.</param>
        /// <param name="CustomRationalNumberSerializer">A delegate to serialize custom rational numbers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PowerSchedule>?       CustomPowerScheduleSerializer        = null,
                              CustomJObjectSerializerDelegate<PowerScheduleEntry>?  CustomPowerScheduleEntrySerializer   = null,
                              CustomJObjectSerializerDelegate<RationalNumber>?      CustomRationalNumberSerializer       = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("timeAnchor",            TimeAnchor.           ToIso8601()),
                                 new JProperty("powerScheduleEntries",  new JArray(PowerScheduleEntries.Select(powerScheduleEntry => powerScheduleEntry.ToJSON(CustomPowerScheduleEntrySerializer,
                                                                                                                                                               CustomRationalNumberSerializer)))),

                           AvailableEnergy is not null
                               ? new JProperty("availableEnergy",       AvailableEnergy.Value.ToJSON(CustomRationalNumberSerializer))
                               : null,

                           PowerTolerance is not null
                               ? new JProperty("powerTolerance",        PowerTolerance. Value.ToJSON(CustomRationalNumberSerializer))
                               : null

                       );

            return CustomPowerScheduleSerializer is not null
                       ? CustomPowerScheduleSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (PowerSchedule1, PowerSchedule2)

        /// <summary>
        /// Compares two power schedules for equality.
        /// </summary>
        /// <param name="PowerSchedule1">A power schedule.</param>
        /// <param name="PowerSchedule2">Another power schedule.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (PowerSchedule? PowerSchedule1,
                                           PowerSchedule? PowerSchedule2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(PowerSchedule1, PowerSchedule2))
                return true;

            // If one is null, but not both, return false.
            if (PowerSchedule1 is null || PowerSchedule2 is null)
                return false;

            return PowerSchedule1.Equals(PowerSchedule2);

        }

        #endregion

        #region Operator != (PowerSchedule1, PowerSchedule2)

        /// <summary>
        /// Compares two power schedules for inequality.
        /// </summary>
        /// <param name="PowerSchedule1">A power schedule.</param>
        /// <param name="PowerSchedule2">Another power schedule.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (PowerSchedule? PowerSchedule1,
                                           PowerSchedule? PowerSchedule2)

            => !(PowerSchedule1 == PowerSchedule2);

        #endregion

        #endregion

        #region IEquatable<PowerSchedule> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two power schedules for equality.
        /// </summary>
        /// <param name="Object">A power schedule to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is PowerSchedule powerSchedule &&
                   Equals(powerSchedule);

        #endregion

        #region Equals(PowerSchedule)

        /// <summary>
        /// Compares two power schedules for equality.
        /// </summary>
        /// <param name="PowerSchedule">A power schedule to compare with.</param>
        public Boolean Equals(PowerSchedule? PowerSchedule)

            => PowerSchedule is not null &&

               TimeAnchor.Equals(PowerSchedule.TimeAnchor) &&

             ((AvailableEnergy is     null && PowerSchedule.AvailableEnergy is     null) ||
              (AvailableEnergy is not null && PowerSchedule.AvailableEnergy is not null && AvailableEnergy.Equals(PowerSchedule.AvailableEnergy))) &&

             ((PowerTolerance  is     null && PowerSchedule.PowerTolerance  is     null) ||
              (PowerTolerance  is not null && PowerSchedule.PowerTolerance  is not null && PowerTolerance. Equals(PowerSchedule.PowerTolerance)))  &&

               PowerScheduleEntries.Count().Equals(PowerSchedule.PowerScheduleEntries.Count()) &&
               PowerScheduleEntries.All(powerScheduleEntry => PowerSchedule.PowerScheduleEntries.Contains(powerScheduleEntry));

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

                return TimeAnchor.          GetHashCode()       * 11 ^
                      (AvailableEnergy?.    GetHashCode() ?? 0) *  7 ^
                      (PowerTolerance?.     GetHashCode() ?? 0) *  5 ^
                       PowerScheduleEntries.CalcHashCode()      *  3 ^

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

                   TimeAnchor.ToIso8601(),

                   AvailableEnergy is not null
                       ? $", {AvailableEnergy} kW"
                       : "",

                   PowerTolerance is not null
                       ? $", {PowerTolerance}"
                       : "",

                   $", {PowerScheduleEntries.Count()} power schedule entry/entries"

               );

        #endregion

    }

}
