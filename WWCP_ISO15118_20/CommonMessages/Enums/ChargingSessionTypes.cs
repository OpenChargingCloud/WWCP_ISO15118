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
    /// Extensions methods for charging session types.
    /// </summary>
    public static class ChargingSessionTypesExtensions
    {

        #region Parse   (Text)

        /// <summary>
        /// Parse the given text as a charging session type.
        /// </summary>
        /// <param name="Text">A text representation of a charging session type.</param>
        public static ChargingSessionTypes Parse(String Text)
        {

            if (TryParse(Text, out var type))
                return type;

            return ChargingSessionTypes.Unknown;

        }

        #endregion

        #region TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a charging session type.
        /// </summary>
        /// <param name="Text">A text representation of a charging session type.</param>
        public static ChargingSessionTypes? TryParse(String Text)
        {

            if (TryParse(Text, out var type))
                return type;

            return null;

        }

        #endregion

        #region TryParse(Text, out ChargingSessionType)

        /// <summary>
        /// Try to parse the given text as a charging session type.
        /// </summary>
        /// <param name="Text">A text representation of a charging session type.</param>
        /// <param name="ChargingSessionType">The parsed charging session type.</param>
        public static Boolean TryParse(String Text, out ChargingSessionTypes ChargingSessionType)
        {
            switch (Text.Trim())
            {

                case "Pause":
                    ChargingSessionType = ChargingSessionTypes.Pause;
                    return true;

                case "Terminate":
                    ChargingSessionType = ChargingSessionTypes.Terminate;
                    return true;

                case "ServiceRenegotiation":
                    ChargingSessionType = ChargingSessionTypes.ServiceRenegotiation;
                    return true;

                default:
                    ChargingSessionType = ChargingSessionTypes.Unknown;
                    return false;

            }
        }

        #endregion

        #region AsText  (this ChargingSessionType)

        public static String AsText(this ChargingSessionTypes ChargingSessionType)

            => ChargingSessionType switch {

                   ChargingSessionTypes.Pause                 => "Pause",
                   ChargingSessionTypes.Terminate             => "Terminate",
                   ChargingSessionTypes.ServiceRenegotiation  => "ServiceRenegotiation",

                   _                                          => "Unknown"

               };

        #endregion

    }


    #region Documentation

    // <xs:simpleType name="chargingSessionType">
    //     <xs:restriction base="xs:string">
    //         <xs:enumeration value="Pause"/>
    //         <xs:enumeration value="Terminate"/>
    //         <xs:enumeration value="ServiceRenegotiation"/>
    //     </xs:restriction>
    // </xs:simpleType>

    #endregion


    /// <summary>
    /// Charging session types
    /// </summary>
    public enum ChargingSessionTypes
    {

        /// <summary>
        /// Unknown charging session type.
        /// </summary>
        Unknown,

        /// <summary>
        /// Pause
        /// </summary>
        Pause,

        /// <summary>
        /// Terminate
        /// </summary>
        Terminate,

        /// <summary>
        /// Service renegotiation
        /// </summary>
        ServiceRenegotiation

    }

}
