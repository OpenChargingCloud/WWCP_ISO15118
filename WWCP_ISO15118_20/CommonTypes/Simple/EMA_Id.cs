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
    /// Extention methods for e-mobility account identifications.
    /// </summary>
    public static class EMAIdExtensions
    {

        /// <summary>
        /// Indicates whether this e-mobility account identification is null or empty.
        /// </summary>
        /// <param name="EMAId">An e-mobility account identification.</param>
        public static Boolean IsNullOrEmpty(this EMA_Id? EMAId)
            => !EMAId.HasValue || EMAId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this e-mobility account identification is null or empty.
        /// </summary>
        /// <param name="EMAId">An e-mobility account identification.</param>
        public static Boolean IsNotNullOrEmpty(this EMA_Id? EMAId)
            => EMAId.HasValue && EMAId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// An e-mobility account identification.
    /// </summary>
    public readonly struct EMA_Id : IId,
                                    IEquatable<EMA_Id>,
                                    IComparable<EMA_Id>
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
        /// The length of the e-mobility account identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new e-mobility account identification based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of an e-mobility account identification.</param>
        private EMA_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as an e-mobility account identification.
        /// </summary>
        /// <param name="Text">A text representation of an e-mobility account identification.</param>
        public static EMA_Id Parse(String Text)
        {

            if (TryParse(Text, out var evseId))
                return evseId;

            throw new ArgumentException("Invalid text representation of an e-mobility account identification: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as an e-mobility account identification.
        /// </summary>
        /// <param name="Text">A text representation of an e-mobility account identification.</param>
        public static EMA_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var evseId))
                return evseId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out EMAId)

        /// <summary>
        /// Try to parse the given text as an e-mobility account identification.
        /// </summary>
        /// <param name="Text">A text representation of an e-mobility account identification.</param>
        /// <param name="EMAId">The parsed e-mobility account identification.</param>
        public static Boolean TryParse(String Text, out EMA_Id EMAId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                EMAId = new EMA_Id(Text);
                return true;
            }

            EMAId = default;
            return false;

        }

        #endregion

        #region Clone

        /// <summary>
        /// Clone this e-mobility account identification.
        /// </summary>
        public EMA_Id Clone

            => new(
                   new String(InternalId?.ToCharArray())
               );

        #endregion


        #region Operator overloading

        #region Operator == (evseId1, evseId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="evseId1">An e-mobility account identification.</param>
        /// <param name="evseId2">Another e-mobility account identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (EMA_Id evseId1,
                                           EMA_Id evseId2)

            => evseId1.Equals(evseId2);

        #endregion

        #region Operator != (evseId1, evseId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="evseId1">An e-mobility account identification.</param>
        /// <param name="evseId2">Another e-mobility account identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (EMA_Id evseId1,
                                           EMA_Id evseId2)

            => !evseId1.Equals(evseId2);

        #endregion

        #region Operator <  (evseId1, evseId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="evseId1">An e-mobility account identification.</param>
        /// <param name="evseId2">Another e-mobility account identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator < (EMA_Id evseId1,
                                          EMA_Id evseId2)

            => evseId1.CompareTo(evseId2) < 0;

        #endregion

        #region Operator <= (evseId1, evseId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="evseId1">An e-mobility account identification.</param>
        /// <param name="evseId2">Another e-mobility account identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator <= (EMA_Id evseId1,
                                           EMA_Id evseId2)

            => evseId1.CompareTo(evseId2) <= 0;

        #endregion

        #region Operator >  (evseId1, evseId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="evseId1">An e-mobility account identification.</param>
        /// <param name="evseId2">Another e-mobility account identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator > (EMA_Id evseId1,
                                          EMA_Id evseId2)

            => evseId1.CompareTo(evseId2) > 0;

        #endregion

        #region Operator >= (evseId1, evseId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="evseId1">An e-mobility account identification.</param>
        /// <param name="evseId2">Another e-mobility account identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator >= (EMA_Id evseId1,
                                           EMA_Id evseId2)

            => evseId1.CompareTo(evseId2) >= 0;

        #endregion

        #endregion

        #region IComparable<EMAId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two e-mobility account identifications.
        /// </summary>
        /// <param name="Object">An e-mobility account identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is EMA_Id evseId
                   ? CompareTo(evseId)
                   : throw new ArgumentException("The given object is not an e-mobility account identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(EMAId)

        /// <summary>
        /// Compares two e-mobility account identifications.
        /// </summary>
        /// <param name="EMAId">An e-mobility account identification to compare with.</param>
        public Int32 CompareTo(EMA_Id EMAId)

            => String.Compare(InternalId,
                              EMAId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<EMAId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two e-mobility account identifications for equality.
        /// </summary>
        /// <param name="Object">An e-mobility account identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EMA_Id evseId &&
                   Equals(evseId);

        #endregion

        #region Equals(EMAId)

        /// <summary>
        /// Compares two e-mobility account identifications for equality.
        /// </summary>
        /// <param name="EMAId">An e-mobility account identification to compare with.</param>
        public Boolean Equals(EMA_Id EMAId)

            => String.Equals(InternalId,
                             EMAId.InternalId,
                             StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region (override) GetHashCode()

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
