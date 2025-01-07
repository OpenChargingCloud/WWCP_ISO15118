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

namespace cloud.charging.open.protocols.ISO15118_20.CommonTypes
{

    /// <summary>
    /// An abstract ISO 15118-20 V2G request.
    /// </summary>
    [Obsolete("Use ARequest<TRequest>!")]
    public abstract class ARequest : AMessage,
                                     IEquatable<ARequest>
    {

        #region Constructor(s)

        /// <summary>
        /// Create a new abstract request message.
        /// </summary>
        public ARequest()

            : base(new MessageHeader(Session_Id.NewRandom(),
                                     DateTime.UtcNow))

        { }


        /// <summary>
        /// Create a new abstract request message.
        /// </summary>
        /// <param name="MessageHeader">A common message header.</param>
        public ARequest(MessageHeader MessageHeader)

            : base(MessageHeader)

        { }

        #endregion


        #region IEquatable<ARequest> Members

        #region Equals(Object)

        /// <summary>
        /// Compare two abstract V2G requests for equality.
        /// </summary>
        /// <param name="Object">Another abstract V2G request.</param>
        public override Boolean Equals(Object? Object)

            => Object is ARequest aRequest &&
                   Equals(aRequest);

        #endregion

        #region Equals(ARequest)

        /// <summary>
        /// Compare two abstract V2G requests for equality.
        /// </summary>
        /// <param name="ARequest">Another abstract V2G request.</param>
        public virtual Boolean Equals(ARequest? ARequest)

            => ARequest is not null &&

               base.Equals(ARequest);

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

                return base.GetHashCode();

            }
        }

        #endregion

    }


    /// <summary>
    /// An abstract generic ISO 15118-20 V2G request.
    /// </summary>
    public abstract class ARequest<TRequest> : AMessage,
                                               IRequest<TRequest>

        where TRequest: IRequest<TRequest>

    {

        #region Constructor(s)

        /// <summary>
        /// Create a new abstract generic ISO 15118-20 V2G request.
        /// </summary>
        /// <param name="MessageHeader">An ISO 15118-20 V2G common message header.</param>
        public ARequest(MessageHeader MessageHeader)

            : base(MessageHeader)

        { }

        #endregion


        #region Documentation

        // <xs:complexType name = "V2GRequestType" abstract="true">
        //     <xs:complexContent>
        //         <xs:extension base="V2GMessageType"/>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

        #region IEquatable<ARequest<TRequest>> Members

        #region Equals(Object)

        /// <summary>
        /// Compare two abstract generic V2G requests for equality.
        /// </summary>
        /// <param name="Object">Another abstract generic V2G request.</param>
        public override Boolean Equals(Object? Object)

            => Object is ARequest<TRequest> aRequest &&
                   Equals(aRequest);

        #endregion

        #region Equals(ARequest<TRequest>)

        /// <summary>
        /// Compare two abstract generic V2G requests for equality.
        /// </summary>
        /// <param name="ARequest">Another abstract generic V2G request.</param>
        public virtual Boolean Equals(ARequest<TRequest>? ARequest)

            => ARequest is not null &&

               base.Equals(ARequest);

        #endregion

        #region Equals(ARequest)

        /// <summary>
        /// Compare two abstract generic V2G requests for equality.
        /// </summary>
        /// <param name="ARequest">Another abstract generic V2G request.</param>
        public virtual Boolean Equals(TRequest? ARequest)

            => ARequest is not null &&

               base.Equals(ARequest);

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

                return base.GetHashCode();

            }
        }

        #endregion

    }

}
