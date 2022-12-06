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

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// The contract certificate chain.
    /// </summary>
    public class ContractCertificateChain : IEquatable<ContractCertificateChain>
    {

        #region Properties

        /// <summary>
        /// The certificate.
        /// </summary>
        [Mandatory]
        public Certificate               Certificate        { get; }

        /// <summary>
        /// The enumeration of sub certificates.
        /// [max 3]
        /// </summary>
        [Mandatory]
        public IEnumerable<Certificate>  SubCertificates    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new contract certificate chain.
        /// </summary>
        /// <param name="Certificate">A certificate.</param>
        /// <param name="SubCertificates">An enumeration of sub certificates [max 3].</param>
        public ContractCertificateChain(Certificate               Certificate,
                                        IEnumerable<Certificate>  SubCertificates)
        {

            //ToDo: Minimal 0 or 1 sub certificates?

            this.Certificate      = Certificate;
            this.SubCertificates  = SubCertificates.Distinct();

        }

        #endregion


        #region Documentation

        // <xs:complexType name="ContractCertificateChainType">
        //     <xs:sequence>
        //         <xs:element name="Certificate"     type="certificateType"/>
        //         <xs:element name="SubCertificates" type="SubCertificatesType"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomContractCertificateChainParser = null)

        /// <summary>
        /// Parse the given JSON representation of a contract certificate chain.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomContractCertificateChainParser">A delegate to parse custom contract certificate chains.</param>
        public static ContractCertificateChain Parse(JObject                                                 JSON,
                                                     CustomJObjectParserDelegate<ContractCertificateChain>?  CustomContractCertificateChainParser   = null)
        {

            if (TryParse(JSON,
                         out var contractCertificateChain,
                         out var errorResponse,
                         CustomContractCertificateChainParser))
            {
                return contractCertificateChain!;
            }

            throw new ArgumentException("The given JSON representation of a contract certificate chain is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out ContractCertificateChain, out ErrorResponse, CustomContractCertificateChainParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a contract certificate chain.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ContractCertificateChain">The parsed contract certificate chain.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                        JSON,
                                       out ContractCertificateChain?  ContractCertificateChain,
                                       out String?                    ErrorResponse)

            => TryParse(JSON,
                        out ContractCertificateChain,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a contract certificate chain.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ContractCertificateChain">The parsed contract certificate chain.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomContractCertificateChainParser">A delegate to parse custom contract certificates.</param>
        public static Boolean TryParse(JObject                                                 JSON,
                                       out ContractCertificateChain?                           ContractCertificateChain,
                                       out String?                                             ErrorResponse,
                                       CustomJObjectParserDelegate<ContractCertificateChain>?  CustomContractCertificateChainParser)
        {

            try
            {

                ContractCertificateChain = null;

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

                #region SubCertificates    [mandatory]

                if (!JSON.ParseMandatoryHashSet("subCertificates",
                                                "sub certificates",
                                                CommonMessages.Certificate.TryParse,
                                                out HashSet<Certificate>? SubCertificates,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion


                ContractCertificateChain = new ContractCertificateChain(Certificate,
                                                                        SubCertificates);

                if (CustomContractCertificateChainParser is not null)
                    ContractCertificateChain = CustomContractCertificateChainParser(JSON,
                                                                                    ContractCertificateChain);

                return true;

            }
            catch (Exception e)
            {
                ContractCertificateChain  = null;
                ErrorResponse             = "The given JSON representation of a contract certificate chain is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomContractCertificateChainSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomContractCertificateChainSerializer">A delegate to serialize custom contract certificate chains.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<ContractCertificateChain>? CustomContractCertificateChainSerializer = null)
        {

            var json = JSONObject.Create(

                           new JProperty("certificate",      Certificate.ToString()),
                           new JProperty("subCertificates",  new JArray(SubCertificates.Select(subCertificate => subCertificate.ToString())))

                       );

            return CustomContractCertificateChainSerializer is not null
                       ? CustomContractCertificateChainSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (ContractCertificateChain1, ContractCertificateChain2)

        /// <summary>
        /// Compares two contract certificate chains for equality.
        /// </summary>
        /// <param name="ContractCertificateChain1">A contract certificate chain.</param>
        /// <param name="ContractCertificateChain2">Another contract certificate chain.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ContractCertificateChain? ContractCertificateChain1,
                                           ContractCertificateChain? ContractCertificateChain2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(ContractCertificateChain1, ContractCertificateChain2))
                return true;

            // If one is null, but not both, return false.
            if (ContractCertificateChain1 is null || ContractCertificateChain2 is null)
                return false;

            return ContractCertificateChain1.Equals(ContractCertificateChain2);

        }

        #endregion

        #region Operator != (ContractCertificateChain1, ContractCertificateChain2)

        /// <summary>
        /// Compares two contract certificate chains for inequality.
        /// </summary>
        /// <param name="ContractCertificateChain1">A contract certificate chain.</param>
        /// <param name="ContractCertificateChain2">Another contract certificate chain.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ContractCertificateChain? ContractCertificateChain1,
                                           ContractCertificateChain? ContractCertificateChain2)

            => !(ContractCertificateChain1 == ContractCertificateChain2);

        #endregion

        #endregion

        #region IEquatable<ContractCertificateChain> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two contract certificate chains for equality.
        /// </summary>
        /// <param name="Object">A contract certificate chain to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ContractCertificateChain contractCertificateChain &&
                   Equals(contractCertificateChain);

        #endregion

        #region Equals(ContractCertificateChain)

        /// <summary>
        /// Compares two contract certificate chains for equality.
        /// </summary>
        /// <param name="ContractCertificateChain">A contract certificate chain to compare with.</param>
        public Boolean Equals(ContractCertificateChain? ContractCertificateChain)

            => ContractCertificateChain is not null &&

               Certificate.Equals(ContractCertificateChain.Certificate) &&

               SubCertificates.Count().Equals(ContractCertificateChain.SubCertificates.Count()) &&
               SubCertificates.All(subCertificate => ContractCertificateChain.SubCertificates.Contains(subCertificate));

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
