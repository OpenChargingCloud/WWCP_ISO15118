/*
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

using cloud.charging.open.protocols.ISO15118_20.CommonTypes;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.AC
{

    public class BPT_Scheduled_AC_CLReqControlMode : Scheduled_AC_CLReqControlMode
    {

        public RationalNumber?  EVMaximumDischargePower       { get; }
        public RationalNumber?  EVMaximumDischargePower_L2    { get; }
        public RationalNumber?  EVMaximumDischargePower_L3    { get; }

        public RationalNumber?  EVMinimumDischargePower       { get; }
        public RationalNumber?  EVMinimumDischargePower_L2    { get; }
        public RationalNumber?  EVMinimumDischargePower_L3    { get; }


        public BPT_Scheduled_AC_CLReqControlMode(RationalNumber?  EVTargetEnergyRequest,
                                                 RationalNumber?  EVMaximumEnergyRequest,
                                                 RationalNumber?  EVMinimumEnergyRequest,

                                                 RationalNumber?  EVMaximumChargePower,
                                                 RationalNumber?  EVMaximumChargePower_L2,
                                                 RationalNumber?  EVMaximumChargePower_L3,

                                                 RationalNumber?  EVMinimumChargePower,
                                                 RationalNumber?  EVMinimumChargePower_L2,
                                                 RationalNumber?  EVMinimumChargePower_L3,

                                                 RationalNumber   EVPresentActivePower,
                                                 RationalNumber?  EVPresentActivePower_L2,
                                                 RationalNumber?  EVPresentActivePower_L3,

                                                 RationalNumber?  EVPresentReactivePower,
                                                 RationalNumber?  EVPresentReactivePower_L2,
                                                 RationalNumber?  EVPresentReactivePower_L3,

                                                 RationalNumber?  EVMaximumDischargePower,
                                                 RationalNumber?  EVMaximumDischargePower_L2,
                                                 RationalNumber?  EVMaximumDischargePower_L3,

                                                 RationalNumber?  EVMinimumDischargePower,
                                                 RationalNumber?  EVMinimumDischargePower_L2,
                                                 RationalNumber?  EVMinimumDischargePower_L3)

            : base(EVTargetEnergyRequest,
                   EVMaximumEnergyRequest,
                   EVMinimumEnergyRequest,

                   EVMaximumChargePower,
                   EVMaximumChargePower_L2,
                   EVMaximumChargePower_L3,

                   EVMinimumChargePower,
                   EVMinimumChargePower_L2,
                   EVMinimumChargePower_L3,

                   EVPresentActivePower,
                   EVPresentActivePower_L2,
                   EVPresentActivePower_L3,

                   EVPresentReactivePower,
                   EVPresentReactivePower_L2,
                   EVPresentReactivePower_L3)

        {

            this.EVMaximumDischargePower     = EVMaximumDischargePower;
            this.EVMaximumDischargePower_L2  = EVMaximumDischargePower_L2;
            this.EVMaximumDischargePower_L3  = EVMaximumDischargePower_L3;

            this.EVMinimumDischargePower     = EVMinimumDischargePower;
            this.EVMinimumDischargePower_L2  = EVMinimumDischargePower_L2;
            this.EVMinimumDischargePower_L3  = EVMinimumDischargePower_L3;

        }


        #region Documentation

        // <xs:complexType name="BPT_Scheduled_AC_CLReqControlModeType">
        //     <xs:complexContent>
        //         <xs:extension base="Scheduled_AC_CLReqControlModeType">
        //             <xs:sequence>
        //                 <xs:element name="EVMaximumDischargePower"    type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVMaximumDischargePower_L2" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVMaximumDischargePower_L3" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVMinimumDischargePower"    type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVMinimumDischargePower_L2" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //                 <xs:element name="EVMinimumDischargePower_L3" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion


    }

}
