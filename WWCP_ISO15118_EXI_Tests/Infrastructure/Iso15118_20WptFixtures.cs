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

using System.Linq;

using cloud.charging.open.protocols.ISO15118_20.WPT.Generated;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests.Infrastructure
{
    /// <summary>The fixed ISO 15118-20 WPT messages shared by the cbV2G byte-diff tests
    /// (<c>Vectors/Iso15118_20.WPT.vectors.json</c>, <c>main_iso20.c</c>'s <c>do_wpt</c>). Baseline
    /// coverage only: <c>VendorSpecificDataContainer</c>/<c>ManufacturerSpecificDataContainer</c> empty,
    /// <c>WPT_LF_DataPackageList</c>/<c>LF_SystemSetupData</c> absent — see the vector file's header
    /// note for why those two are covered separately (self-consistency roundtrip only, no cbV2G
    /// reference).</summary>
    public static class Iso15118_20WptFixtures
    {
        private static MessageHeaderType Header() => new(SessionID: new byte[8], TimeStamp: 1_700_000_000UL, Signature: null);
        private static RationalNumberType Rational(sbyte exponent, short value) => new(exponent, value);

        /// <summary>
        /// <paramref name="items"/> coils at distinct positions, so the list is distinguishable in the
        /// stream rather than the same tuple repeated — a run of identical values would still encode,
        /// but a misread offset would be far harder to see in a byte diff.
        /// </summary>
        private static WPT_TxRxSpecDataType[] SpecData(int items)
            => Enumerable.Range(0, items)
                         .Select(i => new WPT_TxRxSpecDataType(
                                          TxRxIdentifier:  (uint) (i + 1),
                                          TxRxPosition:    new WPT_CoordinateXYZType((short) (i * 10), 0, 0),
                                          TxRxOrientation: new WPT_CoordinateXYZType(0, (short) (i * 5), 0)))
                         .ToArray();

        /// <summary><c>PulseSequenceOrder</c>, the third minOccurs="2" particle, one level further down.</summary>
        private static WPT_TxRxPackageSpecDataType PackageSpec(int items)
            => new(PulseSequenceOrder: Enumerable.Range(0, items)
                                                 .Select(i => new WPT_TxRxPulseOrderType(
                                                                  IndexNumber:    (ushort) (i + 1),
                                                                  TxRxIdentifier: (uint)   (i + 1)))
                                                 .ToArray(),
                   PulseSeparationTime:   10,
                   PulseDuration:         20,
                   PackageSeparationTime: 30);

        /// <summary>The transmitter branch: <c>TxSpecData</c> and, under it, <c>PulseSequenceOrder</c> —
        /// two of the three particles in one message, at the same list length.</summary>
        private static WPT_FinePositioningSetupReq LfTransmitterSetup(int items)
            => new(Header(),
                   Processing.Finished,
                   new WPT_FinePositioningMethodListType(new[] { WPT_FinePositioningMethod.Manual }),
                   new WPT_PairingMethodListType(new[] { WPT_PairingMethod.LPE }),
                   new WPT_AlignmentCheckMethodListType(new[] { WPT_AlignmentCheckMethod.PowerCheck }),
                   NaturalOffset: 0,
                   VendorSpecificDataContainer: System.Array.Empty<byte[]>(),
                   LF_SystemSetupData: new WPT_LF_SystemSetupDataType(
                       LF_TransmitterSetupData: new WPT_LF_TransmitterDataType(
                           NumberOfTransmitters: (byte) items,
                           SignalFrequency:      Rational(0, 100),
                           TxSpecData:           SpecData(items),
                           TxPackageSpecData:    PackageSpec(items)),
                       LF_ReceiverSetupData: null));

        /// <summary>The receiver branch, which carries <c>RxSpecData</c> and nothing else repeating.</summary>
        private static WPT_FinePositioningSetupRes LfReceiverSetup(int items)
            => new(Header(), ResponseCode.OK,
                   new WPT_FinePositioningMethodListType(new[] { WPT_FinePositioningMethod.Manual }),
                   new WPT_PairingMethodListType(new[] { WPT_PairingMethod.LPE }),
                   new WPT_AlignmentCheckMethodListType(new[] { WPT_AlignmentCheckMethod.PowerCheck }),
                   NaturalOffset: 0,
                   VendorSpecificDataContainer: System.Array.Empty<byte[]>(),
                   LF_SystemSetupData: new WPT_LF_SystemSetupDataType(
                       LF_TransmitterSetupData: null,
                       LF_ReceiverSetupData: new WPT_LF_ReceiverDataType(
                           NumberOfReceivers: (byte) items,
                           RxSpecData:        SpecData(items))));


        public static bool TryEncode(string vectorName, byte[] dest, out int bytesWritten)
        {
            bytesWritten = 0;
            switch (vectorName)
            {
                case "WPT_FinePositioningSetupReq":
                    return new WPT_FinePositioningSetupReq(Header(),
                            Processing.Finished,
                            new WPT_FinePositioningMethodListType(new[] { WPT_FinePositioningMethod.Manual }),
                            new WPT_PairingMethodListType(new[] { WPT_PairingMethod.LPE }),
                            new WPT_AlignmentCheckMethodListType(new[] { WPT_AlignmentCheckMethod.PowerCheck }),
                            NaturalOffset: 0,
                            VendorSpecificDataContainer: System.Array.Empty<byte[]>(),
                            LF_SystemSetupData: null)
                        .TryEncode(dest, out bytesWritten);

                case "WPT_FinePositioningSetupRes":
                    return new WPT_FinePositioningSetupRes(Header(), ResponseCode.OK,
                            new WPT_FinePositioningMethodListType(new[] { WPT_FinePositioningMethod.Manual }),
                            new WPT_PairingMethodListType(new[] { WPT_PairingMethod.LPE }),
                            new WPT_AlignmentCheckMethodListType(new[] { WPT_AlignmentCheckMethod.PowerCheck }),
                            NaturalOffset: 0,
                            VendorSpecificDataContainer: System.Array.Empty<byte[]>(),
                            LF_SystemSetupData: null)
                        .TryEncode(dest, out bytesWritten);

                case "WPT_FinePositioningReq":
                    return new WPT_FinePositioningReq(Header(), Processing.Finished, WPT_EVResult.EVResultSuccess,
                            VendorSpecificDataContainer: System.Array.Empty<byte[]>(),
                            WPT_LF_DataPackageList: null)
                        .TryEncode(dest, out bytesWritten);

                case "WPT_FinePositioningRes":
                    return new WPT_FinePositioningRes(Header(), ResponseCode.OK, Processing.Finished,
                            VendorSpecificDataContainer: System.Array.Empty<byte[]>(),
                            WPT_LF_DataPackageList: null)
                        .TryEncode(dest, out bytesWritten);

                case "WPT_PairingReq":
                    return new WPT_PairingReq(Header(), Processing.Finished, ObservedIDCode: null,
                            WPT_EVResult.EVResultSuccess, VendorSpecificDataContainer: System.Array.Empty<byte[]>())
                        .TryEncode(dest, out bytesWritten);

                case "WPT_PairingRes":
                    return new WPT_PairingRes(Header(), ResponseCode.OK, Processing.Finished, ObservedIDCode: null,
                            AlternativeSECCList: null, VendorSpecificDataContainer: System.Array.Empty<byte[]>())
                        .TryEncode(dest, out bytesWritten);

                case "WPT_ChargeParameterDiscoveryReq":
                    return new WPT_ChargeParameterDiscoveryReq(Header(),
                            Rational(0, 11000), SDMaxGroundClearence: 300, SDMinGroundClearence: 100,
                            Rational(0, 85), EVPCDeviceLocalControl: false,
                            VendorSpecificDataContainer: System.Array.Empty<byte[]>())
                        .TryEncode(dest, out bytesWritten);

                case "WPT_ChargeParameterDiscoveryRes":
                    return new WPT_ChargeParameterDiscoveryRes(Header(), ResponseCode.OK,
                            WPT_PowerClass.MF_WPT1, Rational(0, 100), Rational(0, 11000),
                            SDMaxGroundClearanceSupport: 300, SDMinGroundClearanceSupport: 100,
                            Rational(0, 1), Rational(0, 200),
                            SDManufacturerSpecificDataContainer: System.Array.Empty<byte[]>())
                        .TryEncode(dest, out bytesWritten);

                case "WPT_AlignmentCheckReq":
                    return new WPT_AlignmentCheckReq(Header(), Processing.Finished, TargetCoilCurrent: null,
                            WPT_EVResult.EVResultSuccess, VendorSpecificDataContainer: System.Array.Empty<byte[]>())
                        .TryEncode(dest, out bytesWritten);

                case "WPT_AlignmentCheckRes":
                    return new WPT_AlignmentCheckRes(Header(), ResponseCode.OK, Processing.Finished,
                            PowerTransmitted: null, SupplyDeviceCurrent: null,
                            VendorSpecificDataContainer: System.Array.Empty<byte[]>())
                        .TryEncode(dest, out bytesWritten);

                case "WPT_ChargeLoopReq":
                    return new WPT_ChargeLoopReq(Header(), DisplayParameters: null, MeterInfoRequested: false,
                            Rational(0, 3700), Rational(0, 3700), WPT_EVPCChargeDiagnostics.EVPCNoIssue,
                            EVPCOperatingFrequency: null, EVPCPowerControlParameter: null,
                            ManufacturerSpecificDataContainer: System.Array.Empty<byte[]>())
                        .TryEncode(dest, out bytesWritten);

                case "WPT_ChargeLoopRes":
                    return new WPT_ChargeLoopRes(Header(), ResponseCode.OK,
                            EVSEStatus: null, MeterInfo: null, Receipt: null,
                            Rational(0, 3700), SDPowerInput: null,
                            Rational(0, 3700), Rational(0, 0), WPT_SPCChargeDiagnostics.SPCNoIssue,
                            SPCOperatingFrequency: null, SPCPowerControlParameter: null,
                            ManufacturerSpecificDataContainer: System.Array.Empty<byte[]>())
                        .TryEncode(dest, out bytesWritten);

                // ---- LF_SystemSetupData: the only place WPT's three minOccurs="2" particles live ----
                //
                // TxSpecData, RxSpecData and PulseSequenceOrder are all `minOccurs="2" maxOccurs="255"`,
                // and all three are reachable only behind LF_SystemSetupData — which the cbV2G corpus
                // deliberately leaves absent, because cbexigen's own encoder for WPT_LF_TransmitterDataType
                // fails at runtime with EXI_ERROR__UNKNOWN_EVENT_CODE. So no reference encoder has ever
                // written these bytes, and until 2026-08-08 nothing but our own decoder had ever read
                // them: they were covered by self-consistency round trips, which cannot catch an encoder
                // and decoder that are wrong in the same way. That is exactly how the forced-occurrence
                // defect survived in the DER curves (cause C of the 2026-08-07 EXIficient run).
                //
                // These vectors close that. Their expected bytes are ours, but EXIficient reads them and
                // writes back the same octets — see the vector file's `verifiedBy`.
                //
                // Two lengths each, and the pair is the point: at exactly minOccurs every occurrence is
                // forced and takes a 1-bit start-element code, while the first occurrence *past* minOccurs
                // is the first real choice and widens to 2 bits. A 2-item list alone would not exercise
                // the transition that was wrong.

                case "WPT_FinePositioningSetupReq_LFTransmitter2":
                    return LfTransmitterSetup(items: 2).TryEncode(dest, out bytesWritten);

                case "WPT_FinePositioningSetupReq_LFTransmitter3":
                    return LfTransmitterSetup(items: 3).TryEncode(dest, out bytesWritten);

                case "WPT_FinePositioningSetupRes_LFReceiver2":
                    return LfReceiverSetup(items: 2).TryEncode(dest, out bytesWritten);

                case "WPT_FinePositioningSetupRes_LFReceiver3":
                    return LfReceiverSetup(items: 3).TryEncode(dest, out bytesWritten);

                default:
                    throw new ArgumentException($"no WPT fixture for vector '{vectorName}'");
            }
        }

        /// <summary>Decodes a WPT wire message and re-encodes it, so callers can assert decode∘encode is
        /// the identity without referencing the generated types themselves.</summary>
        public static byte[] DecodeReEncode(byte[] wireBytes)
        {
            var decoded = WptCodec.DecodeAny(wireBytes, out int consumed);
            if (consumed != wireBytes.Length)
                throw new InvalidDataException($"decoder consumed {consumed} of {wireBytes.Length} bytes");

            var buf = new byte[512];
            if (!TryReEncode(decoded, buf, out int n))
                throw new InvalidDataException("re-encode failed");
            return buf.AsSpan(0, n).ToArray();
        }

        private static bool TryReEncode(object message, byte[] dest, out int bytesWritten)
        {
            bytesWritten = 0;
            return message switch
            {
                WPT_FinePositioningSetupReq m => m.TryEncode(dest, out bytesWritten),
                WPT_FinePositioningSetupRes m => m.TryEncode(dest, out bytesWritten),
                WPT_FinePositioningReq m => m.TryEncode(dest, out bytesWritten),
                WPT_FinePositioningRes m => m.TryEncode(dest, out bytesWritten),
                WPT_PairingReq m => m.TryEncode(dest, out bytesWritten),
                WPT_PairingRes m => m.TryEncode(dest, out bytesWritten),
                WPT_ChargeParameterDiscoveryReq m => m.TryEncode(dest, out bytesWritten),
                WPT_ChargeParameterDiscoveryRes m => m.TryEncode(dest, out bytesWritten),
                WPT_AlignmentCheckReq m => m.TryEncode(dest, out bytesWritten),
                WPT_AlignmentCheckRes m => m.TryEncode(dest, out bytesWritten),
                WPT_ChargeLoopReq m => m.TryEncode(dest, out bytesWritten),
                WPT_ChargeLoopRes m => m.TryEncode(dest, out bytesWritten),
                _ => throw new ArgumentException($"unexpected decoded WPT type {message.GetType()}"),
            };
        }
    }
}
