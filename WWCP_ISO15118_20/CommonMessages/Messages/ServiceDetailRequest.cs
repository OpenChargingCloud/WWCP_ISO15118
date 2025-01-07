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
    /// The service detail request.
    /// </summary>
    public class ServiceDetailRequest : ARequest<ServiceDetailRequest>
    {

        #region Properties

        /// <summary>
        /// The service identification.
        /// </summary>
        [Mandatory]
        public Service_Id  ServiceId    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new service detail request.
        /// </summary>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="ServiceId">A service identification.</param>
        public ServiceDetailRequest(MessageHeader  MessageHeader,
                                    Service_Id     ServiceId)

            : base(MessageHeader)

        {

            this.ServiceId = ServiceId;

        }

        #endregion


        #region Documentation

        // <xs:element name="ServiceDetailReq" type="ServiceDetailReqType"/>
        //
        // <xs:complexType name="ServiceDetailReqType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:V2GRequestType">
        //             <xs:sequence>
        //                 <xs:element name="ServiceID" type="serviceIDType"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomServiceDetailRequestParser = null)

        /// <summary>
        /// Parse the given JSON representation of a service detail request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomServiceDetailRequestParser">An optional delegate to parse custom service detail requests.</param>
        public static ServiceDetailRequest Parse(JObject                                             JSON,
                                                 CustomJObjectParserDelegate<ServiceDetailRequest>?  CustomServiceDetailRequestParser   = null)
        {

            if (TryParse(JSON,
                         out var serviceDetailRequest,
                         out var errorResponse,
                         CustomServiceDetailRequestParser))
            {
                return serviceDetailRequest!;
            }

            throw new ArgumentException("The given JSON representation of a service detail request is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out ServiceDetailRequest, out ErrorResponse, CustomServiceDetailRequestParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a service detail request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ServiceDetailRequest">The parsed service detail request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                    JSON,
                                       out ServiceDetailRequest?  ServiceDetailRequest,
                                       out String?                ErrorResponse)

            => TryParse(JSON,
                        out ServiceDetailRequest,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a service detail request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ServiceDetailRequest">The parsed service detail request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomServiceDetailRequestParser">An optional delegate to parse custom service detail requests.</param>
        public static Boolean TryParse(JObject                                             JSON,
                                       out ServiceDetailRequest?                           ServiceDetailRequest,
                                       out String?                                         ErrorResponse,
                                       CustomJObjectParserDelegate<ServiceDetailRequest>?  CustomServiceDetailRequestParser)
        {

            try
            {

                ServiceDetailRequest = null;

                #region MessageHeader    [mandatory]

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

                #region ServiceId        [mandatory]

                if (!JSON.ParseMandatory("serviceId",
                                         "service identification",
                                         Service_Id.TryParse,
                                         out Service_Id ServiceId,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion


                ServiceDetailRequest = new ServiceDetailRequest(MessageHeader,
                                                                ServiceId);

                if (CustomServiceDetailRequestParser is not null)
                    ServiceDetailRequest = CustomServiceDetailRequestParser(JSON,
                                                                            ServiceDetailRequest);

                return true;

            }
            catch (Exception e)
            {
                ServiceDetailRequest  = null;
                ErrorResponse         = "The given JSON representation of a service detail request is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomServiceDetailRequestSerializer = null, CustomMessageHeaderSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomServiceDetailRequestSerializer">A delegate to serialize custom service detail requests.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<ServiceDetailRequest>?  CustomServiceDetailRequestSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?         CustomMessageHeaderSerializer          = null)
        {

            var json = JSONObject.Create(

                           new JProperty("messageHeader",  MessageHeader.ToJSON(CustomMessageHeaderSerializer)),
                           new JProperty("serviceId",      ServiceId.    ToString())

                       );

            return CustomServiceDetailRequestSerializer is not null
                       ? CustomServiceDetailRequestSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (ServiceDetailRequest1, ServiceDetailRequest2)

        /// <summary>
        /// Compares two service detail requests for equality.
        /// </summary>
        /// <param name="ServiceDetailRequest1">A service detail request.</param>
        /// <param name="ServiceDetailRequest2">Another service detail request.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ServiceDetailRequest? ServiceDetailRequest1,
                                           ServiceDetailRequest? ServiceDetailRequest2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(ServiceDetailRequest1, ServiceDetailRequest2))
                return true;

            // If one is null, but not both, return false.
            if (ServiceDetailRequest1 is null || ServiceDetailRequest2 is null)
                return false;

            return ServiceDetailRequest1.Equals(ServiceDetailRequest2);

        }

        #endregion

        #region Operator != (ServiceDetailRequest1, ServiceDetailRequest2)

        /// <summary>
        /// Compares two service detail requests for inequality.
        /// </summary>
        /// <param name="ServiceDetailRequest1">A service detail request.</param>
        /// <param name="ServiceDetailRequest2">Another service detail request.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ServiceDetailRequest? ServiceDetailRequest1,
                                           ServiceDetailRequest? ServiceDetailRequest2)

            => !(ServiceDetailRequest1 == ServiceDetailRequest2);

        #endregion

        #endregion

        #region IEquatable<ServiceDetailRequest> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two service detail requests for equality.
        /// </summary>
        /// <param name="Object">A service detail request to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ServiceDetailRequest serviceDetailRequest &&
                   Equals(serviceDetailRequest);

        #endregion

        #region Equals(ServiceDetailRequest)

        /// <summary>
        /// Compares two service detail requests for equality.
        /// </summary>
        /// <param name="ServiceDetailRequest">A service detail request to compare with.</param>
        public override Boolean Equals(ServiceDetailRequest? ServiceDetailRequest)

            => ServiceDetailRequest is not null &&

               ServiceId.Equals(ServiceDetailRequest.ServiceId) &&

               base.     Equals(ServiceDetailRequest);

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

                return ServiceId.GetHashCode() * 3 ^
                       base.     GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => ServiceId.ToString();

        #endregion

    }

}
