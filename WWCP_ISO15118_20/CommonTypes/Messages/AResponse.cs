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
    /// An abstract response message.
    /// </summary>
    [Obsolete("Use AV2GResponse<TRequest, TResponse>!")]
    public abstract class AResponse : AMessage
    {

        #region Properties

        /// <summary>
        /// The message response code.
        /// </summary>
        public ResponseCodes  ResponseCode    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new abstract response message.
        /// </summary>
        public AResponse()

            : base(new MessageHeader(Session_Id.NewRandom(),
                                     DateTime.UtcNow))

        { }


        /// <summary>
        /// Create a new abstract response message.
        /// </summary>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        public AResponse(MessageHeader      MessageHeader,
                            ResponseCodes  ResponseCode)

            : base(MessageHeader)

        {

            this.ResponseCode = ResponseCode;

        }

        #endregion

    }



    /// <summary>
    /// An abstract generic ISO 15118-20 V2G response.
    /// </summary>
    public abstract class AResponse<TRequest, TResponse> : AResponse<TResponse>

        where TRequest  : class, IRequest <TRequest>
        where TResponse : class, IResponse<TResponse>

    {

        #region Properties

        /// <summary>
        /// The request leading to this response.
        /// </summary>
        public TRequest  Request    { get; }

        /// <summary>
        /// The runtime of the request.
        /// </summary>
        public TimeSpan  Runtime    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new abstract generic ISO 15118-20 V2G response.
        /// </summary>
        /// <param name="Request">The ISO 15118-20 V2G request leading to this result.</param>
        /// <param name="MessageHeader">An ISO 15118-20 V2G common message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        public AResponse(TRequest           Request,
                            MessageHeader      MessageHeader,
                            ResponseCodes  ResponseCode)

            : base(MessageHeader,
                   ResponseCode)

        {

            this.Request  = Request;
            //this.Runtime  = MessageHeader.Timestamp - Request.MessageHeader.Timestamp;

        }

        #endregion


        #region GenericEquals(AV2GResponse)

        /// <summary>
        /// Compares two abstract generic responses for equality.
        /// </summary>
        /// <param name="AV2GResponse">An abstract generic response to compare with.</param>
        public Boolean GenericEquals(AResponse<TRequest, TResponse> AV2GResponse)

            => AV2GResponse is not null &&

             ((Request is     null && AV2GResponse.Request is     null) ||
              (Request is not null && AV2GResponse.Request is not null && Request.Equals(AV2GResponse.Request))) &&

               Runtime.        Equals(AV2GResponse.Runtime) &&

               base.BaseGenericEquals(AV2GResponse);

        #endregion


        #region GetHashCode()

        /// <summary>
        /// Return the HashCode of this object.
        /// </summary>
        /// <returns>The HashCode of this object.</returns>
        public override Int32 GetHashCode()
        {
            unchecked
            {

                return (Request?.GetHashCode() ?? 0) * 5 ^
                        Runtime. GetHashCode()       * 3 ^

                        base.    GetHashCode();

            }
        }

        #endregion


    }


    /// <summary>
    /// An abstract generic ISO 15118-20 V2G response.
    /// </summary>
    public abstract class AResponse<TResponse> : AMessage,
                                                 IResponse<TResponse>

        where TResponse : class, IResponse<TResponse>

    {

        #region Properties

        /// <summary>
        /// The message response code.
        /// </summary>
        public ResponseCodes  ResponseCode    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new abstract generic ISO 15118-20 V2G response.
        /// </summary>
        /// <param name="MessageHeader">An ISO 15118-20 V2G common message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        public AResponse(MessageHeader      MessageHeader,
                            ResponseCodes  ResponseCode)

            : base(MessageHeader)

        {

            this.ResponseCode = ResponseCode;

        }

        #endregion


        #region Operator overloading

        #region Operator == (AV2GResponse1, AV2GResponse2)

        /// <summary>
        /// Compares two responses for equality.
        /// </summary>
        /// <param name="AV2GResponse1">A response.</param>
        /// <param name="AV2GResponse2">Another response.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (AResponse<TResponse>? AV2GResponse1,
                                           AResponse<TResponse>? AV2GResponse2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(AV2GResponse1, AV2GResponse2))
                return true;

            // If one is null, but not both, return false.
            if (AV2GResponse1 is null || AV2GResponse2 is null)
                return false;

            return AV2GResponse1.Equals(AV2GResponse2);

        }

        #endregion

        #region Operator != (AV2GResponse1, AV2GResponse2)

        /// <summary>
        /// Compares two responses for inequality.
        /// </summary>
        /// <param name="AV2GResponse1">A response.</param>
        /// <param name="AV2GResponse2">Another response.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (AResponse<TResponse>? AV2GResponse1,
                                           AResponse<TResponse>? AV2GResponse2)

            => !(AV2GResponse1 == AV2GResponse2);

        #endregion

        #endregion

        #region IEquatable<AV2GResponse> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two abstract generic responses for equality.
        /// </summary>
        /// <param name="Object">An abstract generic response to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is AResponse<TResponse> aResponse &&
                   Equals(aResponse);

        #endregion

        #region BaseGenericEquals(AV2GResponse)

        /// <summary>
        /// Compares two abstract generic responses for equality.
        /// </summary>
        /// <param name="AV2GResponse">An abstract generic response to compare with.</param>
        public Boolean BaseGenericEquals(AResponse<TResponse> AV2GResponse)

            => AV2GResponse is not null &&

               ResponseCode.Equals(AV2GResponse.ResponseCode) &&

               base.        Equals(AV2GResponse);

        #endregion

        #region IEquatable<AV2GResponse> Members

        /// <summary>
        /// Compares two abstract generic responses for equality.
        /// </summary>
        /// <param name="AV2GResponse">An abstract generic response to compare with.</param>
        public abstract Boolean Equals(TResponse? AV2GResponse);

        #endregion

        #endregion

        #region GetHashCode()

        /// <summary>
        /// Return the HashCode of this object.
        /// </summary>
        /// <returns>The HashCode of this object.</returns>
        public override Int32 GetHashCode()
        {
            unchecked
            {

                return ResponseCode.GetHashCode() * 3 ^
                       base.        GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   ResponseCode.ToString(),
                   ": ",
                   base.ToString()

               );

        #endregion

    }

}
