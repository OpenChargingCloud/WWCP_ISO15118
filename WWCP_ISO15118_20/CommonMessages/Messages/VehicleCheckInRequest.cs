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
    /// The vehicle checkIn request
    /// </summary>
    public class VehicleCheckInRequest : ARequest<VehicleCheckInRequest>
    {

        #region Properties

        /// <summary>
        /// The EV checkIn status.
        /// </summary>
        [Mandatory]
        public EVCheckInStatus  EVCheckInStatus    { get; }

        /// <summary>
        /// The parking method.
        /// </summary>
        [Mandatory]
        public ParkingMethods   ParkingMethod      { get; }

        /// <summary>
        /// The optional vehicle frame.
        /// </summary>
        [Optional]
        public Int16?           VehicleFrame       { get; }

        /// <summary>
        /// The optional device offset.
        /// </summary>
        [Optional]
        public Int16?           DeviceOffset       { get; }

        /// <summary>
        /// The optional vehicle travel.
        /// </summary>
        [Optional]
        public Int16?           VehicleTravel      { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new vehicle checkIn request
        /// </summary>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="EVCheckInStatus">An EV checkIn status.</param>
        /// <param name="ParkingMethod">A parking method.</param>
        /// <param name="VehicleFrame">An optional vehicle frame.</param>
        /// <param name="DeviceOffset">An optional device offset.</param>
        /// <param name="VehicleTravel">An optional vehicle travel.</param>
        public VehicleCheckInRequest(MessageHeader    MessageHeader,
                                     EVCheckInStatus  EVCheckInStatus,
                                     ParkingMethods   ParkingMethod,
                                     Int16?           VehicleFrame,
                                     Int16?           DeviceOffset,
                                     Int16?           VehicleTravel)

            : base(MessageHeader)

        {

            this.EVCheckInStatus  = EVCheckInStatus;
            this.ParkingMethod    = ParkingMethod;
            this.VehicleFrame     = VehicleFrame;
            this.DeviceOffset     = DeviceOffset;
            this.VehicleTravel    = VehicleTravel;

        }

        #endregion


        #region Documentation

        // <xs:element name="VehicleCheckInReq" type="VehicleCheckInReqType"/>
        // 
        // <xs:complexType name="VehicleCheckInReqType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:V2GRequestType">
        //             <xs:sequence>
        //                 <xs:element name="EVCheckInStatus" type="evCheckInStatusType"/>
        //                 <xs:element name="ParkingMethod"   type="parkingMethodType"/>
        //                 <xs:element name="VehicleFrame"    type="xs:short" minOccurs="0"/>
        //                 <xs:element name="DeviceOffset"    type="xs:short" minOccurs="0"/>
        //                 <xs:element name="VehicleTravel"   type="xs:short" minOccurs="0"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomVehicleCheckInRequestParser = null)

        /// <summary>
        /// Parse the given JSON representation of a vehicle checkIn request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomVehicleCheckInRequestParser">A delegate to parse custom vehicle checkIn requests.</param>
        public static VehicleCheckInRequest Parse(JObject                                              JSON,
                                                  CustomJObjectParserDelegate<VehicleCheckInRequest>?  CustomVehicleCheckInRequestParser   = null)
        {

            if (TryParse(JSON,
                         out var vehicleCheckInRequest,
                         out var errorResponse,
                         CustomVehicleCheckInRequestParser))
            {
                return vehicleCheckInRequest!;
            }

            throw new ArgumentException("The given JSON representation of a vehicle checkIn request is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out VehicleCheckInRequest, out ErrorResponse, CustomVehicleCheckInRequestParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a vehicle checkIn request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="VehicleCheckInRequest">The parsed vehicle checkIn request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                     JSON,
                                       out VehicleCheckInRequest?  VehicleCheckInRequest,
                                       out String?                 ErrorResponse)

            => TryParse(JSON,
                        out VehicleCheckInRequest,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a vehicle checkIn request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="VehicleCheckInRequest">The parsed vehicle checkIn request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomVehicleCheckInRequestParser">A delegate to parse custom vehicle checkIn requests.</param>
        public static Boolean TryParse(JObject                                              JSON,
                                       out VehicleCheckInRequest?                           VehicleCheckInRequest,
                                       out String?                                          ErrorResponse,
                                       CustomJObjectParserDelegate<VehicleCheckInRequest>?  CustomVehicleCheckInRequestParser)
        {

            try
            {

                VehicleCheckInRequest = null;

                #region MessageHeader      [mandatory]

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

                #region EVCheckInStatus    [mandatory]

                if (!JSON.ParseMandatory("evCheckInStatus",
                                         "EV checkIn status",
                                         EVCheckInStatusExtensions.TryParse,
                                         out EVCheckInStatus EVCheckInStatus,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region ParkingMethod      [mandatory]

                if (!JSON.ParseMandatory("ParkingMethod",
                                         "ParkingMethod",
                                         ParkingMethodsExtensions.TryParse,
                                         out ParkingMethods ParkingMethod,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region VehicleFrame       [optional]

                if (JSON.ParseOptional("vehicleFrame",
                                       "vehicle frame",
                                       out Int16? VehicleFrame,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region DeviceOffset       [optional]

                if (JSON.ParseOptional("deviceOffset",
                                       "device offset",
                                       out Int16? DeviceOffset,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region VehicleTravel      [optional]

                if (JSON.ParseOptional("vehicleTravel",
                                       "vehicle travel",
                                       out Int16? VehicleTravel,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                VehicleCheckInRequest = new VehicleCheckInRequest(MessageHeader,
                                                                  EVCheckInStatus,
                                                                  ParkingMethod,
                                                                  VehicleFrame,
                                                                  DeviceOffset,
                                                                  VehicleTravel);

                if (CustomVehicleCheckInRequestParser is not null)
                    VehicleCheckInRequest = CustomVehicleCheckInRequestParser(JSON,
                                                                              VehicleCheckInRequest);

                return true;

            }
            catch (Exception e)
            {
                VehicleCheckInRequest  = null;
                ErrorResponse          = "The given JSON representation of a vehicle checkIn request is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomVehicleCheckInRequestSerializer = null, CustomMessageHeaderSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomVehicleCheckInRequestSerializer">A delegate to serialize custom vehicle checkIn requests.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<VehicleCheckInRequest>?  CustomVehicleCheckInRequestSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?          CustomMessageHeaderSerializer           = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("messageHeader",    MessageHeader.  ToJSON(CustomMessageHeaderSerializer)),

                                 new JProperty("evCheckInStatus",  EVCheckInStatus.AsText()),

                                 new JProperty("parkingMethod",    ParkingMethod.  AsText()),

                           VehicleFrame.HasValue
                               ? new JProperty("vehicleFrame",     VehicleFrame. Value)
                               : null,

                           DeviceOffset.HasValue
                               ? new JProperty("deviceOffset",     DeviceOffset. Value)
                               : null,

                           VehicleTravel.HasValue
                               ? new JProperty("vehicleTravel",    VehicleTravel.Value)
                               : null

                       );

            return CustomVehicleCheckInRequestSerializer is not null
                       ? CustomVehicleCheckInRequestSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (VehicleCheckInRequest1, VehicleCheckInRequest2)

        /// <summary>
        /// Compares two vehicle checkIn requests for equality.
        /// </summary>
        /// <param name="VehicleCheckInRequest1">A vehicle checkIn request.</param>
        /// <param name="VehicleCheckInRequest2">Another vehicle checkIn request.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (VehicleCheckInRequest? VehicleCheckInRequest1,
                                           VehicleCheckInRequest? VehicleCheckInRequest2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(VehicleCheckInRequest1, VehicleCheckInRequest2))
                return true;

            // If one is null, but not both, return false.
            if (VehicleCheckInRequest1 is null || VehicleCheckInRequest2 is null)
                return false;

            return VehicleCheckInRequest1.Equals(VehicleCheckInRequest2);

        }

        #endregion

        #region Operator != (VehicleCheckInRequest1, VehicleCheckInRequest2)

        /// <summary>
        /// Compares two vehicle checkIn requests for inequality.
        /// </summary>
        /// <param name="VehicleCheckInRequest1">A vehicle checkIn request.</param>
        /// <param name="VehicleCheckInRequest2">Another vehicle checkIn request.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (VehicleCheckInRequest? VehicleCheckInRequest1,
                                           VehicleCheckInRequest? VehicleCheckInRequest2)

            => !(VehicleCheckInRequest1 == VehicleCheckInRequest2);

        #endregion

        #endregion

        #region IEquatable<VehicleCheckInRequest> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two vehicle checkIn requests for equality.
        /// </summary>
        /// <param name="Object">A vehicle checkIn request to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is VehicleCheckInRequest vehicleCheckInRequest &&
                   Equals(vehicleCheckInRequest);

        #endregion

        #region Equals(VehicleCheckInRequest)

        /// <summary>
        /// Compares two vehicle checkIn requests for equality.
        /// </summary>
        /// <param name="VehicleCheckInRequest">A vehicle checkIn request to compare with.</param>
        public override Boolean Equals(VehicleCheckInRequest? VehicleCheckInRequest)

            => VehicleCheckInRequest is not null &&

               EVCheckInStatus.Equals(VehicleCheckInRequest.EVCheckInStatus) &&
               ParkingMethod.  Equals(VehicleCheckInRequest.ParkingMethod)   &&

            ((!VehicleFrame. HasValue && !VehicleCheckInRequest.VehicleFrame. HasValue) ||
              (VehicleFrame. HasValue &&  VehicleCheckInRequest.VehicleFrame. HasValue && VehicleFrame. Value.Equals(VehicleCheckInRequest.VehicleFrame. Value))) &&

            ((!DeviceOffset. HasValue && !VehicleCheckInRequest.DeviceOffset. HasValue) ||
              (DeviceOffset. HasValue &&  VehicleCheckInRequest.DeviceOffset. HasValue && DeviceOffset. Value.Equals(VehicleCheckInRequest.DeviceOffset. Value))) &&

            ((!VehicleTravel.HasValue && !VehicleCheckInRequest.VehicleTravel.HasValue) ||
              (VehicleTravel.HasValue &&  VehicleCheckInRequest.VehicleTravel.HasValue && VehicleTravel.Value.Equals(VehicleCheckInRequest.VehicleTravel.Value))) &&

               base.           Equals(VehicleCheckInRequest);

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

                return EVCheckInStatus.GetHashCode()       * 13 ^
                       ParkingMethod.  GetHashCode()       * 11 ^
                      (VehicleFrame?.  GetHashCode() ?? 0) *  7 ^
                      (DeviceOffset?.  GetHashCode() ?? 0) *  5 ^
                      (VehicleTravel?. GetHashCode() ?? 0) *  3 ^

                       base.           GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                    EVCheckInStatus.AsText(), ", ",
                    ParkingMethod.  AsText(), ", ",

                    VehicleFrame.HasValue
                        ? ", vehicle frame: "  + VehicleFrame
                        : "",

                    DeviceOffset.HasValue
                        ? ", device offset: "  + DeviceOffset
                        : "",

                    VehicleTravel.HasValue
                        ? ", vehicle travel: " + VehicleTravel
                        : ""

               );

        #endregion

    }

}
