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
    /// The scheduled schedule exchange request.
    /// </summary>
    public class ScheduledScheduleExchangeRequest : ScheduleExchangeRequest,
                                                    IEqualityComparer<ScheduledScheduleExchangeRequest>
    {

        #region Properties

        public DateTime?        DepartureTime             { get; }
        public RationalNumber?  EVTargetEnergyRequest     { get; }
        public RationalNumber?  EVMaximumEnergyRequest    { get; }
        public RationalNumber?  EVMinimumEnergyRequest    { get; }
        public EVEnergyOffer?   EVEnergyOffer             { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new scheduled schedule exchange request.
        /// </summary>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="MaximumSupportingPoints"></param>
        /// <param name="DepartureTime"></param>
        /// <param name="EVTargetEnergyRequest"></param>
        /// <param name="EVMaximumEnergyRequest"></param>
        /// <param name="EVMinimumEnergyRequest"></param>
        /// <param name="EVEnergyOffer"></param>
        public ScheduledScheduleExchangeRequest(MessageHeader    MessageHeader,
                                                Int32            MaximumSupportingPoints,
                                                DateTime?        DepartureTime,
                                                RationalNumber?  EVTargetEnergyRequest,
                                                RationalNumber?  EVMaximumEnergyRequest,
                                                RationalNumber?  EVMinimumEnergyRequest,
                                                EVEnergyOffer?   EVEnergyOffer)

            : base(MessageHeader,
                   MaximumSupportingPoints)

        {

            this.DepartureTime           = DepartureTime;
            this.EVTargetEnergyRequest   = EVTargetEnergyRequest;
            this.EVMaximumEnergyRequest  = EVMaximumEnergyRequest;
            this.EVMinimumEnergyRequest  = EVMinimumEnergyRequest;
            this.EVEnergyOffer           = EVEnergyOffer;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="Scheduled_SEReqControlModeType">
        //     <xs:sequence>
        //         <xs:element name="DepartureTime"          type="xs:unsignedInt"              minOccurs="0"/>
        //         <xs:element name="EVTargetEnergyRequest"  type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //         <xs:element name="EVMaximumEnergyRequest" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //         <xs:element name="EVMinimumEnergyRequest" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //         <xs:element name="EVEnergyOffer"          type="EVEnergyOfferType"           minOccurs="0"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion


    }

}
