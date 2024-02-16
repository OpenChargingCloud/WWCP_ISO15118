/*
 * Copyright (c) 2021-2024 GraphDefined GmbH <achim.friedland@graphdefined.com>
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
    /// The service selection request.
    /// </summary>
    public class ServiceSelectionRequest : ARequest<ServiceSelectionRequest>
    {

        #region Properties

        /// <summary>
        /// The selected energy transfer service.
        /// </summary>
        [Mandatory]
        public SelectedService               SelectedEnergyTransferService    { get; }

        /// <summary>
        /// The optional enumeration of selected value added services.
        /// </summary>
        [Optional]
        public IEnumerable<SelectedService>  SelectedValueAddedServices       { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new service selection request.
        /// </summary>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="SelectedEnergyTransferService">The selected energy transfer service.</param>
        /// <param name="SelectedValueAddedServices">The optional enumeration of selected value added services.</param>
        public ServiceSelectionRequest(MessageHeader                  MessageHeader,
                                       SelectedService                SelectedEnergyTransferService,
                                       IEnumerable<SelectedService>?  SelectedValueAddedServices   = null)

            : base(MessageHeader)

        {

            this.SelectedEnergyTransferService  = SelectedEnergyTransferService;
            this.SelectedValueAddedServices     = SelectedValueAddedServices?.Distinct() ?? Array.Empty<SelectedService>();

        }

        #endregion


        #region Documentation

        // <xs:element name="ServiceSelectionReq" type="ServiceSelectionReqType"/>
        // <xs:complexType name="ServiceSelectionReqType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:V2GRequestType">
        //             <xs:sequence>
        //                 <xs:element name="SelectedEnergyTransferService" type="SelectedServiceType"/>
        //                 <xs:element name="SelectedVASList"               type="SelectedServiceListType" minOccurs="0"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomServiceSelectionRequestParser = null)

        /// <summary>
        /// Parse the given JSON representation of a service selection request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomServiceSelectionRequestParser">An optional delegate to parse custom service selection requests.</param>
        public static ServiceSelectionRequest Parse(JObject                                                JSON,
                                                    CustomJObjectParserDelegate<ServiceSelectionRequest>?  CustomServiceSelectionRequestParser   = null)
        {

            if (TryParse(JSON,
                         out var serviceSelectionRequest,
                         out var errorResponse,
                         CustomServiceSelectionRequestParser))
            {
                return serviceSelectionRequest!;
            }

            throw new ArgumentException("The given JSON representation of a service selection request is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out ServiceSelectionRequest, out ErrorResponse, CustomServiceSelectionRequestParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a service selection request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ServiceSelectionRequest">The parsed service selection request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                       JSON,
                                       out ServiceSelectionRequest?  ServiceSelectionRequest,
                                       out String?                   ErrorResponse)

            => TryParse(JSON,
                        out ServiceSelectionRequest,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a service selection request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ServiceSelectionRequest">The parsed service selection request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomServiceSelectionRequestParser">An optional delegate to parse custom service selection requests.</param>
        public static Boolean TryParse(JObject                                                JSON,
                                       out ServiceSelectionRequest?                           ServiceSelectionRequest,
                                       out String?                                            ErrorResponse,
                                       CustomJObjectParserDelegate<ServiceSelectionRequest>?  CustomServiceSelectionRequestParser)
        {

            try
            {

                ServiceSelectionRequest = null;

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

                #region SelectedEnergyTransferService    [mandatory]

                if (!JSON.ParseMandatoryJSON("selectedEnergyTransferService",
                                             "service identification",
                                             SelectedService.TryParse,
                                             out SelectedService? SelectedEnergyTransferService,
                                             out ErrorResponse))
                {
                    return false;
                }

                if (SelectedEnergyTransferService is null)
                    return false;

                #endregion

                #region SelectedValueAddedServices       [optional]

                if (!JSON.ParseMandatoryHashSet("selectedEnergyTransferService",
                                                "service identification",
                                                SelectedService.TryParse,
                                                out HashSet<SelectedService> SelectedValueAddedServices,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion


                ServiceSelectionRequest = new ServiceSelectionRequest(MessageHeader,
                                                                      SelectedEnergyTransferService,
                                                                      SelectedValueAddedServices);

                if (CustomServiceSelectionRequestParser is not null)
                    ServiceSelectionRequest = CustomServiceSelectionRequestParser(JSON,
                                                                                  ServiceSelectionRequest);

                return true;

            }
            catch (Exception e)
            {
                ServiceSelectionRequest  = null;
                ErrorResponse            = "The given JSON representation of a service selection request is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomServiceSelectionRequestSerializer = null, CustomMessageHeaderSerializer = null, ...)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomServiceSelectionRequestSerializer">A delegate to serialize custom service selection requests.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        /// <param name="CustomSelectedServiceSerializer">A delegate to serialize custom selected services.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<ServiceSelectionRequest>?  CustomServiceSelectionRequestSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?            CustomMessageHeaderSerializer             = null,
                              CustomJObjectSerializerDelegate<SelectedService>?          CustomSelectedServiceSerializer           = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("messageHeader",                  MessageHeader.                ToJSON(CustomMessageHeaderSerializer)),
                                 new JProperty("selectedEnergyTransferService",  SelectedEnergyTransferService.ToJSON(CustomSelectedServiceSerializer)),

                           SelectedValueAddedServices.Any()
                               ? new JProperty("selectedValueAddedServices",     new JArray(SelectedValueAddedServices.Select(selectedValueAddedService => selectedValueAddedService.ToJSON(CustomSelectedServiceSerializer))))
                               : null

                       );

            return CustomServiceSelectionRequestSerializer is not null
                       ? CustomServiceSelectionRequestSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (ServiceSelectionRequest1, ServiceSelectionRequest2)

        /// <summary>
        /// Compares two service selection requests for equality.
        /// </summary>
        /// <param name="ServiceSelectionRequest1">A service selection request.</param>
        /// <param name="ServiceSelectionRequest2">Another service selection request.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ServiceSelectionRequest? ServiceSelectionRequest1,
                                           ServiceSelectionRequest? ServiceSelectionRequest2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(ServiceSelectionRequest1, ServiceSelectionRequest2))
                return true;

            // If one is null, but not both, return false.
            if (ServiceSelectionRequest1 is null || ServiceSelectionRequest2 is null)
                return false;

            return ServiceSelectionRequest1.Equals(ServiceSelectionRequest2);

        }

        #endregion

        #region Operator != (ServiceSelectionRequest1, ServiceSelectionRequest2)

        /// <summary>
        /// Compares two service selection requests for inequality.
        /// </summary>
        /// <param name="ServiceSelectionRequest1">A service selection request.</param>
        /// <param name="ServiceSelectionRequest2">Another service selection request.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ServiceSelectionRequest? ServiceSelectionRequest1,
                                           ServiceSelectionRequest? ServiceSelectionRequest2)

            => !(ServiceSelectionRequest1 == ServiceSelectionRequest2);

        #endregion

        #endregion

        #region IEquatable<ServiceSelectionRequest> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two service selection requests for equality.
        /// </summary>
        /// <param name="Object">A service selection request to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ServiceSelectionRequest serviceSelectionRequest &&
                   Equals(serviceSelectionRequest);

        #endregion

        #region Equals(ServiceSelectionRequest)

        /// <summary>
        /// Compares two service selection requests for equality.
        /// </summary>
        /// <param name="ServiceSelectionRequest">A service selection request to compare with.</param>
        public override Boolean Equals(ServiceSelectionRequest? ServiceSelectionRequest)

            => ServiceSelectionRequest is not null &&

               SelectedEnergyTransferService.Equals(ServiceSelectionRequest.SelectedEnergyTransferService) &&

               SelectedValueAddedServices.Count().Equals(ServiceSelectionRequest.SelectedValueAddedServices.Count())                                               &&
               SelectedValueAddedServices.All(selectedValueAddedService => ServiceSelectionRequest.SelectedValueAddedServices.Contains(selectedValueAddedService)) &&

               base.Equals(ServiceSelectionRequest);

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

                return SelectedEnergyTransferService.GetHashCode()  * 5 ^
                       SelectedValueAddedServices.   CalcHashCode() * 3 ^
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

                   SelectedEnergyTransferService,

                   SelectedValueAddedServices.Any()
                       ? ", " + SelectedValueAddedServices.Count() + "selected value added service(s)"
                       : ""

               );

        #endregion

    }

}
