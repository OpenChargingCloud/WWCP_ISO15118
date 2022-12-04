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

    public class EIM_ASResAuthorizationModeType
    {

        #region Constructor(s)

        /// <summary>
        /// Create a new EIM_ASResAuthorizationModeType.
        /// </summary>
        public EIM_ASResAuthorizationModeType()
        { }

        #endregion


        #region (static) Parse   (JSON, CustomEIM_ASResAuthorizationModeTypeParser = null)

        /// <summary>
        /// Parse the given JSON representation of an EIM_ASResAuthorizationModeType.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomEIM_ASResAuthorizationModeTypeParser">A delegate to parse custom EIM_ASResAuthorizationModeTypes.</param>
        public static EIM_ASResAuthorizationModeType Parse(JObject                                                       JSON,
                                                           CustomJObjectParserDelegate<EIM_ASResAuthorizationModeType>?  CustomEIM_ASResAuthorizationModeTypeParser   = null)
        {

            if (TryParse(JSON,
                         out var eim_ASResAuthorizationModeType,
                         out var errorResponse,
                         CustomEIM_ASResAuthorizationModeTypeParser))
            {
                return eim_ASResAuthorizationModeType!;
            }

            throw new ArgumentException("The given JSON representation of an EIM_ASResAuthorizationModeType is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out EIM_ASResAuthorizationModeType, out ErrorResponse, CustomEIM_ASResAuthorizationModeTypeParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of an EIM_ASResAuthorizationModeType.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EIM_ASResAuthorizationModeType">The parsed EIM_ASResAuthorizationModeType.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                              JSON,
                                       out EIM_ASResAuthorizationModeType?  EIM_ASResAuthorizationModeType,
                                       out String?                          ErrorResponse)

            => TryParse(JSON,
                        out EIM_ASResAuthorizationModeType,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an EIM_ASResAuthorizationModeType.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EIM_ASResAuthorizationModeType">The parsed EIM_ASResAuthorizationModeType.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomEIM_ASResAuthorizationModeTypeParser">A delegate to parse custom EIM_ASResAuthorizationModeTypes.</param>
        public static Boolean TryParse(JObject                                                       JSON,
                                       out EIM_ASResAuthorizationModeType?                           EIM_ASResAuthorizationModeType,
                                       out String?                                                   ErrorResponse,
                                       CustomJObjectParserDelegate<EIM_ASResAuthorizationModeType>?  CustomEIM_ASResAuthorizationModeTypeParser)
        {

            try
            {

                EIM_ASResAuthorizationModeType  = new EIM_ASResAuthorizationModeType();
                ErrorResponse                   = null;

                if (CustomEIM_ASResAuthorizationModeTypeParser is not null)
                    EIM_ASResAuthorizationModeType = CustomEIM_ASResAuthorizationModeTypeParser(JSON,
                                                                                                EIM_ASResAuthorizationModeType);

                return true;

            }
            catch (Exception e)
            {
                EIM_ASResAuthorizationModeType  = null;
                ErrorResponse                   = "The given JSON representation of an EIM_ASResAuthorizationModeType is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomEIM_ASResAuthorizationModeTypeSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomEIM_ASResAuthorizationModeTypeSerializer">A delegate to serialize custom EIM_ASResAuthorizationModeTypes.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<EIM_ASResAuthorizationModeType>? CustomEIM_ASResAuthorizationModeTypeSerializer = null)
        {

            var json = JSONObject.Create();

            return CustomEIM_ASResAuthorizationModeTypeSerializer is not null
                       ? CustomEIM_ASResAuthorizationModeTypeSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (EIM_ASResAuthorizationModeType1, EIM_ASResAuthorizationModeType2)

        /// <summary>
        /// Compares two EIM_ASResAuthorizationModeTypes for equality.
        /// </summary>
        /// <param name="EIM_ASResAuthorizationModeType1">An EIM_ASResAuthorizationModeType.</param>
        /// <param name="EIM_ASResAuthorizationModeType2">Another EIM_ASResAuthorizationModeType.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EIM_ASResAuthorizationModeType? EIM_ASResAuthorizationModeType1,
                                           EIM_ASResAuthorizationModeType? EIM_ASResAuthorizationModeType2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(EIM_ASResAuthorizationModeType1, EIM_ASResAuthorizationModeType2))
                return true;

            // If one is null, but not both, return false.
            if (EIM_ASResAuthorizationModeType1 is null || EIM_ASResAuthorizationModeType2 is null)
                return false;

            return EIM_ASResAuthorizationModeType1.Equals(EIM_ASResAuthorizationModeType2);

        }

        #endregion

        #region Operator != (EIM_ASResAuthorizationModeType1, EIM_ASResAuthorizationModeType2)

        /// <summary>
        /// Compares two EIM_ASResAuthorizationModeTypes for inequality.
        /// </summary>
        /// <param name="EIM_ASResAuthorizationModeType1">An EIM_ASResAuthorizationModeType.</param>
        /// <param name="EIM_ASResAuthorizationModeType2">Another EIM_ASResAuthorizationModeType.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EIM_ASResAuthorizationModeType? EIM_ASResAuthorizationModeType1,
                                           EIM_ASResAuthorizationModeType? EIM_ASResAuthorizationModeType2)

            => !(EIM_ASResAuthorizationModeType1 == EIM_ASResAuthorizationModeType2);

        #endregion

        #endregion

        #region IEquatable<EIM_ASResAuthorizationModeType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two EIM_ASResAuthorizationModeTypes for equality.
        /// </summary>
        /// <param name="Object">An EIM_ASResAuthorizationModeType to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EIM_ASResAuthorizationModeType eim_ASResAuthorizationModeType &&
                   Equals(eim_ASResAuthorizationModeType);

        #endregion

        #region Equals(EIM_ASResAuthorizationModeType)

        /// <summary>
        /// Compares two EIM_ASResAuthorizationModeTypes for equality.
        /// </summary>
        /// <param name="EIM_ASResAuthorizationModeType">An EIM_ASResAuthorizationModeType to compare with.</param>
        public Boolean Equals(EIM_ASResAuthorizationModeType? EIM_ASResAuthorizationModeType)

            => EIM_ASResAuthorizationModeType is not null;

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the HashCode of this object.
        /// </summary>
        /// <returns>The HashCode of this object.</returns>
        public override Int32 GetHashCode()

            => base.GetHashCode();

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => nameof(EIM_ASResAuthorizationModeType);

        #endregion


    }

}
