/*
 * Copyright (c) 2021-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

using cloud.charging.open.protocols.ISO15118.AppProtocol;
using cloud.charging.open.protocols.ISO15118_2.Generated;
using cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated;
using cloud.charging.open.protocols.ISO15118_20.DC.Generated;
using cloud.charging.open.protocols.ISO15118_20.AC.Generated;
using cloud.charging.open.protocols.ISO15118_20.WPT.Generated;
using cloud.charging.open.protocols.ISO15118_20.ACDP.Generated;

namespace cloud.charging.open.protocols.ISO15118.EXI.Dispatch
{
    /// <summary>
    /// Maps V2GTP payload types (§ the 8-byte <see cref="V2GTP"/> header) to the message set that owns
    /// them, so a transport layer can decode an incoming frame without knowing in advance which of the
    /// seven generated/hand-written codecs applies, and wrap an already-EXI-encoded payload with the
    /// header for the set it came from. An unknown payload type is reported as a clean error rather than
    /// guessed at.
    /// </summary>
    public static class V2GTPDispatcher
    {
        /// <summary>
        /// Reads the V2GTP header, validates the length field against what the frame actually carries,
        /// resolves the payload type to a <see cref="MessageSet"/>, and decodes the payload with that
        /// set's generated <c>DecodeAny</c>. A malformed header, a length mismatch, or an unrecognised
        /// payload type is a clean <c>false</c> + <paramref name="error"/> — malformed EXI *within* a
        /// recognised set still throws <see cref="InvalidDataException"/> from the underlying codec, the
        /// same as calling that codec's <c>DecodeAny</c> directly.
        /// </summary>
        public static bool TryDecode(
            ReadOnlySpan<byte> frame, out MessageSet set, out object? message, out string? error)
        {
            set = default;
            message = null;
            error = null;

            if (!V2GTP.TryReadHeader(frame, out ushort payloadType, out uint payloadLength))
            {
                error = "not a valid V2GTP frame (bad version bytes, or too short for the 8-byte header).";
                return false;
            }

            var payload = frame[V2GTP.HeaderSize..];
            if (payloadLength != (uint)payload.Length)
            {
                error = $"payload length mismatch: header declares {payloadLength} byte(s), " +
                        $"frame carries {payload.Length}.";
                return false;
            }

            switch (payloadType)
            {
                // NB: the SupportedAppProtocol handshake shares payload id 0x8001 with the DIN/-2 messages
                // (see V2GTP.PayloadType_AppProtocol) and is disambiguated by session phase, not payload type —
                // so it is decoded explicitly by the SAP handshake, never through this payload-type dispatcher.
                // 0x8001 here therefore resolves to the -2 message set.
                case V2GTP.PayloadType_DinIso2Main:
                    set = MessageSet.Iso15118_2;
                    message = Iso2Codec.DecodeAny(payload, out _);
                    return true;

                case V2GTP.PayloadType_Iso20Main:
                    set = MessageSet.Iso20CommonMessages;
                    message = CommonMessagesCodec.DecodeAny(payload, out _);
                    return true;

                case V2GTP.PayloadType_Iso20AC:
                    set = MessageSet.Iso20AC;
                    message = AcCodec.DecodeAny(payload, out _);
                    return true;

                case V2GTP.PayloadType_Iso20DC:
                    set = MessageSet.Iso20DC;
                    message = DcCodec.DecodeAny(payload, out _);
                    return true;

                case V2GTP.PayloadType_Iso20WPT:
                    set = MessageSet.Iso20WPT;
                    message = WptCodec.DecodeAny(payload, out _);
                    return true;

                case V2GTP.PayloadType_Iso20ACDP:
                    set = MessageSet.Iso20ACDP;
                    message = AcdpCodec.DecodeAny(payload, out _);
                    return true;

                default:
                    error = $"unknown V2GTP payload type 0x{payloadType:X4}.";
                    return false;
            }
        }

        /// <summary>Prepends the V2GTP header for <paramref name="set"/> to an already-EXI-encoded
        /// payload. <c>false</c> if <paramref name="dest"/> is too small; never inspects the payload
        /// bytes themselves (encoding is still each set's own generated <c>TryEncode</c>).</summary>
        public static bool TryEncode(
            MessageSet set, ReadOnlySpan<byte> exiPayload, Span<byte> dest, out int bytesWritten)
        {
            bytesWritten = 0;
            if (dest.Length < V2GTP.HeaderSize + exiPayload.Length) return false;

            ushort payloadType = set switch
            {
                MessageSet.AppProtocol         => V2GTP.PayloadType_AppProtocol,
                MessageSet.Iso15118_2          => V2GTP.PayloadType_DinIso2Main,
                MessageSet.Iso20CommonMessages => V2GTP.PayloadType_Iso20Main,
                MessageSet.Iso20AC             => V2GTP.PayloadType_Iso20AC,
                MessageSet.Iso20DC             => V2GTP.PayloadType_Iso20DC,
                MessageSet.Iso20WPT            => V2GTP.PayloadType_Iso20WPT,
                MessageSet.Iso20ACDP           => V2GTP.PayloadType_Iso20ACDP,
                _ => throw new ArgumentOutOfRangeException(nameof(set), set, "unknown message set"),
            };

            V2GTP.WriteHeader(dest, payloadType, (uint)exiPayload.Length);
            exiPayload.CopyTo(dest[V2GTP.HeaderSize..]);
            bytesWritten = V2GTP.HeaderSize + exiPayload.Length;
            return true;
        }
    }
}
