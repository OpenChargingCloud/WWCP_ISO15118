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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// A certificate chain.
    /// </summary>
    public class CertificateChain : IEquatable<CertificateChain>
    {

        #region Properties

        /// <summary>
        /// The certificate.
        /// </summary>
        [Mandatory]
        public Certificate               Certificate        { get; }

        /// <summary>
        /// The optional enumeration of sub certificates.
        /// [max 3]
        /// </summary>
        [Optional]
        public IEnumerable<Certificate>  SubCertificates    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new certificate chain.
        /// </summary>
        /// <param name="Certificate">A certificate.</param>
        /// <param name="SubCertificates">An optional enumeration of sub certificates.</param>
        public CertificateChain(Certificate                Certificate,
                                IEnumerable<Certificate>?  SubCertificates   = null)
        {

            this.Certificate      = Certificate;
            this.SubCertificates  = SubCertificates?.Distinct() ?? Array.Empty<Certificate>();

        }

        #endregion


        #region Documentation

        // <xs:complexType name="CertificateChainType">
        //     <xs:sequence>
        //         <xs:element name="Certificate"     type="certificateType"/>
        //         <xs:element name="SubCertificates" type="SubCertificatesType" minOccurs="0"/>
        //     </xs:sequence>
        // </xs:complexType>


        // <xs:complexType name="SubCertificatesType">
        //     <xs:sequence>
        //         <xs:element name="Certificate" type="certificateType" maxOccurs="3"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomCertificateChainParser = null)

        /// <summary>
        /// Parse the given JSON representation of a certificate chain.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomCertificateChainParser">A delegate to parse custom certificate chains.</param>
        public static CertificateChain Parse(JObject                                         JSON,
                                             CustomJObjectParserDelegate<CertificateChain>?  CustomCertificateChainParser   = null)
        {

            if (TryParse(JSON,
                         out var certificateChain,
                         out var errorResponse,
                         CustomCertificateChainParser))
            {
                return certificateChain!;
            }

            throw new ArgumentException("The given JSON representation of a certificate chain is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out CertificateChain, out ErrorResponse, CustomCertificateChainParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a certificate chain.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CertificateChain">The parsed certificate chain.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                JSON,
                                       out CertificateChain?  CertificateChain,
                                       out String?            ErrorResponse)

            => TryParse(JSON,
                        out CertificateChain,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a certificate chain.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CertificateChain">The parsed certificate chain.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomCertificateChainParser">A delegate to parse custom certificate chains.</param>
        public static Boolean TryParse(JObject                                         JSON,
                                       out CertificateChain?                           CertificateChain,
                                       out String?                                     ErrorResponse,
                                       CustomJObjectParserDelegate<CertificateChain>?  CustomCertificateChainParser)
        {

            try
            {

                CertificateChain = null;

                #region Certificate        [mandatory]

                if (!JSON.ParseMandatory("certificate",
                                         "certificate",
                                         CommonMessages.Certificate.TryParse,
                                         out Certificate Certificate,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region SubCertificates    [optional]

                if (JSON.ParseOptionalHashSet("subCertificates",
                                               "sub certificates",
                                               CommonMessages.Certificate.TryParse,
                                               out HashSet<Certificate> SubCertificates,
                                               out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                CertificateChain = new CertificateChain(Certificate,
                                                        SubCertificates);

                if (CustomCertificateChainParser is not null)
                    CertificateChain = CustomCertificateChainParser(JSON,
                                                                    CertificateChain);

                return true;

            }
            catch (Exception e)
            {
                CertificateChain  = null;
                ErrorResponse     = "The given JSON representation of a certificate chain is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomCertificateChainSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomCertificateChainSerializer">A delegate to serialize custom certificate chains.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<CertificateChain>? CustomCertificateChainSerializer = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("certificate",      Certificate.ToString()),

                           SubCertificates.Any()
                               ? new JProperty("subCertificates",  new JArray(SubCertificates.Select(subCertificate => subCertificate.ToString())))
                               : null

                       );

            return CustomCertificateChainSerializer is not null
                       ? CustomCertificateChainSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (CertificateChain1, CertificateChain2)

        /// <summary>
        /// Compares two certificate chains for equality.
        /// </summary>
        /// <param name="CertificateChain1">A certificate chain.</param>
        /// <param name="CertificateChain2">Another certificate chain.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (CertificateChain? CertificateChain1,
                                           CertificateChain? CertificateChain2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(CertificateChain1, CertificateChain2))
                return true;

            // If one is null, but not both, return false.
            if (CertificateChain1 is null || CertificateChain2 is null)
                return false;

            return CertificateChain1.Equals(CertificateChain2);

        }

        #endregion

        #region Operator != (CertificateChain1, CertificateChain2)

        /// <summary>
        /// Compares two certificate chains for inequality.
        /// </summary>
        /// <param name="CertificateChain1">A certificate chain.</param>
        /// <param name="CertificateChain2">Another certificate chain.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (CertificateChain? CertificateChain1,
                                           CertificateChain? CertificateChain2)

            => !(CertificateChain1 == CertificateChain2);

        #endregion

        #endregion

        #region IEquatable<CertificateChain> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two certificate chains for equality.
        /// </summary>
        /// <param name="Object">A certificate chain to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is CertificateChain certificateChain &&
                   Equals(certificateChain);

        #endregion

        #region Equals(CertificateChain)

        /// <summary>
        /// Compares two certificate chains for equality.
        /// </summary>
        /// <param name="CertificateChain">A certificate chain to compare with.</param>
        public Boolean Equals(CertificateChain? CertificateChain)

            => CertificateChain is not null &&

               Certificate.Equals(CertificateChain.Certificate) &&

               SubCertificates.Count().Equals(CertificateChain.SubCertificates.Count()) &&
               SubCertificates.All(subCertificate => CertificateChain.SubCertificates.Contains(subCertificate));

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

                return Certificate.    GetHashCode()  * 5 ^
                       SubCertificates.CalcHashCode() * 3 ^

                       base.           GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   Certificate.ToString().SubstringMax(30),
                   ", ",

                   SubCertificates.Count(),
                   " sub certificate(s)"

               );

        #endregion

    }

}
