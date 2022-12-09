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

    public class PnC_AuthorizationSetupResponse : AuthorizationSetupResponse,
                                                  IEquatable<PnC_AuthorizationSetupResponse>
    {

        #region Properties

        /// <summary>
        /// The gen challenge.
        /// </summary>
        [Mandatory]
        public GenChallenge              GenChallenge    { get; }

        /// <summary>
        /// The enumeration of supported provider identifications.
        /// [max 128]
        /// </summary>
        [Optional]
        public IEnumerable<Provider_Id>  ProviderIds     { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new authorization response message.
        /// </summary>
        /// <param name="Request">The authorization setup request leading to this response.</param>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        /// <param name="CertificateInstallationService">Whether this charging station will provide a certificate installation service.</param>
        /// <param name="GenChallenge">A gen challenge.</param>
        /// <param name="ProviderIds">An enumeration of supported provider identifications.</param>
        public PnC_AuthorizationSetupResponse(AuthorizationSetupRequest  Request,
                                              MessageHeader              MessageHeader,
                                              ResponseCodes              ResponseCode,
                                              Boolean                    CertificateInstallationService,
                                              GenChallenge               GenChallenge,
                                              IEnumerable<Provider_Id>?  ProviderIds   = null)

            : base(Request,
                   MessageHeader,
                   ResponseCode,
                   new AuthorizationTypes[] { AuthorizationTypes.PnC },
                   CertificateInstallationService)

        {

            this.GenChallenge  = GenChallenge;
            this.ProviderIds   = ProviderIds?.Distinct() ?? Array.Empty<Provider_Id>();

        }

        #endregion


        #region Documentation

        // <xs:complexType name="PnC_ASResAuthorizationModeType">
        //     <xs:sequence>
        //         <xs:element name="GenChallenge"       type="genChallengeType"/>
        //         <xs:element name="SupportedProviders" type="SupportedProvidersListType" minOccurs="0"/>
        //     </xs:sequence>
        // </xs:complexType>


        // <xs:complexType name="SupportedProvidersListType">
        //     <xs:sequence>
        //         <xs:element name="ProviderID" type="v2gci_ct:nameType" maxOccurs="128"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (Request, JSON, CustomPnC_AuthorizationSetupResponseParser = null)

        /// <summary>
        /// Parse the given JSON representation of a plug and charge authorization setup response.
        /// </summary>
        /// <param name="Request">The authorization setup request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomPnC_AuthorizationSetupResponseParser">A delegate to parse custom plug and charge authorization setup responses.</param>
        public static PnC_AuthorizationSetupResponse Parse(AuthorizationSetupRequest                                     Request,
                                                           JObject                                                       JSON,
                                                           CustomJObjectParserDelegate<PnC_AuthorizationSetupResponse>?  CustomPnC_AuthorizationSetupResponseParser   = null)
        {

            if (TryParse(Request,
                         JSON,
                         out var pncAuthorizationSetupResponse,
                         out var errorResponse,
                         CustomPnC_AuthorizationSetupResponseParser))
            {
                return pncAuthorizationSetupResponse!;
            }

            throw new ArgumentException("The given JSON representation of a plug and charge authorization authorization setup response is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(Request, JSON, out AuthorizationSetupResponse, out ErrorResponse, CustomPnC_AuthorizationSetupResponseParser = null)

        /// <summary>
        /// Try to parse the given JSON representation of a plug and charge authorization setup response.
        /// </summary>
        /// <param name="Request">The authorization setup request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="AuthorizationSetupResponse">The parsed plug and charge authorization setup response.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomPnC_AuthorizationSetupResponseParser">A delegate to parse custom plug and charge authorization setup responses.</param>
        public static Boolean TryParse(AuthorizationSetupRequest                                     Request,
                                       JObject                                                       JSON,
                                       out PnC_AuthorizationSetupResponse?                           AuthorizationSetupResponse,
                                       out String?                                                   ErrorResponse,
                                       CustomJObjectParserDelegate<PnC_AuthorizationSetupResponse>?  CustomPnC_AuthorizationSetupResponseParser   = null)
        {

            ErrorResponse = null;

            try
            {

                AuthorizationSetupResponse = null;

                #region MessageHeader                     [mandatory]

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

                #region ResponseCode                      [mandatory]

                if (!JSON.ParseMandatory("responseCode",
                                         "response code",
                                         ResponseCodesExtensions.TryParse,
                                         out ResponseCodes ResponseCode,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region AuthorizationServices             [mandatory]

                if (!JSON.ParseMandatoryHashSet("authorizationServices",
                                                "authorization services",
                                                AuthorizationTypesExtensions.TryParse,
                                                out HashSet<AuthorizationTypes> AuthorizationServices,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region CertificateInstallationService    [mandatory]

                if (!JSON.ParseMandatory("certificateInstallationService",
                                         "certificate installation service",
                                         out Boolean CertificateInstallationService,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region GenChallenge                      [mandatory]

                if (!JSON.ParseMandatory("genChallenge",
                                         "gen challenge",
                                         CommonMessages.GenChallenge.TryParse,
                                         out GenChallenge GenChallenge,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region ProviderIds                       [optional]

                if (JSON.ParseOptionalHashSet("providerIds",
                                              "provider identifications",
                                              Provider_Id.TryParse,
                                              out HashSet<Provider_Id> ProviderIds,
                                              out ErrorResponse))
                {
                    return false;
                }

                #endregion


                if (!AuthorizationServices.Contains(AuthorizationTypes.PnC))
                {
                    ErrorResponse = "The enumeration of authorization services does not contain 'plug and charge authorization'!";
                    return false;
                }


                AuthorizationSetupResponse = new PnC_AuthorizationSetupResponse(Request,
                                                                                MessageHeader,
                                                                                ResponseCode,
                                                                                CertificateInstallationService,
                                                                                GenChallenge,
                                                                                ProviderIds);

                if (CustomPnC_AuthorizationSetupResponseParser is not null)
                    AuthorizationSetupResponse = CustomPnC_AuthorizationSetupResponseParser(JSON,
                                                                                            AuthorizationSetupResponse);

                return true;

            }
            catch (Exception e)
            {
                AuthorizationSetupResponse  = null;
                ErrorResponse               = "The given JSON representation of a plug and charge authorization setup response is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomPnC_AuthorizationSetupResponseSerializer = null, CustomMessageHeaderSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomPnC_AuthorizationSetupResponseSerializer">A delegate to serialize custom plug and charge authorization setup responses.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<PnC_AuthorizationSetupResponse>?  CustomPnC_AuthorizationSetupResponseSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?                   CustomMessageHeaderSerializer                    = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("messageHeader",                   MessageHeader.ToJSON(CustomMessageHeaderSerializer)),
                                 new JProperty("responseCode",                    ResponseCode. AsText()),
                                 new JProperty("certificateInstallationService",  CertificateInstallationService),
                                 new JProperty("genChallenge",                    GenChallenge. ToString()),

                           ProviderIds.Any()
                               ? new JProperty("providerIds",                     new JArray(ProviderIds.Select(providerId => providerId.ToString())))
                               : null
                       );


            return CustomPnC_AuthorizationSetupResponseSerializer is not null
                       ? CustomPnC_AuthorizationSetupResponseSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (AuthorizationSetupResponse1, AuthorizationSetupResponse2)

        /// <summary>
        /// Compares two plug and charge authorization setup responses for equality.
        /// </summary>
        /// <param name="AuthorizationSetupResponse1">An plug and charge authorization setup response.</param>
        /// <param name="AuthorizationSetupResponse2">Another plug and charge authorization setup response.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (PnC_AuthorizationSetupResponse? AuthorizationSetupResponse1,
                                           PnC_AuthorizationSetupResponse? AuthorizationSetupResponse2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(AuthorizationSetupResponse1, AuthorizationSetupResponse2))
                return true;

            // If one is null, but not both, return false.
            if (AuthorizationSetupResponse1 is null || AuthorizationSetupResponse2 is null)
                return false;

            return AuthorizationSetupResponse1.Equals(AuthorizationSetupResponse2);

        }

        #endregion

        #region Operator != (AuthorizationSetupResponse1, AuthorizationSetupResponse2)

        /// <summary>
        /// Compares two plug and charge authorization setup responses for inequality.
        /// </summary>
        /// <param name="AuthorizationSetupResponse1">An plug and charge authorization setup response.</param>
        /// <param name="AuthorizationSetupResponse2">Another plug and charge authorization setup response.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (PnC_AuthorizationSetupResponse? AuthorizationSetupResponse1,
                                           PnC_AuthorizationSetupResponse? AuthorizationSetupResponse2)

            => !(AuthorizationSetupResponse1 == AuthorizationSetupResponse2);

        #endregion

        #endregion

        #region IEquatable<AuthorizationSetupResponse> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two plug and charge authorization setup responses for equality.
        /// </summary>
        /// <param name="Object">An plug and charge authorization setup response to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is PnC_AuthorizationSetupResponse pncAuthorizationSetupResponse &&
                   Equals(pncAuthorizationSetupResponse);

        #endregion

        #region Equals(AuthorizationSetupResponse)

        /// <summary>
        /// Compares two plug and charge authorization setup responses for equality.
        /// </summary>
        /// <param name="AuthorizationSetupResponse">An plug and charge authorization setup response to compare with.</param>
        public Boolean Equals(PnC_AuthorizationSetupResponse? AuthorizationSetupResponse)

            => AuthorizationSetupResponse is not null &&

               CertificateInstallationService.Equals(AuthorizationSetupResponse.CertificateInstallationService) &&

               base.GenericEquals(AuthorizationSetupResponse);

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

                return CertificateInstallationService.GetHashCode() * 3 ^
                       base.                          GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   CertificateInstallationService
                       ? "yes"
                       : "no"

               );

        #endregion


    }

}
