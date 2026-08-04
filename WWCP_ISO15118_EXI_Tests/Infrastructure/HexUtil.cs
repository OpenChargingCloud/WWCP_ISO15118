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

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests.Infrastructure
{
    /// <summary>
    /// Hex parsing and human-readable diff output for byte-level test failures.
    /// </summary>
    public static class HexUtil
    {
        /// <summary>
        /// Parse a hex string. Whitespace, commas, colons and a leading "0x" are ignored.
        /// </summary>
        public static byte[] Parse(string hex)
        {
            var sb = new StringBuilder(hex.Length);
            foreach (char c in hex)
            {
                if (char.IsWhiteSpace(c) || c == ',' || c == ':') continue;
                sb.Append(c);
            }
            var s = sb.ToString();
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s[2..];
            if ((s.Length & 1) != 0)
                throw new FormatException("Hex string has odd number of digits.");

            var bytes = new byte[s.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(s.Substring(i * 2, 2), 16);
            return bytes;
        }

        public static string Format(ReadOnlySpan<byte> bytes)
        {
            var sb = new StringBuilder(bytes.Length * 3);
            for (int i = 0; i < bytes.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(bytes[i].ToString("x2"));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Build a multi-line diff between expected and actual byte sequences. Highlights
        /// the first differing byte index, the bit position within that byte (MSB=bit 7),
        /// and shows ±4 bytes of context.
        /// </summary>
        public static string Diff(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual)
        {
            var sb = new StringBuilder();
            sb.Append("expected (").Append(expected.Length).Append(" bytes): ").AppendLine(Format(expected));
            sb.Append("actual   (").Append(actual.Length).Append(" bytes): ").AppendLine(Format(actual));

            int min = Math.Min(expected.Length, actual.Length);
            int firstDiff = -1;
            for (int i = 0; i < min; i++)
                if (expected[i] != actual[i]) { firstDiff = i; break; }

            if (firstDiff < 0 && expected.Length != actual.Length)
            {
                sb.Append("length mismatch: expected ")
                  .Append(expected.Length).Append(", got ").Append(actual.Length).AppendLine();
                return sb.ToString();
            }
            if (firstDiff < 0) return sb.ToString();

            byte e = expected[firstDiff], a = actual[firstDiff];
            int bitInByte = -1;
            for (int b = 7; b >= 0; b--)
                if (((e >> b) & 1) != ((a >> b) & 1)) { bitInByte = 7 - b; break; }

            sb.Append("first byte diff at index ").Append(firstDiff)
              .Append(": expected 0x").Append(e.ToString("x2"))
              .Append(" (").Append(Convert.ToString(e, 2).PadLeft(8, '0')).Append("), ")
              .Append("got 0x").Append(a.ToString("x2"))
              .Append(" (").Append(Convert.ToString(a, 2).PadLeft(8, '0')).Append("); ")
              .Append("first differing bit is bit-position ").Append(bitInByte)
              .Append(" within the byte (MSB-first)").AppendLine();

            int ctxStart = Math.Max(0, firstDiff - 4);
            int ctxEndE = Math.Min(expected.Length, firstDiff + 5);
            int ctxEndA = Math.Min(actual.Length,   firstDiff + 5);
            sb.Append("  expected context: ").AppendLine(Format(expected[ctxStart..ctxEndE]));
            sb.Append("  actual   context: ").AppendLine(Format(actual[ctxStart..ctxEndA]));
            return sb.ToString();
        }
    }
}
