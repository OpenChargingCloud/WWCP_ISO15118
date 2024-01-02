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

namespace cloud.charging.open.protocols.ISO15118_20.CommonTypes
{

    /// <summary>
    /// The common message header.
    /// </summary>
    public class MessageHeader : IEquatable<MessageHeader>
    {

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        [Mandatory]
        public Session_Id   SessionId    { get; }

        /// <summary>
        /// TimeStamp: An UInt64 in the specification!
        /// </summary>
        [Mandatory]
        public DateTime     Timestamp    { get; }

        /// <summary>
        /// 
        /// </summary>
        [Optional]
        public Object?      Signature    { get; }

        /// <summary>
        /// 
        /// </summary>
        [Optional]
        public Message_Id?  MessageId    { get; }

        /// <summary>
        /// 
        /// </summary>
        [Optional]
        public Device_Id?   DeviceId     { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new common message header.
        /// </summary>
        /// <param name="SessionID"></param>
        /// <param name="Timestamp"></param>
        /// <param name="Signature"></param>
        /// <param name="MessageId"></param>
        /// <param name="DeviceId"></param>
        public MessageHeader(Session_Id   SessionID,
                             DateTime     Timestamp,
                             Object?      Signature   = null,
                             Message_Id?  MessageId   = null,
                             Device_Id?   DeviceId    = null)
        {

            this.SessionId  = SessionID;
            this.Timestamp  = Timestamp;
            this.Signature  = Signature;
            this.MessageId  = MessageId;
            this.DeviceId   = DeviceId;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="MessageHeaderType">
        //     <xs:sequence>
        //         <xs:element name="SessionID" type="sessionIDType"/>
        //         <xs:element name="TimeStamp" type="xs:unsignedLong"/>
        //         <xs:element ref="xmlsig:Signature" minOccurs="0"/>
        //     </xs:sequence>
        // </xs:complexType>


        // <ns:AuthorizationReq xmlns:v2gci_ct="urn:iso:std:iso:15118:-20:CommonTypes"
        //                      xmlns:xmlsig="http://www.w3.org/2000/09/xmldsig#"
        //                      xmlns:ns="urn:iso:std:iso:15118:-20:CommonMessages"
        //                      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
        //                      xsi:schemaLocation="urn:iso:std:iso:15118:-20:CommonMessages file://V2G_CI_CommonMessages.xsd">
        //
        //     <v2gci_ct:Header>
        //
        //         <v2gci_ct:SessionID>212D322D33212D32</v2gci_ct:SessionID>
        //         <v2gci_ct:TimeStamp>4422</v2gci_ct:TimeStamp>
        //
        //         <xmlsig:Signature>
        //             <xmlsig:SignedInfo>
        //                 <xmlsig:CanonicalizationMethod Algorithm="https://www.liquid-technologies.com" />
        //                 <xmlsig:SignatureMethod Algorithm="https://www.liquid-technologies.com" />
        //                 <xmlsig:Reference>
        //                     <xmlsig:DigestMethod Algorithm="https://www.liquid-technologies.com" />
        //                     <xmlsig:DigestValue>YTM0NZomIzI2OTsmIzM0NTueYQ==</xmlsig:DigestValue>
        //                 </xmlsig:Reference>
        //             </xmlsig:SignedInfo>
        //             <xmlsig:SignatureValue>YTM0NZomIzI2OTsmIzM0NTueYQ==</xmlsig:SignatureValue>
        //         </xmlsig:Signature>
        //
        //     </v2gci_ct:Header>
        //
        // </ns:AuthorizationReq>

        #endregion

        #region (static) Parse   (JSON, CustomMessageHeaderParser = null)

        /// <summary>
        /// Parse the given JSON representation of a message header.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomMessageHeaderParser">A delegate to parse custom message headers.</param>
        public static MessageHeader Parse(JObject                                      JSON,
                                          CustomJObjectParserDelegate<MessageHeader>?  CustomMessageHeaderParser   = null)
        {

            if (TryParse(JSON,
                         out var messageHeader,
                         out var errorResponse,
                         CustomMessageHeaderParser))
            {
                return messageHeader!;
            }

            throw new ArgumentException("The given JSON representation of a message header is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out MessageHeader, out ErrorResponse, CustomMessageHeaderParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a message header.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="MessageHeader">The parsed message header.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject             JSON,
                                       out MessageHeader?  MessageHeader,
                                       out String?         ErrorResponse)

            => TryParse(JSON,
                        out MessageHeader,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a message header.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="MessageHeader">The parsed message header request.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomMessageHeaderParser">A delegate to parse custom message headers.</param>
        public static Boolean TryParse(JObject                                      JSON,
                                       out MessageHeader?                           MessageHeader,
                                       out String?                                  ErrorResponse,
                                       CustomJObjectParserDelegate<MessageHeader>?  CustomMessageHeaderParser)
        {

            try
            {

                MessageHeader = null;

                #region SessionId    [mandatory]

                if (!JSON.ParseMandatory("sessionId",
                                         "session identification",
                                         Session_Id.TryParse,
                                         out Session_Id SessionId,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Timestamp    [mandatory]

                if (!JSON.ParseMandatory("timestamp",
                                         "message timestamp",
                                         out DateTime Timestamp,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                //ToDo: Signature?

                #region MessageId    [optional]

                if (JSON.ParseOptional("messageId",
                                       "message identification",
                                       Message_Id.TryParse,
                                       out Message_Id MessageId,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region DeviceId     [optional]

                if (JSON.ParseOptional("deviceId",
                                       "device identification",
                                       Device_Id.TryParse,
                                       out Device_Id DeviceId,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                MessageHeader = new MessageHeader(SessionId,
                                                  Timestamp,
                                                  null,
                                                  MessageId,
                                                  DeviceId);

                if (CustomMessageHeaderParser is not null)
                    MessageHeader = CustomMessageHeaderParser(JSON,
                                                              MessageHeader);

                return true;

            }
            catch (Exception e)
            {
                MessageHeader  = null;
                ErrorResponse  = "The given JSON representation of a message header is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomMessageHeaderSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomMessageHeaderSerializer">A delegate to serialize custom message headers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<MessageHeader>? CustomMessageHeaderSerializer = null)
        {

            var json = JSONObject.Create(

                           new JProperty("sessionId",  SessionId.ToString()),
                           new JProperty("timestamp",  Timestamp.ToIso8601())

                       );

            return CustomMessageHeaderSerializer is not null
                       ? CustomMessageHeaderSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (MessageHeader1, MessageHeader2)

        /// <summary>
        /// Compares two message headers for equality.
        /// </summary>
        /// <param name="MessageHeader1">A message header.</param>
        /// <param name="MessageHeader2">Another message header.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (MessageHeader? MessageHeader1,
                                           MessageHeader? MessageHeader2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(MessageHeader1, MessageHeader2))
                return true;

            // If one is null, but not both, return false.
            if (MessageHeader1 is null || MessageHeader2 is null)
                return false;

            return MessageHeader1.Equals(MessageHeader2);

        }

        #endregion

        #region Operator != (MessageHeader1, MessageHeader2)

        /// <summary>
        /// Compares two message headers for inequality.
        /// </summary>
        /// <param name="MessageHeader1">A message header.</param>
        /// <param name="MessageHeader2">Another message header.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (MessageHeader? MessageHeader1,
                                           MessageHeader? MessageHeader2)

            => !(MessageHeader1 == MessageHeader2);

        #endregion

        #endregion

        #region IEquatable<MessageHeader> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two message headers for equality.
        /// </summary>
        /// <param name="Object">A message header to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is MessageHeader messageHeader &&
                   Equals(messageHeader);

        #endregion

        #region Equals(MessageHeader)

        /// <summary>
        /// Compares two message headers for equality.
        /// </summary>
        /// <param name="MessageHeader">A message header to compare with.</param>
        public Boolean Equals(MessageHeader? MessageHeader)

            => MessageHeader is not null &&

               SessionId.Equals(MessageHeader.SessionId) &&
               Timestamp.Equals(MessageHeader.Timestamp);

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

                return SessionId.GetHashCode() * 7 ^
                       Timestamp.GetHashCode() * 5 ^

                       base.     GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   SessionId,
                   " / ",
                   Timestamp.ToIso8601()

               );

        #endregion

    }

}
