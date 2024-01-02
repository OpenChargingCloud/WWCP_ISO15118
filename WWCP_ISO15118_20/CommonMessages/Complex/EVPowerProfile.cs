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
    /// The EV power profile.
    /// </summary>
    public abstract class EVPowerProfile : IEquatable<EVPowerProfile>
    {

        #region Properties

        /// <summary>
        /// The time anchor.
        /// </summary>
        [Mandatory]
        public DateTime                         TimeAnchor               { get; }

        /// <summary>
        /// The enumeration of power schedule entries.
        /// [max 2048]
        /// </summary>
        [Mandatory]
        public IEnumerable<PowerScheduleEntry>  EVPowerProfileEntries    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new EV power profile.
        /// </summary>
        /// <param name="TimeAnchor">A time anchor.</param>
        /// <param name="EVPowerProfileEntries">An enumeration of power schedule entries.</param>
        public EVPowerProfile(DateTime                         TimeAnchor,
                              IEnumerable<PowerScheduleEntry>  EVPowerProfileEntries)
        {

            this.TimeAnchor             = TimeAnchor;
            this.EVPowerProfileEntries  = EVPowerProfileEntries.Distinct();

        }

        #endregion


        #region Documentation

        // <xs:complexType name="EVPowerProfileType">
        //     <xs:sequence>
        //
        //         <xs:element name="TimeAnchor" type="xs:unsignedLong"/>
        //
        //         <xs:choice>
        //             <xs:element name="Dynamic_EVPPTControlMode"   type="Dynamic_EVPPTControlModeType"/>
        //             <xs:element name="Scheduled_EVPPTControlMode" type="Scheduled_EVPPTControlModeType"/>
        //         </xs:choice>
        //
        //         <xs:element name="EVPowerProfileEntries" type="EVPowerProfileEntryListType"/>
        //
        //     </xs:sequence>
        // </xs:complexType>


        // <xs:complexType name="EVPowerProfileEntryListType">
        //     <xs:sequence>
        //         <xs:element name="EVPowerProfileEntry" type="PowerScheduleEntryType" maxOccurs="2048"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomEVPowerProfileParser = null)

        /// <summary>
        /// Parse the given JSON representation of an EV power profile.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomEVPowerProfileParser">A delegate to parse custom EV power profiles.</param>
        public static EVPowerProfile Parse(JObject                                              JSON,
                                                  CustomJObjectParserDelegate<EVPowerProfile>?  CustomEVPowerProfileParser   = null)
        {

            if (TryParse(JSON,
                         out var evPowerProfile,
                         out var errorResponse,
                         CustomEVPowerProfileParser))
            {
                return evPowerProfile!;
            }

            throw new ArgumentException("The given JSON representation of an EV power profile is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out EVPowerProfile, out ErrorResponse, CustomEVPowerProfileParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of an EV power profile.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EVPowerProfile">The parsed EV power profile.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject              JSON,
                                       out EVPowerProfile?  EVPowerProfile,
                                       out String?          ErrorResponse)

            => TryParse(JSON,
                        out EVPowerProfile,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an EV power profile.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EVPowerProfile">The parsed EV power profile.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomEVPowerProfileParser">A delegate to parse custom contract certificates.</param>
        public static Boolean TryParse(JObject                                       JSON,
                                       out EVPowerProfile?                           EVPowerProfile,
                                       out String?                                   ErrorResponse,
                                       CustomJObjectParserDelegate<EVPowerProfile>?  CustomEVPowerProfileParser)
        {

            try
            {

                EVPowerProfile = null;

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


                #region SelectedScheduleTupleId     [optional]

                if (JSON.ParseOptional("selectedScheduleTupleId",
                                       "selected schedule tuple identification",
                                       ScheduleTuple_Id.TryParse,
                                       out ScheduleTuple_Id? SelectedScheduleTupleId,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
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


                EVPowerProfile = SelectedScheduleTupleId.HasValue

                                     ? new ScheduledEVPowerProfile(TimeAnchor,
                                                                   EVPowerProfileEntries,
                                                                   SelectedScheduleTupleId.Value,
                                                                   PowerToleranceAcceptance)

                                     : new DynamicEVPowerProfile  (TimeAnchor,
                                                                   EVPowerProfileEntries);

                if (CustomEVPowerProfileParser is not null)
                    EVPowerProfile = CustomEVPowerProfileParser(JSON,
                                                                EVPowerProfile);

                return true;

            }
            catch (Exception e)
            {
                EVPowerProfile  = null;
                ErrorResponse   = "The given JSON representation of an EV power profile is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomEVPowerProfileSerializer = null, CustomDynamicEVPowerProfileSerializer = null, ...)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomEVPowerProfileSerializer">A delegate to serialize custom EV power profiles.</param>
        /// <param name="CustomDynamicEVPowerProfileSerializer">A delegate to serialize custom dynamic EV power profiles.</param>
        /// <param name="CustomPowerScheduleEntrySerializer">A delegate to serialize custom power schedule entries.</param>
        /// <param name="CustomRationalNumberSerializer">A delegate to serialize custom rational numbers.</param>
        /// <param name="CustomScheduledEVPowerProfileSerializer">A delegate to serialize custom scheduled EV power profiles.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<EVPowerProfile>?           CustomEVPowerProfileSerializer            = null,
                              CustomJObjectSerializerDelegate<DynamicEVPowerProfile>?    CustomDynamicEVPowerProfileSerializer     = null,
                              CustomJObjectSerializerDelegate<PowerScheduleEntry>?       CustomPowerScheduleEntrySerializer        = null,
                              CustomJObjectSerializerDelegate<RationalNumber>?           CustomRationalNumberSerializer            = null,
                              CustomJObjectSerializerDelegate<ScheduledEVPowerProfile>?  CustomScheduledEVPowerProfileSerializer   = null)
        {

            var json = new JObject();

            if (this is DynamicEVPowerProfile dynamicEVPowerProfile)
                json = dynamicEVPowerProfile.  ToJSON(CustomDynamicEVPowerProfileSerializer,
                                                      CustomPowerScheduleEntrySerializer,
                                                      CustomRationalNumberSerializer);

            if (this is ScheduledEVPowerProfile scheduledEVPowerProfile)
                json = scheduledEVPowerProfile.ToJSON(CustomScheduledEVPowerProfileSerializer,
                                                      CustomPowerScheduleEntrySerializer,
                                                      CustomRationalNumberSerializer);

            return CustomEVPowerProfileSerializer is not null
                       ? CustomEVPowerProfileSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (EVPowerProfile1, EVPowerProfile2)

        /// <summary>
        /// Compares two EV power profiles for equality.
        /// </summary>
        /// <param name="EVPowerProfile1">An EV power profile.</param>
        /// <param name="EVPowerProfile2">Another EV power profile.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EVPowerProfile? EVPowerProfile1,
                                           EVPowerProfile? EVPowerProfile2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(EVPowerProfile1, EVPowerProfile2))
                return true;

            // If one is null, but not both, return false.
            if (EVPowerProfile1 is null || EVPowerProfile2 is null)
                return false;

            return EVPowerProfile1.Equals(EVPowerProfile2);

        }

        #endregion

        #region Operator != (EVPowerProfile1, EVPowerProfile2)

        /// <summary>
        /// Compares two EV power profiles for inequality.
        /// </summary>
        /// <param name="EVPowerProfile1">An EV power profile.</param>
        /// <param name="EVPowerProfile2">Another EV power profile.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EVPowerProfile? EVPowerProfile1,
                                           EVPowerProfile? EVPowerProfile2)

            => !(EVPowerProfile1 == EVPowerProfile2);

        #endregion

        #endregion

        #region IEquatable<EVPowerProfile> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two EV power profiles for equality.
        /// </summary>
        /// <param name="Object">An EV power profile to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EVPowerProfile evPowerProfile &&
                   Equals(evPowerProfile);

        #endregion

        #region Equals(EVPowerProfile)

        /// <summary>
        /// Compares two EV power profiles for equality.
        /// </summary>
        /// <param name="EVPowerProfile">An EV power profile to compare with.</param>
        public Boolean Equals(EVPowerProfile? EVPowerProfile)

            => EVPowerProfile is not null &&

               TimeAnchor.Equals(EVPowerProfile.TimeAnchor) &&

               EVPowerProfileEntries.Count().Equals(EVPowerProfile.EVPowerProfileEntries.Count()) &&
               EVPowerProfileEntries.All(powerScheduleEntry => EVPowerProfile.EVPowerProfileEntries.Contains(powerScheduleEntry));

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

                return TimeAnchor.           GetHashCode()  * 5 ^
                       EVPowerProfileEntries.CalcHashCode() * 3 ^

                       base.                 GetHashCode();

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
                   " EV power profile entry/entries"

               );

        #endregion

    }

}
