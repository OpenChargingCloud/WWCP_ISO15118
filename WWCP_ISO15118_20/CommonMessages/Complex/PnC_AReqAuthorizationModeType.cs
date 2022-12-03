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
    /// The PnC_AReqAuthorizationModeType.
    /// </summary>
    public class PnC_AReqAuthorizationModeType : IEquatable<PnC_AReqAuthorizationModeType>
    {

        #region Properties

        /// <summary>
        /// The identification of this authorization.
        /// (From XML attribute!)
        /// </summary>
        [Mandatory]
        public XML_Id                    Id                          { get; }

        /// <summary>
        /// The gen challenge.
        /// </summary>
        [Mandatory]
        public GenChallenge              GenChallenge                { get; }

        /// <summary>
        /// The contract certificate chain.
        /// </summary>
        [Mandatory]
        public ContractCertificateChain  ContractCertificateChain    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new PnC_AReqAuthorizationModeType.
        /// </summary>
        /// <param name="Id">An identification of this authorization.</param>
        /// <param name="GenChallenge">A gen challenge.</param>
        /// <param name="ContractCertificateChain">A contract certificate chain.</param>
        public PnC_AReqAuthorizationModeType(XML_Id                    Id,
                                             GenChallenge              GenChallenge,
                                             ContractCertificateChain  ContractCertificateChain)
        {

            this.Id                        = Id;
            this.GenChallenge              = GenChallenge;
            this.ContractCertificateChain  = ContractCertificateChain;

        }

        #endregion


        #region (static) Parse   (JSON, CustomPnC_AReqAuthorizationModeTypeParser = null)

        /// <summary>
        /// Parse the given JSON representation of a pnc_AReqAuthorizationModeType.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomPnC_AReqAuthorizationModeTypeParser">A delegate to parse custom pnc_AReqAuthorizationModeTypes.</param>
        public static PnC_AReqAuthorizationModeType Parse(JObject                                                      JSON,
                                                          CustomJObjectParserDelegate<PnC_AReqAuthorizationModeType>?  CustomPnC_AReqAuthorizationModeTypeParser   = null)
        {

            if (TryParse(JSON,
                         out var pnc_AReqAuthorizationModeType,
                         out var errorResponse,
                         CustomPnC_AReqAuthorizationModeTypeParser))
            {
                return pnc_AReqAuthorizationModeType!;
            }

            throw new ArgumentException("The given JSON representation of a pnc_AReqAuthorizationModeType is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out PnC_AReqAuthorizationModeType, out ErrorResponse, CustomPnC_AReqAuthorizationModeTypeParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a pnc_AReqAuthorizationModeType.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PnC_AReqAuthorizationModeType">The parsed pnc_AReqAuthorizationModeType.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                             JSON,
                                       out PnC_AReqAuthorizationModeType?  PnC_AReqAuthorizationModeType,
                                       out String?                         ErrorResponse)

            => TryParse(JSON,
                        out PnC_AReqAuthorizationModeType,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a pnc_AReqAuthorizationModeType.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PnC_AReqAuthorizationModeType">The parsed pnc_AReqAuthorizationModeType.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomPnC_AReqAuthorizationModeTypeParser">A delegate to parse custom BootNotification requests.</param>
        public static Boolean TryParse(JObject                                                      JSON,
                                       out PnC_AReqAuthorizationModeType?                           PnC_AReqAuthorizationModeType,
                                       out String?                                                  ErrorResponse,
                                       CustomJObjectParserDelegate<PnC_AReqAuthorizationModeType>?  CustomPnC_AReqAuthorizationModeTypeParser)
        {

            try
            {

                PnC_AReqAuthorizationModeType = null;

                #region Id                          [mandatory]

                if (!JSON.ParseMandatory("Id",
                                         "identification",
                                         XML_Id.TryParse,
                                         out XML_Id Id,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region GenChallenge                [mandatory]

                if (!JSON.ParseMandatory("genChallenge",
                                         "gen challenge",
                                         CommonMessages.GenChallenge.TryParse,
                                         out GenChallenge GenChallenge,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region ContractCertificateChain    [mandatory]

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


                PnC_AReqAuthorizationModeType = new PnC_AReqAuthorizationModeType(Id,
                                                                                  GenChallenge,
                                                                                  ContractCertificateChain);

                if (CustomPnC_AReqAuthorizationModeTypeParser is not null)
                    PnC_AReqAuthorizationModeType = CustomPnC_AReqAuthorizationModeTypeParser(JSON,
                                                                                              PnC_AReqAuthorizationModeType);

                return true;

            }
            catch (Exception e)
            {
                PnC_AReqAuthorizationModeType  = null;
                ErrorResponse                  = "The given JSON representation of a pnc_AReqAuthorizationModeType is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomPnC_AReqAuthorizationModeTypeSerializer = null, CustomContractCertificateChainSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomPnC_AReqAuthorizationModeTypeSerializer">A delegate to serialize custom pnc_AReqAuthorizationModeTypes.</param>
        /// <param name="CustomContractCertificateChainSerializer">A delegate to serialize custom contract certificate chains.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PnC_AReqAuthorizationModeType>?  CustomPnC_AReqAuthorizationModeTypeSerializer   = null,
                              CustomJObjectSerializerDelegate<ContractCertificateChain>?       CustomContractCertificateChainSerializer        = null)
        {

            var json = JSONObject.Create(

                           new JProperty("id",                        Id.                      ToString()),
                           new JProperty("genChallenge",              GenChallenge.            ToString()),
                           new JProperty("contractCertificateChain",  ContractCertificateChain.ToJSON(CustomContractCertificateChainSerializer))

                       );

            return CustomPnC_AReqAuthorizationModeTypeSerializer is not null
                       ? CustomPnC_AReqAuthorizationModeTypeSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (PnC_AReqAuthorizationModeType1, PnC_AReqAuthorizationModeType2)

        /// <summary>
        /// Compares two pnc_AReqAuthorizationModeTypes for equality.
        /// </summary>
        /// <param name="PnC_AReqAuthorizationModeType1">A pnc_AReqAuthorizationModeType.</param>
        /// <param name="PnC_AReqAuthorizationModeType2">Another pnc_AReqAuthorizationModeType.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (PnC_AReqAuthorizationModeType? PnC_AReqAuthorizationModeType1,
                                           PnC_AReqAuthorizationModeType? PnC_AReqAuthorizationModeType2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(PnC_AReqAuthorizationModeType1, PnC_AReqAuthorizationModeType2))
                return true;

            // If one is null, but not both, return false.
            if (PnC_AReqAuthorizationModeType1 is null || PnC_AReqAuthorizationModeType2 is null)
                return false;

            return PnC_AReqAuthorizationModeType1.Equals(PnC_AReqAuthorizationModeType2);

        }

        #endregion

        #region Operator != (PnC_AReqAuthorizationModeType1, PnC_AReqAuthorizationModeType2)

        /// <summary>
        /// Compares two pnc_AReqAuthorizationModeTypes for inequality.
        /// </summary>
        /// <param name="PnC_AReqAuthorizationModeType1">A pnc_AReqAuthorizationModeType.</param>
        /// <param name="PnC_AReqAuthorizationModeType2">Another pnc_AReqAuthorizationModeType.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (PnC_AReqAuthorizationModeType? PnC_AReqAuthorizationModeType1,
                                           PnC_AReqAuthorizationModeType? PnC_AReqAuthorizationModeType2)

            => !(PnC_AReqAuthorizationModeType1 == PnC_AReqAuthorizationModeType2);

        #endregion

        #endregion

        #region IEquatable<PnC_AReqAuthorizationModeType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two pnc_AReqAuthorizationModeTypes for equality.
        /// </summary>
        /// <param name="Object">A pnc_AReqAuthorizationModeType to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is PnC_AReqAuthorizationModeType pnc_AReqAuthorizationModeType &&
                   Equals(pnc_AReqAuthorizationModeType);

        #endregion

        #region Equals(PnC_AReqAuthorizationModeType)

        /// <summary>
        /// Compares two pnc_AReqAuthorizationModeTypes for equality.
        /// </summary>
        /// <param name="PnC_AReqAuthorizationModeType">A pnc_AReqAuthorizationModeType to compare with.</param>
        public Boolean Equals(PnC_AReqAuthorizationModeType? PnC_AReqAuthorizationModeType)

            => PnC_AReqAuthorizationModeType is not null &&

               Id.                      Equals(PnC_AReqAuthorizationModeType.Id)           &&
               GenChallenge.            Equals(PnC_AReqAuthorizationModeType.GenChallenge) &&
               ContractCertificateChain.Equals(PnC_AReqAuthorizationModeType.ContractCertificateChain);

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

                return Id.                      GetHashCode() * 7 ^
                       GenChallenge.            GetHashCode() * 5 ^
                       ContractCertificateChain.GetHashCode() * 3 ^
                       base.                    GetHashCode();

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
                   ": ",
                   GenChallenge

               );

        #endregion

    }

}
