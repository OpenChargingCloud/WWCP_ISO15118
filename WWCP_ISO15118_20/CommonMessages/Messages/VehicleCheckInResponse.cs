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
    /// The vehicle checkIn response.
    /// </summary>
    public class VehicleCheckInResponse : AResponse<VehicleCheckInRequest,
                                                    VehicleCheckInResponse>
    {

        #region Properties

        /// <summary>
        /// The optional parking space.
        /// </summary>
        [Optional]
        public Int16?  ParkingSpace      { get; }

        /// <summary>
        /// The optional device location.
        /// </summary>
        [Optional]
        public Int16?  DeviceLocation    { get; }

        /// <summary>
        /// The optional target distance.
        /// </summary>
        [Optional]
        public Int16?  TargetDistance    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new vehicle checkIn response.
        /// </summary>
        /// <param name="Request">The vehicle checkIn request leading to this response.</param>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        /// 
        /// <param name="ParkingSpace">An optional parking space.</param>
        /// <param name="DeviceLocation">An optional device location.</param>
        /// <param name="TargetDistance">An optional target distance.</param>
        public VehicleCheckInResponse(VehicleCheckInRequest  Request,
                                      MessageHeader          MessageHeader,
                                      ResponseCodes          ResponseCode,

                                      Int16?                 ParkingSpace     = null,
                                      Int16?                 DeviceLocation   = null,
                                      Int16?                 TargetDistance   = null)

            : base(Request,
                   MessageHeader,
                   ResponseCode)

        {

            this.ParkingSpace    = ParkingSpace;
            this.DeviceLocation  = DeviceLocation;
            this.TargetDistance  = TargetDistance;

        }

        #endregion


        #region Documentation

        // <xs:element name="VehicleCheckInRes" type="VehicleCheckInResType"/>
        //
        // <xs:complexType name="VehicleCheckInResType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:V2GResponseType">
        //             <xs:sequence>
        //                 <xs:element name="ParkingSpace"   type="xs:short" minOccurs="0"/>
        //                 <xs:element name="DeviceLocation" type="xs:short" minOccurs="0"/>
        //                 <xs:element name="TargetDistance" type="xs:short" minOccurs="0"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (Request, JSON, CustomVehicleCheckInResponseParser = null)

        /// <summary>
        /// Parse the given JSON representation of a vehicle checkIn response.
        /// </summary>
        /// <param name="Request">The vehicle checkIn request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomVehicleCheckInResponseParser">A delegate to parse custom vehicle checkIn responses.</param>
        public static VehicleCheckInResponse Parse(VehicleCheckInRequest                                 Request,
                                                   JObject                                               JSON,
                                                   CustomJObjectParserDelegate<VehicleCheckInResponse>?  CustomVehicleCheckInResponseParser   = null)
        {

            if (TryParse(Request,
                         JSON,
                         out var vehicleCheckInResponse,
                         out var errorResponse,
                         CustomVehicleCheckInResponseParser))
            {
                return vehicleCheckInResponse!;
            }

            throw new ArgumentException("The given JSON representation of a vehicle checkIn response is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(Request, JSON, out VehicleCheckInResponse, out ErrorResponse, CustomVehicleCheckInResponseParser = null)

        /// <summary>
        /// Try to parse the given JSON representation of a vehicle checkIn response.
        /// </summary>
        /// <param name="Request">The vehicle checkIn request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="VehicleCheckInResponse">The parsed vehicle checkIn response.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomVehicleCheckInResponseParser">A delegate to parse custom vehicle checkIn responses.</param>
        public static Boolean TryParse(VehicleCheckInRequest                                 Request,
                                       JObject                                               JSON,
                                       out VehicleCheckInResponse?                           VehicleCheckInResponse,
                                       out String?                                           ErrorResponse,
                                       CustomJObjectParserDelegate<VehicleCheckInResponse>?  CustomVehicleCheckInResponseParser   = null)
        {

            ErrorResponse = null;

            try
            {

                VehicleCheckInResponse = null;

                #region MessageHeader     [mandatory]

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

                #region ResponseCode      [mandatory]

                if (!JSON.ParseMandatory("responseCode",
                                         "response code",
                                         ResponseCodesExtensions.TryParse,
                                         out ResponseCodes ResponseCode,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion


                #region ParkingSpace      [optional]

                if (JSON.ParseOptional("parkingSpace",
                                       "parking space",
                                       out Int16? ParkingSpace,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region DeviceLocation    [optional]

                if (JSON.ParseOptional("deviceLocation",
                                       "device location",
                                       out Int16? DeviceLocation,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region TargetDistance    [optional]

                if (JSON.ParseOptional("targetDistance",
                                       "target distance",
                                       out Int16? TargetDistance,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                VehicleCheckInResponse = new VehicleCheckInResponse(Request,
                                                                    MessageHeader,
                                                                    ResponseCode,

                                                                    ParkingSpace,
                                                                    DeviceLocation,
                                                                    TargetDistance);

                if (CustomVehicleCheckInResponseParser is not null)
                    VehicleCheckInResponse = CustomVehicleCheckInResponseParser(JSON,
                                                                                VehicleCheckInResponse);

                return true;

            }
            catch (Exception e)
            {
                VehicleCheckInResponse  = null;
                ErrorResponse           = "The given JSON representation of a vehicle checkIn response is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomVehicleCheckInResponseSerializer = null, CustomMessageHeaderSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomVehicleCheckInResponseSerializer">A delegate to serialize custom vehicle checkIn responses.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<VehicleCheckInResponse>?  CustomVehicleCheckInResponseSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?           CustomMessageHeaderSerializer            = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("messageHeader",    MessageHeader.ToJSON(CustomMessageHeaderSerializer)),

                                 new JProperty("responseCode",     ResponseCode. AsText()),

                           ParkingSpace.HasValue
                               ? new JProperty("ParkingSpace",     ParkingSpace.  Value)
                               : null,

                           DeviceLocation.HasValue
                               ? new JProperty("DeviceLocation",   DeviceLocation.Value)
                               : null,

                           TargetDistance.HasValue
                               ? new JProperty("TargetDistance",   TargetDistance.Value)
                               : null

                       );

            return CustomVehicleCheckInResponseSerializer is not null
                       ? CustomVehicleCheckInResponseSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (VehicleCheckInResponse1, VehicleCheckInResponse2)

        /// <summary>
        /// Compares two vehicle checkIn responses for equality.
        /// </summary>
        /// <param name="VehicleCheckInResponse1">A vehicle checkIn response.</param>
        /// <param name="VehicleCheckInResponse2">Another vehicle checkIn response.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (VehicleCheckInResponse? VehicleCheckInResponse1,
                                           VehicleCheckInResponse? VehicleCheckInResponse2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(VehicleCheckInResponse1, VehicleCheckInResponse2))
                return true;

            // If one is null, but not both, return false.
            if (VehicleCheckInResponse1 is null || VehicleCheckInResponse2 is null)
                return false;

            return VehicleCheckInResponse1.Equals(VehicleCheckInResponse2);

        }

        #endregion

        #region Operator != (VehicleCheckInResponse1, VehicleCheckInResponse2)

        /// <summary>
        /// Compares two vehicle checkIn responses for inequality.
        /// </summary>
        /// <param name="VehicleCheckInResponse1">A vehicle checkIn response.</param>
        /// <param name="VehicleCheckInResponse2">Another vehicle checkIn response.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (VehicleCheckInResponse? VehicleCheckInResponse1,
                                           VehicleCheckInResponse? VehicleCheckInResponse2)

            => !(VehicleCheckInResponse1 == VehicleCheckInResponse2);

        #endregion

        #endregion

        #region IEquatable<VehicleCheckInResponse> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two vehicle checkIn responses for equality.
        /// </summary>
        /// <param name="Object">A vehicle checkIn response to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is VehicleCheckInResponse vehicleCheckInResponse &&
                   Equals(vehicleCheckInResponse);

        #endregion

        #region Equals(VehicleCheckInResponse)

        /// <summary>
        /// Compares two vehicle checkIn responses for equality.
        /// </summary>
        /// <param name="VehicleCheckInResponse">A vehicle checkIn response to compare with.</param>
        public override Boolean Equals(VehicleCheckInResponse? VehicleCheckInResponse)

            => VehicleCheckInResponse is not null &&

            ((!ParkingSpace.  HasValue && !VehicleCheckInResponse.ParkingSpace.  HasValue) ||
              (ParkingSpace.  HasValue &&  VehicleCheckInResponse.ParkingSpace.  HasValue && ParkingSpace.  Value.Equals(VehicleCheckInResponse.ParkingSpace.  Value))) &&

            ((!DeviceLocation.HasValue && !VehicleCheckInResponse.DeviceLocation.HasValue) ||
              (DeviceLocation.HasValue &&  VehicleCheckInResponse.DeviceLocation.HasValue && DeviceLocation.Value.Equals(VehicleCheckInResponse.DeviceLocation.Value))) &&

            ((!TargetDistance.HasValue && !VehicleCheckInResponse.TargetDistance.HasValue) ||
              (TargetDistance.HasValue &&  VehicleCheckInResponse.TargetDistance.HasValue && TargetDistance.Value.Equals(VehicleCheckInResponse.TargetDistance.Value))) &&

               base.GenericEquals(VehicleCheckInResponse);

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

                return (ParkingSpace?.  GetHashCode() ?? 0) * 7 ^
                       (DeviceLocation?.GetHashCode() ?? 0) * 5 ^
                       (TargetDistance?.GetHashCode() ?? 0) * 3 ^

                        base.           GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => new String?[] {

                   ParkingSpace.HasValue
                       ? "parking space: "   + ParkingSpace
                       : null,

                   DeviceLocation.HasValue
                       ? "device location: " + DeviceLocation
                       : null,

                   TargetDistance.HasValue
                       ? "target distance: " + TargetDistance
                       : null

               }.Where(text => text is not null).
                 AggregateWith(", ");

        #endregion

    }

}
