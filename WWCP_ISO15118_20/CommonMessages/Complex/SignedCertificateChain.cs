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

using cloud.charging.open.protocols.ISO15118_20.XMLSchema;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// A signed certificate chain.
    /// </summary>
    public class SignedCertificateChain : IEquatable<SignedCertificateChain>
    {

        #region Properties

        /// <summary>
        /// The identification of this signed certificate chain.
        /// (From XML attribute!)
        /// </summary>
        [Mandatory]
        public XML_Id                    Id                 { get; }

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
        /// Create a new signed certificate chain.
        /// </summary>
        /// <param name="Id">An identification of this signed certificate chain.</param>
        /// <param name="Certificate">A certificate.</param>
        /// <param name="SubCertificates">An optional enumeration of sub certificates.</param>
        public SignedCertificateChain(XML_Id                     Id,
                                      Certificate                Certificate,
                                      IEnumerable<Certificate>?  SubCertificates   = null)
        {

            this.Id               = Id;
            this.Certificate      = Certificate;
            this.SubCertificates  = SubCertificates?.Distinct() ?? Array.Empty<Certificate>();

        }

        #endregion


        #region Documentation

        // <xs:complexType name="SignedCertificateChainType">
        //     <xs:sequence>
        //         <xs:element name="Certificate"     type="certificateType"/>
        //         <xs:element name="SubCertificates" type="SubCertificatesType" minOccurs="0"/>
        //     </xs:sequence>
        //     <xs:attribute name="Id" type="xs:ID" use="required"/>
        // </xs:complexType>


        // <xs:complexType name="SubCertificatesType">
        //     <xs:sequence>
        //         <xs:element name="Certificate" type="certificateType" maxOccurs="3"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomSignedCertificateChainParser = null)

        /// <summary>
        /// Parse the given JSON representation of a signed certificate chain.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomSignedCertificateChainParser">A delegate to parse custom signed certificate chains.</param>
        public static SignedCertificateChain Parse(JObject                                               JSON,
                                                   CustomJObjectParserDelegate<SignedCertificateChain>?  CustomSignedCertificateChainParser   = null)
        {

            if (TryParse(JSON,
                         out var signedCertificateChain,
                         out var errorResponse,
                         CustomSignedCertificateChainParser))
            {
                return signedCertificateChain!;
            }

            throw new ArgumentException("The given JSON representation of a signed certificate chain is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out SignedCertificateChain, out ErrorResponse, CustomSignedCertificateChainParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a signed certificate chain.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="SignedCertificateChain">The parsed signed certificate chain.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                      JSON,
                                       out SignedCertificateChain?  SignedCertificateChain,
                                       out String?                  ErrorResponse)

            => TryParse(JSON,
                        out SignedCertificateChain,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a signed certificate chain.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="SignedCertificateChain">The parsed signed certificate chain.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomSignedCertificateChainParser">A delegate to parse custom signed certificate chains.</param>
        public static Boolean TryParse(JObject                                               JSON,
                                       out SignedCertificateChain?                           SignedCertificateChain,
                                       out String?                                           ErrorResponse,
                                       CustomJObjectParserDelegate<SignedCertificateChain>?  CustomSignedCertificateChainParser)
        {

            try
            {

                SignedCertificateChain = null;

                #region Id                 [mandatory]

                if (!JSON.ParseMandatory("id",
                                         "identification",
                                         XML_Id.TryParse,
                                         out XML_Id Id,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

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


                SignedCertificateChain = new SignedCertificateChain(Id,
                                                                    Certificate,
                                                                    SubCertificates);

                if (CustomSignedCertificateChainParser is not null)
                    SignedCertificateChain = CustomSignedCertificateChainParser(JSON,
                                                                                SignedCertificateChain);

                return true;

            }
            catch (Exception e)
            {
                SignedCertificateChain  = null;
                ErrorResponse           = "The given JSON representation of a signed certificate chain is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomSignedCertificateChainSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomSignedCertificateChainSerializer">A delegate to serialize custom signed certificate chains.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<SignedCertificateChain>? CustomSignedCertificateChainSerializer = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("id",               Id.         ToString()),
                                 new JProperty("certificate",      Certificate.ToString()),

                           SubCertificates.Any()
                               ? new JProperty("subCertificates",  new JArray(SubCertificates.Select(subCertificate => subCertificate.ToString())))
                               : null

                       );

            return CustomSignedCertificateChainSerializer is not null
                       ? CustomSignedCertificateChainSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (SignedCertificateChain1, SignedCertificateChain2)

        /// <summary>
        /// Compares two signed certificate chains for equality.
        /// </summary>
        /// <param name="SignedCertificateChain1">A signed certificate chain.</param>
        /// <param name="SignedCertificateChain2">Another signed certificate chain.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (SignedCertificateChain? SignedCertificateChain1,
                                           SignedCertificateChain? SignedCertificateChain2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(SignedCertificateChain1, SignedCertificateChain2))
                return true;

            // If one is null, but not both, return false.
            if (SignedCertificateChain1 is null || SignedCertificateChain2 is null)
                return false;

            return SignedCertificateChain1.Equals(SignedCertificateChain2);

        }

        #endregion

        #region Operator != (SignedCertificateChain1, SignedCertificateChain2)

        /// <summary>
        /// Compares two signed certificate chains for inequality.
        /// </summary>
        /// <param name="SignedCertificateChain1">A signed certificate chain.</param>
        /// <param name="SignedCertificateChain2">Another signed certificate chain.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (SignedCertificateChain? SignedCertificateChain1,
                                           SignedCertificateChain? SignedCertificateChain2)

            => !(SignedCertificateChain1 == SignedCertificateChain2);

        #endregion

        #endregion

        #region IEquatable<SignedCertificateChain> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two signed certificate chains for equality.
        /// </summary>
        /// <param name="Object">A signed certificate chain to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is SignedCertificateChain signedCertificateChain &&
                   Equals(signedCertificateChain);

        #endregion

        #region Equals(SignedCertificateChain)

        /// <summary>
        /// Compares two signed certificate chains for equality.
        /// </summary>
        /// <param name="SignedCertificateChain">A signed certificate chain to compare with.</param>
        public Boolean Equals(SignedCertificateChain? SignedCertificateChain)

            => SignedCertificateChain is not null &&

               Id.         Equals(SignedCertificateChain.Id)          &&
               Certificate.Equals(SignedCertificateChain.Certificate) &&

               SubCertificates.Count().Equals(SignedCertificateChain.SubCertificates.Count()) &&
               SubCertificates.All(subCertificate => SignedCertificateChain.SubCertificates.Contains(subCertificate));

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

                return Id.             GetHashCode()  * 7 ^
                       Certificate.    GetHashCode()  * 5 ^
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

                   Id,
                   " / ",
                   Certificate.ToString().SubstringMax(30),
                   ", ",

                   SubCertificates.Count(),
                   " sub certificate(s)"

               );

        #endregion

    }

}
