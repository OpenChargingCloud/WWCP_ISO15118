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
    /// Extensions methods for EV check out status.
    /// </summary>
    public static class EVCheckOutStatusExtensions
    {

        #region Parse   (Text)

        /// <summary>
        /// Parse the given text as an EV check out status.
        /// </summary>
        /// <param name="Text">A text representation of an EV check out status.</param>
        public static EVCheckOutStatus Parse(String Text)
        {

            if (TryParse(Text, out var status))
                return status;

            return EVCheckOutStatus.Unknown;

        }

        #endregion

        #region TryParse(Text)

        /// <summary>
        /// Try to parse the given text as an EV check out status.
        /// </summary>
        /// <param name="Text">A text representation of an EV check out status.</param>
        public static EVCheckOutStatus? TryParse(String Text)
        {

            if (TryParse(Text, out var status))
                return status;

            return null;

        }

        #endregion

        #region TryParse(Text, out EVCheckOutStatus)

        /// <summary>
        /// Try to parse the given text as an EV check out status.
        /// </summary>
        /// <param name="Text">A text representation of an EV check out status.</param>
        /// <param name="EVCheckOutStatus">The parsed EV check out status.</param>
        public static Boolean TryParse(String Text, out EVCheckOutStatus EVCheckOutStatus)
        {
            switch (Text.Trim())
            {

                case "CheckOut":
                    EVCheckOutStatus = EVCheckOutStatus.CheckOut;
                    return true;

                case "Processing":
                    EVCheckOutStatus = EVCheckOutStatus.Processing;
                    return true;

                case "Completed":
                    EVCheckOutStatus = EVCheckOutStatus.Completed;
                    return true;

                default:
                    EVCheckOutStatus = EVCheckOutStatus.Unknown;
                    return false;

            }
        }

        #endregion

        #region AsText  (this EVCheckOutStatus)

        public static String AsText(this EVCheckOutStatus EVCheckOutStatus)

            => EVCheckOutStatus switch {

                   EVCheckOutStatus.CheckOut    => "CheckOut",
                   EVCheckOutStatus.Processing  => "Processing",
                   EVCheckOutStatus.Completed   => "Completed",
                   _                            => "Unknown"

            };

        #endregion

    }


    #region Documentation

    // <xs:simpleType name="evCheckOutStatusType">
    //     <xs:restriction base="xs:string">
    //         <xs:enumeration value="CheckOut"/>
    //         <xs:enumeration value="Processing"/>
    //         <xs:enumeration value="Completed"/>
    //     </xs:restriction>
    // </xs:simpleType>

    #endregion


    /// <summary>
    /// EV check out status.
    /// </summary>
    public enum EVCheckOutStatus
    {

        /// <summary>
        /// Unknown EV check out status.
        /// </summary>
        Unknown,

        /// <summary>
        /// CheckOut
        /// </summary>
        CheckOut,

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
