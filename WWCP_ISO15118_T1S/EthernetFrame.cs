/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

#endregion

namespace cloud.charging.open.protocols.ISO15118.T1S
{

    /// <summary>
    /// An Ethernet II frame: destination, source, EtherType, payload. What a
    /// 10BASE-T1S PHY carries and what the emulation carries in its place.
    /// </summary>
    /// <remarks>
    /// The bytes here are exactly what a real adapter would put on the wire,
    /// minus the frame check sequence the hardware adds. That is the point of
    /// keeping the format rather than inventing a UDP one: a transport over a
    /// real 10BASE-T1S adapter - AF_PACKET on Linux, the way the SLAC library
    /// does it for the powerline - drops in below everything above this
    /// without anything above it noticing.
    /// </remarks>
    public sealed record EthernetFrame(MACAddress  Destination,
                                       MACAddress  Source,
                                       UInt16      EtherType,
                                       Byte[]      Payload)
    {

        #region Data

        /// <summary>
        /// Destination (6), source (6), EtherType (2).
        /// </summary>
        public const Int32  HeaderLength  = 14;

        #endregion


        #region Encode()

        /// <summary>
        /// The frame as bytes.
        /// </summary>
        public Byte[] Encode()
        {

            var bytes = new Byte[HeaderLength + Payload.Length];

            Destination.GetBytes().CopyTo(bytes, 0);
            Source.     GetBytes().CopyTo(bytes, 6);

            BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(12, 2), EtherType);

            Payload.CopyTo(bytes, HeaderLength);

            return bytes;

        }

        #endregion

        #region (static) TryDecode(Bytes)

        /// <summary>
        /// A frame out of bytes, or null for anything that is not one.
        /// </summary>
        /// <remarks>
        /// Null rather than an exception, because this is the first thing
        /// every datagram off the medium meets and the medium is shared: one
        /// node's mistake is every other node's malformed input, and a bus
        /// that stops on it is a bus one node can stop.
        /// </remarks>
        public static EthernetFrame? TryDecode(ReadOnlySpan<Byte> Bytes)
        {

            if (Bytes.Length < HeaderLength)
                return null;

            if (Bytes.Length - HeaderLength > T1SConstants.MaxPayloadLength)
                return null;

            return new EthernetFrame(
                       MACAddress.From(Bytes[..6]),
                       MACAddress.From(Bytes[6..12]),
                       BinaryPrimitives.ReadUInt16BigEndian(Bytes[12..14]),
                       Bytes[HeaderLength..].ToArray()
                   );

        }

        #endregion


        #region (override) ToString()

        public override String ToString()
            => $"{Source} -> {Destination}, 0x{EtherType:X4}, {Payload.Length} byte(s)";

        #endregion

    }

}
