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
    /// Scenario tests for the full EXI string value-table codec (<see cref="ExiStringTable"/>):
    /// local hits, global hits, interleaving, compact-id bit-width growth, and rejection of
    /// out-of-range hit indices. The ISO 15118 wire path itself is miss-only (see
    /// <see cref="ExiPrimitives"/>); this class exercises the machinery used to decode streams
    /// from stacks that do emit hits.
    /// </summary>
    [TestFixture]
    public class ExiStringTableTests
    {
        /// <summary>
        /// The cross-language contract. The Kotlin port of this class asserts the SAME hex for the
        /// SAME sequence, so the two implementations are pinned to each other rather than each to
        /// its own idea of the format. A round trip inside one language cannot catch a shared
        /// misreading of the spec; this can.
        /// </summary>
        [Test]
        public void MixedHitsAndMisses_MatchTheCrossLanguageVector()
        {
            var bytes = Encode(new ExiStringTable(),
                ("1", "alpha"), ("2", "beta"), ("1", "alpha"), ("2", "alpha"),
                ("1", "gamma"), ("1", "gamma"), ("2", "beta"));

            Assert.That(Convert.ToHexString(bytes), Is.EqualTo("07616C7068610662657461000103B3B0B6B6B0804000"),
                        "the Kotlin ExiStringTable pins this same value — if one moves, both must");
        }

        [Test]
        public void LocalHit_Roundtrip_And_IsShorterThanTwoMisses()
        {
            var enc = new ExiStringTable();
            var twoMisses = Encode(new ExiStringTable(), ("1", "urn:a"), ("1", "urn:b"));
            var missThenHit = Encode(enc, ("1", "urn:a"), ("1", "urn:a"));

            // The second "urn:a" is a local hit: far shorter than encoding a second distinct value.
            Assert.That(missThenHit.Length, Is.LessThan(twoMisses.Length));

            var got = Decode(new ExiStringTable(), missThenHit, "1", "1");
            Assert.That(got, Is.EqualTo(new[] { "urn:a", "urn:a" }));
        }

        [Test]
        public void GlobalHit_Roundtrip_AcrossDifferentKeys()
        {
            // "urn:x" first seen at key 1 (miss), then at key 2 → not in key-2's local partition
            // but present globally → a global hit.
            var enc = new ExiStringTable();
            var bytes = Encode(enc, ("1", "urn:x"), ("2", "urn:x"));

            var got = Decode(new ExiStringTable(), bytes, "1", "2");
            Assert.That(got, Is.EqualTo(new[] { "urn:x", "urn:x" }));
        }

        [Test]
        public void Interleaved_HitsAndMisses_Roundtrip()
        {
            var items = new (string, string)[]
            {
                ("1", "alpha"),   // miss (local1=[alpha], global=[alpha])
                ("2", "beta"),    // miss (local2=[beta],  global=[alpha,beta])
                ("1", "alpha"),   // local hit
                ("2", "alpha"),   // global hit (not in local2)
                ("1", "gamma"),   // miss
                ("1", "gamma"),   // local hit
                ("2", "beta"),    // local hit
            };

            var bytes = Encode(new ExiStringTable(), items);
            var keys = Array.ConvertAll(items, i => i.Item1);
            var expected = Array.ConvertAll(items, i => i.Item2);

            Assert.That(Decode(new ExiStringTable(), bytes, keys), Is.EqualTo(expected));
        }

        [TestCase(1, 0)]   // size-1 partition → 0-bit compact id
        [TestCase(2, 1)]   // size-2           → 1-bit
        [TestCase(3, 2)]   // size-3           → 2-bit
        [TestCase(4, 2)]   // size-4           → 2-bit
        [TestCase(5, 3)]   // size-5           → 3-bit
        public void CompactId_BitWidth_GrowsWithPartitionSize(int partitionSize, int expectedCompactBits)
        {
            var table = new ExiStringTable();
            Span<byte> buf = stackalloc byte[4096];
            var w = new BitWriter(buf);

            // Prime the local partition of key 7 with `partitionSize` distinct misses.
            for (int i = 0; i < partitionSize; i++)
                table.WriteStringValue(ref w, "7", "v" + i);

            int bitsBefore = w.BitsWritten;
            table.WriteStringValue(ref w, "7", "v0");   // local hit on the first entry
            int hitBits = w.BitsWritten - bitsBefore;

            // A hit is UnsignedInteger(0) — one octet — followed by the compact id.
            Assert.That(hitBits - 8, Is.EqualTo(expectedCompactBits));
        }

        [Test]
        public void Decode_LocalHit_OnEmptyPartition_Throws()
        {
            // 0x00 = UnsignedInteger(0) = "local hit"; the empty partition has 0-bit ids, so id 0
            // is read and found out of range. (The BitReader is created inside the delegate:
            // a ref struct cannot be captured by a lambda.)
            Assert.Throws<InvalidDataException>(() =>
            {
                var table = new ExiStringTable();
                var r = new BitReader(new byte[] { 0x00 });
                table.ReadStringValue(ref r, "0");
            });
        }

        [Test]
        public void Decode_GlobalHit_OnEmptyPartition_Throws()
        {
            // 0x01 = UnsignedInteger(1) = "global hit" with an empty global partition.
            Assert.Throws<InvalidDataException>(() =>
            {
                var table = new ExiStringTable();
                var r = new BitReader(new byte[] { 0x01 });
                table.ReadStringValue(ref r, "0");
            });
        }

        [Test]
        public void TableMiss_ByteIdentical_To_PrimitiveMiss()
        {
            // A first-occurrence value through the table is a miss and must match the miss-only
            // primitive exactly — this is why migrating a repeat-free codec to the table changes
            // no bytes.
            const string s = "urn:iso:15118:2:2013:MsgDef";

            Span<byte> a = stackalloc byte[128];
            var wa = new BitWriter(a);
            new ExiStringTable().WriteStringValue(ref wa, "0", s);
            wa.AlignToByte();

            Span<byte> b = stackalloc byte[128];
            var wb = new BitWriter(b);
            ExiPrimitives.WriteStringValue(ref wb, s);
            wb.AlignToByte();

            Assert.That(a[..wa.BytesWritten].ToArray(), Is.EqualTo(b[..wb.BytesWritten].ToArray()));
        }

        // ---- helpers -----------------------------------------------------------

        private static byte[] Encode(ExiStringTable table, params (string key, string val)[] items)
        {
            Span<byte> buf = stackalloc byte[8192];
            var w = new BitWriter(buf);
            foreach (var (k, v) in items)
                table.WriteStringValue(ref w, k, v);
            w.AlignToByte();
            return buf[..w.BytesWritten].ToArray();
        }

        private static List<string> Decode(ExiStringTable table, byte[] bytes, params string[] keys)
        {
            var r = new BitReader(bytes);
            var result = new List<string>(keys.Length);
            foreach (var k in keys)
                result.Add(table.ReadStringValue(ref r, k));
            return result;
        }
    }
}
