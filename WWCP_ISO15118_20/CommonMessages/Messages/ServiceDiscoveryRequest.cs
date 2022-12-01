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
    /// The service discovery request.
    /// </summary>
    public class ServiceDiscoveryRequest : AV2GRequest<ServiceDiscoveryRequest>
    {

        #region Properties

        /// <summary>
        /// The optional list of supported service identifications.
        /// </summary>
        [Optional]
        public IEnumerable<Service_Id>  SupportedServiceIds    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new service discovery request.
        /// </summary>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="SupportedServiceIds">An optional list of supported service identifications.</param>
        public ServiceDiscoveryRequest(MessageHeader             MessageHeader,
                                       IEnumerable<Service_Id>?  SupportedServiceIds   = null)

            : base(MessageHeader)

        {

            this.SupportedServiceIds = SupportedServiceIds?.Distinct() ?? Array.Empty<Service_Id>();

        }

        #endregion


        #region (static) Parse   (JSON, CustomServiceDiscoveryRequestParser = null)

        /// <summary>
        /// Parse the given JSON representation of a service discovery request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomServiceDiscoveryRequestParser">A delegate to parse custom service discovery requests.</param>
        public static ServiceDiscoveryRequest Parse(JObject                                                JSON,
                                                    CustomJObjectParserDelegate<ServiceDiscoveryRequest>?  CustomServiceDiscoveryRequestParser   = null)
        {

            if (TryParse(JSON,
                         out var serviceDiscoveryRequest,
                         out var errorResponse,
                         CustomServiceDiscoveryRequestParser))
            {
                return serviceDiscoveryRequest!;
            }

            throw new ArgumentException("The given JSON representation of a service discovery request is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out ServiceDiscoveryRequest, out ErrorResponse, CustomServiceDiscoveryRequestParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a service discovery request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ServiceDiscoveryRequest">The parsed service discovery request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                       JSON,
                                       out ServiceDiscoveryRequest?  ServiceDiscoveryRequest,
                                       out String?                   ErrorResponse)

            => TryParse(JSON,
                        out ServiceDiscoveryRequest,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a service discovery request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ServiceDiscoveryRequest">The parsed service discovery request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomServiceDiscoveryRequestParser">A delegate to parse custom BootNotification requests.</param>
        public static Boolean TryParse(JObject                                                JSON,
                                       out ServiceDiscoveryRequest?                           ServiceDiscoveryRequest,
                                       out String?                                            ErrorResponse,
                                       CustomJObjectParserDelegate<ServiceDiscoveryRequest>?  CustomServiceDiscoveryRequestParser)
        {

            try
            {

                ServiceDiscoveryRequest = null;

                #region MessageHeader          [mandatory]

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

                #region SupportedServiceIds    [optional]

                if (JSON.ParseOptionalHashSet("selectedServiceDiscoveryService",
                                              "selected service discovery service",
                                              Service_Id.TryParse,
                                              out HashSet<Service_Id> SupportedServiceIds,
                                              out ErrorResponse))
                {
                    return false;
                }

                #endregion


                ServiceDiscoveryRequest = new ServiceDiscoveryRequest(MessageHeader,
                                                                      SupportedServiceIds);

                if (CustomServiceDiscoveryRequestParser is not null)
                    ServiceDiscoveryRequest = CustomServiceDiscoveryRequestParser(JSON,
                                                                                  ServiceDiscoveryRequest);

                return true;

            }
            catch (Exception e)
            {
                ServiceDiscoveryRequest  = null;
                ErrorResponse            = "The given JSON representation of a service discovery request is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomServiceDiscoveryRequestSerializer = null, CustomMessageHeaderSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomServiceDiscoveryRequestSerializer">A delegate to serialize custom service discovery requests.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<ServiceDiscoveryRequest>?  CustomServiceDiscoveryRequestSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?            CustomMessageHeaderSerializer             = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("messageHeader",        MessageHeader.ToJSON(CustomMessageHeaderSerializer)),

                           SupportedServiceIds.Any()
                               ? new JProperty("supportedServiceIds",  new JArray(SupportedServiceIds.Select(supportedServiceId => supportedServiceId.Value)))
                               : null

                       );

            return CustomServiceDiscoveryRequestSerializer is not null
                       ? CustomServiceDiscoveryRequestSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (ServiceDiscoveryRequest1, ServiceDiscoveryRequest2)

        /// <summary>
        /// Compares two service discovery requests for equality.
        /// </summary>
        /// <param name="ServiceDiscoveryRequest1">A service discovery request.</param>
        /// <param name="ServiceDiscoveryRequest2">Another service discovery request.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ServiceDiscoveryRequest? ServiceDiscoveryRequest1,
                                           ServiceDiscoveryRequest? ServiceDiscoveryRequest2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(ServiceDiscoveryRequest1, ServiceDiscoveryRequest2))
                return true;

            // If one is null, but not both, return false.
            if (ServiceDiscoveryRequest1 is null || ServiceDiscoveryRequest2 is null)
                return false;

            return ServiceDiscoveryRequest1.Equals(ServiceDiscoveryRequest2);

        }

        #endregion

        #region Operator != (ServiceDiscoveryRequest1, ServiceDiscoveryRequest2)

        /// <summary>
        /// Compares two service discovery requests for inequality.
        /// </summary>
        /// <param name="ServiceDiscoveryRequest1">A service discovery request.</param>
        /// <param name="ServiceDiscoveryRequest2">Another service discovery request.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ServiceDiscoveryRequest? ServiceDiscoveryRequest1,
                                           ServiceDiscoveryRequest? ServiceDiscoveryRequest2)

            => !(ServiceDiscoveryRequest1 == ServiceDiscoveryRequest2);

        #endregion

        #endregion

        #region IEquatable<ServiceDiscoveryRequest> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two service discovery requests for equality.
        /// </summary>
        /// <param name="Object">A service discovery request to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ServiceDiscoveryRequest serviceDiscoveryRequest &&
                   Equals(serviceDiscoveryRequest);

        #endregion

        #region Equals(ServiceDiscoveryRequest)

        /// <summary>
        /// Compares two service discovery requests for equality.
        /// </summary>
        /// <param name="ServiceDiscoveryRequest">A service discovery request to compare with.</param>
        public override Boolean Equals(ServiceDiscoveryRequest? ServiceDiscoveryRequest)

            => ServiceDiscoveryRequest is not null &&

               SupportedServiceIds.Count().Equals(ServiceDiscoveryRequest.SupportedServiceIds.Count())     &&
               SupportedServiceIds.All(data => ServiceDiscoveryRequest.SupportedServiceIds.Contains(data)) &&

               base.GenericEquals(ServiceDiscoveryRequest);

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

                return SupportedServiceIds.GetHashCode() * 3 ^
                       base.               GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => SupportedServiceIds.AggregateWith(", ");

        #endregion

    }

}
