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
    /// The dynamic EV power profile.
    /// </summary>
    public class DynamicEVPowerProfile : EVPowerProfile,
                                         IEquatable<DynamicEVPowerProfile>
    {

        #region Constructor(s)

        /// <summary>
        /// Create a new dynamic EV power profile.
        /// </summary>
        /// <param name="TimeAnchor">A time anchor.</param>
        /// <param name="EVPowerProfileEntries">An enumeration of power schedule entries.</param>
        public DynamicEVPowerProfile(DateTime                         TimeAnchor,
                                     IEnumerable<PowerScheduleEntry>  EVPowerProfileEntries)

            : base(TimeAnchor,
                   EVPowerProfileEntries)

        { }

        #endregion


        #region Documentation

        //<xs:complexType name="Dynamic_EVPPTControlModeType"/>

        #endregion

        #region (static) Parse   (JSON, CustomDynamicEVPowerProfileParser = null)

        /// <summary>
        /// Parse the given JSON representation of a dynamic EV power profile.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomDynamicEVPowerProfileParser">An optional delegate to parse custom dynamic EV power profiles.</param>
        public static DynamicEVPowerProfile Parse(JObject                                              JSON,
                                                  CustomJObjectParserDelegate<DynamicEVPowerProfile>?  CustomDynamicEVPowerProfileParser   = null)
        {

            if (TryParse(JSON,
                         out var dynamicEVPowerProfile,
                         out var errorResponse,
                         CustomDynamicEVPowerProfileParser))
            {
                return dynamicEVPowerProfile!;
            }

            throw new ArgumentException("The given JSON representation of a dynamic EV power profile is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out DynamicEVPowerProfile, out ErrorResponse, CustomDynamicEVPowerProfileParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a dynamic EV power profile.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DynamicEVPowerProfile">The parsed dynamic EV power profile.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                     JSON,
                                       out DynamicEVPowerProfile?  DynamicEVPowerProfile,
                                       out String?                 ErrorResponse)

            => TryParse(JSON,
                        out DynamicEVPowerProfile,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a dynamic EV power profile.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DynamicEVPowerProfile">The parsed dynamic EV power profile.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomDynamicEVPowerProfileParser">An optional delegate to parse custom contract certificates.</param>
        public static Boolean TryParse(JObject                                              JSON,
                                       out DynamicEVPowerProfile?                           DynamicEVPowerProfile,
                                       out String?                                          ErrorResponse,
                                       CustomJObjectParserDelegate<DynamicEVPowerProfile>?  CustomDynamicEVPowerProfileParser)
        {

            try
            {

                DynamicEVPowerProfile = null;

                #region TimeAnchor               [mandatory]

                if (!JSON.ParseMandatory("timeAnchor",
                                         "time anchor",
                                         out DateTime TimeAnchor,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region EVPowerProfileEntries    [mandatory]

                if (!JSON.ParseMandatoryHashSet("evPowerProfileEntries",
                                                "evPowerProfileEntries",
                                                PowerScheduleEntry.TryParse,
                                                out HashSet<PowerScheduleEntry> EVPowerProfileEntries,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion


                DynamicEVPowerProfile = new DynamicEVPowerProfile(TimeAnchor,
                                                                  EVPowerProfileEntries);

                if (CustomDynamicEVPowerProfileParser is not null)
                    DynamicEVPowerProfile = CustomDynamicEVPowerProfileParser(JSON,
                                                                              DynamicEVPowerProfile);

                return true;

            }
            catch (Exception e)
            {
                DynamicEVPowerProfile  = null;
                ErrorResponse          = "The given JSON representation of a dynamic EV power profile is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomDynamicEVPowerProfileSerializer = null, CustomPowerScheduleEntrySerializer = null, ...)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomDynamicEVPowerProfileSerializer">A delegate to serialize custom dynamic EV power profiles.</param>
        /// <param name="CustomPowerScheduleEntrySerializer">A delegate to serialize custom power schedule entries.</param>
        /// <param name="CustomRationalNumberSerializer">A delegate to serialize custom rational numbers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<DynamicEVPowerProfile>?  CustomDynamicEVPowerProfileSerializer   = null,
                              CustomJObjectSerializerDelegate<PowerScheduleEntry>?     CustomPowerScheduleEntrySerializer      = null,
                              CustomJObjectSerializerDelegate<RationalNumber>?         CustomRationalNumberSerializer          = null)
        {

            var json = JSONObject.Create(

                           new JProperty("timeAnchor",             TimeAnchor.ToISO8601()),
                           new JProperty("evPowerProfileEntries",  new JArray(EVPowerProfileEntries.Select(powerScheduleEntry => powerScheduleEntry.ToJSON(CustomPowerScheduleEntrySerializer,
                                                                                                                                                           CustomRationalNumberSerializer))))

                       );

            return CustomDynamicEVPowerProfileSerializer is not null
                       ? CustomDynamicEVPowerProfileSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (DynamicEVPowerProfile1, DynamicEVPowerProfile2)

        /// <summary>
        /// Compares two dynamic EV power profiles for equality.
        /// </summary>
        /// <param name="DynamicEVPowerProfile1">A dynamic EV power profile.</param>
        /// <param name="DynamicEVPowerProfile2">Another dynamic EV power profile.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (DynamicEVPowerProfile? DynamicEVPowerProfile1,
                                           DynamicEVPowerProfile? DynamicEVPowerProfile2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(DynamicEVPowerProfile1, DynamicEVPowerProfile2))
                return true;

            // If one is null, but not both, return false.
            if (DynamicEVPowerProfile1 is null || DynamicEVPowerProfile2 is null)
                return false;

            return DynamicEVPowerProfile1.Equals(DynamicEVPowerProfile2);

        }

        #endregion

        #region Operator != (DynamicEVPowerProfile1, DynamicEVPowerProfile2)

        /// <summary>
        /// Compares two dynamic EV power profiles for inequality.
        /// </summary>
        /// <param name="DynamicEVPowerProfile1">A dynamic EV power profile.</param>
        /// <param name="DynamicEVPowerProfile2">Another dynamic EV power profile.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (DynamicEVPowerProfile? DynamicEVPowerProfile1,
                                           DynamicEVPowerProfile? DynamicEVPowerProfile2)

            => !(DynamicEVPowerProfile1 == DynamicEVPowerProfile2);

        #endregion

        #endregion

        #region IEquatable<DynamicEVPowerProfile> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two dynamic EV power profiles for equality.
        /// </summary>
        /// <param name="Object">A dynamic EV power profile to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is DynamicEVPowerProfile dynamicEVPowerProfile &&
                   Equals(dynamicEVPowerProfile);

        #endregion

        #region Equals(DynamicEVPowerProfile)

        /// <summary>
        /// Compares two dynamic EV power profiles for equality.
        /// </summary>
        /// <param name="DynamicEVPowerProfile">A dynamic EV power profile to compare with.</param>
        public Boolean Equals(DynamicEVPowerProfile? DynamicEVPowerProfile)

            => DynamicEVPowerProfile is not null &&

               TimeAnchor.Equals(DynamicEVPowerProfile.TimeAnchor) &&

               EVPowerProfileEntries.Count().Equals(DynamicEVPowerProfile.EVPowerProfileEntries.Count()) &&
               EVPowerProfileEntries.All(subCertificate => DynamicEVPowerProfile.EVPowerProfileEntries.Contains(subCertificate)) &&

               base.Equals(DynamicEVPowerProfile);

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

                   TimeAnchor.ToISO8601(),
                   ": ",

                   EVPowerProfileEntries.Count(),
                   " dynamic EV power profile entry/entries"

               );

        #endregion

    }

}
