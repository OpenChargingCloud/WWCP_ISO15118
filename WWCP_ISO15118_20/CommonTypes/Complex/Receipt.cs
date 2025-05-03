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

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonTypes
{

    /// <summary>
    /// The receipt.
    /// </summary>
    public class Receipt
    {

        #region Properties

        /// <summary>
        /// The time anchor.
        /// </summary>
        [Mandatory]
        public DateTime                      TimeAnchor                 { get; }

        /// <summary>
        /// The optional energy costs.
        /// </summary>
        [Optional]
        public DetailedCost?             EnergyCosts                { get; }

        /// <summary>
        /// The optional occupancy costs.
        /// </summary>
        [Optional]
        public DetailedCost?             OccupancyCosts             { get; }

        /// <summary>
        /// The optional additional services costs.
        /// </summary>
        [Optional]
        public DetailedCost?             AdditionalServicesCosts    { get; }

        /// <summary>
        /// The optional overstay costs.
        /// </summary>
        [Optional]
        public DetailedCost?             OverstayCosts              { get; }

        /// <summary>
        /// The optional enumeration of tax costs.
        /// [max 10]
        /// </summary>
        [Optional]
        public IEnumerable<DetailedTax>  TaxCosts                   { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new receipt.
        /// </summary>
        /// <param name="TimeAnchor">A time anchor.</param>
        /// <param name="EnergyCosts">Optional energy costs.</param>
        /// <param name="OccupancyCosts">Optional occupancy costs.</param>
        /// <param name="AdditionalServicesCosts">Optional additional services costs.</param>
        /// <param name="OverstayCosts">Optional overstay costs.</param>
        /// <param name="TaxCosts">An optional enumeration of tax costs.</param>
        public Receipt(DateTime                   TimeAnchor,
                       DetailedCost?              EnergyCosts               = null,
                       DetailedCost?              OccupancyCosts            = null,
                       DetailedCost?              AdditionalServicesCosts   = null,
                       DetailedCost?              OverstayCosts             = null,
                       IEnumerable<DetailedTax>?  TaxCosts                  = null)
        {

            this.TimeAnchor               = TimeAnchor;
            this.EnergyCosts              = EnergyCosts;
            this.OccupancyCosts           = OccupancyCosts;
            this.AdditionalServicesCosts  = AdditionalServicesCosts;
            this.OverstayCosts            = OverstayCosts;
            this.TaxCosts                 = TaxCosts?.Distinct() ?? Array.Empty<DetailedTax>();

        }

        #endregion


        #region Documentation

        // <xs:complexType name="ReceiptType">
        //     <xs:sequence>
        //         <xs:element name="TimeAnchor"              type="xs:unsignedLong"/>
        //         <xs:element name="EnergyCosts"             type="DetailedCostType" minOccurs="0"/>
        //         <xs:element name="OccupancyCosts"          type="DetailedCostType" minOccurs="0"/>
        //         <xs:element name="AdditionalServicesCosts" type="DetailedCostType" minOccurs="0"/>
        //         <xs:element name="OverstayCosts"           type="DetailedCostType" minOccurs="0"/>
        //         <xs:element name="TaxCosts"                type="DetailedTaxType"  minOccurs="0" maxOccurs="10"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomReceiptParser = null)

        /// <summary>
        /// Parse the given JSON representation of a receipt.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomReceiptParser">An optional delegate to parse custom receipts.</param>
        public static Receipt Parse(JObject                                JSON,
                                    CustomJObjectParserDelegate<Receipt>?  CustomReceiptParser   = null)
        {

            if (TryParse(JSON,
                         out var receipt,
                         out var errorResponse,
                         CustomReceiptParser))
            {
                return receipt!;
            }

            throw new ArgumentException("The given JSON representation of a receipt is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out Receipt, out ErrorResponse, CustomReceiptParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a receipt.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="Receipt">The parsed receipt.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject       JSON,
                                       out Receipt?  Receipt,
                                       out String?   ErrorResponse)

            => TryParse(JSON,
                        out Receipt,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a receipt.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="Receipt">The parsed receipt.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomReceiptParser">An optional delegate to parse custom contract certificates.</param>
        public static Boolean TryParse(JObject                                JSON,
                                       out Receipt?                           Receipt,
                                       out String?                            ErrorResponse,
                                       CustomJObjectParserDelegate<Receipt>?  CustomReceiptParser)
        {

            try
            {

                Receipt = null;

                #region TimeAnchor                 [mandatory]

                if (!JSON.ParseMandatory("timeAnchor",
                                         "time anchor",
                                         out DateTime TimeAnchor,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region EnergyCosts                [optional]

                if (JSON.ParseOptionalJSON("energyCosts",
                                           "energy costs",
                                           DetailedCost.TryParse,
                                           out DetailedCost? EnergyCosts,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region OccupancyCosts             [optional]

                if (JSON.ParseOptionalJSON("OccupancyCosts",
                                           "occupancy costs",
                                           DetailedCost.TryParse,
                                           out DetailedCost? OccupancyCosts,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region AdditionalServicesCosts    [optional]

                if (JSON.ParseOptionalJSON("AdditionalServicesCosts",
                                           "additional services costs",
                                           DetailedCost.TryParse,
                                           out DetailedCost? AdditionalServicesCosts,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region OverstayCosts              [optional]

                if (JSON.ParseOptionalJSON("overstayCosts",
                                           "overstay costs",
                                           DetailedCost.TryParse,
                                           out DetailedCost? OverstayCosts,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region TaxCosts                   [optional]

                if (JSON.ParseOptionalHashSet("overstayCosts",
                                              "overstay costs",
                                              DetailedTax.TryParse,
                                              out HashSet<DetailedTax> TaxCosts,
                                              out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                Receipt = new Receipt(TimeAnchor,
                                      EnergyCosts,
                                      OccupancyCosts,
                                      AdditionalServicesCosts,
                                      OverstayCosts,
                                      TaxCosts);

                if (CustomReceiptParser is not null)
                    Receipt = CustomReceiptParser(JSON,
                                                  Receipt);

                return true;

            }
            catch (Exception e)
            {
                Receipt        = null;
                ErrorResponse  = "The given JSON representation of a receipt is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomReceiptSerializer = null, CustomDetailedCostSerializer = null, ...)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomReceiptSerializer">A delegate to serialize custom receipts.</param>
        /// <param name="CustomDetailedCostSerializer">A delegate to serialize custom detailed costs.</param>
        /// <param name="CustomRationalNumberSerializer">A delegate to serialize custom rational numbers.</param>
        /// <param name="CustomDetailedTaxSerializer">A delegate to serialize custom detailed taxes.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<Receipt>?         CustomReceiptSerializer          = null,
                              CustomJObjectSerializerDelegate<DetailedCost>?    CustomDetailedCostSerializer     = null,
                              CustomJObjectSerializerDelegate<RationalNumber>?  CustomRationalNumberSerializer   = null,
                              CustomJObjectSerializerDelegate<DetailedTax>?     CustomDetailedTaxSerializer      = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("timeAnchor",                TimeAnchor.             ToISO8601()),

                           EnergyCosts is not null
                               ? new JProperty("energyCosts",               EnergyCosts.            ToJSON(CustomDetailedCostSerializer,
                                                                                                           CustomRationalNumberSerializer))
                               : null,

                           OccupancyCosts is not null
                               ? new JProperty("occupancyCosts",            OccupancyCosts.         ToJSON(CustomDetailedCostSerializer,
                                                                                                           CustomRationalNumberSerializer))
                               : null,

                           AdditionalServicesCosts is not null
                               ? new JProperty("additionalServicesCosts",   AdditionalServicesCosts.ToJSON(CustomDetailedCostSerializer,
                                                                                                           CustomRationalNumberSerializer))
                               : null,

                           OverstayCosts is not null
                               ? new JProperty("overstayCosts",             OverstayCosts.          ToJSON(CustomDetailedCostSerializer,
                                                                                                           CustomRationalNumberSerializer))
                               : null,

                           TaxCosts.Any()
                               ? new JProperty("taxCosts",                  new JArray(TaxCosts.Select(taxCosts => taxCosts.ToJSON(CustomDetailedTaxSerializer,
                                                                                                                                   CustomRationalNumberSerializer))))
                               : null

                       );

            return CustomReceiptSerializer is not null
                       ? CustomReceiptSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (Receipt1, Receipt2)

        /// <summary>
        /// Compares two receipts for equality.
        /// </summary>
        /// <param name="Receipt1">A receipt.</param>
        /// <param name="Receipt2">Another receipt.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (Receipt? Receipt1,
                                           Receipt? Receipt2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(Receipt1, Receipt2))
                return true;

            // If one is null, but not both, return false.
            if (Receipt1 is null || Receipt2 is null)
                return false;

            return Receipt1.Equals(Receipt2);

        }

        #endregion

        #region Operator != (Receipt1, Receipt2)

        /// <summary>
        /// Compares two receipts for inequality.
        /// </summary>
        /// <param name="Receipt1">A receipt.</param>
        /// <param name="Receipt2">Another receipt.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (Receipt? Receipt1,
                                           Receipt? Receipt2)

            => !(Receipt1 == Receipt2);

        #endregion

        #endregion

        #region IEquatable<Receipt> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two receipts for equality.
        /// </summary>
        /// <param name="Object">A receipt to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is Receipt receipt &&
                   Equals(receipt);

        #endregion

        #region Equals(Receipt)

        /// <summary>
        /// Compares two receipts for equality.
        /// </summary>
        /// <param name="Receipt">A receipt to compare with.</param>
        public Boolean Equals(Receipt? Receipt)

            => Receipt is not null &&

               TimeAnchor.Equals(Receipt.TimeAnchor) &&

             ((EnergyCosts             is     null &&  Receipt.EnergyCosts             is     null) ||
              (EnergyCosts             is not null &&  Receipt.EnergyCosts             is not null && EnergyCosts.            Equals(Receipt.EnergyCosts)))             &&

             ((OccupancyCosts          is     null &&  Receipt.OccupancyCosts          is     null) ||
              (OccupancyCosts          is not null &&  Receipt.OccupancyCosts          is not null && OccupancyCosts.         Equals(Receipt.OccupancyCosts)))          &&

             ((AdditionalServicesCosts is     null &&  Receipt.AdditionalServicesCosts is     null) ||
              (AdditionalServicesCosts is not null &&  Receipt.AdditionalServicesCosts is not null && AdditionalServicesCosts.Equals(Receipt.AdditionalServicesCosts))) &&

             ((OverstayCosts           is     null &&  Receipt.OverstayCosts           is     null) ||
              (OverstayCosts           is not null &&  Receipt.OverstayCosts           is not null && OverstayCosts.          Equals(Receipt.OverstayCosts)))           &&

               TaxCosts.Count().Equals(Receipt.TaxCosts.Count()) &&
               TaxCosts.All(subCertificate => Receipt.TaxCosts.Contains(subCertificate));

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

                return TimeAnchor.              GetHashCode()        * 17 ^
                      (EnergyCosts?.            GetHashCode()  ?? 0) * 13 ^
                      (OccupancyCosts?.         GetHashCode()  ?? 0) * 11 ^
                      (AdditionalServicesCosts?.GetHashCode()  ?? 0) *  7 ^
                      (OverstayCosts?.          GetHashCode()  ?? 0) *  5 ^
                      (TaxCosts?.               CalcHashCode() ?? 0) *  3 ^

                       base.                    GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   TimeAnchor,
                   ": ",

                   EnergyCosts is not null
                       ? ", energy: "              + EnergyCosts
                       : "",

                   OccupancyCosts is not null
                       ? ", occupancy: "           + OccupancyCosts
                       : "",

                   AdditionalServicesCosts is not null
                       ? ", additional services: " + AdditionalServicesCosts
                       : "",

                   OverstayCosts is not null
                       ? ", overstay: "            + OverstayCosts
                       : "",

                   TaxCosts.Any()
                       ? ", "                      + TaxCosts.Count() + " tax costs"
                       : ""

               );

        #endregion

    }

}
