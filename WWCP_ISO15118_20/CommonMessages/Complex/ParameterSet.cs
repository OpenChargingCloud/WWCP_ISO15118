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

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// A parameter set.
    /// </summary>
    public class ParameterSet : IEquatable<ParameterSet>
    {

        #region Properties

        /// <summary>
        /// The unique identification of this parameter set.
        /// </summary>
        [Mandatory]
        public ParameterSet_Id         ParameterSetId    { get; }

        /// <summary>
        /// The enumeration of parameters [max 32].
        /// </summary>
        [Mandatory]
        public IEnumerable<Parameter>  Parameters        { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new parameter set.
        /// </summary>
        /// <param name="ParameterSetId">An unique identification for this parameter set.</param>
        /// <param name="Parameters">An enumeration of parameters [max 32].</param>
        public ParameterSet(ParameterSet_Id         ParameterSetId,
                            IEnumerable<Parameter>  Parameters)
        {

            this.ParameterSetId  = ParameterSetId;
            this.Parameters      = Parameters.Distinct();

        }

        #endregion


        #region (static) Parse   (JSON, CustomParameterSetParser = null)

        /// <summary>
        /// Parse the given JSON representation of a parameter set.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomParameterSetParser">A delegate to parse a custom parameter set.</param>
        public static ParameterSet Parse(JObject                                     JSON,
                                         CustomJObjectParserDelegate<ParameterSet>?  CustomParameterSetParser = null)
        {

            if (TryParse(JSON,
                         out var parameterSet,
                         out var errorResponse,
                         CustomParameterSetParser))
            {
                return parameterSet!;
            }

            throw new ArgumentException("The given JSON representation of a parameter set is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out ParameterSet, out ErrorResponse, CustomParameterSetParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a parameter set.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ParameterSet">The parsed parameter set.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject            JSON,
                                       out ParameterSet?  ParameterSet,
                                       out String?        ErrorResponse)

            => TryParse(JSON,
                        out ParameterSet,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a parameter set.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ParameterSet">The parsed parameter set.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomParameterSetParser">A delegate to parse custom parameter sets.</param>
        public static Boolean TryParse(JObject                                     JSON,
                                       out ParameterSet?                           ParameterSet,
                                       out String?                                 ErrorResponse,
                                       CustomJObjectParserDelegate<ParameterSet>?  CustomParameterSetParser)
        {

            ErrorResponse = null;

            try
            {

                ParameterSet = null;

                #region ParameterSetId    [mandatory]

                if (!JSON.ParseMandatory("parameterSetId",
                                         "parameter set identification",
                                         ParameterSet_Id.TryParse,
                                         out ParameterSet_Id ParameterSetId,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parameters        [mandatory]

                if (!JSON.ParseMandatoryHashSet("parameters",
                                                "parameters",
                                                Parameter.TryParse,
                                                out HashSet<Parameter> Parameters,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion


                ParameterSet = new ParameterSet(ParameterSetId,
                                                Parameters);

                if (CustomParameterSetParser is not null)
                    ParameterSet = CustomParameterSetParser(JSON,
                                                            ParameterSet);

                return true;

            }
            catch (Exception e)
            {
                ParameterSet   = null;
                ErrorResponse  = "The given JSON representation of a parameter set is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomParameterSetSerializer = null, CustomParameterSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomParameterSerializer">A delegate to serialize custom parameter sets.</param>
        /// <param name="CustomParameterSerializer">A delegate to serialize custom parameters.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<ParameterSet>?  CustomParameterSetSerializer   = null,
                              CustomJObjectSerializerDelegate<Parameter>?     CustomParameterSerializer      = null)
        {

            var json = JSONObject.Create(

                           new JProperty("parameterSetId",  ParameterSetId.ToString()),
                           new JProperty("parameters",      new JArray(Parameters.Select(parameter => parameter.ToJSON(CustomParameterSerializer))))

                       );

            return CustomParameterSetSerializer is not null
                       ? CustomParameterSetSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (ParameterSet1, ParameterSet2)

        /// <summary>
        /// Compares two parameter sets for equality.
        /// </summary>
        /// <param name="ParameterSet1">A parameter set.</param>
        /// <param name="ParameterSet2">Another parameter set.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ParameterSet? ParameterSet1,
                                           ParameterSet? ParameterSet2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(ParameterSet1, ParameterSet2))
                return true;

            // If one is null, but not both, return false.
            if (ParameterSet1 is null || ParameterSet2 is null)
                return false;

            return ParameterSet1.Equals(ParameterSet2);

        }

        #endregion

        #region Operator != (ParameterSet1, ParameterSet2)

        /// <summary>
        /// Compares two parameter sets for inequality.
        /// </summary>
        /// <param name="ParameterSet1">A parameter set.</param>
        /// <param name="ParameterSet2">Another parameter set.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ParameterSet? ParameterSet1,
                                           ParameterSet? ParameterSet2)

            => !(ParameterSet1 == ParameterSet2);

        #endregion

        #endregion

        #region IEquatable<ParameterSet> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two parameter sets for equality.
        /// </summary>
        /// <param name="Object">A parameter set to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ParameterSet parameterSet &&
                   Equals(parameterSet);

        #endregion

        #region Equals(ParameterSet)

        /// <summary>
        /// Compares two parameter sets for equality.
        /// </summary>
        /// <param name="ParameterSet">A parameter set to compare with.</param>
        public Boolean Equals(ParameterSet? ParameterSet)

            => ParameterSet is not null &&

               ParameterSetId.Equals(ParameterSet.ParameterSetId) &&

               Parameters.Count().Equals(ParameterSet.Parameters.Count()) &&
               Parameters.All(parameter => ParameterSet.Parameters.Contains(parameter));

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

                return ParameterSetId.GetHashCode()  * 5 ^
                       Parameters.    CalcHashCode() * 3 ^

                       base.          GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   ParameterSetId,

                   Parameters.Count(),
                   " parameter(s)"

               );

        #endregion

    }

}
