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
    /// The vehicle checkOut request.
    /// </summary>
    public class VehicleCheckOutRequest : ARequest<VehicleCheckOutRequest>
    {

        #region Properties

        /// <summary>
        /// The EV checkOut status.
        /// </summary>
        [Mandatory]
        public EVCheckOutStatus  EVCheckOutStatus    { get; }

        /// <summary>
        /// The checkOut time.
        /// </summary>
        [Mandatory]
        public DateTime          CheckOutTime        { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new vehicle checkOut request.
        /// </summary>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="EVCheckOutStatus">A service identification.</param>
        /// <param name="CheckOutTime"></param>
        public VehicleCheckOutRequest(MessageHeader     MessageHeader,
                                      EVCheckOutStatus  EVCheckOutStatus,
                                      DateTime          CheckOutTime)

            : base(MessageHeader)

        {

            this.EVCheckOutStatus  = EVCheckOutStatus;
            this.CheckOutTime      = CheckOutTime;

        }

        #endregion


        #region Documentation

        // <xs:element name="VehicleCheckOutReq" type="VehicleCheckOutReqType"/>
        //
        // <xs:complexType name="VehicleCheckOutReqType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:V2GRequestType">
        //             <xs:sequence>
        //                 <xs:element name="EVCheckOutStatus" type="evCheckOutStatusType"/>
        //                 <xs:element name="CheckOutTime"     type="xs:unsignedLong"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomVehicleCheckOutRequestParser = null)

        /// <summary>
        /// Parse the given JSON representation of a vehicle checkOut request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomVehicleCheckOutRequestParser">An optional delegate to parse custom vehicle checkOut requests.</param>
        public static VehicleCheckOutRequest Parse(JObject                                               JSON,
                                                   CustomJObjectParserDelegate<VehicleCheckOutRequest>?  CustomVehicleCheckOutRequestParser   = null)
        {

            if (TryParse(JSON,
                         out var vehicleCheckOutRequest,
                         out var errorResponse,
                         CustomVehicleCheckOutRequestParser))
            {
                return vehicleCheckOutRequest!;
            }

            throw new ArgumentException("The given JSON representation of a vehicle checkOut request is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out VehicleCheckOutRequest, out ErrorResponse, CustomVehicleCheckOutRequestParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a vehicle checkOut request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="VehicleCheckOutRequest">The parsed vehicle checkOut request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                      JSON,
                                       out VehicleCheckOutRequest?  VehicleCheckOutRequest,
                                       out String?                  ErrorResponse)

            => TryParse(JSON,
                        out VehicleCheckOutRequest,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a vehicle checkOut request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="VehicleCheckOutRequest">The parsed vehicle checkOut request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomVehicleCheckOutRequestParser">An optional delegate to parse custom vehicle checkOut requests.</param>
        public static Boolean TryParse(JObject                                               JSON,
                                       out VehicleCheckOutRequest?                           VehicleCheckOutRequest,
                                       out String?                                           ErrorResponse,
                                       CustomJObjectParserDelegate<VehicleCheckOutRequest>?  CustomVehicleCheckOutRequestParser)
        {

            try
            {

                VehicleCheckOutRequest = null;

                #region MessageHeader       [mandatory]

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

                #region EVCheckOutStatus    [mandatory]

                if (!JSON.ParseMandatory("EVCheckOutStatus",
                                         "EVCheckOutStatus",
                                         EVCheckOutStatusExtensions.TryParse,
                                         out EVCheckOutStatus EVCheckOutStatus,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region CheckOutTime        [mandatory]

                if (!JSON.ParseMandatory("CheckOutTime",
                                         "CheckOutTime",
                                         out DateTime CheckOutTime,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion


                VehicleCheckOutRequest = new VehicleCheckOutRequest(MessageHeader,
                                                                    EVCheckOutStatus,
                                                                    CheckOutTime);

                if (CustomVehicleCheckOutRequestParser is not null)
                    VehicleCheckOutRequest = CustomVehicleCheckOutRequestParser(JSON,
                                                                                VehicleCheckOutRequest);

                return true;

            }
            catch (Exception e)
            {
                VehicleCheckOutRequest  = null;
                ErrorResponse           = "The given JSON representation of a vehicle checkOut request is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomVehicleCheckOutRequestSerializer = null, CustomMessageHeaderSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomVehicleCheckOutRequestSerializer">A delegate to serialize custom vehicle checkOut requests.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<VehicleCheckOutRequest>?  CustomVehicleCheckOutRequestSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?           CustomMessageHeaderSerializer            = null)
        {

            var json = JSONObject.Create(

                           new JProperty("messageHeader",     MessageHeader.   ToJSON(CustomMessageHeaderSerializer)),
                           new JProperty("evCheckOutStatus",  EVCheckOutStatus.AsText()),
                           new JProperty("checkOutTime",      CheckOutTime.    ToISO8601())

                       );

            return CustomVehicleCheckOutRequestSerializer is not null
                       ? CustomVehicleCheckOutRequestSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (VehicleCheckOutRequest1, VehicleCheckOutRequest2)

        /// <summary>
        /// Compares two vehicle checkOut requests for equality.
        /// </summary>
        /// <param name="VehicleCheckOutRequest1">A vehicle checkOut request.</param>
        /// <param name="VehicleCheckOutRequest2">Another vehicle checkOut request.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (VehicleCheckOutRequest? VehicleCheckOutRequest1,
                                           VehicleCheckOutRequest? VehicleCheckOutRequest2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(VehicleCheckOutRequest1, VehicleCheckOutRequest2))
                return true;

            // If one is null, but not both, return false.
            if (VehicleCheckOutRequest1 is null || VehicleCheckOutRequest2 is null)
                return false;

            return VehicleCheckOutRequest1.Equals(VehicleCheckOutRequest2);

        }

        #endregion

        #region Operator != (VehicleCheckOutRequest1, VehicleCheckOutRequest2)

        /// <summary>
        /// Compares two vehicle checkOut requests for inequality.
        /// </summary>
        /// <param name="VehicleCheckOutRequest1">A vehicle checkOut request.</param>
        /// <param name="VehicleCheckOutRequest2">Another vehicle checkOut request.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (VehicleCheckOutRequest? VehicleCheckOutRequest1,
                                           VehicleCheckOutRequest? VehicleCheckOutRequest2)

            => !(VehicleCheckOutRequest1 == VehicleCheckOutRequest2);

        #endregion

        #endregion

        #region IEquatable<VehicleCheckOutRequest> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two vehicle checkOut requests for equality.
        /// </summary>
        /// <param name="Object">A vehicle checkOut request to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is VehicleCheckOutRequest vehicleCheckOutRequest &&
                   Equals(vehicleCheckOutRequest);

        #endregion

        #region Equals(VehicleCheckOutRequest)

        /// <summary>
        /// Compares two vehicle checkOut requests for equality.
        /// </summary>
        /// <param name="VehicleCheckOutRequest">A vehicle checkOut request to compare with.</param>
        public override Boolean Equals(VehicleCheckOutRequest? VehicleCheckOutRequest)

            => VehicleCheckOutRequest is not null &&

               EVCheckOutStatus.Equals(VehicleCheckOutRequest.EVCheckOutStatus) &&
               CheckOutTime.    Equals(VehicleCheckOutRequest.CheckOutTime)     &&

               base.            Equals(VehicleCheckOutRequest);

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the HashCode of this object.
        /// </summary>
        public override Int32 GetHashCode()
        {
            unchecked
            {

                return EVCheckOutStatus.GetHashCode() * 5 ^
                       CheckOutTime.    GetHashCode() * 3 ^

                       base.            GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   EVCheckOutStatus.AsText(),
                   ", ",
                   CheckOutTime.ToISO8601()

               );

        #endregion

    }

}
