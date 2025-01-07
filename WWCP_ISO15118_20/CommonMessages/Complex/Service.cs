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

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// The (charging) service.
    /// </summary>
    public class Service
    {

        #region Properties

        /// <summary>
        /// The unique (charging) service identification.
        /// </summary>
        [Mandatory]
        public Service_Id  ServiceId      { get; }

        /// <summary>
        /// Whether this (charging) service is free.
        /// </summary>
        [Mandatory]
        public Boolean     FreeService    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new (charging) service.
        /// </summary>
        /// <param name="ServiceId">An unique (charging) service identification.</param>
        /// <param name="FreeService">Whether this (charging) service is free.</param>
        public Service(Service_Id  ServiceId,
                       Boolean     FreeService)
        {

            this.ServiceId    = ServiceId;
            this.FreeService  = FreeService;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="ServiceType">
        //     <xs:sequence>
        //         <xs:element name="ServiceID"   type="serviceIDType"/>
        //         <xs:element name="FreeService" type="xs:boolean"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomServiceParser = null)

        /// <summary>
        /// Parse the given JSON representation of a service.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomServiceParser">An optional delegate to parse custom services.</param>
        public static Service Parse(JObject                                JSON,
                                    CustomJObjectParserDelegate<Service>?  CustomServiceParser   = null)
        {

            if (TryParse(JSON,
                         out var service,
                         out var errorResponse,
                         CustomServiceParser))
            {
                return service!;
            }

            throw new ArgumentException("The given JSON representation of a service is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out Service, out ErrorResponse, CustomServiceParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a service.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="Service">The parsed service.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject       JSON,
                                       out Service?  Service,
                                       out String?   ErrorResponse)

            => TryParse(JSON,
                        out Service,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a service.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="Service">The parsed service.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomServiceParser">An optional delegate to parse custom contract certificates.</param>
        public static Boolean TryParse(JObject                                JSON,
                                       out Service?                           Service,
                                       out String?                            ErrorResponse,
                                       CustomJObjectParserDelegate<Service>?  CustomServiceParser)
        {

            try
            {

                Service = null;

                #region ServiceId      [mandatory]

                if (!JSON.ParseMandatory("serviceId",
                                         "service identification",
                                         Service_Id.TryParse,
                                         out Service_Id ServiceId,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region FreeService    [mandatory]

                if (!JSON.ParseMandatory("freeService",
                                         "free service",
                                         out Boolean FreeService,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion


                Service = new Service(ServiceId,
                                      FreeService);

                if (CustomServiceParser is not null)
                    Service = CustomServiceParser(JSON,
                                                  Service);

                return true;

            }
            catch (Exception e)
            {
                Service        = null;
                ErrorResponse  = "The given JSON representation of a service is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomServiceSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomServiceSerializer">A delegate to serialize custom services.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<Service>? CustomServiceSerializer = null)
        {

            var json = JSONObject.Create(

                           new JProperty("serviceId",    ServiceId.ToString()),
                           new JProperty("freeService",  FreeService)

                       );

            return CustomServiceSerializer is not null
                       ? CustomServiceSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (Service1, Service2)

        /// <summary>
        /// Compares two services for equality.
        /// </summary>
        /// <param name="Service1">A service.</param>
        /// <param name="Service2">Another service.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (Service? Service1,
                                           Service? Service2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(Service1, Service2))
                return true;

            // If one is null, but not both, return false.
            if (Service1 is null || Service2 is null)
                return false;

            return Service1.Equals(Service2);

        }

        #endregion

        #region Operator != (Service1, Service2)

        /// <summary>
        /// Compares two services for inequality.
        /// </summary>
        /// <param name="Service1">A service.</param>
        /// <param name="Service2">Another service.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (Service? Service1,
                                           Service? Service2)

            => !(Service1 == Service2);

        #endregion

        #endregion

        #region IEquatable<Service> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two services for equality.
        /// </summary>
        /// <param name="Object">A service to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is Service service &&
                   Equals(service);

        #endregion

        #region Equals(Service)

        /// <summary>
        /// Compares two services for equality.
        /// </summary>
        /// <param name="Service">A service to compare with.</param>
        public Boolean Equals(Service? Service)

            => Service is not null &&

               ServiceId.  Equals(Service.ServiceId) &&
               FreeService.Equals(Service.FreeService);

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

                return ServiceId.  GetHashCode() * 5 ^
                       FreeService.GetHashCode() * 3 ^

                       base.       GetHashCode();

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

                   FreeService
                       ? " [free]"
                       : ""

               );

        #endregion

    }

}
