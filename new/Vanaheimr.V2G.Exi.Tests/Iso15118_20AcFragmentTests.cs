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
using cloud.charging.open.protocols.ISO15118_20.AC.Generated;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{
    /// <summary>
    /// Differential wire conformance for the ISO 15118-20 AC EXI fragment codec. cbV2G's
    /// <c>iso20_ac_exiFragment</c> carries exactly two signable elements
    /// (<c>include/cbv2g/iso_20/iso20_AC_Datatypes.h</c>): <c>SignedInfo</c> and
    /// <c>AC_ChargeParameterDiscoveryRes</c>. Diffed against <c>cbv2g_iso20 Fragment_AC_&lt;name&gt;</c>
    /// (tools/cbv2g-ref/main_iso20.c, <c>do_fragment_ac</c>).
    /// </summary>
    [TestFixture]
    public class Iso15118_20AcFragmentTests
    {
        [Test]
        public void SignedInfo_Fragment_MatchesCbV2G()
        {
            var content = new SignedInfoType(
                Id: null,
                CanonicalizationMethod: new CanonicalizationMethodType(
                    Algorithm: "http://www.w3.org/TR/canonical-exi/", ANY: null),
                SignatureMethod: new SignatureMethodType(
                    Algorithm: "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha512",
                    HMACOutputLength: null, ANY: null),
                Reference: new[]
                {
                    new ReferenceType(Id: null, Type: null, URI: "#ID1", Transforms: null,
                        DigestMethod: new DigestMethodType(
                            Algorithm: "http://www.w3.org/2001/04/xmlenc#sha512", ANY: null),
                        DigestValue: Enumerable.Range(1, 64).Select(i => (byte)i).ToArray()),
                });
            var buf = new byte[512];
            Assert.That(AcCodec.EncodeFragment_SignedInfo(content, buf, out int n), Is.True);
            AssertFragment(
                "80 87 44 ad 0e 8e 8e 07 45 e5 ee ee ee e5 ce e6 65 cd ee 4c e5 ea 8a 45 ec 6c 2d cd ed cd " +
                "2c 6c 2d 85 ac af 0d 25 e8 6a d0 e8 e8 e0 74 5e 5e ee ee ee 5c ee 66 5c de e4 ce 5e 64 60 " +
                "60 62 5e 60 68 5e f0 da d8 c8 e6 d2 ce 5a da de e4 ca 46 ca c6 c8 e6 c2 5a e6 d0 c2 6a 62 " +
                "64 88 18 8d 25 10 c5 14 b4 3a 3a 38 1d 17 97 bb bb bb 97 3b 99 97 37 b9 33 97 99 18 18 18 " +
                "97 98 1a 17 bc 36 b6 32 b7 31 91 b9 b4 30 9a 98 99 22 00 08 10 18 20 28 30 38 40 48 50 58 " +
                "60 68 70 78 80 88 90 98 a0 a8 b0 b8 c0 c8 d0 d8 e0 e8 f0 f9 01 09 11 19 21 29 31 39 41 49 " +
                "51 59 61 69 71 79 81 89 91 99 a1 a9 b1 b9 c1 c9 d1 d9 e1 e9 f1 fa 00 cd 80",
                buf.AsSpan(0, n).ToArray());
        }

        [Test]
        public void ChargeParameterDiscoveryRes_Fragment_MatchesCbV2G()
        {
            var content = new AC_ChargeParameterDiscoveryResType(
                Header: new MessageHeaderType(SessionID: new byte[8], TimeStamp: 1_700_000_000UL, Signature: null),
                ResponseCode: ResponseCode.OK,
                AC_CPDResEnergyTransferMode: new AC_CPDResEnergyTransferModeType(
                    EVSEMaximumChargePower: new RationalNumberType(0, 22000),
                    EVSEMaximumChargePower_L2: null,
                    EVSEMaximumChargePower_L3: null,
                    EVSEMinimumChargePower: new RationalNumberType(0, 100),
                    EVSEMinimumChargePower_L2: null,
                    EVSEMinimumChargePower_L3: null,
                    EVSENominalFrequency: new RationalNumberType(0, 50),
                    MaximumPowerAsymmetry: null,
                    EVSEPowerRampLimitation: null,
                    EVSEPresentActivePower: null,
                    EVSEPresentActivePower_L2: null,
                    EVSEPresentActivePower_L3: null));
            var buf = new byte[512];
            Assert.That(AcCodec.EncodeFragment_AC_ChargeParameterDiscoveryRes(content, buf, out int n), Is.True);
            AssertFragment(
                "80 05 01 00 00 00 00 00 00 00 00 02 03 8b 3e a8 18 80 01 00 1e 15 60 24 40 03 21 10 00 64 " +
                "54 d8",
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
