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
    /// The metering confirmation response.
    /// </summary>
    public class MeteringConfirmationResponse : AResponse<MeteringConfirmationRequest,
                                                          MeteringConfirmationResponse>
    {

        #region Constructor(s)

        /// <summary>
        /// Create a new metering confirmation response.
        /// </summary>
        /// <param name="Request">The metering confirmation request leading to this response.</param>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        public MeteringConfirmationResponse(MeteringConfirmationRequest  Request,
                                            MessageHeader                MessageHeader,
                                            ResponseCodes                ResponseCode)

            : base(Request,
                   MessageHeader,
                   ResponseCode)

        { }

        #endregion


        #region Documentation

        // <xs:element name="MeteringConfirmationRes" type="MeteringConfirmationResType"/>
        //
        // <xs:complexType name="MeteringConfirmationResType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:V2GResponseType"/>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (Request, JSON, CustomMeteringConfirmationResponseParser = null)

        /// <summary>
        /// Parse the given JSON representation of a metering confirmation response.
        /// </summary>
        /// <param name="Request">The metering confirmation request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomMeteringConfirmationResponseParser">A delegate to parse custom metering confirmation responses.</param>
        public static MeteringConfirmationResponse Parse(MeteringConfirmationRequest                                        Request,
                                                         JObject                                                     JSON,
                                                         CustomJObjectParserDelegate<MeteringConfirmationResponse>?  CustomMeteringConfirmationResponseParser   = null)
        {

            if (TryParse(Request,
                         JSON,
                         out var meteringConfirmationResponse,
                         out var errorResponse,
                         CustomMeteringConfirmationResponseParser))
            {
                return meteringConfirmationResponse!;
            }

            throw new ArgumentException("The given JSON representation of a metering confirmation response is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(Request, JSON, out MeteringConfirmationResponse, out ErrorResponse, CustomMeteringConfirmationResponseParser = null)

        /// <summary>
        /// Try to parse the given JSON representation of a metering confirmation response.
        /// </summary>
        /// <param name="Request">The metering confirmation request leading to this response.</param>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="MeteringConfirmationResponse">The parsed metering confirmation response.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomMeteringConfirmationResponseParser">A delegate to parse custom metering confirmation responses.</param>
        public static Boolean TryParse(MeteringConfirmationRequest                                 Request,
                                       JObject                                                     JSON,
                                       out MeteringConfirmationResponse?                           MeteringConfirmationResponse,
                                       out String?                                                 ErrorResponse,
                                       CustomJObjectParserDelegate<MeteringConfirmationResponse>?  CustomMeteringConfirmationResponseParser   = null)
        {

            ErrorResponse = null;

            try
            {

                MeteringConfirmationResponse = null;

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


                MeteringConfirmationResponse = new MeteringConfirmationResponse(Request,
                                                                                MessageHeader,
                                                                                ResponseCode);

                if (CustomMeteringConfirmationResponseParser is not null)
                    MeteringConfirmationResponse = CustomMeteringConfirmationResponseParser(JSON,
                                                                                            MeteringConfirmationResponse);

                return true;

            }
            catch (Exception e)
            {
                MeteringConfirmationResponse  = null;
                ErrorResponse                 = "The given JSON representation of a metering confirmation response is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomMeteringConfirmationResponseSerializer = null, CustomMessageHeaderSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomMeteringConfirmationResponseSerializer">A delegate to serialize custom metering confirmation responses.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<MeteringConfirmationResponse>?  CustomMeteringConfirmationResponseSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?                 CustomMessageHeaderSerializer                  = null)
        {

            var json = JSONObject.Create(

                           new JProperty("messageHeader",  MessageHeader.ToJSON(CustomMessageHeaderSerializer)),
                           new JProperty("responseCode",   ResponseCode. AsText())

                       );

            return CustomMeteringConfirmationResponseSerializer is not null
                       ? CustomMeteringConfirmationResponseSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (MeteringConfirmationResponse1, MeteringConfirmationResponse2)

        /// <summary>
        /// Compares two metering confirmation responses for equality.
        /// </summary>
        /// <param name="MeteringConfirmationResponse1">A metering confirmation response.</param>
        /// <param name="MeteringConfirmationResponse2">Another metering confirmation response.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (MeteringConfirmationResponse? MeteringConfirmationResponse1,
                                           MeteringConfirmationResponse? MeteringConfirmationResponse2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(MeteringConfirmationResponse1, MeteringConfirmationResponse2))
                return true;

            // If one is null, but not both, return false.
            if (MeteringConfirmationResponse1 is null || MeteringConfirmationResponse2 is null)
                return false;

            return MeteringConfirmationResponse1.Equals(MeteringConfirmationResponse2);

        }

        #endregion

        #region Operator != (MeteringConfirmationResponse1, MeteringConfirmationResponse2)

        /// <summary>
        /// Compares two metering confirmation responses for inequality.
        /// </summary>
        /// <param name="MeteringConfirmationResponse1">A metering confirmation response.</param>
        /// <param name="MeteringConfirmationResponse2">Another metering confirmation response.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (MeteringConfirmationResponse? MeteringConfirmationResponse1,
                                           MeteringConfirmationResponse? MeteringConfirmationResponse2)

            => !(MeteringConfirmationResponse1 == MeteringConfirmationResponse2);

        #endregion

        #endregion

        #region IEquatable<MeteringConfirmationResponse> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two metering confirmation responses for equality.
        /// </summary>
        /// <param name="Object">A metering confirmation response to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is MeteringConfirmationResponse meteringConfirmationResponse &&
                   Equals(meteringConfirmationResponse);

        #endregion

        #region Equals(MeteringConfirmationResponse)

        /// <summary>
        /// Compares two metering confirmation responses for equality.
        /// </summary>
        /// <param name="MeteringConfirmationResponse">A metering confirmation response to compare with.</param>
        public override Boolean Equals(MeteringConfirmationResponse? MeteringConfirmationResponse)

            => MeteringConfirmationResponse is not null &&

               base.GenericEquals(MeteringConfirmationResponse);

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the HashCode of this object.
        /// </summary>
        /// <returns>The HashCode of this object.</returns>
        public override Int32 GetHashCode()

            => base.GetHashCode();

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => nameof(MeteringConfirmationResponse);

        #endregion

    }

}
