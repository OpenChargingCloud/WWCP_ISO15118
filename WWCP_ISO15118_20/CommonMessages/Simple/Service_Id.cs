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
    /// Extention methods for service identifications.
    /// </summary>
    public static class ServiceIdExtensions
    {

        /// <summary>
        /// Indicates whether this service identification is null or empty.
        /// </summary>
        /// <param name="ServiceId">A service identification.</param>
        public static Boolean IsNullOrEmpty(this Service_Id? ServiceId)
            => !ServiceId.HasValue || ServiceId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this service identification is null or empty.
        /// </summary>
        /// <param name="ServiceId">A service identification.</param>
        public static Boolean IsNotNullOrEmpty(this Service_Id? ServiceId)
            => ServiceId.HasValue && ServiceId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// A service identification (but in ISO 15118 this is just an integer!).
    /// </summary>
    public readonly struct Service_Id : IId,
                                        IEquatable<Service_Id>,
                                        IComparable<Service_Id>
    {

        #region Data

        /// <summary>
        /// The nummeric value of the service identification.
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
        /// The length of the service identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) Value.ToString().Length;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new service identification based on the given number.
        /// </summary>
        /// <param name="Number">A numeric representation of a display message identification.</param>
        private Service_Id(UInt16 Number)
        {
            this.Value = Number;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a service identification.
        /// </summary>
        /// <param name="Text">A text representation of a service identification.</param>
        public static Service_Id Parse(String Text)
        {

            if (TryParse(Text, out var evseId))
                return evseId;

            throw new ArgumentException("Invalid text representation of a service identification: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) Parse   (Number)

        /// <summary>
        /// Parse the given number as a service identification.
        /// </summary>
        /// <param name="Number">A numeric representation of a service identification.</param>
        public static Service_Id Parse(UInt16 Number)

            => new (Number);

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a service identification.
        /// </summary>
        /// <param name="Text">A text representation of a service identification.</param>
        public static Service_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var evseId))
                return evseId;

            return null;

        }

        #endregion

        #region (static) TryParse(Number)

        /// <summary>
        /// Try to parse the given number as a service identification.
        /// </summary>
        /// <param name="Number">A numeric representation of a service identification.</param>
        public static Service_Id? TryParse(UInt16 Number)
        {

            if (TryParse(Number, out var evseId))
                return evseId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text,   out ServiceId)

        /// <summary>
        /// Try to parse the given text as a service identification.
        /// </summary>
        /// <param name="Text">A text representation of a service identification.</param>
        /// <param name="ServiceId">The parsed service identification.</param>
        public static Boolean TryParse(String Text, out Service_Id ServiceId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty() &&
                UInt16.TryParse(Text, out var number))
            {
                ServiceId = new Service_Id(number);
                return true;
            }

            ServiceId = default;
            return false;

        }

        #endregion

        #region (static) TryParse(Number, out ServiceId)

        /// <summary>
        /// Try to parse the given number as a service identification.
        /// </summary>
        /// <param name="Number">A numeric representation of a service identification.</param>
        /// <param name="ServiceId">The parsed service identification.</param>
        public static Boolean TryParse(UInt16 Number, out Service_Id ServiceId)
        {

            ServiceId = new Service_Id(Number);

            return true;

        }

        #endregion

        #region Clone

        /// <summary>
        /// Clone this service identification.
        /// </summary>
        public Service_Id Clone

            => new (Value);

        #endregion


        #region Operator overloading

        #region Operator == (ServiceId1, ServiceId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ServiceId1">A service identification.</param>
        /// <param name="ServiceId2">Another service identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (Service_Id ServiceId1,
                                           Service_Id ServiceId2)

            => ServiceId1.Equals(ServiceId2);

        #endregion

        #region Operator != (ServiceId1, ServiceId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ServiceId1">A service identification.</param>
        /// <param name="ServiceId2">Another service identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (Service_Id ServiceId1,
                                           Service_Id ServiceId2)

            => !ServiceId1.Equals(ServiceId2);

        #endregion

        #region Operator <  (ServiceId1, ServiceId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ServiceId1">A service identification.</param>
        /// <param name="ServiceId2">Another service identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator < (Service_Id ServiceId1,
                                          Service_Id ServiceId2)

            => ServiceId1.CompareTo(ServiceId2) < 0;

        #endregion

        #region Operator <= (ServiceId1, ServiceId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ServiceId1">A service identification.</param>
        /// <param name="ServiceId2">Another service identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator <= (Service_Id ServiceId1,
                                           Service_Id ServiceId2)

            => ServiceId1.CompareTo(ServiceId2) <= 0;

        #endregion

        #region Operator >  (ServiceId1, ServiceId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ServiceId1">A service identification.</param>
        /// <param name="ServiceId2">Another service identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator > (Service_Id ServiceId1,
                                          Service_Id ServiceId2)

            => ServiceId1.CompareTo(ServiceId2) > 0;

        #endregion

        #region Operator >= (ServiceId1, ServiceId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ServiceId1">A service identification.</param>
        /// <param name="ServiceId2">Another service identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator >= (Service_Id ServiceId1,
                                           Service_Id ServiceId2)

            => ServiceId1.CompareTo(ServiceId2) >= 0;

        #endregion

        #endregion

        #region IComparable<ServiceId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two service identifications.
        /// </summary>
        /// <param name="Object">A service identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is Service_Id evseId
                   ? CompareTo(evseId)
                   : throw new ArgumentException("The given object is not a service identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ServiceId)

        /// <summary>
        /// Compares two service identifications.
        /// </summary>
        /// <param name="ServiceId">A service identification to compare with.</param>
        public Int32 CompareTo(Service_Id ServiceId)

            => Value.CompareTo(ServiceId.Value);

        #endregion

        #endregion

        #region IEquatable<ServiceId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two service identifications for equality.
        /// </summary>
        /// <param name="Object">A service identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is Service_Id evseId &&
                   Equals(evseId);

        #endregion

        #region Equals(ServiceId)

        /// <summary>
        /// Compares two service identifications for equality.
        /// </summary>
        /// <param name="ServiceId">A service identification to compare with.</param>
        public Boolean Equals(Service_Id ServiceId)

            => Value.Equals(ServiceId.Value);

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
