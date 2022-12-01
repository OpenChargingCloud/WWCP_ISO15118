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
    /// Extensions methods for authorization types.
    /// </summary>
    public static class AuthorizationTypesExtensions
    {

        #region Parse   (Text)

        /// <summary>
        /// Parse the given text as an authorization type.
        /// </summary>
        /// <param name="Text">A text representation of an authorization type.</param>
        public static AuthorizationTypes Parse(String Text)
        {

            if (TryParse(Text, out var type))
                return type;

            return AuthorizationTypes.Unknown;

        }

        #endregion

        #region TryParse(Text)

        /// <summary>
        /// Try to parse the given text as an authorization type.
        /// </summary>
        /// <param name="Text">A text representation of an authorization type.</param>
        public static AuthorizationTypes? TryParse(String Text)
        {

            if (TryParse(Text, out var type))
                return type;

            return null;

        }

        #endregion

        #region TryParse(Text, out ResponseCode)

        /// <summary>
        /// Try to parse the given text as an authorization type.
        /// </summary>
        /// <param name="Text">A text representation of an authorization type.</param>
        /// <param name="ResponseCode">The parsed authorization types.</param>
        public static Boolean TryParse(String Text, out AuthorizationTypes ResponseCode)
        {
            switch (Text.Trim())
            {

                case "EIM":
                    ResponseCode = AuthorizationTypes.EIM;
                    return true;

                case "PnC":
                    ResponseCode = AuthorizationTypes.PnC;
                    return true;

                default:
                    ResponseCode = AuthorizationTypes.Unknown;
                    return false;

            }
        }

        #endregion

        #region AsText  (this AuthorizationTypes)

        public static String AsText(this AuthorizationTypes ResponseCode)

            => ResponseCode switch {

                   AuthorizationTypes.EIM  => "EIM",
                   AuthorizationTypes.PnC  => "PnC",

                   _                       => "Unknown"

               };

        #endregion

    }

    public enum AuthorizationTypes
    {

        Unknown,

        EIM,

        PnC

    }

}
