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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    public class PriceRule
    {

        public RationalNumber   PowerRangeStart                  { get; }
        public RationalNumber   EnergyFee                        { get; }
        public RationalNumber?  ParkingFee                       { get; }
        public UInt32?          ParkingFeePeriod                 { get; }
        public UInt16?          CarbonDioxideEmission            { get; }
        public Byte?            RenewableGenerationPercentage    { get; }


        #region Documentation

        // <xs:complexType name="PriceRuleType">
        //     <xs:sequence>
        //         <xs:element name="EnergyFee"                     type="v2gci_ct:RationalNumberType"/>
        //         <xs:element name="ParkingFee"                    type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //         <xs:element name="ParkingFeePeriod"              type="xs:unsignedInt"              minOccurs="0"/>
        //         <xs:element name="CarbonDioxideEmission"         type="xs:unsignedShort"            minOccurs="0"/>
        //         <xs:element name="RenewableGenerationPercentage" type="xs:unsignedByte"             minOccurs="0"/>
        //         <xs:element name="PowerRangeStart"               type="v2gci_ct:RationalNumberType"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

    }

}
