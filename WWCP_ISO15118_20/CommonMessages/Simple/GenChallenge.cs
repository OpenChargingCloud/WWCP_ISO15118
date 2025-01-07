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
    /// Extension methods for gen challenges.
    /// </summary>
    public static class GenChallengeExtensions
    {

        /// <summary>
        /// Indicates whether this gen challenge is null or empty.
        /// </summary>
        /// <param name="GenChallenge">A gen challenge.</param>
        public static Boolean IsNullOrEmpty(this GenChallenge? GenChallenge)
            => !GenChallenge.HasValue || GenChallenge.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this gen challenge is null or empty.
        /// </summary>
        /// <param name="GenChallenge">A gen challenge.</param>
        public static Boolean IsNotNullOrEmpty(this GenChallenge? GenChallenge)
            => GenChallenge.HasValue && GenChallenge.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// A gen challenge.
    /// 
    /// xs:base64Binary, length: 16
    /// </summary>
    public readonly struct GenChallenge : IId,
                                          IEquatable<GenChallenge>,
                                          IComparable<GenChallenge>
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
        /// The length of the gen challenge.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new gen challenge based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a gen challenge.</param>
        private GenChallenge(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region Documentation

        // <xs:simpleType name="genChallengeType">
        //     <xs:restriction base="xs:base64Binary">
        //         <xs:length value="16"/>
        //     </xs:restriction>
        // </xs:simpleType>

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a gen challenge.
        /// </summary>
        /// <param name="Text">A text representation of a gen challenge.</param>
        public static GenChallenge Parse(String Text)
        {

            if (TryParse(Text, out var genChallenge))
                return genChallenge;

            throw new ArgumentException("Invalid text representation of a gen challenge: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a gen challenge.
        /// </summary>
        /// <param name="Text">A text representation of a gen challenge.</param>
        public static GenChallenge? TryParse(String Text)
        {

            if (TryParse(Text, out var genChallenge))
                return genChallenge;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out GenChallenge)

        /// <summary>
        /// Try to parse the given text as a gen challenge.
        /// </summary>
        /// <param name="Text">A text representation of a gen challenge.</param>
        /// <param name="GenChallenge">The parsed gen challenge.</param>
        public static Boolean TryParse(String Text, out GenChallenge GenChallenge)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                GenChallenge = new GenChallenge(Text);
                return true;
            }

            GenChallenge = default;
            return false;

        }

        #endregion

        #region Clone

        /// <summary>
        /// Clone this gen challenge.
        /// </summary>
        public GenChallenge Clone

            => new(
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (GenChallenge1, GenChallenge2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="GenChallenge1">A gen challenge.</param>
        /// <param name="GenChallenge2">Another gen challenge.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (GenChallenge GenChallenge1,
                                           GenChallenge GenChallenge2)

            => GenChallenge1.Equals(GenChallenge2);

        #endregion

        #region Operator != (GenChallenge1, GenChallenge2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="GenChallenge1">A gen challenge.</param>
        /// <param name="GenChallenge2">Another gen challenge.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (GenChallenge GenChallenge1,
                                           GenChallenge GenChallenge2)

            => !GenChallenge1.Equals(GenChallenge2);

        #endregion

        #region Operator <  (GenChallenge1, GenChallenge2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="GenChallenge1">A gen challenge.</param>
        /// <param name="GenChallenge2">Another gen challenge.</param>
        /// <returns>true|false</returns>
        public static Boolean operator < (GenChallenge GenChallenge1,
                                          GenChallenge GenChallenge2)

            => GenChallenge1.CompareTo(GenChallenge2) < 0;

        #endregion

        #region Operator <= (GenChallenge1, GenChallenge2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="GenChallenge1">A gen challenge.</param>
        /// <param name="GenChallenge2">Another gen challenge.</param>
        /// <returns>true|false</returns>
        public static Boolean operator <= (GenChallenge GenChallenge1,
                                           GenChallenge GenChallenge2)

            => GenChallenge1.CompareTo(GenChallenge2) <= 0;

        #endregion

        #region Operator >  (GenChallenge1, GenChallenge2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="GenChallenge1">A gen challenge.</param>
        /// <param name="GenChallenge2">Another gen challenge.</param>
        /// <returns>true|false</returns>
        public static Boolean operator > (GenChallenge GenChallenge1,
                                          GenChallenge GenChallenge2)

            => GenChallenge1.CompareTo(GenChallenge2) > 0;

        #endregion

        #region Operator >= (GenChallenge1, GenChallenge2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="GenChallenge1">A gen challenge.</param>
        /// <param name="GenChallenge2">Another gen challenge.</param>
        /// <returns>true|false</returns>
        public static Boolean operator >= (GenChallenge GenChallenge1,
                                           GenChallenge GenChallenge2)

            => GenChallenge1.CompareTo(GenChallenge2) >= 0;

        #endregion

        #endregion

        #region IComparable<GenChallenge> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two gen challenges.
        /// </summary>
        /// <param name="Object">A gen challenge to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is GenChallenge genChallenge
                   ? CompareTo(genChallenge)
                   : throw new ArgumentException("The given object is not a gen challenge!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(GenChallenge)

        /// <summary>
        /// Compares two gen challenges.
        /// </summary>
        /// <param name="GenChallenge">A gen challenge to compare with.</param>
        public Int32 CompareTo(GenChallenge GenChallenge)

            => String.Compare(InternalId,
                              GenChallenge.InternalId,
                              StringComparison.Ordinal);

        #endregion

        #endregion

        #region IEquatable<GenChallenge> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two gen challenges for equality.
        /// </summary>
        /// <param name="Object">A gen challenge to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is GenChallenge genChallenge &&
                   Equals(genChallenge);

        #endregion

        #region Equals(GenChallenge)

        /// <summary>
        /// Compares two gen challenges for equality.
        /// </summary>
        /// <param name="GenChallenge">A gen challenge to compare with.</param>
        public Boolean Equals(GenChallenge GenChallenge)

            => String.Equals(InternalId,
                             GenChallenge.InternalId,
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
