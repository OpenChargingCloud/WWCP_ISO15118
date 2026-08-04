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

using NUnit.Framework;
using cloud.charging.open.protocols.ISO15118.EXI;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{
    /// <summary>
    /// Hand-computed bit-layout vectors and round-trips for the EXI datatypes added in
    /// Phase 1: Signed Integer (§7.1.5), Binary (§7.1.1) and Boolean (§7.1.2).
    /// <para>
    /// Signed values are bit-packed as [sign bit][magnitude as Unsigned Integer], so the
    /// magnitude's byte falls across a 1-bit boundary — the expected bytes below are derived
    /// by hand from that layout (MSB-first, zero-padded to the next byte).
    /// </para>
    /// </summary>
    [TestFixture]
    public class ExiDatatypeTests
    {
        // ---- Signed Integer ----------------------------------------------------

        public static IEnumerable<TestCaseData> SignedVectors()
        {
            //                         sign | UnsignedInteger(magnitude)         → bytes
            yield return new TestCaseData(0L,   new byte[] { 0x00, 0x00 }).SetName("Int 0    → 0|UInt(0)");
            yield return new TestCaseData(1L,   new byte[] { 0x00, 0x80 }).SetName("Int 1    → 0|UInt(1)");
            yield return new TestCaseData(-1L,  new byte[] { 0x80, 0x00 }).SetName("Int -1   → 1|UInt(0)");
            yield return new TestCaseData(127L, new byte[] { 0x3F, 0x80 }).SetName("Int 127  → 0|UInt(127)");
            yield return new TestCaseData(-128L,new byte[] { 0xBF, 0x80 }).SetName("Int -128 → 1|UInt(127)");
            // 128 needs a two-byte Unsigned Integer magnitude (0x80,0x01).
            yield return new TestCaseData(128L, new byte[] { 0x40, 0x00, 0x80 }).SetName("Int 128  → 0|UInt(128)");
        }

        [TestCaseSource(nameof(SignedVectors))]
        public void SignedInteger_Encode_KnownValues(long value, byte[] expected)
        {
            Span<byte> buf = stackalloc byte[16];
            var w = new BitWriter(buf);
            ExiPrimitives.WriteSignedInteger(ref w, value);
            w.AlignToByte();
            Assert.That(buf[..w.BytesWritten].ToArray(), Is.EqualTo(expected));
        }

        [TestCase(0L)]
        [TestCase(1L)]
        [TestCase(-1L)]
        [TestCase(127L)]
        [TestCase(128L)]
        [TestCase(-128L)]
        [TestCase(long.MaxValue)]
        [TestCase(long.MinValue)]
        public void SignedInteger_Roundtrip(long value)
        {
            Span<byte> buf = stackalloc byte[16];
            var w = new BitWriter(buf);
            ExiPrimitives.WriteSignedInteger(ref w, value);
            w.AlignToByte();

            var r = new BitReader(buf[..w.BytesWritten]);
            Assert.That(ExiPrimitives.ReadSignedInteger(ref r), Is.EqualTo(value));
        }

        // ---- Binary ------------------------------------------------------------

        public static IEnumerable<TestCaseData> BinaryVectors()
        {
            yield return new TestCaseData(Array.Empty<byte>(),          new byte[] { 0x00 })
                .SetName("Binary []       → UInt(0)");
            yield return new TestCaseData(new byte[] { 0xAB },          new byte[] { 0x01, 0xAB })
                .SetName("Binary [AB]     → UInt(1),AB");
            yield return new TestCaseData(new byte[] { 0x01, 0x02, 0x03 }, new byte[] { 0x03, 0x01, 0x02, 0x03 })
                .SetName("Binary [010203] → UInt(3),010203");
        }

        [TestCaseSource(nameof(BinaryVectors))]
        public void Binary_Encode_KnownValues(byte[] data, byte[] expected)
        {
            Span<byte> buf = stackalloc byte[64];
            var w = new BitWriter(buf);
            ExiPrimitives.WriteBinary(ref w, data);
            w.AlignToByte();
            Assert.That(buf[..w.BytesWritten].ToArray(), Is.EqualTo(expected));
        }

        [Test]
        public void Binary_Roundtrip_MultiByteLength()
        {
            // 200 bytes forces a two-byte Unsigned Integer length prefix (200 = 0xC8,0x01).
            var data = new byte[200];
            for (int i = 0; i < data.Length; i++) data[i] = (byte)(i * 7);

            var buf = new byte[512];
            var w = new BitWriter(buf);
            ExiPrimitives.WriteBinary(ref w, data);
            w.AlignToByte();

            var r = new BitReader(buf.AsSpan(0, w.BytesWritten));
            Assert.That(ExiPrimitives.ReadBinary(ref r), Is.EqualTo(data));
        }

        // ---- Boolean -----------------------------------------------------------

        [TestCase(true,  0x80)]
        [TestCase(false, 0x00)]
        public void Boolean_Encode_KnownValues(bool value, byte expectedByte)
        {
            Span<byte> buf = stackalloc byte[1];
            var w = new BitWriter(buf);
            ExiPrimitives.WriteBoolean(ref w, value);
            w.AlignToByte();
            Assert.That(buf[0], Is.EqualTo(expectedByte));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Boolean_Roundtrip(bool value)
        {
            Span<byte> buf = stackalloc byte[1];
            var w = new BitWriter(buf);
            ExiPrimitives.WriteBoolean(ref w, value);
            w.AlignToByte();

            var r = new BitReader(buf);
            Assert.That(ExiPrimitives.ReadBoolean(ref r), Is.EqualTo(value));
        }
    }
}
