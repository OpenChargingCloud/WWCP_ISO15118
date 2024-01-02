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
    /// The scheduled EV power profile.
    /// </summary>
    public class ScheduledEVPowerProfile : EVPowerProfile,
                                           IEquatable<ScheduledEVPowerProfile>
    {

        #region Properties

        /// <summary>
        /// The selected schedule tuple identification.
        /// </summary>
        [Mandatory]
        public ScheduleTuple_Id            SelectedScheduleTupleId     { get; }

        /// <summary>
        /// The optional power tolerance acceptance.
        /// </summary>
        [Optional]
        public PowerToleranceAcceptances?  PowerToleranceAcceptance    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new scheduled EV power profile.
        /// </summary>
        /// <param name="TimeAnchor">A time anchor.</param>
        /// <param name="EVPowerProfileEntries">An enumeration of power schedule entries.</param>
        /// <param name="SelectedScheduleTupleId">The selected schedule tuple identification.</param>
        /// <param name="PowerToleranceAcceptance">An optional power tolerance acceptance.</param>
        public ScheduledEVPowerProfile(DateTime                         TimeAnchor,
                                       IEnumerable<PowerScheduleEntry>  EVPowerProfileEntries,
                                       ScheduleTuple_Id                 SelectedScheduleTupleId,
                                       PowerToleranceAcceptances?       PowerToleranceAcceptance   = null)

            : base(TimeAnchor,
                   EVPowerProfileEntries)

        {

            this.SelectedScheduleTupleId   = SelectedScheduleTupleId;
            this.PowerToleranceAcceptance  = PowerToleranceAcceptance;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="Scheduled_EVPPTControlModeType">
        //     <xs:sequence>
        //         <xs:element name="SelectedScheduleTupleID"  type="v2gci_ct:numericIDType"/>
        //         <xs:element name="PowerToleranceAcceptance" type="powerToleranceAcceptanceType" minOccurs="0"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomScheduledEVPowerProfileParser = null)

        /// <summary>
        /// Parse the given JSON representation of a scheduled EV power profile.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomScheduledEVPowerProfileParser">A delegate to parse custom scheduled EV power profiles.</param>
        public static ScheduledEVPowerProfile Parse(JObject                                                JSON,
                                                    CustomJObjectParserDelegate<ScheduledEVPowerProfile>?  CustomScheduledEVPowerProfileParser   = null)
        {

            if (TryParse(JSON,
                         out var scheduledEVPowerProfile,
                         out var errorResponse,
                         CustomScheduledEVPowerProfileParser))
            {
                return scheduledEVPowerProfile!;
            }

            throw new ArgumentException("The given JSON representation of a scheduled EV power profile is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out ScheduledEVPowerProfile, out ErrorResponse, CustomScheduledEVPowerProfileParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a scheduled EV power profile.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ScheduledEVPowerProfile">The parsed scheduled EV power profile.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                       JSON,
                                       out ScheduledEVPowerProfile?  ScheduledEVPowerProfile,
                                       out String?                   ErrorResponse)

            => TryParse(JSON,
                        out ScheduledEVPowerProfile,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a scheduled EV power profile.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ScheduledEVPowerProfile">The parsed scheduled EV power profile.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomScheduledEVPowerProfileParser">A delegate to parse custom contract certificates.</param>
        public static Boolean TryParse(JObject                                                JSON,
                                       out ScheduledEVPowerProfile?                           ScheduledEVPowerProfile,
                                       out String?                                            ErrorResponse,
                                       CustomJObjectParserDelegate<ScheduledEVPowerProfile>?  CustomScheduledEVPowerProfileParser)
        {

            try
            {

                ScheduledEVPowerProfile = null;

                #region TimeAnchor                  [mandatory]

                if (!JSON.ParseMandatory("timeAnchor",
                                         "time anchor",
                                         out DateTime TimeAnchor,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region EVPowerProfileEntries       [mandatory]

                if (!JSON.ParseMandatoryHashSet("evPowerProfileEntries",
                                                "evPowerProfileEntries",
                                                PowerScheduleEntry.TryParse,
                                                out HashSet<PowerScheduleEntry> EVPowerProfileEntries,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region SelectedScheduleTupleId     [mandatory]

                if (!JSON.ParseMandatory("selectedScheduleTupleId",
                                         "selected schedule tuple identification",
                                         ScheduleTuple_Id.TryParse,
                                         out ScheduleTuple_Id SelectedScheduleTupleId,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region PowerToleranceAcceptance    [optional]

                if (JSON.ParseOptional("powerToleranceAcceptance",
                                       "power tolerance acceptance",
                                       PowerToleranceAcceptancesExtensions.TryParse,
                                       out PowerToleranceAcceptances PowerToleranceAcceptance,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                ScheduledEVPowerProfile = new ScheduledEVPowerProfile(TimeAnchor,
                                                                      EVPowerProfileEntries,
                                                                      SelectedScheduleTupleId,
                                                                      PowerToleranceAcceptance);

                if (CustomScheduledEVPowerProfileParser is not null)
                    ScheduledEVPowerProfile = CustomScheduledEVPowerProfileParser(JSON,
                                                                                  ScheduledEVPowerProfile);

                return true;

            }
            catch (Exception e)
            {
                ScheduledEVPowerProfile  = null;
                ErrorResponse            = "The given JSON representation of a scheduled EV power profile is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomScheduledEVPowerProfileSerializer = null, CustomPowerScheduleEntrySerializer = null, ...)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomScheduledEVPowerProfileSerializer">A delegate to serialize custom scheduled EV power profiles.</param>
        /// <param name="CustomPowerScheduleEntrySerializer">A delegate to serialize custom power schedule entries.</param>
        /// <param name="CustomRationalNumberSerializer">A delegate to serialize custom rational numbers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<ScheduledEVPowerProfile>?  CustomScheduledEVPowerProfileSerializer   = null,
                              CustomJObjectSerializerDelegate<PowerScheduleEntry>?       CustomPowerScheduleEntrySerializer        = null,
                              CustomJObjectSerializerDelegate<RationalNumber>?           CustomRationalNumberSerializer            = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("timeAnchor",                TimeAnchor.ToIso8601()),

                                 new JProperty("evPowerProfileEntries",     new JArray(EVPowerProfileEntries.Select(powerScheduleEntry => powerScheduleEntry.ToJSON(CustomPowerScheduleEntrySerializer,
                                                                                                                                                                    CustomRationalNumberSerializer)))),
                                 new JProperty("selectedScheduleTupleId",   SelectedScheduleTupleId.Value),

                           PowerToleranceAcceptance.HasValue
                               ? new JProperty("powerToleranceAcceptance",  PowerToleranceAcceptance.Value.AsText())
                               : null

                       );

            return CustomScheduledEVPowerProfileSerializer is not null
                       ? CustomScheduledEVPowerProfileSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (ScheduledEVPowerProfile1, ScheduledEVPowerProfile2)

        /// <summary>
        /// Compares two scheduled EV power profiles for equality.
        /// </summary>
        /// <param name="ScheduledEVPowerProfile1">A scheduled EV power profile.</param>
        /// <param name="ScheduledEVPowerProfile2">Another scheduled EV power profile.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ScheduledEVPowerProfile? ScheduledEVPowerProfile1,
                                           ScheduledEVPowerProfile? ScheduledEVPowerProfile2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(ScheduledEVPowerProfile1, ScheduledEVPowerProfile2))
                return true;

            // If one is null, but not both, return false.
            if (ScheduledEVPowerProfile1 is null || ScheduledEVPowerProfile2 is null)
                return false;

            return ScheduledEVPowerProfile1.Equals(ScheduledEVPowerProfile2);

        }

        #endregion

        #region Operator != (ScheduledEVPowerProfile1, ScheduledEVPowerProfile2)

        /// <summary>
        /// Compares two scheduled EV power profiles for inequality.
        /// </summary>
        /// <param name="ScheduledEVPowerProfile1">A scheduled EV power profile.</param>
        /// <param name="ScheduledEVPowerProfile2">Another scheduled EV power profile.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ScheduledEVPowerProfile? ScheduledEVPowerProfile1,
                                           ScheduledEVPowerProfile? ScheduledEVPowerProfile2)

            => !(ScheduledEVPowerProfile1 == ScheduledEVPowerProfile2);

        #endregion

        #endregion

        #region IEquatable<ScheduledEVPowerProfile> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two scheduled EV power profiles for equality.
        /// </summary>
        /// <param name="Object">A scheduled EV power profile to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ScheduledEVPowerProfile scheduledEVPowerProfile &&
                   Equals(scheduledEVPowerProfile);

        #endregion

        #region Equals(ScheduledEVPowerProfile)

        /// <summary>
        /// Compares two scheduled EV power profiles for equality.
        /// </summary>
        /// <param name="ScheduledEVPowerProfile">A scheduled EV power profile to compare with.</param>
        public Boolean Equals(ScheduledEVPowerProfile? ScheduledEVPowerProfile)

            => ScheduledEVPowerProfile is not null &&

               TimeAnchor.             Equals(ScheduledEVPowerProfile.TimeAnchor)              &&
               SelectedScheduleTupleId.Equals(ScheduledEVPowerProfile.SelectedScheduleTupleId) &&

               EVPowerProfileEntries.Count().Equals(ScheduledEVPowerProfile.EVPowerProfileEntries.Count()) &&
               EVPowerProfileEntries.All(subCertificate => ScheduledEVPowerProfile.EVPowerProfileEntries.Contains(subCertificate)) &&

            ((!PowerToleranceAcceptance.HasValue && !ScheduledEVPowerProfile.PowerToleranceAcceptance.HasValue) ||
              (PowerToleranceAcceptance.HasValue &&  ScheduledEVPowerProfile.PowerToleranceAcceptance.HasValue && PowerToleranceAcceptance.Value.Equals(ScheduledEVPowerProfile.PowerToleranceAcceptance.Value))) &&

               base.Equals(ScheduledEVPowerProfile);

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

                return TimeAnchor.               GetHashCode()       * 11 ^
                       EVPowerProfileEntries.    CalcHashCode()      *  7 ^
                       SelectedScheduleTupleId.  GetHashCode()       *  5 ^
                      (PowerToleranceAcceptance?.GetHashCode() ?? 0) *  3 ^

                       base.                     GetHashCode();

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
                   ": ",

                   EVPowerProfileEntries.Count(),
                   " scheduled EV power profile entry/entries, ",

                   SelectedScheduleTupleId,

                   PowerToleranceAcceptance.HasValue
                       ? ", power tolerance: " + PowerToleranceAcceptance.Value.AsText()
                       : ""

               );

        #endregion

    }

}
