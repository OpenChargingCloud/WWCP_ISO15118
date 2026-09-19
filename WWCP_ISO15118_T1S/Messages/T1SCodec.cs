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

using System.Text;
using System.Buffers.Binary;

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

#endregion

namespace cloud.charging.open.protocols.ISO15118.T1S.Messages
{

    /// <summary>
    /// Messages to payload bytes and back.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every number is big-endian, the way every Ethernet field above it is,
    /// and the way the SLAC library found out the hard way matters: its
    /// HomePlug fields are little-endian two bytes after a big-endian
    /// EtherType, and a frame with the pair the wrong way round is decoded
    /// by nobody. One byte order here, so there is nothing to get wrong.
    /// </para>
    /// <para>
    /// Every layout is fixed and every length is checked before it is read.
    /// <see cref="TryDecode(ReadOnlySpan{Byte})"/> answers null for anything
    /// it will not read, never an exception: it is the first thing every
    /// datagram off a shared medium meets.
    /// </para>
    /// </remarks>
    public static class T1SCodec
    {

        #region Encode(Message)

        /// <summary>
        /// The payload of the frame that carries this message.
        /// </summary>
        public static Byte[] Encode(IT1SMessage Message)
        {

            switch (Message)
            {

                case Beacon beacon:
                {
                    var bytes = new Byte[6];
                    bytes[0]  = (Byte) T1SMessageType.Beacon;
                    BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(1, 4), beacon.Cycle);
                    bytes[5]  = beacon.NodeCount;
                    return bytes;
                }

                case TransmitOpportunity to:
                {
                    var bytes = new Byte[7];
                    bytes[0]  = (Byte) T1SMessageType.TransmitOpportunity;
                    BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(1, 4), to.Cycle);
                    bytes[5]  = to.Slot;
                    bytes[6]  = to.NodeId;
                    return bytes;
                }

                case Yield yield:
                {
                    var bytes = new Byte[6];
                    bytes[0]  = (Byte) T1SMessageType.Yield;
                    BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(1, 4), yield.Cycle);
                    bytes[5]  = yield.NodeId;
                    return bytes;
                }

                case Join join:
                {
                    var name  = NameBytes(join.Name);
                    var bytes = new Byte[8 + name.Length];
                    bytes[0]  = (Byte) T1SMessageType.Join;
                    bytes[1]  = (Byte) join.Role;
                    BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(2, 4), join.Nonce);
                    bytes[6]  = join.Weight;
                    bytes[7]  = (Byte) name.Length;
                    name.CopyTo(bytes, 8);
                    return bytes;
                }

                case Assign assign:
                {
                    var bytes = new Byte[8];
                    bytes[0]  = (Byte) T1SMessageType.Assign;
                    BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(1, 4), assign.Nonce);
                    bytes[5]  = assign.NodeId;
                    bytes[6]  = (Byte) assign.Role;
                    bytes[7]  = assign.Weight;
                    return bytes;
                }

                case Announce announce:
                {
                    var name  = NameBytes(announce.Name);
                    var bytes = new Byte[4 + name.Length];
                    bytes[0]  = (Byte) T1SMessageType.Announce;
                    bytes[1]  = announce.NodeId;
                    bytes[2]  = (Byte) announce.Role;
                    bytes[3]  = (Byte) name.Length;
                    name.CopyTo(bytes, 4);
                    return bytes;
                }

                case Leave leave:
                    return [ (Byte) T1SMessageType.Leave, leave.NodeId ];

                case SensorReading reading:
                {
                    var bytes = new Byte[8];
                    bytes[0]  = (Byte) T1SMessageType.SensorReading;
                    bytes[1]  = reading.NodeId;
                    bytes[2]  = (Byte) reading.Kind;
                    BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(3, 2), reading.Sequence);
                    BinaryPrimitives.WriteInt16BigEndian (bytes.AsSpan(5, 2), reading.Value);
                    bytes[7]  = (Byte) reading.Flags;
                    return bytes;
                }

                case Data data:
                {
                    var bytes = new Byte[1 + data.Payload.Length];
                    bytes[0]  = (Byte) T1SMessageType.Data;
                    data.Payload.CopyTo(bytes, 1);
                    return bytes;
                }

                default:
                    throw new ArgumentException($"'{Message.GetType().Name}' is not a message this codec writes.", nameof(Message));

            }

        }

        #endregion

        #region TryDecode(Payload)

        /// <summary>
        /// The message in a payload, or null for anything that is not one.
        /// </summary>
        public static IT1SMessage? TryDecode(ReadOnlySpan<Byte> Payload)
        {

            if (Payload.Length < 1)
                return null;

            switch ((T1SMessageType) Payload[0])
            {

                case T1SMessageType.Beacon:
                    return Payload.Length == 6
                               ? new Beacon(
                                     BinaryPrimitives.ReadUInt32BigEndian(Payload[1..5]),
                                     Payload[5]
                                 )
                               : null;

                case T1SMessageType.TransmitOpportunity:
                    return Payload.Length == 7
                               ? new TransmitOpportunity(
                                     BinaryPrimitives.ReadUInt32BigEndian(Payload[1..5]),
                                     Payload[5],
                                     Payload[6]
                                 )
                               : null;

                case T1SMessageType.Yield:
                    return Payload.Length == 6
                               ? new Yield(
                                     BinaryPrimitives.ReadUInt32BigEndian(Payload[1..5]),
                                     Payload[5]
                                 )
                               : null;

                case T1SMessageType.Join:
                {

                    if (Payload.Length < 8 || !TryReadName(Payload[7..], out var name))
                        return null;

                    var role = (T1SNodeRole) Payload[1];

                    return Enum.IsDefined(role)
                               ? new Join(
                                     role,
                                     BinaryPrimitives.ReadUInt32BigEndian(Payload[2..6]),
                                     Payload[6],
                                     name
                                 )
                               : null;

                }

                case T1SMessageType.Assign:
                {

                    if (Payload.Length != 8)
                        return null;

                    var role = (T1SNodeRole) Payload[6];

                    return Enum.IsDefined(role)
                               ? new Assign(
                                     BinaryPrimitives.ReadUInt32BigEndian(Payload[1..5]),
                                     Payload[5],
                                     role,
                                     Payload[7]
                                 )
                               : null;

                }

                case T1SMessageType.Announce:
                {

                    if (Payload.Length < 4 || !TryReadName(Payload[3..], out var name))
                        return null;

                    var role = (T1SNodeRole) Payload[2];

                    return Enum.IsDefined(role)
                               ? new Announce(Payload[1], role, name)
                               : null;

                }

                case T1SMessageType.Leave:
                    return Payload.Length == 2
                               ? new Leave(Payload[1])
                               : null;

                case T1SMessageType.SensorReading:
                    return Payload.Length == 8
                               ? new SensorReading(
                                     Payload[1],
                                     (SensorKind) Payload[2],
                                     BinaryPrimitives.ReadUInt16BigEndian(Payload[3..5]),
                                     BinaryPrimitives.ReadInt16BigEndian (Payload[5..7]),
                                     (SensorFlags) Payload[7]
                                 )
                               : null;

                case T1SMessageType.Data:
                    return new Data(Payload[1..].ToArray());

                default:
                    return null;

            }

        }

        #endregion


        #region Frame(Destination, Source, Message) / TryDecode(EthernetFrame)

        /// <summary>
        /// The frame that carries a message.
        /// </summary>
        public static EthernetFrame Frame(MACAddress   Destination,
                                          MACAddress   Source,
                                          IT1SMessage  Message)

            => new (Destination,
                    Source,
                    T1SConstants.EtherType,
                    Encode(Message));


        /// <summary>
        /// The message in a frame, or null when the frame is not one of ours -
        /// another EtherType, which a shared medium may well carry, or a
        /// payload that does not read.
        /// </summary>
        public static IT1SMessage? TryDecode(EthernetFrame Frame)

            => Frame.EtherType == T1SConstants.EtherType
                   ? TryDecode(Frame.Payload.AsSpan())
                   : null;

        #endregion


        #region (private static) NameBytes(Name) / TryReadName(Bytes, out Name)

        /// <summary>
        /// A name as it goes on the wire: UTF-8, cut to what fits.
        /// </summary>
        /// <remarks>
        /// Cut rather than refused, because a name is for a log and a name
        /// too long for the wire is still a name - and a node that cannot
        /// join because somebody called it something elaborate is a node
        /// missing from the bus over a label.
        /// </remarks>
        private static Byte[] NameBytes(String Name)
        {

            var bytes = Encoding.UTF8.GetBytes(Name ?? "");

            if (bytes.Length <= T1SConstants.MaxNameLength)
                return bytes;

            // Cut on a character boundary, not in the middle of one.
            var text = Encoding.UTF8.GetString(bytes, 0, T1SConstants.MaxNameLength);

            while (Encoding.UTF8.GetByteCount(text) > T1SConstants.MaxNameLength)
                text = text[..^1];

            return Encoding.UTF8.GetBytes(text);

        }

        private static Boolean TryReadName(ReadOnlySpan<Byte> Bytes, out String Name)
        {

            Name = "";

            if (Bytes.Length < 1)
                return false;

            var length = Bytes[0];

            if (length > T1SConstants.MaxNameLength || Bytes.Length != 1 + length)
                return false;

            try
            {
                Name = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).
                           GetString(Bytes[1..]);
            }
            catch (DecoderFallbackException)
            {
                return false;
            }

            return true;

        }

        #endregion

    }

}
