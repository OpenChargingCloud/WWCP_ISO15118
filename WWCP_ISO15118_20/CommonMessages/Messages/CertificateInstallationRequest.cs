/*
 * Copyright (c) 2021-2024 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

using cloud.charging.open.protocols.ISO15118_20.XMLDSig;
using cloud.charging.open.protocols.ISO15118_20.CommonTypes;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// The certificate installation request.
    /// </summary>
    public class CertificateInstallationRequest : ARequest<CertificateInstallationRequest>
    {

        #region Properties

        /// <summary>
        /// The OEM provisioning certificate chain.
        /// </summary>
        [Mandatory]
        public SignedCertificateChain         OEMProvisioningCertificateChain     { get; }

        /// <summary>
        /// The enumeration of root certificate identifications.
        /// </summary>
        [Mandatory]
        public IEnumerable<X509IssuerSerial>  RootCertificateIds                  { get; }

        /// <summary>
        /// The maximum number of contract certificate chains.
        /// </summary>
        [Mandatory]
        public Byte                           MaximumContractCertificateChains    { get; }

        /// <summary>
        /// The optional enumeration of prioritized e-mobility account identifications.
        /// </summary>
        [Optional]
        public IEnumerable<EMA_Id>            PrioritizedEMAIds                   { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new certificate installation request.
        /// </summary>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="OEMProvisioningCertificateChain">An OEM provisioning certificate chain.</param>
        /// <param name="RootCertificateIds">An enumeration of root certificate identifications.</param>
        /// <param name="MaximumContractCertificateChains">A maximum number of contract certificate chains.</param>
        /// <param name="PrioritizedEMAIds">An optional enumeration of prioritized e-mobility account identifications.</param>
        public CertificateInstallationRequest(MessageHeader                  MessageHeader,
                                              SignedCertificateChain         OEMProvisioningCertificateChain,
                                              IEnumerable<X509IssuerSerial>  RootCertificateIds,
                                              Byte                           MaximumContractCertificateChains,
                                              IEnumerable<EMA_Id>?           PrioritizedEMAIds)

            : base(MessageHeader)

        {

            this.OEMProvisioningCertificateChain   = OEMProvisioningCertificateChain;
            this.RootCertificateIds                = RootCertificateIds;
            this.MaximumContractCertificateChains  = MaximumContractCertificateChains;
            this.PrioritizedEMAIds                 = PrioritizedEMAIds?.Distinct() ?? Array.Empty<EMA_Id>();

        }

        #endregion


        #region Documentation

        // <xs:element name="CertificateInstallationReq" type="CertificateInstallationReqType"/>
        //
        // <xs:complexType name="CertificateInstallationReqType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:V2GRequestType">
        //             <xs:sequence>
        //                 <xs:element name="OEMProvisioningCertificateChain"  type="SignedCertificateChainType"/>
        //                 <xs:element name="ListOfRootCertificateIDs"         type="v2gci_ct:ListOfRootCertificateIDsType"/>
        //                 <xs:element name="MaximumContractCertificateChains" type="xs:unsignedByte"/>
        //                 <xs:element name="PrioritizedEMAIDs"                type="EMAIDListType" minOccurs="0"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>


        // <xs:complexType name="ListOfRootCertificateIDsType">
        //     <xs:sequence>
        //         <xs:element name="RootCertificateID" type="xmlsig:X509IssuerSerialType" maxOccurs="20"/>
        //     </xs:sequence>
        // </xs:complexType>


        // <xs:complexType name="EMAIDListType">
        //     <xs:sequence>
        //         <xs:element name="EMAID" type="v2gci_ct:identifierType" maxOccurs="8"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomCertificateInstallationRequestParser = null)

        /// <summary>
        /// Parse the given JSON representation of a certificate installation request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomCertificateInstallationRequestParser">A delegate to parse custom certificate installation requests.</param>
        public static CertificateInstallationRequest Parse(JObject                                                       JSON,
                                                           CustomJObjectParserDelegate<CertificateInstallationRequest>?  CustomCertificateInstallationRequestParser   = null)
        {

            if (TryParse(JSON,
                         out var certificateInstallationRequest,
                         out var errorResponse,
                         CustomCertificateInstallationRequestParser))
            {
                return certificateInstallationRequest!;
            }

            throw new ArgumentException("The given JSON representation of a certificate installation request is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out CertificateInstallationRequest, out ErrorResponse, CustomCertificateInstallationRequestParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a certificate installation request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CertificateInstallationRequest">The parsed certificate installation request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                              JSON,
                                       out CertificateInstallationRequest?  CertificateInstallationRequest,
                                       out String?                          ErrorResponse)

            => TryParse(JSON,
                        out CertificateInstallationRequest,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a certificate installation request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CertificateInstallationRequest">The parsed certificate installation request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomCertificateInstallationRequestParser">A delegate to parse custom certificate installation requests.</param>
        public static Boolean TryParse(JObject                                                       JSON,
                                       out CertificateInstallationRequest?                           CertificateInstallationRequest,
                                       out String?                                                   ErrorResponse,
                                       CustomJObjectParserDelegate<CertificateInstallationRequest>?  CustomCertificateInstallationRequestParser)
        {

            try
            {

                CertificateInstallationRequest = null;

                #region MessageHeader                       [mandatory]

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

                #region OEMProvisioningCertificateChain     [mandatory]

                if (!JSON.ParseMandatoryJSON("oemProvisioningCertificateChain",
                                             "OEM provisioning certificate chain",
                                             SignedCertificateChain.TryParse,
                                             out SignedCertificateChain? OEMProvisioningCertificateChain,
                                             out ErrorResponse))
                {
                    return false;
                }

                if (OEMProvisioningCertificateChain is null)
                    return false;

                #endregion

                #region RootCertificateIds                  [mandatory]

                if (!JSON.ParseMandatoryHashSet("rootCertificateIds",
                                                "root certificate identifications",
                                                X509IssuerSerial.TryParse,
                                                out HashSet<X509IssuerSerial> RootCertificateIds,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region MaximumContractCertificateChains    [mandatory]

                if (!JSON.ParseMandatory("maximumContractCertificateChains",
                                         "maximum contract certificate chains",
                                         out Byte MaximumContractCertificateChains,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region PrioritizedEMAIds                   [optional]

                if (JSON.ParseOptionalHashSet("prioritizedEMAIds",
                                              "prioritized e-mobility account identifications",
                                              EMA_Id.TryParse,
                                              out HashSet<EMA_Id> PrioritizedEMAIds,
                                              out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                CertificateInstallationRequest = new CertificateInstallationRequest(MessageHeader,
                                                                                    OEMProvisioningCertificateChain,
                                                                                    RootCertificateIds,
                                                                                    MaximumContractCertificateChains,
                                                                                    PrioritizedEMAIds);

                if (CustomCertificateInstallationRequestParser is not null)
                    CertificateInstallationRequest = CustomCertificateInstallationRequestParser(JSON,
                                                                                                CertificateInstallationRequest);

                return true;

            }
            catch (Exception e)
            {
                CertificateInstallationRequest  = null;
                ErrorResponse                   = "The given JSON representation of a certificate installation request is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomCertificateInstallationRequestSerializer = null, CustomMessageHeaderSerializer = null, ...)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomCertificateInstallationRequestSerializer">A delegate to serialize custom certificate installation requests.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        /// <param name="CustomSignedCertificateChainSerializer">A delegate to serialize custom signed certificate chains.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<CertificateInstallationRequest>?  CustomCertificateInstallationRequestSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?                   CustomMessageHeaderSerializer                    = null,
                              CustomJObjectSerializerDelegate<SignedCertificateChain>?          CustomSignedCertificateChainSerializer           = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("messageHeader",                     MessageHeader.                  ToJSON(CustomMessageHeaderSerializer)),

                                 new JProperty("oemProvisioningCertificateChain",   OEMProvisioningCertificateChain.ToJSON(CustomSignedCertificateChainSerializer)),

                                 new JProperty("rootCertificateIds",                new JArray(RootCertificateIds.Select(rootCertificateId => rootCertificateId.ToString()))),

                                 new JProperty("maximumContractCertificateChains",  MaximumContractCertificateChains),

                           PrioritizedEMAIds.Any()
                               ? new JProperty("prioritizedEMAIds",                 new JArray(PrioritizedEMAIds. Select(eMAId             => eMAId.            ToString())))
                               : null

                       );

            return CustomCertificateInstallationRequestSerializer is not null
                       ? CustomCertificateInstallationRequestSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (CertificateInstallationRequest1, CertificateInstallationRequest2)

        /// <summary>
        /// Compares two certificate installation requests for equality.
        /// </summary>
        /// <param name="CertificateInstallationRequest1">A certificate installation request.</param>
        /// <param name="CertificateInstallationRequest2">Another certificate installation request.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (CertificateInstallationRequest? CertificateInstallationRequest1,
                                           CertificateInstallationRequest? CertificateInstallationRequest2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(CertificateInstallationRequest1, CertificateInstallationRequest2))
                return true;

            // If one is null, but not both, return false.
            if (CertificateInstallationRequest1 is null || CertificateInstallationRequest2 is null)
                return false;

            return CertificateInstallationRequest1.Equals(CertificateInstallationRequest2);

        }

        #endregion

        #region Operator != (CertificateInstallationRequest1, CertificateInstallationRequest2)

        /// <summary>
        /// Compares two certificate installation requests for inequality.
        /// </summary>
        /// <param name="CertificateInstallationRequest1">A certificate installation request.</param>
        /// <param name="CertificateInstallationRequest2">Another certificate installation request.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (CertificateInstallationRequest? CertificateInstallationRequest1,
                                           CertificateInstallationRequest? CertificateInstallationRequest2)

            => !(CertificateInstallationRequest1 == CertificateInstallationRequest2);

        #endregion

        #endregion

        #region IEquatable<CertificateInstallationRequest> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two certificate installation requests for equality.
        /// </summary>
        /// <param name="Object">A certificate installation request to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is CertificateInstallationRequest certificateInstallationRequest &&
                   Equals(certificateInstallationRequest);

        #endregion

        #region Equals(CertificateInstallationRequest)

        /// <summary>
        /// Compares two certificate installation requests for equality.
        /// </summary>
        /// <param name="CertificateInstallationRequest">A certificate installation request to compare with.</param>
        public override Boolean Equals(CertificateInstallationRequest? CertificateInstallationRequest)

            => CertificateInstallationRequest is not null &&

               OEMProvisioningCertificateChain. Equals(CertificateInstallationRequest.OEMProvisioningCertificateChain)  &&
               MaximumContractCertificateChains.Equals(CertificateInstallationRequest.MaximumContractCertificateChains) &&

               RootCertificateIds.Count().Equals(CertificateInstallationRequest.RootCertificateIds.Count())                             &&
               RootCertificateIds.All(x509issuerSerial => CertificateInstallationRequest.RootCertificateIds.Contains(x509issuerSerial)) &&

               PrioritizedEMAIds. Count().Equals(CertificateInstallationRequest.PrioritizedEMAIds.Count())                              &&
               PrioritizedEMAIds. All(eMAId            => CertificateInstallationRequest.PrioritizedEMAIds. Contains(eMAId))            &&

               base.                            Equals(CertificateInstallationRequest);

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

                return OEMProvisioningCertificateChain. GetHashCode()  * 11 ^
                       RootCertificateIds.              CalcHashCode() *  7 ^
                       MaximumContractCertificateChains.GetHashCode()  *  5 ^
                       PrioritizedEMAIds.               CalcHashCode() *  3 ^

                       base.                            GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   OEMProvisioningCertificateChain,           ", ",
                   RootCertificateIds.Count(),                " root certificate identification(s), ",
                   "max ", MaximumContractCertificateChains,  " contract certificate chain(s), ",
                   PrioritizedEMAIds.Count(),                 " prioritized eMAIds identification(s)"

               );

        #endregion

    }

}
