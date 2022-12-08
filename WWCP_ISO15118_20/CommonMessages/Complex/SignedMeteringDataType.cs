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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    public class SignedMeteringDataType
    {

        public XML_Id           Id           { get; }

        public Session_Id       SessionId    { get; }

        public MeterInfoType    MeterInfo    { get; }

        public ReceiptType      Receipt      { get; }


        public Dynamic_SMDTControlModeType?    Dynamic_SMDTControlMode      { get; }
        public Scheduled_SMDTControlModeType?  Scheduled_SMDTControlMode    { get; }


        // <xs:complexType name="SignedMeteringDataType">
        //
        //     <xs:sequence>
        //
        //         <xs:element name="SessionID" type="v2gci_ct:sessionIDType"/>
        //         <xs:element name="MeterInfo" type="v2gci_ct:MeterInfoType"/>
        //         <xs:element name="Receipt"   type="v2gci_ct:ReceiptType" minOccurs="0"/>
        //
        //         <xs:choice>
        //             <xs:element name="Dynamic_SMDTControlMode"   type="Dynamic_SMDTControlModeType"/>
        //             <xs:element name="Scheduled_SMDTControlMode" type="Scheduled_SMDTControlModeType"/>
        //         </xs:choice>
        //
        //     </xs:sequence>
        //
        //     <xs:attribute name="Id" type="xs:ID" use="required"/>
        //
        // </xs:complexType>

    }

}
