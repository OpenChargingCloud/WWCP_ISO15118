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
    /// Extension methods for schedule tuple identifications.
    /// </summary>
    public static class ScheduleTupleIdExtensions
    {

        /// <summary>
        /// Indicates whether this schedule tuple identification is null or empty.
        /// </summary>
        /// <param name="ScheduleTupleId">A schedule tuple identification.</param>
        public static Boolean IsNullOrEmpty(this ScheduleTuple_Id? ScheduleTupleId)
            => !ScheduleTupleId.HasValue || ScheduleTupleId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this schedule tuple identification is null or empty.
        /// </summary>
        /// <param name="ScheduleTupleId">A schedule tuple identification.</param>
        public static Boolean IsNotNullOrEmpty(this ScheduleTuple_Id? ScheduleTupleId)
            => ScheduleTupleId.HasValue && ScheduleTupleId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// A schedule tuple identification.
    /// </summary>
    public readonly struct ScheduleTuple_Id : IId,
                                              IEquatable<ScheduleTuple_Id>,
                                              IComparable<ScheduleTuple_Id>
    {

        #region Data

        /// <summary>
        /// The nummeric value of the schedule tuple identification.
        /// </summary>
        public readonly UInt32 Value;

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
        /// The length of the schedule tuple identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) Value.ToString().Length;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new schedule tuple identification based on the given number.
        /// </summary>
        /// <param name="Number">A numeric representation of a display message identification.</param>
        private ScheduleTuple_Id(UInt32 Number)
        {
            this.Value = Number;
        }

        #endregion


        #region Documentation

        // <xs:simpleType name="schedule tupleIDType">
        //     <xs:restriction base="xs:unsignedShort"/>
        // </xs:simpleType>

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a schedule tuple identification.
        /// </summary>
        /// <param name="Text">A text representation of a schedule tuple identification.</param>
        public static ScheduleTuple_Id Parse(String Text)
        {

            if (TryParse(Text, out var scheduleTupleId))
                return scheduleTupleId;

            throw new ArgumentException("Invalid text representation of a schedule tuple identification: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) Parse   (Number)

        /// <summary>
        /// Parse the given number as a schedule tuple identification.
        /// </summary>
        /// <param name="Number">A numeric representation of a schedule tuple identification.</param>
        public static ScheduleTuple_Id Parse(UInt32 Number)
        {

            if (TryParse(Number, out var scheduleTupleId))
                return scheduleTupleId;

            throw new ArgumentException("Invalid numeric representation of a schedule tuple identification: '" + Number + "'!",
                                        nameof(Number));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a schedule tuple identification.
        /// </summary>
        /// <param name="Text">A text representation of a schedule tuple identification.</param>
        public static ScheduleTuple_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var scheduleTupleId))
                return scheduleTupleId;

            return null;

        }

        #endregion

        #region (static) TryParse(Number)

        /// <summary>
        /// Try to parse the given number as a schedule tuple identification.
        /// </summary>
        /// <param name="Number">A numeric representation of a schedule tuple identification.</param>
        public static ScheduleTuple_Id? TryParse(UInt32 Number)
        {

            if (TryParse(Number, out var scheduleTupleId))
                return scheduleTupleId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text,   out ScheduleTupleId)

        /// <summary>
        /// Try to parse the given text as a schedule tuple identification.
        /// </summary>
        /// <param name="Text">A text representation of a schedule tuple identification.</param>
        /// <param name="ScheduleTupleId">The parsed schedule tuple identification.</param>
        public static Boolean TryParse(String Text, out ScheduleTuple_Id ScheduleTupleId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty() &&
                UInt32.TryParse(Text, out var number))
            {
                ScheduleTupleId = new ScheduleTuple_Id(number);
                return true;
            }

            ScheduleTupleId = default;
            return false;

        }

        #endregion

        #region (static) TryParse(Number, out ScheduleTupleId)

        /// <summary>
        /// Try to parse the given number as a schedule tuple identification.
        /// </summary>
        /// <param name="Number">A numeric representation of a schedule tuple identification.</param>
        /// <param name="ScheduleTupleId">The parsed schedule tuple identification.</param>
        public static Boolean TryParse(UInt32 Number, out ScheduleTuple_Id ScheduleTupleId)
        {

            ScheduleTupleId = new ScheduleTuple_Id(Number);

            return true;

        }

        #endregion

        #region Clone

        /// <summary>
        /// Clone this schedule tuple identification.
        /// </summary>
        public ScheduleTuple_Id Clone

            => new (Value);

        #endregion


        #region Operator overloading

        #region Operator == (ScheduleTupleId1, ScheduleTupleId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ScheduleTupleId1">A schedule tuple identification.</param>
        /// <param name="ScheduleTupleId2">Another schedule tuple identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (ScheduleTuple_Id ScheduleTupleId1,
                                           ScheduleTuple_Id ScheduleTupleId2)

            => ScheduleTupleId1.Equals(ScheduleTupleId2);

        #endregion

        #region Operator != (ScheduleTupleId1, ScheduleTupleId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ScheduleTupleId1">A schedule tuple identification.</param>
        /// <param name="ScheduleTupleId2">Another schedule tuple identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (ScheduleTuple_Id ScheduleTupleId1,
                                           ScheduleTuple_Id ScheduleTupleId2)

            => !ScheduleTupleId1.Equals(ScheduleTupleId2);

        #endregion

        #region Operator <  (ScheduleTupleId1, ScheduleTupleId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ScheduleTupleId1">A schedule tuple identification.</param>
        /// <param name="ScheduleTupleId2">Another schedule tuple identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator < (ScheduleTuple_Id ScheduleTupleId1,
                                          ScheduleTuple_Id ScheduleTupleId2)

            => ScheduleTupleId1.CompareTo(ScheduleTupleId2) < 0;

        #endregion

        #region Operator <= (ScheduleTupleId1, ScheduleTupleId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ScheduleTupleId1">A schedule tuple identification.</param>
        /// <param name="ScheduleTupleId2">Another schedule tuple identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator <= (ScheduleTuple_Id ScheduleTupleId1,
                                           ScheduleTuple_Id ScheduleTupleId2)

            => ScheduleTupleId1.CompareTo(ScheduleTupleId2) <= 0;

        #endregion

        #region Operator >  (ScheduleTupleId1, ScheduleTupleId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ScheduleTupleId1">A schedule tuple identification.</param>
        /// <param name="ScheduleTupleId2">Another schedule tuple identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator > (ScheduleTuple_Id ScheduleTupleId1,
                                          ScheduleTuple_Id ScheduleTupleId2)

            => ScheduleTupleId1.CompareTo(ScheduleTupleId2) > 0;

        #endregion

        #region Operator >= (ScheduleTupleId1, ScheduleTupleId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ScheduleTupleId1">A schedule tuple identification.</param>
        /// <param name="ScheduleTupleId2">Another schedule tuple identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator >= (ScheduleTuple_Id ScheduleTupleId1,
                                           ScheduleTuple_Id ScheduleTupleId2)

            => ScheduleTupleId1.CompareTo(ScheduleTupleId2) >= 0;

        #endregion

        #endregion

        #region IComparable<ScheduleTupleId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two schedule tuple identifications.
        /// </summary>
        /// <param name="Object">A schedule tuple identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ScheduleTuple_Id scheduleTupleId
                   ? CompareTo(scheduleTupleId)
                   : throw new ArgumentException("The given object is not a schedule tuple identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ScheduleTupleId)

        /// <summary>
        /// Compares two schedule tuple identifications.
        /// </summary>
        /// <param name="ScheduleTupleId">A schedule tuple identification to compare with.</param>
        public Int32 CompareTo(ScheduleTuple_Id ScheduleTupleId)

            => Value.CompareTo(ScheduleTupleId.Value);

        #endregion

        #endregion

        #region IEquatable<ScheduleTupleId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two schedule tuple identifications for equality.
        /// </summary>
        /// <param name="Object">A schedule tuple identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ScheduleTuple_Id scheduleTupleId &&
                   Equals(scheduleTupleId);

        #endregion

        #region Equals(ScheduleTupleId)

        /// <summary>
        /// Compares two schedule tuple identifications for equality.
        /// </summary>
        /// <param name="ScheduleTupleId">A schedule tuple identification to compare with.</param>
        public Boolean Equals(ScheduleTuple_Id ScheduleTupleId)

            => Value.Equals(ScheduleTupleId.Value);

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
