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

namespace cloud.charging.open.protocols.ISO15118.V2GTP
{

    /// <summary>
    /// A complete V2GTP frame (header + payload bytes). Use this when you need
    /// to round-trip an entire V2GTP datagram – e.g. between the SDP UDP socket
    /// and the SDP message decoder.
    /// </summary>
    public readonly record struct V2GTP_Frame(V2GTP_Header          Header,
                                              ReadOnlyMemory<Byte>  Payload)
    {
        public Int32 TotalLength
            => V2GTP_Header.Size + Payload.Length;

        /// <summary>
        /// Encode header + payload into a single new array.
        /// </summary>
        public Byte[] ToArray()
        {

            var buf = new Byte[TotalLength];
            Header.WriteTo(buf);
            Payload.Span.CopyTo(buf.AsSpan(V2GTP_Header.Size));

            return buf;

        }

        /// <summary>
        /// Build a standard frame from <paramref name="payloadType"/> and a payload
        /// buffer, computing the length and version bytes automatically.
        /// </summary>
        public static V2GTP_Frame Wrap(V2GTP_PayloadType     PayloadType,
                                       ReadOnlyMemory<Byte>  Payload)

            => new (
                   V2GTP_Header.Standard(
                       PayloadType,
                       (UInt32) Payload.Length
                   ),
                   Payload
               );

        /// <summary>
        /// Parse a complete V2GTP frame from a buffer. The header is validated
        /// (version-complement check); the payload length is checked against
        /// the buffer size.
        /// </summary>
        public static V2GTP_Frame Parse(ReadOnlyMemory<Byte> Source)
        {

            var header   = V2GTP_Header.Parse(Source.Span);
            var have     = Source.Length - V2GTP_Header.Size;

            if ((UInt32) have < header.PayloadLength)
                throw new V2GTP_PayloadLengthException(
                          header.PayloadLength,
                          have
                      );

            var payload  = Source.Slice(
                               V2GTP_Header.Size,
                               (Int32) header.PayloadLength
                           );

            return new V2GTP_Frame(
                       header,
                       payload
                   );

        }

        /// <summary>
        /// Lenient parse: does not enforce version-complement validity and does
        /// not enforce length consistency. Useful for pentest tooling that wants
        /// to inspect anything that arrives on the wire.
        /// </summary>
        public static V2GTP_Frame ParseRaw(ReadOnlyMemory<Byte> Source)
        {

            var header   = V2GTP_Header.ParseRaw(Source.Span);

            var have     = Source.Length - V2GTP_Header.Size;

            var take     = Math.Min(
                               (Int32) Math.Min(
                                   header.PayloadLength,
                                   Int32.MaxValue
                               ),
                               have
                           );

            var payload  = take > 0
                               ? Source.Slice(V2GTP_Header.Size, take)
                               : ReadOnlyMemory<Byte>.Empty;

            return new V2GTP_Frame(
                       header,
                       payload
                   );

        }

    }

}
