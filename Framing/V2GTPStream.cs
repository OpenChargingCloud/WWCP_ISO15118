/*
 * Copyright (c) 2021-2025 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP ISO/IEC 15118 <https://github.com/OpenChargingCloud/WWCP_ISO15118>
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

using cloud.charging.open.protocols.ISO15118.EXI.Dispatch;

namespace Vanaheimr.V2G.Simulation.Framing
{
    /// <summary>
    /// Reads and writes single V2GTP frames on a <see cref="Stream"/> — the one place in this project
    /// that touches the transport octets directly. Works unchanged over a plain <see cref="System.Net.Sockets.NetworkStream"/>
    /// or an authenticated <see cref="System.Net.Security.SslStream"/>; everything above this layer only
    /// ever sees a <see cref="MessageSet"/> and a decoded message object.
    /// </summary>
    public static class V2GTPStream
    {
        /// <summary>
        /// Reads one V2GTP frame: the 8-byte header, then exactly as many payload bytes as it declares,
        /// then hands the whole frame to <see cref="V2GTPDispatcher.TryDecode"/>. Throws
        /// <see cref="InvalidDataException"/> if the header is malformed, the peer closes mid-frame, or the
        /// payload type is unrecognised — there is no "try" variant because a broken frame is not a
        /// recoverable condition for a session peer.
        /// </summary>
        public static async Task<(MessageSet Set, object Message)> ReadFrameAsync(Stream stream, CancellationToken ct = default)
        {
            var (frame, _) = await ReadRawFrameAsync(stream, ct).ConfigureAwait(false);

            if (!V2GTPDispatcher.TryDecode(frame, out var set, out var message, out var error))
                throw new InvalidDataException($"V2GTP frame: {error}");

            return (set, message!);
        }

        /// <summary>
        /// Reads one V2GTP frame at the transport level — the 8-byte header plus its declared payload — and
        /// returns the whole frame together with its payload type, WITHOUT resolving it to a message set.
        /// Used by the SupportedAppProtocol handshake, which shares payload id 0x8001 with the -2 messages and
        /// so cannot be routed by payload type alone (see <see cref="V2GTP.PayloadType_AppProtocol"/>).
        /// </summary>
        public static async Task<(byte[] Frame, ushort PayloadType)> ReadRawFrameAsync(Stream stream, CancellationToken ct = default)
        {
            var frame = new byte[V2GTP.HeaderSize];
            try
            {
                await stream.ReadExactlyAsync(frame, ct).ConfigureAwait(false);
            }
            catch (EndOfStreamException ex)
            {
                throw new InvalidDataException("V2GTP frame: connection closed before a full 8-byte header arrived.", ex);
            }

            if (!V2GTP.TryReadHeader(frame, out ushort payloadType, out uint payloadLength))
                throw new InvalidDataException("V2GTP frame: bad version/type bytes in the 8-byte header.");

            // Before the allocation, not after. The length is the peer's word for how much memory to
            // set aside, and `checked` would only turn the largest lies into an OverflowException
            // while still honouring a 2 GiB one. See V2GTP.MaximumPayloadBytes.
            if (payloadLength > V2GTP.MaximumPayloadBytes)
                throw new InvalidDataException(
                    $"V2GTP frame: a frame of payload type 0x{payloadType:x4} declares {payloadLength} " +
                    $"payload byte(s); this reader accepts at most {V2GTP.MaximumPayloadBytes}.");

            Array.Resize(ref frame, V2GTP.HeaderSize + (int)payloadLength);
            if (payloadLength > 0)
            {
                try
                {
                    await stream.ReadExactlyAsync(frame.AsMemory(V2GTP.HeaderSize), ct).ConfigureAwait(false);
                }
                catch (EndOfStreamException ex)
                {
                    throw new InvalidDataException(
                        $"V2GTP frame: connection closed after {frame.Length - V2GTP.HeaderSize} of {payloadLength} declared payload byte(s).", ex);
                }
            }

            return (frame, payloadType);
        }

        /// <summary>
        /// Encodes one already-EXI-encoded payload with the V2GTP header for <paramref name="set"/> and
        /// writes + flushes it to <paramref name="stream"/> in one call.
        /// </summary>
        public static async Task WriteFrameAsync(
            Stream stream, MessageSet set, ReadOnlyMemory<byte> exiPayload, CancellationToken ct = default)
        {
            var dest = new byte[V2GTP.HeaderSize + exiPayload.Length];
            if (!V2GTPDispatcher.TryEncode(set, exiPayload.Span, dest, out int bytesWritten))
                throw new InvalidOperationException("V2GTP frame: encode failed (payload too large for its length field?).");

            await stream.WriteAsync(dest.AsMemory(0, bytesWritten), ct).ConfigureAwait(false);
            await stream.FlushAsync(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Writes one V2GTP frame with an explicit payload type — used by the SupportedAppProtocol handshake,
        /// which frames with payload id 0x8001 directly rather than through the message-set dispatcher (SAP
        /// and -2 share that id; see <see cref="V2GTP.PayloadType_AppProtocol"/>).
        /// </summary>
        public static async Task WriteRawFrameAsync(
            Stream stream, ushort payloadType, ReadOnlyMemory<byte> exiPayload, CancellationToken ct = default)
        {
            var dest = new byte[V2GTP.HeaderSize + exiPayload.Length];
            V2GTP.WriteHeader(dest, payloadType, (uint)exiPayload.Length);
            exiPayload.Span.CopyTo(dest.AsSpan(V2GTP.HeaderSize));

            await stream.WriteAsync(dest, ct).ConfigureAwait(false);
            await stream.FlushAsync(ct).ConfigureAwait(false);
        }
    }
}
