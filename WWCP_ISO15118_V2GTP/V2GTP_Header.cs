/*
 * Copyright (c) 2014-2025 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP ISO/IEC 15118 <https://github.com/OpenChargingCloud/WWCP_ISO15118>
 *
 * Licensed under the Affero GPL license, Version 3.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.gnu.org/licenses/agpl.html
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#region Usings

using System.Buffers.Binary;

#endregion

namespace cloud.charging.open.protocols.ISO15118.V2GTP
{

    /// <summary>
    /// The 8-byte V2GTP header that prefixes every V2G application-layer message
    /// (SDP, SAP, EXI). All multi-byte fields are network byte order (big-endian).
    ///
    /// Wire format:
    ///
    ///   0                   1                   2                   3
    ///   0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7
    ///  +---------------+---------------+-------------------------------+
    ///  |ProtocolVersion|InverseVersion |          PayloadType          |
    ///  +---------------+---------------+-------------------------------+
    ///  |                         PayloadLength                         |
    ///  +---------------------------------------------------------------+
    /// </summary>
    public readonly record struct V2GTP_Header(Byte               ProtocolVersion,
                                               Byte               InverseProtocolVersion,
                                               V2GTP_PayloadType  PayloadType,
                                               UInt32             PayloadLength)
    {

        public const Int32 Size = 8;

        /// <summary>
        /// Build a header with the canonical (correct) protocol version bytes.
        /// </summary>
        public static V2GTP_Header Standard(V2GTP_PayloadType  PayloadType,
                                            UInt32             PayloadLength)

            => new (
                   V2GTP_ProtocolVersion.Current,
                   V2GTP_ProtocolVersion.Inverse,
                   PayloadType,
                   PayloadLength
               );

        /// <summary>
        /// True iff the header passes the "version + inverse-version are
        /// bitwise complements" sanity check defined by 15118-2 §7.8.2.
        /// </summary>
        public Boolean IsVersionValid

            => ProtocolVersion        == V2GTP_ProtocolVersion.Current &&
               InverseProtocolVersion == V2GTP_ProtocolVersion.Inverse;


        #region Encoding

        /// <summary>
        /// Encode the header into <paramref name="Destination"/> (must be ≥ 8 bytes).
        /// </summary>
        public void WriteTo(Span<Byte> Destination)
        {

            if (Destination.Length < Size)
                throw new ArgumentException($"Destination buffer is too small: {Destination.Length} < {Size}.", nameof(Destination));

            Destination[0] = ProtocolVersion;
            Destination[1] = InverseProtocolVersion;
            BinaryPrimitives.WriteUInt16BigEndian(Destination[2..], (UInt16) PayloadType);
            BinaryPrimitives.WriteUInt32BigEndian(Destination[4..], PayloadLength);

        }

        /// <summary>
        /// Allocate a new 8-byte array containing the encoded header.
        /// </summary>
        public Byte[] ToArray()
        {
            var buf = new Byte[Size];
            WriteTo(buf);
            return buf;
        }

        #endregion

        #region Decoding

        /// <summary>
        /// Parse a header from the start of <paramref name="Source"/>.
        /// Performs the version-complement check; throws on malformed headers.
        /// </summary>
        public static V2GTP_Header Parse(ReadOnlySpan<Byte> Source)
        {

            var h = ParseRaw(Source);

            if (!h.IsVersionValid)
                throw new V2GTP_ProtocolVersionException(
                          h.ProtocolVersion,
                          h.InverseProtocolVersion
                      );

            return h;

        }

        /// <summary>
        /// Parse without the version-complement check. Useful for pentest tooling
        /// that wants to inspect deliberately malformed headers.
        /// </summary>
        public static V2GTP_Header ParseRaw(ReadOnlySpan<Byte> Source)
        {

            if (Source.Length < Size)
                throw new V2GTP_TruncatedException(Source.Length);

            return new V2GTP_Header(
                       ProtocolVersion:         Source[0],
                       InverseProtocolVersion:  Source[1],
                       PayloadType:             (V2GTP_PayloadType) BinaryPrimitives.ReadUInt16BigEndian(Source[2..4]),
                       PayloadLength:                               BinaryPrimitives.ReadUInt32BigEndian(Source[4..8])
                   );

        }

        /// <summary>
        /// Try to parse, returning <c>false</c> instead of throwing.
        /// Does not enforce version validity – caller checks <see cref="IsVersionValid"/>.
        /// </summary>
        public static Boolean TryParseRaw(ReadOnlySpan<Byte>  Source,
                                          out V2GTP_Header    Header)
        {

            if (Source.Length < Size)
            {
                Header = default;
                return false;
            }

            Header = ParseRaw(Source);
            return true;

        }

        #endregion

    }

}
