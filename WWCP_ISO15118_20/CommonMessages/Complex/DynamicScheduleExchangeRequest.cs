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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

using cloud.charging.open.protocols.ISO15118_20.CommonTypes;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// The dynamic schedule exchange request.
    /// </summary>
    public class DynamicScheduleExchangeRequest : ScheduleExchangeRequest,
                                                  IEqualityComparer<DynamicScheduleExchangeRequest>
    {

        #region Properties

        public DateTime         DepartureTime                { get; }
        public PercentValue?    MinimumSOC                   { get; }
        public PercentValue?    TargetSOC                    { get; }
        public RationalNumber   EVTargetEnergyRequest        { get; }
        public RationalNumber   EVMaximumEnergyRequest       { get; }
        public RationalNumber   EVMinimumEnergyRequest       { get; }
        public RationalNumber?  EVMaximumV2XEnergyRequest    { get; }
        public RationalNumber?  EVMinimumV2XEnergyRequest    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new dynamic schedule exchange request.
        /// </summary>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="MaximumSupportingPoints"></param>
        /// <param name="DepartureTime"></param>
        /// <param name="EVTargetEnergyRequest"></param>
        /// <param name="EVMaximumEnergyRequest"></param>
        /// <param name="EVMinimumEnergyRequest"></param>
        /// <param name="MinimumSOC"></param>
        /// <param name="TargetSOC"></param>
        /// <param name="EVMaximumV2XEnergyRequest"></param>
        /// <param name="EVMinimumV2XEnergyRequest"></param>
        public DynamicScheduleExchangeRequest(MessageHeader    MessageHeader,
                                              Int32            MaximumSupportingPoints,
                                              DateTime         DepartureTime,
                                              RationalNumber   EVTargetEnergyRequest,
                                              RationalNumber   EVMaximumEnergyRequest,
                                              RationalNumber   EVMinimumEnergyRequest,
                                              PercentValue?    MinimumSOC                  = null,
                                              PercentValue?    TargetSOC                   = null,
                                              RationalNumber?  EVMaximumV2XEnergyRequest   = null,
                                              RationalNumber?  EVMinimumV2XEnergyRequest   = null)

            : base(MessageHeader,
                   MaximumSupportingPoints)

        {

            this.DepartureTime              = DepartureTime;
            this.EVTargetEnergyRequest      = EVTargetEnergyRequest;
            this.EVMaximumEnergyRequest     = EVMaximumEnergyRequest;
            this.EVMinimumEnergyRequest     = EVMinimumEnergyRequest;
            this.MinimumSOC                 = MinimumSOC;
            this.TargetSOC                  = TargetSOC;
            this.EVMaximumV2XEnergyRequest  = EVMaximumV2XEnergyRequest;
            this.EVMinimumV2XEnergyRequest  = EVMinimumV2XEnergyRequest;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="Dynamic_SEReqControlModeType">
        //     <xs:sequence>
        //         <xs:element name="DepartureTime"             type="xs:unsignedInt"/>
        //         <xs:element name="MinimumSOC"                type="v2gci_ct:percentValueType" minOccurs="0"/>
        //         <xs:element name="TargetSOC"                 type="v2gci_ct:percentValueType" minOccurs="0"/>
        //         <xs:element name="EVTargetEnergyRequest"     type="v2gci_ct:RationalNumberType"/>
        //         <xs:element name="EVMaximumEnergyRequest"    type="v2gci_ct:RationalNumberType"/>
        //         <xs:element name="EVMinimumEnergyRequest"    type="v2gci_ct:RationalNumberType"/>
        //         <xs:element name="EVMaximumV2XEnergyRequest" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //         <xs:element name="EVMinimumV2XEnergyRequest" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion


    }

}
