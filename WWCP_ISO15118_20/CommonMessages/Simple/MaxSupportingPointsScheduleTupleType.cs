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
using static System.Net.Mime.MediaTypeNames;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// Extention methods for max supporting points schedule tuple types.
    /// </summary>
    public static class MaxSupportingPointsScheduleTupleTypeExtensions
    {

        /// <summary>
        /// Indicates whether this max supporting points schedule tuple type is null or empty.
        /// </summary>
        /// <param name="MaxSupportingPointsScheduleTupleType">A max supporting points schedule tuple type.</param>
        public static Boolean IsNullOrEmpty(this MaxSupportingPointsScheduleTupleType? MaxSupportingPointsScheduleTupleType)
            => !MaxSupportingPointsScheduleTupleType.HasValue || MaxSupportingPointsScheduleTupleType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this max supporting points schedule tuple type is null or empty.
        /// </summary>
        /// <param name="MaxSupportingPointsScheduleTupleType">A max supporting points schedule tuple type.</param>
        public static Boolean IsNotNullOrEmpty(this MaxSupportingPointsScheduleTupleType? MaxSupportingPointsScheduleTupleType)
            => MaxSupportingPointsScheduleTupleType.HasValue && MaxSupportingPointsScheduleTupleType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// A max supporting points schedule tuple type (but in ISO 15118 this is just an integer!).
    /// </summary>
    public readonly struct MaxSupportingPointsScheduleTupleType : IId,
                                                                  IEquatable<MaxSupportingPointsScheduleTupleType>,
                                                                  IComparable<MaxSupportingPointsScheduleTupleType>
    {

        #region Data

        /// <summary>
        /// The nummeric value of the max supporting points schedule tuple type.
        /// </summary>
        public readonly UInt16 Value;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => false;

        /// <summary>
        /// Indicates whether this identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => true;

        /// <summary>
        /// The length of the max supporting points schedule tuple type.
        /// </summary>
        public UInt64 Length
            => (UInt64) Value.ToString().Length;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new max supporting points schedule tuple type based on the given number.
        /// </summary>
        /// <param name="Number">A numeric representation of a display message identification.</param>
        private MaxSupportingPointsScheduleTupleType(UInt16 Number)
        {
            this.Value = Number;
        }

        #endregion


        #region Documentation

        // <xs:simpleType name="serviceIDType">
        //     <xs:restriction base="xs:unsignedShort"/>
        // </xs:simpleType>

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a max supporting points schedule tuple type.
        /// </summary>
        /// <param name="Text">A text representation of a max supporting points schedule tuple type.</param>
        public static MaxSupportingPointsScheduleTupleType Parse(String Text)
        {

            if (TryParse(Text, out var number))
                return number;

            throw new ArgumentException("Invalid text representation of a max supporting points schedule tuple type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) Parse   (Number)

        /// <summary>
        /// Parse the given number as a max supporting points schedule tuple type.
        /// </summary>
        /// <param name="Number">A numeric representation of a max supporting points schedule tuple type.</param>
        public static MaxSupportingPointsScheduleTupleType Parse(UInt16 Number)
        {

            if (TryParse(Number, out var number))
                return number;

            throw new ArgumentException("Invalid numeric representation of a max supporting points schedule tuple type: '" + Number + "'!",
                                        nameof(Number));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a max supporting points schedule tuple type.
        /// </summary>
        /// <param name="Text">A text representation of a max supporting points schedule tuple type.</param>
        public static MaxSupportingPointsScheduleTupleType? TryParse(String Text)
        {

            if (TryParse(Text, out var number))
                return number;

            return null;

        }

        #endregion

        #region (static) TryParse(Number)

        /// <summary>
        /// Try to parse the given number as a max supporting points schedule tuple type.
        /// </summary>
        /// <param name="Number">A numeric representation of a max supporting points schedule tuple type.</param>
        public static MaxSupportingPointsScheduleTupleType? TryParse(UInt16 Number)
        {

            if (TryParse(Number, out var number))
                return number;

            return null;

        }

        #endregion

        #region (static) TryParse(Text,   out MaxSupportingPointsScheduleTupleType)

        /// <summary>
        /// Try to parse the given text as a max supporting points schedule tuple type.
        /// </summary>
        /// <param name="Text">A text representation of a max supporting points schedule tuple type.</param>
        /// <param name="MaxSupportingPointsScheduleTupleType">The parsed max supporting points schedule tuple type.</param>
        public static Boolean TryParse(String Text, out MaxSupportingPointsScheduleTupleType MaxSupportingPointsScheduleTupleType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty() &&
                UInt16.TryParse(Text, out var number) &&
                number >= 12 &&
                number <= 1024)
            {
                MaxSupportingPointsScheduleTupleType = new MaxSupportingPointsScheduleTupleType(number);
                return true;
            }

            MaxSupportingPointsScheduleTupleType = default;
            return false;

        }

        #endregion

        #region (static) TryParse(Number, out MaxSupportingPointsScheduleTupleType)

        /// <summary>
        /// Try to parse the given number as a max supporting points schedule tuple type.
        /// </summary>
        /// <param name="Number">A numeric representation of a max supporting points schedule tuple type.</param>
        /// <param name="MaxSupportingPointsScheduleTupleType">The parsed max supporting points schedule tuple type.</param>
        public static Boolean TryParse(UInt16 Number, out MaxSupportingPointsScheduleTupleType MaxSupportingPointsScheduleTupleType)
        {

            if (Number >= 12 &&
                Number <= 1024)
            {

                MaxSupportingPointsScheduleTupleType = new MaxSupportingPointsScheduleTupleType(Number);

                return true;

            }

            MaxSupportingPointsScheduleTupleType = default;
            return false;

        }

        #endregion

        #region Clone

        /// <summary>
        /// Clone this max supporting points schedule tuple type.
        /// </summary>
        public MaxSupportingPointsScheduleTupleType Clone

            => new (Value);

        #endregion


        #region Operator overloading

        #region Operator == (MaxSupportingPointsScheduleTupleType1, MaxSupportingPointsScheduleTupleType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="MaxSupportingPointsScheduleTupleType1">A max supporting points schedule tuple type.</param>
        /// <param name="MaxSupportingPointsScheduleTupleType2">Another max supporting points schedule tuple type.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (MaxSupportingPointsScheduleTupleType MaxSupportingPointsScheduleTupleType1,
                                           MaxSupportingPointsScheduleTupleType MaxSupportingPointsScheduleTupleType2)

            => MaxSupportingPointsScheduleTupleType1.Equals(MaxSupportingPointsScheduleTupleType2);

        #endregion

        #region Operator != (MaxSupportingPointsScheduleTupleType1, MaxSupportingPointsScheduleTupleType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="MaxSupportingPointsScheduleTupleType1">A max supporting points schedule tuple type.</param>
        /// <param name="MaxSupportingPointsScheduleTupleType2">Another max supporting points schedule tuple type.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (MaxSupportingPointsScheduleTupleType MaxSupportingPointsScheduleTupleType1,
                                           MaxSupportingPointsScheduleTupleType MaxSupportingPointsScheduleTupleType2)

            => !MaxSupportingPointsScheduleTupleType1.Equals(MaxSupportingPointsScheduleTupleType2);

        #endregion

        #region Operator <  (MaxSupportingPointsScheduleTupleType1, MaxSupportingPointsScheduleTupleType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="MaxSupportingPointsScheduleTupleType1">A max supporting points schedule tuple type.</param>
        /// <param name="MaxSupportingPointsScheduleTupleType2">Another max supporting points schedule tuple type.</param>
        /// <returns>true|false</returns>
        public static Boolean operator < (MaxSupportingPointsScheduleTupleType MaxSupportingPointsScheduleTupleType1,
                                          MaxSupportingPointsScheduleTupleType MaxSupportingPointsScheduleTupleType2)

            => MaxSupportingPointsScheduleTupleType1.CompareTo(MaxSupportingPointsScheduleTupleType2) < 0;

        #endregion

        #region Operator <= (MaxSupportingPointsScheduleTupleType1, MaxSupportingPointsScheduleTupleType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="MaxSupportingPointsScheduleTupleType1">A max supporting points schedule tuple type.</param>
        /// <param name="MaxSupportingPointsScheduleTupleType2">Another max supporting points schedule tuple type.</param>
        /// <returns>true|false</returns>
        public static Boolean operator <= (MaxSupportingPointsScheduleTupleType MaxSupportingPointsScheduleTupleType1,
                                           MaxSupportingPointsScheduleTupleType MaxSupportingPointsScheduleTupleType2)

            => MaxSupportingPointsScheduleTupleType1.CompareTo(MaxSupportingPointsScheduleTupleType2) <= 0;

        #endregion

        #region Operator >  (MaxSupportingPointsScheduleTupleType1, MaxSupportingPointsScheduleTupleType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="MaxSupportingPointsScheduleTupleType1">A max supporting points schedule tuple type.</param>
        /// <param name="MaxSupportingPointsScheduleTupleType2">Another max supporting points schedule tuple type.</param>
        /// <returns>true|false</returns>
        public static Boolean operator > (MaxSupportingPointsScheduleTupleType MaxSupportingPointsScheduleTupleType1,
                                          MaxSupportingPointsScheduleTupleType MaxSupportingPointsScheduleTupleType2)

            => MaxSupportingPointsScheduleTupleType1.CompareTo(MaxSupportingPointsScheduleTupleType2) > 0;

        #endregion

        #region Operator >= (MaxSupportingPointsScheduleTupleType1, MaxSupportingPointsScheduleTupleType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="MaxSupportingPointsScheduleTupleType1">A max supporting points schedule tuple type.</param>
        /// <param name="MaxSupportingPointsScheduleTupleType2">Another max supporting points schedule tuple type.</param>
        /// <returns>true|false</returns>
        public static Boolean operator >= (MaxSupportingPointsScheduleTupleType MaxSupportingPointsScheduleTupleType1,
                                           MaxSupportingPointsScheduleTupleType MaxSupportingPointsScheduleTupleType2)

            => MaxSupportingPointsScheduleTupleType1.CompareTo(MaxSupportingPointsScheduleTupleType2) >= 0;

        #endregion

        #endregion

        #region IComparable<MaxSupportingPointsScheduleTupleType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two max supporting points schedule tuple types.
        /// </summary>
        /// <param name="Object">A max supporting points schedule tuple type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is MaxSupportingPointsScheduleTupleType number
                   ? CompareTo(number)
                   : throw new ArgumentException("The given object is not a max supporting points schedule tuple type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(MaxSupportingPointsScheduleTupleType)

        /// <summary>
        /// Compares two max supporting points schedule tuple types.
        /// </summary>
        /// <param name="MaxSupportingPointsScheduleTupleType">A max supporting points schedule tuple type to compare with.</param>
        public Int32 CompareTo(MaxSupportingPointsScheduleTupleType MaxSupportingPointsScheduleTupleType)

            => Value.CompareTo(MaxSupportingPointsScheduleTupleType.Value);

        #endregion

        #endregion

        #region IEquatable<MaxSupportingPointsScheduleTupleType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two max supporting points schedule tuple types for equality.
        /// </summary>
        /// <param name="Object">A max supporting points schedule tuple type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is MaxSupportingPointsScheduleTupleType number &&
                   Equals(number);

        #endregion

        #region Equals(MaxSupportingPointsScheduleTupleType)

        /// <summary>
        /// Compares two max supporting points schedule tuple types for equality.
        /// </summary>
        /// <param name="MaxSupportingPointsScheduleTupleType">A max supporting points schedule tuple type to compare with.</param>
        public Boolean Equals(MaxSupportingPointsScheduleTupleType MaxSupportingPointsScheduleTupleType)

            => Value.Equals(MaxSupportingPointsScheduleTupleType.Value);

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the HashCode of this object.
        /// </summary>
        /// <returns>The HashCode of this object.</returns>
        public override Int32 GetHashCode()

            => Value.GetHashCode();

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => Value.ToString();

        #endregion

    }

}
