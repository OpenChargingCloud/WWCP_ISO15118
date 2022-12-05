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

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    public class Dynamic_SEResControlModeType : ScheduleExchangeResponse
    {

        public UInt32?                     DepartureTime            { get; }
        public SByte?                      MinimumSOC               { get; }
        public SByte?                      TargetSOC                { get; }


        // Choose one of the following...
        public AbsolutePriceScheduleType?  AbsolutePriceSchedule    { get; }
        public PriceLevelScheduleType?     PriceLevelSchedule       { get; }


        #region Documentation

        // <xs:complexType name="Dynamic_SEResControlModeType">
        //     <xs:sequence>
        //
        //         <xs:element name="DepartureTime" type="xs:unsignedInt"            minOccurs="0"/>
        //         <xs:element name="MinimumSOC"    type="v2gci_ct:percentValueType" minOccurs="0"/>
        //         <xs:element name="TargetSOC"     type="v2gci_ct:percentValueType" minOccurs="0"/>
        //
        //         <xs:choice minOccurs="0">
        //             <xs:element name="AbsolutePriceSchedule" type="AbsolutePriceScheduleType"/>
        //             <xs:element name="PriceLevelSchedule" type="PriceLevelScheduleType"/>
        //         </xs:choice>
        //
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

    }

}
