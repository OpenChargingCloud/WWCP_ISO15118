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
    /// The service detail response.
    /// </summary>
    public class ServiceDetailResponse : AV2GResponse<ServiceDetailRequest,
                                                      ServiceDetailResponse>
    {

        #region Properties

        /// <summary>
        /// The service identification.
        /// </summary>
        [Mandatory]
        public Service_Id                 ServiceId            { get; }

        /// <summary>
        /// 
        /// </summary>
        [Mandatory]
        public IEnumerable<ParameterSet>  ServiceParameters    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new service detail response.
        /// </summary>
        /// <param name="Request">The service detail setup request leading to this response.</param>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        /// <param name="ServiceId">A service identification.</param>
        /// <param name="ServiceParameters"></param>
        public ServiceDetailResponse(ServiceDetailRequest       Request,
                                     MessageHeader              MessageHeader,
                                     ResponseCodes              ResponseCode,
                                     Service_Id                 ServiceId,
                                     IEnumerable<ParameterSet>  ServiceParameters)

            : base(Request,
                   MessageHeader,
                   ResponseCode)

        {

            this.ServiceId          = ServiceId;
            this.ServiceParameters  = ServiceParameters;

        }

        #endregion


        #region (static) Parse   (Request, JSON, CustomServiceDetailResponseParser = null)

        /// <summary>
        /// Parse the given JSON representation of a service detail response.
        /// </summary>
        /// <param name="Request">The service detail request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomServiceDetailResponseParser">A delegate to parse custom service detail responses.</param>
        public static ServiceDetailResponse Parse(ServiceDetailRequest                                 Request,
                                                  JObject                                              JSON,
                                                  CustomJObjectParserDelegate<ServiceDetailResponse>?  CustomServiceDetailResponseParser   = null)
        {

            if (TryParse(Request,
                         JSON,
                         out var serviceDetailResponse,
                         out var errorResponse,
                         CustomServiceDetailResponseParser))
            {
                return serviceDetailResponse!;
            }

            throw new ArgumentException("The given JSON representation of a service detail response is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(Request, JSON, out ServiceDetailResponse, out ErrorResponse, CustomServiceDetailResponseParser = null)

        /// <summary>
        /// Try to parse the given JSON representation of a service detail response.
        /// </summary>
        /// <param name="Request">The service detail request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ServiceDetailResponse">The parsed service detail response.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomServiceDetailResponseParser">A delegate to parse custom service detail responses.</param>
        public static Boolean TryParse(ServiceDetailRequest                                 Request,
                                       JObject                                              JSON,
                                       out ServiceDetailResponse?                           ServiceDetailResponse,
                                       out String?                                          ErrorResponse,
                                       CustomJObjectParserDelegate<ServiceDetailResponse>?  CustomServiceDetailResponseParser   = null)
        {

            ErrorResponse = null;

            try
            {

                ServiceDetailResponse = null;

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

                #region ServiceId            [mandatory]

                if (!JSON.ParseMandatory("serviceId",
                                         "service identification",
                                         Service_Id.TryParse,
                                         out Service_Id ServiceId,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region ServiceParameters    [mandatory]

                if (!JSON.ParseMandatoryHashSet("serviceParameters",
                                                "service parameters",
                                                ParameterSet.TryParse,
                                                out HashSet<ParameterSet> ServiceParameters,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion


                ServiceDetailResponse = new ServiceDetailResponse(Request,
                                                                  MessageHeader,
                                                                  ResponseCode,
                                                                  ServiceId,
                                                                  ServiceParameters);

                if (CustomServiceDetailResponseParser is not null)
                    ServiceDetailResponse = CustomServiceDetailResponseParser(JSON,
                                                                              ServiceDetailResponse);

                return true;

            }
            catch (Exception e)
            {
                ServiceDetailResponse  = null;
                ErrorResponse          = "The given JSON representation of a service detail response is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomServiceDetailResponseSerializer = null, CustomMessageHeaderSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomServiceDetailResponseSerializer">A delegate to serialize custom service detail responses.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<ServiceDetailResponse>?  CustomServiceDetailResponseSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?          CustomMessageHeaderSerializer           = null)
        {

            var json = JSONObject.Create(

                           new JProperty("messageHeader",      MessageHeader.ToJSON  (CustomMessageHeaderSerializer)),
                           new JProperty("responseCode",       ResponseCode. AsText  ()),
                           new JProperty("serviceId",          ServiceId.    ToString()),

                           new JProperty("serviceParameters",  new JArray(ServiceParameters.Select(serviceParameter => serviceParameter.ToJSON())))

                       );

            return CustomServiceDetailResponseSerializer is not null
                       ? CustomServiceDetailResponseSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (ServiceDetailResponse1, ServiceDetailResponse2)

        /// <summary>
        /// Compares two service detail responses for equality.
        /// </summary>
        /// <param name="ServiceDetailResponse1">A service detail response.</param>
        /// <param name="ServiceDetailResponse2">Another service detail response.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ServiceDetailResponse? ServiceDetailResponse1,
                                           ServiceDetailResponse? ServiceDetailResponse2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(ServiceDetailResponse1, ServiceDetailResponse2))
                return true;

            // If one is null, but not both, return false.
            if (ServiceDetailResponse1 is null || ServiceDetailResponse2 is null)
                return false;

            return ServiceDetailResponse1.Equals(ServiceDetailResponse2);

        }

        #endregion

        #region Operator != (ServiceDetailResponse1, ServiceDetailResponse2)

        /// <summary>
        /// Compares two service detail responses for inequality.
        /// </summary>
        /// <param name="ServiceDetailResponse1">A service detail response.</param>
        /// <param name="ServiceDetailResponse2">Another service detail response.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ServiceDetailResponse? ServiceDetailResponse1,
                                           ServiceDetailResponse? ServiceDetailResponse2)

            => !(ServiceDetailResponse1 == ServiceDetailResponse2);

        #endregion

        #endregion

        #region IEquatable<ServiceDetailResponse> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two service detail responses for equality.
        /// </summary>
        /// <param name="Object">A service detail response to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ServiceDetailResponse serviceDetailResponse &&
                   Equals(serviceDetailResponse);

        #endregion

        #region Equals(ServiceDetailResponse)

        /// <summary>
        /// Compares two service detail responses for equality.
        /// </summary>
        /// <param name="ServiceDetailResponse">A service detail response to compare with.</param>
        public override Boolean Equals(ServiceDetailResponse? ServiceDetailResponse)

            => ServiceDetailResponse is not null &&

               ServiceId.  Equals(ServiceDetailResponse.ServiceId) &&

               ServiceParameters.Count().Equals(ServiceDetailResponse.ServiceParameters.Count())     &&
               ServiceParameters.All(data => ServiceDetailResponse.ServiceParameters.Contains(data)) &&

               base.GenericEquals(ServiceDetailResponse);

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

                return ServiceId.GetHashCode() * 5 ^
                       //ToDo: Add ServiceParameters!
                       base.     GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   ServiceId,
                   ": ",
                   ServiceParameters.Count(),
                   " service parameters"

               );

        #endregion

    }

}
