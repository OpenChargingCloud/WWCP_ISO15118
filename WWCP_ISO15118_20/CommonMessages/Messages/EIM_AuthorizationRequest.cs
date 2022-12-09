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

using cloud.charging.open.protocols.ISO15118_20.CommonTypes;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// The external identification means authorization request.
    /// </summary>
    public class EIM_AuthorizationRequest : AuthorizationRequest,
                                            IEquatable<EIM_AuthorizationRequest>
    {

        #region Constructor(s)

        /// <summary>
        /// Create a new external identification means authorization request.
        /// </summary>
        /// <param name="MessageHeader">A message header.</param>
        public EIM_AuthorizationRequest(MessageHeader MessageHeader)

            : base(MessageHeader)

        { }

        #endregion


        #region Documentation

        // <xs:complexType name = "EIM_AReqAuthorizationModeType" />

        #endregion

        #region (static) Parse   (JSON, CustomEIM_AuthorizationRequestParser = null)

        /// <summary>
        /// Parse the given JSON representation of an external identification means authorization mode request type.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomEIM_AuthorizationRequestParser">A delegate to parse custom external identification means authorization mode request types.</param>
        public static EIM_AuthorizationRequest Parse(JObject                                                     JSON,
                                                         CustomJObjectParserDelegate<EIM_AuthorizationRequest>?  CustomEIM_AuthorizationRequestParser   = null)
        {

            if (TryParse(JSON,
                         out var eim_AReqAuthorizationModeType,
                         out var errorResponse,
                         CustomEIM_AuthorizationRequestParser))
            {
                return eim_AReqAuthorizationModeType!;
            }

            throw new ArgumentException("The given JSON representation of an EIM_AuthorizationRequest is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out EIM_AuthorizationRequest, out ErrorResponse, CustomEIM_AuthorizationRequestParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of an external identification means authorization mode request type.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EIM_AuthorizationRequest">The parsed external identification means authorization mode request type.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                             JSON,
                                       out EIM_AuthorizationRequest?  EIM_AuthorizationRequest,
                                       out String?                         ErrorResponse)

            => TryParse(JSON,
                        out EIM_AuthorizationRequest,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an external identification means authorization request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EIM_AReqAuthorizationRequest">The parsed external identification means authorization request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomEIM_AuthorizationRequestParser">A delegate to parse custom external identification means authorization mode request types.</param>
        public static Boolean TryParse(JObject                                                 JSON,
                                       out EIM_AuthorizationRequest?                           EIM_AReqAuthorizationRequest,
                                       out String?                                             ErrorResponse,
                                       CustomJObjectParserDelegate<EIM_AuthorizationRequest>?  CustomEIM_AuthorizationRequestParser)
        {

            try
            {

                EIM_AReqAuthorizationRequest  = null;

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


                EIM_AReqAuthorizationRequest = new EIM_AuthorizationRequest(MessageHeader);


                if (CustomEIM_AuthorizationRequestParser is not null)
                    EIM_AReqAuthorizationRequest = CustomEIM_AuthorizationRequestParser(JSON,
                                                                                             EIM_AReqAuthorizationRequest);

                return true;

            }
            catch (Exception e)
            {
                EIM_AReqAuthorizationRequest  = null;
                ErrorResponse                 = "The given JSON representation of an EIM_AuthorizationRequest is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomEIM_AuthorizationRequestSerializer = null, CustomMessageHeaderSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomEIM_AuthorizationRequestSerializer">A delegate to serialize custom external identification means authorization mode request types.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<EIM_AuthorizationRequest>?  CustomEIM_AuthorizationRequestSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?             CustomMessageHeaderSerializer              = null)
        {

            var json = JSONObject.Create(

                           new JProperty("messageHeader",  MessageHeader.ToJSON(CustomMessageHeaderSerializer))

                       );

            return CustomEIM_AuthorizationRequestSerializer is not null
                       ? CustomEIM_AuthorizationRequestSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (EIM_AuthorizationRequest1, EIM_AuthorizationRequest2)

        /// <summary>
        /// Compares two external identification means authorization mode request types for equality.
        /// </summary>
        /// <param name="EIM_AuthorizationRequest1">An external identification means authorization mode request type.</param>
        /// <param name="EIM_AuthorizationRequest2">Another external identification means authorization mode request type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EIM_AuthorizationRequest? EIM_AuthorizationRequest1,
                                           EIM_AuthorizationRequest? EIM_AuthorizationRequest2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(EIM_AuthorizationRequest1, EIM_AuthorizationRequest2))
                return true;

            // If one is null, but not both, return false.
            if (EIM_AuthorizationRequest1 is null || EIM_AuthorizationRequest2 is null)
                return false;

            return EIM_AuthorizationRequest1.Equals(EIM_AuthorizationRequest2);

        }

        #endregion

        #region Operator != (EIM_AuthorizationRequest1, EIM_AuthorizationRequest2)

        /// <summary>
        /// Compares two external identification means authorization mode request types for inequality.
        /// </summary>
        /// <param name="EIM_AuthorizationRequest1">An external identification means authorization mode request type.</param>
        /// <param name="EIM_AuthorizationRequest2">Another external identification means authorization mode request type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EIM_AuthorizationRequest? EIM_AuthorizationRequest1,
                                           EIM_AuthorizationRequest? EIM_AuthorizationRequest2)

            => !(EIM_AuthorizationRequest1 == EIM_AuthorizationRequest2);

        #endregion

        #endregion

        #region IEquatable<EIM_AuthorizationRequest> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two external identification means authorization mode request types for equality.
        /// </summary>
        /// <param name="Object">An external identification means authorization mode request type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EIM_AuthorizationRequest eim_AReqAuthorizationModeType &&
                   Equals(eim_AReqAuthorizationModeType);

        #endregion

        #region Equals(EIM_AuthorizationRequest)

        /// <summary>
        /// Compares two external identification means authorization mode request types for equality.
        /// </summary>
        /// <param name="EIM_AuthorizationRequest">An external identification means authorization mode request type to compare with.</param>
        public Boolean Equals(EIM_AuthorizationRequest? EIM_AuthorizationRequest)

            => EIM_AuthorizationRequest is not null &&

               base.Equals(EIM_AuthorizationRequest);

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

            => nameof(EIM_AuthorizationRequest);

        #endregion

    }

}
