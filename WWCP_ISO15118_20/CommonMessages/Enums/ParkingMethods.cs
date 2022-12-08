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
    /// Extensions methods for parking methods.
    /// </summary>
    public static class ParkingMethodsExtensions
    {

        #region Parse   (Text)

        /// <summary>
        /// Parse the given text as a parking method.
        /// </summary>
        /// <param name="Text">A text representation of a parking method.</param>
        public static ParkingMethods Parse(String Text)
        {

            if (TryParse(Text, out var method))
                return method;

            return ParkingMethods.Manual;

        }

        #endregion

        #region TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a parking method.
        /// </summary>
        /// <param name="Text">A text representation of a parking method.</param>
        public static ParkingMethods? TryParse(String Text)
        {

            if (TryParse(Text, out var method))
                return method;

            return null;

        }

        #endregion

        #region TryParse(Text, out ParkingMethod)

        /// <summary>
        /// Try to parse the given text as a parking method.
        /// </summary>
        /// <param name="Text">A text representation of a parking method.</param>
        /// <param name="ParkingMethod">The parsed parking method.</param>
        public static Boolean TryParse(String Text, out ParkingMethods ParkingMethod)
        {
            switch (Text.Trim())
            {

                case "AutoParking":
                    ParkingMethod = ParkingMethods.AutoParking;
                    return true;

                case "MVGuideManual":
                    ParkingMethod = ParkingMethods.MVGuideManual;
                    return true;

                default:
                    ParkingMethod = ParkingMethods.Manual;
                    return false;

            }
        }

        #endregion

        #region AsText  (this ParkingMethods)

        public static String AsText(this ParkingMethods ParkingMethod)

            => ParkingMethod switch {

                   ParkingMethods.AutoParking    => "AutoParking",
                   ParkingMethods.MVGuideManual  => "MVGuideManual",
                   _                             => "Manual"

            };

        #endregion

    }


    #region Documentation

    // <xs:simpleType name="parkingMethodType">
    //     <xs:restriction base="xs:string">
    //         <xs:enumeration value="AutoParking"/>
    //         <xs:enumeration value="MVGuideManual"/>
    //         <xs:enumeration value="Manual"/>
    //     </xs:restriction>
    // </xs:simpleType>

    #endregion


    /// <summary>
    /// Parking methods.
    /// </summary>
    public enum ParkingMethods
    {

        /// <summary>
        /// Auto parking
        /// </summary>
        AutoParking,

        /// <summary>
        /// MV guide manual
        /// </summary>
        MVGuideManual,

        /// <summary>
        /// Manual
        /// </summary>
        Manual

    }

}
