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
    /// Extention methods for provider identifications.
    /// </summary>
    public static class ProviderIdExtensions
    {

        /// <summary>
        /// Indicates whether this provider identification is null or empty.
        /// </summary>
        /// <param name="ProviderId">A provider identification.</param>
        public static Boolean IsNullOrEmpty(this Provider_Id? ProviderId)
            => !ProviderId.HasValue || ProviderId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this provider identification is null or empty.
        /// </summary>
        /// <param name="ProviderId">A provider identification.</param>
        public static Boolean IsNotNullOrEmpty(this Provider_Id? ProviderId)
            => ProviderId.HasValue && ProviderId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// A provider identification.
    /// [max 80]
    /// </summary>
    public readonly struct Provider_Id : IId,
                                         IEquatable<Provider_Id>,
                                         IComparable<Provider_Id>
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
        /// The length of the provider identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new provider identification based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a provider identification.</param>
        private Provider_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a provider identification.
        /// </summary>
        /// <param name="Text">A text representation of a provider identification.</param>
        public static Provider_Id Parse(String Text)
        {

            if (TryParse(Text, out var evseId))
                return evseId;

            throw new ArgumentException("Invalid text representation of a provider identification: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a provider identification.
        /// </summary>
        /// <param name="Text">A text representation of a provider identification.</param>
        public static Provider_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var evseId))
                return evseId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ProviderId)

        /// <summary>
        /// Try to parse the given text as a provider identification.
        /// </summary>
        /// <param name="Text">A text representation of a provider identification.</param>
        /// <param name="ProviderId">The parsed provider identification.</param>
        public static Boolean TryParse(String Text, out Provider_Id ProviderId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                ProviderId = new Provider_Id(Text);
                return true;
            }

            ProviderId = default;
            return false;

        }

        #endregion

        #region Clone

        /// <summary>
        /// Clone this provider identification.
        /// </summary>
        public Provider_Id Clone

            => new(
                   new String(InternalId?.ToCharArray())
               );

        #endregion


        #region Operator overloading

        #region Operator == (evseId1, evseId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="evseId1">A provider identification.</param>
        /// <param name="evseId2">Another provider identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (Provider_Id evseId1,
                                           Provider_Id evseId2)

            => evseId1.Equals(evseId2);

        #endregion

        #region Operator != (evseId1, evseId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="evseId1">A provider identification.</param>
        /// <param name="evseId2">Another provider identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (Provider_Id evseId1,
                                           Provider_Id evseId2)

            => !evseId1.Equals(evseId2);

        #endregion

        #region Operator <  (evseId1, evseId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="evseId1">A provider identification.</param>
        /// <param name="evseId2">Another provider identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator < (Provider_Id evseId1,
                                          Provider_Id evseId2)

            => evseId1.CompareTo(evseId2) < 0;

        #endregion

        #region Operator <= (evseId1, evseId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="evseId1">A provider identification.</param>
        /// <param name="evseId2">Another provider identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator <= (Provider_Id evseId1,
                                           Provider_Id evseId2)

            => evseId1.CompareTo(evseId2) <= 0;

        #endregion

        #region Operator >  (evseId1, evseId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="evseId1">A provider identification.</param>
        /// <param name="evseId2">Another provider identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator > (Provider_Id evseId1,
                                          Provider_Id evseId2)

            => evseId1.CompareTo(evseId2) > 0;

        #endregion

        #region Operator >= (evseId1, evseId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="evseId1">A provider identification.</param>
        /// <param name="evseId2">Another provider identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator >= (Provider_Id evseId1,
                                           Provider_Id evseId2)

            => evseId1.CompareTo(evseId2) >= 0;

        #endregion

        #endregion

        #region IComparable<ProviderId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two provider identifications.
        /// </summary>
        /// <param name="Object">A provider identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is Provider_Id evseId
                   ? CompareTo(evseId)
                   : throw new ArgumentException("The given object is not a provider identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ProviderId)

        /// <summary>
        /// Compares two provider identifications.
        /// </summary>
        /// <param name="ProviderId">A provider identification to compare with.</param>
        public Int32 CompareTo(Provider_Id ProviderId)

            => String.Compare(InternalId,
                              ProviderId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ProviderId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two provider identifications for equality.
        /// </summary>
        /// <param name="Object">A provider identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is Provider_Id evseId &&
                   Equals(evseId);

        #endregion

        #region Equals(ProviderId)

        /// <summary>
        /// Compares two provider identifications for equality.
        /// </summary>
        /// <param name="ProviderId">A provider identification to compare with.</param>
        public Boolean Equals(Provider_Id ProviderId)

            => String.Equals(InternalId,
                             ProviderId.InternalId,
                             StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        /// <returns>The hash code of this object.</returns>
        public override Int32 GetHashCode()

            => InternalId?.ToLower().GetHashCode() ?? 0;

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
