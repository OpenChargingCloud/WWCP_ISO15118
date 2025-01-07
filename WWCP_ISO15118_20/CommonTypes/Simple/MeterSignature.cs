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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonTypes
{

    /// <summary>
    /// Extension methods for meter signatures.
    /// </summary>
    public static class MeterSignatureExtensions
    {

        /// <summary>
        /// Indicates whether this meter signature is null or empty.
        /// </summary>
        /// <param name="MeterSignature">A meter signature.</param>
        public static Boolean IsNullOrEmpty(this MeterSignature? MeterSignature)
            => !MeterSignature.HasValue || MeterSignature.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this meter signature is null or empty.
        /// </summary>
        /// <param name="MeterSignature">A meter signature.</param>
        public static Boolean IsNotNullOrEmpty(this MeterSignature? MeterSignature)
            => MeterSignature.HasValue && MeterSignature.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// A meter signature.
    /// Max length: 64
    /// </summary>
    public readonly struct MeterSignature : IId,
                                            IEquatable<MeterSignature>,
                                            IComparable<MeterSignature>
    {

        #region Data

        /// <summary>
        /// The internal signature.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this signature is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this signature is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the meter signature.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new meter signature based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a meter signature.</param>
        private MeterSignature(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) NewRandom()

        /// <summary>
        /// Create a new random meter signature.
        /// </summary>
        public static MeterSignature NewRandom()

            => new (new Guid().ToString());

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a meter signature.
        /// </summary>
        /// <param name="Text">A text representation of a meter signature.</param>
        public static MeterSignature Parse(String Text)
        {

            if (TryParse(Text, out var meterSignature))
                return meterSignature;

            throw new ArgumentException("Invalid text representation of a meter signature: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a meter signature.
        /// </summary>
        /// <param name="Text">A text representation of a meter signature.</param>
        public static MeterSignature? TryParse(String Text)
        {

            if (TryParse(Text, out var meterSignature))
                return meterSignature;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out MeterSignature)

        /// <summary>
        /// Try to parse the given text as a meter signature.
        /// </summary>
        /// <param name="Text">A text representation of a meter signature.</param>
        /// <param name="MeterSignature">The parsed meter signature.</param>
        public static Boolean TryParse(String Text, out MeterSignature MeterSignature)
        {

            Text = Text.Trim();

            //ToDo: Max length: 64

            if (Text.IsNotNullOrEmpty())
            {
                MeterSignature = new MeterSignature(Text);
                return true;
            }

            MeterSignature = default;
            return false;

        }

        #endregion

        #region Clone

        /// <summary>
        /// Clone this meter signature.
        /// </summary>
        public MeterSignature Clone

            => new(
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (MeterSignature1, MeterSignature2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="MeterSignature1">A meter signature.</param>
        /// <param name="MeterSignature2">Another meter signature.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (MeterSignature MeterSignature1,
                                           MeterSignature MeterSignature2)

            => MeterSignature1.Equals(MeterSignature2);

        #endregion

        #region Operator != (MeterSignature1, MeterSignature2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="MeterSignature1">A meter signature.</param>
        /// <param name="MeterSignature2">Another meter signature.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (MeterSignature MeterSignature1,
                                           MeterSignature MeterSignature2)

            => !MeterSignature1.Equals(MeterSignature2);

        #endregion

        #region Operator <  (MeterSignature1, MeterSignature2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="MeterSignature1">A meter signature.</param>
        /// <param name="MeterSignature2">Another meter signature.</param>
        /// <returns>true|false</returns>
        public static Boolean operator < (MeterSignature MeterSignature1,
                                          MeterSignature MeterSignature2)

            => MeterSignature1.CompareTo(MeterSignature2) < 0;

        #endregion

        #region Operator <= (MeterSignature1, MeterSignature2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="MeterSignature1">A meter signature.</param>
        /// <param name="MeterSignature2">Another meter signature.</param>
        /// <returns>true|false</returns>
        public static Boolean operator <= (MeterSignature MeterSignature1,
                                           MeterSignature MeterSignature2)

            => MeterSignature1.CompareTo(MeterSignature2) <= 0;

        #endregion

        #region Operator >  (MeterSignature1, MeterSignature2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="MeterSignature1">A meter signature.</param>
        /// <param name="MeterSignature2">Another meter signature.</param>
        /// <returns>true|false</returns>
        public static Boolean operator > (MeterSignature MeterSignature1,
                                          MeterSignature MeterSignature2)

            => MeterSignature1.CompareTo(MeterSignature2) > 0;

        #endregion

        #region Operator >= (MeterSignature1, MeterSignature2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="MeterSignature1">A meter signature.</param>
        /// <param name="MeterSignature2">Another meter signature.</param>
        /// <returns>true|false</returns>
        public static Boolean operator >= (MeterSignature MeterSignature1,
                                           MeterSignature MeterSignature2)

            => MeterSignature1.CompareTo(MeterSignature2) >= 0;

        #endregion

        #endregion

        #region IComparable<MeterSignature> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two meter signatures.
        /// </summary>
        /// <param name="Object">A meter signature to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is MeterSignature meterSignature
                   ? CompareTo(meterSignature)
                   : throw new ArgumentException("The given object is not a meter signature!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(MeterSignature)

        /// <summary>
        /// Compares two meter signatures.
        /// </summary>
        /// <param name="MeterSignature">A meter signature to compare with.</param>
        public Int32 CompareTo(MeterSignature MeterSignature)

            => String.Compare(InternalId,
                              MeterSignature.InternalId,
                              StringComparison.Ordinal);

        #endregion

        #endregion

        #region IEquatable<MeterSignature> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two meter signatures for equality.
        /// </summary>
        /// <param name="Object">A meter signature to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is MeterSignature meterSignature &&
                   Equals(meterSignature);

        #endregion

        #region Equals(MeterSignature)

        /// <summary>
        /// Compares two meter signatures for equality.
        /// </summary>
        /// <param name="MeterSignature">A meter signature to compare with.</param>
        public Boolean Equals(MeterSignature MeterSignature)

            => String.Equals(InternalId,
                             MeterSignature.InternalId,
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
