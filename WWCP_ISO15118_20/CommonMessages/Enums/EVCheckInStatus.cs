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

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// Extensions methods for EV check in status.
    /// </summary>
    public static class EVCheckInStatusExtensions
    {

        #region Parse   (Text)

        /// <summary>
        /// Parse the given text as an EV check in status.
        /// </summary>
        /// <param name="Text">A text representation of an EV check in status.</param>
        public static EVCheckInStatus Parse(String Text)
        {

            if (TryParse(Text, out var status))
                return status;

            return EVCheckInStatus.Unknown;

        }

        #endregion

        #region TryParse(Text)

        /// <summary>
        /// Try to parse the given text as an EV check in status.
        /// </summary>
        /// <param name="Text">A text representation of an EV check in status.</param>
        public static EVCheckInStatus? TryParse(String Text)
        {

            if (TryParse(Text, out var status))
                return status;

            return null;

        }

        #endregion

        #region TryParse(Text, out EVCheckInStatus)

        /// <summary>
        /// Try to parse the given text as an EV check in status.
        /// </summary>
        /// <param name="Text">A text representation of an EV check in status.</param>
        /// <param name="EVCheckInStatus">The parsed EV check in status.</param>
        public static Boolean TryParse(String Text, out EVCheckInStatus EVCheckInStatus)
        {
            switch (Text.Trim())
            {

                case "CheckIn":
                    EVCheckInStatus = EVCheckInStatus.CheckIn;
                    return true;

                case "Processing":
                    EVCheckInStatus = EVCheckInStatus.Processing;
                    return true;

                case "Completed":
                    EVCheckInStatus = EVCheckInStatus.Completed;
                    return true;

                default:
                    EVCheckInStatus = EVCheckInStatus.Unknown;
                    return false;

            }
        }

        #endregion

        #region AsText  (this EVCheckInStatus)

        public static String AsText(this EVCheckInStatus EVCheckInStatus)

            => EVCheckInStatus switch {

                   EVCheckInStatus.CheckIn     => "CheckIn",
                   EVCheckInStatus.Processing  => "Processing",
                   EVCheckInStatus.Completed   => "Completed",
                   _                           => "Unknown"

            };

        #endregion

    }


    #region Documentation

    // <xs:simpleType name="evCheckInStatusType">
    //     <xs:restriction base="xs:string">
    //         <xs:enumeration value="CheckIn"/>
    //         <xs:enumeration value="Processing"/>
    //         <xs:enumeration value="Completed"/>
    //     </xs:restriction>
    // </xs:simpleType>

    #endregion


    /// <summary>
    /// EV check in status.
    /// </summary>
    public enum EVCheckInStatus
    {

        /// <summary>
        /// Unknown EV check in status.
        /// </summary>
        Unknown,

        /// <summary>
        /// CheckIn
        /// </summary>
        CheckIn,

        /// <summary>
        /// Processing
        /// </summary>
        Processing,

        /// <summary>
        /// Completed
        /// </summary>
        Completed

    }

}
