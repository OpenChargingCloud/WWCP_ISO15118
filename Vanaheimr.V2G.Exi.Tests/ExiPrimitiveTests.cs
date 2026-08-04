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

using NUnit.Framework;
using cloud.charging.open.protocols.ISO15118.EXI;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{
    /// <summary>
    /// Tests for the schema-less EXI primitive codecs. These exercise corner cases the
    /// AppProtocol vectors don't reach (large integers, multi-byte continuation, runes
    /// outside the BMP, n-bit boundary at 0/max).
    /// </summary>
    [TestFixture]
    public class ExiPrimitiveTests
    {
        // Note: NUnit doesn't allow [TestCase] arguments to be byte[], but we can pass
        // them via a strongly-typed source method.
        public static IEnumerable<TestCaseData> UnsignedIntegerVectors()
        {
            yield return new TestCaseData(0UL,            new byte[] { 0x00 })             .SetName("UInt 0 → [0x00]");
            yield return new TestCaseData(1UL,            new byte[] { 0x01 })             .SetName("UInt 1 → [0x01]");
            yield return new TestCaseData(127UL,          new byte[] { 0x7F })             .SetName("UInt 127 → [0x7F] (max single byte)");
            yield return new TestCaseData(128UL,          new byte[] { 0x80, 0x01 })       .SetName("UInt 128 → [0x80,0x01] (continuation flag)");
            yield return new TestCaseData(255UL,          new byte[] { 0xFF, 0x01 })       .SetName("UInt 255 → [0xFF,0x01]");
            yield return new TestCaseData(16383UL,        new byte[] { 0xFF, 0x7F })       .SetName("UInt 16383 → [0xFF,0x7F]");
            yield return new TestCaseData(16384UL,        new byte[] { 0x80, 0x80, 0x01 }) .SetName("UInt 16384 → [0x80,0x80,0x01]");
        }

        [TestCaseSource(nameof(UnsignedIntegerVectors))]
        public void UnsignedInteger_Encode_KnownValues(ulong value, byte[] expected)
        {
            var actual = EncodeUInt(value);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(0UL)]
        [TestCase(1UL)]
        [TestCase(127UL)]
        [TestCase(128UL)]
        [TestCase(0xFFFFUL)]
        [TestCase(0xFFFF_FFFFUL)]
        [TestCase(ulong.MaxValue)]
        public void UnsignedInteger_Roundtrip(ulong value)
        {
            var encoded = EncodeUInt(value);
            Assert.That(DecodeUInt(encoded), Is.EqualTo(value));
        }

        [TestCase("")]
        [TestCase("a")]
        [TestCase("urn:iso:15118:2:2013:MsgDef")]
        [TestCase("urn:iso:std:iso:15118:-20:CommonMessages")]
        [TestCase("Grüße aus Jena")]                        // non-ASCII BMP
        [TestCase("emoji: \U0001F50C \U0001F50B")]          // 🔌 🔋, supplementary plane
        public void StringValue_Roundtrip(string s)
        {
            var encoded = EncodeString(s);
            Assert.That(DecodeString(encoded), Is.EqualTo(s));
        }

        [TestCase(1,  0u)]
        [TestCase(1,  1u)]
        [TestCase(2,  3u)]
        [TestCase(5, 19u)]    // Priority - 1 = 19, max in [1..20]
        [TestCase(8,  0u)]
        [TestCase(8, 255u)]
        [TestCase(16, 0xFFFFu)]
        public void NBitUnsigned_Roundtrip(int n, uint value)
        {
            Span<byte> buf = stackalloc byte[8];
            var w = new BitWriter(buf);
            w.WriteBits(value, n);
            w.AlignToByte();

            var r = new BitReader(buf[..w.BytesWritten]);
            Assert.That(r.ReadBits(n), Is.EqualTo(value));
        }

        [Test]
        public void BitWriter_PacksMsbFirst()
        {
            // Writing 1, 0, 0, 0, 0, 0, 0, 1 must produce 0b1000_0001 = 0x81.
            Span<byte> buf = stackalloc byte[1];
            var w = new BitWriter(buf);
            w.WriteBit(true);
            for (int i = 0; i < 6; i++) w.WriteBit(false);
            w.WriteBit(true);
            Assert.That(buf[0], Is.EqualTo(0x81));
        }

        [Test]
        public void BitWriter_OverwritesDirtyBuffer()
        {
            // WriteBit must overwrite each target bit (clear 0s, set 1s), not only OR-in 1s — otherwise a
            // reused/non-zeroed destination keeps stale 1-bits and corrupts the output. Regression for the
            // buffer-reuse corruption found by the charging simulation (a 0x81 pattern on a 0xFF buffer
            // would read back as 0xFF without the clear).
            Span<byte> buf = stackalloc byte[1];
            buf[0] = 0xFF;
            var w = new BitWriter(buf);
            w.WriteBit(true);
            for (int i = 0; i < 6; i++) w.WriteBit(false);
            w.WriteBit(true);
            Assert.That(buf[0], Is.EqualTo(0x81), "0-bits must be cleared, not left set from prior buffer content");

            // And a full primitive encode into a dirty buffer must match the clean-buffer encoding.
            Span<byte> clean = stackalloc byte[16];
            Span<byte> dirty = stackalloc byte[16];
            dirty.Fill(0xFF);
            var wc = new BitWriter(clean);
            var wd = new BitWriter(dirty);
            ExiPrimitives.WriteUnsignedInteger(ref wc, 16384UL);
            ExiPrimitives.WriteUnsignedInteger(ref wd, 16384UL);
            wc.AlignToByte();
            wd.AlignToByte();
            Assert.That(dirty[..wd.BytesWritten].ToArray(), Is.EqualTo(clean[..wc.BytesWritten].ToArray()));
        }

        // ---- helpers ----------------------------------------------------------
        // BitWriter / BitReader are ref structs (cannot live across yields or be
        // returned as fields). These small helpers keep them stack-local while
        // returning plain byte[] / values to the test.

        private static byte[] EncodeUInt(ulong value)
        {
            Span<byte> buf = stackalloc byte[16];
            var w = new BitWriter(buf);
            ExiPrimitives.WriteUnsignedInteger(ref w, value);
            w.AlignToByte();
            return buf[..w.BytesWritten].ToArray();
        }

        private static ulong DecodeUInt(byte[] bytes)
        {
            var r = new BitReader(bytes);
            return ExiPrimitives.ReadUnsignedInteger(ref r);
        }

        private static byte[] EncodeString(string s)
        {
            Span<byte> buf = stackalloc byte[512];
            var w = new BitWriter(buf);
            ExiPrimitives.WriteStringValue(ref w, s);
            w.AlignToByte();
            return buf[..w.BytesWritten].ToArray();
        }

        private static string DecodeString(byte[] bytes)
        {
            var r = new BitReader(bytes);
            return ExiPrimitives.ReadStringValue(ref r, "value");
        }
    }
}
