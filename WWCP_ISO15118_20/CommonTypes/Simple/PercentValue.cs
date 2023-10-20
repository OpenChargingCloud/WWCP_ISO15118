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

namespace cloud.charging.open.protocols.ISO15118_20.CommonTypes
{

    /// <summary>
    /// Extension methods for percent values.
    /// </summary>
    public static class PercentValueExtensions
    {

        /// <summary>
        /// Indicates whether this percent value is null or empty.
        /// </summary>
        /// <param name="PercentValue">A percent value.</param>
        public static Boolean IsNullOrEmpty(this PercentValue? PercentValue)
            => !PercentValue.HasValue || PercentValue.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this percent value is null or empty.
        /// </summary>
        /// <param name="PercentValue">A percent value.</param>
        public static Boolean IsNotNullOrEmpty(this PercentValue? PercentValue)
            => PercentValue.HasValue && PercentValue.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// A percent value (0 - 100%).
    /// </summary>
    public readonly struct PercentValue : IId,
                                          IEquatable<PercentValue>,
                                          IComparable<PercentValue>
    {

        #region Data

        /// <summary>
        /// The nummeric value of the percent value.
        /// </summary>
        public readonly Byte Value;

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
        /// The length of the percent value.
        /// </summary>
        public UInt64 Length
            => (UInt64) Value.ToString().Length;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new percent value based on the given number.
        /// </summary>
        /// <param name="Number">A numeric representation of a display message identification.</param>
        private PercentValue(Byte Number)
        {
            this.Value = Number;
        }

        #endregion


        #region Documentation

        // <xs:simpleType name="percentValueType">
        //     <xs:restriction base="xs:byte">
        //         <xs:minInclusive value="0"/>
        //         <xs:maxInclusive value="100"/>
        //     </xs:restriction>
        // </xs:simpleType>

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a percent value.
        /// </summary>
        /// <param name="Text">A text representation of a percent value.</param>
        public static PercentValue Parse(String Text)
        {

            if (TryParse(Text, out var percentValue))
                return percentValue;

            throw new ArgumentException("Invalid text representation of a percent value: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) Parse   (Number)

        /// <summary>
        /// Parse the given number as a percent value.
        /// </summary>
        /// <param name="Number">A numeric representation of a percent value.</param>
        public static PercentValue Parse(Byte Number)
        {

            if (TryParse(Number, out var percentValue))
                return percentValue;

            throw new ArgumentException("Invalid numeric representation of a percent value: '" + Number + "'!",
                                        nameof(Number));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a percent value.
        /// </summary>
        /// <param name="Text">A text representation of a percent value.</param>
        public static PercentValue? TryParse(String Text)
        {

            if (TryParse(Text, out var percentValue))
                return percentValue;

            return null;

        }

        #endregion

        #region (static) TryParse(Number)

        /// <summary>
        /// Try to parse the given number as a percent value.
        /// </summary>
        /// <param name="Number">A numeric representation of a percent value.</param>
        public static PercentValue? TryParse(Byte Number)
        {

            if (TryParse(Number, out var percentValue))
                return percentValue;

            return null;

        }

        #endregion

        #region (static) TryParse(Text,   out PercentValue)

        /// <summary>
        /// Try to parse the given text as a percent value.
        /// </summary>
        /// <param name="Text">A text representation of a percent value.</param>
        /// <param name="PercentValue">The parsed percent value.</param>
        public static Boolean TryParse(String Text, out PercentValue PercentValue)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty() &&
                Byte.TryParse(Text, out var number) &&
                number >= 0 &&
                number <= 100)
            {
                PercentValue = new PercentValue(number);
                return true;
            }

            PercentValue = default;
            return false;

        }

        #endregion

        #region (static) TryParse(Number, out PercentValue)

        /// <summary>
        /// Try to parse the given number as a percent value.
        /// </summary>
        /// <param name="Number">A numeric representation of a percent value.</param>
        /// <param name="PercentValue">The parsed percent value.</param>
        public static Boolean TryParse(Byte Number, out PercentValue PercentValue)
        {

            if (Number >= 0 &&
                Number <= 100)
            {

                PercentValue = new PercentValue(Number);

                return true;

            }

            PercentValue = default;
            return false;

        }

        #endregion

        #region Clone

        /// <summary>
        /// Clone this percent value.
        /// </summary>
        public PercentValue Clone

            => new (Value);

        #endregion


        #region Operator overloading

        #region Operator == (PercentValue1, PercentValue2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="PercentValue1">A percent value.</param>
        /// <param name="PercentValue2">Another percent value.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (PercentValue PercentValue1,
                                           PercentValue PercentValue2)

            => PercentValue1.Equals(PercentValue2);

        #endregion

        #region Operator != (PercentValue1, PercentValue2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="PercentValue1">A percent value.</param>
        /// <param name="PercentValue2">Another percent value.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (PercentValue PercentValue1,
                                           PercentValue PercentValue2)

            => !PercentValue1.Equals(PercentValue2);

        #endregion

        #region Operator <  (PercentValue1, PercentValue2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="PercentValue1">A percent value.</param>
        /// <param name="PercentValue2">Another percent value.</param>
        /// <returns>true|false</returns>
        public static Boolean operator < (PercentValue PercentValue1,
                                          PercentValue PercentValue2)

            => PercentValue1.CompareTo(PercentValue2) < 0;

        #endregion

        #region Operator <= (PercentValue1, PercentValue2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="PercentValue1">A percent value.</param>
        /// <param name="PercentValue2">Another percent value.</param>
        /// <returns>true|false</returns>
        public static Boolean operator <= (PercentValue PercentValue1,
                                           PercentValue PercentValue2)

            => PercentValue1.CompareTo(PercentValue2) <= 0;

        #endregion

        #region Operator >  (PercentValue1, PercentValue2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="PercentValue1">A percent value.</param>
        /// <param name="PercentValue2">Another percent value.</param>
        /// <returns>true|false</returns>
        public static Boolean operator > (PercentValue PercentValue1,
                                          PercentValue PercentValue2)

            => PercentValue1.CompareTo(PercentValue2) > 0;

        #endregion

        #region Operator >= (PercentValue1, PercentValue2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="PercentValue1">A percent value.</param>
        /// <param name="PercentValue2">Another percent value.</param>
        /// <returns>true|false</returns>
        public static Boolean operator >= (PercentValue PercentValue1,
                                           PercentValue PercentValue2)

            => PercentValue1.CompareTo(PercentValue2) >= 0;

        #endregion

        #endregion

        #region IComparable<PercentValue> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two percent values.
        /// </summary>
        /// <param name="Object">A percent value to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is PercentValue percentValue
                   ? CompareTo(percentValue)
                   : throw new ArgumentException("The given object is not a percent value!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(PercentValue)

        /// <summary>
        /// Compares two percent values.
        /// </summary>
        /// <param name="PercentValue">A percent value to compare with.</param>
        public Int32 CompareTo(PercentValue PercentValue)

            => Value.CompareTo(PercentValue.Value);

        #endregion

        #endregion

        #region IEquatable<PercentValue> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two percent values for equality.
        /// </summary>
        /// <param name="Object">A percent value to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is PercentValue percentValue &&
                   Equals(percentValue);

        #endregion

        #region Equals(PercentValue)

        /// <summary>
        /// Compares two percent values for equality.
        /// </summary>
        /// <param name="PercentValue">A percent value to compare with.</param>
        public Boolean Equals(PercentValue PercentValue)

            => Value.Equals(PercentValue.Value);

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
