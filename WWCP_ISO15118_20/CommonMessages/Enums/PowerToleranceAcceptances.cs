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
    /// Extensions methods for power tolerance acceptances.
    /// </summary>
    public static class PowerToleranceAcceptancesExtensions
    {

        #region Parse   (Text)

        /// <summary>
        /// Parse the given text as an power tolerance acceptance.
        /// </summary>
        /// <param name="Text">A text representation of an power tolerance acceptance.</param>
        public static PowerToleranceAcceptances Parse(String Text)
        {

            if (TryParse(Text, out var acceptance))
                return acceptance;

            return PowerToleranceAcceptances.PowerToleranceNotConfirmed;

        }

        #endregion

        #region TryParse(Text)

        /// <summary>
        /// Try to parse the given text as an power tolerance acceptance.
        /// </summary>
        /// <param name="Text">A text representation of an power tolerance acceptance.</param>
        public static PowerToleranceAcceptances? TryParse(String Text)
        {

            if (TryParse(Text, out var acceptance))
                return acceptance;

            return null;

        }

        #endregion

        #region TryParse(Text, out PowerToleranceAcceptance)

        /// <summary>
        /// Try to parse the given text as an power tolerance acceptance.
        /// </summary>
        /// <param name="Text">A text representation of an power tolerance acceptance.</param>
        /// <param name="PowerToleranceAcceptance">The parsed power tolerance acceptance.</param>
        public static Boolean TryParse(String Text, out PowerToleranceAcceptances PowerToleranceAcceptance)
        {
            switch (Text.Trim())
            {

                case "PowerToleranceConfirmed":
                    PowerToleranceAcceptance = PowerToleranceAcceptances.PowerToleranceConfirmed;
                    return true;

                default:
                    PowerToleranceAcceptance = PowerToleranceAcceptances.PowerToleranceNotConfirmed;
                    return false;

            }
        }

        #endregion

        #region AsText  (this PowerToleranceAcceptances)

        public static String AsText(this PowerToleranceAcceptances PowerToleranceAcceptance)

            => PowerToleranceAcceptance switch {

                   PowerToleranceAcceptances.PowerToleranceConfirmed  => "PowerToleranceConfirmed",
                   _                                                  => "PowerToleranceNotConfirmed"

               };

        #endregion

    }


    #region Documentation

    // <xs:simpleType name="powerToleranceAcceptanceType">
    //     <xs:restriction base="xs:string">
    //         <xs:enumeration value="PowerToleranceNotConfirmed"/>
    //         <xs:enumeration value="PowerToleranceConfirmed"/>
    //     </xs:restriction>
    // </xs:simpleType>

    #endregion


    /// <summary>
    /// Power tolerance acceptances.
    /// </summary>
    public enum PowerToleranceAcceptances
    {

        /// <summary>
        /// Power tolerance not confirmed
        /// </summary>
        PowerToleranceNotConfirmed,

        /// <summary>
        /// Power tolerance confirmed
        /// </summary>
        PowerToleranceConfirmed

    }

}
