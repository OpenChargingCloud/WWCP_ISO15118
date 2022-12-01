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
    /// The authorization request.
    /// </summary>
    public class AuthorizationRequest : AV2GRequest<AuthorizationRequest>
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
        /// Create a new authorization request.
        /// </summary>
        /// <param name="Header">A message header.</param>
        /// <param name="SelectedAuthorizationService">A selected authorization service.</param>
        /// <param name="EIM_AReqAuthorizationMode"></param>
        /// <param name="PnC_AReqAuthorizationMode"></param>
        private AuthorizationRequest(MessageHeader                   Header,
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
        /// Create a new authorization request.
        /// </summary>
        /// <param name="Header">A message header.</param>
        /// <param name="EIM_AReqAuthorizationMode"></param>
        public AuthorizationRequest(MessageHeader                  Header,
                                    EIM_AReqAuthorizationModeType  EIM_AReqAuthorizationMode)

            : this(Header,
                   AuthorizationTypes.EIM,
                   EIM_AReqAuthorizationMode,
                   null)

        { }

        #endregion

        #region AuthorizationRequest(..., PnC_AReqAuthorizationMode)

        /// <summary>
        /// Create a new authorization request.
        /// </summary>
        /// <param name="Header">A message header.</param>
        /// <param name="PnC_AReqAuthorizationMode"></param>
        public AuthorizationRequest(MessageHeader                  Header,
                                    PnC_AReqAuthorizationModeType  PnC_AReqAuthorizationMode)

            : this(Header,
                   AuthorizationTypes.PnC,
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

        #region (static) Parse   (JSON, CustomAuthorizationRequestParser = null)

        /// <summary>
        /// Parse the given JSON representation of an authorization request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomAuthorizationRequestParser">A delegate to parse custom authorization requests.</param>
        public static AuthorizationRequest Parse(JObject                                             JSON,
                                                 CustomJObjectParserDelegate<AuthorizationRequest>?  CustomAuthorizationRequestParser   = null)
        {

            if (TryParse(JSON,
                         out var authorizationRequest,
                         out var errorResponse,
                         CustomAuthorizationRequestParser))
            {
                return authorizationRequest!;
            }

            throw new ArgumentException("The given JSON representation of an authorization request is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out AuthorizationRequest, out ErrorResponse, CustomAuthorizationRequestParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of an authorization request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="AuthorizationRequest">The parsed authorization request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                    JSON,
                                       out AuthorizationRequest?  AuthorizationRequest,
                                       out String?                ErrorResponse)

            => TryParse(JSON,
                        out AuthorizationRequest,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an authorization request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="AuthorizationRequest">The parsed authorization request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomAuthorizationRequestParser">A delegate to parse custom BootNotification requests.</param>
        public static Boolean TryParse(JObject                                             JSON,
                                       out AuthorizationRequest?                           AuthorizationRequest,
                                       out String?                                         ErrorResponse,
                                       CustomJObjectParserDelegate<AuthorizationRequest>?  CustomAuthorizationRequestParser)
        {

            try
            {

                AuthorizationRequest = null;

                #region MessageHeader                   [mandatory]

                if (!JSON.ParseMandatoryJSON("messageHeader",
                                             "message header",
                                             CommonTypes.MessageHeader.TryParse,
                                             out MessageHeader? MessageHeader,
                                             out ErrorResponse))
                {
                    return false;
                }

                if (MessageHeader is null)
                    return false;

                #endregion

                #region SelectedAuthorizationService    [mandatory]

                if (!JSON.ParseMandatory("selectedAuthorizationService",
                                         "selected authorization service",
                                         AuthorizationTypesExtensions.TryParse,
                                         out AuthorizationTypes SelectedAuthorizationService,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region EIM_AReqAuthorizationMode        [optional]

                if (JSON.ParseOptionalJSON("EIM_AReqAuthorizationMode",
                                           "EIM AReq authorization mode",
                                           EIM_AReqAuthorizationModeType.TryParse,
                                           out EIM_AReqAuthorizationModeType? EIM_AReqAuthorizationMode,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region PnC_AReqAuthorizationMode        [optional]

                if (JSON.ParseOptionalJSON("PnC_AReqAuthorizationMode",
                                           "PnC AReq authorization mode",
                                           PnC_AReqAuthorizationModeType.TryParse,
                                           out PnC_AReqAuthorizationModeType? PnC_AReqAuthorizationMode,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                AuthorizationRequest = new AuthorizationRequest(MessageHeader,
                                                                SelectedAuthorizationService,
                                                                EIM_AReqAuthorizationMode,
                                                                PnC_AReqAuthorizationMode);

                if (CustomAuthorizationRequestParser is not null)
                    AuthorizationRequest = CustomAuthorizationRequestParser(JSON,
                                                                            AuthorizationRequest);

                return true;

            }
            catch (Exception e)
            {
                AuthorizationRequest  = null;
                ErrorResponse         = "The given JSON representation of an authorization request is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomAuthorizationRequestSerializer = null, CustomMessageHeaderSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomAuthorizationRequestSerializer">A delegate to serialize custom authorization requests.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<AuthorizationRequest>?  CustomAuthorizationRequestSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?         CustomMessageHeaderSerializer          = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("messageHeader",                 MessageHeader.ToJSON(CustomMessageHeaderSerializer)),
                                 new JProperty("selectedAuthorizationService",  SelectedAuthorizationService.AsText()),

                           EIM_AReqAuthorizationMode is not null
                               ? new JProperty("EIM_AReqAuthorizationMode",     EIM_AReqAuthorizationMode.ToJSON())
                               : null,

                           PnC_AReqAuthorizationMode is not null
                               ? new JProperty("PnC_AReqAuthorizationMode",     PnC_AReqAuthorizationMode.ToJSON())
                               : null

                       );

            return CustomAuthorizationRequestSerializer is not null
                       ? CustomAuthorizationRequestSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (AuthorizationRequest1, AuthorizationRequest2)

        /// <summary>
        /// Compares two authorization requests for equality.
        /// </summary>
        /// <param name="AuthorizationRequest1">An authorization request.</param>
        /// <param name="AuthorizationRequest2">Another authorization request.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (AuthorizationRequest? AuthorizationRequest1,
                                           AuthorizationRequest? AuthorizationRequest2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(AuthorizationRequest1, AuthorizationRequest2))
                return true;

            // If one is null, but not both, return false.
            if (AuthorizationRequest1 is null || AuthorizationRequest2 is null)
                return false;

            return AuthorizationRequest1.Equals(AuthorizationRequest2);

        }

        #endregion

        #region Operator != (AuthorizationRequest1, AuthorizationRequest2)

        /// <summary>
        /// Compares two authorization requests for inequality.
        /// </summary>
        /// <param name="AuthorizationRequest1">An authorization request.</param>
        /// <param name="AuthorizationRequest2">Another authorization request.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (AuthorizationRequest? AuthorizationRequest1,
                                           AuthorizationRequest? AuthorizationRequest2)

            => !(AuthorizationRequest1 == AuthorizationRequest2);

        #endregion

        #endregion

        #region IEquatable<AuthorizationRequest> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two authorization requests for equality.
        /// </summary>
        /// <param name="Object">An authorization request to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is AuthorizationRequest authorizationRequest &&
                   Equals(authorizationRequest);

        #endregion

        #region Equals(AuthorizationRequest)

        /// <summary>
        /// Compares two authorization requests for equality.
        /// </summary>
        /// <param name="AuthorizationRequest">An authorization request to compare with.</param>
        public override Boolean Equals(AuthorizationRequest? AuthorizationRequest)

            => AuthorizationRequest is not null &&

               SelectedAuthorizationService.Equals(AuthorizationRequest.SelectedAuthorizationService) &&

             ((EIM_AReqAuthorizationMode is     null && AuthorizationRequest.EIM_AReqAuthorizationMode is     null) ||
              (EIM_AReqAuthorizationMode is not null && AuthorizationRequest.EIM_AReqAuthorizationMode is not null && EIM_AReqAuthorizationMode.Equals(AuthorizationRequest.EIM_AReqAuthorizationMode))) &&

             ((PnC_AReqAuthorizationMode is     null && AuthorizationRequest.PnC_AReqAuthorizationMode is     null) ||
              (PnC_AReqAuthorizationMode is not null && AuthorizationRequest.PnC_AReqAuthorizationMode is not null && PnC_AReqAuthorizationMode.Equals(AuthorizationRequest.PnC_AReqAuthorizationMode))) &&

               base.GenericEquals(AuthorizationRequest);

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the HashCode of this object.
        /// </summary>
        /// <returns>The HashCode of this object.</returns>
        public override Int32 GetHashCode()
        {
            unchecked
            {

                return SelectedAuthorizationService.GetHashCode()       * 7 ^
                      (EIM_AReqAuthorizationMode?.  GetHashCode() ?? 0) * 5 ^
                      (PnC_AReqAuthorizationMode?.  GetHashCode() ?? 0) * 3 ^

                       base.                        GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   SelectedAuthorizationService.AsText(),
                   ": ",

                   EIM_AReqAuthorizationMode is not null
                       ? EIM_AReqAuthorizationMode.ToString()
                       : "",

                   PnC_AReqAuthorizationMode is not null
                       ? PnC_AReqAuthorizationMode.ToString()
                       : ""

               );

        #endregion

    }

}
