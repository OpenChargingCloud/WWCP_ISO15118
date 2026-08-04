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

namespace cloud.charging.open.protocols.ISO15118.EXI
{
    /// <summary>
    /// Bit-level writer over a <see cref="Span{Byte}"/>.
    /// <para>
    /// EXI bit-packed alignment is MSB-first within each byte: the first bit written
    /// occupies bit 7 (0x80) of byte 0, the second bit occupies bit 6 (0x40), and so on.
    /// </para>
    /// <para>
    /// As a <c>ref struct</c> this lives on the stack only — no allocations, and the
    /// compiler prevents accidental boxing or async capture.
    /// </para>
    /// </summary>
    public ref struct BitWriter
    {
        private readonly Span<byte> _buffer;
        private int _bitPos;

        public BitWriter(Span<byte> buffer)
        {
            _buffer = buffer;
            _bitPos = 0;
            // The destination need NOT be zero-initialised: every byte is cleared as it is first
            // reached (see WriteBit). It used to be the caller's job, and the trailing partial byte
            // was the one nobody could do it for — see the note there.
        }

        public readonly int BitsWritten => _bitPos;
        public readonly int BytesWritten => (_bitPos + 7) >> 3;

        /// <summary>
        /// Write the lowest <paramref name="numBits"/> of <paramref name="value"/>, MSB first.
        /// </summary>
        public void WriteBits(uint value, int numBits)
        {
            if ((uint)numBits > 32)
                throw new ArgumentOutOfRangeException(nameof(numBits));

            for (int i = numBits - 1; i >= 0; i--)
                WriteBit(((value >> i) & 1u) != 0u);
        }

        public void WriteBit(bool b)
        {
            int byteIdx = _bitPos >> 3;
            int bit     = _bitPos & 7;

            // Clear each byte as it is first reached, rather than only overwriting the bits actually
            // written. Both matter for a reused (non-zeroed) destination, but for different reasons:
            // stale 1-bits inside the message would corrupt it, and stale bits in the trailing
            // PARTIAL byte — the padding no one ever writes — travel silently. That last case is real
            // and was found by re-recording a session trace: two runs of the identical -20
            // ServiceDiscoveryRes differed in the low six bits of their final byte, which held
            // leftovers of the AuthorizationSetupRes encoded into the same buffer one message earlier
            // (its random GenChallenge is what made the difference visible at all). Up to seven bits of
            // the previous message go on the wire that way; with a PnC session in that buffer they are
            // bits of a contract certificate or a signature.
            //
            // Neither existing gate could see it. A round trip never reads padding, and the vector
            // corpus always encodes into a fresh — therefore zeroed — buffer, so the recorded bytes
            // are the ones this now always produces.
            if (bit == 0)
                _buffer[byteIdx] = 0;

            if (b)
                _buffer[byteIdx] |= (byte)(1 << (7 - bit));

            _bitPos++;
        }

        /// <summary>Pad to the next byte boundary. The skipped bits are already zero: the byte was
        /// cleared when <see cref="WriteBit"/> first reached it.</summary>
        public void AlignToByte()
        {
            int rem = _bitPos & 7;
            if (rem != 0) _bitPos += 8 - rem;
        }
    }
}
