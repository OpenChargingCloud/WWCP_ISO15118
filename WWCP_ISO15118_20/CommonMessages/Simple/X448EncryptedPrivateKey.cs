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
    /// Extention methods for X448 encrypted private keys.
    /// </summary>
    public static class X448EncryptedPrivateKeyExtensions
    {

        /// <summary>
        /// Indicates whether this X448 encrypted private key is null or empty.
        /// </summary>
        /// <param name="X448EncryptedPrivateKey">A X448 encrypted private key.</param>
        public static Boolean IsNullOrEmpty(this X448EncryptedPrivateKey? X448EncryptedPrivateKey)
            => !X448EncryptedPrivateKey.HasValue || X448EncryptedPrivateKey.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this X448 encrypted private key is null or empty.
        /// </summary>
        /// <param name="X448EncryptedPrivateKey">A X448 encrypted private key.</param>
        public static Boolean IsNotNullOrEmpty(this X448EncryptedPrivateKey? X448EncryptedPrivateKey)
            => X448EncryptedPrivateKey.HasValue && X448EncryptedPrivateKey.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// A X448 encrypted private key.
    /// 
    /// xs:base64Binary, length: 84
    /// </summary>
    public readonly struct X448EncryptedPrivateKey : IId,
                                         IEquatable<X448EncryptedPrivateKey>,
                                         IComparable<X448EncryptedPrivateKey>
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
        /// The length of the X448 encrypted private key.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new X448 encrypted private key based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a X448 encrypted private key.</param>
        private X448EncryptedPrivateKey(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a X448 encrypted private key.
        /// </summary>
        /// <param name="Text">A text representation of a X448 encrypted private key.</param>
        public static X448EncryptedPrivateKey Parse(String Text)
        {

            if (TryParse(Text, out var dhPublicKey))
                return dhPublicKey;

            throw new ArgumentException("Invalid text representation of a X448 encrypted private key: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a X448 encrypted private key.
        /// </summary>
        /// <param name="Text">A text representation of a X448 encrypted private key.</param>
        public static X448EncryptedPrivateKey? TryParse(String Text)
        {

            if (TryParse(Text, out var dhPublicKey))
                return dhPublicKey;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out X448EncryptedPrivateKey)

        /// <summary>
        /// Try to parse the given text as a X448 encrypted private key.
        /// </summary>
        /// <param name="Text">A text representation of a X448 encrypted private key.</param>
        /// <param name="X448EncryptedPrivateKey">The parsed X448 encrypted private key.</param>
        public static Boolean TryParse(String Text, out X448EncryptedPrivateKey X448EncryptedPrivateKey)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                X448EncryptedPrivateKey = new X448EncryptedPrivateKey(Text);
                return true;
            }

            X448EncryptedPrivateKey = default;
            return false;

        }

        #endregion

        #region Clone

        /// <summary>
        /// Clone this X448 encrypted private key.
        /// </summary>
        public X448EncryptedPrivateKey Clone

            => new(
                   new String(InternalId?.ToCharArray())
               );

        #endregion


        #region Operator overloading

        #region Operator == (X448EncryptedPrivateKey1, X448EncryptedPrivateKey2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="X448EncryptedPrivateKey1">A X448 encrypted private key.</param>
        /// <param name="X448EncryptedPrivateKey2">Another X448 encrypted private key.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (X448EncryptedPrivateKey X448EncryptedPrivateKey1,
                                           X448EncryptedPrivateKey X448EncryptedPrivateKey2)

            => X448EncryptedPrivateKey1.Equals(X448EncryptedPrivateKey2);

        #endregion

        #region Operator != (X448EncryptedPrivateKey1, X448EncryptedPrivateKey2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="X448EncryptedPrivateKey1">A X448 encrypted private key.</param>
        /// <param name="X448EncryptedPrivateKey2">Another X448 encrypted private key.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (X448EncryptedPrivateKey X448EncryptedPrivateKey1,
                                           X448EncryptedPrivateKey X448EncryptedPrivateKey2)

            => !X448EncryptedPrivateKey1.Equals(X448EncryptedPrivateKey2);

        #endregion

        #region Operator <  (X448EncryptedPrivateKey1, X448EncryptedPrivateKey2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="X448EncryptedPrivateKey1">A X448 encrypted private key.</param>
        /// <param name="X448EncryptedPrivateKey2">Another X448 encrypted private key.</param>
        /// <returns>true|false</returns>
        public static Boolean operator < (X448EncryptedPrivateKey X448EncryptedPrivateKey1,
                                          X448EncryptedPrivateKey X448EncryptedPrivateKey2)

            => X448EncryptedPrivateKey1.CompareTo(X448EncryptedPrivateKey2) < 0;

        #endregion

        #region Operator <= (X448EncryptedPrivateKey1, X448EncryptedPrivateKey2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="X448EncryptedPrivateKey1">A X448 encrypted private key.</param>
        /// <param name="X448EncryptedPrivateKey2">Another X448 encrypted private key.</param>
        /// <returns>true|false</returns>
        public static Boolean operator <= (X448EncryptedPrivateKey X448EncryptedPrivateKey1,
                                           X448EncryptedPrivateKey X448EncryptedPrivateKey2)

            => X448EncryptedPrivateKey1.CompareTo(X448EncryptedPrivateKey2) <= 0;

        #endregion

        #region Operator >  (X448EncryptedPrivateKey1, X448EncryptedPrivateKey2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="X448EncryptedPrivateKey1">A X448 encrypted private key.</param>
        /// <param name="X448EncryptedPrivateKey2">Another X448 encrypted private key.</param>
        /// <returns>true|false</returns>
        public static Boolean operator > (X448EncryptedPrivateKey X448EncryptedPrivateKey1,
                                          X448EncryptedPrivateKey X448EncryptedPrivateKey2)

            => X448EncryptedPrivateKey1.CompareTo(X448EncryptedPrivateKey2) > 0;

        #endregion

        #region Operator >= (X448EncryptedPrivateKey1, X448EncryptedPrivateKey2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="X448EncryptedPrivateKey1">A X448 encrypted private key.</param>
        /// <param name="X448EncryptedPrivateKey2">Another X448 encrypted private key.</param>
        /// <returns>true|false</returns>
        public static Boolean operator >= (X448EncryptedPrivateKey X448EncryptedPrivateKey1,
                                           X448EncryptedPrivateKey X448EncryptedPrivateKey2)

            => X448EncryptedPrivateKey1.CompareTo(X448EncryptedPrivateKey2) >= 0;

        #endregion

        #endregion

        #region IComparable<X448EncryptedPrivateKey> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two X448 encrypted private keys.
        /// </summary>
        /// <param name="Object">A X448 encrypted private key to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is X448EncryptedPrivateKey dhPublicKey
                   ? CompareTo(dhPublicKey)
                   : throw new ArgumentException("The given object is not a X448 encrypted private key!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(X448EncryptedPrivateKey)

        /// <summary>
        /// Compares two X448 encrypted private keys.
        /// </summary>
        /// <param name="X448EncryptedPrivateKey">A X448 encrypted private key to compare with.</param>
        public Int32 CompareTo(X448EncryptedPrivateKey X448EncryptedPrivateKey)

            => String.Compare(InternalId,
                              X448EncryptedPrivateKey.InternalId,
                              StringComparison.Ordinal);

        #endregion

        #endregion

        #region IEquatable<X448EncryptedPrivateKey> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two X448 encrypted private keys for equality.
        /// </summary>
        /// <param name="Object">A X448 encrypted private key to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is X448EncryptedPrivateKey dhPublicKey &&
                   Equals(dhPublicKey);

        #endregion

        #region Equals(X448EncryptedPrivateKey)

        /// <summary>
        /// Compares two X448 encrypted private keys for equality.
        /// </summary>
        /// <param name="X448EncryptedPrivateKey">A X448 encrypted private key to compare with.</param>
        public Boolean Equals(X448EncryptedPrivateKey X448EncryptedPrivateKey)

            => String.Equals(InternalId,
                             X448EncryptedPrivateKey.InternalId,
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
