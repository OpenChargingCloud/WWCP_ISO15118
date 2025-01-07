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

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// Extensions methods for charge progress types.
    /// </summary>
    public static class ChargeProgressTypesExtensions
    {

        #region Parse   (Text)

        /// <summary>
        /// Parse the given text as a charge progress type.
        /// </summary>
        /// <param name="Text">A text representation of a charge progress type.</param>
        public static ChargeProgressTypes Parse(String Text)
        {

            if (TryParse(Text, out var type))
                return type;

            return ChargeProgressTypes.Unknown;

        }

        #endregion

        #region TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a charge progress type.
        /// </summary>
        /// <param name="Text">A text representation of a charge progress type.</param>
        public static ChargeProgressTypes? TryParse(String Text)
        {

            if (TryParse(Text, out var type))
                return type;

            return null;

        }

        #endregion

        #region TryParse(Text, out ChargingSessionType)

        /// <summary>
        /// Try to parse the given text as a charge progress type.
        /// </summary>
        /// <param name="Text">A text representation of a charge progress type.</param>
        /// <param name="ChargingSessionType">The parsed charge progress type.</param>
        public static Boolean TryParse(String Text, out ChargeProgressTypes ChargingSessionType)
        {
            switch (Text.Trim())
            {

                case "Start":
                    ChargingSessionType = ChargeProgressTypes.Start;
                    return true;

                case "Stop":
                    ChargingSessionType = ChargeProgressTypes.Stop;
                    return true;

                case "Standby":
                    ChargingSessionType = ChargeProgressTypes.Standby;
                    return true;

                case "ScheduleRenegotiation":
                    ChargingSessionType = ChargeProgressTypes.ScheduleRenegotiation;
                    return true;

                default:
                    ChargingSessionType = ChargeProgressTypes.Unknown;
                    return false;

            }
        }

        #endregion

        #region AsText  (this ChargingSessionType)

        public static String AsText(this ChargeProgressTypes ChargingSessionType)

            => ChargingSessionType switch {

                   ChargeProgressTypes.Start                  => "Start",
                   ChargeProgressTypes.Stop                   => "Stop",
                   ChargeProgressTypes.Standby                => "Standby",
                   ChargeProgressTypes.ScheduleRenegotiation  => "ScheduleRenegotiation",

                   _                                          => "Unknown"

               };

        #endregion

    }


    #region Documentation

    // <xs:simpleType name="chargeProgressType">
    //     <xs:restriction base="xs:string">
    //         <xs:enumeration value="Start"/>
    //         <xs:enumeration value="Stop"/>
    //         <xs:enumeration value="Standby"/>
    //         <xs:enumeration value="ScheduleRenegotiation"/>
    //     </xs:restriction>
    // </xs:simpleType>

    #endregion


    /// <summary>
    /// Charge progress types.
    /// </summary>
    public enum ChargeProgressTypes
    {

        /// <summary>
        /// Unknown charge progress type.
        /// </summary>
        Unknown,

        /// <summary>
        /// Start charging.
        /// </summary>
        Start,

        /// <summary>
        /// Stop charging.
        /// </summary>
        Stop,

        /// <summary>
        /// Standby.
        /// </summary>
        Standby,

        /// <summary>
        /// Schedule renegotiation.
        /// </summary>
        ScheduleRenegotiation

    }

}
