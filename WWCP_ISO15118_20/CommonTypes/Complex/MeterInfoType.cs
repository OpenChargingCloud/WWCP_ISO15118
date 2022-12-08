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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonTypes
{

    public class MeterInfoType
    {

        public Meter_Id         MeterId                           { get; }

        public UInt64           ChargedEnergyReadingWh            { get; }

        public UInt64?          BPT_DischargedEnergyReadingWh     { get; }

        public UInt64?          CapacitiveEnergyReadingVARh       { get; }

        public UInt64?          BPT_InductiveEnergyReadingVARh    { get; }

        public MeterSignature?  MeterSignature                    { get; }

        public Int16?           MeterStatus                       { get; }

        public UInt64?          MeterTimestamp                    { get; }



        // <xs:complexType name="MeterInfoType">
        //     <xs:sequence>
        //         <xs:element name="MeterID"                        type="meterIDType"/>
        //         <xs:element name="ChargedEnergyReadingWh"         type="xs:unsignedLong"/>
        //         <xs:element name="BPT_DischargedEnergyReadingWh"  type="xs:unsignedLong"    minOccurs="0"/>
        //         <xs:element name="CapacitiveEnergyReadingVARh"    type="xs:unsignedLong"    minOccurs="0"/>
        //         <xs:element name="BPT_InductiveEnergyReadingVARh" type="xs:unsignedLong"    minOccurs="0"/>
        //         <xs:element name="MeterSignature"                 type="meterSignatureType" minOccurs="0"/>
        //         <xs:element name="MeterStatus"                    type="xs:short"           minOccurs="0"/>
        //         <xs:element name="MeterTimestamp"                 type="xs:unsignedLong"    minOccurs="0"/>
        //     </xs:sequence>
        // </xs:complexType>


    }

}
