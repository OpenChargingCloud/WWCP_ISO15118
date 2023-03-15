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
    /// Extensions methods for ECDH curves.
    /// </summary>
    public static class ECDHCurvesExtensions
    {

        #region Parse   (Text)

        /// <summary>
        /// Parse the given text as an ECDH curve.
        /// </summary>
        /// <param name="Text">A text representation of an ECDH curve.</param>
        public static ECDHCurves Parse(String Text)
        {

            if (TryParse(Text, out var curve))
                return curve;

            return ECDHCurves.Unknown;

        }

        #endregion

        #region TryParse(Text)

        /// <summary>
        /// Try to parse the given text as an ECDH curve.
        /// </summary>
        /// <param name="Text">A text representation of an ECDH curve.</param>
        public static ECDHCurves? TryParse(String Text)
        {

            if (TryParse(Text, out var curve))
                return curve;

            return null;

        }

        #endregion

        #region TryParse(Text, out ECDHCurve)

        /// <summary>
        /// Try to parse the given text as an ECDH curve.
        /// </summary>
        /// <param name="Text">A text representation of an ECDH curve.</param>
        /// <param name="ECDHCurve">The parsed ECDH curve.</param>
        public static Boolean TryParse(String Text, out ECDHCurves ECDHCurve)
        {
            switch (Text.Trim())
            {

                case "SECP521":
                    ECDHCurve = ECDHCurves.SECP521;
                    return true;

                case "X448":
                    ECDHCurve = ECDHCurves.X448;
                    return true;

                default:
                    ECDHCurve = ECDHCurves.Unknown;
                    return false;

            }
        }

        #endregion

        #region AsText  (this ECDHCurve)

        public static String AsText(this ECDHCurves ECDHCurve)

            => ECDHCurve switch {

                   ECDHCurves.SECP521  => "SECP521",
                   ECDHCurves.X448     => "X448",
                   _                   => "Unknown"

               };

        #endregion

    }

    //ToDo: A readonly struct would be more future-proof here!

    #region Documentation

    // <xs:simpleType name="ecdhCurveType">
    //     <xs:restriction base="xs:string">
    //         <xs:enumeration value="SECP521"/>
    //         <xs:enumeration value="X448"/>
    //     </xs:restriction>
    // </xs:simpleType>

    #endregion


    /// <summary>
    /// ECDH Curves
    /// </summary>
    public enum ECDHCurves
    {

        /// <summary>
        /// Unknown ECDH Curve.
        /// </summary>
        Unknown,

        /// <summary>
        /// 521-bit prime field Weierstrass curve.
        /// Also known as: secp521r1, P-521, ansip521r1, 1.3.132.0.35
        /// https://neuromancer.sk/std/secg/secp521r1
        /// </summary>
        SECP521,

        /// <summary>
        /// Curve448-Goldilocks 224-bit security
        /// Also known as: X448
        /// </summary>
        X448

    }

}
