/*
 * Copyright (c) 2021-2023 GraphDefined GmbH
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

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonTypes
{

    /// <summary>
    /// An abstract ISO 15118-20 V2G message.
    /// </summary>
    public abstract class AMessage : IEquatable<AMessage>
    {

        #region Properties

        /// <summary>
        /// The ISO 15118-20 V2G common message header.
        /// </summary>
        [Mandatory]
        public MessageHeader  MessageHeader    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new abstract ISO 15118-20 V2G message.
        /// </summary>
        /// <param name="MessageHeader">An ISO 15118-20 V2G common message header.</param>
        public AMessage(MessageHeader MessageHeader)
        {

            this.MessageHeader = MessageHeader;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="V2GMessageType" abstract="true">
        //     <xs:sequence>
        //         <xs:element name="Header" type="MessageHeaderType"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region IEquatable<AMessage> Members

        #region Equals(Object)

        /// <summary>
        /// Compare two abstract V2G messages for equality.
        /// </summary>
        /// <param name="Object">Another abstract V2G message.</param>
        public override Boolean Equals(Object? Object)

            => Object is AMessage aMessage &&
                   Equals(aMessage);

        #endregion

        #region Equals(AMessage)

        /// <summary>
        /// Compare two abstract V2G messages for equality.
        /// </summary>
        /// <param name="AMessage">Another abstract V2G message.</param>
        public virtual Boolean Equals(AMessage? AMessage)

            => AMessage is not null &&

               MessageHeader.Equals(AMessage.MessageHeader);

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

                return MessageHeader.GetHashCode();

            }
        }

        #endregion

    }

}
