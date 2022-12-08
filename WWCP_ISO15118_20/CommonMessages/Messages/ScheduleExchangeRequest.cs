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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

using cloud.charging.open.protocols.ISO15118_20.CommonTypes;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// The abstract schedule exchange request.
    /// </summary>
    public abstract class ScheduleExchangeRequest : ARequest<ScheduleExchangeRequest>
    {

        #region Properties

        /// <summary>
        /// The maximum number of supporting points of the energy schedule.
        /// [min/max 12...1024]
        /// </summary>
        [Mandatory]
        public UInt16  MaximumSupportingPoints    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new abstract schedule exchange request.
        /// </summary>
        /// <param name="MessageHeader">A message header.</param>
        /// <param name="MaximumSupportingPoints">The maximum number of supporting points of the energy schedule [min/max 12...1024].</param>
        public ScheduleExchangeRequest(MessageHeader  MessageHeader,
                                       UInt16         MaximumSupportingPoints)

            : base(MessageHeader)

        {

            this.MaximumSupportingPoints = MaximumSupportingPoints;

        }

        #endregion


        #region Documentation

        // <xs:element name="ScheduleExchangeReq" type="ScheduleExchangeReqType"/>
        // <xs:complexType name="ScheduleExchangeReqType">
        //     <xs:complexContent>
        //         <xs:extension base="v2gci_ct:V2GRequestType">
        //             <xs:sequence>
        //
        //                 <xs:element name="MaximumSupportingPoints" type="maxSupportingPointsScheduleTupleType"/>
        //
        //                 <xs:choice>
        //                     <xs:element name="Dynamic_SEReqControlMode"   type="Dynamic_SEReqControlModeType"/>
        //                     <xs:element name="Scheduled_SEReqControlMode" type="Scheduled_SEReqControlModeType"/>
        //                 </xs:choice>
        //
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>


        // <xs:simpleType name="maxSupportingPointsScheduleTupleType">
        //     <xs:restriction base="xs:unsignedShort">
        //         <xs:minInclusive value="12"/>
        //         <xs:maxInclusive value="1024"/>
        //     </xs:restriction>
        // </xs:simpleType>

        #endregion


        #region Operator overloading

        #region Operator == (ScheduleExchangeRequest1, ScheduleExchangeRequest2)

        /// <summary>
        /// Compares two schedule exchange requests for equality.
        /// </summary>
        /// <param name="ScheduleExchangeRequest1">A schedule exchange request.</param>
        /// <param name="ScheduleExchangeRequest2">Another schedule exchange request.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ScheduleExchangeRequest? ScheduleExchangeRequest1,
                                           ScheduleExchangeRequest? ScheduleExchangeRequest2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(ScheduleExchangeRequest1, ScheduleExchangeRequest2))
                return true;

            // If one is null, but not both, return false.
            if (ScheduleExchangeRequest1 is null || ScheduleExchangeRequest2 is null)
                return false;

            return ScheduleExchangeRequest1.Equals(ScheduleExchangeRequest2);

        }

        #endregion

        #region Operator != (ScheduleExchangeRequest1, ScheduleExchangeRequest2)

        /// <summary>
        /// Compares two schedule exchange requests for inequality.
        /// </summary>
        /// <param name="ScheduleExchangeRequest1">A schedule exchange request.</param>
        /// <param name="ScheduleExchangeRequest2">Another schedule exchange request.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ScheduleExchangeRequest? ScheduleExchangeRequest1,
                                           ScheduleExchangeRequest? ScheduleExchangeRequest2)

            => !(ScheduleExchangeRequest1 == ScheduleExchangeRequest2);

        #endregion

        #endregion

        #region IEquatable<ScheduleExchangeRequest> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two schedule exchange requests for equality.
        /// </summary>
        /// <param name="Object">A schedule exchange request to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ScheduleExchangeRequest scheduleExchangeRequest &&
                   Equals(scheduleExchangeRequest);

        #endregion

        #region Equals(ScheduleExchangeRequest)

        /// <summary>
        /// Compares two schedule exchange requests for equality.
        /// </summary>
        /// <param name="ScheduleExchangeRequest">A schedule exchange request to compare with.</param>
        public override Boolean Equals(ScheduleExchangeRequest? ScheduleExchangeRequest)

            => ScheduleExchangeRequest is not null &&

               MaximumSupportingPoints.Equals(ScheduleExchangeRequest.MaximumSupportingPoints) &&

               base.Equals(ScheduleExchangeRequest);

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

                return MaximumSupportingPoints.GetHashCode() * 3 ^
                       base.                   GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => MaximumSupportingPoints + " maximum supporting points";

        #endregion

    }

}
