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

using cloud.charging.open.protocols.ISO15118_20.CommonTypes;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// The service selection response.
    /// </summary>
    public class ServiceSelectionResponse : AResponse<ServiceSelectionRequest,
                                                      ServiceSelectionResponse>
    {

        #region Constructor(s)

        /// <summary>
        /// Create a new service selection response.
        /// </summary>
        /// <param name="Request">The service selection request leading to this response.</param>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        public ServiceSelectionResponse(ServiceSelectionRequest  Request,
                                        MessageHeader            MessageHeader,
                                        ResponseCodes            ResponseCode)

            : base(Request,
                   MessageHeader,
                   ResponseCode)

        { }

        #endregion


        #region Documentation

        // <xs:element name="ServiceSelectionRes" type="ServiceSelectionResType"/>
        // <xs:complexType name="ServiceSelectionResType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:V2GResponseType"/>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (Request, JSON, CustomServiceSelectionResponseParser = null)

        /// <summary>
        /// Parse the given JSON representation of a service selection response.
        /// </summary>
        /// <param name="Request">The service selection request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomServiceSelectionResponseParser">A delegate to parse custom service selection responses.</param>
        public static ServiceSelectionResponse Parse(ServiceSelectionRequest                                 Request,
                                                     JObject                                                 JSON,
                                                     CustomJObjectParserDelegate<ServiceSelectionResponse>?  CustomServiceSelectionResponseParser   = null)
        {

            if (TryParse(Request,
                         JSON,
                         out var serviceSelectionResponse,
                         out var errorResponse,
                         CustomServiceSelectionResponseParser))
            {
                return serviceSelectionResponse!;
            }

            throw new ArgumentException("The given JSON representation of a service selection response is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(Request, JSON, out ServiceSelectionResponse, out ErrorResponse, CustomServiceSelectionResponseParser = null)

        /// <summary>
        /// Try to parse the given JSON representation of a service selection response.
        /// </summary>
        /// <param name="Request">The service selection request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ServiceSelectionResponse">The parsed service selection response.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomServiceSelectionResponseParser">A delegate to parse custom service selection responses.</param>
        public static Boolean TryParse(ServiceSelectionRequest                                 Request,
                                       JObject                                                 JSON,
                                       out ServiceSelectionResponse?                           ServiceSelectionResponse,
                                       out String?                                             ErrorResponse,
                                       CustomJObjectParserDelegate<ServiceSelectionResponse>?  CustomServiceSelectionResponseParser   = null)
        {

            ErrorResponse = null;

            try
            {

                ServiceSelectionResponse = null;

                #region MessageHeader        [mandatory]

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

                #region ResponseCode         [mandatory]

                if (!JSON.ParseMandatory("responseCode",
                                         "response code",
                                         ResponseCodesExtensions.TryParse,
                                         out ResponseCodes ResponseCode,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion


                ServiceSelectionResponse = new ServiceSelectionResponse(Request,
                                                                        MessageHeader,
                                                                        ResponseCode);

                if (CustomServiceSelectionResponseParser is not null)
                    ServiceSelectionResponse = CustomServiceSelectionResponseParser(JSON,
                                                                                    ServiceSelectionResponse);

                return true;

            }
            catch (Exception e)
            {
                ServiceSelectionResponse  = null;
                ErrorResponse             = "The given JSON representation of a service selection response is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomServiceSelectionResponseSerializer = null, CustomMessageHeaderSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomServiceSelectionResponseSerializer">A delegate to serialize custom service selection responses.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<ServiceSelectionResponse>?  CustomServiceSelectionResponseSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?             CustomMessageHeaderSerializer              = null)
        {

            var json = JSONObject.Create(

                           new JProperty("messageHeader",  MessageHeader.ToJSON(CustomMessageHeaderSerializer)),
                           new JProperty("responseCode",   ResponseCode. AsText())

                       );

            return CustomServiceSelectionResponseSerializer is not null
                       ? CustomServiceSelectionResponseSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (ServiceSelectionResponse1, ServiceSelectionResponse2)

        /// <summary>
        /// Compares two service selection responses for equality.
        /// </summary>
        /// <param name="ServiceSelectionResponse1">A service selection response.</param>
        /// <param name="ServiceSelectionResponse2">Another service selection response.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ServiceSelectionResponse? ServiceSelectionResponse1,
                                           ServiceSelectionResponse? ServiceSelectionResponse2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(ServiceSelectionResponse1, ServiceSelectionResponse2))
                return true;

            // If one is null, but not both, return false.
            if (ServiceSelectionResponse1 is null || ServiceSelectionResponse2 is null)
                return false;

            return ServiceSelectionResponse1.Equals(ServiceSelectionResponse2);

        }

        #endregion

        #region Operator != (ServiceSelectionResponse1, ServiceSelectionResponse2)

        /// <summary>
        /// Compares two service selection responses for inequality.
        /// </summary>
        /// <param name="ServiceSelectionResponse1">A service selection response.</param>
        /// <param name="ServiceSelectionResponse2">Another service selection response.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ServiceSelectionResponse? ServiceSelectionResponse1,
                                           ServiceSelectionResponse? ServiceSelectionResponse2)

            => !(ServiceSelectionResponse1 == ServiceSelectionResponse2);

        #endregion

        #endregion

        #region IEquatable<ServiceSelectionResponse> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two service selection responses for equality.
        /// </summary>
        /// <param name="Object">A service selection response to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ServiceSelectionResponse serviceSelectionResponse &&
                   Equals(serviceSelectionResponse);

        #endregion

        #region Equals(ServiceSelectionResponse)

        /// <summary>
        /// Compares two service selection responses for equality.
        /// </summary>
        /// <param name="ServiceSelectionResponse">A service selection response to compare with.</param>
        public override Boolean Equals(ServiceSelectionResponse? ServiceSelectionResponse)

            => ServiceSelectionResponse is not null &&

               base.GenericEquals(ServiceSelectionResponse);

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the HashCode of this object.
        /// </summary>
        /// <returns>The HashCode of this object.</returns>
        public override Int32 GetHashCode()

            => base.GetHashCode();

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => base.ToString();

        #endregion

    }

}
