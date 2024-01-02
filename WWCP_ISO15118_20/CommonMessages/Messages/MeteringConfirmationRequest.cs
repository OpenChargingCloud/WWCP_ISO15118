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
    /// The metering confirmation request.
    /// </summary>
    public class MeteringConfirmationRequest : ARequest<MeteringConfirmationRequest>
    {

        #region Properties

        /// <summary>
        /// Signed metering data.
        /// </summary>
        [Mandatory]
        public SignedMeteringData  SignedMeteringData    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new metering confirmation request.
        /// </summary>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="SignedMeteringData">Signed metering data.</param>
        public MeteringConfirmationRequest(MessageHeader       MessageHeader,
                                           SignedMeteringData  SignedMeteringData)

            : base(MessageHeader)

        {

            this.SignedMeteringData = SignedMeteringData;

        }

        #endregion


        #region Documentation

        // <xs:element name="MeteringConfirmationReq" type="MeteringConfirmationReqType"/>
        //
        // <xs:complexType name="MeteringConfirmationReqType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:V2GRequestType">
        //             <xs:sequence>
        //                 <xs:element name="SignedMeteringData" type="SignedMeteringDataType"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomMeteringConfirmationRequestParser = null)

        /// <summary>
        /// Parse the given JSON representation of a metering confirmation request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomMeteringConfirmationRequestParser">A delegate to parse custom metering confirmation requests.</param>
        public static MeteringConfirmationRequest Parse(JObject                                                    JSON,
                                                        CustomJObjectParserDelegate<MeteringConfirmationRequest>?  CustomMeteringConfirmationRequestParser   = null)
        {

            if (TryParse(JSON,
                         out var meteringConfirmationRequest,
                         out var errorResponse,
                         CustomMeteringConfirmationRequestParser))
            {
                return meteringConfirmationRequest!;
            }

            throw new ArgumentException("The given JSON representation of a metering confirmation request is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out MeteringConfirmationRequest, out ErrorResponse, CustomMeteringConfirmationRequestParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a metering confirmation request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="MeteringConfirmationRequest">The parsed metering confirmation request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                           JSON,
                                       out MeteringConfirmationRequest?  MeteringConfirmationRequest,
                                       out String?                       ErrorResponse)

            => TryParse(JSON,
                        out MeteringConfirmationRequest,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a metering confirmation request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="MeteringConfirmationRequest">The parsed metering confirmation request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomMeteringConfirmationRequestParser">A delegate to parse custom metering confirmation requests.</param>
        public static Boolean TryParse(JObject                                                    JSON,
                                       out MeteringConfirmationRequest?                           MeteringConfirmationRequest,
                                       out String?                                                ErrorResponse,
                                       CustomJObjectParserDelegate<MeteringConfirmationRequest>?  CustomMeteringConfirmationRequestParser)
        {

            try
            {

                MeteringConfirmationRequest = null;

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

                #region SignedMeteringData    [mandatory]

                if (!JSON.ParseMandatoryJSON("signedMeteringData",
                                             "signed metering data",
                                             CommonMessages.SignedMeteringData.TryParse,
                                             out SignedMeteringData? SignedMeteringData,
                                             out ErrorResponse))
                {
                    return false;
                }

                if (SignedMeteringData is null)
                    return false;

                #endregion


                MeteringConfirmationRequest = new MeteringConfirmationRequest(MessageHeader,
                                                                              SignedMeteringData);

                if (CustomMeteringConfirmationRequestParser is not null)
                    MeteringConfirmationRequest = CustomMeteringConfirmationRequestParser(JSON,
                                                                                          MeteringConfirmationRequest);

                return true;

            }
            catch (Exception e)
            {
                MeteringConfirmationRequest  = null;
                ErrorResponse                = "The given JSON representation of a metering confirmation request is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomMeteringConfirmationRequestSerializer = null, CustomMessageHeaderSerializer = null, ...)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomMeteringConfirmationRequestSerializer">A delegate to serialize custom metering confirmation requests.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        /// <param name="CustomSignedMeteringDataSerializer">A delegate to serialize custom signed metering datas.</param>
        /// <param name="CustomMeterInfoSerializer">A delegate to serialize custom energy meter information.</param>
        /// <param name="CustomReceiptSerializer">A delegate to serialize custom receipts.</param>
        /// <param name="CustomDetailedCostSerializer">A delegate to serialize custom detailed costs.</param>
        /// <param name="CustomRationalNumberSerializer">A delegate to serialize custom rational numbers.</param>
        /// <param name="CustomDetailedTaxSerializer">A delegate to serialize custom detailed taxes.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<MeteringConfirmationRequest>?  CustomMeteringConfirmationRequestSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?                CustomMessageHeaderSerializer                 = null,
                              CustomJObjectSerializerDelegate<SignedMeteringData>?           CustomSignedMeteringDataSerializer            = null,
                              CustomJObjectSerializerDelegate<MeterInfo>?                    CustomMeterInfoSerializer                     = null,
                              CustomJObjectSerializerDelegate<Receipt>?                      CustomReceiptSerializer                       = null,
                              CustomJObjectSerializerDelegate<DetailedCost>?                 CustomDetailedCostSerializer                  = null,
                              CustomJObjectSerializerDelegate<RationalNumber>?               CustomRationalNumberSerializer                = null,
                              CustomJObjectSerializerDelegate<DetailedTax>?                  CustomDetailedTaxSerializer                   = null)
        {

            var json = JSONObject.Create(

                           new JProperty("messageHeader",       MessageHeader.     ToJSON(CustomMessageHeaderSerializer)),

                           new JProperty("signedMeteringData",  SignedMeteringData.ToJSON(CustomSignedMeteringDataSerializer,
                                                                                          CustomMeterInfoSerializer,
                                                                                          CustomReceiptSerializer,
                                                                                          CustomDetailedCostSerializer,
                                                                                          CustomRationalNumberSerializer,
                                                                                          CustomDetailedTaxSerializer))

                       );

            return CustomMeteringConfirmationRequestSerializer is not null
                       ? CustomMeteringConfirmationRequestSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (MeteringConfirmationRequest1, MeteringConfirmationRequest2)

        /// <summary>
        /// Compares two metering confirmation requests for equality.
        /// </summary>
        /// <param name="MeteringConfirmationRequest1">A metering confirmation request.</param>
        /// <param name="MeteringConfirmationRequest2">Another metering confirmation request.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (MeteringConfirmationRequest? MeteringConfirmationRequest1,
                                           MeteringConfirmationRequest? MeteringConfirmationRequest2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(MeteringConfirmationRequest1, MeteringConfirmationRequest2))
                return true;

            // If one is null, but not both, return false.
            if (MeteringConfirmationRequest1 is null || MeteringConfirmationRequest2 is null)
                return false;

            return MeteringConfirmationRequest1.Equals(MeteringConfirmationRequest2);

        }

        #endregion

        #region Operator != (MeteringConfirmationRequest1, MeteringConfirmationRequest2)

        /// <summary>
        /// Compares two metering confirmation requests for inequality.
        /// </summary>
        /// <param name="MeteringConfirmationRequest1">A metering confirmation request.</param>
        /// <param name="MeteringConfirmationRequest2">Another metering confirmation request.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (MeteringConfirmationRequest? MeteringConfirmationRequest1,
                                           MeteringConfirmationRequest? MeteringConfirmationRequest2)

            => !(MeteringConfirmationRequest1 == MeteringConfirmationRequest2);

        #endregion

        #endregion

        #region IEquatable<MeteringConfirmationRequest> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two metering confirmation requests for equality.
        /// </summary>
        /// <param name="Object">A metering confirmation request to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is MeteringConfirmationRequest meteringConfirmationRequest &&
                   Equals(meteringConfirmationRequest);

        #endregion

        #region Equals(MeteringConfirmationRequest)

        /// <summary>
        /// Compares two metering confirmation requests for equality.
        /// </summary>
        /// <param name="MeteringConfirmationRequest">A metering confirmation request to compare with.</param>
        public override Boolean Equals(MeteringConfirmationRequest? MeteringConfirmationRequest)

            => MeteringConfirmationRequest is not null &&

               SignedMeteringData.Equals(MeteringConfirmationRequest.SignedMeteringData) &&

               base.              Equals(MeteringConfirmationRequest);

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

                return SignedMeteringData.GetHashCode() * 3 ^
                       base.              GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => SignedMeteringData.ToString();

        #endregion

    }

}
