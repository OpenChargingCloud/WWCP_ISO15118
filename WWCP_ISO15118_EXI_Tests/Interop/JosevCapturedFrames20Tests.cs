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

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests.Interop
{
    /// <summary>
    /// ISO 15118-<b>20</b> DC frames captured from a live Josev session (SwitchEV/iso15118 @ <c>d645255</c>,
    /// rebuilt on Debian trixie, EXI codec 1.55 - see <c>docs/interop-runs/2026-07-21-iso20-dc-pnc-notls/</c>).
    /// The session negotiated <c>ISO_15118_20_DC</c> over SAP and ran a full <b>Plug &amp; Charge</b> DC loop
    /// (SessionSetup, AuthorizationSetup, signed Authorization, ServiceDiscovery/Detail/Selection,
    /// DC_ChargeParameterDiscovery, ScheduleExchange, CableCheck, PreCharge, PowerDelivery, DC_ChargeLoop,
    /// WeldingDetection, SessionStop). Josev encodes with EXIficient (independent of the cbV2G oracle our
    /// vectors come from), so bytes our codec both decodes and re-encodes identically are an <i>independent</i>
    /// conformance signal - now including a real -20 header <c>Signature</c> on the wire (the ~1.3 KB signed
    /// <c>AuthorizationReq</c>). Runs in normal CI (bytes baked in, no Josev at test time).
    /// </summary>
    [TestFixture]
    public class JosevCapturedFrames20Tests
    {
        // -- SupportedAppProtocol (SAP handshake, negotiating urn:iso:std:iso:15118:-20:DC) --------------

        [Test]
        public void Josev20_SupportedAppProtocolReq_RoundTripsIdentically()
        {
            var josev = HexUtil.Parse("8000f3ab9371d34b9b79d39ba321d34b9b79d189a98989c1d1699181d22218010000040040");

            var decoded = SupportedAppProtocolCodec.DecodeAny(josev, out int consumed);
            Assert.That(consumed, Is.EqualTo(josev.Length));
            Assert.That(decoded, Is.InstanceOf<SupportedAppProtocolReq>());

            var buf = new byte[256];
            Assert.That(SupportedAppProtocolCodec.TryEncodeRequest((SupportedAppProtocolReq) decoded, buf, out int n), Is.True);
            Assert.That(buf.AsSpan(0, n).ToArray(), Is.EqualTo(josev),
                "our codec must re-encode Josev's -20 SupportedAppProtocolReq to the identical bytes");
        }

        [Test]
        public void Josev20_SupportedAppProtocolRes_RoundTripsIdentically()
        {
            var josev = HexUtil.Parse("80400040");

            var decoded = SupportedAppProtocolCodec.DecodeAny(josev, out int consumed);
            Assert.That(consumed, Is.EqualTo(josev.Length));
            Assert.That(decoded, Is.InstanceOf<SupportedAppProtocolRes>());

            var buf = new byte[64];
            Assert.That(SupportedAppProtocolCodec.TryEncodeResponse((SupportedAppProtocolRes) decoded, buf, out int n), Is.True);
            Assert.That(buf.AsSpan(0, n).ToArray(), Is.EqualTo(josev));
        }

        // -- ISO 15118-20 CommonMessages ----------------------------------------------------------------

        private static readonly (string Name, string Hex)[] CommonFrames =
        {
            ("SessionSetupReq", "808c040000000000000000084d3fdd20620b2ba6a4ab1899199a1a9b1b9c1c9820a121a222ac00"),
            ("SessionSetupRes", "80900437eeba1382ed0912884d3fdd206204031552cc4c8cd14c4c8ccd00"),
            ("AuthorizationSetupReq", "80080437eeba1382ed0912884d3fdd2062"),
            ("AuthorizationSetupRes", "800c0437eeba1382ed0912885d3fdd2062002012085a618e31278d563cf4d4ab3d822512d390"),
            ("AuthorizationRes", "80040437eeba1382ed0912885d3fdd20620000"),
            ("ServiceDiscoveryReq", "807c0437eeba1382ed0912886d3fdd206280"),
            ("ServiceDiscoveryRes", "80800437eeba1382ed0912886d3fdd206200000200018050"),
            ("ServiceDetailReq", "80740437eeba1382ed0912886d3fdd20620100"),
            ("ServiceDetailRes", "80780437eeba1382ed0912886d3fdd20620000800202d0dbdb9b9958dd1bdc9804014455653454e6f6d696e616c566f6c7461676564801802541c9a58da5b99d80000d436f6e74726f6c4d6f6465600804d35bd89a5b1a5d1e539959591cd35bd9195802200402d0dbdb9b9958dd1bdc9804014455653454e6f6d696e616c566f6c7461676564801802541c9a58da5b99d80000d436f6e74726f6c4d6f6465601004d35bd89a5b1a5d1e539959591cd35bd9195804200602d0dbdb9b9958dd1bdc9804014455653454e6f6d696e616c566f6c7461676564801802541c9a58da5b99d80000d436f6e74726f6c4d6f6465601004d35bd89a5b1a5d1e539959591cd35bd919580228"),
            ("ServiceSelectionReq", "80840437eeba1382ed0912886d3fdd206200800880"),
            ("ServiceSelectionRes", "80880437eeba1382ed0912886d3fdd20620000"),
            ("ScheduleExchangeReq", "806c0437eeba1382ed0912886d3fdd20627e84280e008300a010602803f00280000480e0418848400002a2aaa903275726e3a69736f3a7374643a69736f3a31353131383a2d32303a5072696365416c676f726974686d3a312d506f7765720000200000100000140"),
            ("ScheduleExchangeRes", "80700437eeba1382ed0912886d3fdd206200040040000830ac0202003403c0901c08300a2420000240a8aaaa401515391c193ab9371d34b9b79d39ba321d34b9b79d189a98989c1d1699181d283934b1b2a0b633b7b934ba34369d1896a837bbb2b901000020400050001017576861742061206772656174207461782072756c6508000a00444442407010002802000000000000000800000a0901c04180f0034aed0c2e840c240cee4cac2e840c8cae6c6e4d2e0e8d2dedc0002000c8240704036aed0c2e840c240cee4cac2e840e6cae4ecd2c6ca40dcc2daca100000100008480e0418051210000120545555200a8a9c8e0c9d5c9b8e9a5cdbce9cdd190e9a5cdbce8c4d4c4c4e0e8b4c8c0e941c9a58d9505b19dbdc9a5d1a1b4e8c4b541bddd95c8080001020002800080babb430ba10309033b932b0ba103a30bc10393ab6328400050022222120380800140100000000000000040000050480e020c07801a576861742061206772656174206465736372697074696f6e00010006412038201b5768617420612067726561742073657276696365206e616d650800000840"),
            ("PowerDeliveryReq", "80540437eeba1382ed0912888d3fdd20622040"),
            ("PowerDeliveryRes", "80580437eeba1382ed0912888d3fdd20620040"),
            ("SessionStopReq", "80940437eeba1382ed091288ad3fdd206228"),
            ("SessionStopRes", "80980437eeba1382ed091288ad3fdd20620000"),
        };

        private static IEnumerable<TestCaseData> CommonCases()
        {
            foreach (var (name, hex) in CommonFrames)
                yield return new TestCaseData(hex).SetName(name);
        }

        [TestCaseSource(nameof(CommonCases))]
        public void Josev20_CommonMessagesFrame_DecodesAndReEncodesIdentically(string hex)
        {
            var josev = HexUtil.Parse(hex);
            var reEncoded = Iso15118_20CommonFixtures.DecodeReEncode(josev);
            Assert.That(reEncoded, Is.EqualTo(josev),
                "our codec must re-encode Josev's -20 CommonMessages frame to the identical bytes");
        }

        // -- ISO 15118-20 DC ----------------------------------------------------------------------------

        private static readonly (string Name, string Hex)[] DcFrames =
        {
            ("DC_ChargeParameterDiscoveryReq", "803c0437eeba1382ed0912886d3fdd20628830ac0204003202002b0081000140800e807040005080"),
            ("DC_ChargeParameterDiscoveryRes", "80400437eeba1382ed0912886d3fdd2062004400740382001901000c808000a04007a01820002808000a00"),
            ("DC_CableCheckReq", "802c0437eeba1382ed0912887d3fdd2062"),
            ("DC_CableCheckRes", "80300437eeba1382ed0912887d3fdd20620010"),
            ("DC_PreChargeReq", "80440437eeba1382ed0912887d3fdd206221060280830140"),
            ("DC_PreChargeRes", "80480437eeba1382ed0912887d3fdd20620010000200"),
            ("DC_ChargeLoopReq", "80340437eeba1382ed0912888d3fdd2062005184020c0508c8101404080a14"),
            ("DC_ChargeLoopRes", "80380437eeba1382ed0912888d3fdd206200640000820000400080800e80701000c804003201001e8060"),
            ("DC_WeldingDetectionReq", "804c0437eeba1382ed091288ad3fdd206220"),
            ("DC_WeldingDetectionRes", "80500437eeba1382ed091288ad3fdd20620010000200"),
        };

        private static IEnumerable<TestCaseData> DcCases()
        {
            foreach (var (name, hex) in DcFrames)
                yield return new TestCaseData(hex).SetName(name);
        }

        [TestCaseSource(nameof(DcCases))]
        public void Josev20_DcFrame_DecodesAndReEncodesIdentically(string hex)
        {
            var josev = HexUtil.Parse(hex);
            var reEncoded = Iso15118_20DcFixtures.DecodeReEncode(josev);
            Assert.That(reEncoded, Is.EqualTo(josev),
                "our codec must re-encode Josev's -20 DC frame to the identical bytes");
        }

        // ── Signed PnC AuthorizationReq with a Transforms element ──────────────────────────────────────
        //
        // Josev's ~1.3 KB signed AuthorizationReq carries a header <Signature> whose <SignedInfo>/<Reference>
        // includes a <Transforms> element (the "http://www.w3.org/TR/canonical-exi/" EXI-canonicalisation
        // transform). This originally surfaced a real source-generator gap — TransformType's content is
        // <choice minOccurs="0" maxOccurs="unbounded"> (mixed) but was emitted as a *mandatory* single choice
        // with no END-Element alternative, and TransformsType's unbounded list got ListMax=0 — because cbV2G
        // (our vector oracle) never emits Transforms inside a Reference, so the path was never validated. The
        // generator now models the optional/repeatable direct choice as an EE-terminated optional run (matching
        // cbexigen's TransformType/TransformsType grammar), so this signed frame round-trips byte-for-byte like
        // the rest. See docs/interop-runs/2026-07-21-iso20-dc-pnc-notls/.
        [Test]
        public void Josev20_SignedAuthorizationReq_WithTransforms_RoundTripsIdentically()
        {
            var josev = HexUtil.Parse(SignedAuthorizationReqHex);
            var reEncoded = Iso15118_20CommonFixtures.DecodeReEncode(josev);
            Assert.That(reEncoded, Is.EqualTo(josev),
                "our codec must re-encode Josev's signed -20 AuthorizationReq (with a SignedInfo Transforms element) identically");
        }

        internal const string SignedAuthorizationReqHex =
            "80000437eeba1382ed0912885d3fdd2060a25687474703a2f2f7777772e77332e6f72672f54522f63616e6f6e6963616c2d6578692f435687474703a2f2f7777772e77332e6f72672f323030312f30342f786d6c647369672d6d6f72652365636473612d736861323536440c46d2c86204ad0e8e8e0745e5eeeeeee5cee665cdee4ce5ea8a45ec6c2dcdedcd2c6c2d85acaf0d25e90a5a1d1d1c0e8bcbddddddcb9dcccb9bdc99cbcc8c0c0c4bcc0d0bde1b5b195b98c8dcda184c8d4d9082e6cea9b286a7e50dfe9c6aca2ea4d38c36fd314c02a172fdd27a2c829add1c344a07d863a1e7be11d4cd8413043364a86bc8897898f03ac330401d86ec2017881b6fd49443ec435efe81d8e237f2ee360c29260068328664f0d2667cb3537bb4c2ba1205696431042d30c71893c6ab1e7a6a559ec1128969c3a010c2080990c2080826800c0804080808c110c0281820aa192338f4100c08c158c488c080180d54100c3065412d24b515e1d17d0d49517d353d7d4d5508c97d5905312510c43cc034180d54102830194ddda5d18da0c42cc024180d5410184c09552cc448c040182826489a264fc8b19004645809353cc0785c34c8d8c0dcc8c4c4c4d4ccc8c5685c34c8e0c0dcc8c0c4c4d4ccc8c568c130c460c058180d54100c303d552d4d5d24c4c8ccd0d4d8dce4c504c43cc034180d54102830194ddda5d18da0c42cc024180d5410184c09552cc448c040182826489a264fc8b19004645809353cc164c04c181caa192338f408041820aa192338f40c041c0d0800123dc311f4f5a8446b0952d6a7c161b7e62b3524d6b3428b98982aa5bec8842900f22e94824cb008c0ad002638bb14381ba47ace33ef366028ab272c1c6736ae1e8e0740c20734c030180d54744c0407fc1008c000c038180d54743c0407fc10100c080fa0c074180d54743810581050593fc6e338706bffb249e1849251c2cbbcba1844c1b41820ac180414141c04041184c17cc0901820ac180414141cc0061861a1d1d1c1cce8bcbddddddcb995e185b5c1b194b98dbdb4bcc0dc1820ac180414141cc00a18ada1d1d1c1cce8bcbddddddcb995e185b5c1b194b98dbdb4bd25b9d195c9b59591a585d194b50d04b98d95c8c07c180d54748c1060c05a00501392864d0b5e8dee907a36600c3a7e1fef4f99a8c0281820aa192338f4100c080d2400c11808840219c57395234d94026ee664991ede1e7274dcf323fb507717f50b4c59e31cb23c0884031586cb6d7ab2bc84464a30d0563a149a1a6abc68387b167222029fd625a71dac3d810c20809c8c2080866800c0804080808c10cc0281820aa192338f4100c08c158c488c080180d54100c3065412d24b515e1d17d0d49517d353d7d4d5508c57d5905312510c43cc034180d54102830194ddda5d18da0c42cc024180d5410184c09552cc448c040182826489a264fc8b19004645809353cc0785c34c8d8c0dcc8c4c4c4d4ccc8c5685c34ccc0c0dcc8c0c4c4d4ccc8c568c158c488c080180d54100c3065412d24b515e1d17d0d49517d353d7d4d5508c97d5905312510c43cc034180d54102830194ddda5d18da0c42cc024180d5410184c09552cc448c040182826489a264fc8b19004645809353cc164c04c181caa192338f408041820aa192338f40c041c0d0800107311f0106e43af6c6b9a289cdccdc93329a72280f4916738a1e5ad2ba578ab19a1515d442a4994716850e9a7c49df06b2af8b5c845077c41ee326301c90b678e8e0758c2074cc048180d54744c0407fc1020c0180407fc080400c038180d54743c0407fc10100c080718c074180d547438105810501392864d0b5e8dee907a36600c3a7e1fef4f99a8c1b41820ac180414141c04041184c17cc0901820ac180414141cc0061861a1d1d1c1cce8bcbddddddcb995e185b5c1b194b98dbdb4bcc0dc1820ac180414141cc00a18ada1d1d1c1cce8bcbddddddcb995e185b5c1b194b98dbdb4bd25b9d195c9b59591a585d194b50d04b98d95c8c07c180d54748c1060c05a00508c3820e3f31458ebad38723558529347cab5dfb0c0281820aa192338f4100c080d1c00c1100880608d138b56814aa3cc0c53a30dd47fc1cfa0a9c24f7f68799605306bccfcb618088140305f255bb5def11e787189650d15337b6c8dbf7d24236090fcba590e06dcb839c10c208098cc2080822800c0804080808c108c0281820aa192338f4100c08c114c444c03c180d54100c3021353d49bdbdd10d04c43cc034180d54102830194ddda5d18da0c42cc024180d5410184c09552cc448c040182826489a264fc8b19004645809353cc0785c34c8d8c0dcc8c4c4c4d4ccc8c5685c34ccc0c0dcc8c0c4c4d4ccc8c568c158c488c080180d54100c3065412d24b515e1d17d0d49517d353d7d4d5508c57d5905312510c43cc034180d54102830194ddda5d18da0c42cc024180d5410184c09552cc448c040182826489a264fc8b19004645809353cc164c04c181caa192338f408041820aa192338f40c041c0d08001197f3b88d8407beee6cd69024bac9cddbe21b94dcd5e8cf5307d0f4dc5caa769f95cf57eebee7fe3901b84a4af780013b9951d407ea41883a447003f299eeeeb28e0758c2074cc048180d54744c0407fc1020c0180407fc080404c038180d54743c0407fc10100c080418c074180d547438105810508c3820e3f31458ebad38723558529347cab5dfb0c1b41820ac180414141c04041184c17cc0901820ac180414141cc0061861a1d1d1c1cce8bcbddddddcb995e185b5c1b194b98dbdb4bcc0dc1820ac180414141cc00a18ada1d1d1c1cce8bcbddddddcb995e185b5c1b194b98dbdb4bd25b9d195c9b59591a585d194b50d04b98d95c8c07c180d54748c1060c05a005247d02ee0027e9706e4f564f2fcccdbebfb3365dcc0281820aa192338f4100c080d2400c11808840219171f92e29c246ab5cd584c24e32c2759163a28878d06cc8508e86f0f16a95c08840389fbb9fa7566eca1cfefdf9dc6f0bf51db673ff98eadf01a186c67b9ce29a4e080";
    }
}
