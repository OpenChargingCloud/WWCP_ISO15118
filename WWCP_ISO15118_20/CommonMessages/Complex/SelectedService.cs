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

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// A selected service.
    /// </summary>
    public class SelectedService
    {

        #region Properties

        /// <summary>
        /// The selected service identification.
        /// </summary>
        public Service_Id       ServiceId         { get; }

        /// <summary>
        /// The selected parameter set identification.
        /// </summary>
        public ParameterSet_Id  ParameterSetId    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new selected service.
        /// </summary>
        /// <param name="ServiceId">A selected service identification.</param>
        /// <param name="ParameterSetId">A selected parameter set identification.</param>
        public SelectedService(Service_Id       ServiceId,
                               ParameterSet_Id  ParameterSetId)
        {

            this.ServiceId       = ServiceId;
            this.ParameterSetId  = ParameterSetId;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="SelectedServiceType">
        //     <xs:sequence>
        //         <xs:element name="ServiceID"      type="serviceIDType"/>
        //         <xs:element name="ParameterSetID" type="serviceIDType"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomSelectedServiceParser = null)

        /// <summary>
        /// Parse the given JSON representation of a selected service.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomSelectedServiceParser">A delegate to parse custom selected services.</param>
        public static SelectedService Parse(JObject                                        JSON,
                                            CustomJObjectParserDelegate<SelectedService>?  CustomSelectedServiceParser   = null)
        {

            if (TryParse(JSON,
                         out var selectedService,
                         out var errorResponse,
                         CustomSelectedServiceParser))
            {
                return selectedService!;
            }

            throw new ArgumentException("The given JSON representation of a selected service is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out SelectedService, out ErrorResponse, CustomSelectedServiceParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a selected service.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="SelectedService">The parsed selected service.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject               JSON,
                                       out SelectedService?  SelectedService,
                                       out String?           ErrorResponse)

            => TryParse(JSON,
                        out SelectedService,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a selected service.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="SelectedService">The parsed selected service.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomSelectedServiceParser">A delegate to parse custom selected services.</param>
        public static Boolean TryParse(JObject                                        JSON,
                                       out SelectedService?                           SelectedService,
                                       out String?                                    ErrorResponse,
                                       CustomJObjectParserDelegate<SelectedService>?  CustomSelectedServiceParser)
        {

            try
            {

                SelectedService = null;

                #region ServiceId         [mandatory]

                if (!JSON.ParseMandatory("serviceId",
                                         "service identification",
                                         Service_Id.TryParse,
                                         out Service_Id ServiceId,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region ParameterSetId    [mandatory]

                if (!JSON.ParseMandatory("parameterSetId",
                                         "parameter set identification",
                                         ParameterSet_Id.TryParse,
                                         out ParameterSet_Id ParameterSetId,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion


                SelectedService = new SelectedService(ServiceId,
                                                      ParameterSetId);

                if (CustomSelectedServiceParser is not null)
                    SelectedService = CustomSelectedServiceParser(JSON,
                                                                  SelectedService);

                return true;

            }
            catch (Exception e)
            {
                SelectedService  = null;
                ErrorResponse    = "The given JSON representation of a selected service is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomSelectedServiceSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomSelectedServiceSerializer">A delegate to serialize custom selected services.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<SelectedService>? CustomSelectedServiceSerializer = null)
        {

            var json = JSONObject.Create(

                           new JProperty("serviceId",       ServiceId.     ToString()),
                           new JProperty("parameterSetId",  ParameterSetId.ToString())

                       );

            return CustomSelectedServiceSerializer is not null
                       ? CustomSelectedServiceSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (SelectedService1, SelectedService2)

        /// <summary>
        /// Compares two selected services for equality.
        /// </summary>
        /// <param name="SelectedService1">A selected service.</param>
        /// <param name="SelectedService2">Another selected service.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (SelectedService? SelectedService1,
                                           SelectedService? SelectedService2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(SelectedService1, SelectedService2))
                return true;

            // If one is null, but not both, return false.
            if (SelectedService1 is null || SelectedService2 is null)
                return false;

            return SelectedService1.Equals(SelectedService2);

        }

        #endregion

        #region Operator != (SelectedService1, SelectedService2)

        /// <summary>
        /// Compares two selected services for inequality.
        /// </summary>
        /// <param name="SelectedService1">A selected service.</param>
        /// <param name="SelectedService2">Another selected service.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (SelectedService? SelectedService1,
                                           SelectedService? SelectedService2)

            => !(SelectedService1 == SelectedService2);

        #endregion

        #endregion

        #region IEquatable<SelectedService> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two selected services for equality.
        /// </summary>
        /// <param name="Object">A selected service to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is SelectedService selectedService &&
                   Equals(selectedService);

        #endregion

        #region Equals(SelectedService)

        /// <summary>
        /// Compares two selected services for equality.
        /// </summary>
        /// <param name="SelectedService">A selected service to compare with.</param>
        public Boolean Equals(SelectedService? SelectedService)

            => SelectedService is not null &&

               ServiceId.     Equals(SelectedService.ServiceId) &&
               ParameterSetId.Equals(SelectedService.ParameterSetId);

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

                return ServiceId.     GetHashCode() * 5 ^
                       ParameterSetId.GetHashCode() * 3 ^

                       base.          GetHashCode();

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
                   " / ",
                   ParameterSetId

               );

        #endregion


    }

}
