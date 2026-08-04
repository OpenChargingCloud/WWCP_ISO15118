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

using System.Text;

namespace cloud.charging.open.protocols.ISO15118.EXI
{
    /// <summary>
    /// Full EXI string value-table codec (EXI Format 1.0 §7.1.10 / §7.3.3): local value
    /// partitions (one per value slot, keyed by an <see cref="int"/> the grammar layer
    /// assigns per QName) plus a single global partition per stream.
    ///
    /// <para><b>Why this is separate from <see cref="ExiPrimitives"/>.</b> The ISO 15118
    /// reference codec (cbexigen/cbV2G) is miss-only: it never emits value-table hits and its
    /// decoder rejects them. Our ISO 15118 <i>encode</i> path therefore uses the miss-only
    /// <see cref="ExiPrimitives.WriteStringValue"/>. This class exists so we can (a) decode
    /// streams from stacks that <i>do</i> emit hits (EXIficient/Josev), and (b) round-trip the
    /// full value-table behaviour in tests. A single instance carries the partition state for
    /// one stream and is threaded alongside the <see cref="BitWriter"/> / <see cref="BitReader"/>.</para>
    ///
    /// <para><b>Encoding a value at a given local key:</b></para>
    /// <list type="bullet">
    ///   <item>value present in the local partition → <c>UnsignedInteger(0)</c> then the
    ///         compact id as an n-bit Unsigned Integer, n = ⌈log₂(m)⌉, m = local partition size;</item>
    ///   <item>else present in the global partition → <c>UnsignedInteger(1)</c> then the compact
    ///         id, n = ⌈log₂(g)⌉, g = global partition size;</item>
    ///   <item>else (miss) → <c>UnsignedInteger(length + 2)</c> then one codepoint per rune, and
    ///         the value is appended to <b>both</b> the local and the global partition.</item>
    /// </list>
    /// A partition of size 1 needs a 0-bit compact id. Hits never grow a partition; only misses
    /// do, which keeps encoder and decoder partitions in lock-step.
    /// </summary>
    public sealed class ExiStringTable
    {
        private readonly List<string> _global = new();
        private readonly Dictionary<string, int> _globalIndex = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Partition> _locals = new(StringComparer.Ordinal);

        private sealed class Partition
        {
            public readonly List<string> Values = new();
            public readonly Dictionary<string, int> Index = new(StringComparer.Ordinal);
        }

        private Partition Local(string key)
        {
            if (!_locals.TryGetValue(key, out var p))
            {
                p = new Partition();
                _locals[key] = p;
            }
            return p;
        }

        /// <summary>Encode a string value at <paramref name="localKey"/>, emitting a hit when possible.</summary>
        public void WriteStringValue(ref BitWriter w, string localKey, string value)
        {
            var local = Local(localKey);

            if (local.Index.TryGetValue(value, out int localId))
            {
                ExiPrimitives.WriteUnsignedInteger(ref w, 0);
                WriteCompactId(ref w, localId, local.Values.Count);
                return;
            }

            if (_globalIndex.TryGetValue(value, out int globalId))
            {
                ExiPrimitives.WriteUnsignedInteger(ref w, 1);
                WriteCompactId(ref w, globalId, _global.Count);
                return;
            }

            // Miss: length+2 prefix, then codepoints. Then grow both partitions.
            int runeCount = 0;
            foreach (var _ in value.EnumerateRunes()) runeCount++;
            ExiPrimitives.WriteUnsignedInteger(ref w, (ulong)(runeCount + 2));
            foreach (var rune in value.EnumerateRunes())
                ExiPrimitives.WriteUnsignedInteger(ref w, (ulong)rune.Value);

            Add(local, value);
        }

        /// <summary>Decode a string value at <paramref name="localKey"/>, resolving hits against the partitions.</summary>
        public string ReadStringValue(ref BitReader r, string localKey)
        {
            var local = Local(localKey);
            ulong head = ExiPrimitives.ReadUnsignedInteger(ref r);

            if (head == 0) // local hit
            {
                int id = (int)ReadCompactId(ref r, local.Values.Count);
                if (id >= local.Values.Count)
                    throw new InvalidDataException(
                        $"Local value-table hit id {id} out of range (partition size {local.Values.Count}).");
                return local.Values[id];
            }

            if (head == 1) // global hit
            {
                int id = (int)ReadCompactId(ref r, _global.Count);
                if (id >= _global.Count)
                    throw new InvalidDataException(
                        $"Global value-table hit id {id} out of range (partition size {_global.Count}).");
                return _global[id];
            }

            int len = checked((int)(head - 2));
            var sb = new StringBuilder(len);
            for (int i = 0; i < len; i++)
            {
                int cp = checked((int)ExiPrimitives.ReadUnsignedInteger(ref r));
                sb.Append(char.ConvertFromUtf32(cp));
            }
            var value = sb.ToString();
            Add(local, value);
            return value;
        }

        /// <summary>Append a freshly-seen (miss) value to the local and global partitions.</summary>
        private void Add(Partition local, string value)
        {
            local.Index[value] = local.Values.Count;
            local.Values.Add(value);

            // A miss means the value was in neither partition, so it is new globally too.
            _globalIndex[value] = _global.Count;
            _global.Add(value);
        }

        private static void WriteCompactId(ref BitWriter w, int id, int partitionSize)
        {
            int n = BitsFor(partitionSize);
            if (n > 0) w.WriteBits((uint)id, n);
        }

        private static uint ReadCompactId(ref BitReader r, int partitionSize)
        {
            int n = BitsFor(partitionSize);
            return n > 0 ? r.ReadBits(n) : 0u;
        }

        /// <summary>⌈log₂(count)⌉, with the EXI convention that a size-1 partition needs 0 bits.</summary>
        private static int BitsFor(int count)
        {
            if (count <= 1) return 0;
            int bits = 0, v = count - 1;
            while (v > 0) { bits++; v >>= 1; }
            return bits;
        }
    }
}
