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

namespace cloud.charging.open.protocols.ISO15118_20.CommonTypes
{

    /// <summary>
    /// The detailed tax.
    /// </summary>
    public class DetailedTax
    {

        #region Properties

        /// <summary>
        /// The unique tax rule identification.
        /// </summary>
        [Mandatory]
        public TaxRule_Id      TaxRuleId    { get; }

        /// <summary>
        /// The amount.
        /// </summary>
        [Mandatory]
        public RationalNumber  Amount       { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new detailed tax.
        /// </summary>
        /// <param name="TaxRuleId"></param>
        /// <param name="Amount"></param>
        public DetailedTax(TaxRule_Id      TaxRuleId,
                           RationalNumber  Amount)
        {

            this.TaxRuleId  = TaxRuleId;
            this.Amount     = Amount;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="DetailedTaxType">
        //     <xs:sequence>
        //         <xs:element name="TaxRuleID" type="numericIDType"/>
        //         <xs:element name="Amount"    type="RationalNumberType"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomDetailedTaxParser = null)

        /// <summary>
        /// Parse the given JSON representation of a detailed tax.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomDetailedTaxParser">A delegate to parse custom detailed taxes.</param>
        public static DetailedTax Parse(JObject                                    JSON,
                                        CustomJObjectParserDelegate<DetailedTax>?  CustomDetailedTaxParser   = null)
        {

            if (TryParse(JSON,
                         out var detailedTax,
                         out var errorResponse,
                         CustomDetailedTaxParser))
            {
                return detailedTax!;
            }

            throw new ArgumentException("The given JSON representation of a detailed tax is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out DetailedTax, out ErrorResponse, CustomDetailedTaxParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a detailed tax.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DetailedTax">The parsed detailed tax.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject           JSON,
                                       out DetailedTax?  DetailedTax,
                                       out String?       ErrorResponse)

            => TryParse(JSON,
                        out DetailedTax,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a detailed tax.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DetailedTax">The parsed detailed tax.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomDetailedTaxParser">A delegate to parse custom contract certificates.</param>
        public static Boolean TryParse(JObject                                    JSON,
                                       out DetailedTax?                           DetailedTax,
                                       out String?                                ErrorResponse,
                                       CustomJObjectParserDelegate<DetailedTax>?  CustomDetailedTaxParser)
        {

            try
            {

                DetailedTax = null;

                #region TaxRuleId    [mandatory]

                if (!JSON.ParseMandatory("taxRuleId",
                                         "tax rule identification",
                                         TaxRule_Id.TryParse,
                                         out TaxRule_Id TaxRuleId,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Amount       [mandatory]

                if (!JSON.ParseMandatoryJSON("amount",
                                             "amount",
                                             RationalNumber.TryParse,
                                             out RationalNumber Amount,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion


                DetailedTax = new DetailedTax(TaxRuleId,
                                              Amount);

                if (CustomDetailedTaxParser is not null)
                    DetailedTax = CustomDetailedTaxParser(JSON,
                                                          DetailedTax);

                return true;

            }
            catch (Exception e)
            {
                DetailedTax    = null;
                ErrorResponse  = "The given JSON representation of a detailed tax is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomDetailedTaxSerializer = null, CustomRationalNumberSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomDetailedTaxSerializer">A delegate to serialize custom detailed taxes.</param>
        /// <param name="CustomRationalNumberSerializer">A delegate to serialize custom rational numbers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<DetailedTax>?     CustomDetailedTaxSerializer      = null,
                              CustomJObjectSerializerDelegate<RationalNumber>?  CustomRationalNumberSerializer   = null)
        {

            var json = JSONObject.Create(

                           new JProperty("taxRuleId",  TaxRuleId.ToString()),
                           new JProperty("amount",     Amount.   ToJSON(CustomRationalNumberSerializer))

                       );

            return CustomDetailedTaxSerializer is not null
                       ? CustomDetailedTaxSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (DetailedTax1, DetailedTax2)

        /// <summary>
        /// Compares two detailed taxes for equality.
        /// </summary>
        /// <param name="DetailedTax1">A detailed tax.</param>
        /// <param name="DetailedTax2">Another detailed tax.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (DetailedTax? DetailedTax1,
                                           DetailedTax? DetailedTax2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(DetailedTax1, DetailedTax2))
                return true;

            // If one is null, but not both, return false.
            if (DetailedTax1 is null || DetailedTax2 is null)
                return false;

            return DetailedTax1.Equals(DetailedTax2);

        }

        #endregion

        #region Operator != (DetailedTax1, DetailedTax2)

        /// <summary>
        /// Compares two detailed taxes for inequality.
        /// </summary>
        /// <param name="DetailedTax1">A detailed tax.</param>
        /// <param name="DetailedTax2">Another detailed tax.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (DetailedTax? DetailedTax1,
                                           DetailedTax? DetailedTax2)

            => !(DetailedTax1 == DetailedTax2);

        #endregion

        #endregion

        #region IEquatable<DetailedTax> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two detailed taxes for equality.
        /// </summary>
        /// <param name="Object">A detailed tax to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is DetailedTax detailedTax &&
                   Equals(detailedTax);

        #endregion

        #region Equals(DetailedTax)

        /// <summary>
        /// Compares two detailed taxes for equality.
        /// </summary>
        /// <param name="DetailedTax">A detailed tax to compare with.</param>
        public Boolean Equals(DetailedTax? DetailedTax)

            => DetailedTax is not null &&

               TaxRuleId.Equals(DetailedTax.TaxRuleId) &&
               Amount.   Equals(DetailedTax.Amount);

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

                return TaxRuleId.GetHashCode() * 5 ^
                       Amount.   GetHashCode() * 3 ^

                       base.     GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   TaxRuleId.ToString(),
                   ": ",
                   Amount.   ToString()

               );

        #endregion

    }

}
