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

using cloud.charging.open.protocols.ISO15118_20.CommonMessages;

namespace cloud.charging.open.protocols.ISO15118_20.CommonTypes
{

    /// <summary>
    /// An abstract ISO 15118-20 V2G request.
    /// </summary>
    [Obsolete("Use AV2GRequest<TRequest>!")]
    public abstract class AV2GRequest : AV2GMessage,
                                        IEquatable<AV2GRequest>
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


        #region IEquatable<AV2GRequest> Members

        #region Equals(Object)

        /// <summary>
        /// Compare two abstract V2G requests for equality.
        /// </summary>
        /// <param name="Object">Another abstract V2G request.</param>
        public override Boolean Equals(Object? Object)

            => Object is AV2GRequest aV2GRequest &&
                   Equals(aV2GRequest);

        #endregion

        #region Equals(AV2GRequest)

        /// <summary>
        /// Compare two abstract V2G requests for equality.
        /// </summary>
        /// <param name="AV2GRequest">Another abstract V2G request.</param>
        public virtual Boolean Equals(AV2GRequest? AV2GRequest)

            => AV2GRequest is not null &&

               base.Equals(AV2GRequest);

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
    public abstract class AV2GRequest<TV2GRequest> : AV2GMessage,
                                                     IEquatable<AV2GRequest<TV2GRequest>>
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


        #region IEquatable<AV2GRequest<TV2GRequest>> Members

        #region Equals(Object)

        /// <summary>
        /// Compare two abstract generic V2G requests for equality.
        /// </summary>
        /// <param name="Object">Another abstract generic V2G request.</param>
        public override Boolean Equals(Object? Object)

            => Object is AV2GRequest<TV2GRequest> aV2GRequest &&
                   Equals(aV2GRequest);

        #endregion

        #region Equals(AV2GRequest)

        /// <summary>
        /// Compare two abstract generic V2G requests for equality.
        /// </summary>
        /// <param name="AV2GRequest">Another abstract generic V2G request.</param>
        public virtual Boolean Equals(AV2GRequest<TV2GRequest>? AV2GRequest)

            => AV2GRequest is not null &&

               base.Equals(AV2GRequest);

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
