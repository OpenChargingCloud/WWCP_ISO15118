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

#region Usings

using System.Numerics;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.XMLDSig
{

    /// <summary>
    /// The X.509 issuer serial.
    /// </summary>
    public class X509IssuerSerial
    {

        #region Properties

        /// <summary>
        /// The issuer name.
        /// </summary>
        [Mandatory]
        public String      IssuerName      { get; }

        /// <summary>
        /// The serial number.
        /// </summary>
        [Mandatory]
        public BigInteger  SerialNumber    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new X.509 issuer serial.
        /// </summary>
        /// <param name="IssuerName">An issuer name.</param>
        /// <param name="SerialNumber">A serial number.</param>
        public X509IssuerSerial(String      IssuerName,
                                BigInteger  SerialNumber)
        {

            this.IssuerName    = IssuerName;
            this.SerialNumber  = SerialNumber;

        }

        #endregion


        #region Documentation

        // <complexType name="X509IssuerSerialType"> 
        //   <sequence> 
        //     <element name="X509IssuerName"   type="string"/> 
        //     <element name="X509SerialNumber" type="integer"/> 
        //   </sequence>
        // </complexType>

        #endregion

        #region (static) Parse   (JSON, CustomX509IssuerSerialParser = null)

        /// <summary>
        /// Parse the given JSON representation of a X.509 issuer serial.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomX509IssuerSerialParser">A delegate to parse custom X.509 issuer serials.</param>
        public static X509IssuerSerial Parse(JObject                                         JSON,
                                             CustomJObjectParserDelegate<X509IssuerSerial>?  CustomX509IssuerSerialParser   = null)
        {

            if (TryParse(JSON,
                         out var x509IssuerSerial,
                         out var errorResponse,
                         CustomX509IssuerSerialParser))
            {
                return x509IssuerSerial!;
            }

            throw new ArgumentException("The given JSON representation of a X.509 issuer serial is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out X509IssuerSerial, out ErrorResponse, CustomX509IssuerSerialParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of a X.509 issuer serial.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="X509IssuerSerial">The parsed X.509 issuer serial.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                JSON,
                                       out X509IssuerSerial?  X509IssuerSerial,
                                       out String?            ErrorResponse)

            => TryParse(JSON,
                        out X509IssuerSerial,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a X.509 issuer serial.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="X509IssuerSerial">The parsed X.509 issuer serial.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomX509IssuerSerialParser">A delegate to parse custom contract certificates.</param>
        public static Boolean TryParse(JObject                                         JSON,
                                       out X509IssuerSerial?                           X509IssuerSerial,
                                       out String?                                     ErrorResponse,
                                       CustomJObjectParserDelegate<X509IssuerSerial>?  CustomX509IssuerSerialParser)
        {

            try
            {

                X509IssuerSerial = null;

                #region X509IssuerName      [mandatory]

                if (!JSON.ParseMandatoryText("issuerName",
                                             "issuerName",
                                             out String X509IssuerName,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region X509SerialNumber    [mandatory]

                if (!JSON.ParseMandatory("serialNumber",
                                         "serialNumber",
                                         BigInteger.TryParse,
                                         out BigInteger X509SerialNumber,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion


                X509IssuerSerial = new X509IssuerSerial(X509IssuerName,
                                                        X509SerialNumber);

                if (CustomX509IssuerSerialParser is not null)
                    X509IssuerSerial = CustomX509IssuerSerialParser(JSON,
                                                                    X509IssuerSerial);

                return true;

            }
            catch (Exception e)
            {
                X509IssuerSerial  = null;
                ErrorResponse     = "The given JSON representation of a X.509 issuer serial is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomX509IssuerSerialSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomX509IssuerSerialSerializer">A delegate to serialize custom X.509 issuer serials.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<X509IssuerSerial>? CustomX509IssuerSerialSerializer = null)
        {

            var json = JSONObject.Create(

                           new JProperty("issuerName",    IssuerName),
                           new JProperty("serialNumber",  SerialNumber.ToString())

                       );

            return CustomX509IssuerSerialSerializer is not null
                       ? CustomX509IssuerSerialSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (X509IssuerSerial1, X509IssuerSerial2)

        /// <summary>
        /// Compares two X.509 issuer serials for equality.
        /// </summary>
        /// <param name="X509IssuerSerial1">A X.509 issuer serial.</param>
        /// <param name="X509IssuerSerial2">Another X.509 issuer serial.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (X509IssuerSerial? X509IssuerSerial1,
                                           X509IssuerSerial? X509IssuerSerial2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(X509IssuerSerial1, X509IssuerSerial2))
                return true;

            // If one is null, but not both, return false.
            if (X509IssuerSerial1 is null || X509IssuerSerial2 is null)
                return false;

            return X509IssuerSerial1.Equals(X509IssuerSerial2);

        }

        #endregion

        #region Operator != (X509IssuerSerial1, X509IssuerSerial2)

        /// <summary>
        /// Compares two X.509 issuer serials for inequality.
        /// </summary>
        /// <param name="X509IssuerSerial1">A X.509 issuer serial.</param>
        /// <param name="X509IssuerSerial2">Another X.509 issuer serial.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (X509IssuerSerial? X509IssuerSerial1,
                                           X509IssuerSerial? X509IssuerSerial2)

            => !(X509IssuerSerial1 == X509IssuerSerial2);

        #endregion

        #endregion

        #region IEquatable<X509IssuerSerial> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two X.509 issuer serials for equality.
        /// </summary>
        /// <param name="Object">A X.509 issuer serial to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is X509IssuerSerial x509IssuerSerial &&
                   Equals(x509IssuerSerial);

        #endregion

        #region Equals(X509IssuerSerial)

        /// <summary>
        /// Compares two X.509 issuer serials for equality.
        /// </summary>
        /// <param name="X509IssuerSerial">A X.509 issuer serial to compare with.</param>
        public Boolean Equals(X509IssuerSerial? X509IssuerSerial)

            => X509IssuerSerial is not null &&

               IssuerName.  Equals(X509IssuerSerial.IssuerName) &&
               SerialNumber.Equals(X509IssuerSerial.SerialNumber);

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the HashCode of this object.
        /// </summary>
        /// <returns>The HashCode of this object.</returns>
        public override Int32 GetHashCode()
        {
            unchecked
            {

                return IssuerName.  GetHashCode() * 5 ^
                       SerialNumber.GetHashCode() * 3 ^

                       base.        GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   IssuerName,
                   ": ",
                   SerialNumber

               );

        #endregion

    }

}
