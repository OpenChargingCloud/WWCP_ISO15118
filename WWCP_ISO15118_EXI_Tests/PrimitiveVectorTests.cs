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

using System.Text.Json;
using NUnit.Framework;
using cloud.charging.open.protocols.ISO15118.EXI;
using cloud.charging.open.protocols.ISO15118.EXI.Tests.Infrastructure;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{
    /// <summary>
    /// Vector-driven tests for the schema-less EXI datatypes, loaded from
    /// <c>Vectors/Primitives.vectors.json</c>.
    /// <para>
    /// <b>Provenance.</b> These <c>expectedHex</c> values are produced by the codec under test
    /// <i>and</i> independently reproduced, byte-for-byte, by EXIficient's
    /// <c>BitEncoderChannel</c> (EXI 1.0 §7.1) — so they are no longer self-referential (18/18 as
    /// of 2026-07-25). That cross-check needs a JRE and therefore lives outside <c>dotnet test</c>:
    /// re-run <c>python tools/exificient-ref/primitives.py</c> after touching the primitive layer.
    /// </para>
    /// </summary>
    [TestFixture]
    public class PrimitiveVectorTests
    {
        public static IEnumerable<TestCaseData> All()
        {
            var dir  = Path.Combine(TestContext.CurrentContext.TestDirectory, "Vectors");
            var file = Path.Combine(dir, "Primitives.vectors.json");
            if (!File.Exists(file)) yield break;

            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            foreach (var v in doc.RootElement.GetProperty("vectors").EnumerateArray())
            {
                var name = v.GetProperty("name").GetString()!;
                yield return new TestCaseData(v.Clone()).SetName($"{{m}}({name})");
            }
        }

        [TestCaseSource(nameof(All))]
        public void Encode_Matches_Expected(JsonElement v)
        {
            var datatype = v.GetProperty("datatype").GetString()!;
            var expected = HexUtil.Parse(v.GetProperty("expectedHex").GetString()!);

            var buf = new byte[512];
            var w = new BitWriter(buf);

            switch (datatype)
            {
                case "unsignedInteger":
                    ExiPrimitives.WriteUnsignedInteger(ref w, ulong.Parse(v.GetProperty("value").GetString()!));
                    break;
                case "signedInteger":
                    ExiPrimitives.WriteSignedInteger(ref w, long.Parse(v.GetProperty("value").GetString()!));
                    break;
                case "binary":
                    ExiPrimitives.WriteBinary(ref w, HexUtil.Parse(v.GetProperty("valueHex").GetString()!));
                    break;
                case "boolean":
                    ExiPrimitives.WriteBoolean(ref w, bool.Parse(v.GetProperty("value").GetString()!));
                    break;
                case "string":
                    ExiPrimitives.WriteStringValue(ref w, v.GetProperty("value").GetString()!);
                    break;
                default:
                    throw new NotSupportedException($"Unknown primitive datatype '{datatype}'.");
            }

            w.AlignToByte();
            var actual = buf.AsSpan(0, w.BytesWritten).ToArray();

            if (!actual.AsSpan().SequenceEqual(expected))
                Assert.Fail($"{datatype}: encode mismatch\n{HexUtil.Diff(expected, actual)}");
        }
    }
}
