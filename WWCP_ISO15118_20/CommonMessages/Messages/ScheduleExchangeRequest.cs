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

    public abstract class ScheduleExchangeRequest : AV2GRequest<ScheduleExchangeRequest>
    {

        #region Properties

        /// <summary>
        /// The maximum number of supporting points of the energy schedule.
        /// </summary>
        [Mandatory]
        public Int32  MaximumSupportingPoints    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// The selected energy transfer service.
        /// </summary>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="MaximumSupportingPoints">The maximum number of supporting points of the energy schedule.</param>
        public ScheduleExchangeRequest(MessageHeader  MessageHeader,
                                       Int32          MaximumSupportingPoints)

            : base(MessageHeader)

        {

            this.MaximumSupportingPoints = MaximumSupportingPoints;

        }

        #endregion


        #region Documentation

        // <xs:element name="ScheduleExchangeReq" type="ScheduleExchangeReqType"/>
        // <xs:complexType name="ScheduleExchangeReqType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:V2GRequestType">
        //             <xs:sequence>
        //                 <xs:element name="MaximumSupportingPoints" type="maxSupportingPointsScheduleTupleType"/>
        //                 <xs:choice>
        //                     <xs:element name="Dynamic_SEReqControlMode"   type="Dynamic_SEReqControlModeType"/>
        //                     <xs:element name="Scheduled_SEReqControlMode" type="Scheduled_SEReqControlModeType"/>
        //                 </xs:choice>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion


    }

}
