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
    /// The certificate installation response.
    /// </summary>
    public class CertificateInstallationResponse : AResponse<CertificateInstallationRequest,
                                                             CertificateInstallationResponse>
    {

        #region Properties

        /// <summary>
        /// The EVSE processing type.
        /// </summary>
        [Mandatory]
        public ProcessingTypes           EVSEProcessing                        { get; }

        /// <summary>
        /// The CPS certificate chain.
        /// </summary>
        [Mandatory]
        public CertificateChain          CPSCertificateChain                   { get; }

        /// <summary>
        /// The signed installation data.
        /// </summary>
        [Mandatory]
        public SignedInstallationData    SignedInstallationData                { get; }

        /// <summary>
        /// The number of remaining contract certificate chains.
        /// </summary>
        [Mandatory]
        public Byte                      RemainingContractCertificateChains    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new certificate installation response.
        /// </summary>
        /// <param name="Request">The certificate installation request leading to this response.</param>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        /// 
        /// <param name="EVSEProcessing">An EVSE processing type.</param>
        /// <param name="CPSCertificateChain">A CPS certificate chain.</param>
        /// <param name="SignedInstallationData">Signed installation data.</param>
        /// <param name="RemainingContractCertificateChains">A number of remaining contract certificate chains.</param>
        public CertificateInstallationResponse(CertificateInstallationRequest  Request,
                                               MessageHeader                   MessageHeader,
                                               ResponseCodes                   ResponseCode,

                                               ProcessingTypes                 EVSEProcessing,
                                               CertificateChain                CPSCertificateChain,
                                               SignedInstallationData          SignedInstallationData,
                                               Byte                            RemainingContractCertificateChains)

            : base(Request,
                   MessageHeader,
                   ResponseCode)

        {

            this.EVSEProcessing                      = EVSEProcessing;
            this.CPSCertificateChain                 = CPSCertificateChain;
            this.SignedInstallationData              = SignedInstallationData;
            this.RemainingContractCertificateChains  = RemainingContractCertificateChains;

        }

        #endregion


        #region Documentation

        // <xs:element name="CertificateInstallationRes" type="CertificateInstallationResType"/>
        //
        // <xs:complexType name="CertificateInstallationResType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:V2GResponseType">
        //             <xs:sequence>
        //                 <xs:element name="EVSEProcessing"                     type="v2gci_ct:processingType"/>
        //                 <xs:element name="CPSCertificateChain"                type="CertificateChainType"/>
        //                 <xs:element name="SignedInstallationData"             type="SignedInstallationDataType"/>
        //                 <xs:element name="RemainingContractCertificateChains" type="xs:unsignedByte"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (Request, JSON, CustomCertificateInstallationResponseParser = null)

        /// <summary>
        /// Parse the given JSON representation of a certificate installation response.
        /// </summary>
        /// <param name="Request">The certificate installation request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomCertificateInstallationResponseParser">A delegate to parse custom certificate installation responses.</param>
        public static CertificateInstallationResponse Parse(CertificateInstallationRequest                                 Request,
                                                            JObject                                                        JSON,
                                                            CustomJObjectParserDelegate<CertificateInstallationResponse>?  CustomCertificateInstallationResponseParser   = null)
        {

            if (TryParse(Request,
                         JSON,
                         out var certificateInstallationResponse,
                         out var errorResponse,
                         CustomCertificateInstallationResponseParser))
            {
                return certificateInstallationResponse!;
            }

            throw new ArgumentException("The given JSON representation of a certificate installation response is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(Request, JSON, out CertificateInstallationResponse, out ErrorResponse, CustomCertificateInstallationResponseParser = null)

        /// <summary>
        /// Try to parse the given JSON representation of a certificate installation response.
        /// </summary>
        /// <param name="Request">The certificate installation request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CertificateInstallationResponse">The parsed certificate installation response.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomCertificateInstallationResponseParser">A delegate to parse custom certificate installation responses.</param>
        public static Boolean TryParse(CertificateInstallationRequest                                 Request,
                                       JObject                                                        JSON,
                                       out CertificateInstallationResponse?                           CertificateInstallationResponse,
                                       out String?                                                    ErrorResponse,
                                       CustomJObjectParserDelegate<CertificateInstallationResponse>?  CustomCertificateInstallationResponseParser   = null)
        {

            ErrorResponse = null;

            try
            {

                CertificateInstallationResponse = null;

                #region MessageHeader                         [mandatory]

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

                #region ResponseCode                          [mandatory]

                if (!JSON.ParseMandatory("responseCode",
                                         "response code",
                                         ResponseCodesExtensions.TryParse,
                                         out ResponseCodes ResponseCode,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion


                #region EVSEProcessing                        [mandatory]

                if (!JSON.ParseMandatory("evseProcessing",
                                         "EVSE processing",
                                         ProcessingTypesExtensions.TryParse,
                                         out ProcessingTypes EVSEProcessing,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region CPSCertificateChain                   [mandatory]

                if (!JSON.ParseMandatoryJSON("cpsCertificateChain",
                                             "CPS certificate chain",
                                             CertificateChain.TryParse,
                                             out CertificateChain? CPSCertificateChain,
                                             out ErrorResponse))
                {
                    return false;
                }

                if (CPSCertificateChain is null)
                    return false;

                #endregion

                #region SignedInstallationData                [mandatory]

                if (!JSON.ParseMandatoryJSON("signedInstallationData",
                                             "signed installation data",
                                             CommonMessages.SignedInstallationData.TryParse,
                                             out SignedInstallationData? SignedInstallationData,
                                             out ErrorResponse))
                {
                    return false;
                }

                if (SignedInstallationData is null)
                    return false;

                #endregion

                #region RemainingContractCertificateChains    [mandatory]

                if (!JSON.ParseMandatory("remainingContractCertificateChains",
                                         "number of remaining contract certificate chains",
                                         out Byte RemainingContractCertificateChains,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion


                CertificateInstallationResponse = new CertificateInstallationResponse(Request,
                                                                                      MessageHeader,
                                                                                      ResponseCode,

                                                                                      EVSEProcessing,
                                                                                      CPSCertificateChain,
                                                                                      SignedInstallationData,
                                                                                      RemainingContractCertificateChains);

                if (CustomCertificateInstallationResponseParser is not null)
                    CertificateInstallationResponse = CustomCertificateInstallationResponseParser(JSON,
                                                                                                  CertificateInstallationResponse);

                return true;

            }
            catch (Exception e)
            {
                CertificateInstallationResponse  = null;
                ErrorResponse                    = "The given JSON representation of a certificate installation response is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomCertificateInstallationResponseSerializer = null, CustomMessageHeaderSerializer = null, ...)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomCertificateInstallationResponseSerializer">A delegate to serialize custom certificate installation responses.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        /// <param name="CustomCertificateChainSerializer">A delegate to serialize custom certificate chains.</param>
        /// <param name="CustomSignedInstallationDataSerializer">A delegate to serialize custom signed installation data.</param>
        /// <param name="CustomContractCertificateChainSerializer">A delegate to serialize custom contract certificate chains.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<CertificateInstallationResponse>?  CustomCertificateInstallationResponseSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?                    CustomMessageHeaderSerializer                     = null,
                              CustomJObjectSerializerDelegate<CertificateChain>?                 CustomCertificateChainSerializer                  = null,
                              CustomJObjectSerializerDelegate<SignedInstallationData>?           CustomSignedInstallationDataSerializer            = null,
                              CustomJObjectSerializerDelegate<ContractCertificateChain>?         CustomContractCertificateChainSerializer          = null)
        {

            var json = JSONObject.Create(

                           new JProperty("messageHeader",                       MessageHeader.         ToJSON(CustomMessageHeaderSerializer)),
                           new JProperty("responseCode",                        ResponseCode.          AsText()),

                           new JProperty("evseProcessing",                      EVSEProcessing.        AsText()),

                           new JProperty("CPSCertificateChain",                 CPSCertificateChain.   ToJSON(CustomCertificateChainSerializer)),

                           new JProperty("SignedInstallationData",              SignedInstallationData.ToJSON(CustomSignedInstallationDataSerializer,
                                                                                                              CustomContractCertificateChainSerializer)),

                           new JProperty("remainingContractCertificateChains",  RemainingContractCertificateChains)

                       );

            return CustomCertificateInstallationResponseSerializer is not null
                       ? CustomCertificateInstallationResponseSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (CertificateInstallationResponse1, CertificateInstallationResponse2)

        /// <summary>
        /// Compares two certificate installation responses for equality.
        /// </summary>
        /// <param name="CertificateInstallationResponse1">A certificate installation response.</param>
        /// <param name="CertificateInstallationResponse2">Another certificate installation response.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (CertificateInstallationResponse? CertificateInstallationResponse1,
                                           CertificateInstallationResponse? CertificateInstallationResponse2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(CertificateInstallationResponse1, CertificateInstallationResponse2))
                return true;

            // If one is null, but not both, return false.
            if (CertificateInstallationResponse1 is null || CertificateInstallationResponse2 is null)
                return false;

            return CertificateInstallationResponse1.Equals(CertificateInstallationResponse2);

        }

        #endregion

        #region Operator != (CertificateInstallationResponse1, CertificateInstallationResponse2)

        /// <summary>
        /// Compares two certificate installation responses for inequality.
        /// </summary>
        /// <param name="CertificateInstallationResponse1">A certificate installation response.</param>
        /// <param name="CertificateInstallationResponse2">Another certificate installation response.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (CertificateInstallationResponse? CertificateInstallationResponse1,
                                           CertificateInstallationResponse? CertificateInstallationResponse2)

            => !(CertificateInstallationResponse1 == CertificateInstallationResponse2);

        #endregion

        #endregion

        #region IEquatable<CertificateInstallationResponse> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two certificate installation responses for equality.
        /// </summary>
        /// <param name="Object">A certificate installation response to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is CertificateInstallationResponse certificateInstallationResponse &&
                   Equals(certificateInstallationResponse);

        #endregion

        #region Equals(CertificateInstallationResponse)

        /// <summary>
        /// Compares two certificate installation responses for equality.
        /// </summary>
        /// <param name="CertificateInstallationResponse">A certificate installation response to compare with.</param>
        public override Boolean Equals(CertificateInstallationResponse? CertificateInstallationResponse)

            => CertificateInstallationResponse is not null &&

               EVSEProcessing.                    Equals(CertificateInstallationResponse.EVSEProcessing)                     &&
               CPSCertificateChain.               Equals(CertificateInstallationResponse.CPSCertificateChain)                &&
               SignedInstallationData.            Equals(CertificateInstallationResponse.SignedInstallationData)             &&
               RemainingContractCertificateChains.Equals(CertificateInstallationResponse.RemainingContractCertificateChains) &&

               base.GenericEquals(CertificateInstallationResponse);

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

                return EVSEProcessing.                    GetHashCode() * 11 ^
                       CPSCertificateChain.               GetHashCode() *  7 ^
                       SignedInstallationData.            GetHashCode() *  5 ^
                       RemainingContractCertificateChains.GetHashCode() *  3 ^

                       base.                              GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   EVSEProcessing,                      ": ",
                   CPSCertificateChain,                 ", ",
                   SignedInstallationData,              ", ",
                   RemainingContractCertificateChains,  " remaining contract certificate chain(s)"

               );

        #endregion

    }

}
