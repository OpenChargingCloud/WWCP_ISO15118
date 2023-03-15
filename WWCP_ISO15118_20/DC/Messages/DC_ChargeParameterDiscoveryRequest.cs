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

namespace cloud.charging.open.protocols.ISO15118_20.DC
{

    public class DC_ChargeParameterDiscoveryRequest : AChargeParameterDiscoveryRequest
    {

        public DC_CPDReqEnergyTransferMode?  DC_CPDReqEnergyTransferMode    { get; }


        #region Documentation

        // <xs:complexType name="DC_ChargeParameterDiscoveryReqType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:ChargeParameterDiscoveryReqType">
        //             <xs:sequence>
        //                 <xs:element ref="DC_CPDReqEnergyTransferMode"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

    }

}
