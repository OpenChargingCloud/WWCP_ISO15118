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
    /// The EV power schedule.
    /// </summary>
    public class EVPowerSchedule : IEquatable<EVPowerSchedule>
    {

        #region Properties

        /// <summary>
        /// The time anchor.
        /// </summary>
        [Mandatory]
        public DateTime                           TimeAnchor                { get; }

        /// <summary>
        /// The enumeration of EV power schedule entries.
        /// </summary>
        [Mandatory]
        public IEnumerable<EVPowerScheduleEntry>  EVPowerScheduleEntries    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new EV power schedule.
        /// </summary>
        /// <param name="Duration">A time anchor.</param>
        /// <param name="Power">An enumeration of EV power schedule entries.</param>
        public EVPowerSchedule(DateTime                           TimeAnchor,
                               IEnumerable<EVPowerScheduleEntry>  EVPowerScheduleEntries)
        {

            this.TimeAnchor              = TimeAnchor;
            this.EVPowerScheduleEntries  = EVPowerScheduleEntries.Distinct();

        }

        #endregion


        #region Documentation

        // <xs:complexType name="EVPowerScheduleType">
        //     <xs:sequence>
        //         <xs:element name="TimeAnchor"             type="xs:unsignedLong"/>
        //         <xs:element name="EVPowerScheduleEntries" type="EVPowerScheduleEntryListType"/>
        //     </xs:sequence>
        // </xs:complexType>

        // <xs:complexType name="EVPowerScheduleEntryListType">
        //     <xs:sequence>
        //         <xs:element name="EVPowerScheduleEntry" type="EVPowerScheduleEntryType" maxOccurs="1024"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomEVPowerScheduleParser = null)

        /// <summary>
        /// Parse the given JSON representation of an EV power schedule.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomEVPowerScheduleParser">An optional delegate to parse custom EV power schedules.</param>
        public static EVPowerSchedule Parse(JObject                                        JSON,
                                            CustomJObjectParserDelegate<EVPowerSchedule>?  CustomEVPowerScheduleParser   = null)
        {

            if (TryParse(JSON,
                         out var evPowerSchedule,
                         out var errorResponse,
                         CustomEVPowerScheduleParser))
            {
                return evPowerSchedule!;
            }

            throw new ArgumentException("The given JSON representation of an EV power schedule is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out EVPowerSchedule, out ErrorResponse, CustomEVPowerScheduleParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of an EV power schedule.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EVPowerSchedule">The parsed EV power schedule.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject               JSON,
                                       out EVPowerSchedule?  EVPowerSchedule,
                                       out String?           ErrorResponse)

            => TryParse(JSON,
                        out EVPowerSchedule,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an EV power schedule.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EVPowerSchedule">The parsed EV power schedule.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomEVPowerScheduleParser">An optional delegate to parse custom EV power schedules.</param>
        public static Boolean TryParse(JObject                                        JSON,
                                       out EVPowerSchedule?                           EVPowerSchedule,
                                       out String?                                    ErrorResponse,
                                       CustomJObjectParserDelegate<EVPowerSchedule>?  CustomEVPowerScheduleParser)
        {

            try
            {

                EVPowerSchedule = null;

                #region TimeAnchor                [mandatory]

                if (!JSON.ParseMandatory("timeAnchor",
                                         "time anchor",
                                         out DateTime TimeAnchor,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region EVPowerScheduleEntries    [mandatory]

                if (!JSON.ParseMandatoryHashSet("evPowerScheduleEntries",
                                                "EV power schedule entries",
                                                EVPowerScheduleEntry.TryParse,
                                                out HashSet<EVPowerScheduleEntry> EVPowerScheduleEntries,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion


                EVPowerSchedule = new EVPowerSchedule(TimeAnchor,
                                                      EVPowerScheduleEntries);

                if (CustomEVPowerScheduleParser is not null)
                    EVPowerSchedule = CustomEVPowerScheduleParser(JSON,
                                                                  EVPowerSchedule);

                return true;

            }
            catch (Exception e)
            {
                EVPowerSchedule  = null;
                ErrorResponse    = "The given JSON representation of an EV power schedule is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomEVPowerScheduleSerializer = null, CustomEVPowerScheduleEntrySerializer = null, ...)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomEVPowerScheduleSerializer">A delegate to serialize custom EV power schedules.</param>
        /// <param name="CustomEVPowerScheduleEntrySerializer">A delegate to serialize custom EV power schedule entries.</param>
        /// <param name="CustomRationalNumberSerializer">A delegate to serialize custom rational numbers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<EVPowerSchedule>?       CustomEVPowerScheduleSerializer        = null,
                              CustomJObjectSerializerDelegate<EVPowerScheduleEntry>?  CustomEVPowerScheduleEntrySerializer   = null,
                              CustomJObjectSerializerDelegate<RationalNumber>?        CustomRationalNumberSerializer         = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("timeAnchor",              TimeAnchor.ToIso8601()),
                                 new JProperty("evPowerScheduleEntries",  new JArray(EVPowerScheduleEntries.Select(evPowerScheduleEntry => evPowerScheduleEntry.ToJSON(CustomEVPowerScheduleEntrySerializer,
                                                                                                                                                                       CustomRationalNumberSerializer))))

                       );

            return CustomEVPowerScheduleSerializer is not null
                       ? CustomEVPowerScheduleSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (EVPowerSchedule1, EVPowerSchedule2)

        /// <summary>
        /// Compares two EV power schedules for equality.
        /// </summary>
        /// <param name="EVPowerSchedule1">An EV power schedule.</param>
        /// <param name="EVPowerSchedule2">Another EV power schedule.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EVPowerSchedule? EVPowerSchedule1,
                                           EVPowerSchedule? EVPowerSchedule2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(EVPowerSchedule1, EVPowerSchedule2))
                return true;

            // If one is null, but not both, return false.
            if (EVPowerSchedule1 is null || EVPowerSchedule2 is null)
                return false;

            return EVPowerSchedule1.Equals(EVPowerSchedule2);

        }

        #endregion

        #region Operator != (EVPowerSchedule1, EVPowerSchedule2)

        /// <summary>
        /// Compares two EV power schedules for inequality.
        /// </summary>
        /// <param name="EVPowerSchedule1">An EV power schedule.</param>
        /// <param name="EVPowerSchedule2">Another EV power schedule.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EVPowerSchedule? EVPowerSchedule1,
                                           EVPowerSchedule? EVPowerSchedule2)

            => !(EVPowerSchedule1 == EVPowerSchedule2);

        #endregion

        #endregion

        #region IEquatable<EVPowerSchedule> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two EV power schedules for equality.
        /// </summary>
        /// <param name="Object">An EV power schedule to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EVPowerSchedule evPowerSchedule &&
                   Equals(evPowerSchedule);

        #endregion

        #region Equals(EVPowerSchedule)

        /// <summary>
        /// Compares two EV power schedules for equality.
        /// </summary>
        /// <param name="EVPowerSchedule">An EV power schedule to compare with.</param>
        public Boolean Equals(EVPowerSchedule? EVPowerSchedule)

            => EVPowerSchedule is not null &&

               TimeAnchor.Equals(EVPowerSchedule.TimeAnchor) &&

               EVPowerScheduleEntries.Count().Equals(EVPowerSchedule.EVPowerScheduleEntries.Count()) &&
               EVPowerScheduleEntries.All(evPowerScheduleEntry => EVPowerSchedule.EVPowerScheduleEntries.Contains(evPowerScheduleEntry));

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

                return TimeAnchor.            GetHashCode()  * 5 ^
                       EVPowerScheduleEntries.CalcHashCode() * 3 ^

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

                   TimeAnchor.ToIso8601(),
                   ", ",
                   EVPowerScheduleEntries.Count(),
                   " EV power schedule entry/entries"

               );

        #endregion

    }

}
