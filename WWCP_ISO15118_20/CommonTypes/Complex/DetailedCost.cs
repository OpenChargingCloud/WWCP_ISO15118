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

namespace cloud.charging.open.protocols.ISO15118_20.CommonTypes
{

    /// <summary>
    /// The detailed cost.
    /// </summary>
    public class DetailedCost
    {

        #region Properties

        /// <summary>
        /// The amount.
        /// </summary>
        [Mandatory]
        public RationalNumber  Amount         { get; }

        /// <summary>
        /// The cost per unit.
        /// </summary>
        [Mandatory]
        public RationalNumber  CostPerUnit    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new detailed cost.
        /// </summary>
        /// <param name="Amount">The amount.</param>
        /// <param name="CostPerUnit">The cost per unit.</param>
        public DetailedCost(RationalNumber Amount,
                            RationalNumber CostPerUnit)
        {

            this.Amount       = Amount;
            this.CostPerUnit  = CostPerUnit;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="DetailedCostType">
        //     <xs:sequence>
        //         <xs:element name="Amount"      type="RationalNumberType"/>
        //         <xs:element name="CostPerUnit" type="RationalNumberType"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomDetailedCostParser = null)

        /// <summary>
        /// Parse the given JSON representation of a detailed cost.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomDetailedCostParser">An optional delegate to parse custom detailed costs.</param>
        public static DetailedCost Parse(JObject                                     JSON,
                                         CustomJObjectParserDelegate<DetailedCost>?  CustomDetailedCostParser   = null)
        {

            if (TryParse(JSON,
                         out var detailedCost,
                         out var errorResponse,
                         CustomDetailedCostParser))
            {
                return detailedCost!;
            }

            throw new ArgumentException("The given JSON representation of a detailed cost is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out DetailedCost, out ErrorResponse, CustomDetailedCostParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a detailed cost.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DetailedCost">The parsed detailed cost.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject            JSON,
                                       out DetailedCost?  DetailedCost,
                                       out String?        ErrorResponse)

            => TryParse(JSON,
                        out DetailedCost,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a detailed cost.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DetailedCost">The parsed detailed cost.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomDetailedCostParser">An optional delegate to parse custom contract certificates.</param>
        public static Boolean TryParse(JObject                                     JSON,
                                       out DetailedCost?                           DetailedCost,
                                       out String?                                 ErrorResponse,
                                       CustomJObjectParserDelegate<DetailedCost>?  CustomDetailedCostParser)
        {

            try
            {

                DetailedCost = null;

                #region Amount         [mandatory]

                if (!JSON.ParseMandatoryJSON("amount",
                                             "amount",
                                             RationalNumber.TryParse,
                                             out RationalNumber Amount,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region CostPerUnit    [mandatory]

                if (!JSON.ParseMandatoryJSON("costPerUnit",
                                             "costPerUnit",
                                             RationalNumber.TryParse,
                                             out RationalNumber CostPerUnit,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion


                DetailedCost = new DetailedCost(Amount,
                                                CostPerUnit);

                if (CustomDetailedCostParser is not null)
                    DetailedCost = CustomDetailedCostParser(JSON,
                                                            DetailedCost);

                return true;

            }
            catch (Exception e)
            {
                DetailedCost   = null;
                ErrorResponse  = "The given JSON representation of a detailed cost is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomDetailedCostSerializer = null, CustomRationalNumberSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomDetailedCostSerializer">A delegate to serialize custom detailed costs.</param>
        /// <param name="CustomRationalNumberSerializer">A delegate to serialize custom rational numbers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<DetailedCost>?    CustomDetailedCostSerializer     = null,
                              CustomJObjectSerializerDelegate<RationalNumber>?  CustomRationalNumberSerializer   = null)
        {

            var json = JSONObject.Create(

                           new JProperty("amount",       Amount.     ToJSON(CustomRationalNumberSerializer)),
                           new JProperty("costPerUnit",  CostPerUnit.ToJSON(CustomRationalNumberSerializer))

                       );

            return CustomDetailedCostSerializer is not null
                       ? CustomDetailedCostSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (DetailedCost1, DetailedCost2)

        /// <summary>
        /// Compares two detailed costs for equality.
        /// </summary>
        /// <param name="DetailedCost1">A detailed cost.</param>
        /// <param name="DetailedCost2">Another detailed cost.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (DetailedCost? DetailedCost1,
                                           DetailedCost? DetailedCost2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(DetailedCost1, DetailedCost2))
                return true;

            // If one is null, but not both, return false.
            if (DetailedCost1 is null || DetailedCost2 is null)
                return false;

            return DetailedCost1.Equals(DetailedCost2);

        }

        #endregion

        #region Operator != (DetailedCost1, DetailedCost2)

        /// <summary>
        /// Compares two detailed costs for inequality.
        /// </summary>
        /// <param name="DetailedCost1">A detailed cost.</param>
        /// <param name="DetailedCost2">Another detailed cost.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (DetailedCost? DetailedCost1,
                                           DetailedCost? DetailedCost2)

            => !(DetailedCost1 == DetailedCost2);

        #endregion

        #endregion

        #region IEquatable<DetailedCost> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two detailed costs for equality.
        /// </summary>
        /// <param name="Object">A detailed cost to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is DetailedCost detailedCost &&
                   Equals(detailedCost);

        #endregion

        #region Equals(DetailedCost)

        /// <summary>
        /// Compares two detailed costs for equality.
        /// </summary>
        /// <param name="DetailedCost">A detailed cost to compare with.</param>
        public Boolean Equals(DetailedCost? DetailedCost)

            => DetailedCost is not null &&

               Amount.Equals(DetailedCost.Amount) &&
               CostPerUnit.Equals(DetailedCost.CostPerUnit);

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

                return Amount.     GetHashCode() * 5 ^
                       CostPerUnit.GetHashCode() * 3 ^

                       base.       GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   Amount,
                   ", ",
                   CostPerUnit

               );

        #endregion


    }

}
