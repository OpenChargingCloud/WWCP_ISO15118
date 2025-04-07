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

namespace cloud.charging.open.protocols.ISO15118_20.CommonTypes
{

    /// <summary>
    /// Extension methods for device identifications.
    /// </summary>
    public static class DeviceIdExtensions
    {

        /// <summary>
        /// Indicates whether this device identification is null or empty.
        /// </summary>
        /// <param name="DeviceId">A device identification.</param>
        public static Boolean IsNullOrEmpty(this Device_Id? DeviceId)
            => !DeviceId.HasValue || DeviceId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this device identification is null or empty.
        /// </summary>
        /// <param name="DeviceId">A device identification.</param>
        public static Boolean IsNotNullOrEmpty(this Device_Id? DeviceId)
            => DeviceId.HasValue && DeviceId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// A device identification.
    /// </summary>
    public readonly struct Device_Id : IId,
                                       IEquatable<Device_Id>,
                                       IComparable<Device_Id>
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
        /// The length of the device identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new device identification based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a device identification.</param>
        private Device_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) NewRandom()

        /// <summary>
        /// Create a new random device identification.
        /// </summary>
        public static Device_Id NewRandom()

            => new (new Guid().ToString());

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a device identification.
        /// </summary>
        /// <param name="Text">A text representation of a device identification.</param>
        public static Device_Id Parse(String Text)
        {

            if (TryParse(Text, out var deviceId))
                return deviceId;

            throw new ArgumentException("Invalid text representation of a device identification: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a device identification.
        /// </summary>
        /// <param name="Text">A text representation of a device identification.</param>
        public static Device_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var deviceId))
                return deviceId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out DeviceId)

        /// <summary>
        /// Try to parse the given text as a device identification.
        /// </summary>
        /// <param name="Text">A text representation of a device identification.</param>
        /// <param name="DeviceId">The parsed device identification.</param>
        public static Boolean TryParse(String Text, out Device_Id DeviceId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                DeviceId = new Device_Id(Text);
                return true;
            }

            DeviceId = default;
            return false;

        }

        #endregion

        #region Clone

        /// <summary>
        /// Clone this device identification.
        /// </summary>
        public Device_Id Clone

            => new(
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (DeviceId1, DeviceId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="DeviceId1">A device identification.</param>
        /// <param name="DeviceId2">Another device identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (Device_Id DeviceId1,
                                           Device_Id DeviceId2)

            => DeviceId1.Equals(DeviceId2);

        #endregion

        #region Operator != (DeviceId1, DeviceId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="DeviceId1">A device identification.</param>
        /// <param name="DeviceId2">Another device identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (Device_Id DeviceId1,
                                           Device_Id DeviceId2)

            => !DeviceId1.Equals(DeviceId2);

        #endregion

        #region Operator <  (DeviceId1, DeviceId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="DeviceId1">A device identification.</param>
        /// <param name="DeviceId2">Another device identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator < (Device_Id DeviceId1,
                                          Device_Id DeviceId2)

            => DeviceId1.CompareTo(DeviceId2) < 0;

        #endregion

        #region Operator <= (DeviceId1, DeviceId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="DeviceId1">A device identification.</param>
        /// <param name="DeviceId2">Another device identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator <= (Device_Id DeviceId1,
                                           Device_Id DeviceId2)

            => DeviceId1.CompareTo(DeviceId2) <= 0;

        #endregion

        #region Operator >  (DeviceId1, DeviceId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="DeviceId1">A device identification.</param>
        /// <param name="DeviceId2">Another device identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator > (Device_Id DeviceId1,
                                          Device_Id DeviceId2)

            => DeviceId1.CompareTo(DeviceId2) > 0;

        #endregion

        #region Operator >= (DeviceId1, DeviceId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="DeviceId1">A device identification.</param>
        /// <param name="DeviceId2">Another device identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator >= (Device_Id DeviceId1,
                                           Device_Id DeviceId2)

            => DeviceId1.CompareTo(DeviceId2) >= 0;

        #endregion

        #endregion

        #region IComparable<DeviceId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two device identifications.
        /// </summary>
        /// <param name="Object">A device identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is Device_Id deviceId
                   ? CompareTo(deviceId)
                   : throw new ArgumentException("The given object is not a device identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(DeviceId)

        /// <summary>
        /// Compares two device identifications.
        /// </summary>
        /// <param name="DeviceId">A device identification to compare with.</param>
        public Int32 CompareTo(Device_Id DeviceId)

            => String.Compare(InternalId,
                              DeviceId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<DeviceId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two device identifications for equality.
        /// </summary>
        /// <param name="Object">A device identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is Device_Id deviceId &&
                   Equals(deviceId);

        #endregion

        #region Equals(DeviceId)

        /// <summary>
        /// Compares two device identifications for equality.
        /// </summary>
        /// <param name="DeviceId">A device identification to compare with.</param>
        public Boolean Equals(Device_Id DeviceId)

            => String.Equals(InternalId,
                             DeviceId.InternalId,
                             StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
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
