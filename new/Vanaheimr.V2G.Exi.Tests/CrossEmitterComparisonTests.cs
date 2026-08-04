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

using cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Emit;
using cloud.charging.open.protocols.ISO15118.EXI.Tests.Infrastructure;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{
    /// <summary>
    /// Gate 2 of the three in <c>kotlin/README.md</c>: two back ends, one <c>SchemaPlan</c>, and
    /// every generated function performing the same wire operations in the same order.
    /// </summary>
    /// <remarks>
    /// The vectors pin the encoder against cbV2G, and each decoder is then checked by round-tripping
    /// through its own encoder — so a bug mirrored in both directions passes both. Independent
    /// emitters agreeing is what catches it. Until now this comparison existed only as prose in
    /// <c>kotlin/README.md</c>; nothing in the repository ran it.
    /// </remarks>
    [TestFixture]
    public class CrossEmitterComparisonTests
    {
        private static (string, string)[] AppProtocolSchema() =>
            EmitterHarness.RealSchemaSet("Vanaheimr.V2G.Exi.Prototype")
                          .Where(f => f.Name.Contains("AppProtocol"))
                          .Select(f => (f.Name, f.Xsd))
                          .ToArray();

        [Test]
        public void SwiftAndCSharpAgreeOperationForOperation()
        {
            var schema = AppProtocolSchema();

            var swift  = CrossEmitterComparison.Operations(
                             EmitterHarness.EmitSwift("app", "SupportedAppProtocolCodec", schema));
            var csharp = CrossEmitterComparison.Operations(
                             EmitterHarness.EmitCSharp("Test.App", "SupportedAppProtocolCodec", schema));

            Assert.That(swift, Is.Not.Empty, "no functions parsed out of the Swift output");

            var problems = CrossEmitterComparison.Diff(swift, "swift", csharp, "csharp");
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void KotlinAndCSharpAgreeOperationForOperation()
        {
            // Kotlin is compared too, not for Swift's sake but because this gate has never actually
            // run: the claim that the two agree was carried by the documentation alone.
            var schema = AppProtocolSchema();

            var kotlin = CrossEmitterComparison.Operations(
                             EmitterHarness.Emit("app", "SupportedAppProtocolCodec", schema));
            var csharp = CrossEmitterComparison.Operations(
                             EmitterHarness.EmitCSharp("Test.App", "SupportedAppProtocolCodec", schema));

            var problems = CrossEmitterComparison.Diff(kotlin, "kotlin", csharp, "csharp");
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        /// <summary>
        /// The whole ISO 15118-2 set — 94 types, 111 generated files. This is the first schema set
        /// large enough for the comparison to be a real gate rather than a demonstration: every
        /// construct the back end models appears here, in combinations no mini-XSD covers.
        /// </summary>
        [Test]
        public void SwiftAndCSharpAgreeAcrossTheWholeIso2Set()
        {
            var schema = EmitterHarness.RealSchemaSet("Vanaheimr.V2G.Exi.Iso15118_2")
                                       .Select(f => (f.Name, f.Xsd))
                                       .ToArray();

            AssertAgrees(EmitterHarness.EmitSwift("iso2", "Iso15118_2Codec", schema), "swift",
                         EmitterHarness.EmitCSharp("Iso2", "Iso15118_2Codec", schema));
        }

        /// <summary>
        /// The same set through the Kotlin back end. Kotlin shipped first and was the back end this
        /// gate was *written about*, but for a long while it was the one the gate did not cover:
        /// when the comparison was finally built, only AppProtocol ran through it, and the whole-set
        /// runs were added for Swift. So the older back end was the less-checked one — exactly
        /// backwards from what its age suggests.
        /// </summary>
        [Test]
        public void KotlinAndCSharpAgreeAcrossTheWholeIso2Set()
        {
            var schema = EmitterHarness.RealSchemaSet("Vanaheimr.V2G.Exi.Iso15118_2")
                                       .Select(f => (f.Name, f.Xsd))
                                       .ToArray();

            AssertAgrees(EmitterHarness.Emit("iso2", "Iso15118_2Codec", schema), "kotlin",
                         EmitterHarness.EmitCSharp("Iso2", "Iso15118_2Codec", schema));
        }

        /// <summary>
        /// Every ISO 15118-20 set the back end generates. CommonMessages brings inline choices,
        /// which -2 has none of; the others are separate grammars that happen to share CommonTypes.
        /// WPT is absent on purpose — it is refused, and for a reason that is not going away
        /// (see <c>SwiftEmitterSplitTests.RefusesConstructsItDoesNotModel</c>).
        /// <para>
        /// This gate matters most for the two AC DER sets. Ten of their sixteen vectors have no
        /// reference encoder behind them at all — cbexigen does not generate the Amendment 1 DER
        /// schemas — so for those messages the only thing standing between the Swift codec and a
        /// silent misreading of the grammar is two independent emitters agreeing.
        /// </para>
        /// </summary>
        [TestCase("Vanaheimr.V2G.Exi.Iso15118_20.CommonMessages", "CommonMessagesCodec")]
        [TestCase("Vanaheimr.V2G.Exi.Iso15118_20.DC",             "DCCodec")]
        [TestCase("Vanaheimr.V2G.Exi.Iso15118_20.AC",             "ACCodec")]
        [TestCase("Vanaheimr.V2G.Exi.Iso15118_20.ACDP",           "ACDPCodec")]
        [TestCase("Vanaheimr.V2G.Exi.Iso15118_20.AC_DER_IEC",      "AcDerIecCodec")]
        [TestCase("Vanaheimr.V2G.Exi.Iso15118_20.AC_DER_SAE",      "AcDerSaeCodec")]
        public void SwiftAndCSharpAgreeAcrossAnIso20Set(string project, string codec)
        {
            var schema = EmitterHarness.RealSchemaSet(project)
                                       .Select(f => (f.Name, f.Xsd))
                                       .ToArray();

            AssertAgrees(EmitterHarness.EmitSwift("c20", codec, schema), "swift",
                         EmitterHarness.EmitCSharp("C20", codec, schema));
        }

        /// <summary>
        /// The same, for Kotlin — plus <b>WPT</b>, which Swift refuses and Kotlin generates.
        /// </summary>
        /// <remarks>
        /// WPT is where this gate is worth the most and trusted the least. Its
        /// <c>WPT_LF_TransmitterDataType</c> is the self-loop list shape for which cbexigen's own
        /// encoder cannot represent even the schema's required minimum, so the vectors cannot reach
        /// it: there is no reference encoding of that construct anywhere. Two independent emitters
        /// agreeing is therefore the *only* check the set has — and agreement between two ports of
        /// one grammar is not evidence that the grammar was read correctly, just that it was read
        /// the same way twice. Swift's answer to the same problem was to refuse the set outright.
        /// </remarks>
        [TestCase("Vanaheimr.V2G.Exi.Iso15118_20.CommonMessages", "CommonMessagesCodec")]
        [TestCase("Vanaheimr.V2G.Exi.Iso15118_20.DC",             "DCCodec")]
        [TestCase("Vanaheimr.V2G.Exi.Iso15118_20.AC",             "ACCodec")]
        [TestCase("Vanaheimr.V2G.Exi.Iso15118_20.ACDP",           "ACDPCodec")]
        [TestCase("Vanaheimr.V2G.Exi.Iso15118_20.AC_DER_IEC",     "AcDerIecCodec")]
        [TestCase("Vanaheimr.V2G.Exi.Iso15118_20.AC_DER_SAE",     "AcDerSaeCodec")]
        [TestCase("Vanaheimr.V2G.Exi.Iso15118_20.WPT",            "WPTCodec")]
        public void KotlinAndCSharpAgreeAcrossAnIso20Set(string project, string codec)
        {
            var schema = EmitterHarness.RealSchemaSet(project)
                                       .Select(f => (f.Name, f.Xsd))
                                       .ToArray();

            AssertAgrees(EmitterHarness.Emit("c20", codec, schema), "kotlin",
                         EmitterHarness.EmitCSharp("C20", codec, schema));
        }

        /// <summary>
        /// Every whole-set comparison asserts the same three things, so they are stated once.
        /// </summary>
        /// <remarks>
        /// The count check is relative to the reference rather than a guessed constant: a back end
        /// may emit a helper the C# one has no counterpart for — as Swift does for enumerations,
        /// whose fallible initialiser C# expresses as an inline cast — but never fewer codecs. And
        /// it guards the failure mode that would make all of this vacuous: a parse that silently
        /// produced nothing, against which an empty diff proves precisely nothing.
        /// </remarks>
        private static void AssertAgrees(IReadOnlyList<GeneratedFile> emitted, string name,
                                         IReadOnlyList<GeneratedFile> reference)
        {
            var actual = CrossEmitterComparison.Operations(emitted);
            var csharp = CrossEmitterComparison.Operations(reference);

            Assert.That(csharp.Count, Is.GreaterThan(20), "the reference parse produced almost nothing");
            Assert.That(actual, Is.Not.Empty, $"no functions parsed out of the {name} output");
            Assert.That(actual.Count, Is.GreaterThanOrEqualTo(csharp.Count));

            var problems = CrossEmitterComparison.Diff(actual, name, csharp, "csharp");
            Assert.That(problems, Is.Empty,
                        $"{problems.Count} of {csharp.Count} functions differ:\n" +
                        string.Join("\n", problems.Take(15)));
        }

        /// <summary>
        /// Every back end must route each document index to the same message decoder.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A separate claim from the operation sequences above, and one nothing else was making.
        /// The sequences say each codec reads and writes the same bits in the same order; this says
        /// the dispatcher sends event code 4 to the same message everywhere. The arm keys are
        /// deliberately excluded from an operation's identity — the back ends spell a keyed branch
        /// too differently — so a back end that swapped two indices would agree operation for
        /// operation and still hand every peer the wrong message.
        /// </para>
        /// <para>
        /// The vectors would not have caught it either. They drive the per-message codecs, and a
        /// document whose index is misrouted decodes into a well-formed instance of the wrong type
        /// — which round-trips back to the bytes it came from as long as both directions share the
        /// mistake, which they do, because both read the same table.
        /// </para>
        /// </remarks>
        [TestCase("Vanaheimr.V2G.Exi.Iso15118_2",                  "Iso15118_2Codec",     true)]
        [TestCase("Vanaheimr.V2G.Exi.Iso15118_20.CommonMessages",  "CommonMessagesCodec", true)]
        [TestCase("Vanaheimr.V2G.Exi.Iso15118_20.DC",              "DCCodec",             true)]
        [TestCase("Vanaheimr.V2G.Exi.Iso15118_20.AC",              "ACCodec",             true)]
        [TestCase("Vanaheimr.V2G.Exi.Iso15118_20.ACDP",            "ACDPCodec",           true)]
        [TestCase("Vanaheimr.V2G.Exi.Iso15118_20.AC_DER_IEC",      "AcDerIecCodec",       true)]
        [TestCase("Vanaheimr.V2G.Exi.Iso15118_20.AC_DER_SAE",      "AcDerSaeCodec",       true)]
        [TestCase("Vanaheimr.V2G.Exi.Iso15118_20.WPT",             "WPTCodec",            false)]
        public void EveryBackEndRoutesEachDocumentIndexToTheSameMessage(
            string project, string codec, bool hasSwift)
        {
            var schema = EmitterHarness.RealSchemaSet(project)
                                       .Select(f => (f.Name, f.Xsd))
                                       .ToArray();

            var csharp = CrossEmitterComparison.DocumentIndexMap(EmitterHarness.EmitCSharp("Ref", codec, schema));
            var kotlin = CrossEmitterComparison.DocumentIndexMap(EmitterHarness.Emit("ref", codec, schema));

            // Without these the comparison below is two empty dictionaries agreeing with each other.
            //
            // The counts are deliberately not asserted against a number: -2 has exactly **one**
            // document index (76 → V2G_Message, because -2 wraps every message in that one element),
            // while CommonMessages has 28. Nor are the indices a gapless run — they are the schema
            // set's global element codes, so CommonMessages goes 0,1,2,3,7,8,16,… and the gaps are
            // the elements that are not documents. What *is* invariant is that no two indices route
            // to the same message, which is the shape a copy-paste in the emitter would break.
            Assert.Multiple(() =>
            {
                Assert.That(csharp, Is.Not.Empty, "no document-index switch found in the C# output");
                Assert.That(csharp.Values.Distinct().Count(), Is.EqualTo(csharp.Count),
                            "two document indices route to the same message");
            });

            Assert.That(kotlin, Is.EqualTo(csharp), "kotlin routes a document index elsewhere than C#");

            // WPT is the one set Swift refuses (see the Kotlin -20 case above).
            if (hasSwift)
                Assert.That(CrossEmitterComparison.DocumentIndexMap(EmitterHarness.EmitSwift("ref", codec, schema)),
                            Is.EqualTo(csharp), "swift routes a document index elsewhere than C#");
        }

        /// <summary>
        /// And that check has to be able to fail, for the same reason the one below does.
        /// </summary>
        /// <remarks>
        /// The mutation is applied to the emitted <em>source</em> rather than to the extracted
        /// dictionary — swapping two entries of a dictionary and finding it unequal to itself would
        /// test NUnit, not the extractor. Rewriting the arm keys in the generated text is the thing
        /// a broken emitter would actually do.
        /// </remarks>
        [Test]
        public void DocumentIndexMapDetectsASwapInTheGeneratedSource()
        {
            var schema = EmitterHarness.RealSchemaSet("Vanaheimr.V2G.Exi.Iso15118_20.AC")
                                       .Select(f => (f.Name, f.Xsd))
                                       .ToArray();

            var files    = EmitterHarness.EmitCSharp("Ref", "ACCodec", schema);
            var original = CrossEmitterComparison.DocumentIndexMap(files);
            Assert.That(original.Count, Is.GreaterThan(1), "need at least two indices to swap");

            // Exchange the two lowest arm keys where they are written, leaving everything else alone.
            var keys    = original.Keys.OrderBy(k => k).Take(2).ToArray();
            var swapped = files.Select(f => new GeneratedFile(
                                           f.FileName,
                                           f.Source.Replace($"{keys[0]}u => ", "\0")
                                                   .Replace($"{keys[1]}u => ", $"{keys[0]}u => ")
                                                   .Replace("\0", $"{keys[1]}u => ")))
                               .ToList();

            var after = CrossEmitterComparison.DocumentIndexMap(swapped);

            Assert.That(after, Is.Not.EqualTo(original), "the extractor did not see the swap");
            Assert.That(after[keys[0]], Is.EqualTo(original[keys[1]]));
            Assert.That(after[keys[1]], Is.EqualTo(original[keys[0]]));
        }

        /// <summary>
        /// The comparison is only worth anything if it can fail. A back end whose output is parsed
        /// into an empty operation list would make every assertion above vacuously true.
        /// </summary>
        [Test]
        public void ComparisonDetectsADivergence()
        {
            var schema = AppProtocolSchema();
            var swift  = CrossEmitterComparison.Operations(
                             EmitterHarness.EmitSwift("app", "SupportedAppProtocolCodec", schema));

            Assert.That(swift.Values.Sum(v => v.Count), Is.GreaterThan(50),
                        "too few operations parsed — the extractor is not seeing the generated code");

            // Drop one operation from one function and require the diff to name it.
            var mangled = swift.ToDictionary(
                kv => kv.Key,
                kv => kv.Key == "encode:appprotocoltype"
                          ? (IReadOnlyList<string>) kv.Value.Skip(1).ToList()
                          : kv.Value,
                StringComparer.Ordinal);

            var problems = CrossEmitterComparison.Diff(mangled, "mangled", swift, "swift");
            Assert.That(problems, Has.Exactly(1).Contains("encode:appprotocoltype"));
        }
    }
}
