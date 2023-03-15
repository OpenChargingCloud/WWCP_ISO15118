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
    /// Extention methods for DH public keys.
    /// </summary>
    public static class DHPublicKeyExtensions
    {

        /// <summary>
        /// Indicates whether this DH public key is null or empty.
        /// </summary>
        /// <param name="DHPublicKey">A DH public key.</param>
        public static Boolean IsNullOrEmpty(this DHPublicKey? DHPublicKey)
            => !DHPublicKey.HasValue || DHPublicKey.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this DH public key is null or empty.
        /// </summary>
        /// <param name="DHPublicKey">A DH public key.</param>
        public static Boolean IsNotNullOrEmpty(this DHPublicKey? DHPublicKey)
            => DHPublicKey.HasValue && DHPublicKey.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// A DH public key.
    /// 
    /// xs:base64Binary, length: 133
    /// </summary>
    public readonly struct DHPublicKey : IId,
                                         IEquatable<DHPublicKey>,
                                         IComparable<DHPublicKey>
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
        /// The length of the DH public key.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new DH public key based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a DH public key.</param>
        private DHPublicKey(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a DH public key.
        /// </summary>
        /// <param name="Text">A text representation of a DH public key.</param>
        public static DHPublicKey Parse(String Text)
        {

            if (TryParse(Text, out var dhPublicKey))
                return dhPublicKey;

            throw new ArgumentException("Invalid text representation of a DH public key: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a DH public key.
        /// </summary>
        /// <param name="Text">A text representation of a DH public key.</param>
        public static DHPublicKey? TryParse(String Text)
        {

            if (TryParse(Text, out var dhPublicKey))
                return dhPublicKey;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out DHPublicKey)

        /// <summary>
        /// Try to parse the given text as a DH public key.
        /// </summary>
        /// <param name="Text">A text representation of a DH public key.</param>
        /// <param name="DHPublicKey">The parsed DH public key.</param>
        public static Boolean TryParse(String Text, out DHPublicKey DHPublicKey)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                DHPublicKey = new DHPublicKey(Text);
                return true;
            }

            DHPublicKey = default;
            return false;

        }

        #endregion

        #region Clone

        /// <summary>
        /// Clone this DH public key.
        /// </summary>
        public DHPublicKey Clone

            => new(
                   new String(InternalId?.ToCharArray())
               );

        #endregion


        #region Operator overloading

        #region Operator == (DHPublicKey1, DHPublicKey2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="DHPublicKey1">A DH public key.</param>
        /// <param name="DHPublicKey2">Another DH public key.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (DHPublicKey DHPublicKey1,
                                           DHPublicKey DHPublicKey2)

            => DHPublicKey1.Equals(DHPublicKey2);

        #endregion

        #region Operator != (DHPublicKey1, DHPublicKey2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="DHPublicKey1">A DH public key.</param>
        /// <param name="DHPublicKey2">Another DH public key.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (DHPublicKey DHPublicKey1,
                                           DHPublicKey DHPublicKey2)

            => !DHPublicKey1.Equals(DHPublicKey2);

        #endregion

        #region Operator <  (DHPublicKey1, DHPublicKey2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="DHPublicKey1">A DH public key.</param>
        /// <param name="DHPublicKey2">Another DH public key.</param>
        /// <returns>true|false</returns>
        public static Boolean operator < (DHPublicKey DHPublicKey1,
                                          DHPublicKey DHPublicKey2)

            => DHPublicKey1.CompareTo(DHPublicKey2) < 0;

        #endregion

        #region Operator <= (DHPublicKey1, DHPublicKey2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="DHPublicKey1">A DH public key.</param>
        /// <param name="DHPublicKey2">Another DH public key.</param>
        /// <returns>true|false</returns>
        public static Boolean operator <= (DHPublicKey DHPublicKey1,
                                           DHPublicKey DHPublicKey2)

            => DHPublicKey1.CompareTo(DHPublicKey2) <= 0;

        #endregion

        #region Operator >  (DHPublicKey1, DHPublicKey2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="DHPublicKey1">A DH public key.</param>
        /// <param name="DHPublicKey2">Another DH public key.</param>
        /// <returns>true|false</returns>
        public static Boolean operator > (DHPublicKey DHPublicKey1,
                                          DHPublicKey DHPublicKey2)

            => DHPublicKey1.CompareTo(DHPublicKey2) > 0;

        #endregion

        #region Operator >= (DHPublicKey1, DHPublicKey2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="DHPublicKey1">A DH public key.</param>
        /// <param name="DHPublicKey2">Another DH public key.</param>
        /// <returns>true|false</returns>
        public static Boolean operator >= (DHPublicKey DHPublicKey1,
                                           DHPublicKey DHPublicKey2)

            => DHPublicKey1.CompareTo(DHPublicKey2) >= 0;

        #endregion

        #endregion

        #region IComparable<DHPublicKey> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two DH public keys.
        /// </summary>
        /// <param name="Object">A DH public key to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is DHPublicKey dhPublicKey
                   ? CompareTo(dhPublicKey)
                   : throw new ArgumentException("The given object is not a DH public key!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(DHPublicKey)

        /// <summary>
        /// Compares two DH public keys.
        /// </summary>
        /// <param name="DHPublicKey">A DH public key to compare with.</param>
        public Int32 CompareTo(DHPublicKey DHPublicKey)

            => String.Compare(InternalId,
                              DHPublicKey.InternalId,
                              StringComparison.Ordinal);

        #endregion

        #endregion

        #region IEquatable<DHPublicKey> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two DH public keys for equality.
        /// </summary>
        /// <param name="Object">A DH public key to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is DHPublicKey dhPublicKey &&
                   Equals(dhPublicKey);

        #endregion

        #region Equals(DHPublicKey)

        /// <summary>
        /// Compares two DH public keys for equality.
        /// </summary>
        /// <param name="DHPublicKey">A DH public key to compare with.</param>
        public Boolean Equals(DHPublicKey DHPublicKey)

            => String.Equals(InternalId,
                             DHPublicKey.InternalId,
                             StringComparison.Ordinal);

        #endregion

        #endregion

        #region GetHashCode()

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
