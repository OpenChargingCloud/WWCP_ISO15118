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

using System.Text;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonTypes
{

    public class RationalNumber
    {

        #region Properties

        public SByte  Exponent    { get; }
        public Int16  Value       { get; }

        #endregion

        #region Constructor(s)

        public RationalNumber(SByte  Exponent,
                              Int16  Value)
        {

            this.Exponent  = Exponent;
            this.Value     = Value;

        }

        #endregion


        #region (static) Parse   (JSON, CustomRationalNumberParser = null)

        /// <summary>
        /// Parse the given JSON representation of a rational number.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomRationalNumberParser">A delegate to parse a custom rational number.</param>
        public static RationalNumber Parse(JObject                                       JSON,
                                           CustomJObjectParserDelegate<RationalNumber>?  CustomRationalNumberParser   = null)
        {

            if (TryParse(JSON,
                         out var rationalNumber,
                         out var errorResponse,
                         CustomRationalNumberParser))
            {
                return rationalNumber!;
            }

            throw new ArgumentException("The given JSON representation of a rational number is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out RationalNumber, out ErrorResponse, CustomRationalNumberParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a rational number.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="RationalNumber">The parsed rational number.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject              JSON,
                                       out RationalNumber?  RationalNumber,
                                       out String?          ErrorResponse)

            => TryParse(JSON,
                        out RationalNumber,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a rational number.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="RationalNumber">The parsed rational number.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomRationalNumberParser">A delegate to parse a custom rational number.</param>
        public static Boolean TryParse(JObject                                       JSON,
                                       out RationalNumber?                           RationalNumber,
                                       out String?                                   ErrorResponse,
                                       CustomJObjectParserDelegate<RationalNumber>?  CustomRationalNumberParser   = null)
        {

            ErrorResponse = null;

            try
            {

                RationalNumber = null;

                #region Exponent    [mandatory]

                if (!JSON.ParseMandatory("exponent",
                                         "exponent",
                                         out SByte Exponent,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Name        [mandatory]

                if (!JSON.ParseMandatory("value",
                                         "value",
                                         out Int16 Value,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion


                RationalNumber = new RationalNumber(Exponent,
                                                    Value);

                if (CustomRationalNumberParser is not null)
                    RationalNumber = CustomRationalNumberParser(JSON,
                                                                RationalNumber);

                return true;

            }
            catch (Exception e)
            {
                RationalNumber  = null;
                ErrorResponse   = "The given JSON representation of a rational number is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomRationalNumberSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomRationalNumberSerializer">A delegate to serialize custom rational numbers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<RationalNumber>? CustomRationalNumberSerializer = null)
        {

            var json = JSONObject.Create(

                           new JProperty("exponent",  Exponent),
                           new JProperty("value",     Value)

                       );

            return CustomRationalNumberSerializer is not null
                       ? CustomRationalNumberSerializer(this, json)
                       : json;

        }

        #endregion


    }

}
