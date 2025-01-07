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

using cloud.charging.open.protocols.ISO15118_20.CommonTypes;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// The service discovery response.
    /// </summary>
    public class ServiceDiscoveryResponse : AResponse<ServiceDiscoveryRequest,
                                                      ServiceDiscoveryResponse>
    {

        #region Properties

        /// <summary>
        /// Whether service renegotiation is supported.
        /// </summary>
        [Mandatory]
        public Boolean                   ServiceRenegotiationSupported    { get; }

        /// <summary>
        /// The enumeration of available energy transfer services.
        /// [max 8]
        /// </summary>
        [Mandatory]
        public IEnumerable<Service>  EnergyTransferServices           { get; }

        /// <summary>
        /// The optional enumeration of value added services.
        /// [max 8]
        /// </summary>
        [Optional]
        public IEnumerable<Service>  ValueAddedServices               { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new service discovery response.
        /// </summary>
        /// <param name="Request">The service discovery setup request leading to this response.</param>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        /// <param name="ServiceRenegotiationSupported">Whether service renegotiation is supported.</param>
        /// <param name="EnergyTransferServices">An enumeration of available energy transfer services.</param>
        /// <param name="ValueAddedServices">An optional enumeration of value added services.</param>
        public ServiceDiscoveryResponse(ServiceDiscoveryRequest    Request,
                                        MessageHeader              MessageHeader,
                                        ResponseCodes              ResponseCode,
                                        Boolean                    ServiceRenegotiationSupported,
                                        IEnumerable<Service>   EnergyTransferServices,
                                        IEnumerable<Service>?  ValueAddedServices   = null)

            : base(Request,
                   MessageHeader,
                   ResponseCode)

        {

            this.ServiceRenegotiationSupported  = ServiceRenegotiationSupported;
            this.EnergyTransferServices         = EnergyTransferServices.Distinct();
            this.ValueAddedServices             = ValueAddedServices?.   Distinct() ?? Array.Empty<Service>();

        }

        #endregion


        #region Documentation

        // <xs:element name="ServiceDiscoveryRes" type="ServiceDiscoveryResType"/>
        //
        // <xs:complexType name="ServiceDiscoveryResType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:V2GResponseType">
        //             <xs:sequence>
        //                 <xs:element name="ServiceRenegotiationSupported" type="xs:boolean"/>
        //                 <xs:element name="EnergyTransferServiceList"     type="ServiceListType"/>
        //                 <xs:element name="VASList"                       type="ServiceListType" minOccurs="0"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>


        // <xs:complexType name="ServiceListType">
        //     <xs:sequence>
        //         <xs:element name="Service" type="ServiceType" maxOccurs="8"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (Request, JSON, CustomServiceDiscoveryResponseParser = null)

        /// <summary>
        /// Parse the given JSON representation of a service discovery response.
        /// </summary>
        /// <param name="Request">The service discovery request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomServiceDiscoveryResponseParser">An optional delegate to parse custom service discovery responses.</param>
        public static ServiceDiscoveryResponse Parse(ServiceDiscoveryRequest                                 Request,
                                                     JObject                                                 JSON,
                                                     CustomJObjectParserDelegate<ServiceDiscoveryResponse>?  CustomServiceDiscoveryResponseParser   = null)
        {

            if (TryParse(Request,
                         JSON,
                         out var serviceDiscoveryResponse,
                         out var errorResponse,
                         CustomServiceDiscoveryResponseParser))
            {
                return serviceDiscoveryResponse!;
            }

            throw new ArgumentException("The given JSON representation of a service discovery response is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(Request, JSON, out ServiceDiscoveryResponse, out ErrorResponse, CustomServiceDiscoveryResponseParser = null)

        /// <summary>
        /// Try to parse the given JSON representation of a service discovery response.
        /// </summary>
        /// <param name="Request">The service discovery request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ServiceDiscoveryResponse">The parsed service discovery response.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomServiceDiscoveryResponseParser">An optional delegate to parse custom service discovery responses.</param>
        public static Boolean TryParse(ServiceDiscoveryRequest                                 Request,
                                       JObject                                                 JSON,
                                       out ServiceDiscoveryResponse?                           ServiceDiscoveryResponse,
                                       out String?                                             ErrorResponse,
                                       CustomJObjectParserDelegate<ServiceDiscoveryResponse>?  CustomServiceDiscoveryResponseParser   = null)
        {

            ErrorResponse = null;

            try
            {

                ServiceDiscoveryResponse = null;

                #region MessageHeader                    [mandatory]

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

                #region ResponseCode                     [mandatory]

                if (!JSON.ParseMandatory("responseCode",
                                         "response code",
                                         ResponseCodesExtensions.TryParse,
                                         out ResponseCodes ResponseCode,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region ServiceRenegotiationSupported    [mandatory]

                if (!JSON.ParseMandatory("serviceRenegotiationSupported",
                                         "service renegotiation supported",
                                         out Boolean ServiceRenegotiationSupported,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region EnergyTransferServices           [mandatory]

                if (!JSON.ParseMandatoryHashSet("energyTransferServices",
                                                "energy transfer services",
                                                Service.TryParse,
                                                out HashSet<Service> EnergyTransferServices,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region ValueAddedServices               [optional]

                if (JSON.ParseOptionalHashSet("valueAddedServices",
                                              "value added services",
                                              Service.TryParse,
                                              out HashSet<Service> ValueAddedServices,
                                              out ErrorResponse))
                {
                    return false;
                }

                #endregion


                ServiceDiscoveryResponse = new ServiceDiscoveryResponse(Request,
                                                                        MessageHeader,
                                                                        ResponseCode,
                                                                        ServiceRenegotiationSupported,
                                                                        EnergyTransferServices,
                                                                        ValueAddedServices);

                if (CustomServiceDiscoveryResponseParser is not null)
                    ServiceDiscoveryResponse = CustomServiceDiscoveryResponseParser(JSON,
                                                                                    ServiceDiscoveryResponse);

                return true;

            }
            catch (Exception e)
            {
                ServiceDiscoveryResponse  = null;
                ErrorResponse             = "The given JSON representation of a service discovery response is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomServiceDiscoveryResponseSerializer = null, CustomMessageHeaderSerializer = null, ...)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomServiceDiscoveryResponseSerializer">A delegate to serialize custom service discovery responses.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        /// <param name="CustomServiceSerializer">A delegate to serialize custom services.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<ServiceDiscoveryResponse>?  CustomServiceDiscoveryResponseSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?             CustomMessageHeaderSerializer              = null,
                              CustomJObjectSerializerDelegate<Service>?                   CustomServiceSerializer                    = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("messageHeader",                  MessageHeader.ToJSON(CustomMessageHeaderSerializer)),
                                 new JProperty("responseCode",                   ResponseCode. AsText()),

                                 new JProperty("serviceRenegotiationSupported",  ServiceRenegotiationSupported),

                                 new JProperty("energyTransferServices",         new JArray(EnergyTransferServices.Select(energyTransferService => energyTransferService.ToJSON(CustomServiceSerializer)))),

                           ValueAddedServices.Any()
                               ? new JProperty("energyTransferServices",         new JArray(ValueAddedServices.    Select(valueAddedService     => valueAddedService.    ToJSON(CustomServiceSerializer))))
                               : null

                       );

            return CustomServiceDiscoveryResponseSerializer is not null
                       ? CustomServiceDiscoveryResponseSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (ServiceDiscoveryResponse1, ServiceDiscoveryResponse2)

        /// <summary>
        /// Compares two service discovery responses for equality.
        /// </summary>
        /// <param name="ServiceDiscoveryResponse1">A service discovery response.</param>
        /// <param name="ServiceDiscoveryResponse2">Another service discovery response.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ServiceDiscoveryResponse? ServiceDiscoveryResponse1,
                                           ServiceDiscoveryResponse? ServiceDiscoveryResponse2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(ServiceDiscoveryResponse1, ServiceDiscoveryResponse2))
                return true;

            // If one is null, but not both, return false.
            if (ServiceDiscoveryResponse1 is null || ServiceDiscoveryResponse2 is null)
                return false;

            return ServiceDiscoveryResponse1.Equals(ServiceDiscoveryResponse2);

        }

        #endregion

        #region Operator != (ServiceDiscoveryResponse1, ServiceDiscoveryResponse2)

        /// <summary>
        /// Compares two service discovery responses for inequality.
        /// </summary>
        /// <param name="ServiceDiscoveryResponse1">A service discovery response.</param>
        /// <param name="ServiceDiscoveryResponse2">Another service discovery response.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ServiceDiscoveryResponse? ServiceDiscoveryResponse1,
                                           ServiceDiscoveryResponse? ServiceDiscoveryResponse2)

            => !(ServiceDiscoveryResponse1 == ServiceDiscoveryResponse2);

        #endregion

        #endregion

        #region IEquatable<ServiceDiscoveryResponse> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two service discovery responses for equality.
        /// </summary>
        /// <param name="Object">A service discovery response to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ServiceDiscoveryResponse serviceDiscoveryResponse &&
                   Equals(serviceDiscoveryResponse);

        #endregion

        #region Equals(ServiceDiscoveryResponse)

        /// <summary>
        /// Compares two service discovery responses for equality.
        /// </summary>
        /// <param name="ServiceDiscoveryResponse">A service discovery response to compare with.</param>
        public override Boolean Equals(ServiceDiscoveryResponse? ServiceDiscoveryResponse)

            => ServiceDiscoveryResponse is not null &&

               ServiceRenegotiationSupported. Equals(ServiceDiscoveryResponse.ServiceRenegotiationSupported) &&

               EnergyTransferServices.Count().Equals(ServiceDiscoveryResponse.EnergyTransferServices.Count())                                       &&
               EnergyTransferServices.All(energyTransferService => ServiceDiscoveryResponse.EnergyTransferServices.Contains(energyTransferService)) &&

               ValueAddedServices.    Count().Equals(ServiceDiscoveryResponse.ValueAddedServices.    Count())                                       &&
               ValueAddedServices.    All(valueAddedService     => ServiceDiscoveryResponse.ValueAddedServices.    Contains(valueAddedService))     &&

               base.                   GenericEquals(ServiceDiscoveryResponse);

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

                return ServiceRenegotiationSupported.GetHashCode()       * 7 ^
                       EnergyTransferServices.       GetHashCode()       * 5 ^
                      (ValueAddedServices?.          GetHashCode() ?? 0) * 3 ^
                       base.                         GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   ServiceRenegotiationSupported
                       ? "yes"
                       : "no",

                   ", energy transfer services: ",
                   EnergyTransferServices.AggregateWith(", ", "n/a"),

                   ", VAS list: ",
                   ValueAddedServices.AggregateWith(", ", "n/a")

               );

        #endregion

    }

}
