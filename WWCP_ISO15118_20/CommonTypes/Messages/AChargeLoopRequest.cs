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

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonTypes
{

    /// <summary>
    /// The abstract charge loop request.
    /// </summary>
    public abstract class AChargeLoopRequest : ARequest<AChargeLoopRequest>,
                                               IEquatable<AChargeLoopRequest>
    {

        #region Properties

        /// <summary>
        /// Whether meter information is requested.
        /// </summary>
        [Mandatory]
        public Boolean             MeterInfoRequested    { get; }

        /// <summary>
        /// The optional display parameters.
        /// </summary>
        [Optional]
        public DisplayParameters?  DisplayParameters     { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new abstract charge loop request.
        /// </summary>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="MeterInfoRequested">Whether meter information is requested.</param>
        /// <param name="DisplayParameters">Optional display parameters.</param>
        public AChargeLoopRequest(MessageHeader       MessageHeader,
                                  Boolean             MeterInfoRequested,
                                  DisplayParameters?  DisplayParameters)

            : base(MessageHeader)

        {

            this.MeterInfoRequested  = MeterInfoRequested;
            this.DisplayParameters   = DisplayParameters;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="ChargeLoopReqType" abstract="true">
        //     <xs:complexContent>
        //         <xs:extension base="V2GRequestType">
        //             <xs:sequence>
        //                 <xs:element name="DisplayParameters"  type="DisplayParametersType" minOccurs="0"/>
        //                 <xs:element name="MeterInfoRequested" type="xs:boolean"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

        #region Operator overloading

        #region Operator == (AChargeLoopRequest1, AChargeLoopRequest2)

        /// <summary>
        /// Compares two abstract charge loop requests for equality.
        /// </summary>
        /// <param name="AChargeLoopRequest1">An abstract charge loop request.</param>
        /// <param name="AChargeLoopRequest2">Another abstract charge loop request.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (AChargeLoopRequest? AChargeLoopRequest1,
                                           AChargeLoopRequest? AChargeLoopRequest2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(AChargeLoopRequest1, AChargeLoopRequest2))
                return true;

            // If one is null, but not both, return false.
            if (AChargeLoopRequest1 is null || AChargeLoopRequest2 is null)
                return false;

            return AChargeLoopRequest1.Equals(AChargeLoopRequest2);

        }

        #endregion

        #region Operator != (AChargeLoopRequest1, AChargeLoopRequest2)

        /// <summary>
        /// Compares two abstract charge loop requests for inequality.
        /// </summary>
        /// <param name="AChargeLoopRequest1">An abstract charge loop request.</param>
        /// <param name="AChargeLoopRequest2">Another abstract charge loop request.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (AChargeLoopRequest? AChargeLoopRequest1,
                                           AChargeLoopRequest? AChargeLoopRequest2)

            => !(AChargeLoopRequest1 == AChargeLoopRequest2);

        #endregion

        #endregion

        #region IEquatable<AChargeLoopRequest> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two abstract charge loop requests for equality.
        /// </summary>
        /// <param name="Object">An abstract charge loop request to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is AChargeLoopRequest aChargeLoopRequest &&
                   Equals(aChargeLoopRequest);

        #endregion

        #region Equals(AChargeLoopRequest)

        /// <summary>
        /// Compares two abstract charge loop requests for equality.
        /// </summary>
        /// <param name="AChargeLoopRequest">An abstract charge loop request to compare with.</param>
        public override Boolean Equals(AChargeLoopRequest? AChargeLoopRequest)

            => AChargeLoopRequest is not null &&

               MeterInfoRequested.Equals(AChargeLoopRequest.MeterInfoRequested) &&

             ((DisplayParameters is     null && AChargeLoopRequest.DisplayParameters is     null) ||
              (DisplayParameters is not null && AChargeLoopRequest.DisplayParameters is not null && DisplayParameters.Equals(AChargeLoopRequest.DisplayParameters))) &&

               base.              Equals(AChargeLoopRequest);

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

                return MeterInfoRequested.GetHashCode()       * 5 ^
                      (DisplayParameters?.GetHashCode() ?? 0) * 3 ^

                       base.              GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   MeterInfoRequested
                       ? "meter information requested"
                       : "no meter information requested",

                   DisplayParameters is not null
                       ? ", " + DisplayParameters.ToString()
                       : ""

               );

        #endregion

    }

}
