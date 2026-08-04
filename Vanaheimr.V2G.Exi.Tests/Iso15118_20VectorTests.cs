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

using System.Text.Json;
using NUnit.Framework;
using cloud.charging.open.protocols.ISO15118.EXI.Tests.Infrastructure;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{
    /// <summary>
    /// Differential wire conformance for ISO 15118-20: the generated codec must produce exactly the
    /// bytes cbV2G produces for each target message, across all three Phase-4 message sets
    /// (CommonMessages, DC, AC). Each set has its own generated assembly/namespace, its own vector file,
    /// and its own <c>Iso15118_20&lt;Set&gt;Fixtures.TryEncode(name, buf, out n)</c> — kept separate so
    /// this test file never needs to reference the (colliding) generated types directly.
    /// </summary>
    [TestFixture]
    public class Iso15118_20VectorTests
    {
        public sealed record Vec(string Name, string ExpectedHex);

        private static IEnumerable<TestCaseData> Vectors(string fileName)
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Vectors", fileName);
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            foreach (var v in doc.RootElement.GetProperty("vectors").EnumerateArray())
            {
                var name = v.GetProperty("name").GetString()!;
                yield return new TestCaseData(new Vec(name, v.GetProperty("expectedHex").GetString()!)).SetName(name);
            }
        }

        private static IEnumerable<TestCaseData> CommonVectors() => Vectors("Iso15118_20.CommonMessages.vectors.json");
        private static IEnumerable<TestCaseData> DcVectors() => Vectors("Iso15118_20.DC.vectors.json");
        private static IEnumerable<TestCaseData> AcVectors() => Vectors("Iso15118_20.AC.vectors.json");
        private static IEnumerable<TestCaseData> WptVectors() => Vectors("Iso15118_20.WPT.vectors.json");
        private static IEnumerable<TestCaseData> AcdpVectors() => Vectors("Iso15118_20.ACDP.vectors.json");
        private static IEnumerable<TestCaseData> AcDerIecVectors() => Vectors("Iso15118_20.AC_DER_IEC.vectors.json");
        private static IEnumerable<TestCaseData> AcDerSaeVectors() => Vectors("Iso15118_20.AC_DER_SAE.vectors.json");

        [TestCaseSource(nameof(CommonVectors))]
        public void CommonMessages_Matches_CbV2G(Vec vector) =>
            AssertMatches(vector, Iso15118_20CommonFixtures.TryEncode);

        [TestCaseSource(nameof(DcVectors))]
        public void DC_Matches_CbV2G(Vec vector) =>
            AssertMatches(vector, Iso15118_20DcFixtures.TryEncode);

        [TestCaseSource(nameof(AcVectors))]
        public void AC_Matches_CbV2G(Vec vector) =>
            AssertMatches(vector, Iso15118_20AcFixtures.TryEncode);

        [TestCaseSource(nameof(WptVectors))]
        public void WPT_Matches_CbV2G(Vec vector) =>
            AssertMatches(vector, Iso15118_20WptFixtures.TryEncode);

        [TestCaseSource(nameof(AcdpVectors))]
        public void ACDP_Matches_CbV2G(Vec vector) =>
            AssertMatches(vector, Iso15118_20AcdpFixtures.TryEncode);

        // AC DER: a mixed corpus. Six of the ten plain-AC vectors carry cbV2G bytes, because the DER
        // grammar was measured to encode those messages identically; the rest — the four whose
        // members shift event code, and every message using a DER member — are this project's own
        // output, since cbexigen does not generate the amendment schemas. Which is which is in each
        // vector's `source`, and AcDerCorpusTests asserts the split has not quietly moved.

        [TestCaseSource(nameof(AcDerIecVectors))]
        public void AcDerIec_Matches_Corpus(Vec vector) =>
            AssertMatches(vector, Iso15118_20AcDerIecFixtures.TryEncode);

        [TestCaseSource(nameof(AcDerSaeVectors))]
        public void AcDerSae_Matches_Corpus(Vec vector) =>
            AssertMatches(vector, Iso15118_20AcDerSaeFixtures.TryEncode);

        [TestCaseSource(nameof(CommonVectors))]
        public void CommonMessages_RoundtripsThroughDecode(Vec vector) =>
            AssertRoundtrip(vector, Iso15118_20CommonFixtures.TryEncode, Iso15118_20CommonFixtures.DecodeReEncode);

        [TestCaseSource(nameof(DcVectors))]
        public void DC_RoundtripsThroughDecode(Vec vector) =>
            AssertRoundtrip(vector, Iso15118_20DcFixtures.TryEncode, Iso15118_20DcFixtures.DecodeReEncode);

        [TestCaseSource(nameof(AcVectors))]
        public void AC_RoundtripsThroughDecode(Vec vector) =>
            AssertRoundtrip(vector, Iso15118_20AcFixtures.TryEncode, Iso15118_20AcFixtures.DecodeReEncode);

        [TestCaseSource(nameof(WptVectors))]
        public void WPT_RoundtripsThroughDecode(Vec vector) =>
            AssertRoundtrip(vector, Iso15118_20WptFixtures.TryEncode, Iso15118_20WptFixtures.DecodeReEncode);

        [TestCaseSource(nameof(AcdpVectors))]
        public void ACDP_RoundtripsThroughDecode(Vec vector) =>
            AssertRoundtrip(vector, Iso15118_20AcdpFixtures.TryEncode, Iso15118_20AcdpFixtures.DecodeReEncode);

        [TestCaseSource(nameof(AcDerIecVectors))]
        public void AcDerIec_RoundtripsThroughDecode(Vec vector) =>
            AssertRoundtrip(vector, Iso15118_20AcDerIecFixtures.TryEncode, Iso15118_20AcDerIecFixtures.DecodeReEncode);

        [TestCaseSource(nameof(AcDerSaeVectors))]
        public void AcDerSae_RoundtripsThroughDecode(Vec vector) =>
            AssertRoundtrip(vector, Iso15118_20AcDerSaeFixtures.TryEncode, Iso15118_20AcDerSaeFixtures.DecodeReEncode);

        private delegate bool TryEncodeFn(string vectorName, byte[] dest, out int bytesWritten);
        private delegate byte[] DecodeReEncodeFn(byte[] wireBytes);

        private static void AssertMatches(Vec vector, TryEncodeFn tryEncode)
        {
            var buf = new byte[512];
            Assert.That(tryEncode(vector.Name, buf, out int n), Is.True, "encode failed");

            var actual = buf.AsSpan(0, n).ToArray();
            var expected = ParseHex(vector.ExpectedHex);

            if (!actual.AsSpan().SequenceEqual(expected))
                Assert.Fail($"{vector.Name}: generated bytes diverge from cbV2G.\n" +
                            $"  expected ({expected.Length}): {ToHex(expected)}\n" +
                            $"  actual   ({actual.Length}): {ToHex(actual)}");
        }

        /// <summary>
        /// Every cbV2G-verified message must also survive an encode → decode → re-encode cycle
        /// byte-for-byte through the generated <c>DecodeAny</c> dispatcher, exercising the decode path
        /// (event-code dispatch, choice/substitution resolution) that <see cref="AssertMatches"/> never
        /// touches.
        /// </summary>
        private static void AssertRoundtrip(Vec vector, TryEncodeFn tryEncode, DecodeReEncodeFn decodeReEncode)
        {
            var buf = new byte[512];
            Assert.That(tryEncode(vector.Name, buf, out int n), Is.True, "encode failed");
            var original = buf.AsSpan(0, n).ToArray();

            var reEncoded = decodeReEncode(original);

            Assert.That(reEncoded, Is.EqualTo(original),
                $"{vector.Name}: decode∘encode is not the identity on the wire");
        }

        private static byte[] ParseHex(string hex)
        {
            var parts = hex.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var bytes = new byte[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                bytes[i] = Convert.ToByte(parts[i], 16);
            return bytes;
        }

        private static string ToHex(byte[] b) => string.Join(' ', b.Select(x => x.ToString("x2")));
    }
}
