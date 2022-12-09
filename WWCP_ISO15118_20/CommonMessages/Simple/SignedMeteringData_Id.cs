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

#region Usings

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// Extention methods for signed metering data identifications.
    /// </summary>
    public static class SignedMeteringDataIdExtensions
    {

        /// <summary>
        /// Indicates whether this signed metering data identification is null or empty.
        /// </summary>
        /// <param name="SignedMeteringDataId">A signed metering data identification.</param>
        public static Boolean IsNullOrEmpty(this SignedMeteringData_Id? SignedMeteringDataId)
            => !SignedMeteringDataId.HasValue || SignedMeteringDataId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this signed metering data identification is null or empty.
        /// </summary>
        /// <param name="SignedMeteringDataId">A signed metering data identification.</param>
        public static Boolean IsNotNullOrEmpty(this SignedMeteringData_Id? SignedMeteringDataId)
            => SignedMeteringDataId.HasValue && SignedMeteringDataId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// A signed metering data identification.
    /// 
    /// xs:pattern value="[\i-[:]][\c-[:]]*"/
    /// </summary>
    public readonly struct SignedMeteringData_Id : IId,
                                                   IEquatable<SignedMeteringData_Id>,
                                                   IComparable<SignedMeteringData_Id>
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
        /// The length of the signed metering data identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new signed metering data identification based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a signed metering data identification.</param>
        private SignedMeteringData_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a signed metering data identification.
        /// </summary>
        /// <param name="Text">A text representation of a signed metering data identification.</param>
        public static SignedMeteringData_Id Parse(String Text)
        {

            if (TryParse(Text, out var priceAlgorithmId))
                return priceAlgorithmId;

            throw new ArgumentException("Invalid text representation of a signed metering data identification: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a signed metering data identification.
        /// </summary>
        /// <param name="Text">A text representation of a signed metering data identification.</param>
        public static SignedMeteringData_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var priceAlgorithmId))
                return priceAlgorithmId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out SignedMeteringDataId)

        /// <summary>
        /// Try to parse the given text as a signed metering data identification.
        /// </summary>
        /// <param name="Text">A text representation of a signed metering data identification.</param>
        /// <param name="SignedMeteringDataId">The parsed signed metering data identification.</param>
        public static Boolean TryParse(String Text, out SignedMeteringData_Id SignedMeteringDataId)
        {

            Text = Text.Trim();

            //ToDo: xs:hexBinary, length: 8

            if (Text.IsNotNullOrEmpty())
            {
                SignedMeteringDataId = new SignedMeteringData_Id(Text);
                return true;
            }

            SignedMeteringDataId = default;
            return false;

        }

        #endregion

        #region Clone

        /// <summary>
        /// Clone this signed metering data identification.
        /// </summary>
        public SignedMeteringData_Id Clone

            => new(
                   new String(InternalId?.ToCharArray())
               );

        #endregion


        #region Operator overloading

        #region Operator == (SignedMeteringDataId1, SignedMeteringDataId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="SignedMeteringDataId1">A signed metering data identification.</param>
        /// <param name="SignedMeteringDataId2">Another signed metering data identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (SignedMeteringData_Id SignedMeteringDataId1,
                                           SignedMeteringData_Id SignedMeteringDataId2)

            => SignedMeteringDataId1.Equals(SignedMeteringDataId2);

        #endregion

        #region Operator != (SignedMeteringDataId1, SignedMeteringDataId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="SignedMeteringDataId1">A signed metering data identification.</param>
        /// <param name="SignedMeteringDataId2">Another signed metering data identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (SignedMeteringData_Id SignedMeteringDataId1,
                                           SignedMeteringData_Id SignedMeteringDataId2)

            => !SignedMeteringDataId1.Equals(SignedMeteringDataId2);

        #endregion

        #region Operator <  (SignedMeteringDataId1, SignedMeteringDataId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="SignedMeteringDataId1">A signed metering data identification.</param>
        /// <param name="SignedMeteringDataId2">Another signed metering data identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator < (SignedMeteringData_Id SignedMeteringDataId1,
                                          SignedMeteringData_Id SignedMeteringDataId2)

            => SignedMeteringDataId1.CompareTo(SignedMeteringDataId2) < 0;

        #endregion

        #region Operator <= (SignedMeteringDataId1, SignedMeteringDataId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="SignedMeteringDataId1">A signed metering data identification.</param>
        /// <param name="SignedMeteringDataId2">Another signed metering data identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator <= (SignedMeteringData_Id SignedMeteringDataId1,
                                           SignedMeteringData_Id SignedMeteringDataId2)

            => SignedMeteringDataId1.CompareTo(SignedMeteringDataId2) <= 0;

        #endregion

        #region Operator >  (SignedMeteringDataId1, SignedMeteringDataId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="SignedMeteringDataId1">A signed metering data identification.</param>
        /// <param name="SignedMeteringDataId2">Another signed metering data identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator > (SignedMeteringData_Id SignedMeteringDataId1,
                                          SignedMeteringData_Id SignedMeteringDataId2)

            => SignedMeteringDataId1.CompareTo(SignedMeteringDataId2) > 0;

        #endregion

        #region Operator >= (SignedMeteringDataId1, SignedMeteringDataId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="SignedMeteringDataId1">A signed metering data identification.</param>
        /// <param name="SignedMeteringDataId2">Another signed metering data identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator >= (SignedMeteringData_Id SignedMeteringDataId1,
                                           SignedMeteringData_Id SignedMeteringDataId2)

            => SignedMeteringDataId1.CompareTo(SignedMeteringDataId2) >= 0;

        #endregion

        #endregion

        #region IComparable<SignedMeteringDataId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two signed metering data identifications.
        /// </summary>
        /// <param name="Object">A signed metering data identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is SignedMeteringData_Id priceAlgorithmId
                   ? CompareTo(priceAlgorithmId)
                   : throw new ArgumentException("The given object is not a signed metering data identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(SignedMeteringDataId)

        /// <summary>
        /// Compares two signed metering data identifications.
        /// </summary>
        /// <param name="SignedMeteringDataId">A signed metering data identification to compare with.</param>
        public Int32 CompareTo(SignedMeteringData_Id SignedMeteringDataId)

            => String.Compare(InternalId,
                              SignedMeteringDataId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<SignedMeteringDataId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two signed metering data identifications for equality.
        /// </summary>
        /// <param name="Object">A signed metering data identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is SignedMeteringData_Id priceAlgorithmId &&
                   Equals(priceAlgorithmId);

        #endregion

        #region Equals(SignedMeteringDataId)

        /// <summary>
        /// Compares two signed metering data identifications for equality.
        /// </summary>
        /// <param name="SignedMeteringDataId">A signed metering data identification to compare with.</param>
        public Boolean Equals(SignedMeteringData_Id SignedMeteringDataId)

            => String.Equals(InternalId,
                             SignedMeteringDataId.InternalId,
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
