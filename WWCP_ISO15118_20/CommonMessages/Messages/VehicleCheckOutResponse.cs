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
    /// The vehicle checkOut response.
    /// </summary>
    public class VehicleCheckOutResponse : AResponse<VehicleCheckOutRequest,
                                                     VehicleCheckOutResponse>
    {

        #region Properties

        /// <summary>
        /// The EVSE checkOut status.
        /// </summary>
        [Mandatory]
        public EVSECheckOutStatus  EVSECheckOutStatus    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new vehicle checkOut response.
        /// </summary>
        /// <param name="Request">The vehicle checkOut request leading to this response.</param>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        /// 
        /// <param name="EVSECheckOutStatus">An EVSE checkOut status.</param>
        public VehicleCheckOutResponse(VehicleCheckOutRequest  Request,
                                       MessageHeader           MessageHeader,
                                       ResponseCodes           ResponseCode,

                                       EVSECheckOutStatus      EVSECheckOutStatus)

            : base(Request,
                   MessageHeader,
                   ResponseCode)

        {

            this.EVSECheckOutStatus = EVSECheckOutStatus;

        }

        #endregion


        #region Documentation

        // <xs:element name="VehicleCheckOutRes" type="VehicleCheckOutResType"/>
        //
        // <xs:complexType name="VehicleCheckOutResType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:V2GResponseType">
        //             <xs:sequence>
        //                 <xs:element name="EVSECheckOutStatus" type="evseCheckOutStatusType"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (Request, JSON, CustomVehicleCheckOutResponseParser = null)

        /// <summary>
        /// Parse the given JSON representation of a vehicle checkOut response.
        /// </summary>
        /// <param name="Request">The vehicle checkOut request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomVehicleCheckOutResponseParser">An optional delegate to parse custom vehicle checkOut responses.</param>
        public static VehicleCheckOutResponse Parse(VehicleCheckOutRequest                                 Request,
                                                    JObject                                                JSON,
                                                    CustomJObjectParserDelegate<VehicleCheckOutResponse>?  CustomVehicleCheckOutResponseParser   = null)
        {

            if (TryParse(Request,
                         JSON,
                         out var vehicleCheckOutResponse,
                         out var errorResponse,
                         CustomVehicleCheckOutResponseParser))
            {
                return vehicleCheckOutResponse!;
            }

            throw new ArgumentException("The given JSON representation of a vehicle checkOut response is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(Request, JSON, out VehicleCheckOutResponse, out ErrorResponse, CustomVehicleCheckOutResponseParser = null)

        /// <summary>
        /// Try to parse the given JSON representation of a vehicle checkOut response.
        /// </summary>
        /// <param name="Request">The vehicle checkOut request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="VehicleCheckOutResponse">The parsed vehicle checkOut response.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomVehicleCheckOutResponseParser">An optional delegate to parse custom vehicle checkOut responses.</param>
        public static Boolean TryParse(VehicleCheckOutRequest                                 Request,
                                       JObject                                                JSON,
                                       out VehicleCheckOutResponse?                           VehicleCheckOutResponse,
                                       out String?                                            ErrorResponse,
                                       CustomJObjectParserDelegate<VehicleCheckOutResponse>?  CustomVehicleCheckOutResponseParser   = null)
        {

            ErrorResponse = null;

            try
            {

                VehicleCheckOutResponse = null;

                #region MessageHeader         [mandatory]

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

                #region ResponseCode          [mandatory]

                if (!JSON.ParseMandatory("responseCode",
                                         "response code",
                                         ResponseCodesExtensions.TryParse,
                                         out ResponseCodes ResponseCode,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region EVSECheckOutStatus    [mandatory]

                if (!JSON.ParseMandatory("EVSECheckOutStatus",
                                         "EVSECheckOutStatus",
                                         EVSECheckOutStatusExtensions.TryParse,
                                         out EVSECheckOutStatus EVSECheckOutStatus,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion


                VehicleCheckOutResponse = new VehicleCheckOutResponse(Request,
                                                                      MessageHeader,
                                                                      ResponseCode,
                                                                      EVSECheckOutStatus);

                if (CustomVehicleCheckOutResponseParser is not null)
                    VehicleCheckOutResponse = CustomVehicleCheckOutResponseParser(JSON,
                                                                                  VehicleCheckOutResponse);

                return true;

            }
            catch (Exception e)
            {
                VehicleCheckOutResponse  = null;
                ErrorResponse            = "The given JSON representation of a vehicle checkOut response is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomVehicleCheckOutResponseSerializer = null, CustomMessageHeaderSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomVehicleCheckOutResponseSerializer">A delegate to serialize custom vehicle checkOut responses.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<VehicleCheckOutResponse>?  CustomVehicleCheckOutResponseSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?          CustomMessageHeaderSerializer           = null)
        {

            var json = JSONObject.Create(

                           new JProperty("messageHeader",       MessageHeader.     ToJSON(CustomMessageHeaderSerializer)),

                           new JProperty("responseCode",        ResponseCode.      AsText()),

                           new JProperty("EVSECheckOutStatus",  EVSECheckOutStatus.AsText())

                       );

            return CustomVehicleCheckOutResponseSerializer is not null
                       ? CustomVehicleCheckOutResponseSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (VehicleCheckOutResponse1, VehicleCheckOutResponse2)

        /// <summary>
        /// Compares two vehicle checkOut responses for equality.
        /// </summary>
        /// <param name="VehicleCheckOutResponse1">A vehicle checkOut response.</param>
        /// <param name="VehicleCheckOutResponse2">Another vehicle checkOut response.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (VehicleCheckOutResponse? VehicleCheckOutResponse1,
                                           VehicleCheckOutResponse? VehicleCheckOutResponse2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(VehicleCheckOutResponse1, VehicleCheckOutResponse2))
                return true;

            // If one is null, but not both, return false.
            if (VehicleCheckOutResponse1 is null || VehicleCheckOutResponse2 is null)
                return false;

            return VehicleCheckOutResponse1.Equals(VehicleCheckOutResponse2);

        }

        #endregion

        #region Operator != (VehicleCheckOutResponse1, VehicleCheckOutResponse2)

        /// <summary>
        /// Compares two vehicle checkOut responses for inequality.
        /// </summary>
        /// <param name="VehicleCheckOutResponse1">A vehicle checkOut response.</param>
        /// <param name="VehicleCheckOutResponse2">Another vehicle checkOut response.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (VehicleCheckOutResponse? VehicleCheckOutResponse1,
                                           VehicleCheckOutResponse? VehicleCheckOutResponse2)

            => !(VehicleCheckOutResponse1 == VehicleCheckOutResponse2);

        #endregion

        #endregion

        #region IEquatable<VehicleCheckOutResponse> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two vehicle checkOut responses for equality.
        /// </summary>
        /// <param name="Object">A vehicle checkOut response to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is VehicleCheckOutResponse vehicleCheckOutResponse &&
                   Equals(vehicleCheckOutResponse);

        #endregion

        #region Equals(VehicleCheckOutResponse)

        /// <summary>
        /// Compares two vehicle checkOut responses for equality.
        /// </summary>
        /// <param name="VehicleCheckOutResponse">A vehicle checkOut response to compare with.</param>
        public override Boolean Equals(VehicleCheckOutResponse? VehicleCheckOutResponse)

            => VehicleCheckOutResponse is not null &&

               EVSECheckOutStatus.Equals(VehicleCheckOutResponse.EVSECheckOutStatus) &&

               base.       GenericEquals(VehicleCheckOutResponse);

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

                return EVSECheckOutStatus.GetHashCode() * 3 ^
                       base.              GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => EVSECheckOutStatus.AsText();

        #endregion

    }

}
