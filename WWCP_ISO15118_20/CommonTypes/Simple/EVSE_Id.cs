﻿/*
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
    /// Extension methods for EVSE identifications.
    /// </summary>
    public static class EVSEIdExtensions
    {

        /// <summary>
        /// Indicates whether this EVSE identification is null or empty.
        /// </summary>
        /// <param name="EVSEId">An EVSE identification.</param>
        public static Boolean IsNullOrEmpty(this EVSE_Id? EVSEId)
            => !EVSEId.HasValue || EVSEId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this EVSE identification is null or empty.
        /// </summary>
        /// <param name="EVSEId">An EVSE identification.</param>
        public static Boolean IsNotNullOrEmpty(this EVSE_Id? EVSEId)
            => EVSEId.HasValue && EVSEId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// An EVSE identification.
    /// </summary>
    public readonly struct EVSE_Id : IId,
                                     IEquatable<EVSE_Id>,
                                     IComparable<EVSE_Id>
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
        /// The length of the EVSE identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new EVSE identification based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of an EVSE identification.</param>
        private EVSE_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as an EVSE identification.
        /// </summary>
        /// <param name="Text">A text representation of an EVSE identification.</param>
        public static EVSE_Id Parse(String Text)
        {

            if (TryParse(Text, out var evseId))
                return evseId;

            throw new ArgumentException("Invalid text representation of an EVSE identification: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as an EVSE identification.
        /// </summary>
        /// <param name="Text">A text representation of an EVSE identification.</param>
        public static EVSE_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var evseId))
                return evseId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out EVSEId)

        /// <summary>
        /// Try to parse the given text as an EVSE identification.
        /// </summary>
        /// <param name="Text">A text representation of an EVSE identification.</param>
        /// <param name="EVSEId">The parsed EVSE identification.</param>
        public static Boolean TryParse(String Text, out EVSE_Id EVSEId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                EVSEId = new EVSE_Id(Text);
                return true;
            }

            EVSEId = default;
            return false;

        }

        #endregion

        #region Clone

        /// <summary>
        /// Clone this EVSE identification.
        /// </summary>
        public EVSE_Id Clone

            => new(
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (evseId1, evseId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="evseId1">An EVSE identification.</param>
        /// <param name="evseId2">Another EVSE identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (EVSE_Id evseId1,
                                           EVSE_Id evseId2)

            => evseId1.Equals(evseId2);

        #endregion

        #region Operator != (evseId1, evseId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="evseId1">An EVSE identification.</param>
        /// <param name="evseId2">Another EVSE identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (EVSE_Id evseId1,
                                           EVSE_Id evseId2)

            => !evseId1.Equals(evseId2);

        #endregion

        #region Operator <  (evseId1, evseId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="evseId1">An EVSE identification.</param>
        /// <param name="evseId2">Another EVSE identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator < (EVSE_Id evseId1,
                                          EVSE_Id evseId2)

            => evseId1.CompareTo(evseId2) < 0;

        #endregion

        #region Operator <= (evseId1, evseId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="evseId1">An EVSE identification.</param>
        /// <param name="evseId2">Another EVSE identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator <= (EVSE_Id evseId1,
                                           EVSE_Id evseId2)

            => evseId1.CompareTo(evseId2) <= 0;

        #endregion

        #region Operator >  (evseId1, evseId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="evseId1">An EVSE identification.</param>
        /// <param name="evseId2">Another EVSE identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator > (EVSE_Id evseId1,
                                          EVSE_Id evseId2)

            => evseId1.CompareTo(evseId2) > 0;

        #endregion

        #region Operator >= (evseId1, evseId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="evseId1">An EVSE identification.</param>
        /// <param name="evseId2">Another EVSE identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator >= (EVSE_Id evseId1,
                                           EVSE_Id evseId2)

            => evseId1.CompareTo(evseId2) >= 0;

        #endregion

        #endregion

        #region IComparable<EVSEId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two EVSE identifications.
        /// </summary>
        /// <param name="Object">An EVSE identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is EVSE_Id evseId
                   ? CompareTo(evseId)
                   : throw new ArgumentException("The given object is not an EVSE identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(EVSEId)

        /// <summary>
        /// Compares two EVSE identifications.
        /// </summary>
        /// <param name="EVSEId">An EVSE identification to compare with.</param>
        public Int32 CompareTo(EVSE_Id EVSEId)

            => String.Compare(InternalId,
                              EVSEId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<EVSEId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two EVSE identifications for equality.
        /// </summary>
        /// <param name="Object">An EVSE identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EVSE_Id evseId &&
                   Equals(evseId);

        #endregion

        #region Equals(EVSEId)

        /// <summary>
        /// Compares two EVSE identifications for equality.
        /// </summary>
        /// <param name="EVSEId">An EVSE identification to compare with.</param>
        public Boolean Equals(EVSE_Id EVSEId)

            => String.Equals(InternalId,
                             EVSEId.InternalId,
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
