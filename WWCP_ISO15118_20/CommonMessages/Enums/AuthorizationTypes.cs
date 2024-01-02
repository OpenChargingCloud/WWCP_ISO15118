/*
 * Copyright (c) 2021-2024 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

        #region TryParse(Text, out AuthorizationType)

        /// <summary>
        /// Try to parse the given text as an authorization type.
        /// </summary>
        /// <param name="Text">A text representation of an authorization type.</param>
        /// <param name="AuthorizationType">The parsed authorization type.</param>
        public static Boolean TryParse(String Text, out AuthorizationTypes AuthorizationType)
        {
            switch (Text.Trim())
            {

                case "EIM":
                    AuthorizationType = AuthorizationTypes.EIM;
                    return true;

                case "PnC":
                    AuthorizationType = AuthorizationTypes.PnC;
                    return true;

                default:
                    AuthorizationType = AuthorizationTypes.Unknown;
                    return false;

            }
        }

        #endregion

        #region AsText  (this AuthorizationTypes)

        public static String AsText(this AuthorizationTypes AuthorizationType)

            => AuthorizationType switch {

                   AuthorizationTypes.EIM  => "EIM",
                   AuthorizationTypes.PnC  => "PnC",

                   _                       => "Unknown"

               };

        #endregion

    }


    #region Documentation

    // <xs:simpleType name="authorizationType">
    //     <xs:restriction base="xs:string">
    //         <xs:enumeration value="EIM"/>
    //         <xs:enumeration value="PnC"/>
    //     </xs:restriction>
    // </xs:simpleType>

    #endregion


    /// <summary>
    /// Authorization types.
    /// </summary>
    public enum AuthorizationTypes
    {

        /// <summary>
        /// Unknown authorization type.
        /// </summary>
        Unknown,

        /// <summary>
        /// External Identification Means.
        /// </summary>
        EIM,

        /// <summary>
        /// Plug and Charge authorization.
        /// </summary>
        PnC

    }

}
