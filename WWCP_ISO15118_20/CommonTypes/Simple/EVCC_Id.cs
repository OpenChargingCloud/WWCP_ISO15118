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
    /// Extention methods for EVCC identifications.
    /// </summary>
    public static class EVCCIdExtensions
    {

        /// <summary>
        /// Indicates whether this EVCC identification is null or empty.
        /// </summary>
        /// <param name="EVCCId">An EVCC identification.</param>
        public static Boolean IsNullOrEmpty(this EVCC_Id? EVCCId)
            => !EVCCId.HasValue || EVCCId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this EVCC identification is null or empty.
        /// </summary>
        /// <param name="EVCCId">An EVCC identification.</param>
        public static Boolean IsNotNullOrEmpty(this EVCC_Id? EVCCId)
            => EVCCId.HasValue && EVCCId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// An EVCC identification.
    /// </summary>
    public readonly struct EVCC_Id : IId,
                                     IEquatable<EVCC_Id>,
                                     IComparable<EVCC_Id>
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
        /// The length of the EVCC identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new EVCC identification based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of an EVCC identification.</param>
        private EVCC_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as an EVCC identification.
        /// </summary>
        /// <param name="Text">A text representation of an EVCC identification.</param>
        public static EVCC_Id Parse(String Text)
        {

            if (TryParse(Text, out var evccId))
                return evccId;

            throw new ArgumentException("Invalid text representation of an EVCC identification: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as an EVCC identification.
        /// </summary>
        /// <param name="Text">A text representation of an EVCC identification.</param>
        public static EVCC_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var evccId))
                return evccId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out EVCCId)

        /// <summary>
        /// Try to parse the given text as an EVCC identification.
        /// </summary>
        /// <param name="Text">A text representation of an EVCC identification.</param>
        /// <param name="EVCCId">The parsed EVCC identification.</param>
        public static Boolean TryParse(String Text, out EVCC_Id EVCCId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                EVCCId = new EVCC_Id(Text);
                return true;
            }

            EVCCId = default;
            return false;

        }

        #endregion

        #region Clone

        /// <summary>
        /// Clone this EVCC identification.
        /// </summary>
        public EVCC_Id Clone

            => new(
                   new String(InternalId?.ToCharArray())
               );

        #endregion


        #region Operator overloading

        #region Operator == (evccId1, evccId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="evccId1">An EVCC identification.</param>
        /// <param name="evccId2">Another EVCC identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (EVCC_Id evccId1,
                                           EVCC_Id evccId2)

            => evccId1.Equals(evccId2);

        #endregion

        #region Operator != (evccId1, evccId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="evccId1">An EVCC identification.</param>
        /// <param name="evccId2">Another EVCC identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (EVCC_Id evccId1,
                                           EVCC_Id evccId2)

            => !evccId1.Equals(evccId2);

        #endregion

        #region Operator <  (evccId1, evccId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="evccId1">An EVCC identification.</param>
        /// <param name="evccId2">Another EVCC identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator < (EVCC_Id evccId1,
                                          EVCC_Id evccId2)

            => evccId1.CompareTo(evccId2) < 0;

        #endregion

        #region Operator <= (evccId1, evccId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="evccId1">An EVCC identification.</param>
        /// <param name="evccId2">Another EVCC identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator <= (EVCC_Id evccId1,
                                           EVCC_Id evccId2)

            => evccId1.CompareTo(evccId2) <= 0;

        #endregion

        #region Operator >  (evccId1, evccId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="evccId1">An EVCC identification.</param>
        /// <param name="evccId2">Another EVCC identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator > (EVCC_Id evccId1,
                                          EVCC_Id evccId2)

            => evccId1.CompareTo(evccId2) > 0;

        #endregion

        #region Operator >= (evccId1, evccId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="evccId1">An EVCC identification.</param>
        /// <param name="evccId2">Another EVCC identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator >= (EVCC_Id evccId1,
                                           EVCC_Id evccId2)

            => evccId1.CompareTo(evccId2) >= 0;

        #endregion

        #endregion

        #region IComparable<EVCCId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two EVCC identifications.
        /// </summary>
        /// <param name="Object">An EVCC identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is EVCC_Id evccId
                   ? CompareTo(evccId)
                   : throw new ArgumentException("The given object is not an EVCC identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(EVCCId)

        /// <summary>
        /// Compares two EVCC identifications.
        /// </summary>
        /// <param name="EVCCId">An EVCC identification to compare with.</param>
        public Int32 CompareTo(EVCC_Id EVCCId)

            => String.Compare(InternalId,
                              EVCCId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<EVCCId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two EVCC identifications for equality.
        /// </summary>
        /// <param name="Object">An EVCC identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EVCC_Id evccId &&
                   Equals(evccId);

        #endregion

        #region Equals(EVCCId)

        /// <summary>
        /// Compares two EVCC identifications for equality.
        /// </summary>
        /// <param name="EVCCId">An EVCC identification to compare with.</param>
        public Boolean Equals(EVCC_Id EVCCId)

            => String.Equals(InternalId,
                             EVCCId.InternalId,
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
