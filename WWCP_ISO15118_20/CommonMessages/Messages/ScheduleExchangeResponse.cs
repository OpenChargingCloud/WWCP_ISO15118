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

#region Usings

using org.GraphDefined.Vanaheimr.Illias;

using cloud.charging.open.protocols.ISO15118_20.CommonTypes;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// The abstract schedule exchange response.
    /// </summary>
    public abstract class ScheduleExchangeResponse : AResponse<ScheduleExchangeRequest,
                                                               ScheduleExchangeResponse>
    {

        #region Properties

        /// <summary>
        /// The EVSE processing type.
        /// </summary>
        [Mandatory]
        public ProcessingTypes  EVSEProcessing    { get; }

        /// <summary>
        /// The optional indication whether to pause charging.
        /// </summary>
        [Optional]
        public Boolean?         GoToPause         { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new abstract schedule exchange response.
        /// </summary>
        /// <param name="Request">The schedule exchange request leading to this response.</param>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="ResponseCode">A message response code.</param>
        /// <param name="EVSEProcessing">An EVSE processing type.</param>
        /// <param name="GoToPause">An optional indication whether to pause charging.</param>
        public ScheduleExchangeResponse(ScheduleExchangeRequest  Request,
                                        MessageHeader            MessageHeader,
                                        ResponseCodes            ResponseCode,

                                        ProcessingTypes          EVSEProcessing,
                                        Boolean?                 GoToPause   = null)

            : base(Request,
                   MessageHeader,
                   ResponseCode)

        {

            this.EVSEProcessing  = EVSEProcessing;
            this.GoToPause       = GoToPause;

        }

        #endregion


        #region Documentation

        // <xs:element name="ScheduleExchangeRes" type="ScheduleExchangeResType"/>
        //
        // <xs:complexType name="ScheduleExchangeResType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:V2GResponseType">
        //             <xs:sequence>
        //
        //                 <xs:element name="EVSEProcessing" type="v2gci_ct:processingType"/>
        //                 <xs:element name="GoToPause"      type="xs:boolean" minOccurs="0"/>
        //
        //                 <xs:choice>
        //                     <xs:element name="Dynamic_SEResControlMode"   type="Dynamic_SEResControlModeType"/>
        //                     <xs:element name="Scheduled_SEResControlMode" type="Scheduled_SEResControlModeType"/>
        //                 </xs:choice>
        //
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion


        #region Operator overloading

        #region Operator == (ScheduleExchangeResponse1, ScheduleExchangeResponse2)

        /// <summary>
        /// Compares two schedule exchange responses for equality.
        /// </summary>
        /// <param name="ScheduleExchangeResponse1">A schedule exchange response.</param>
        /// <param name="ScheduleExchangeResponse2">Another schedule exchange response.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ScheduleExchangeResponse? ScheduleExchangeResponse1,
                                           ScheduleExchangeResponse? ScheduleExchangeResponse2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(ScheduleExchangeResponse1, ScheduleExchangeResponse2))
                return true;

            // If one is null, but not both, return false.
            if (ScheduleExchangeResponse1 is null || ScheduleExchangeResponse2 is null)
                return false;

            return ScheduleExchangeResponse1.Equals(ScheduleExchangeResponse2);

        }

        #endregion

        #region Operator != (ScheduleExchangeResponse1, ScheduleExchangeResponse2)

        /// <summary>
        /// Compares two schedule exchange responses for inequality.
        /// </summary>
        /// <param name="ScheduleExchangeResponse1">A schedule exchange response.</param>
        /// <param name="ScheduleExchangeResponse2">Another schedule exchange response.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ScheduleExchangeResponse? ScheduleExchangeResponse1,
                                           ScheduleExchangeResponse? ScheduleExchangeResponse2)

            => !(ScheduleExchangeResponse1 == ScheduleExchangeResponse2);

        #endregion

        #endregion

        #region IEquatable<ScheduleExchangeResponse> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two schedule exchange responses for equality.
        /// </summary>
        /// <param name="Object">A schedule exchange response to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ScheduleExchangeResponse scheduledScheduleExchangeResponse &&
                   Equals(scheduledScheduleExchangeResponse);

        #endregion

        #region Equals(ScheduleExchangeResponse)

        /// <summary>
        /// Compares two schedule exchange responses for equality.
        /// </summary>
        /// <param name="ScheduleExchangeResponse">A schedule exchange response to compare with.</param>
        public override Boolean Equals(ScheduleExchangeResponse? ScheduleExchangeResponse)

            => ScheduleExchangeResponse is not null &&

               EVSEProcessing.Equals(ScheduleExchangeResponse.EVSEProcessing) &&

            ((!GoToPause.HasValue && !ScheduleExchangeResponse.GoToPause.HasValue) ||
              (GoToPause.HasValue &&  ScheduleExchangeResponse.GoToPause.HasValue && GoToPause.Value.Equals(ScheduleExchangeResponse.GoToPause.Value))) &&

               base.GenericEquals(ScheduleExchangeResponse);

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the HashCode of this object.
        /// </summary>
        public override Int32 GetHashCode()
        {
            unchecked
            {

                return EVSEProcessing.GetHashCode()       * 5 ^
                      (GoToPause?.    GetHashCode() ?? 0) * 3 ^

                       base.          GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   EVSEProcessing.AsText(),

                   GoToPause.HasValue
                       ? GoToPause.Value
                             ? " [pause]"
                             : ""
                       : ""

               );

        #endregion

    }

}
