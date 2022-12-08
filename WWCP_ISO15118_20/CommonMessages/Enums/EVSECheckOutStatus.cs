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

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// Extensions methods for EVSE check out status.
    /// </summary>
    public static class EVSECheckOutStatusExtensions
    {

        #region Parse   (Text)

        /// <summary>
        /// Parse the given text as an EVSE check out status.
        /// </summary>
        /// <param name="Text">A text representation of an EVSE check out status.</param>
        public static EVSECheckOutStatus Parse(String Text)
        {

            if (TryParse(Text, out var status))
                return status;

            return EVSECheckOutStatus.Unknown;

        }

        #endregion

        #region TryParse(Text)

        /// <summary>
        /// Try to parse the given text as an EVSE check out status.
        /// </summary>
        /// <param name="Text">A text representation of an EVSE check out status.</param>
        public static EVSECheckOutStatus? TryParse(String Text)
        {

            if (TryParse(Text, out var status))
                return status;

            return null;

        }

        #endregion

        #region TryParse(Text, out EVSECheckOutStatus)

        /// <summary>
        /// Try to parse the given text as an EVSE check out status.
        /// </summary>
        /// <param name="Text">A text representation of an EVSE check out status.</param>
        /// <param name="EVSECheckOutStatus">The parsed EVSE check out status.</param>
        public static Boolean TryParse(String Text, out EVSECheckOutStatus EVSECheckOutStatus)
        {
            switch (Text.Trim())
            {

                case "Scheduled":
                    EVSECheckOutStatus = EVSECheckOutStatus.Scheduled;
                    return true;

                case "Completed":
                    EVSECheckOutStatus = EVSECheckOutStatus.Completed;
                    return true;

                default:
                    EVSECheckOutStatus = EVSECheckOutStatus.Unknown;
                    return false;

            }
        }

        #endregion

        #region AsText  (this EVSECheckOutStatus)

        public static String AsText(this EVSECheckOutStatus EVSECheckOutStatus)

            => EVSECheckOutStatus switch {

                   EVSECheckOutStatus.Scheduled  => "Scheduled",
                   EVSECheckOutStatus.Completed  => "Completed",
                   _                             => "Unknown"

            };

        #endregion

    }


    #region Documentation

    // <xs:simpleType name="evseCheckOutStatusType">
    //     <xs:restriction base="xs:string">
    //         <xs:enumeration value="Scheduled"/>
    //         <xs:enumeration value="Completed"/>
    //     </xs:restriction>
    // </xs:simpleType>

    #endregion


    /// <summary>
    /// EVSE check out status.
    /// </summary>
    public enum EVSECheckOutStatus
    {

        /// <summary>
        /// Unknown EVSE check out status.
        /// </summary>
        Unknown,

        /// <summary>
        /// Scheduled
        /// </summary>
        Scheduled,

        /// <summary>
        /// Completed
        /// </summary>
        Completed

    }

}
