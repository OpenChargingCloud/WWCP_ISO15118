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
using cloud.charging.open.protocols.ISO15118_20.CommonTypes;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// The plug and charge authorization request.
    /// </summary>
    public class PnC_AuthorizationRequest : AuthorizationRequest,
                                            IEquatable<PnC_AuthorizationRequest>
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
        /// Create a new plug and charge authorization request.
        /// </summary>
        /// <param name="Header">A message header.</param>
        /// <param name="Id">An identification of this authorization.</param>
        /// <param name="GenChallenge">A gen challenge.</param>
        /// <param name="ContractCertificateChain">A contract certificate chain.</param>
        public PnC_AuthorizationRequest(MessageHeader             Header,
                                        XML_Id                    Id,
                                        GenChallenge              GenChallenge,
                                        ContractCertificateChain  ContractCertificateChain)

            : base(Header)

        {

            this.Id                        = Id;
            this.GenChallenge              = GenChallenge;
            this.ContractCertificateChain  = ContractCertificateChain;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="PnC_AReqAuthorizationModeType">
        //     <xs:sequence>
        //         <xs:element name="GenChallenge"             type="genChallengeType"/>
        //         <xs:element name="ContractCertificateChain" type="ContractCertificateChainType"/>
        //     </xs:sequence>
        //     <xs:attribute name="Id" type="xs:ID" use="required"/>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomPnC_AuthorizationRequestParser = null)

        /// <summary>
        /// Parse the given JSON representation of a plug and charge authorization request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomPnC_AuthorizationRequestParser">A delegate to parse custom plug and charge authorization requests.</param>
        public static PnC_AuthorizationRequest Parse(JObject                                                 JSON,
                                                     CustomJObjectParserDelegate<PnC_AuthorizationRequest>?  CustomPnC_AuthorizationRequestParser   = null)
        {

            if (TryParse(JSON,
                         out var pnc_AReqAuthorizationModeType,
                         out var errorResponse,
                         CustomPnC_AuthorizationRequestParser))
            {
                return pnc_AReqAuthorizationModeType!;
            }

            throw new ArgumentException("The given JSON representation of a PnC_AuthorizationRequest is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out PnC_AuthorizationRequest, out ErrorResponse, CustomPnC_AuthorizationRequestParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a plug and charge authorization request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PnC_AuthorizationRequest">The parsed plug and charge authorization request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                        JSON,
                                       out PnC_AuthorizationRequest?  PnC_AuthorizationRequest,
                                       out String?                    ErrorResponse)

            => TryParse(JSON,
                        out PnC_AuthorizationRequest,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a plug and charge authorization request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PnC_AuthorizationRequest">The parsed plug and charge authorization request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomPnC_AuthorizationRequestParser">A delegate to parse custom plug and charge authorization requests.</param>
        public static Boolean TryParse(JObject                                                 JSON,
                                       out PnC_AuthorizationRequest?                           PnC_AuthorizationRequest,
                                       out String?                                             ErrorResponse,
                                       CustomJObjectParserDelegate<PnC_AuthorizationRequest>?  CustomPnC_AuthorizationRequestParser)
        {

            try
            {

                PnC_AuthorizationRequest = null;

                #region MessageHeader               [mandatory]

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


                PnC_AuthorizationRequest = new PnC_AuthorizationRequest(MessageHeader,
                                                                        Id,
                                                                        GenChallenge,
                                                                        ContractCertificateChain);

                if (CustomPnC_AuthorizationRequestParser is not null)
                    PnC_AuthorizationRequest = CustomPnC_AuthorizationRequestParser(JSON,
                                                                                    PnC_AuthorizationRequest);

                return true;

            }
            catch (Exception e)
            {
                PnC_AuthorizationRequest  = null;
                ErrorResponse             = "The given JSON representation of a PnC_AuthorizationRequest is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomPnC_AuthorizationRequestSerializer = null, CustomMessageHeaderSerializer = null, ...)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomPnC_AuthorizationRequestSerializer">A delegate to serialize custom plug and charge authorization requests.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        /// <param name="CustomContractCertificateChainSerializer">A delegate to serialize custom contract certificate chains.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PnC_AuthorizationRequest>?  CustomPnC_AuthorizationRequestSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?             CustomMessageHeaderSerializer              = null,
                              CustomJObjectSerializerDelegate<ContractCertificateChain>?  CustomContractCertificateChainSerializer   = null)
        {

            var json = JSONObject.Create(

                           new JProperty("messageHeader",             MessageHeader.           ToJSON(CustomMessageHeaderSerializer)),

                           new JProperty("id",                        Id.                      ToString()),
                           new JProperty("genChallenge",              GenChallenge.            ToString()),
                           new JProperty("contractCertificateChain",  ContractCertificateChain.ToJSON(CustomContractCertificateChainSerializer))

                       );

            return CustomPnC_AuthorizationRequestSerializer is not null
                       ? CustomPnC_AuthorizationRequestSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (PnC_AuthorizationRequest1, PnC_AuthorizationRequest2)

        /// <summary>
        /// Compares two plug and charge authorization requests for equality.
        /// </summary>
        /// <param name="PnC_AuthorizationRequest1">A plug and charge authorization request.</param>
        /// <param name="PnC_AuthorizationRequest2">Another plug and charge authorization request.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (PnC_AuthorizationRequest? PnC_AuthorizationRequest1,
                                           PnC_AuthorizationRequest? PnC_AuthorizationRequest2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(PnC_AuthorizationRequest1, PnC_AuthorizationRequest2))
                return true;

            // If one is null, but not both, return false.
            if (PnC_AuthorizationRequest1 is null || PnC_AuthorizationRequest2 is null)
                return false;

            return PnC_AuthorizationRequest1.Equals(PnC_AuthorizationRequest2);

        }

        #endregion

        #region Operator != (PnC_AuthorizationRequest1, PnC_AuthorizationRequest2)

        /// <summary>
        /// Compares two plug and charge authorization requests for inequality.
        /// </summary>
        /// <param name="PnC_AuthorizationRequest1">A plug and charge authorization request.</param>
        /// <param name="PnC_AuthorizationRequest2">Another plug and charge authorization request.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (PnC_AuthorizationRequest? PnC_AuthorizationRequest1,
                                           PnC_AuthorizationRequest? PnC_AuthorizationRequest2)

            => !(PnC_AuthorizationRequest1 == PnC_AuthorizationRequest2);

        #endregion

        #endregion

        #region IEquatable<PnC_AuthorizationRequest> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two plug and charge authorization requests for equality.
        /// </summary>
        /// <param name="Object">A plug and charge authorization request to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is PnC_AuthorizationRequest pnc_AuthorizationRequest &&
                   Equals(pnc_AuthorizationRequest);

        #endregion

        #region Equals(PnC_AuthorizationRequest)

        /// <summary>
        /// Compares two plug and charge authorization requests for equality.
        /// </summary>
        /// <param name="PnC_AuthorizationRequest">A plug and charge authorization request to compare with.</param>
        public Boolean Equals(PnC_AuthorizationRequest? PnC_AuthorizationRequest)

            => PnC_AuthorizationRequest is not null &&

               Id.                      Equals(PnC_AuthorizationRequest.Id)           &&
               GenChallenge.            Equals(PnC_AuthorizationRequest.GenChallenge) &&
               ContractCertificateChain.Equals(PnC_AuthorizationRequest.ContractCertificateChain);

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
