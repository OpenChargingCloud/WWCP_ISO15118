﻿/*
 * Copyright (c) 2021-2023 GraphDefined GmbH
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
    /// The tax rule.
    /// </summary>
    public class TaxRule : IEquatable<TaxRule>
    {

        #region Properties

        /// <summary>
        /// The unique identification of the tax rule.
        /// </summary>
        [Mandatory]
        public TaxRule_Id      TaxRuleId                    { get; }

        /// <summary>
        /// The optional name of the tax rule.
        /// </summary>
        [Optional]
        public Name?       TaxRuleName                  { get; }

        /// <summary>
        /// The tax rate.
        /// </summary>
        [Mandatory]
        public RationalNumber  TaxRate                      { get; }

        /// <summary>
        /// Whether the tax is included within the price.
        /// </summary>
        [Optional]
        public Boolean?        TaxIncludedInPrice           { get; }

        /// <summary>
        /// Whether the tax applies to the energy fee.
        /// </summary>
        [Mandatory]
        public Boolean         AppliesToEnergyFee           { get; }

        /// <summary>
        /// Whether the tax applies to the parking fee.
        /// </summary>
        [Mandatory]
        public Boolean         AppliesToParkingFee          { get; }

        /// <summary>
        /// Whether the tax applies to the overstay fee.
        /// </summary>
        [Mandatory]
        public Boolean         AppliesToOverstayFee         { get; }

        /// <summary>
        /// Whether the tax applies to minimum/maximum cost.
        /// </summary>
        [Mandatory]
        public Boolean         AppliesMinimumMaximumCost    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new tax rule.
        /// </summary>
        /// <param name="TaxRuleId">An unique identification of the tax rule.</param>
        /// <param name="TaxRate">An tax rate.</param>
        /// <param name="AppliesToEnergyFee">Whether the tax applies to the energy fee.</param>
        /// <param name="AppliesToParkingFee">Whether the tax applies to the parking fee.</param>
        /// <param name="AppliesToOverstayFee">Whether the tax applies to the overstay fee.</param>
        /// <param name="AppliesMinimumMaximumCost">Whether the tax applies to minimum/maximum cost.</param>
        /// <param name="TaxRuleName">An optional name of the tax rule.</param>
        /// <param name="TaxIncludedInPrice">Whether the tax is included within the price.</param>
        public TaxRule(TaxRule_Id      TaxRuleId,
                       RationalNumber  TaxRate,
                       Boolean         AppliesToEnergyFee,
                       Boolean         AppliesToParkingFee,
                       Boolean         AppliesToOverstayFee,
                       Boolean         AppliesMinimumMaximumCost,
                       Name?           TaxRuleName          = null,
                       Boolean?        TaxIncludedInPrice   = null)

        {

            this.TaxRuleId                  = TaxRuleId;
            this.TaxRate                    = TaxRate;
            this.AppliesToEnergyFee         = AppliesToEnergyFee;
            this.AppliesToParkingFee        = AppliesToParkingFee;
            this.AppliesToOverstayFee       = AppliesToOverstayFee;
            this.AppliesMinimumMaximumCost  = AppliesMinimumMaximumCost;
            this.TaxRuleName                = TaxRuleName;
            this.TaxIncludedInPrice         = TaxIncludedInPrice;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="TaxRuleType">
        //     <xs:sequence>
        //         <xs:element name="TaxRuleID"                 type="v2gci_ct:numericIDType"/>
        //         <xs:element name="TaxRuleName"               type="v2gci_ct:nameType" minOccurs="0"/>
        //         <xs:element name="TaxRate"                   type="v2gci_ct:RationalNumberType"/>
        //         <xs:element name="TaxIncludedInPrice"        type="xs:boolean"        minOccurs="0"/>
        //         <xs:element name="AppliesToEnergyFee"        type="xs:boolean"/>
        //         <xs:element name="AppliesToParkingFee"       type="xs:boolean"/>
        //         <xs:element name="AppliesToOverstayFee"      type="xs:boolean"/>
        //         <xs:element name="AppliesMinimumMaximumCost" type="xs:boolean"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomTaxRuleParser = null)

        /// <summary>
        /// Parse the given JSON representation of a tax rule.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomTaxRuleParser">A delegate to parse custom tax rules.</param>
        public static TaxRule Parse(JObject                                JSON,
                                    CustomJObjectParserDelegate<TaxRule>?  CustomTaxRuleParser   = null)
        {

            if (TryParse(JSON,
                         out var taxRule,
                         out var errorResponse,
                         CustomTaxRuleParser))
            {
                return taxRule!;
            }

            throw new ArgumentException("The given JSON representation of a tax rule is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out TaxRule, out ErrorResponse, CustomTaxRuleParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a tax rule.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="TaxRule">The parsed tax rule.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject       JSON,
                                       out TaxRule?  TaxRule,
                                       out String?   ErrorResponse)

            => TryParse(JSON,
                        out TaxRule,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a tax rule.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="TaxRule">The parsed tax rule.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomTaxRuleParser">A delegate to parse custom contract certificates.</param>
        public static Boolean TryParse(JObject                                JSON,
                                       out TaxRule?                           TaxRule,
                                       out String?                            ErrorResponse,
                                       CustomJObjectParserDelegate<TaxRule>?  CustomTaxRuleParser)
        {

            try
            {

                TaxRule = null;

                #region TaxRuleId                    [mandatory]

                if (!JSON.ParseMandatory("taxRuleId",
                                         "tax rule identification",
                                         TaxRule_Id.TryParse,
                                         out TaxRule_Id TaxRuleId,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region TaxRate                      [mandatory]

                if (!JSON.ParseMandatoryJSON("taxRate",
                                             "tax rate",
                                             RationalNumber.TryParse,
                                             out RationalNumber TaxRate,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region AppliesToEnergyFee           [mandatory]

                if (!JSON.ParseMandatory("appliesToEnergyFee",
                                         "applies to energy fee",
                                         out Boolean AppliesToEnergyFee,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region AppliesToParkingFee          [mandatory]

                if (!JSON.ParseMandatory("appliesToParkingFee",
                                         "applies to parking fee",
                                         out Boolean AppliesToParkingFee,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region AppliesToOverstayFee         [mandatory]

                if (!JSON.ParseMandatory("appliesToOverstayFee",
                                         "applies to overstay fee",
                                         out Boolean AppliesToOverstayFee,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region AppliesMinimumMaximumCost    [mandatory]

                if (!JSON.ParseMandatory("appliesMinimumMaximumCost",
                                         "applies minimum/maximum cost",
                                         out Boolean AppliesMinimumMaximumCost,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region TaxRuleName                  [optional]

                if (!JSON.ParseOptional("taxRuleName",
                                        "tax rule name",
                                        Name.TryParse,
                                        out Name? TaxRuleName,
                                        out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region TaxIncludedInPrice           [optional]

                if (!JSON.ParseOptional("taxIncludedInPrice",
                                        "tax included in price",
                                        out Boolean? TaxIncludedInPrice,
                                        out ErrorResponse))
                {
                    return false;
                }

                #endregion


                TaxRule = new TaxRule(TaxRuleId,
                                      TaxRate,
                                      AppliesToEnergyFee,
                                      AppliesToParkingFee,
                                      AppliesToOverstayFee,
                                      AppliesMinimumMaximumCost,
                                      TaxRuleName,
                                      TaxIncludedInPrice);

                if (CustomTaxRuleParser is not null)
                    TaxRule = CustomTaxRuleParser(JSON,
                                                  TaxRule);

                return true;

            }
            catch (Exception e)
            {
                TaxRule        = null;
                ErrorResponse  = "The given JSON representation of a tax rule is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomTaxRuleSerializer = null, CustomRationalNumberSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomTaxRuleSerializer">A delegate to serialize custom tax rules.</param>
        /// <param name="CustomRationalNumberSerializer">A delegate to serialize custom rational numbers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<TaxRule>?         CustomTaxRuleSerializer          = null,
                              CustomJObjectSerializerDelegate<RationalNumber>?  CustomRationalNumberSerializer   = null)
        {

            var json = JSONObject.Create(

                                  new JProperty("taxRuleId",                  TaxRuleId.        ToString()),
                                  new JProperty("taxRate",                    TaxRate.          ToJSON(CustomRationalNumberSerializer)),
                                  new JProperty("appliesToEnergyFee",         AppliesToEnergyFee),
                                  new JProperty("appliesToParkingFee",        AppliesToParkingFee),
                                  new JProperty("appliesToOverstayFee",       AppliesToOverstayFee),
                                  new JProperty("appliesMinimumMaximumCost",  AppliesMinimumMaximumCost),

                           TaxRuleName.HasValue
                                ? new JProperty("taxRuleName",                TaxRuleName.Value.ToString())
                                : null,

                           TaxIncludedInPrice.HasValue
                                ? new JProperty("taxIncludedInPrice",         TaxIncludedInPrice.Value)
                                : null

                       );

            return CustomTaxRuleSerializer is not null
                       ? CustomTaxRuleSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (TaxRule1, TaxRule2)

        /// <summary>
        /// Compares two tax rules for equality.
        /// </summary>
        /// <param name="TaxRule1">A tax rule.</param>
        /// <param name="TaxRule2">Another tax rule.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (TaxRule? TaxRule1,
                                           TaxRule? TaxRule2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(TaxRule1, TaxRule2))
                return true;

            // If one is null, but not both, return false.
            if (TaxRule1 is null || TaxRule2 is null)
                return false;

            return TaxRule1.Equals(TaxRule2);

        }

        #endregion

        #region Operator != (TaxRule1, TaxRule2)

        /// <summary>
        /// Compares two tax rules for inequality.
        /// </summary>
        /// <param name="TaxRule1">A tax rule.</param>
        /// <param name="TaxRule2">Another tax rule.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (TaxRule? TaxRule1,
                                           TaxRule? TaxRule2)

            => !(TaxRule1 == TaxRule2);

        #endregion

        #endregion

        #region IEquatable<TaxRule> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two tax rules for equality.
        /// </summary>
        /// <param name="Object">A tax rule to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is TaxRule taxRule &&
                   Equals(taxRule);

        #endregion

        #region Equals(TaxRule)

        /// <summary>
        /// Compares two tax rules for equality.
        /// </summary>
        /// <param name="TaxRule">A tax rule to compare with.</param>
        public Boolean Equals(TaxRule? TaxRule)

            => TaxRule is not null &&

               TaxRuleId.                Equals(TaxRule.TaxRuleId)                 &&
               TaxRate.                  Equals(TaxRule.TaxRate)                   &&
               AppliesToEnergyFee.       Equals(TaxRule.AppliesToEnergyFee)        &&
               AppliesToParkingFee.      Equals(TaxRule.AppliesToParkingFee)       &&
               AppliesToOverstayFee.     Equals(TaxRule.AppliesToOverstayFee)      &&
               AppliesMinimumMaximumCost.Equals(TaxRule.AppliesMinimumMaximumCost) &&

            ((!TaxRuleName.       HasValue && !TaxRule.TaxRuleName.       HasValue) ||
              (TaxRuleName.       HasValue &&  TaxRule.TaxRuleName.       HasValue && TaxRuleName.       Value.Equals(TaxRule.TaxRuleName.       Value))) &&

            ((!TaxIncludedInPrice.HasValue && !TaxRule.TaxIncludedInPrice.HasValue) ||
              (TaxIncludedInPrice.HasValue &&  TaxRule.TaxIncludedInPrice.HasValue && TaxIncludedInPrice.Value.Equals(TaxRule.TaxIncludedInPrice.Value)));

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

                return TaxRuleId.                GetHashCode()       * 23 ^
                       TaxRate.                  GetHashCode()       * 19 ^
                       AppliesToEnergyFee.       GetHashCode()       * 17 ^
                       AppliesToParkingFee.      GetHashCode()       * 13 ^
                       AppliesToOverstayFee.     GetHashCode()       * 11 ^
                       AppliesMinimumMaximumCost.GetHashCode()       *  7 ^
                      (TaxRuleName?.             GetHashCode() ?? 0) *  5 ^
                      (TaxIncludedInPrice?.      GetHashCode() ?? 0) *  3 ^

                       base.                     GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   TaxRuleName.HasValue
                       ? TaxRuleName.Value + " (" + TaxRuleId + ") "
                       : TaxRuleId,

                   TaxRate

               );

        #endregion

    }

}
