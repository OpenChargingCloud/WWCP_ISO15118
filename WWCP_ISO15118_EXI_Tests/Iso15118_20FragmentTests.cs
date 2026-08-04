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
using cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{
    /// <summary>
    /// Differential wire conformance for the ISO 15118-20 CommonMessages EXI <em>fragment</em> codec —
    /// the encoding used to digest a signable element for XMLDSig. Each signable element is encoded as a
    /// standalone fragment and diffed against cbV2G's <c>encode_iso20_exiFragment</c>
    /// (tools/cbv2g-ref/main_iso20.c, <c>Fragment_&lt;name&gt;</c>). Unlike -2 (ECDSA-SHA256/32-byte
    /// digest), -20 uses the stronger ECDSA-SHA512 suite with a 64-byte digest.
    /// </summary>
    [TestFixture]
    public class Iso15118_20FragmentTests
    {
        [Test]
        public void SignedInfo_Fragment_MatchesCbV2G()
        {
            // The XMLDSig SignedInfo subtree ISO 15118-20 puts on the wire: EXI-canonical C14N,
            // ECDSA-SHA512 signature method, a single Reference (no Transforms) over a 64-byte SHA-512
            // digest. Mirrors tools/cbv2g-ref do_fragment("SignedInfo") for -20.
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
            Assert.That(CommonMessagesCodec.EncodeFragment_SignedInfo(content, buf, out int n), Is.True);
            AssertFragment(
                "80 73 22 56 87 47 47 03 a2 f2 f7 77 77 72 e7 73 32 e6 f7 26 72 f5 45 22 f6 36 16 e6 f6 e6 " +
                "96 36 16 c2 d6 57 86 92 f4 35 68 74 74 70 3a 2f 2f 77 77 77 2e 77 33 2e 6f 72 67 2f 32 30 " +
                "30 31 2f 30 34 2f 78 6d 6c 64 73 69 67 2d 6d 6f 72 65 23 65 63 64 73 61 2d 73 68 61 35 31 " +
                "32 44 0c 46 92 88 62 8a 5a 1d 1d 1c 0e 8b cb dd dd dd cb 9d cc cb 9b dc 99 cb cc 8c 0c 0c " +
                "4b cc 0d 0b de 1b 5b 19 5b 98 c8 dc da 18 4d 4c 4c 91 00 04 08 0c 10 14 18 1c 20 24 28 2c " +
                "30 34 38 3c 40 44 48 4c 50 54 58 5c 60 64 68 6c 70 74 78 7c 80 84 88 8c 90 94 98 9c a0 a4 " +
                "a8 ac b0 b4 b8 bc c0 c4 c8 cc d0 d4 d8 dc e0 e4 e8 ec f0 f4 f8 fd 00 63 40",
                buf.AsSpan(0, n).ToArray());
        }

        [Test]
        public void MeteringConfirmationReq_Fragment_MatchesCbV2G()
        {
            var content = new MeteringConfirmationReqType(
                Header: new MessageHeaderType(SessionID: new byte[8], TimeStamp: 1_700_000_000UL, Signature: null),
                SignedMeteringData: new SignedMeteringDataType(
                    Id: "ID1", SessionID: new byte[8],
                    MeterInfo: new MeterInfoType(
                        MeterID: "M1", ChargedEnergyReadingWh: 5000,
                        BPT_DischargedEnergyReadingWh: null, CapacitiveEnergyReadingVARh: null,
                        BPT_InductiveEnergyReadingVARh: null, MeterSignature: null,
                        MeterStatus: null, MeterTimestamp: null),
                    Receipt: null,
                    Dynamic_SMDTControlMode: null,
                    Scheduled_SMDTControlMode: new Scheduled_SMDTControlModeType(SelectedScheduleTupleID: 1)));
            var buf = new byte[512];
            Assert.That(CommonMessagesCodec.EncodeFragment_MeteringConfirmationReq(content, buf, out int n), Is.True);
            AssertFragment(
                "80 3b 80 80 00 00 00 00 00 00 00 01 01 c5 9f 54 0c 40 54 94 43 10 20 00 00 00 00 00 00 " +
                "00 00 01 13 4c 44 41 3b 40 08 46 80",
                buf.AsSpan(0, n).ToArray());
        }

        [Test]
        public void CertificateInstallationReq_Fragment_MatchesCbV2G()
        {
            var content = new CertificateInstallationReqType(
                Header: new MessageHeaderType(SessionID: new byte[8], TimeStamp: 1_700_000_000UL, Signature: null),
                OEMProvisioningCertificateChain: new SignedCertificateChainType(
                    Id: "OEMCERT1", Certificate: new byte[] { 0xAA, 0xBB, 0xCC }, SubCertificates: null),
                ListOfRootCertificateIDs: new ListOfRootCertificateIDsType(
                    // X509SerialNumber 47456, not the 12345 fed into cbV2G: exi_basetypes_encoder_unsigned()
                    // re-chunks the octets exi_basetypes_convert_64_to_signed() already EXI-chunked, so the
                    // wire value isn't the input value — same quirk as Common_CertificateInstallationReq.
                    new[] { new X509IssuerSerialType(X509IssuerName: "Root CA", X509SerialNumber: 47456) }),
                MaximumContractCertificateChains: 3,
                PrioritizedEMAIDs: null);
            var buf = new byte[512];
            Assert.That(CommonMessagesCodec.EncodeFragment_CertificateInstallationReq(content, buf, out int n), Is.True);
            AssertFragment(
                "80 0d 80 80 00 00 00 00 00 00 00 01 01 c5 9f 54 0c 40 a4 f4 54 d4 34 55 25 43 10 0e aa ef " +
                "30 80 4a 93 7b 7b a1 02 1a 08 70 79 01 08 06 63 40",
                buf.AsSpan(0, n).ToArray());
        }

        [Test]
        public void PnC_AReqAuthorizationMode_Fragment_MatchesCbV2G()
        {
            var content = new PnC_AReqAuthorizationModeType(
                Id: "ID1",
                GenChallenge: Enumerable.Range(1, 16).Select(i => (byte)i).ToArray(),
                ContractCertificateChain: new ContractCertificateChainType(
                    Certificate: new byte[] { 0x03, 0x04 },
                    SubCertificates: new SubCertificatesType(new[] { new byte[] { 0x05 } })));
            var buf = new byte[512];
            Assert.That(CommonMessagesCodec.EncodeFragment_PnC_AReqAuthorizationMode(content, buf, out int n), Is.True);
            AssertFragment(
                "80 4b 81 52 51 0c 41 00 10 20 30 40 50 60 70 80 90 a0 b0 c0 d0 e0 f1 00 02 03 04 00 10 52 46 80",
                buf.AsSpan(0, n).ToArray());
        }

        [Test]
        public void SignedInstallationData_Fragment_MatchesCbV2G()
        {
            var content = new SignedInstallationDataType(
                Id: "SID1",
                ContractCertificateChain: new ContractCertificateChainType(
                    Certificate: new byte[] { 0x03, 0x04 },
                    SubCertificates: new SubCertificatesType(new[] { new byte[] { 0x05 } })),
                ECDHCurve: EcdhCurve.SECP521,
                DHPublicKey: new byte[] { 0x06, 0x07 },
                SECP521_EncryptedPrivateKey: new byte[] { 0x08, 0x09 },
                X448_EncryptedPrivateKey: null,
                TPM_EncryptedPrivateKey: null);
            var buf = new byte[512];
            Assert.That(CommonMessagesCodec.EncodeFragment_SignedInstallationData(content, buf, out int n), Is.True);
            AssertFragment(
                "80 73 81 94 d2 51 0c 40 10 18 20 00 82 90 00 40 c0 e0 04 10 12 46 80",
                buf.AsSpan(0, n).ToArray());
        }

        [Test]
        public void AbsolutePriceSchedule_Fragment_MatchesCbV2G()
        {
            var content = new AbsolutePriceScheduleType(
                Id: null, TimeAnchor: 1_700_000_000UL, PriceScheduleID: 1, PriceScheduleDescription: null,
                Currency: "EUR", Language: "EN", PriceAlgorithm: "Alg1",
                MinimumCost: null, MaximumCost: null, TaxRules: null,
                PriceRuleStacks: new PriceRuleStackListType(new[]
                {
                    new PriceRuleStackType(Duration: 3600, new[]
                    {
                        new PriceRuleType(
                            EnergyFee: new RationalNumberType(0, 30), ParkingFee: null,
                            ParkingFeePeriod: null, CarbonDioxideEmission: null,
                            RenewableGenerationPercentage: null,
                            PowerRangeStart: new RationalNumberType(0, 0)),
                    }),
                }),
                OverstayRules: null, AdditionalSelectedServices: null);
            var buf = new byte[512];
            Assert.That(CommonMessagesCodec.EncodeFragment_AbsolutePriceSchedule(content, buf, out int n), Is.True);
            AssertFragment(
                "80 00 28 0e 2c fa a0 60 02 40 a8 aa aa 40 11 15 38 03 20 b6 33 98 98 90 1c 04 00 0f 10 80 00 " +
                "00 b4 68",
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
