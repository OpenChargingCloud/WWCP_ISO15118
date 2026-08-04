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

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{
    /// <summary>
    /// Guards the provenance of the AC DER corpora.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These two files are the only vector corpora in the repository whose bytes are not all from a
    /// reference encoder, and that is a property worth defending. Six of the ten plain-AC vectors
    /// carry cbV2G bytes verbatim — legitimately, because the DER grammar was measured to encode
    /// those messages identically to the plain AC grammar. If a future schema or emitter change
    /// moved one of them, the vector test would fail; but if someone then "fixed" it by pasting in
    /// this project's own output, the corpus would quietly lose its only external anchor.
    /// </para>
    /// <para>
    /// So: the cbV2G-sourced vectors must still be byte-identical to the AC corpus they were taken
    /// from, and the count must not shrink.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class AcDerCorpusTests
    {
        private const string CbV2GSource = "cbV2G@03350be048b3 (iso_20/AC)";

        /// <summary>Measured, and fixed here so a change has to be deliberate.</summary>
        private static readonly string[] EncodedIdenticallyUnderTheDerGrammar =
        [
            "AC_ChargeParameterDiscoveryReq", "AC_ChargeParameterDiscoveryRes",
            "AC_ChargeLoopReq_BPTScheduled",  "AC_ChargeLoopRes_BPTScheduled",
            "AC_ChargeLoopReq_BPTDynamic",    "AC_ChargeLoopRes_BPTDynamic",
        ];

        /// <summary>
        /// The other four. <c>DER_</c> sorts before <c>Dynamic_</c> and <c>Scheduled_</c>, so adding
        /// the DER members pushes those two members' event codes along — which is exactly the
        /// backward-compatibility break the plain BPT members do not suffer.
        /// </summary>
        private static readonly string[] ShiftedByTheDerMembers =
        [
            "AC_ChargeLoopReq", "AC_ChargeLoopRes",
            "AC_ChargeLoopReq_Dynamic", "AC_ChargeLoopRes_Dynamic",
        ];

        private static Dictionary<string, (string Source, string Hex)> Load(string fileName)
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Vectors", fileName);
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.GetProperty("vectors").EnumerateArray().ToDictionary(
                v => v.GetProperty("name").GetString()!,
                v => (v.GetProperty("source").GetString()!, v.GetProperty("expectedHex").GetString()!));
        }

        private static Dictionary<string, string> AcCorpus()
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory,
                                    "Vectors", "Iso15118_20.AC.vectors.json");
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.GetProperty("vectors").EnumerateArray().ToDictionary(
                v => v.GetProperty("name").GetString()!,
                v => v.GetProperty("expectedHex").GetString()!);
        }

        [TestCase("Iso15118_20.AC_DER_IEC.vectors.json")]
        [TestCase("Iso15118_20.AC_DER_SAE.vectors.json")]
        public void TheCbV2GVectorsAreStillCbV2GsBytes(string fileName)
        {
            var der = Load(fileName);
            var ac  = AcCorpus();

            foreach (var name in EncodedIdenticallyUnderTheDerGrammar)
            {
                Assert.That(der[name].Source, Is.EqualTo(CbV2GSource),
                            $"{name} lost its cbV2G provenance — the corpus' only external anchor");
                Assert.That(der[name].Hex, Is.EqualTo(ac[name]),
                            $"{name} no longer carries the AC corpus' bytes verbatim");
            }
        }

        [TestCase("Iso15118_20.AC_DER_IEC.vectors.json")]
        [TestCase("Iso15118_20.AC_DER_SAE.vectors.json")]
        public void TheSelfGeneratedVectorsSaySo(string fileName)
        {
            var der = Load(fileName);

            foreach (var (name, (source, _)) in der)
            {
                var anchored = EncodedIdenticallyUnderTheDerGrammar.Contains(name);
                Assert.That(source == CbV2GSource, Is.EqualTo(anchored),
                            $"{name}: source says {source}, which is not what this vector is");

                if (!anchored)
                    Assert.That(der[name].Hex, Is.Not.Empty);
            }
        }

        [TestCase("Iso15118_20.AC_DER_IEC.vectors.json")]
        [TestCase("Iso15118_20.AC_DER_SAE.vectors.json")]
        public void TheShiftedVectorsReallyDoDifferFromPlainAc(string fileName)
        {
            var der = Load(fileName);
            var ac  = AcCorpus();

            // If one of these ever matched plain AC again, the classification above would be stale
            // and the vector should move back to cbV2G provenance — a strictly better position.
            foreach (var name in ShiftedByTheDerMembers)
                Assert.That(der[name].Hex, Is.Not.EqualTo(ac[name]),
                            $"{name} now encodes identically under both grammars — it can and should " +
                            "be re-anchored to the cbV2G corpus");
        }
    }
}
