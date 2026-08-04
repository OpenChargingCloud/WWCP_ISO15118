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
using cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{

    /// <summary>
    /// The decisions the round-trip oracle is structurally unable to check.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="JsonLdRoundtripTests"/> runs entirely in C#, and the JSON-LD form exists to cross a
    /// bridge into JavaScript (§4.4, and B1's event stream is where it lands). Anything decided for
    /// the far side round-trips perfectly here whether it is right or wrong — the check is symmetric,
    /// so it agrees with itself.
    /// </para>
    /// <para>
    /// This is the recurring shape stated in §5 once more: a check made tolerant of some legitimate
    /// variation stops seeing what that variation was hiding. Here the tolerance is "whatever this
    /// language can represent", and what it hides is every consumer that cannot.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class JsonLdBridgeTests
    {

        private static MessageHeaderType Header(ulong timestamp) =>
            new(SessionID: [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08],
                TimeStamp: timestamp,
                Signature: null);


        /// <summary>
        /// A 64-bit value above 2^53 is written as a JSON <b>string</b>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// JSON has no integers, only doubles, and every JavaScript consumer of this bridge rounds
        /// silently above <c>Number.MAX_SAFE_INTEGER</c>. ISO 15118 reaches past it:
        /// <c>X509SerialNumber</c> is an <c>xs:long</c> and real certificate serials use the range,
        /// while <c>TimeAnchor</c> and <c>TimeStamp</c> are <c>xs:unsignedLong</c>.
        /// </para>
        /// <para>
        /// The failure this prevents would appear only on a phone, only for some values, as a
        /// certificate that does not verify — and never once in this test suite if the value were a
        /// number, because <c>System.Text.Json</c> handles <c>ulong</c> exactly.
        /// </para>
        /// </remarks>
        [Test]
        public void SixtyFourBitValuesCrossTheBridgeAsStrings()
        {

            const ulong beyondDoublePrecision = 9_007_199_254_740_993UL;   // 2^53 + 1

            var json = CommonMessagesCodecJson.ToJSON(
                           new SessionStopReq(Header(beyondDoublePrecision), ChargingSession.Terminate, null, null));

            var timestamp = json["header"]!["timeStamp"]!;

            Assert.That(timestamp.GetValueKind(), Is.EqualTo(JsonValueKind.String),
                        "a 64-bit value written as a JSON number is rounded by every JavaScript reader");
            Assert.That(timestamp.GetValue<string>(), Is.EqualTo("9007199254740993"));

            // The hazard, made concrete: this is what the same value would come back as had it been
            // written as a number and read by a consumer whose only numeric type is a double.
            Assert.That((ulong) (double) beyondDoublePrecision, Is.Not.EqualTo(beyondDoublePrecision),
                        "if this ever passes, the premise of the string encoding has changed");

            var parsed = (SessionStopReq) CommonMessagesCodecJson.ParseJSON(JsonNode.Parse(json.ToJsonString())!);
            Assert.That(parsed.Header.TimeStamp, Is.EqualTo(beyondDoublePrecision));

        }


        /// <summary>
        /// Absent optional properties are omitted, not written as <c>null</c>.
        /// </summary>
        /// <remarks>
        /// A round trip cannot tell the two apart — the parser folds absent and null together — but a
        /// reader can. A document listing what is there is a much smaller thing to send over a bridge
        /// than one listing everything a schema permits, and -20 messages are mostly optional fields.
        /// </remarks>
        [Test]
        public void AbsentPropertiesAreOmittedRatherThanNull()
        {

            var json = CommonMessagesCodecJson.ToJSON(
                           new SessionStopReq(Header(1_700_000_000UL), ChargingSession.Terminate, null, null));

            Assert.That(json.ContainsKey("evTerminationCode"), Is.False);
            Assert.That(json["header"]!.AsObject().ContainsKey("signature"), Is.False);

            var present = CommonMessagesCodecJson.ToJSON(
                              new SessionStopReq(Header(1_700_000_000UL), ChargingSession.Terminate,
                                                 "sudden", null));

            Assert.That(present["evTerminationCode"]!.GetValue<string>(), Is.EqualTo("sudden"));

        }


        /// <summary>Octet strings are lower-case hex, and an enum is its name.</summary>
        /// <remarks>
        /// Both are choices rather than consequences — base64 and an ordinal would each round-trip
        /// just as well — and both are what a person reading an event stream can act on. The ordinal
        /// in particular would make the bridge's output depend on the order members happen to appear
        /// in the XSD.
        /// </remarks>
        [Test]
        public void BinaryIsHexAndEnumsAreNames()
        {

            var json = CommonMessagesCodecJson.ToJSON(
                           new SessionStopReq(Header(1_700_000_000UL), ChargingSession.Pause, null, null));

            Assert.That(json["header"]!["sessionID"]!.GetValue<string>(), Is.EqualTo("0102030405060708"));
            Assert.That(json["chargingSession"]!.GetValue<string>(), Is.EqualTo("Pause"));

        }


        /// <summary>
        /// The naming rule reads acronyms the way a person does.
        /// </summary>
        /// <remarks>
        /// Asserted directly because the round trip is blind to it: renaming every property leaves
        /// all 163 round-trip tests green, since the serializer and the parser rename together. The
        /// names are a wire format the moment anything outside this repository reads one.
        /// </remarks>
        [Test]
        public void PropertyNamesKeepAcronymsReadable()
        {

            var json = CommonMessagesCodecJson.ToJSON(
                           new SessionStopReq(Header(1_700_000_000UL), ChargingSession.Terminate, null, null));

            Assert.That(json.ContainsKey("evTerminationCode"), Is.False,
                        "absent, but note the spelling asserted below");

            var header = json["header"]!.AsObject();

            // 'SessionID' -> 'sessionID', not 'sessionId': the rule lower-cases, it never re-spells.
            Assert.That(header.ContainsKey("sessionID"), Is.True, string.Join(", ", header.Select(p => p.Key)));
            Assert.That(header.ContainsKey("timeStamp"), Is.True);

        }


        /// <summary>
        /// A property carrying the wrong <c>@type</c> is refused by name, not by a cast that fails
        /// somewhere unhelpful.
        /// </summary>
        [Test]
        public void APropertyWithTheWrongTypeSaysWhichPropertyItWas()
        {

            var json = CommonMessagesCodecJson.ToJSON(
                           new SessionStopReq(Header(1_700_000_000UL), ChargingSession.Terminate, null, null));

            json["header"] = new JsonObject { ["@type"] = "RationalNumberType", ["exponent"] = 0, ["value"] = 0 };

            var thrown = Assert.Throws<JsonLdException>(() => CommonMessagesCodecJson.ParseJSON(json));

            Assert.That(thrown!.Message, Does.Contain("header"));
            Assert.That(thrown.Message, Does.Contain("RationalNumberType"));
            Assert.That(thrown.Message, Does.Contain("MessageHeaderType"));

        }

    }

}
