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

using cloud.charging.open.protocols.ISO15118_20.CommonTypes;
using cloud.charging.open.protocols.ISO15118_20.XMLSchema;
using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    public class AbsolutePriceSchedule
    {

        public XML_Id                          Id                            { get; }

        public Currency                        Currency                      { get; }
        public String                          Language                      { get; }
        public PriceAlgorithm_Id               PriceAlgorithm                { get; }
        public RationalNumber?                 MinimumCost                   { get; }
        public RationalNumber?                 MaximumCost                   { get; }

        /// <summary>
        /// [max 10]
        /// </summary>
        [Optional]
        public IEnumerable<TaxRuleType>        TaxRules                      { get; }

        public IEnumerable<PriceRuleStack>     PriceRuleStacks               { get; }
        public OverstayRuleList?               OverstayRules                 { get; }

        /// <summary>
        /// 
        /// </summary>
        [Optional]
        public IEnumerable<AdditionalService>  AdditionalSelectedServices    { get; }


        #region Documentation

        // <xs:complexType name="AbsolutePriceScheduleType">
        //     <xs:complexContent>
        //         <xs:extension base="PriceScheduleType">
        //             <xs:sequence>
        //                 <xs:element name="Currency"                   type="currencyType"/>
        //                 <xs:element name="Language"                   type="languageType"/>
        //                 <xs:element name="PriceAlgorithm"             type="v2gci_ct:identifierType"/>
        //                 <xs:element name="MinimumCost"                type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="MaximumCost"                type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="TaxRules"                   type="TaxRuleListType"             minOccurs="0"/>
        //                 <xs:element name="PriceRuleStacks"            type="PriceRuleStackListType"/>
        //                 <xs:element name="OverstayRules"              type="OverstayRuleListType"        minOccurs="0"/>
        //                 <xs:element name="AdditionalSelectedServices" type="AdditionalServiceListType"   minOccurs="0"/>
        //             </xs:sequence>
        //             <xs:attribute name="Id" type="xs:ID"/>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>


        // <xs:complexType name="TaxRuleListType">
        //     <xs:sequence>
        //         <xs:element name="TaxRule" type="TaxRuleType" maxOccurs="10"/>
        //     </xs:sequence>
        // </xs:complexType>


        // <xs:complexType name="PriceRuleStackListType">
        //     <xs:sequence>
        //         <xs:element name="PriceRuleStack" type="PriceRuleStackType" maxOccurs="1024"/>
        //     </xs:sequence>
        // </xs:complexType>


        // <xs:complexType name="AdditionalServiceListType">
        //     <xs:sequence>
        //         <xs:element name="AdditionalService" type="AdditionalServiceType" maxOccurs="5"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion




    }

}
