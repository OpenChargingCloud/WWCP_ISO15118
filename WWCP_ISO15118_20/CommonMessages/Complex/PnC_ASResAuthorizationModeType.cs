/*
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

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// The plug and charge authorization mode response type.
    /// </summary>
    public class PnC_ASResAuthorizationModeType
    {

        #region Properties

        /// <summary>
        /// The gen challenge.
        /// </summary>
        [Mandatory]
        public GenChallenge              GenChallenge    { get; }

        /// <summary>
        /// The enumeration of supported provider identifications.
        /// [max 128]
        /// </summary>
        [Optional]
        public IEnumerable<Provider_Id>  ProviderIds     { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new plug and charge authorization mode response type.
        /// </summary>
        /// <param name="GenChallenge">A gen challenge.</param>
        /// <param name="ProviderIds">An enumeration of supported provider identifications.</param>
        public PnC_ASResAuthorizationModeType(GenChallenge               GenChallenge,
                                              IEnumerable<Provider_Id>?  ProviderIds   = null)
        {

            this.GenChallenge  = GenChallenge;
            this.ProviderIds   = ProviderIds?.Distinct() ?? Array.Empty<Provider_Id>();

        }

        #endregion


        #region (static) Parse   (JSON, CustomPnC_ASResAuthorizationModeTypeParser = null)

        /// <summary>
        /// Parse the given JSON representation of a plug and charge authorization mode response type.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomPnC_ASResAuthorizationModeTypeParser">A delegate to parse custom PnC_ASResAuthorizationModeTypes.</param>
        public static PnC_ASResAuthorizationModeType Parse(JObject                                                       JSON,
                                                           CustomJObjectParserDelegate<PnC_ASResAuthorizationModeType>?  CustomPnC_ASResAuthorizationModeTypeParser   = null)
        {

            if (TryParse(JSON,
                         out var pnc_ASResAuthorizationModeType,
                         out var errorResponse,
                         CustomPnC_ASResAuthorizationModeTypeParser))
            {
                return pnc_ASResAuthorizationModeType!;
            }

            throw new ArgumentException("The given JSON representation of a plug and charge authorization mode response type is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out PnC_ASResAuthorizationModeType, out ErrorResponse, CustomPnC_ASResAuthorizationModeTypeParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a plug and charge authorization mode response type.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PnC_ASResAuthorizationModeType">The parsed plug and charge authorization mode response type.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                              JSON,
                                       out PnC_ASResAuthorizationModeType?  PnC_ASResAuthorizationModeType,
                                       out String?                          ErrorResponse)

            => TryParse(JSON,
                        out PnC_ASResAuthorizationModeType,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a plug and charge authorization mode response type.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PnC_ASResAuthorizationModeType">The parsed PnC_ASResAuthorizationModeType.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomPnC_ASResAuthorizationModeTypeParser">A delegate to parse custom plug and charge authorization mode response types.</param>
        public static Boolean TryParse(JObject                                                       JSON,
                                       out PnC_ASResAuthorizationModeType?                           PnC_ASResAuthorizationModeType,
                                       out String?                                                   ErrorResponse,
                                       CustomJObjectParserDelegate<PnC_ASResAuthorizationModeType>?  CustomPnC_ASResAuthorizationModeTypeParser)
        {

            try
            {

                PnC_ASResAuthorizationModeType = null;

                #region GenChallenge    [mandatory]

                if (!JSON.ParseMandatory("genChallenge",
                                         "gen challenge",
                                         CommonMessages.GenChallenge.TryParse,
                                         out GenChallenge GenChallenge,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region ProviderIds     [optional]

                if (JSON.ParseOptionalHashSet("providerIds",
                                              "provider identifications",
                                              Provider_Id.TryParse,
                                              out HashSet<Provider_Id> ProviderIds,
                                              out ErrorResponse))
                {
                    return false;
                }

                #endregion


                PnC_ASResAuthorizationModeType = new PnC_ASResAuthorizationModeType(GenChallenge,
                                                                                    ProviderIds);

                if (CustomPnC_ASResAuthorizationModeTypeParser is not null)
                    PnC_ASResAuthorizationModeType = CustomPnC_ASResAuthorizationModeTypeParser(JSON,
                                                                                                PnC_ASResAuthorizationModeType);

                return true;

            }
            catch (Exception e)
            {
                PnC_ASResAuthorizationModeType  = null;
                ErrorResponse                   = "The given JSON representation of a plug and charge authorization mode response type is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomPnC_ASResAuthorizationModeTypeSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomPnC_ASResAuthorizationModeTypeSerializer">A delegate to serialize custom plug and charge authorization mode response types.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PnC_ASResAuthorizationModeType>? CustomPnC_ASResAuthorizationModeTypeSerializer = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("genChallenge",  GenChallenge.ToString()),

                           ProviderIds.Any()
                               ? new JProperty("providerIds",   new JArray(ProviderIds.Select(providerId => providerId.ToString())))
                               : null

                       );

            return CustomPnC_ASResAuthorizationModeTypeSerializer is not null
                       ? CustomPnC_ASResAuthorizationModeTypeSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (PnC_ASResAuthorizationModeType1, PnC_ASResAuthorizationModeType2)

        /// <summary>
        /// Compares two plug and charge authorization mode response types for equality.
        /// </summary>
        /// <param name="PnC_ASResAuthorizationModeType1">A plug and charge authorization mode response type.</param>
        /// <param name="PnC_ASResAuthorizationModeType2">Another plug and charge authorization mode response type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (PnC_ASResAuthorizationModeType? PnC_ASResAuthorizationModeType1,
                                           PnC_ASResAuthorizationModeType? PnC_ASResAuthorizationModeType2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(PnC_ASResAuthorizationModeType1, PnC_ASResAuthorizationModeType2))
                return true;

            // If one is null, but not both, return false.
            if (PnC_ASResAuthorizationModeType1 is null || PnC_ASResAuthorizationModeType2 is null)
                return false;

            return PnC_ASResAuthorizationModeType1.Equals(PnC_ASResAuthorizationModeType2);

        }

        #endregion

        #region Operator != (PnC_ASResAuthorizationModeType1, PnC_ASResAuthorizationModeType2)

        /// <summary>
        /// Compares two plug and charge authorization mode response types for inequality.
        /// </summary>
        /// <param name="PnC_ASResAuthorizationModeType1">A plug and charge authorization mode response type.</param>
        /// <param name="PnC_ASResAuthorizationModeType2">Another plug and charge authorization mode response type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (PnC_ASResAuthorizationModeType? PnC_ASResAuthorizationModeType1,
                                           PnC_ASResAuthorizationModeType? PnC_ASResAuthorizationModeType2)

            => !(PnC_ASResAuthorizationModeType1 == PnC_ASResAuthorizationModeType2);

        #endregion

        #endregion

        #region IEquatable<PnC_ASResAuthorizationModeType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two plug and charge authorization mode response types for equality.
        /// </summary>
        /// <param name="Object">A plug and charge authorization mode response type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is PnC_ASResAuthorizationModeType pnc_ASResAuthorizationModeType &&
                   Equals(pnc_ASResAuthorizationModeType);

        #endregion

        #region Equals(PnC_ASResAuthorizationModeType)

        /// <summary>
        /// Compares two plug and charge authorization mode response types for equality.
        /// </summary>
        /// <param name="PnC_ASResAuthorizationModeType">A plug and charge authorization mode response type to compare with.</param>
        public Boolean Equals(PnC_ASResAuthorizationModeType? PnC_ASResAuthorizationModeType)

            => PnC_ASResAuthorizationModeType is not null &&

               GenChallenge.Equals(PnC_ASResAuthorizationModeType.GenChallenge) &&

               ProviderIds.Count().Equals(PnC_ASResAuthorizationModeType.ProviderIds.Count()) &&
               ProviderIds.All(data => PnC_ASResAuthorizationModeType.ProviderIds.Contains(data));

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

                return GenChallenge.GetHashCode() * 5 ^
                       //ToDo: Add ProviderIds!
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

                   GenChallenge,
                   ", ",
                   ProviderIds.Count(),
                   " provider identification(s)"

               );

        #endregion

    }

}
