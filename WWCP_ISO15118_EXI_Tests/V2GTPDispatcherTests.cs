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
using cloud.charging.open.protocols.ISO15118.AppProtocol;
using cloud.charging.open.protocols.ISO15118.EXI.Tests.Infrastructure;
using cloud.charging.open.protocols.ISO15118_2.Generated;
using cloud.charging.open.protocols.ISO15118.EXI.Dispatch;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{

    /// <summary>
    /// The V2GTP payload-type dispatcher: given a frame, resolve the right message set and decode it
    /// without knowing in advance which of the five codecs applies; given a set and an already-encoded
    /// EXI payload, wrap it with the matching header. Covers correct mapping per set plus the error paths
    /// (bad header, length-field mismatch, unknown payload type).
    /// </summary>
    [TestFixture]
    public class V2GTPDispatcherTests
    {
        private static byte[] Frame(MessageSet set, byte[] exiPayload)
        {
            var dest = new byte[V2GTPCodec.HeaderSize + exiPayload.Length];
            Assert.That(V2GTPDispatcher.TryEncode(set, exiPayload, dest, out int n), Is.True);
            Assert.That(n, Is.EqualTo(dest.Length));
            return dest;
        }

        [Test]
        public void SapSharesTheIso2PayloadIdAndIsNotDispatchedByPayloadType()
        {
            // The SupportedAppProtocol handshake uses payload id 0x8001 — the SAME as the DIN/-2 messages
            // (ISO 15118-20 §A / libcbv2g V2GTP20_SAP_PAYLOAD_ID / Josev). SAP is told apart from a -2 message
            // by session phase, not payload type, so the payload-type dispatcher does NOT resolve SAP (a live
            // interop run against Josev caught the earlier distinct 0x8000 here as a wire-conformance bug — the
            // SAP handshake now frames/decodes 0x8001 explicitly). This asserts the shared-id fact.
            Assert.That(V2GTPCodec.PayloadType_AppProtocol, Is.EqualTo((ushort) 0x8001));
            Assert.That(V2GTPCodec.PayloadType_AppProtocol, Is.EqualTo(V2GTPCodec.PayloadType_DinIso2Main));
        }

        [Test]
        public void RoundtripsIso15118_2()
        {
            var msg = Iso15118_2Fixtures.ByName["SessionSetupReq"];
            var payload = new byte[512];
            Assert.That(msg.TryEncode(payload, out int n), Is.True);

            var frame = Frame(MessageSet.Iso15118_2, payload.AsSpan(0, n).ToArray());

            Assert.That(V2GTPDispatcher.TryDecode(frame, out var set, out var message, out var error), Is.True);
            Assert.That(error, Is.Null);
            Assert.That(set, Is.EqualTo(MessageSet.Iso15118_2));
            Assert.That(message, Is.InstanceOf<V2G_Message>());
        }

        [Test]
        public void RoundtripsIso20CommonMessages() =>
            AssertRoundtrips(MessageSet.Iso20CommonMessages, "SessionSetupReq", Iso15118_20CommonFixtures.TryEncode);

        [Test]
        public void RoundtripsIso20DC() =>
            AssertRoundtrips(MessageSet.Iso20DC, "DC_CableCheckReq", Iso15118_20DcFixtures.TryEncode);

        [Test]
        public void RoundtripsIso20AC() =>
            AssertRoundtrips(MessageSet.Iso20AC, "AC_ChargeParameterDiscoveryReq", Iso15118_20AcFixtures.TryEncode);

        [Test]
        public void RoundtripsIso20WPT() =>
            AssertRoundtrips(MessageSet.Iso20WPT, "WPT_FinePositioningReq", Iso15118_20WptFixtures.TryEncode);

        [Test]
        public void RoundtripsIso20ACDP() =>
            AssertRoundtrips(MessageSet.Iso20ACDP, "ACDP_ConnectReq", Iso15118_20AcdpFixtures.TryEncode);

        private delegate bool TryEncodeFn(string vectorName, byte[] dest, out int bytesWritten);

        private static void AssertRoundtrips(MessageSet set, string vectorName, TryEncodeFn tryEncode)
        {
            var payload = new byte[512];
            Assert.That(tryEncode(vectorName, payload, out int n), Is.True);

            var frame = Frame(set, payload.AsSpan(0, n).ToArray());

            Assert.That(V2GTPDispatcher.TryDecode(frame, out var decodedSet, out var message, out var error), Is.True);
            Assert.That(error, Is.Null);
            Assert.That(decodedSet, Is.EqualTo(set));
            Assert.That(message, Is.Not.Null);
        }

        [Test]
        public void TryDecode_RejectsBadHeader()
        {
            var frame = new byte[] { 0x02, 0xFE, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00 }; // wrong version byte
            Assert.That(V2GTPDispatcher.TryDecode(frame, out _, out var message, out var error), Is.False);
            Assert.That(message, Is.Null);
            Assert.That(error, Does.Contain("V2GTP frame"));
        }

        [Test]
        public void TryDecode_RejectsLengthFieldMismatch()
        {
            var dest = new byte[V2GTPCodec.HeaderSize + 3];
            V2GTPCodec.WriteHeader(dest, V2GTPCodec.PayloadType_AppProtocol, payloadLength: 99); // lies about the length
            Assert.That(V2GTPDispatcher.TryDecode(dest, out _, out var message, out var error), Is.False);
            Assert.That(message, Is.Null);
            Assert.That(error, Does.Contain("length mismatch"));
        }

        [Test]
        public void TryDecode_RejectsUnknownPayloadType()
        {
            var dest = new byte[V2GTPCodec.HeaderSize + 1];
            V2GTPCodec.WriteHeader(dest, payloadType: 0x8101, payloadLength: 1); // ScheduleRenegotiation — not modelled
            Assert.That(V2GTPDispatcher.TryDecode(dest, out _, out var message, out var error), Is.False);
            Assert.That(message, Is.Null);
            Assert.That(error, Does.Contain("unknown V2GTP payload type"));
        }

        [Test]
        public void TryEncode_FailsWhenDestinationTooSmall()
        {
            var dest = new byte[V2GTPCodec.HeaderSize]; // no room for the payload itself
            Assert.That(V2GTPDispatcher.TryEncode(MessageSet.AppProtocol, new byte[] { 0x80 }, dest, out int n), Is.False);
            Assert.That(n, Is.EqualTo(0));
        }
    }
}
