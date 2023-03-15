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
    /// Extensions methods for channel selection types.
    /// </summary>
    public static class ChannelSelectionTypesExtensions
    {

        #region Parse   (Text)

        /// <summary>
        /// Parse the given text as a channel selection type.
        /// </summary>
        /// <param name="Text">A text representation of a channel selection type.</param>
        public static ChannelSelectionTypes Parse(String Text)
        {

            if (TryParse(Text, out var type))
                return type;

            return ChannelSelectionTypes.Unknown;

        }

        #endregion

        #region TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a channel selection type.
        /// </summary>
        /// <param name="Text">A text representation of a channel selection type.</param>
        public static ChannelSelectionTypes? TryParse(String Text)
        {

            if (TryParse(Text, out var type))
                return type;

            return null;

        }

        #endregion

        #region TryParse(Text, out ChannelSelectionType)

        /// <summary>
        /// Try to parse the given text as a channel selection type.
        /// </summary>
        /// <param name="Text">A text representation of a channel selection type.</param>
        /// <param name="ChannelSelectionType">The parsed channel selection type.</param>
        public static Boolean TryParse(String Text, out ChannelSelectionTypes ChannelSelectionType)
        {
            switch (Text.Trim())
            {

                case "Charge":
                    ChannelSelectionType = ChannelSelectionTypes.Charge;
                    return true;

                case "Discharge":
                    ChannelSelectionType = ChannelSelectionTypes.Discharge;
                    return true;

                default:
                    ChannelSelectionType = ChannelSelectionTypes.Unknown;
                    return false;

            }
        }

        #endregion

        #region AsText  (this ChannelSelectionTypes)

        public static String AsText(this ChannelSelectionTypes ChannelSelectionType)

            => ChannelSelectionType switch {

                   ChannelSelectionTypes.Charge     => "Charge",
                   ChannelSelectionTypes.Discharge  => "Discharge",

                   _                                => "Unknown"

               };

        #endregion

    }


    #region Documentation

    // <xs:simpleType name="channelSelectionType">
    //     <xs:restriction base="xs:string">
    //         <xs:enumeration value="Charge"/>
    //         <xs:enumeration value="Discharge"/>
    //     </xs:restriction>
    // </xs:simpleType>

    #endregion


    /// <summary>
    /// Channel selection types.
    /// </summary>
    public enum ChannelSelectionTypes
    {

        /// <summary>
        /// Unknown channel.
        /// </summary>
        Unknown,

        /// <summary>
        /// Charge
        /// </summary>
        Charge,

        /// <summary>
        /// Discharge
        /// </summary>
        Discharge

    }

}
