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
using cloud.charging.open.protocols.ISO15118_20.WPT.Generated;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{
    /// <summary>
    /// Self-consistency (encode → decode → re-encode) coverage for the two WPT fields that exercise
    /// grammar shapes this repo's generator designed independently, because no working cbV2G reference
    /// exists for them (see <c>Iso15118_20WptFixtures</c> and <c>docs/xsd-inventory-15118-20.md</c>):
    /// <c>WPT_LF_DataPackageList</c> (an optional bounded list mid-run, capped at 2 items — cbV2G's own
    /// generated grammar for it does work, just not past that cap) and <c>LF_SystemSetupData</c> (whose
    /// <c>WPT_LF_TransmitterDataType.TxSpecData</c>, minOccurs=2/maxOccurs=255 followed by an optional
    /// tail, cbV2G's own generated encoder cannot represent at all — confirmed empirically to fail with
    /// EXI_ERROR__UNKNOWN_EVENT_CODE even at the schema's required minimum). These tests can only assert
    /// the C# codec is internally consistent, not that it matches an external reference.
    /// </summary>
    [TestFixture]
    public class Iso15118_20WptSelfConsistencyTests
    {
        private static MessageHeaderType Header() =>
            new(SessionID: new byte[8], TimeStamp: 1_700_000_000UL, Signature: null);

        [Test]
        public void FinePositioningReq_WithDataPackageList_Present_Roundtrips()
        {
            // Exercises the "mid-run optional list" construct with the list non-empty (2 items, its
            // cbV2G-verified cap) AND the following optional element present.
            var message = new WPT_FinePositioningReq(
                Header(), Processing.Finished, WPT_EVResult.EVResultSuccess,
                VendorSpecificDataContainer: new[] { new byte[] { 0x01 }, new byte[] { 0x02 } },
                WPT_LF_DataPackageList: new WPT_LF_DataPackageListType(
                    NumPackages: 1,
                    WPT_LF_DataPackage: new WPT_LF_DataPackageType(
                        PackageIndex: 0,
                        LF_TxData: new WPT_LF_TxDataListType(new WPT_LF_TxDataType(TxIdentifier: 1, new RationalNumberType(0, 100))),
                        LF_RxData: null)));

            var buf1 = new byte[512];
            Assert.That(message.TryEncode(buf1, out int n1), Is.True, "encode failed");

            var decoded = (WPT_FinePositioningReq)WptCodec.DecodeAny(buf1.AsSpan(0, n1), out int consumed);
            Assert.That(consumed, Is.EqualTo(n1), "decoder did not consume all encoded bytes");

            var buf2 = new byte[512];
            Assert.That(decoded.TryEncode(buf2, out int n2), Is.True, "re-encode failed");

            Assert.That(buf2.AsSpan(0, n2).ToArray(), Is.EqualTo(buf1.AsSpan(0, n1).ToArray()),
                "decode∘encode is not the identity on the wire");
        }

        /// <summary>
        /// The counterpart to the "NOT empty" comments above: with the mid-run list empty, cbV2G's
        /// grammar gives the following particle no event code at all. The encoder used to write the
        /// message anyway, quietly leaving <c>LF_SystemSetupData</c> out — which is how two tests in
        /// this very fixture managed to pass while exercising nothing. It now refuses.
        /// </summary>
        [Test]
        public void FinePositioningSetupReq_LFSystemSetupData_WithEmptyVendorContainer_Throws()
        {
            var message = new WPT_FinePositioningSetupReq(
                Header(), Processing.Finished,
                new WPT_FinePositioningMethodListType(new[] { WPT_FinePositioningMethod.Manual }),
                new WPT_PairingMethodListType(new[] { WPT_PairingMethod.LPE }),
                new WPT_AlignmentCheckMethodListType(new[] { WPT_AlignmentCheckMethod.PowerCheck }),
                NaturalOffset: 0,
                VendorSpecificDataContainer: Array.Empty<byte[]>(),
                LF_SystemSetupData: new WPT_LF_SystemSetupDataType(
                    LF_TransmitterSetupData: new WPT_LF_TransmitterDataType(
                        NumberOfTransmitters: 2,
                        SignalFrequency: new RationalNumberType(0, 100),
                        TxSpecData: new[]
                        {
                            new WPT_TxRxSpecDataType(1, new WPT_CoordinateXYZType(0, 0, 0), new WPT_CoordinateXYZType(0, 0, 0)),
                            new WPT_TxRxSpecDataType(2, new WPT_CoordinateXYZType(10, 0, 0), new WPT_CoordinateXYZType(0, 0, 0)),
                        },
                        TxPackageSpecData: null),
                    LF_ReceiverSetupData: null));

            var buf = new byte[512];
            var ex = Assert.Throws<ArgumentException>(() => message.TryEncode(buf, out _));
            Assert.That(ex!.Message, Does.Contain("LF_SystemSetupData"),
                "the exception should name the field that cannot be encoded");
        }

        /// <summary>
        /// The optional tail of the true-self-loop construct, present. <c>PulseSequenceOrder</c> is
        /// itself a minOccurs=2 list, so it needs two entries to satisfy the encoder's bounds check.
        /// </summary>
        private static WPT_TxRxPackageSpecDataType PackageSpec() =>
            new(PulseSequenceOrder: new[]
                {
                    new WPT_TxRxPulseOrderType(IndexNumber: 1, TxRxIdentifier: 1),
                    new WPT_TxRxPulseOrderType(IndexNumber: 2, TxRxIdentifier: 2),
                },
                PulseSeparationTime: 10,
                PulseDuration: 20,
                PackageSeparationTime: 30);

        [Test]
        public void FinePositioningSetupReq_WithLFSystemSetupData_Transmitter_Roundtrips()
        {
            // Exercises the true-self-loop "required repeating + optional tail" construct at its schema
            // minimum (2 TxSpecData items), tail ABSENT. The present-tail case is a separate test —
            // it takes a different production and used to be decoded one bit short.
            var message = new WPT_FinePositioningSetupReq(
                Header(), Processing.Finished,
                new WPT_FinePositioningMethodListType(new[] { WPT_FinePositioningMethod.Manual }),
                new WPT_PairingMethodListType(new[] { WPT_PairingMethod.LPE }),
                new WPT_AlignmentCheckMethodListType(new[] { WPT_AlignmentCheckMethod.PowerCheck }),
                NaturalOffset: 0,
                // NOT empty: cbexigen's mid-run-list grammar leaves everything after the list
                // unreachable until at least one item has been written, so an empty container here
                // would silently drop LF_SystemSetupData and make this test vacuous.
                VendorSpecificDataContainer: new[] { new byte[] { 0xAA } },
                LF_SystemSetupData: new WPT_LF_SystemSetupDataType(
                    LF_TransmitterSetupData: new WPT_LF_TransmitterDataType(
                        NumberOfTransmitters: 2,
                        SignalFrequency: new RationalNumberType(0, 100),
                        TxSpecData: new[]
                        {
                            new WPT_TxRxSpecDataType(1, new WPT_CoordinateXYZType(0, 0, 0), new WPT_CoordinateXYZType(0, 0, 0)),
                            new WPT_TxRxSpecDataType(2, new WPT_CoordinateXYZType(10, 0, 0), new WPT_CoordinateXYZType(0, 0, 0)),
                        },
                        TxPackageSpecData: null),
                    LF_ReceiverSetupData: null));

            var buf1 = new byte[512];
            Assert.That(message.TryEncode(buf1, out int n1), Is.True, "encode failed");

            var decoded = (WPT_FinePositioningSetupReq)WptCodec.DecodeAny(buf1.AsSpan(0, n1), out int consumed);
            Assert.That(consumed, Is.EqualTo(n1), "decoder did not consume all encoded bytes");

            var buf2 = new byte[512];
            Assert.That(decoded.TryEncode(buf2, out int n2), Is.True, "re-encode failed");

            Assert.That(buf2.AsSpan(0, n2).ToArray(), Is.EqualTo(buf1.AsSpan(0, n1).ToArray()),
                "decode∘encode is not the identity on the wire");
        }

        /// <summary>
        /// The same construct with the optional tail PRESENT — the case the sibling test's comment
        /// once claimed to cover while actually passing <c>null</c>.
        /// <para>
        /// A present tail is followed by the element's own EE as a separate 1-bit event, and the
        /// decoder used not to read it: everything after <c>LF_SystemSetupData</c> was then parsed one
        /// bit out of step. Absent the tail the closing EE is folded into the loop's own event code, so
        /// the null case never touched this.
        /// </para>
        /// </summary>
        [Test]
        public void FinePositioningSetupReq_WithLFSystemSetupData_TransmitterAndPackageSpec_Roundtrips()
        {
            var message = new WPT_FinePositioningSetupReq(
                Header(), Processing.Finished,
                new WPT_FinePositioningMethodListType(new[] { WPT_FinePositioningMethod.Manual }),
                new WPT_PairingMethodListType(new[] { WPT_PairingMethod.LPE }),
                new WPT_AlignmentCheckMethodListType(new[] { WPT_AlignmentCheckMethod.PowerCheck }),
                NaturalOffset: 0,
                // NOT empty: cbexigen's mid-run-list grammar leaves everything after the list
                // unreachable until at least one item has been written, so an empty container here
                // would silently drop LF_SystemSetupData and make this test vacuous.
                VendorSpecificDataContainer: new[] { new byte[] { 0xAA } },
                LF_SystemSetupData: new WPT_LF_SystemSetupDataType(
                    LF_TransmitterSetupData: new WPT_LF_TransmitterDataType(
                        NumberOfTransmitters: 2,
                        SignalFrequency: new RationalNumberType(0, 100),
                        TxSpecData: new[]
                        {
                            new WPT_TxRxSpecDataType(1, new WPT_CoordinateXYZType(0, 0, 0), new WPT_CoordinateXYZType(0, 0, 0)),
                            new WPT_TxRxSpecDataType(2, new WPT_CoordinateXYZType(10, 0, 0), new WPT_CoordinateXYZType(0, 0, 0)),
                        },
                        TxPackageSpecData: PackageSpec()),
                    LF_ReceiverSetupData: null));

            var buf1 = new byte[512];
            Assert.That(message.TryEncode(buf1, out int n1), Is.True, "encode failed");

            var decoded = (WPT_FinePositioningSetupReq)WptCodec.DecodeAny(buf1.AsSpan(0, n1), out int consumed);
            Assert.That(consumed, Is.EqualTo(n1), "decoder did not consume all encoded bytes");

            var tx = decoded.LF_SystemSetupData!.LF_TransmitterSetupData!;
            Assert.That(tx.TxSpecData.Count, Is.EqualTo(2), "list items lost");
            Assert.That(tx.TxPackageSpecData, Is.Not.Null, "optional tail lost");

            var buf2 = new byte[512];
            Assert.That(decoded.TryEncode(buf2, out int n2), Is.True, "re-encode failed");

            Assert.That(buf2.AsSpan(0, n2).ToArray(), Is.EqualTo(buf1.AsSpan(0, n1).ToArray()),
                "decode∘encode is not the identity on the wire");
        }
    }
}
