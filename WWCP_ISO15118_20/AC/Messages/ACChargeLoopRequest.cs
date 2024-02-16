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

namespace cloud.charging.open.protocols.ISO15118_20.AC
{

    /// <summary>
    /// The AC charge loop request.
    /// </summary>
    public class ACChargeLoopRequest : AChargeLoopRequest,
                                       IEquatable<ACChargeLoopRequest>
    {

        #region Properties

        /// <summary>
        /// The abstract control mode.
        /// </summary>
        public ACLReqControlMode  CLReqControlMode    { get; }

        #endregion

        #region Constructor(s)

        #region ACChargeLoopRequest(..., Dynamic_AC_CLReqControlMode)

        /// <summary>
        /// Create a new dynamic AC charge loop request.
        /// </summary>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="MeterInfoRequested">Whether meter information is requested.</param>
        /// <param name="DisplayParameters">Optional display parameters.</param>
        /// 
        /// <param name="Dynamic_AC_CLReqControlMode">A dynamic control mode.</param>
        public ACChargeLoopRequest(MessageHeader                MessageHeader,
                                   Boolean                      MeterInfoRequested,
                                   DisplayParameters?           DisplayParameters,

                                   Dynamic_AC_CLReqControlMode  Dynamic_AC_CLReqControlMode)

            : base(MessageHeader,
                   MeterInfoRequested,
                   DisplayParameters)

        {

            this.CLReqControlMode = Dynamic_AC_CLReqControlMode;

        }

        #endregion

        #region ACChargeLoopRequest(..., Scheduled_AC_CLReqControl)

        /// <summary>
        /// Create a new scheduled AC charge loop request.
        /// </summary>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="MeterInfoRequested">Whether meter information is requested.</param>
        /// <param name="DisplayParameters">Optional display parameters.</param>
        /// 
        /// <param name="Scheduled_AC_CLReqControl">A scheduled control mode.</param>
        public ACChargeLoopRequest(MessageHeader                  MessageHeader,
                                   Boolean                        MeterInfoRequested,
                                   DisplayParameters?             DisplayParameters,

                                   Scheduled_AC_CLReqControlMode  Scheduled_AC_CLReqControl)

            : base(MessageHeader,
                   MeterInfoRequested,
                   DisplayParameters)

        {

            this.CLReqControlMode = Scheduled_AC_CLReqControl;

        }

        #endregion

        #endregion


        #region Documentation

        // <xs:element name="AC_ChargeLoopReq" type="AC_ChargeLoopReqType"/>
        // 
        // <xs:complexType name="AC_ChargeLoopReqType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:ChargeLoopReqType">
        //             <xs:sequence>
        //                 <xs:element ref="v2gci_ct:CLReqControlMode"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomACChargeLoopRequestParser = null)

        /// <summary>
        /// Parse the given JSON representation of a AC charge loop request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomACChargeLoopRequestParser">An optional delegate to parse custom AC charge loop requests.</param>
        public static ACChargeLoopRequest Parse(JObject                                            JSON,
                                                CustomJObjectParserDelegate<ACChargeLoopRequest>?  CustomACChargeLoopRequestParser   = null)
        {

            if (TryParse(JSON,
                         out var acChargeLoopRequest,
                         out var errorResponse,
                         CustomACChargeLoopRequestParser))
            {
                return acChargeLoopRequest!;
            }

            throw new ArgumentException("The given JSON representation of a AC charge loop request is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out ACChargeLoopRequest, out ErrorResponse, CustomACChargeLoopRequestParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a AC charge loop request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ACChargeLoopRequest">The parsed AC charge loop request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                   JSON,
                                       out ACChargeLoopRequest?  ACChargeLoopRequest,
                                       out String?               ErrorResponse)

            => TryParse(JSON,
                        out ACChargeLoopRequest,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a AC charge loop request.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="ACChargeLoopRequest">The parsed AC charge loop request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomACChargeLoopRequestParser">An optional delegate to parse custom AC charge loop requests.</param>
        public static Boolean TryParse(JObject                                            JSON,
                                       out ACChargeLoopRequest?                           ACChargeLoopRequest,
                                       out String?                                        ErrorResponse,
                                       CustomJObjectParserDelegate<ACChargeLoopRequest>?  CustomACChargeLoopRequestParser)
        {

            try
            {

                ACChargeLoopRequest = null;

                #region MessageHeader                    [mandatory]

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

                #region MeterInfoRequested               [mandatory]

                if (!JSON.ParseMandatory("meterInfoRequested",
                                         "meter info requested",
                                         out Boolean MeterInfoRequested,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region DisplayParameters                [optional]

                if (JSON.ParseOptionalJSON("displayParameters",
                                           "display parameters",
                                           CommonTypes.DisplayParameters.TryParse,
                                           out DisplayParameters? DisplayParameters,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                #region Dynamic_AC_CLReqControlMode      [optional]

                if (JSON.ParseOptionalJSON("dynamicACCLReqControlMode",
                                           "dynamic AC control loop request control mode",
                                           AC.Dynamic_AC_CLReqControlMode.TryParse,
                                           out Dynamic_AC_CLReqControlMode? Dynamic_AC_CLReqControlMode,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region Scheduled_AC_CLReqControlMode    [optional]

                if (JSON.ParseOptionalJSON("scheduledACCLReqControlMode",
                                           "scheduled AC control loop request control mode",
                                           AC.Scheduled_AC_CLReqControlMode.TryParse,
                                           out Scheduled_AC_CLReqControlMode? Scheduled_AC_CLReqControlMode,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                if (Dynamic_AC_CLReqControlMode is not null)
                    ACChargeLoopRequest = new ACChargeLoopRequest(MessageHeader,
                                                                  MeterInfoRequested,
                                                                  DisplayParameters,
                                                                  Dynamic_AC_CLReqControlMode);

                else if (Scheduled_AC_CLReqControlMode is not null)
                    ACChargeLoopRequest = new ACChargeLoopRequest(MessageHeader,
                                                                  MeterInfoRequested,
                                                                  DisplayParameters,
                                                                  Scheduled_AC_CLReqControlMode);

                else
                {
                    ErrorResponse = "The given JSON representation of a AC charge loop request is invalid: Either Dynamic_AC_CLReqControlMode or Scheduled_AC_CLReqControlMode expected!";
                    return false;
                }

                if (CustomACChargeLoopRequestParser is not null)
                    ACChargeLoopRequest = CustomACChargeLoopRequestParser(JSON,
                                                                          ACChargeLoopRequest);

                return true;

            }
            catch (Exception e)
            {
                ACChargeLoopRequest  = null;
                ErrorResponse        = "The given JSON representation of a AC charge loop request is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomACChargeLoopRequestSerializer = null, CustomMessageHeaderSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomACChargeLoopRequestSerializer">A delegate to serialize custom AC charge loop requests.</param>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<ACChargeLoopRequest>?  CustomACChargeLoopRequestSerializer   = null,
                              CustomJObjectSerializerDelegate<MessageHeader>?         CustomMessageHeaderSerializer          = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("messageHeader",       MessageHeader.    ToJSON(CustomMessageHeaderSerializer)),
                                 new JProperty("meterInfoRequested",  MeterInfoRequested),

                           DisplayParameters is not null
                               ? new JProperty("displayParameters",   DisplayParameters.ToJSON())
                               : null

                       );

            foreach (var additionalJSON in CLReqControlMode.ToJSON())
                json?.Add(additionalJSON.Key,
                          additionalJSON.Value);


            return CustomACChargeLoopRequestSerializer is not null
                       ? CustomACChargeLoopRequestSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (ACChargeLoopRequest1, ACChargeLoopRequest2)

        /// <summary>
        /// Compares two AC charge loop requests for equality.
        /// </summary>
        /// <param name="ACChargeLoopRequest1">An AC charge loop request.</param>
        /// <param name="ACChargeLoopRequest2">Another AC charge loop request.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ACChargeLoopRequest? ACChargeLoopRequest1,
                                           ACChargeLoopRequest? ACChargeLoopRequest2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(ACChargeLoopRequest1, ACChargeLoopRequest2))
                return true;

            // If one is null, but not both, return false.
            if (ACChargeLoopRequest1 is null || ACChargeLoopRequest2 is null)
                return false;

            return ACChargeLoopRequest1.Equals(ACChargeLoopRequest2);

        }

        #endregion

        #region Operator != (ACChargeLoopRequest1, ACChargeLoopRequest2)

        /// <summary>
        /// Compares two AC charge loop requests for inequality.
        /// </summary>
        /// <param name="ACChargeLoopRequest1">An AC charge loop request.</param>
        /// <param name="ACChargeLoopRequest2">Another AC charge loop request.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ACChargeLoopRequest? ACChargeLoopRequest1,
                                           ACChargeLoopRequest? ACChargeLoopRequest2)

            => !(ACChargeLoopRequest1 == ACChargeLoopRequest2);

        #endregion

        #endregion

        #region IEquatable<ACChargeLoopRequest> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two AC charge loop requests for equality.
        /// </summary>
        /// <param name="Object">An AC charge loop request to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ACChargeLoopRequest acChargeLoopRequest &&
                   Equals(acChargeLoopRequest);

        #endregion

        #region Equals(ACChargeLoopRequest)

        /// <summary>
        /// Compares two AC charge loop requests for equality.
        /// </summary>
        /// <param name="ACChargeLoopRequest">An AC charge loop request to compare with.</param>
        public Boolean Equals(ACChargeLoopRequest? ACChargeLoopRequest)

            => ACChargeLoopRequest is not null &&

               CLReqControlMode.Equals(ACChargeLoopRequest.CLReqControlMode) &&

               base.            Equals(ACChargeLoopRequest);

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

                return CLReqControlMode.GetHashCode() * 3 ^
                       base.            GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => CLReqControlMode.ToString();

        #endregion

    }

}
