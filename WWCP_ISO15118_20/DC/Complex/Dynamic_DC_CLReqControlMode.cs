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

using cloud.charging.open.protocols.ISO15118_20.CommonTypes;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.DCP
{

    public class Dynamic_DC_CLReqControlMode : ADynamic_CLReqControlMode
    {

        public RationalNumber  EVMaximumChargePower      { get; }
        public RationalNumber  EVMinimumChargePower      { get; }

        public RationalNumber  EVMaximumChargeCurrent    { get; }

        public RationalNumber  EVMaximumVoltage          { get; }
        public RationalNumber  EVMinimumVoltage          { get; }


        public Dynamic_DC_CLReqControlMode(RationalNumber  EVTargetEnergyRequest,
                                           RationalNumber  EVMaximumEnergyRequest,
                                           RationalNumber  EVMinimumEnergyRequest,
                                           DateTime?       DepartureTime,

                                           RationalNumber  EVMaximumChargePower,
                                           RationalNumber  EVMinimumChargePower,
                                           RationalNumber  EVMaximumChargeCurrent,
                                           RationalNumber  EVMaximumVoltage,
                                           RationalNumber  EVMinimumVoltage)

            : base(EVTargetEnergyRequest,
                   EVMaximumEnergyRequest,
                   EVMinimumEnergyRequest,
                   DepartureTime)

        {

            this.EVMaximumChargePower    = EVMaximumChargePower;
            this.EVMinimumChargePower    = EVMinimumChargePower;
            this.EVMaximumChargeCurrent  = EVMaximumChargeCurrent;
            this.EVMaximumVoltage        = EVMaximumVoltage;
            this.EVMinimumVoltage        = EVMinimumVoltage;

        }


        #region Documentation

        // <xs:complexType name="Dynamic_DC_CLReqControlModeType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:Dynamic_CLReqControlModeType">
        //             <xs:sequence>
        //                 <xs:element name="EVMaximumChargePower"   type="v2gci_ct:RationalNumberType"/>
        //                 <xs:element name="EVMinimumChargePower"   type="v2gci_ct:RationalNumberType"/>
        //                 <xs:element name="EVMaximumChargeCurrent" type="v2gci_ct:RationalNumberType"/>
        //                 <xs:element name="EVMaximumVoltage"       type="v2gci_ct:RationalNumberType"/>
        //                 <xs:element name="EVMinimumVoltage"       type="v2gci_ct:RationalNumberType"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion


        public override JObject ToJSON(CustomJObjectSerializerDelegate<RationalNumber>? CustomRationalNumberSerializer = null)
        {
            throw new NotImplementedException();
        }


    }

}
