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
using System.Text.Json.Nodes;
using NUnit.Framework;
using cloud.charging.open.protocols.ISO15118.EXI.Tests.Infrastructure;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{

    /// <summary>
    /// The JSON-LD serializer's oracle (EVSimulatorApp/docs/CONCEPT.md §4.4): for every vector in the corpus,
    /// <c>EXI → JSON → EXI</c> must reproduce the original bytes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// §4.4 gave up an oracle to get here — the hand-written WWCP model that would have checked the
    /// JSON is the code the generated approach replaces — and named this as the compensation. It is a
    /// stronger property than "every field survives": a mapping can carry every value and still lose
    /// <em>which substitution member</em> a polymorphic field held, or which branch of a choice was
    /// taken, and both come back as different bytes rather than as a missing field.
    /// </para>
    /// <para>
    /// The corpus is the one the wire codec is already held to, so nothing here needs its own
    /// fixtures: a vector is bytes cbV2G produced, and those bytes are the input.
    /// </para>
    /// <para>
    /// <b>What this cannot see.</b> It runs entirely in C#, and the JSON-LD form exists to cross a
    /// bridge into JavaScript. Every decision made for the far side — the 64-bit-integer-as-string
    /// rule above all — round-trips perfectly here whether it is right or wrong. Those need their own
    /// tests, and <see cref="JsonLdBridgeTests"/> is where they are.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class JsonLdRoundtripTests
    {

        public sealed record Vec(string Name, string ExpectedHex);

        private static IEnumerable<TestCaseData> Vectors(string fileName)
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Vectors", fileName);
            using var doc = JsonDocument.Parse(File.ReadAllText(path));

            foreach (var v in doc.RootElement.GetProperty("vectors").EnumerateArray())
                yield return new TestCaseData(new Vec(v.GetProperty("name").GetString()!,
                                                      v.GetProperty("expectedHex").GetString()!))
                             .SetName(v.GetProperty("name").GetString()!);
        }

        private static IEnumerable<TestCaseData> AppProtocolVectors() => Vectors("AppProtocol.vectors.json");
        private static IEnumerable<TestCaseData> Iso2Vectors()        => Vectors("Iso15118_2.vectors.json");
        private static IEnumerable<TestCaseData> CommonVectors()      => Vectors("Iso15118_20.CommonMessages.vectors.json");
        private static IEnumerable<TestCaseData> DcVectors()          => Vectors("Iso15118_20.DC.vectors.json");
        private static IEnumerable<TestCaseData> AcVectors()          => Vectors("Iso15118_20.AC.vectors.json");
        private static IEnumerable<TestCaseData> WptVectors()         => Vectors("Iso15118_20.WPT.vectors.json");
        private static IEnumerable<TestCaseData> AcdpVectors()        => Vectors("Iso15118_20.ACDP.vectors.json");
        private static IEnumerable<TestCaseData> AcDerIecVectors()    => Vectors("Iso15118_20.AC_DER_IEC.vectors.json");
        private static IEnumerable<TestCaseData> AcDerSaeVectors()    => Vectors("Iso15118_20.AC_DER_SAE.vectors.json");


        [TestCaseSource(nameof(AppProtocolVectors))]
        public void AppProtocol_Survives_JSON(Vec vector) => AssertRoundtrips(vector, JsonLdBridge.AppProtocol);

        [TestCaseSource(nameof(Iso2Vectors))]
        public void Iso15118_2_Survives_JSON(Vec vector) => AssertRoundtrips(vector, JsonLdBridge.Iso2);

        [TestCaseSource(nameof(CommonVectors))]
        public void CommonMessages_Survives_JSON(Vec vector) => AssertRoundtrips(vector, JsonLdBridge.Common);

        [TestCaseSource(nameof(DcVectors))]
        public void DC_Survives_JSON(Vec vector) => AssertRoundtrips(vector, JsonLdBridge.Dc);

        [TestCaseSource(nameof(AcVectors))]
        public void AC_Survives_JSON(Vec vector) => AssertRoundtrips(vector, JsonLdBridge.Ac);

        [TestCaseSource(nameof(WptVectors))]
        public void WPT_Survives_JSON(Vec vector) => AssertRoundtrips(vector, JsonLdBridge.Wpt);

        [TestCaseSource(nameof(AcdpVectors))]
        public void ACDP_Survives_JSON(Vec vector) => AssertRoundtrips(vector, JsonLdBridge.Acdp);

        [TestCaseSource(nameof(AcDerIecVectors))]
        public void AcDerIec_Survives_JSON(Vec vector) => AssertRoundtrips(vector, JsonLdBridge.AcDerIec);

        [TestCaseSource(nameof(AcDerSaeVectors))]
        public void AcDerSae_Survives_JSON(Vec vector) => AssertRoundtrips(vector, JsonLdBridge.AcDerSae);


        private static void AssertRoundtrips(Vec vector, JsonLdBridge bridge)
        {

            var original = HexUtil.Parse(vector.ExpectedHex);

            var decoded = bridge.Decode(original, out var consumed);
            Assert.That(consumed, Is.EqualTo(original.Length), $"{vector.Name}: the vector did not decode fully");

            var json = bridge.ToJSON(decoded);

            // Through text, not just through the object graph: a JsonObject that only round-trips
            // in memory would still be broken on a bridge that serialises it.
            var text     = json.ToJsonString();
            var reparsed = bridge.ParseJSON(JsonNode.Parse(text)!);

            var buffer = new byte[original.Length * 2 + 64];
            Assert.That(bridge.Encode(reparsed, buffer, out var written), Is.True,
                        $"{vector.Name}: re-encoding failed");

            var actual = buffer[..written];
            Assert.That(actual, Is.EqualTo(original),
                        $"{vector.Name}: the bytes changed on the way through JSON.\n"
                      + HexUtil.Diff(original, actual) + "\n" + text);

        }


        /// <summary>
        /// Every message set carries a <c>@context</c>, and it is the XSD target namespace.
        /// </summary>
        /// <remarks>
        /// Asserted because §4.4 asks for "@context per namespace" and a document without one is not
        /// JSON-LD at all — it is JSON that happens to look like it. Nothing in the round-trip would
        /// notice its absence, since the parser dispatches on <c>@type</c>.
        /// </remarks>
        [Test]
        public void EveryDocumentCarriesItsVocabulary()
        {

            foreach (var (bridge, expected) in new (JsonLdBridge, string)[] {
                (JsonLdBridge.AppProtocol, "urn:iso:15118:2:2010:AppProtocol"),
                (JsonLdBridge.Iso2,        "urn:iso:15118:2:2013:MsgDef"),
                (JsonLdBridge.Common,      "urn:iso:std:iso:15118:-20:CommonMessages"),
                (JsonLdBridge.Dc,          "urn:iso:std:iso:15118:-20:DC"),
                (JsonLdBridge.Ac,          "urn:iso:std:iso:15118:-20:AC"),
            })
                Assert.That(bridge.Context, Is.EqualTo(expected));

        }


        /// <summary>
        /// A document whose <c>@type</c> names a type of some other message set is refused, and the
        /// message says so.
        /// </summary>
        /// <remarks>
        /// The bridge carries seven message sets over one channel, and they share type names —
        /// <c>SessionSetupReq</c> exists in -2 and in -20. Reading one as the other would either
        /// throw somewhere deep or, worse, half-succeed.
        /// </remarks>
        [Test]
        public void ADocumentFromAnotherMessageSetIsRefusedByName()
        {

            var alien = new JsonObject { ["@type"] = "PowerDeliveryReq" };

            var thrown = Assert.Throws<JsonLdException>(() => JsonLdBridge.Iso2.ParseJSON(alien));
            Assert.That(thrown!.Message, Does.Contain("PowerDeliveryReq"));
            Assert.That(thrown.Message, Does.Contain("not a type of this message set"));

        }


        /// <summary>An object with no <c>@type</c> cannot be read, and says that rather than crashing.</summary>
        [Test]
        public void ADocumentWithoutATypeTagIsRefused()
        {

            var thrown = Assert.Throws<JsonLdException>(
                             () => JsonLdBridge.Common.ParseJSON(new JsonObject { ["header"] = new JsonObject() }));

            Assert.That(thrown!.Message, Does.Contain("@type"));

        }

    }

}
