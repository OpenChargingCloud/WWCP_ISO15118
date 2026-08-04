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

using System.Text;

namespace cloud.charging.open.protocols.ISO15118.EXI
{
    /// <summary>
    /// EXI primitive type codecs: Unsigned Integer, Signed Integer, n-bit Unsigned Integer,
    /// Binary, Boolean, and String values. All are schema-independent and bit-packed
    /// (MSB-first); the schema-informed grammar layer builds on top of them.
    /// <para>
    /// <b>String values are miss-only here</b> (verbatim value, <c>length+2</c> prefix). This
    /// matches the ISO 15118 wire reality: EVerest's cbexigen/cbV2G — the de-facto reference —
    /// never emits value-table hits and its decoder rejects them
    /// (<c>EXI_ERROR__STRINGVALUES_NOT_SUPPORTED</c>). The full EXI §7.3.3 value-table codec
    /// (hit/miss, local + global partitions) lives in <see cref="ExiStringTable"/> and is used
    /// only to interoperate with stacks that do emit hits (e.g. EXIficient/Josev).
    /// </para>
    /// <para>
    /// <b>Deliberately not implemented:</b> Float, Decimal and DateTime. The ISO 15118-2/-20
    /// schemas do not use them — physical quantities are modelled as multiplier/value integer
    /// pairs (<c>PhysicalValueType</c> / <c>RationalNumberType</c>). Add them only if a real
    /// schema turns out to reference them.
    /// </para>
    /// </summary>
    public static class ExiPrimitives
    {
        /// <summary>
        /// Encode an EXI Unsigned Integer: 7 bits of value per byte, MSB = continuation flag.
        /// </summary>
        public static void WriteUnsignedInteger(ref BitWriter w, ulong value)
        {
            do
            {
                byte chunk = (byte)(value & 0x7F);
                value >>= 7;
                if (value != 0) chunk |= 0x80;
                w.WriteBits(chunk, 8);
            } while (value != 0);
        }

        public static ulong ReadUnsignedInteger(ref BitReader r)
        {
            ulong value = 0;
            int shift = 0;
            while (true)
            {
                byte chunk = (byte)r.ReadBits(8);
                value |= (ulong)(chunk & 0x7F) << shift;
                if ((chunk & 0x80) == 0) return value;
                shift += 7;
                if (shift > 63)
                    throw new InvalidDataException("EXI Unsigned Integer overflow (>64 bits).");
            }
        }

        /// <summary>
        /// EXI string value, "miss" case: <c>UnsignedInteger(charCount + 2)</c> followed by
        /// each Unicode codepoint as <c>UnsignedInteger</c>. The +2 leaves codes 0 and 1
        /// available for local / global value-table hits.
        /// </summary>
        public static void WriteStringValue(ref BitWriter w, string s)
        {
            int runeCount = 0;
            foreach (var _ in s.EnumerateRunes()) runeCount++;

            WriteUnsignedInteger(ref w, (ulong)(runeCount + 2));
            foreach (var rune in s.EnumerateRunes())
                WriteUnsignedInteger(ref w, (ulong)rune.Value);
        }

        /// <summary>
        /// Reads a string value at the given slot, resolving value-table hits against the reader's
        /// own <see cref="BitReader.StringTable"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The slot is the QName local part of the element or attribute whose value this is; EXI
        /// keeps one local value partition per slot, plus one global partition per stream.
        /// </para>
        /// <para>
        /// There is deliberately no encoding counterpart here. cbV2G never emits hits, every
        /// checked-in vector is its output, and an encoder that started emitting them would
        /// invalidate all of them — so <see cref="WriteStringValue"/> stays miss-only while the
        /// decoder accepts what a conforming peer is allowed to send.
        /// </para>
        /// </remarks>
        public static string ReadStringValue(ref BitReader r, string slot) =>
            r.StringTable.ReadStringValue(ref r, slot);

        // -----------------------------------------------------------------------
        //  Signed Integer (EXI §7.1.5)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Encode an EXI Integer: a 1-bit sign (0 = non-negative, 1 = negative) followed by
        /// the magnitude as an Unsigned Integer. For negative values the magnitude is
        /// <c>|value| - 1</c>, so that -1 maps to 0 and there is a single representation of 0.
        /// </summary>
        public static void WriteSignedInteger(ref BitWriter w, long value)
        {
            if (value < 0)
            {
                w.WriteBits(1, 1);
                // -(value + 1) is computed on long to avoid overflow at long.MinValue
                // (where -value would overflow); the result fits in a non-negative long.
                WriteUnsignedInteger(ref w, (ulong)(-(value + 1)));
            }
            else
            {
                w.WriteBits(0, 1);
                WriteUnsignedInteger(ref w, (ulong)value);
            }
        }

        public static long ReadSignedInteger(ref BitReader r)
        {
            bool negative = r.ReadBits(1) != 0;
            ulong mag = ReadUnsignedInteger(ref r);
            if (mag > long.MaxValue)
                throw new InvalidDataException("EXI Signed Integer magnitude out of 64-bit range.");
            return negative ? -(long)mag - 1 : (long)mag;
        }

        // -----------------------------------------------------------------------
        //  Binary — xs:hexBinary / xs:base64Binary (EXI §7.1.1)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Encode EXI Binary: the byte count as an Unsigned Integer, then the raw octets.
        /// On the wire hexBinary and base64Binary are identical — the difference is only in
        /// their lexical (text) form, which EXI never sees.
        /// </summary>
        public static void WriteBinary(ref BitWriter w, ReadOnlySpan<byte> data)
        {
            WriteUnsignedInteger(ref w, (ulong)data.Length);
            foreach (byte b in data)
                w.WriteBits(b, 8);
        }

        public static byte[] ReadBinary(ref BitReader r)
        {
            ulong len = ReadUnsignedInteger(ref r);
            if (len > int.MaxValue)
                throw new InvalidDataException("EXI Binary length out of range.");
            var data = new byte[len];
            for (int i = 0; i < data.Length; i++)
                data[i] = (byte)r.ReadBits(8);
            return data;
        }

        // -----------------------------------------------------------------------
        //  Boolean (EXI §7.1.2, no pattern facet → a single bit)
        // -----------------------------------------------------------------------

        public static void WriteBoolean(ref BitWriter w, bool value) => w.WriteBits(value ? 1u : 0u, 1);

        public static bool ReadBoolean(ref BitReader r) => r.ReadBits(1) != 0u;
    }
}
