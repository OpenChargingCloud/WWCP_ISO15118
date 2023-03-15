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
    /// Extention methods for parameter set identifications.
    /// </summary>
    public static class ParameterSetIdExtensions
    {

        /// <summary>
        /// Indicates whether this parameter set identification is null or empty.
        /// </summary>
        /// <param name="ParameterSetId">A parameter set identification.</param>
        public static Boolean IsNullOrEmpty(this ParameterSet_Id? ParameterSetId)
            => !ParameterSetId.HasValue || ParameterSetId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this parameter set identification is null or empty.
        /// </summary>
        /// <param name="ParameterSetId">A parameter set identification.</param>
        public static Boolean IsNotNullOrEmpty(this ParameterSet_Id? ParameterSetId)
            => ParameterSetId.HasValue && ParameterSetId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// A parameter set identification.
    /// </summary>
    public readonly struct ParameterSet_Id : IId,
                                             IEquatable<ParameterSet_Id>,
                                             IComparable<ParameterSet_Id>
    {

        #region Data

        /// <summary>
        /// The nummeric value of the parameter set identification.
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
        /// The length of the parameter set identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) Value.ToString().Length;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new parameter set identification based on the given number.
        /// </summary>
        /// <param name="Number">A numeric representation of a display message identification.</param>
        private ParameterSet_Id(UInt16 Number)
        {
            this.Value = Number;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a parameter set identification.
        /// </summary>
        /// <param name="Text">A text representation of a parameter set identification.</param>
        public static ParameterSet_Id Parse(String Text)
        {

            if (TryParse(Text, out var parameterSetId))
                return parameterSetId;

            throw new ArgumentException("Invalid text representation of a parameter set identification: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) Parse   (Number)

        /// <summary>
        /// Parse the given number as a parameter set identification.
        /// </summary>
        /// <param name="Number">A numeric representation of a parameter set identification.</param>
        public static ParameterSet_Id Parse(UInt16 Number)

            => new (Number);

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a parameter set identification.
        /// </summary>
        /// <param name="Text">A text representation of a parameter set identification.</param>
        public static ParameterSet_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var parameterSetId))
                return parameterSetId;

            return null;

        }

        #endregion

        #region (static) TryParse(Number)

        /// <summary>
        /// Try to parse the given number as a parameter set identification.
        /// </summary>
        /// <param name="Number">A numeric representation of a parameter set identification.</param>
        public static ParameterSet_Id? TryParse(UInt16 Number)
        {

            if (TryParse(Number, out var parameterSetId))
                return parameterSetId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text,   out ParameterSetId)

        /// <summary>
        /// Try to parse the given text as a parameter set identification.
        /// </summary>
        /// <param name="Text">A text representation of a parameter set identification.</param>
        /// <param name="ParameterSetId">The parsed parameter set identification.</param>
        public static Boolean TryParse(String Text, out ParameterSet_Id ParameterSetId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty() &&
                UInt16.TryParse(Text, out var number))
            {
                ParameterSetId = new ParameterSet_Id(number);
                return true;
            }

            ParameterSetId = default;
            return false;

        }

        #endregion

        #region (static) TryParse(Number, out ParameterSetId)

        /// <summary>
        /// Try to parse the given number as a parameter set identification.
        /// </summary>
        /// <param name="Number">A numeric representation of a parameter set identification.</param>
        /// <param name="ParameterSetId">The parsed parameter set identification.</param>
        public static Boolean TryParse(UInt16 Number, out ParameterSet_Id ParameterSetId)
        {

            ParameterSetId = new ParameterSet_Id(Number);

            return true;

        }

        #endregion

        #region Clone

        /// <summary>
        /// Clone this parameter set identification.
        /// </summary>
        public ParameterSet_Id Clone

            => new (Value);

        #endregion


        #region Operator overloading

        #region Operator == (ParameterSetId1, ParameterSetId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParameterSetId1">A parameter set identification.</param>
        /// <param name="ParameterSetId2">Another parameter set identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (ParameterSet_Id ParameterSetId1,
                                           ParameterSet_Id ParameterSetId2)

            => ParameterSetId1.Equals(ParameterSetId2);

        #endregion

        #region Operator != (ParameterSetId1, ParameterSetId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParameterSetId1">A parameter set identification.</param>
        /// <param name="ParameterSetId2">Another parameter set identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (ParameterSet_Id ParameterSetId1,
                                           ParameterSet_Id ParameterSetId2)

            => !ParameterSetId1.Equals(ParameterSetId2);

        #endregion

        #region Operator <  (ParameterSetId1, ParameterSetId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParameterSetId1">A parameter set identification.</param>
        /// <param name="ParameterSetId2">Another parameter set identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator < (ParameterSet_Id ParameterSetId1,
                                          ParameterSet_Id ParameterSetId2)

            => ParameterSetId1.CompareTo(ParameterSetId2) < 0;

        #endregion

        #region Operator <= (ParameterSetId1, ParameterSetId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParameterSetId1">A parameter set identification.</param>
        /// <param name="ParameterSetId2">Another parameter set identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator <= (ParameterSet_Id ParameterSetId1,
                                           ParameterSet_Id ParameterSetId2)

            => ParameterSetId1.CompareTo(ParameterSetId2) <= 0;

        #endregion

        #region Operator >  (ParameterSetId1, ParameterSetId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParameterSetId1">A parameter set identification.</param>
        /// <param name="ParameterSetId2">Another parameter set identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator > (ParameterSet_Id ParameterSetId1,
                                          ParameterSet_Id ParameterSetId2)

            => ParameterSetId1.CompareTo(ParameterSetId2) > 0;

        #endregion

        #region Operator >= (ParameterSetId1, ParameterSetId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParameterSetId1">A parameter set identification.</param>
        /// <param name="ParameterSetId2">Another parameter set identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator >= (ParameterSet_Id ParameterSetId1,
                                           ParameterSet_Id ParameterSetId2)

            => ParameterSetId1.CompareTo(ParameterSetId2) >= 0;

        #endregion

        #endregion

        #region IComparable<ParameterSetId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two parameter set identifications.
        /// </summary>
        /// <param name="Object">A parameter set identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ParameterSet_Id parameterSetId
                   ? CompareTo(parameterSetId)
                   : throw new ArgumentException("The given object is not a parameter set identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ParameterSetId)

        /// <summary>
        /// Compares two parameter set identifications.
        /// </summary>
        /// <param name="ParameterSetId">A parameter set identification to compare with.</param>
        public Int32 CompareTo(ParameterSet_Id ParameterSetId)

            => Value.CompareTo(ParameterSetId.Value);

        #endregion

        #endregion

        #region IEquatable<ParameterSetId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two parameter set identifications for equality.
        /// </summary>
        /// <param name="Object">A parameter set identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ParameterSet_Id parameterSetId &&
                   Equals(parameterSetId);

        #endregion

        #region Equals(ParameterSetId)

        /// <summary>
        /// Compares two parameter set identifications for equality.
        /// </summary>
        /// <param name="ParameterSetId">A parameter set identification to compare with.</param>
        public Boolean Equals(ParameterSet_Id ParameterSetId)

            => Value.Equals(ParameterSetId.Value);

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
