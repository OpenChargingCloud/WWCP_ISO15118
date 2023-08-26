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

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// Extention methods for SECP521 encrypted private keys.
    /// </summary>
    public static class SECP521EncryptedPrivateKeyExtensions
    {

        /// <summary>
        /// Indicates whether this SECP521 encrypted private key is null or empty.
        /// </summary>
        /// <param name="SECP521EncryptedPrivateKey">A SECP521 encrypted private key.</param>
        public static Boolean IsNullOrEmpty(this SECP521EncryptedPrivateKey? SECP521EncryptedPrivateKey)
            => !SECP521EncryptedPrivateKey.HasValue || SECP521EncryptedPrivateKey.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this SECP521 encrypted private key is null or empty.
        /// </summary>
        /// <param name="SECP521EncryptedPrivateKey">A SECP521 encrypted private key.</param>
        public static Boolean IsNotNullOrEmpty(this SECP521EncryptedPrivateKey? SECP521EncryptedPrivateKey)
            => SECP521EncryptedPrivateKey.HasValue && SECP521EncryptedPrivateKey.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// A SECP521 encrypted private key.
    /// 
    /// xs:base64Binary, length: 94
    /// </summary>
    public readonly struct SECP521EncryptedPrivateKey : IId,
                                         IEquatable<SECP521EncryptedPrivateKey>,
                                         IComparable<SECP521EncryptedPrivateKey>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the SECP521 encrypted private key.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new SECP521 encrypted private key based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a SECP521 encrypted private key.</param>
        private SECP521EncryptedPrivateKey(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a SECP521 encrypted private key.
        /// </summary>
        /// <param name="Text">A text representation of a SECP521 encrypted private key.</param>
        public static SECP521EncryptedPrivateKey Parse(String Text)
        {

            if (TryParse(Text, out var dhPublicKey))
                return dhPublicKey;

            throw new ArgumentException("Invalid text representation of a SECP521 encrypted private key: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a SECP521 encrypted private key.
        /// </summary>
        /// <param name="Text">A text representation of a SECP521 encrypted private key.</param>
        public static SECP521EncryptedPrivateKey? TryParse(String Text)
        {

            if (TryParse(Text, out var dhPublicKey))
                return dhPublicKey;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out SECP521EncryptedPrivateKey)

        /// <summary>
        /// Try to parse the given text as a SECP521 encrypted private key.
        /// </summary>
        /// <param name="Text">A text representation of a SECP521 encrypted private key.</param>
        /// <param name="SECP521EncryptedPrivateKey">The parsed SECP521 encrypted private key.</param>
        public static Boolean TryParse(String Text, out SECP521EncryptedPrivateKey SECP521EncryptedPrivateKey)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                SECP521EncryptedPrivateKey = new SECP521EncryptedPrivateKey(Text);
                return true;
            }

            SECP521EncryptedPrivateKey = default;
            return false;

        }

        #endregion

        #region Clone

        /// <summary>
        /// Clone this SECP521 encrypted private key.
        /// </summary>
        public SECP521EncryptedPrivateKey Clone

            => new(
                   new String(InternalId?.ToCharArray())
               );

        #endregion


        #region Operator overloading

        #region Operator == (SECP521EncryptedPrivateKey1, SECP521EncryptedPrivateKey2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="SECP521EncryptedPrivateKey1">A SECP521 encrypted private key.</param>
        /// <param name="SECP521EncryptedPrivateKey2">Another SECP521 encrypted private key.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (SECP521EncryptedPrivateKey SECP521EncryptedPrivateKey1,
                                           SECP521EncryptedPrivateKey SECP521EncryptedPrivateKey2)

            => SECP521EncryptedPrivateKey1.Equals(SECP521EncryptedPrivateKey2);

        #endregion

        #region Operator != (SECP521EncryptedPrivateKey1, SECP521EncryptedPrivateKey2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="SECP521EncryptedPrivateKey1">A SECP521 encrypted private key.</param>
        /// <param name="SECP521EncryptedPrivateKey2">Another SECP521 encrypted private key.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (SECP521EncryptedPrivateKey SECP521EncryptedPrivateKey1,
                                           SECP521EncryptedPrivateKey SECP521EncryptedPrivateKey2)

            => !SECP521EncryptedPrivateKey1.Equals(SECP521EncryptedPrivateKey2);

        #endregion

        #region Operator <  (SECP521EncryptedPrivateKey1, SECP521EncryptedPrivateKey2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="SECP521EncryptedPrivateKey1">A SECP521 encrypted private key.</param>
        /// <param name="SECP521EncryptedPrivateKey2">Another SECP521 encrypted private key.</param>
        /// <returns>true|false</returns>
        public static Boolean operator < (SECP521EncryptedPrivateKey SECP521EncryptedPrivateKey1,
                                          SECP521EncryptedPrivateKey SECP521EncryptedPrivateKey2)

            => SECP521EncryptedPrivateKey1.CompareTo(SECP521EncryptedPrivateKey2) < 0;

        #endregion

        #region Operator <= (SECP521EncryptedPrivateKey1, SECP521EncryptedPrivateKey2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="SECP521EncryptedPrivateKey1">A SECP521 encrypted private key.</param>
        /// <param name="SECP521EncryptedPrivateKey2">Another SECP521 encrypted private key.</param>
        /// <returns>true|false</returns>
        public static Boolean operator <= (SECP521EncryptedPrivateKey SECP521EncryptedPrivateKey1,
                                           SECP521EncryptedPrivateKey SECP521EncryptedPrivateKey2)

            => SECP521EncryptedPrivateKey1.CompareTo(SECP521EncryptedPrivateKey2) <= 0;

        #endregion

        #region Operator >  (SECP521EncryptedPrivateKey1, SECP521EncryptedPrivateKey2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="SECP521EncryptedPrivateKey1">A SECP521 encrypted private key.</param>
        /// <param name="SECP521EncryptedPrivateKey2">Another SECP521 encrypted private key.</param>
        /// <returns>true|false</returns>
        public static Boolean operator > (SECP521EncryptedPrivateKey SECP521EncryptedPrivateKey1,
                                          SECP521EncryptedPrivateKey SECP521EncryptedPrivateKey2)

            => SECP521EncryptedPrivateKey1.CompareTo(SECP521EncryptedPrivateKey2) > 0;

        #endregion

        #region Operator >= (SECP521EncryptedPrivateKey1, SECP521EncryptedPrivateKey2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="SECP521EncryptedPrivateKey1">A SECP521 encrypted private key.</param>
        /// <param name="SECP521EncryptedPrivateKey2">Another SECP521 encrypted private key.</param>
        /// <returns>true|false</returns>
        public static Boolean operator >= (SECP521EncryptedPrivateKey SECP521EncryptedPrivateKey1,
                                           SECP521EncryptedPrivateKey SECP521EncryptedPrivateKey2)

            => SECP521EncryptedPrivateKey1.CompareTo(SECP521EncryptedPrivateKey2) >= 0;

        #endregion

        #endregion

        #region IComparable<SECP521EncryptedPrivateKey> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two SECP521 encrypted private keys.
        /// </summary>
        /// <param name="Object">A SECP521 encrypted private key to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is SECP521EncryptedPrivateKey dhPublicKey
                   ? CompareTo(dhPublicKey)
                   : throw new ArgumentException("The given object is not a SECP521 encrypted private key!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(SECP521EncryptedPrivateKey)

        /// <summary>
        /// Compares two SECP521 encrypted private keys.
        /// </summary>
        /// <param name="SECP521EncryptedPrivateKey">A SECP521 encrypted private key to compare with.</param>
        public Int32 CompareTo(SECP521EncryptedPrivateKey SECP521EncryptedPrivateKey)

            => String.Compare(InternalId,
                              SECP521EncryptedPrivateKey.InternalId,
                              StringComparison.Ordinal);

        #endregion

        #endregion

        #region IEquatable<SECP521EncryptedPrivateKey> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two SECP521 encrypted private keys for equality.
        /// </summary>
        /// <param name="Object">A SECP521 encrypted private key to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is SECP521EncryptedPrivateKey dhPublicKey &&
                   Equals(dhPublicKey);

        #endregion

        #region Equals(SECP521EncryptedPrivateKey)

        /// <summary>
        /// Compares two SECP521 encrypted private keys for equality.
        /// </summary>
        /// <param name="SECP521EncryptedPrivateKey">A SECP521 encrypted private key to compare with.</param>
        public Boolean Equals(SECP521EncryptedPrivateKey SECP521EncryptedPrivateKey)

            => String.Equals(InternalId,
                             SECP521EncryptedPrivateKey.InternalId,
                             StringComparison.Ordinal);

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        /// <returns>The hash code of this object.</returns>
        public override Int32 GetHashCode()

            => InternalId?.GetHashCode() ?? 0;

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => InternalId ?? "";

        #endregion

    }

}
