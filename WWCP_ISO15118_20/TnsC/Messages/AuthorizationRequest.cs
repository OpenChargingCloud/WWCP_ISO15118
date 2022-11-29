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

using cloud.charging.open.protocols.ISO15118_20.V2gciCt;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.TnsC
{

    public class AuthorizationRequest : AV2GRequest
    {

        public AuthorizationTypes SelectedAuthorizationService { get; }


        // Choose one of the following...
        public EIM_AReqAuthorizationModeType? EIM_AReqAuthorizationMode { get; }
        public PnC_AReqAuthorizationModeType? PnC_AReqAuthorizationMode { get; }


        #region Documentation

        // <ns:AuthorizationReq xmlns:v2gci_ct="urn:iso:std:iso:15118:-20:CommonTypes"
        //                      xmlns:xmlsig="http://www.w3.org/2000/09/xmldsig#"
        //                      xmlns:ns="urn:iso:std:iso:15118:-20:CommonMessages"
        //                      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
        //                      xsi:schemaLocation="urn:iso:std:iso:15118:-20:CommonMessages file://V2G_CI_CommonMessages.xsd">
        //
        //     <v2gci_ct:Header>
        //         [...]
        //     </v2gci_ct:Header>
        //
        //     <ns:SelectedAuthorizationService>EIM</ns:SelectedAuthorizationService>
        //
        //     <ns:PnC_AReqAuthorizationMode ns:Id="AAAAA">
        //         <ns:GenChallenge>YTM0NZomIzI2OTsmIzM0NTueYQ==</ns:GenChallenge>
        //         <ns:ContractCertificateChain>
        //             <ns:Certificate>YTM0NZomIzI2OTsmIzM0NTueYQ==</ns:Certificate>
        //             <ns:SubCertificates>
        //                 <ns:Certificate>YTM0NZomIzI2OTsmIzM0NTueYQ==</ns:Certificate>
        //             </ns:SubCertificates>
        //         </ns:ContractCertificateChain>
        //     </ns:PnC_AReqAuthorizationMode>
        //
        // </ns:AuthorizationReq>

        #endregion

    }

}
