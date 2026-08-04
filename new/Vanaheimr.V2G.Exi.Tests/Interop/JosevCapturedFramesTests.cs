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

using cloud.charging.open.protocols.ISO15118.AppProtocol;
using cloud.charging.open.protocols.ISO15118.EXI.Tests.Infrastructure;
using cloud.charging.open.protocols.ISO15118_2.Generated;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests.Interop
{
    /// <summary>
    /// Cross-validation against real EXI bytes captured from a live <b>Josev</b> session
    /// (SwitchEV/iso15118 @ commit <c>d645255</c>, ISO 15118-2 AC EIM, no TLS — see
    /// <c>docs/interop-runs/2026-07-21-iso2-ac-eim-notls/</c>). Josev encodes with EXIficient, which shares
    /// no lineage with the cbV2G oracle our vectors come from — so bytes that our codec both decodes and
    /// re-encodes identically are an <i>independent</i> conformance signal. These run in normal CI (the
    /// captured bytes are baked in; no Josev needed at test time).
    /// </summary>
    [TestFixture]
    public class JosevCapturedFramesTests
    {
        // ── SupportedAppProtocol (the SAP handshake) ─────────────────────────────────────────────────

        [Test]
        public void Josev_SupportedAppProtocolReq_DecodesToExpectedContent_AndReEncodesIdentically()
        {
            var josev = HexUtil.Parse("8000ebab9371d34b9b79d189a98989c1d191d191818999d26b9b3a232b30020000040040");

            var decoded = SupportedAppProtocolCodec.DecodeAny(josev, out int consumed);
            Assert.That(consumed, Is.EqualTo(josev.Length));
            Assert.That(decoded, Is.InstanceOf<SupportedAppProtocolReq>());

            var req = (SupportedAppProtocolReq) decoded;
            Assert.That(req.AppProtocols, Has.Count.EqualTo(1));
            var p = req.AppProtocols[0];
            Assert.Multiple(() =>
            {
                Assert.That(p.ProtocolNamespace, Is.EqualTo("urn:iso:15118:2:2013:MsgDef"));
                Assert.That(p.VersionNumberMajor, Is.EqualTo(2u));
                Assert.That(p.VersionNumberMinor, Is.EqualTo(0u));
                Assert.That(p.SchemaID, Is.EqualTo((byte) 1));
                Assert.That(p.Priority, Is.EqualTo((byte) 1));
            });

            var buf = new byte[256];
            Assert.That(SupportedAppProtocolCodec.TryEncodeRequest(req, buf, out int written), Is.True);
            Assert.That(buf.AsSpan(0, written).ToArray(), Is.EqualTo(josev),
                "our codec must re-encode Josev's SupportedAppProtocolReq to the identical bytes");
        }

        [Test]
        public void Josev_SupportedAppProtocolRes_RoundTripsIdentically()
        {
            var josev = HexUtil.Parse("80400040");

            var decoded = SupportedAppProtocolCodec.DecodeAny(josev, out int consumed);
            Assert.That(consumed, Is.EqualTo(josev.Length));
            Assert.That(decoded, Is.InstanceOf<SupportedAppProtocolRes>());

            var buf = new byte[64];
            Assert.That(SupportedAppProtocolCodec.TryEncodeResponse((SupportedAppProtocolRes) decoded, buf, out int written), Is.True);
            Assert.That(buf.AsSpan(0, written).ToArray(), Is.EqualTo(josev));
        }

        // ── ISO 15118-2 (a real V2G message) ─────────────────────────────────────────────────────────

        [Test]
        public void Josev_SessionSetupReq_RoundTripsThroughOurCodec()
        {
            // EVCCID 5E929D736493, SessionID 00 — per Josev's decoded log for this exact frame.
            var josev = HexUtil.Parse("8098004011d0197a4a75cd924c00");

            var decoded = (V2G_Message) Iso2Codec.DecodeAny(josev, out int consumed);
            Assert.That(consumed, Is.EqualTo(josev.Length), "our decoder must consume all of Josev's bytes");
            Assert.That(decoded, Is.Not.Null);

            var buf = new byte[256];
            Assert.That(decoded.TryEncode(buf, out int n), Is.True);
            Assert.That(buf.AsSpan(0, n).ToArray(), Is.EqualTo(josev),
                "our codec must re-encode Josev's SessionSetupReq to the identical bytes");
        }
    }
}
