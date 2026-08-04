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

namespace cloud.charging.open.protocols.ISO15118.EXI
{
    /// <summary>
    /// Bit-level reader over a <see cref="ReadOnlySpan{Byte}"/>, MSB-first to match EXI bit-packed alignment.
    /// </summary>
    public ref struct BitReader
    {
        private readonly ReadOnlySpan<byte> _buffer;
        private int _bitPos;

        /// <summary>
        /// The EXI string value-table partitions for this stream, created on first use.
        /// </summary>
        /// <remarks>
        /// It hangs off the reader so the generated decoders need no extra parameter threaded
        /// through every call — a value read only has to name its own slot. The encode path has no
        /// counterpart on purpose: cbV2G is miss-only, every checked-in vector is its output, and
        /// an encoder that started emitting hits would invalidate all of them.
        /// </remarks>
        public ExiStringTable StringTable => _stringTable ??= new ExiStringTable();
        private ExiStringTable? _stringTable;

        public BitReader(ReadOnlySpan<byte> buffer)
        {
            _buffer = buffer;
            _bitPos = 0;
            _stringTable = null;
        }

        public readonly int BitsRead => _bitPos;
        public readonly int BytesConsumed => (_bitPos + 7) >> 3;

        public bool ReadBit()
        {
            int byteIdx = _bitPos >> 3;
            int bitInByte = _bitPos & 7;
            if (byteIdx >= _buffer.Length)
                throw new EndOfStreamException("EXI bitstream exhausted");
            bool bit = ((_buffer[byteIdx] >> (7 - bitInByte)) & 1) != 0;
            _bitPos++;
            return bit;
        }

        public uint ReadBits(int numBits)
        {
            if ((uint)numBits > 32)
                throw new ArgumentOutOfRangeException(nameof(numBits));
            uint value = 0;
            for (int i = 0; i < numBits; i++)
                value = (value << 1) | (ReadBit() ? 1u : 0u);
            return value;
        }
    }
}
