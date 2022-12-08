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
    /// Extensions methods for EVSE notifications.
    /// </summary>
    public static class EVSENotificationsExtensions
    {

        #region Parse   (Text)

        /// <summary>
        /// Parse the given text as an EVSE notification.
        /// </summary>
        /// <param name="Text">A text representation of an EVSE notification.</param>
        public static EVSENotifications Parse(String Text)
        {

            if (TryParse(Text, out var notification))
                return notification;

            return EVSENotifications.Unknown;

        }

        #endregion

        #region TryParse(Text)

        /// <summary>
        /// Try to parse the given text as an EVSE notification.
        /// </summary>
        /// <param name="Text">A text representation of an EVSE notification.</param>
        public static EVSENotifications? TryParse(String Text)
        {

            if (TryParse(Text, out var notification))
                return notification;

            return null;

        }

        #endregion

        #region TryParse(Text, out EVSENotification)

        /// <summary>
        /// Try to parse the given text as an EVSE notification.
        /// </summary>
        /// <param name="Text">A text representation of an EVSE notification.</param>
        /// <param name="EVSENotification">The parsed EVSE notification.</param>
        public static Boolean TryParse(String Text, out EVSENotifications EVSENotification)
        {
            switch (Text.Trim())
            {

                case "Pause":
                    EVSENotification = EVSENotifications.Pause;
                    return true;

                case "ExitStandby":
                    EVSENotification = EVSENotifications.ExitStandby;
                    return true;

                case "Terminate":
                    EVSENotification = EVSENotifications.Terminate;
                    return true;

                case "ScheduleRenegotiation":
                    EVSENotification = EVSENotifications.ScheduleRenegotiation;
                    return true;

                case "ServiceRenegotiation":
                    EVSENotification = EVSENotifications.ServiceRenegotiation;
                    return true;

                case "MeteringConfirmation":
                    EVSENotification = EVSENotifications.MeteringConfirmation;
                    return true;

                default:
                    EVSENotification = EVSENotifications.Unknown;
                    return false;

            }
        }

        #endregion

        #region AsText  (this EVSENotification)

        public static String AsText(this EVSENotifications EVSENotification)

            => EVSENotification switch {

                   EVSENotifications.Pause                  => "Pause",
                   EVSENotifications.ExitStandby            => "ExitStandby",
                   EVSENotifications.Terminate              => "Terminate",
                   EVSENotifications.ScheduleRenegotiation  => "ScheduleRenegotiation",
                   EVSENotifications.ServiceRenegotiation   => "ServiceRenegotiation",
                   EVSENotifications.MeteringConfirmation   => "MeteringConfirmation",

                   _                                        => "Unknown"

               };

        #endregion

    }


    #region Documentation

    // <xs:simpleType name="evseNotificationType">
    //     <xs:restriction base="xs:string">
    //         <xs:enumeration value="Pause"/>
    //         <xs:enumeration value="ExitStandby"/>
    //         <xs:enumeration value="Terminate"/>
    //         <xs:enumeration value="ScheduleRenegotiation"/>
    //         <xs:enumeration value="ServiceRenegotiation"/>
    //         <xs:enumeration value="MeteringConfirmation"/>
    //     </xs:restriction>
    // </xs:simpleType>

    #endregion


    /// <summary>
    /// EVSE notifications.
    /// </summary>
    public enum EVSENotifications
    {

        /// <summary>
        /// Unknown EVSE notification.
        /// </summary>
        Unknown,

        /// <summary>
        /// Pause
        /// </summary>
        Pause,

        /// <summary>
        /// Exit standby
        /// </summary>
        ExitStandby,

        /// <summary>
        /// Terminate
        /// </summary>
        Terminate,

        /// <summary>
        /// Schedule renegotiation
        /// </summary>
        ScheduleRenegotiation,

        /// <summary>
        /// Service renegotiation
        /// </summary>
        ServiceRenegotiation,

        /// <summary>
        /// Metering confirmation
        /// </summary>
        MeteringConfirmation

    }

}
