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
using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// The authorization request message.
    /// </summary>
    public class AuthorizationRequest : AV2GRequest
    {

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        [Mandatory]
        public AuthorizationTypes              SelectedAuthorizationService    { get; }

        /// <summary>
        /// 
        /// </summary>
        [MandatoryChoice("AReqAuthorizationMode")]
        public EIM_AReqAuthorizationModeType?  EIM_AReqAuthorizationMode       { get; }

        /// <summary>
        /// 
        /// </summary>
        [MandatoryChoice("AReqAuthorizationMode")]
        public PnC_AReqAuthorizationModeType?  PnC_AReqAuthorizationMode       { get; }

        #endregion

        #region Constructor(s)

        #region (private) AuthorizationRequest(..., EIM_AReqAuthorizationMode, PnC_AReqAuthorizationMode)

        /// <summary>
        /// Create a new authorization request message.
        /// </summary>
        /// <param name="Header">A message header.</param>
        /// <param name="SelectedAuthorizationService"></param>
        /// <param name="EIM_AReqAuthorizationMode"></param>
        /// <param name="PnC_AReqAuthorizationMode"></param>
        private AuthorizationRequest(MessageHeaderType               Header,
                                     AuthorizationTypes              SelectedAuthorizationService,
                                     EIM_AReqAuthorizationModeType?  EIM_AReqAuthorizationMode,
                                     PnC_AReqAuthorizationModeType?  PnC_AReqAuthorizationMode)

            : base(Header)

        {

            this.SelectedAuthorizationService  = SelectedAuthorizationService;
            this.EIM_AReqAuthorizationMode     = EIM_AReqAuthorizationMode;
            this.PnC_AReqAuthorizationMode     = PnC_AReqAuthorizationMode;

        }

        #endregion

        #region AuthorizationRequest(..., EIM_AReqAuthorizationMode)

        /// <summary>
        /// Create a new authorization request message.
        /// </summary>
        /// <param name="Header">A message header.</param>
        /// <param name="SelectedAuthorizationService"></param>
        /// <param name="EIM_AReqAuthorizationMode"></param>
        public AuthorizationRequest(MessageHeaderType              Header,
                                    AuthorizationTypes             SelectedAuthorizationService,
                                    EIM_AReqAuthorizationModeType  EIM_AReqAuthorizationMode)

            : this(Header,
                   SelectedAuthorizationService,
                   EIM_AReqAuthorizationMode,
                   null)

        { }

        #endregion

        #region AuthorizationRequest(..., PnC_AReqAuthorizationMode)

        /// <summary>
        /// Create a new authorization request message.
        /// </summary>
        /// <param name="Header">A message header.</param>
        /// <param name="SelectedAuthorizationService"></param>
        /// <param name="PnC_AReqAuthorizationMode"></param>
        public AuthorizationRequest(MessageHeaderType              Header,
                                    AuthorizationTypes             SelectedAuthorizationService,
                                    PnC_AReqAuthorizationModeType  PnC_AReqAuthorizationMode)

            : this(Header,
                   SelectedAuthorizationService,
                   null,
                   PnC_AReqAuthorizationMode)

        { }

        #endregion

        #endregion


        #region Documentation

        //<?xml version="1.0" encoding="utf-8"?>
        //<ns:AuthorizationReq xmlns:v2gci_ct="urn:iso:std:iso:15118:-20:CommonTypes"
        //                     xmlns:xmlsig="http://www.w3.org/2000/09/xmldsig#"
        //                     xmlns:ns="urn:iso:std:iso:15118:-20:CommonMessages"
        //                     xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
        //                     xsi:schemaLocation="urn:iso:std:iso:15118:-20:CommonMessages file:///V2G_CI_CommonMessages.xsd">
        //    <v2gci_ct:Header>
        //        <v2gci_ct:SessionID>212D322D33212D32</v2gci_ct:SessionID>
        //        <v2gci_ct:TimeStamp>9506</v2gci_ct:TimeStamp>
        //        <xmlsig:Signature>
        //            <xmlsig:SignedInfo>
        //                <xmlsig:CanonicalizationMethod Algorithm="https://www.liquid-technologies.com" />
        //                <xmlsig:SignatureMethod Algorithm="https://www.liquid-technologies.com" />
        //                <xmlsig:Reference>
        //                    <xmlsig:DigestMethod Algorithm="https://www.liquid-technologies.com" />
        //                    <xmlsig:DigestValue>YTM0NZomIzI2OTsmIzM0NTueYQ==</xmlsig:DigestValue>
        //                </xmlsig:Reference>
        //            </xmlsig:SignedInfo>
        //            <xmlsig:SignatureValue>YTM0NZomIzI2OTsmIzM0NTueYQ==</xmlsig:SignatureValue>
        //        </xmlsig:Signature>
        //    </v2gci_ct:Header>
        //
        //    <ns:SelectedAuthorizationService>EIM</ns:SelectedAuthorizationService>
        //    <ns:EIM_AReqAuthorizationMode />
        //
        //</ns:AuthorizationReq>


        //<?xml version="1.0" encoding="utf-8"?>
        // <ns:AuthorizationReq xmlns:v2gci_ct="urn:iso:std:iso:15118:-20:CommonTypes"
        //                      xmlns:xmlsig="http://www.w3.org/2000/09/xmldsig#"
        //                      xmlns:ns="urn:iso:std:iso:15118:-20:CommonMessages"
        //                      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
        //                      xsi:schemaLocation="urn:iso:std:iso:15118:-20:CommonMessages file:///V2G_CI_CommonMessages.xsd">
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


        //<?xml version="1.0" encoding="utf-8"?>
        //<AuthorizationReq xmlns:ns="http://www.w3.org/2000/09/xmldsig#"
        //                  xmlns:nsA="urn:iso:std:iso:15118:-20:CommonTypes"
        //                  xmlns="urn:iso:std:iso:15118:-20:CommonMessages"
        //                  xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
        //                  xsi:schemaLocation="urn:iso:std:iso:15118:-20:CommonMessages file:///C:/Users/achim/ownCloud/Open%20Charging%20Cloud/Protocols/ISO15118/15118-20/V2G_CI_CommonMessages.xsd">
        //
        //    <nsA:Header>
        //        <nsA:SessionID>212D322D33212D32</nsA:SessionID>
        //        <nsA:TimeStamp>4523</nsA:TimeStamp>
        //        <ns:Signature>
        //            <ns:SignedInfo>
        //                <ns:CanonicalizationMethod Algorithm="https://www.liquid-technologies.com" />
        //                <ns:SignatureMethod Algorithm="https://www.liquid-technologies.com" />
        //                <ns:Reference>
        //                    <ns:DigestMethod Algorithm="https://www.liquid-technologies.com" />
        //                    <ns:DigestValue>YTM0NZomIzI2OTsmIzM0NTueYQ==</ns:DigestValue>
        //                </ns:Reference>
        //            </ns:SignedInfo>
        //            <ns:SignatureValue>YTM0NZomIzI2OTsmIzM0NTueYQ==</ns:SignatureValue>
        //        </ns:Signature>
        //    </nsA:Header>
        //
        //    <SelectedAuthorizationService>PnC</SelectedAuthorizationService>
        //
        //    <PnC_AReqAuthorizationMode p5:Id="AAAAA" xmlns:p5="urn:iso:std:iso:15118:-20:CommonMessages">
        //        <GenChallenge>YTM0NZomIzI2OTsmIzM0NTueYQ==</GenChallenge>
        //        <ContractCertificateChain>
        //            <Certificate>YTM0NZomIzI2OTsmIzM0NTueYQ==</Certificate>
        //            <SubCertificates>
        //                <Certificate>YTM0NZomIzI2OTsmIzM0NTueYQ==</Certificate>
        //            </SubCertificates>
        //        </ContractCertificateChain>
        //    </PnC_AReqAuthorizationMode>
        //
        //</AuthorizationReq>

        #endregion


    }

}
