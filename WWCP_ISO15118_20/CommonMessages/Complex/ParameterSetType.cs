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
    /// A parameter set.
    /// </summary>
    public class ParameterSet
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


    }

}
