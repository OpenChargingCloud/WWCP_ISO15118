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
    /// Extensions methods for processing types.
    /// </summary>
    public static class ProcessingTypesExtensions
    {

        #region Parse   (Text)

        /// <summary>
        /// Parse the given text as a processing type.
        /// </summary>
        /// <param name="Text">A text representation of a processing type.</param>
        public static ProcessingTypes Parse(String Text)
        {

            if (TryParse(Text, out var type))
                return type;

            return ProcessingTypes.Unknown;

        }

        #endregion

        #region TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a processing type.
        /// </summary>
        /// <param name="Text">A text representation of a processing type.</param>
        public static ProcessingTypes? TryParse(String Text)
        {

            if (TryParse(Text, out var type))
                return type;

            return null;

        }

        #endregion

        #region TryParse(Text, out ProcessingType)

        /// <summary>
        /// Try to parse the given text as a processing type.
        /// </summary>
        /// <param name="Text">A text representation of a processing type.</param>
        /// <param name="ProcessingType">The parsed processing types.</param>
        public static Boolean TryParse(String Text, out ProcessingTypes ProcessingType)
        {
            switch (Text.Trim())
            {

                case "Finished":
                    ProcessingType = ProcessingTypes.Finished;
                    return true;

                case "Ongoing":
                    ProcessingType = ProcessingTypes.Ongoing;
                    return true;

                case "Ongoing_WaitingForCustomerInteraction":
                    ProcessingType = ProcessingTypes.Ongoing_WaitingForCustomerInteraction;
                    return true;

                default:
                    ProcessingType = ProcessingTypes.Unknown;
                    return false;

            }
        }

        #endregion

        #region AsText  (this ProcessingType)

        public static String AsText(this ProcessingTypes ProcessingType)

            => ProcessingType switch {

                   ProcessingTypes.Finished                               => "Finished",
                   ProcessingTypes.Ongoing                                => "Ongoing",
                   ProcessingTypes.Ongoing_WaitingForCustomerInteraction  => "Ongoing_WaitingForCustomerInteraction",

                   _                                                      => "Unknown"

               };

        #endregion

    }

    public enum ProcessingTypes
    {

        Unknown,

        Finished,
        Ongoing,
        Ongoing_WaitingForCustomerInteraction

    }

}
