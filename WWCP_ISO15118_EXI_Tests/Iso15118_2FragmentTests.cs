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
using cloud.charging.open.protocols.ISO15118_2.Generated;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{
    /// <summary>
    /// Differential wire conformance for the EXI <em>fragment</em> codec — the encoding used to digest a
    /// signable element for XMLDSig (ISO 15118-2 §7.10 / Annex J). Each signable element is encoded as a
    /// standalone fragment (EXI header + 8-bit fragment-grammar event code + the element's content) and
    /// diffed against cbV2G's <c>encode_iso2_exiFragment</c> (tools/cbv2g-ref, <c>Fragment_&lt;name&gt;</c>).
    /// The content mirrors the corresponding body fixtures.
    /// </summary>
    [TestFixture]
    public class Iso15118_2FragmentTests
    {
        [Test]
        public void AuthorizationReq_Fragment_MatchesCbV2G()
        {
            var content = new AuthorizationReqType(Id: null,
                GenChallenge: new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });
            var buf = new byte[512];
            Assert.That(Iso2Codec.EncodeFragment_AuthorizationReq(content, buf, out int n), Is.True);
            AssertFragment("80 04 42 00 20 40 60 80 a0 c0 e1 01 21 41 61 81 a1 c1 e2 07 a0", buf.AsSpan(0, n).ToArray());
        }

        [Test]
        public void MeteringReceiptReq_Fragment_MatchesCbV2G()
        {
            var content = new MeteringReceiptReqType(Id: null, SessionID: new byte[8], SAScheduleTupleID: 1,
                new MeterInfoType(MeterID: "M1", MeterReading: null, SigMeterReading: null, MeterStatus: null, TMeter: null));
            var buf = new byte[512];
            Assert.That(Iso2Codec.EncodeFragment_MeteringReceiptReq(content, buf, out int n), Is.True);
            AssertFragment("80 79 41 00 00 00 00 00 00 00 00 00 00 00 89 a6 28 f4", buf.AsSpan(0, n).ToArray());
        }

        [Test]
        public void SalesTariff_Fragment_MatchesCbV2G()
        {
            var content = new SalesTariffType(Id: null, SalesTariffID: 1, SalesTariffDescription: null, NumEPriceLevels: null,
                SalesTariffEntry: new[]
                {
                    new SalesTariffEntryType(
                        TimeInterval: new RelativeTimeIntervalType(Start: 0, Duration: null),
                        EPriceLevel: null,
                        ConsumptionCost: System.Array.Empty<ConsumptionCostType>()),
                });
            var buf = new byte[512];
            Assert.That(Iso2Codec.EncodeFragment_SalesTariff(content, buf, out int n), Is.True);
            AssertFragment("80 ae 40 08 00 0c fa 00", buf.AsSpan(0, n).ToArray());
        }

        [Test]
        public void SignedInfo_Fragment_MatchesCbV2G()
        {
            // The XMLDSig SignedInfo subtree ISO 15118-2 actually puts on the wire: EXI-canonical
            // C14N, ECDSA-SHA256 signature method, and a single Reference (no Transforms) over a
            // 32-byte SHA-256 digest. Mirrors tools/cbv2g-ref do_fragment("SignedInfo").
            var content = new SignedInfoType(
                Id: null,
                CanonicalizationMethod: new CanonicalizationMethodType(
                    Algorithm: "http://www.w3.org/TR/canonical-exi/", ANY: null),
                SignatureMethod: new SignatureMethodType(
                    Algorithm: "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha256",
                    HMACOutputLength: null, ANY: null),
                Reference: new[]
                {
                    new ReferenceType(Id: null, Type: null, URI: "#ID1", Transforms: null,
                        DigestMethod: new DigestMethodType(
                            Algorithm: "http://www.w3.org/2001/04/xmlenc#sha256", ANY: null),
                        DigestValue: Enumerable.Range(1, 32).Select(i => (byte)i).ToArray()),
                });
            var buf = new byte[512];
            Assert.That(Iso2Codec.EncodeFragment_SignedInfo(content, buf, out int n), Is.True);
            AssertFragment(
                "80 d0 44 ad 0e 8e 8e 07 45 e5 ee ee ee e5 ce e6 65 cd ee 4c e5 ea 8a 45 ec 6c 2d cd ed " +
                "cd 2c 6c 2d 85 ac af 0d 25 e8 6a d0 e8 e8 e0 74 5e 5e ee ee ee 5c ee 66 5c de e4 ce 5e " +
                "64 60 60 62 5e 60 68 5e f0 da d8 c8 e6 d2 ce 5a da de e4 ca 46 ca c6 c8 e6 c2 5a e6 d0 " +
                "c2 64 6a 6c 88 18 8d 25 10 c5 14 b4 3a 3a 38 1d 17 97 bb bb bb 97 3b 99 97 37 b9 33 97 " +
                "99 18 18 18 97 98 1a 17 bc 36 b6 32 b7 31 91 b9 b4 30 99 1a 9b 21 00 08 10 18 20 28 30 " +
                "38 40 48 50 58 60 68 70 78 80 88 90 98 a0 a8 b0 b8 c0 c8 d0 d8 e0 e8 f0 f9 00 fa 00",
                buf.AsSpan(0, n).ToArray());
        }

        // ---- helpers ----

        private static void AssertFragment(string expectedHex, byte[]? actual)
        {
            Assert.That(actual, Is.Not.Null, "encode failed");
            var expected = expectedHex.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(x => Convert.ToByte(x, 16)).ToArray();
            if (!actual!.AsSpan().SequenceEqual(expected))
                Assert.Fail($"fragment bytes diverge from cbV2G.\n" +
                            $"  expected ({expected.Length}): {ToHex(expected)}\n" +
                            $"  actual   ({actual.Length}): {ToHex(actual)}");
        }

        private static string ToHex(byte[] b) => string.Join(' ', b.Select(x => x.ToString("x2")));
    }
}
