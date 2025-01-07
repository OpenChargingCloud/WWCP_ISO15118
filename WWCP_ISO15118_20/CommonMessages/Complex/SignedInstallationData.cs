/*
 * Copyright (c) 2021-2025 GraphDefined GmbH <achim.friedland@graphdefined.com>
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
    /// The signed installation data.
    /// </summary>
    public class SignedInstallationData
    {

        #region Properties

        /// <summary>
        /// The unique identification of the signed installation data.
        /// </summary>
        [Mandatory]
        public XML_Id                       Id                            { get; }

        /// <summary>
        /// The signed installation data.
        /// </summary>
        [Mandatory]
        public ContractCertificateChain     ContractCertificateChain      { get; }

        /// <summary>
        /// The ECDH Curve.
        /// </summary>
        [Mandatory]
        public ECDHCurves                   ECDHCurve                     { get; }

        /// <summary>
        /// The DH public key.
        /// </summary>
        [Mandatory]
        public DHPublicKey                  DHPublicKey                   { get; }

        /// <summary>
        /// The optional SECP521 encrypted private key.
        /// </summary>
        [OptionalChoice("EncryptedPrivateKey")]
        public SECP521EncryptedPrivateKey?  SECP521EncryptedPrivateKey    { get; }

        /// <summary>
        /// The optional X448 encrypted private key.
        /// </summary>
        [OptionalChoice("EncryptedPrivateKey")]
        public X448EncryptedPrivateKey?     X448EncryptedPrivateKey       { get; }

        /// <summary>
        /// The optional TPM encrypted private key.
        /// </summary>
        [OptionalChoice("EncryptedPrivateKey")]
        public TPMEncryptedPrivateKey?      TPMEncryptedPrivateKey        { get; }

        #endregion

        #region Constructor(s)

        #region (private) SignedInstallationData(Id, ContractCertificateChain, ECDHCurve, DHPublicKey, SECP521EncryptedPrivateKey, X448EncryptedPrivateKey, TPMEncryptedPrivateKey)

        /// <summary>
        /// Create new signed installation data.
        /// </summary>
        /// <param name="Id">An unique identification of the signed installation data.</param>
        /// <param name="ContractCertificateChain">Signed installation data.</param>
        /// <param name="ECDHCurve">An ECDH Curve.</param>
        /// <param name="DHPublicKey">A DH public key.</param>
        /// <param name="SECP521EncryptedPrivateKey">An optional SECP521 encrypted private key.</param>
        /// <param name="X448EncryptedPrivateKey">An optional X448 encrypted private key.</param>
        /// <param name="TPMEncryptedPrivateKey">An optional TPM encrypted private key.</param>
        private SignedInstallationData(XML_Id                       Id,
                                       ContractCertificateChain     ContractCertificateChain,
                                       ECDHCurves                   ECDHCurve,
                                       DHPublicKey                  DHPublicKey,
                                       SECP521EncryptedPrivateKey?  SECP521EncryptedPrivateKey,
                                       X448EncryptedPrivateKey?     X448EncryptedPrivateKey,
                                       TPMEncryptedPrivateKey?      TPMEncryptedPrivateKey)
        {

            this.Id                          = Id;
            this.ContractCertificateChain    = ContractCertificateChain;
            this.ECDHCurve                   = ECDHCurve;
            this.DHPublicKey                 = DHPublicKey;
            this.SECP521EncryptedPrivateKey  = SECP521EncryptedPrivateKey;
            this.X448EncryptedPrivateKey     = X448EncryptedPrivateKey;
            this.TPMEncryptedPrivateKey      = TPMEncryptedPrivateKey;

        }

        #endregion

        #region SignedInstallationData(Id, ContractCertificateChain, ECDHCurve, DHPublicKey, SECP521EncryptedPrivateKey)

        /// <summary>
        /// Create new signed installation data.
        /// </summary>
        /// <param name="Id">An unique identification of the signed installation data.</param>
        /// <param name="ContractCertificateChain">Signed installation data.</param>
        /// <param name="ECDHCurve">An ECDH Curve.</param>
        /// <param name="DHPublicKey">A DH public key.</param>
        /// <param name="SECP521EncryptedPrivateKey">A SECP521 encrypted private key.</param>
        public SignedInstallationData(XML_Id                      Id,
                                      ContractCertificateChain    ContractCertificateChain,
                                      ECDHCurves                  ECDHCurve,
                                      DHPublicKey                 DHPublicKey,
                                      SECP521EncryptedPrivateKey  SECP521EncryptedPrivateKey)

            : this(Id,
                   ContractCertificateChain,
                   ECDHCurve,
                   DHPublicKey,
                   SECP521EncryptedPrivateKey,
                   null,
                   null)

        { }

        #endregion

        #region SignedInstallationData(Id, ContractCertificateChain, ECDHCurve, DHPublicKey, X448EncryptedPrivateKey)

        /// <summary>
        /// Create new signed installation data.
        /// </summary>
        /// <param name="Id">An unique identification of the signed installation data.</param>
        /// <param name="ContractCertificateChain">Signed installation data.</param>
        /// <param name="ECDHCurve">An ECDH Curve.</param>
        /// <param name="DHPublicKey">A DH public key.</param>
        /// <param name="X448EncryptedPrivateKey">An X448 encrypted private key.</param>
        public SignedInstallationData(XML_Id                    Id,
                                      ContractCertificateChain  ContractCertificateChain,
                                      ECDHCurves                ECDHCurve,
                                      DHPublicKey               DHPublicKey,
                                      X448EncryptedPrivateKey   X448EncryptedPrivateKey)

            : this(Id,
                   ContractCertificateChain,
                   ECDHCurve,
                   DHPublicKey,
                   null,
                   X448EncryptedPrivateKey,
                   null)

        { }

        #endregion

        #region SignedInstallationData(Id, ContractCertificateChain, ECDHCurve, DHPublicKey, TPMEncryptedPrivateKey)

        /// <summary>
        /// Create new signed installation data.
        /// </summary>
        /// <param name="Id">An unique identification of the signed installation data.</param>
        /// <param name="ContractCertificateChain">Signed installation data.</param>
        /// <param name="ECDHCurve">An ECDH Curve.</param>
        /// <param name="DHPublicKey">A DH public key.</param>
        /// <param name="TPMEncryptedPrivateKey">A TPM encrypted private key.</param>
        public SignedInstallationData(XML_Id                    Id,
                                      ContractCertificateChain  ContractCertificateChain,
                                      ECDHCurves                ECDHCurve,
                                      DHPublicKey               DHPublicKey,
                                      TPMEncryptedPrivateKey    TPMEncryptedPrivateKey)

            : this(Id,
                   ContractCertificateChain,
                   ECDHCurve,
                   DHPublicKey,
                   null,
                   null,
                   TPMEncryptedPrivateKey)

        { }

        #endregion

        #endregion


        #region Documentation

        // <xs:complexType name="SignedInstallationDataType">
        //     <xs:sequence>
        //         <xs:element name="ContractCertificateChain" type="ContractCertificateChainType"/>
        //         <xs:element name="ECDHCurve"                type="ecdhCurveType"/>
        //         <xs:element name="DHPublicKey"              type="dhPublicKeyType"/>
        //         <xs:choice>
        //             <xs:element name="SECP521_EncryptedPrivateKey" type="secp521_EncryptedPrivateKeyType"/>
        //             <xs:element name="X448_EncryptedPrivateKey"    type="x448_EncryptedPrivateKeyType"/>
        //             <xs:element name="TPM_EncryptedPrivateKey"     type="tpm_EncryptedPrivateKeyType"/>
        //         </xs:choice>
        //     </xs:sequence>
        //     <xs:attribute name="Id" type="xs:ID" use="required"/>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomSignedInstallationDataParser = null)

        /// <summary>
        /// Parse the given JSON representation of signed installation data.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomSignedInstallationDataParser">An optional delegate to parse custom signed installation data.</param>
        public static SignedInstallationData Parse(JObject                                               JSON,
                                                   CustomJObjectParserDelegate<SignedInstallationData>?  CustomSignedInstallationDataParser   = null)
        {

            if (TryParse(JSON,
                         out var signedInstallationData,
                         out var errorResponse,
                         CustomSignedInstallationDataParser))
            {
                return signedInstallationData!;
            }

            throw new ArgumentException("The given JSON representation of signed installation data is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out SignedInstallationData, out ErrorResponse, CustomSignedInstallationDataParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of signed installation data.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="SignedInstallationData">The parsed signed installation data.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                      JSON,
                                       out SignedInstallationData?  SignedInstallationData,
                                       out String?                  ErrorResponse)

            => TryParse(JSON,
                        out SignedInstallationData,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of signed installation data.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="SignedInstallationData">The parsed signed installation data.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomSignedInstallationDataParser">An optional delegate to parse custom contract certificates.</param>
        public static Boolean TryParse(JObject                                               JSON,
                                       out SignedInstallationData?                           SignedInstallationData,
                                       out String?                                           ErrorResponse,
                                       CustomJObjectParserDelegate<SignedInstallationData>?  CustomSignedInstallationDataParser)
        {

            try
            {

                SignedInstallationData = null;

                #region Id                            [mandatory]

                if (!JSON.ParseMandatory("id",
                                         "signed installation data identification",
                                         XML_Id.TryParse,
                                         out XML_Id Id,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region ContractCertificateChain      [mandatory]

                if (!JSON.ParseMandatoryJSON("contractCertificateChain",
                                             "contract certificate chain",
                                             CommonMessages.ContractCertificateChain.TryParse,
                                             out ContractCertificateChain? ContractCertificateChain,
                                             out ErrorResponse))
                {
                    return false;
                }

                if (ContractCertificateChain is null)
                    return false;

                #endregion

                #region ECDHCurve                     [mandatory]

                if (!JSON.ParseMandatory("ecdhCurve",
                                         "ECDH curve",
                                         ECDHCurvesExtensions.TryParse,
                                         out ECDHCurves ECDHCurve,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region DHPublicKey                   [mandatory]

                if (!JSON.ParseMandatory("dhPublicKey",
                                         "DH public key",
                                         CommonMessages.DHPublicKey.TryParse,
                                         out DHPublicKey DHPublicKey,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region SECP521EncryptedPrivateKey    [optional]

                if (JSON.ParseOptional("secp521EncryptedPrivateKey",
                                       "SECP521 encrypted private key",
                                       CommonMessages.SECP521EncryptedPrivateKey.TryParse,
                                       out SECP521EncryptedPrivateKey SECP521EncryptedPrivateKey,
                                       out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region X448EncryptedPrivateKey       [optional]

                if (JSON.ParseOptional("x448EncryptedPrivateKey",
                                       "X448 encrypted private key",
                                       CommonMessages.X448EncryptedPrivateKey.TryParse,
                                       out X448EncryptedPrivateKey X448EncryptedPrivateKey,
                                       out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region TPMEncryptedPrivateKey        [optional]

                if (JSON.ParseOptional("tpmEncryptedPrivateKey",
                                       "TPM encrypted private key",
                                       CommonMessages.TPMEncryptedPrivateKey.TryParse,
                                       out TPMEncryptedPrivateKey TPMEncryptedPrivateKey,
                                       out ErrorResponse))
                {
                    return false;
                }

                #endregion


                SignedInstallationData = new SignedInstallationData(Id,
                                                                    ContractCertificateChain,
                                                                    ECDHCurve,
                                                                    DHPublicKey,
                                                                    SECP521EncryptedPrivateKey,
                                                                    X448EncryptedPrivateKey,
                                                                    TPMEncryptedPrivateKey);

                if (CustomSignedInstallationDataParser is not null)
                    SignedInstallationData = CustomSignedInstallationDataParser(JSON,
                                                                                SignedInstallationData);

                return true;

            }
            catch (Exception e)
            {
                SignedInstallationData  = null;
                ErrorResponse           = "The given JSON representation of signed installation data is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomSignedInstallationDataSerializer = null, CustomContractCertificateChainSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomSignedInstallationDataSerializer">A delegate to serialize custom signed installation data.</param>
        /// <param name="CustomContractCertificateChainSerializer">A delegate to serialize custom contract certificate chains.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<SignedInstallationData>?    CustomSignedInstallationDataSerializer     = null,
                              CustomJObjectSerializerDelegate<ContractCertificateChain>?  CustomContractCertificateChainSerializer   = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("id",                          Id.                              ToString()),
                                 new JProperty("contractCertificateChain",    ContractCertificateChain.        ToJSON(CustomContractCertificateChainSerializer)),
                                 new JProperty("ecdhCurve",                   ECDHCurve.                       AsText()),
                                 new JProperty("dhPublicKey",                 DHPublicKey.                     ToString()),

                           SECP521EncryptedPrivateKey.HasValue
                               ? new JProperty("secp521EncryptedPrivateKey",  SECP521EncryptedPrivateKey.Value.ToString())
                               : null,

                           X448EncryptedPrivateKey.HasValue
                               ? new JProperty("x448EncryptedPrivateKey",     X448EncryptedPrivateKey.   Value.ToString())
                               : null,

                           TPMEncryptedPrivateKey.HasValue
                               ? new JProperty("tpmEncryptedPrivateKey",      TPMEncryptedPrivateKey.    Value.ToString())
                               : null

                       );

            return CustomSignedInstallationDataSerializer is not null
                       ? CustomSignedInstallationDataSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (SignedInstallationData1, SignedInstallationData2)

        /// <summary>
        /// Compares two signed installation data for equality.
        /// </summary>
        /// <param name="SignedInstallationData1">Signed installation data.</param>
        /// <param name="SignedInstallationData2">Other signed installation data.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (SignedInstallationData? SignedInstallationData1,
                                           SignedInstallationData? SignedInstallationData2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(SignedInstallationData1, SignedInstallationData2))
                return true;

            // If one is null, but not both, return false.
            if (SignedInstallationData1 is null || SignedInstallationData2 is null)
                return false;

            return SignedInstallationData1.Equals(SignedInstallationData2);

        }

        #endregion

        #region Operator != (SignedInstallationData1, SignedInstallationData2)

        /// <summary>
        /// Compares two signed installation data for inequality.
        /// </summary>
        /// <param name="SignedInstallationData1">Signed installation data.</param>
        /// <param name="SignedInstallationData2">Other signed installation data.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (SignedInstallationData? SignedInstallationData1,
                                           SignedInstallationData? SignedInstallationData2)

            => !(SignedInstallationData1 == SignedInstallationData2);

        #endregion

        #endregion

        #region IEquatable<SignedInstallationData> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two signed installation data for equality.
        /// </summary>
        /// <param name="Object">Signed installation data to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is SignedInstallationData signedInstallationData &&
                   Equals(signedInstallationData);

        #endregion

        #region Equals(SignedInstallationData)

        /// <summary>
        /// Compares two signed installation data for equality.
        /// </summary>
        /// <param name="SignedInstallationData">Signed installation data to compare with.</param>
        public Boolean Equals(SignedInstallationData? SignedInstallationData)

            => SignedInstallationData is not null &&

               Id.                      Equals(SignedInstallationData.Id)                       &&
               ContractCertificateChain.Equals(SignedInstallationData.ContractCertificateChain) &&
               ECDHCurve.               Equals(SignedInstallationData.ECDHCurve)                &&
               DHPublicKey.             Equals(SignedInstallationData.DHPublicKey)              &&

            ((!SECP521EncryptedPrivateKey.HasValue && !SignedInstallationData.SECP521EncryptedPrivateKey.HasValue) ||
              (SECP521EncryptedPrivateKey.HasValue &&  SignedInstallationData.SECP521EncryptedPrivateKey.HasValue && SECP521EncryptedPrivateKey.Value.Equals(SignedInstallationData.SECP521EncryptedPrivateKey.Value))) &&

            ((!X448EncryptedPrivateKey.   HasValue && !SignedInstallationData.X448EncryptedPrivateKey.   HasValue) ||
              (X448EncryptedPrivateKey.   HasValue &&  SignedInstallationData.X448EncryptedPrivateKey.   HasValue && X448EncryptedPrivateKey.   Value.Equals(SignedInstallationData.X448EncryptedPrivateKey.   Value))) &&

            ((!TPMEncryptedPrivateKey.    HasValue && !SignedInstallationData.TPMEncryptedPrivateKey.    HasValue) ||
              (TPMEncryptedPrivateKey.    HasValue &&  SignedInstallationData.TPMEncryptedPrivateKey.    HasValue && TPMEncryptedPrivateKey.    Value.Equals(SignedInstallationData.TPMEncryptedPrivateKey.    Value)));

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

                return Id.                         GetHashCode()       * 19 ^
                       ContractCertificateChain.   GetHashCode()       * 17 ^
                       ECDHCurve.                  GetHashCode()       * 13 ^
                       DHPublicKey.                GetHashCode()       * 11 ^

                      (SECP521EncryptedPrivateKey?.GetHashCode() ?? 0) *  7 ^
                      (X448EncryptedPrivateKey?.   GetHashCode() ?? 0) *  5 ^
                      (TPMEncryptedPrivateKey?.    GetHashCode() ?? 0) *  3 ^

                       base.                       GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   Id,                       ", ",
                   ContractCertificateChain, ", ",
                   ECDHCurve.AsText(),       ", ",
                   DHPublicKey,              ", ",

                   SECP521EncryptedPrivateKey.HasValue
                       ? ", " + SECP521EncryptedPrivateKey
                       : "",

                   X448EncryptedPrivateKey.HasValue
                       ? ", " + X448EncryptedPrivateKey
                       : "",

                   TPMEncryptedPrivateKey.HasValue
                       ? ", " + TPMEncryptedPrivateKey
                       : ""

               );

        #endregion

    }

}
