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

namespace cloud.charging.open.protocols.ISO15118_20.CommonTypes
{

    /// <summary>
    /// An abstract ISO 15118-20 V2G request.
    /// </summary>
    [Obsolete("Use AV2GRequest<TRequest>!")]
    public abstract class AV2GRequest : AV2GMessage
    {

        #region Constructor(s)

        /// <summary>
        /// Create a new abstract request message.
        /// </summary>
        public AV2GRequest()

            : base(new MessageHeader(Session_Id.NewRandom(),
                                     DateTime.UtcNow))

        { }


        /// <summary>
        /// Create a new abstract request message.
        /// </summary>
        /// <param name="MessageHeader">A common message header.</param>
        public AV2GRequest(MessageHeader MessageHeader)

            : base(MessageHeader)

        { }

        #endregion

    }


    /// <summary>
    /// An abstract generic ISO 15118-20 V2G request.
    /// </summary>
    public abstract class AV2GRequest<TRequest> : AV2GMessage,
                                                  IEquatable<TRequest>
    {

        #region Constructor(s)

        /// <summary>
        /// Create a new abstract generic ISO 15118-20 V2G request.
        /// </summary>
        /// <param name="MessageHeader">An ISO 15118-20 V2G common message header.</param>
        public AV2GRequest(MessageHeader MessageHeader)

            : base(MessageHeader)

        { }

        #endregion


        #region IEquatable<TRequest> Members

        /// <summary>
        /// Compare two abstract generic requests for equality.
        /// </summary>
        /// <param name="TRequest">Another abstract generic request.</param>
        public abstract Boolean Equals(TRequest? TRequest);

        #endregion

        #region GenericEquals(ARequest)

        /// <summary>
        /// Compare two abstract generic requests for equality.
        /// </summary>
        /// <param name="ARequest">Another abstract generic request.</param>
        public Boolean GenericEquals(AV2GRequest<TRequest>? ARequest)

            => ARequest is not null &&
                   base.Equals(ARequest);

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
