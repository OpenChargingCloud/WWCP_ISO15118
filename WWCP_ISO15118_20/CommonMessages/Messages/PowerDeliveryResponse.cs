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
    /// The power delivery response.
    /// </summary>
    public class PowerDeliveryResponse : AResponse<PowerDeliveryRequest,
                                                   PowerDeliveryResponse>
    {

        #region Properties

        /// <summary>
        /// The EVSE status.
        /// </summary>
        [Mandatory]
        public EVSEStatusType?  EVSEStatus    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new power delivery response.
        /// </summary>
        /// <param name="Request">The power delivery request leading to this response.</param>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        /// <param name="EVSEStatus">An EVSE status.</param>
        public PowerDeliveryResponse(PowerDeliveryRequest  Request,
                                     MessageHeader         MessageHeader,
                                     ResponseCodes         ResponseCode,
                                     EVSEStatusType?       EVSEStatus)

            : base(Request,
                   MessageHeader,
                   ResponseCode)

        {

            this.EVSEStatus = EVSEStatus;

        }

        #endregion


        #region Documentation

        // <xs:element name="PowerDeliveryRes" type="PowerDeliveryResType"/>
        // 
        // <xs:complexType name="PowerDeliveryResType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:V2GResponseType">
        //             <xs:sequence>
        //                 <xs:element name="EVSEStatus" type="v2gci_ct:EVSEStatusType" minOccurs="0"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (Request, JSON, CustomPowerDeliveryResponseParser = null)

        /// <summary>
        /// Parse the given JSON representation of a power delivery response.
        /// </summary>
        /// <param name="Request">The power delivery request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomPowerDeliveryResponseParser">An optional delegate to parse custom power delivery responses.</param>
        public static PowerDeliveryResponse Parse(PowerDeliveryRequest                                 Request,
                                                  JObject                                              JSON,
                                                  CustomJObjectParserDelegate<PowerDeliveryResponse>?  CustomPowerDeliveryResponseParser   = null)
        {

            if (TryParse(Request,
                         JSON,
                         out var powerDeliveryResponse,
                         out var errorResponse,
                         CustomPowerDeliveryResponseParser))
            {
                return powerDeliveryResponse!;
            }

            throw new ArgumentException("The given JSON representation of a power delivery response is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(Request, JSON, out PowerDeliveryResponse, out ErrorResponse, CustomPowerDeliveryResponseParser = null)

        /// <summary>
        /// Try to parse the given JSON representation of a power delivery response.
        /// </summary>
        /// <param name="Request">The power delivery request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="PowerDeliveryResponse">The parsed power delivery response.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomPowerDeliveryResponseParser">An optional delegate to parse custom power delivery responses.</param>
        public static Boolean TryParse(PowerDeliveryRequest                                 Request,
                                       JObject                                              JSON,
                                       out PowerDeliveryResponse?                           PowerDeliveryResponse,
                                       out String?                                          ErrorResponse,
                                       CustomJObjectParserDelegate<PowerDeliveryResponse>?  CustomPowerDeliveryResponseParser   = null)
        {

            ErrorResponse = null;

            try
            {

                PowerDeliveryResponse = null;

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

                #region ResponseCode     [mandatory]

                if (!JSON.ParseMandatory("responseCode",
                                         "response code",
                                         ResponseCodesExtensions.TryParse,
                                         out ResponseCodes ResponseCode,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region EVSEStatus       [optional]

                if (JSON.ParseOptionalJSON("evseStatus",
                                           "EVSE status",
                                           EVSEStatusType.TryParse,
                                           out EVSEStatusType? EVSEStatus,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                PowerDeliveryResponse = new PowerDeliveryResponse(Request,
                                                                  MessageHeader,
                                                                  ResponseCode,
                                                                  EVSEStatus);

                if (CustomPowerDeliveryResponseParser is not null)
                    PowerDeliveryResponse = CustomPowerDeliveryResponseParser(JSON,
                                                                              PowerDeliveryResponse);

                return true;

            }
            catch (Exception e)
            {
                PowerDeliveryResponse  = null;
                ErrorResponse          = "The given JSON representation of a power delivery response is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomPowerDeliveryResponseSerializer = null, CustomMessageHeaderSerializer = null, ...)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomPowerDeliveryResponseSerializer">A delegate to serialize custom power delivery responses.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        /// <param name="CustomEVSEStatusTypeSerializer">A delegate to serialize custom EVSE status chains.</param>
        /// 
        public JObject ToJSON(CustomJObjectSerializerDelegate<PowerDeliveryResponse>?  CustomPowerDeliveryResponseSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?          CustomMessageHeaderSerializer           = null,
                              CustomJObjectSerializerDelegate<EVSEStatusType>?         CustomEVSEStatusTypeSerializer          = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("messageHeader",  MessageHeader.ToJSON(CustomMessageHeaderSerializer)),
                                 new JProperty("responseCode",   ResponseCode. AsText()),

                           EVSEStatus is not null
                               ? new JProperty("evseStatus",     EVSEStatus.   ToJSON(CustomEVSEStatusTypeSerializer))
                               : null

                       );

            return CustomPowerDeliveryResponseSerializer is not null
                       ? CustomPowerDeliveryResponseSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (PowerDeliveryResponse1, PowerDeliveryResponse2)

        /// <summary>
        /// Compares two power delivery responses for equality.
        /// </summary>
        /// <param name="PowerDeliveryResponse1">A power delivery response.</param>
        /// <param name="PowerDeliveryResponse2">Another power delivery response.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (PowerDeliveryResponse? PowerDeliveryResponse1,
                                           PowerDeliveryResponse? PowerDeliveryResponse2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(PowerDeliveryResponse1, PowerDeliveryResponse2))
                return true;

            // If one is null, but not both, return false.
            if (PowerDeliveryResponse1 is null || PowerDeliveryResponse2 is null)
                return false;

            return PowerDeliveryResponse1.Equals(PowerDeliveryResponse2);

        }

        #endregion

        #region Operator != (PowerDeliveryResponse1, PowerDeliveryResponse2)

        /// <summary>
        /// Compares two power delivery responses for inequality.
        /// </summary>
        /// <param name="PowerDeliveryResponse1">A power delivery response.</param>
        /// <param name="PowerDeliveryResponse2">Another power delivery response.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (PowerDeliveryResponse? PowerDeliveryResponse1,
                                           PowerDeliveryResponse? PowerDeliveryResponse2)

            => !(PowerDeliveryResponse1 == PowerDeliveryResponse2);

        #endregion

        #endregion

        #region IEquatable<PowerDeliveryResponse> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two power delivery responses for equality.
        /// </summary>
        /// <param name="Object">A power delivery response to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is PowerDeliveryResponse powerDeliveryResponse &&
                   Equals(powerDeliveryResponse);

        #endregion

        #region Equals(PowerDeliveryResponse)

        /// <summary>
        /// Compares two power delivery responses for equality.
        /// </summary>
        /// <param name="PowerDeliveryResponse">A power delivery response to compare with.</param>
        public override Boolean Equals(PowerDeliveryResponse? PowerDeliveryResponse)

            => PowerDeliveryResponse is not null &&

             ((EVSEStatus is     null &&  PowerDeliveryResponse.EVSEStatus is     null) ||
              (EVSEStatus is not null &&  PowerDeliveryResponse.EVSEStatus is not null && EVSEStatus.Equals(PowerDeliveryResponse.EVSEStatus))) &&

               base.GenericEquals(PowerDeliveryResponse);

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

                return (EVSEStatus?.GetHashCode() ?? 0) * 3 ^
                       base.        GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => EVSEStatus?.ToString() ?? nameof(PowerDeliveryResponse);

        #endregion

    }

}
