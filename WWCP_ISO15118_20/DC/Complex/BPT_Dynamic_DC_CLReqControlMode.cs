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

using cloud.charging.open.protocols.ISO15118_20.DCP;
using cloud.charging.open.protocols.ISO15118_20.CommonTypes;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.DC
{

    public class BPT_Dynamic_DC_CLReqControlMode : Dynamic_DC_CLReqControlMode
    {

        public RationalNumber   EVMaximumDischargePower      { get; }
        public RationalNumber   EVMinimumDischargePower      { get; }
        public RationalNumber   EVMaximumDischargeCurrent    { get; }

        public RationalNumber?  EVMaximumV2XEnergyRequest    { get; }
        public RationalNumber?  EVMinimumV2XEnergyRequest    { get; }


        public BPT_Dynamic_DC_CLReqControlMode(RationalNumber   EVTargetEnergyRequest,
                                               RationalNumber   EVMaximumEnergyRequest,
                                               RationalNumber   EVMinimumEnergyRequest,
                                               DateTime?        DepartureTime,

                                               RationalNumber   EVMaximumChargePower,
                                               RationalNumber   EVMinimumChargePower,
                                               RationalNumber   EVMaximumChargeCurrent,
                                               RationalNumber   EVMaximumVoltage,
                                               RationalNumber   EVMinimumVoltage,

                                               RationalNumber   EVMaximumDischargePower,
                                               RationalNumber   EVMinimumDischargePower,
                                               RationalNumber   EVMaximumDischargeCurrent,

                                               RationalNumber?  EVMaximumV2XEnergyRequest,
                                               RationalNumber?  EVMinimumV2XEnergyRequest)

            : base(EVTargetEnergyRequest,
                   EVMaximumEnergyRequest,
                   EVMinimumEnergyRequest,
                   DepartureTime,

                   EVMaximumChargePower,
                   EVMinimumChargePower,
                   EVMaximumChargeCurrent,
                   EVMaximumVoltage,
                   EVMinimumVoltage)

        {

            this.EVMaximumDischargePower    = EVMaximumDischargePower;
            this.EVMinimumDischargePower    = EVMinimumDischargePower;
            this.EVMaximumDischargeCurrent  = EVMaximumDischargeCurrent;

            this.EVMaximumV2XEnergyRequest  = EVMaximumV2XEnergyRequest;
            this.EVMinimumV2XEnergyRequest  = EVMinimumV2XEnergyRequest;

        }


        #region Documentation

        // <xs:complexType name="BPT_Dynamic_DC_CLReqControlModeType">
        //     <xs:complexContent>
        //         <xs:extension base="Dynamic_DC_CLReqControlModeType">
        //             <xs:sequence>
        //                 <xs:element name="EVMaximumDischargePower"   type="v2gci_ct:RationalNumberType"/>
        //                 <xs:element name="EVMinimumDischargePower"   type="v2gci_ct:RationalNumberType"/>
        //                 <xs:element name="EVMaximumDischargeCurrent" type="v2gci_ct:RationalNumberType"/>
        //                 <xs:element name="EVMaximumV2XEnergyRequest" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVMinimumV2XEnergyRequest" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion


    }

}
