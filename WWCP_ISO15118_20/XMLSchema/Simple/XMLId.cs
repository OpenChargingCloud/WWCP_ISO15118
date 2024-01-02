/*
 * Copyright (c) 2021-2024 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

namespace cloud.charging.open.protocols.ISO15118_20.XMLSchema
{

    /// <summary>
    /// Extension methods for XML identifications.
    /// </summary>
    public static class XMLIdExtensions
    {

        /// <summary>
        /// Indicates whether this XML identification is null or empty.
        /// </summary>
        /// <param name="XMLId">A XML identification.</param>
        public static Boolean IsNullOrEmpty(this XML_Id? XMLId)
            => !XMLId.HasValue || XMLId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this XML identification is null or empty.
        /// </summary>
        /// <param name="XMLId">A XML identification.</param>
        public static Boolean IsNotNullOrEmpty(this XML_Id? XMLId)
            => XMLId.HasValue && XMLId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// A XML identification.
    /// 
    /// xs:pattern value="[\i-[:]][\c-[:]]*"/
    /// </summary>
    public readonly struct XML_Id : IId,
                                    IEquatable<XML_Id>,
                                    IComparable<XML_Id>
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
        /// The length of the XML identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new XML identification based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a XML identification.</param>
        private XML_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a XML identification.
        /// </summary>
        /// <param name="Text">A text representation of a XML identification.</param>
        public static XML_Id Parse(String Text)
        {

            if (TryParse(Text, out var sessionId))
                return sessionId;

            throw new ArgumentException("Invalid text representation of a XML identification: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a XML identification.
        /// </summary>
        /// <param name="Text">A text representation of a XML identification.</param>
        public static XML_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var sessionId))
                return sessionId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out XMLId)

        /// <summary>
        /// Try to parse the given text as a XML identification.
        /// </summary>
        /// <param name="Text">A text representation of a XML identification.</param>
        /// <param name="XMLId">The parsed XML identification.</param>
        public static Boolean TryParse(String Text, out XML_Id XMLId)
        {

            Text = Text.Trim();

            //ToDo: xs:hexBinary, length: 8

            if (Text.IsNotNullOrEmpty())
            {
                XMLId = new XML_Id(Text);
                return true;
            }

            XMLId = default;
            return false;

        }

        #endregion

        #region Clone

        /// <summary>
        /// Clone this XML identification.
        /// </summary>
        public XML_Id Clone

            => new(
                   new String(InternalId?.ToCharArray())
               );

        #endregion


        #region Operator overloading

        #region Operator == (XMLId1, XMLId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="XMLId1">A XML identification.</param>
        /// <param name="XMLId2">Another XML identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (XML_Id XMLId1,
                                           XML_Id XMLId2)

            => XMLId1.Equals(XMLId2);

        #endregion

        #region Operator != (XMLId1, XMLId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="XMLId1">A XML identification.</param>
        /// <param name="XMLId2">Another XML identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (XML_Id XMLId1,
                                           XML_Id XMLId2)

            => !XMLId1.Equals(XMLId2);

        #endregion

        #region Operator <  (XMLId1, XMLId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="XMLId1">A XML identification.</param>
        /// <param name="XMLId2">Another XML identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator < (XML_Id XMLId1,
                                          XML_Id XMLId2)

            => XMLId1.CompareTo(XMLId2) < 0;

        #endregion

        #region Operator <= (XMLId1, XMLId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="XMLId1">A XML identification.</param>
        /// <param name="XMLId2">Another XML identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator <= (XML_Id XMLId1,
                                           XML_Id XMLId2)

            => XMLId1.CompareTo(XMLId2) <= 0;

        #endregion

        #region Operator >  (XMLId1, XMLId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="XMLId1">A XML identification.</param>
        /// <param name="XMLId2">Another XML identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator > (XML_Id XMLId1,
                                          XML_Id XMLId2)

            => XMLId1.CompareTo(XMLId2) > 0;

        #endregion

        #region Operator >= (XMLId1, XMLId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="XMLId1">A XML identification.</param>
        /// <param name="XMLId2">Another XML identification.</param>
        /// <returns>true|false</returns>
        public static Boolean operator >= (XML_Id XMLId1,
                                           XML_Id XMLId2)

            => XMLId1.CompareTo(XMLId2) >= 0;

        #endregion

        #endregion

        #region IComparable<XMLId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two XML identifications.
        /// </summary>
        /// <param name="Object">A XML identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is XML_Id sessionId
                   ? CompareTo(sessionId)
                   : throw new ArgumentException("The given object is not a XML identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(XMLId)

        /// <summary>
        /// Compares two XML identifications.
        /// </summary>
        /// <param name="XMLId">A XML identification to compare with.</param>
        public Int32 CompareTo(XML_Id XMLId)

            => String.Compare(InternalId,
                              XMLId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<XMLId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two XML identifications for equality.
        /// </summary>
        /// <param name="Object">A XML identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is XML_Id sessionId &&
                   Equals(sessionId);

        #endregion

        #region Equals(XMLId)

        /// <summary>
        /// Compares two XML identifications for equality.
        /// </summary>
        /// <param name="XMLId">A XML identification to compare with.</param>
        public Boolean Equals(XML_Id XMLId)

            => String.Equals(InternalId,
                             XMLId.InternalId,
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
