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

using cloud.charging.open.protocols.ISO15118_20.CommonTypes;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// A parameter.
    /// </summary>
    public class Parameter
    {

        #region Properties

        /// <summary>
        /// The parameter name.
        /// </summary>
        public String           Name                   { get; }


        /// <summary>
        /// The boolean value.
        /// </summary>
        [MandatoryChoice("ParameterValue")]
        public Boolean?         BooleanValue           { get; }

        /// <summary>
        /// The byte value.
        /// </summary>
        [MandatoryChoice("ParameterValue")]
        public Byte?            ByteValue              { get; }

        /// <summary>
        /// The UInt16 value.
        /// </summary>
        [MandatoryChoice("ParameterValue")]
        public Int16?           ShortValue             { get; }

        /// <summary>
        /// The Int32 value.
        /// </summary>
        [MandatoryChoice("ParameterValue")]
        public Int32?           IntegerValue           { get; }

        /// <summary>
        /// The rational number value.
        /// </summary>
        [MandatoryChoice("ParameterValue")]
        public RationalNumber?  RationalNumberValue    { get; }

        /// <summary>
        /// The string value [max 80]
        /// </summary>
        [MandatoryChoice("ParameterValue")]
        public String?          FiniteStringValue      { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new parameter.
        /// </summary>
        /// <param name="Name">A parameter name.</param>
        /// 
        /// <param name="BooleanValue">A boolean value.</param>
        /// <param name="ByteValue">A byte value.</param>
        /// <param name="ShortValue">An UInt16 value.</param>
        /// <param name="IntegerValue">An Int32 value.</param>
        /// <param name="RationalNumberValue">A rational number value.</param>
        /// <param name="FiniteStringValue">A finite string value [max 80].</param>
        private Parameter(String           Name,

                          Boolean?         BooleanValue          = null,
                          Byte?            ByteValue             = null,
                          Int16?           ShortValue            = null,
                          Int32?           IntegerValue          = null,
                          RationalNumber?  RationalNumberValue   = null,
                          String?          FiniteStringValue     = null)
        {

            this.Name                 = Name;

            this.BooleanValue         = BooleanValue;
            this.ByteValue            = ByteValue;
            this.ShortValue           = ShortValue;
            this.IntegerValue         = IntegerValue;
            this.RationalNumberValue  = RationalNumberValue;
            this.FiniteStringValue    = FiniteStringValue;

        }

        #endregion


        #region Static methods

        /// <summary>
        /// Create a new parameter having a boolean value.
        /// </summary>
        /// <param name="Name">A parameter name.</param>
        /// <param name="BooleanValue">A boolean value.</param>
        public static Parameter WithBooleanValue       (String          Name,
                                                        Boolean         BooleanValue)

            => new (Name,
                    BooleanValue,
                    null,
                    null,
                    null,
                    null,
                    null);


        /// <summary>
        /// Create a new parameter having a byte value.
        /// </summary>
        /// <param name="Name">A parameter name.</param>
        /// <param name="ByteValue">A byte value.</param>
        public static Parameter WithByteValue          (String          Name,
                                                        Byte            ByteValue)

            => new (Name,
                    null,
                    ByteValue,
                    null,
                    null,
                    null,
                    null);


        /// <summary>
        /// Create a new parameter having a short value.
        /// </summary>
        /// <param name="Name">A parameter name.</param>
        /// <param name="ShortValue">A short value.</param>
        public static Parameter WithShortValue         (String          Name,
                                                        Int16           ShortValue)

            => new (Name,
                    null,
                    null,
                    ShortValue,
                    null,
                    null,
                    null);


        /// <summary>
        /// Create a new parameter having an integer value.
        /// </summary>
        /// <param name="Name">A parameter name.</param>
        /// <param name="IntegerValue">An integer value.</param>
        public static Parameter WithIntegerValue       (String          Name,
                                                        Int32           IntegerValue)

            => new (Name,
                    null,
                    null,
                    null,
                    IntegerValue,
                    null,
                    null);


        /// <summary>
        /// Create a new parameter having a rational number value.
        /// </summary>
        /// <param name="Name">A parameter name.</param>
        /// <param name="RationalNumberValue">A rational number value.</param>
        public static Parameter WithRationalNumberValue(String          Name,
                                                        RationalNumber  RationalNumberValue)

            => new (Name,
                    null,
                    null,
                    null,
                    null,
                    RationalNumberValue,
                    null);


        /// <summary>
        /// Create a new parameter having a string value.
        /// </summary>
        /// <param name="Name">A parameter name.</param>
        /// <param name="FiniteStringValue">A finite string value [max 80].</param>
        public static Parameter WithStringValue        (String          Name,
                                                        String          FiniteStringValue)

            => new (Name,
                    null,
                    null,
                    null,
                    null,
                    null,
                    FiniteStringValue);

        #endregion


        #region (static) Parse   (JSON, CustomParameterParser = null)

        /// <summary>
        /// Parse the given JSON representation of a parameter.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomParameterParser">A delegate to parse a custom parameter.</param>
        public static Parameter Parse(JObject                                  JSON,
                                      CustomJObjectParserDelegate<Parameter>?  CustomParameterParser   = null)
        {

            if (TryParse(JSON,
                         out var parameter,
                         out var errorResponse,
                         CustomParameterParser))
            {
                return parameter!;
            }

            throw new ArgumentException("The given JSON representation of a parameter is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out Parameter, out ErrorResponse, CustomParameterParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a parameter.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="Parameter">The parsed parameter.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject         JSON,
                                       out Parameter?  Parameter,
                                       out String?     ErrorResponse)

            => TryParse(JSON,
                        out Parameter,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a parameter.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="Parameter">The parsed parameter.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomParameterParser">A delegate to parse a custom parameter.</param>
        public static Boolean TryParse(JObject                                  JSON,
                                       out Parameter?                           Parameter,
                                       out String?                              ErrorResponse,
                                       CustomJObjectParserDelegate<Parameter>?  CustomParameterParser   = null)
        {

            ErrorResponse = null;

            try
            {

                Parameter = null;

                #region Name                   [mandatory]

                if (!JSON.ParseMandatoryText("name",
                                             "parameter name",
                                             out String Name,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion


                #region BooleanValue           [optional]

                if (JSON.ParseOptional("booleanValue",
                                       "boolean parameter value",
                                       out Boolean? BooleanValue,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region ByteValue              [optional]

                if (JSON.ParseOptional("byteValue",
                                       "byte parameter value",
                                       out Byte? ByteValue,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region ShortValue             [optional]

                if (JSON.ParseOptional("shortValue",
                                       "short parameter value",
                                       out Int16? ShortValue,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region IntegerValue           [optional]

                if (JSON.ParseOptional("integerValue",
                                       "integer parameter value",
                                       out Int32? IntegerValue,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region RationalNumberValue    [optional]

                if (JSON.ParseOptionalJSON("rationalNumberValue",
                                           "rational number parameter value",
                                           RationalNumber.TryParse,
                                           out RationalNumber? RationalNumberValue,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region FinitStringValue       [optional]

                var FinitStringValue = JSON.GetString("FinitStringValue");

                #endregion


                Parameter = new Parameter(Name,
                                          BooleanValue,
                                          ByteValue,
                                          ShortValue,
                                          IntegerValue,
                                          RationalNumberValue,
                                          FinitStringValue);

                if (CustomParameterParser is not null)
                    Parameter = CustomParameterParser(JSON,
                                                      Parameter);

                return true;

            }
            catch (Exception e)
            {
                Parameter      = null;
                ErrorResponse  = "The given JSON representation of a parameter is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomParameterSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomParameterSerializer">A delegate to serialize custom parameters.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<Parameter>? CustomParameterSerializer = null)
        {

            var json = JSONObject.Create(

                           new JProperty("name",  Name)

                       );

            return CustomParameterSerializer is not null
                       ? CustomParameterSerializer(this, json)
                       : json;

        }

        #endregion



    }

}
