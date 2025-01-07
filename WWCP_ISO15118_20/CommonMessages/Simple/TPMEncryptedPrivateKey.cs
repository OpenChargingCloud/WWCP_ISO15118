/*
 * Copyright (c) 2021-2025 GraphDefined GmbH <achim.friedland@graphdefined.com>
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
    /// Extension methods for TPM encrypted private keys.
    /// </summary>
    public static class TPMEncryptedPrivateKeyExtensions
    {

        /// <summary>
        /// Indicates whether this TPM encrypted private key is null or empty.
        /// </summary>
        /// <param name="TPMEncryptedPrivateKey">A TPM encrypted private key.</param>
        public static Boolean IsNullOrEmpty(this TPMEncryptedPrivateKey? TPMEncryptedPrivateKey)
            => !TPMEncryptedPrivateKey.HasValue || TPMEncryptedPrivateKey.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this TPM encrypted private key is null or empty.
        /// </summary>
        /// <param name="TPMEncryptedPrivateKey">A TPM encrypted private key.</param>
        public static Boolean IsNotNullOrEmpty(this TPMEncryptedPrivateKey? TPMEncryptedPrivateKey)
            => TPMEncryptedPrivateKey.HasValue && TPMEncryptedPrivateKey.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// A TPM encrypted private key.
    /// 
    /// xs:base64Binary, length: 206
    /// </summary>
    public readonly struct TPMEncryptedPrivateKey : IId,
                                                    IEquatable<TPMEncryptedPrivateKey>,
                                                    IComparable<TPMEncryptedPrivateKey>
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
        /// The length of the TPM encrypted private key.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new TPM encrypted private key based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a TPM encrypted private key.</param>
        private TPMEncryptedPrivateKey(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a TPM encrypted private key.
        /// </summary>
        /// <param name="Text">A text representation of a TPM encrypted private key.</param>
        public static TPMEncryptedPrivateKey Parse(String Text)
        {

            if (TryParse(Text, out var dhPublicKey))
                return dhPublicKey;

            throw new ArgumentException("Invalid text representation of a TPM encrypted private key: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a TPM encrypted private key.
        /// </summary>
        /// <param name="Text">A text representation of a TPM encrypted private key.</param>
        public static TPMEncryptedPrivateKey? TryParse(String Text)
        {

            if (TryParse(Text, out var dhPublicKey))
                return dhPublicKey;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out TPMEncryptedPrivateKey)

        /// <summary>
        /// Try to parse the given text as a TPM encrypted private key.
        /// </summary>
        /// <param name="Text">A text representation of a TPM encrypted private key.</param>
        /// <param name="TPMEncryptedPrivateKey">The parsed TPM encrypted private key.</param>
        public static Boolean TryParse(String Text, out TPMEncryptedPrivateKey TPMEncryptedPrivateKey)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                TPMEncryptedPrivateKey = new TPMEncryptedPrivateKey(Text);
                return true;
            }

            TPMEncryptedPrivateKey = default;
            return false;

        }

        #endregion

        #region Clone

        /// <summary>
        /// Clone this TPM encrypted private key.
        /// </summary>
        public TPMEncryptedPrivateKey Clone

            => new(
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (TPMEncryptedPrivateKey1, TPMEncryptedPrivateKey2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="TPMEncryptedPrivateKey1">A TPM encrypted private key.</param>
        /// <param name="TPMEncryptedPrivateKey2">Another TPM encrypted private key.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (TPMEncryptedPrivateKey TPMEncryptedPrivateKey1,
                                           TPMEncryptedPrivateKey TPMEncryptedPrivateKey2)

            => TPMEncryptedPrivateKey1.Equals(TPMEncryptedPrivateKey2);

        #endregion

        #region Operator != (TPMEncryptedPrivateKey1, TPMEncryptedPrivateKey2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="TPMEncryptedPrivateKey1">A TPM encrypted private key.</param>
        /// <param name="TPMEncryptedPrivateKey2">Another TPM encrypted private key.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (TPMEncryptedPrivateKey TPMEncryptedPrivateKey1,
                                           TPMEncryptedPrivateKey TPMEncryptedPrivateKey2)

            => !TPMEncryptedPrivateKey1.Equals(TPMEncryptedPrivateKey2);

        #endregion

        #region Operator <  (TPMEncryptedPrivateKey1, TPMEncryptedPrivateKey2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="TPMEncryptedPrivateKey1">A TPM encrypted private key.</param>
        /// <param name="TPMEncryptedPrivateKey2">Another TPM encrypted private key.</param>
        /// <returns>true|false</returns>
        public static Boolean operator < (TPMEncryptedPrivateKey TPMEncryptedPrivateKey1,
                                          TPMEncryptedPrivateKey TPMEncryptedPrivateKey2)

            => TPMEncryptedPrivateKey1.CompareTo(TPMEncryptedPrivateKey2) < 0;

        #endregion

        #region Operator <= (TPMEncryptedPrivateKey1, TPMEncryptedPrivateKey2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="TPMEncryptedPrivateKey1">A TPM encrypted private key.</param>
        /// <param name="TPMEncryptedPrivateKey2">Another TPM encrypted private key.</param>
        /// <returns>true|false</returns>
        public static Boolean operator <= (TPMEncryptedPrivateKey TPMEncryptedPrivateKey1,
                                           TPMEncryptedPrivateKey TPMEncryptedPrivateKey2)

            => TPMEncryptedPrivateKey1.CompareTo(TPMEncryptedPrivateKey2) <= 0;

        #endregion

        #region Operator >  (TPMEncryptedPrivateKey1, TPMEncryptedPrivateKey2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="TPMEncryptedPrivateKey1">A TPM encrypted private key.</param>
        /// <param name="TPMEncryptedPrivateKey2">Another TPM encrypted private key.</param>
        /// <returns>true|false</returns>
        public static Boolean operator > (TPMEncryptedPrivateKey TPMEncryptedPrivateKey1,
                                          TPMEncryptedPrivateKey TPMEncryptedPrivateKey2)

            => TPMEncryptedPrivateKey1.CompareTo(TPMEncryptedPrivateKey2) > 0;

        #endregion

        #region Operator >= (TPMEncryptedPrivateKey1, TPMEncryptedPrivateKey2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="TPMEncryptedPrivateKey1">A TPM encrypted private key.</param>
        /// <param name="TPMEncryptedPrivateKey2">Another TPM encrypted private key.</param>
        /// <returns>true|false</returns>
        public static Boolean operator >= (TPMEncryptedPrivateKey TPMEncryptedPrivateKey1,
                                           TPMEncryptedPrivateKey TPMEncryptedPrivateKey2)

            => TPMEncryptedPrivateKey1.CompareTo(TPMEncryptedPrivateKey2) >= 0;

        #endregion

        #endregion

        #region IComparable<TPMEncryptedPrivateKey> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two TPM encrypted private keys.
        /// </summary>
        /// <param name="Object">A TPM encrypted private key to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is TPMEncryptedPrivateKey dhPublicKey
                   ? CompareTo(dhPublicKey)
                   : throw new ArgumentException("The given object is not a TPM encrypted private key!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(TPMEncryptedPrivateKey)

        /// <summary>
        /// Compares two TPM encrypted private keys.
        /// </summary>
        /// <param name="TPMEncryptedPrivateKey">A TPM encrypted private key to compare with.</param>
        public Int32 CompareTo(TPMEncryptedPrivateKey TPMEncryptedPrivateKey)

            => String.Compare(InternalId,
                              TPMEncryptedPrivateKey.InternalId,
                              StringComparison.Ordinal);

        #endregion

        #endregion

        #region IEquatable<TPMEncryptedPrivateKey> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two TPM encrypted private keys for equality.
        /// </summary>
        /// <param name="Object">A TPM encrypted private key to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is TPMEncryptedPrivateKey dhPublicKey &&
                   Equals(dhPublicKey);

        #endregion

        #region Equals(TPMEncryptedPrivateKey)

        /// <summary>
        /// Compares two TPM encrypted private keys for equality.
        /// </summary>
        /// <param name="TPMEncryptedPrivateKey">A TPM encrypted private key to compare with.</param>
        public Boolean Equals(TPMEncryptedPrivateKey TPMEncryptedPrivateKey)

            => String.Equals(InternalId,
                             TPMEncryptedPrivateKey.InternalId,
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
