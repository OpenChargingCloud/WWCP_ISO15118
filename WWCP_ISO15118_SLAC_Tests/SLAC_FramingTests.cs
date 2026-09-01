/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

#region Usings

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

using cloud.charging.open.protocols.ISO15118.SLAC.Messages;

#endregion

namespace cloud.charging.open.protocols.ISO15118.SLAC.Tests
{

    /// <summary>
    /// HomePlug AV Management Message Entry framing: Ethernet header, MMV, MMTYPE, body.
    ///
    /// The one thing worth staring at is the endianness split — the EtherType is big-endian
    /// (it is an Ethernet field) while the MMTYPE immediately after it is little-endian (it is
    /// a HomePlug field). Getting that pair the wrong way round produces frames that look
    /// plausible and are decoded by nobody.
    ///
    /// <see cref="ManagementMessageEntry.TryDecode"/> is the entry point for everything that
    /// arrives from the wire, so it must return null rather than throw for every malformed
    /// input — these tests are as much about that contract as about the happy path.
    /// </summary>
    [TestFixture]
    public class SLAC_FramingTests
    {

        #region (private static) Data

        private static readonly MACAddress  evMac    = MACAddress.Parse("AA:BB:CC:DD:EE:01");
        private static readonly MACAddress  evseMac  = MACAddress.Parse("AA:BB:CC:DD:EE:02");

        private static RunId SampleRunId()
            => new ([ 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 ]);

        private static Byte[] Filled(Int32 Length, Byte Seed)
        {

            var bytes = new Byte[Length];

            for (var i = 0; i < Length; i++)
                bytes[i] = (Byte) (Seed + i);

            return bytes;

        }

        #endregion


        #region Encode_ProducesTheEthernetAndManagementHeaders()

        [Test]
        public void Encode_ProducesTheEthernetAndManagementHeaders()
        {

            var frame = ManagementMessageEntry.Encode(
                            MACAddress.Broadcast,
                            evMac,
                            new SLACParamReq(SampleRunId())
                        );

            Assert.Multiple(() => {

                Assert.That(frame.Length,     Is.EqualTo(ManagementMessageEntry.EthernetHeaderLength +
                                                         ManagementMessageEntry.MmHeaderLength + 10));

                Assert.That(frame[0..6],      Is.EqualTo(MACAddress.Broadcast.GetBytes()));
                Assert.That(frame[6..12],     Is.EqualTo(evMac.GetBytes()));

                // EtherType 0x88E1, big-endian — an Ethernet field.
                Assert.That(frame[12],        Is.EqualTo(0x88));
                Assert.That(frame[13],        Is.EqualTo(0xE1));

                Assert.That(frame[14],        Is.EqualTo(SLACConstants.Mmv));

                // MMTYPE 0x6064, little-endian — a HomePlug field, the other way round.
                Assert.That(frame[15],        Is.EqualTo(0x64));
                Assert.That(frame[16],        Is.EqualTo(0x60));

            });

        }

        #endregion

        #region HeaderLengths_MatchTheWireLayout()

        [Test]
        public void HeaderLengths_MatchTheWireLayout()
        {

            Assert.Multiple(() => {
                Assert.That(ManagementMessageEntry.EthernetHeaderLength, Is.EqualTo(14));   // DST + SRC + EtherType
                Assert.That(ManagementMessageEntry.MmHeaderLength,       Is.EqualTo(3));    // MMV + MMTYPE
                Assert.That(SLACConstants.HomePlugAvEtherType,           Is.EqualTo((UInt16) 0x88E1));
                Assert.That(SLACConstants.Mmv,                           Is.EqualTo((Byte) 0x01));
            });

        }

        #endregion

        #region TryDecode_RoundTripsEveryMessageType(...)

        private static IEnumerable<TestCaseData> AllMessages()
        {

            yield return new TestCaseData((ISlacMessage) new SLACParamReq(SampleRunId()))
                             .SetName("TryDecode round-trips CM_SLAC_PARM.REQ");

            yield return new TestCaseData((ISlacMessage) new SLACParmCnf(evMac, 10, 6, 0x01, MACAddress.Zero, SampleRunId()))
                             .SetName("TryDecode round-trips CM_SLAC_PARM.CNF");

            yield return new TestCaseData((ISlacMessage) new StartAttenCharInd(10, 6, 0x01, MACAddress.Zero, SampleRunId()))
                             .SetName("TryDecode round-trips CM_START_ATTEN_CHAR.IND");

            yield return new TestCaseData((ISlacMessage) new MnbcSoundInd(Filled(17, 0x40), 5, SampleRunId(), Filled(16, 0x80)))
                             .SetName("TryDecode round-trips CM_MNBC_SOUND.IND");

            yield return new TestCaseData((ISlacMessage) new AttenCharInd(evMac, SampleRunId(), Filled(17, 0x10), Filled(17, 0x30), 10,
                                                                          Filled(SLACConstants.NumAttenGroups, 0x20)))
                             .SetName("TryDecode round-trips CM_ATTEN_CHAR.IND");

            yield return new TestCaseData((ISlacMessage) new AttenCharRsp(evMac, SampleRunId(), Filled(17, 0x10), Filled(17, 0x30), 0x00))
                             .SetName("TryDecode round-trips CM_ATTEN_CHAR.RSP");

            yield return new TestCaseData((ISlacMessage) new ValidateReq(SignalType.PevS2Toggles, 3, ValidateResult.Ready))
                             .SetName("TryDecode round-trips CM_VALIDATE.REQ");

            yield return new TestCaseData((ISlacMessage) new ValidateCnf(SignalType.PevS2Toggles, 3, ValidateResult.Success))
                             .SetName("TryDecode round-trips CM_VALIDATE.CNF");

            yield return new TestCaseData((ISlacMessage) new SlacMatchReq(Filled(17, 0x10), evMac, Filled(17, 0x30), evseMac, SampleRunId()))
                             .SetName("TryDecode round-trips CM_SLAC_MATCH.REQ");

            yield return new TestCaseData((ISlacMessage) new SlacMatchCnf(Filled(17, 0x10), evMac, Filled(17, 0x30), evseMac, SampleRunId(),
                                                                          Filled(SLACConstants.NidLength, 0x50),
                                                                          Filled(SLACConstants.NmkLength, 0x60)))
                             .SetName("TryDecode round-trips CM_SLAC_MATCH.CNF");

            yield return new TestCaseData((ISlacMessage) SetKeyReq.ForNmk(Filled(SLACConstants.NidLength, 0x50),
                                                                          Filled(SLACConstants.NmkLength, 0x60)))
                             .SetName("TryDecode round-trips CM_SET_KEY.REQ");

            yield return new TestCaseData((ISlacMessage) new SetKeyCnf(0x01, 0x11223344, 0x55667788, 0x04, 0x0102, 0x03, 0x02))
                             .SetName("TryDecode round-trips CM_SET_KEY.CNF");

        }

        /// <summary>
        /// Every message the dispatcher claims to know must survive Encode → TryDecode with its
        /// MMTYPE and its body bytes intact. A message added to ManagementMessageEntry but not
        /// here is exactly the gap this fixture exists to close.
        /// </summary>
        [TestCaseSource(nameof(AllMessages))]
        public void TryDecode_RoundTripsEveryMessageType(ISlacMessage Message)
        {

            var frame    = ManagementMessageEntry.Encode(evseMac, evMac, Message);
            var decoded  = ManagementMessageEntry.TryDecode(frame);

            Assert.That(decoded, Is.Not.Null);

            Assert.Multiple(() => {
                Assert.That(decoded!.Destination,     Is.EqualTo(evseMac));
                Assert.That(decoded.Source,           Is.EqualTo(evMac));
                Assert.That(decoded.Message.MmType,   Is.EqualTo(Message.MmType));
                Assert.That(decoded.Message.Encode(), Is.EqualTo(Message.Encode()));
            });

        }

        #endregion

        #region TryDecode_ReturnsNullOnWrongEtherType()

        [Test]
        public void TryDecode_ReturnsNullOnWrongEtherType()
        {

            var frame  = ManagementMessageEntry.Encode(evseMac, evMac, new SLACParamReq(SampleRunId()));

            frame[12]  = 0x08;   // IPv4 instead of HomePlug AV
            frame[13]  = 0x00;

            Assert.That(ManagementMessageEntry.TryDecode(frame), Is.Null);

        }

        #endregion

        #region TryDecode_ReturnsNullOnWrongManagementMessageVersion()

        [Test]
        public void TryDecode_ReturnsNullOnWrongManagementMessageVersion()
        {

            var frame  = ManagementMessageEntry.Encode(evseMac, evMac, new SLACParamReq(SampleRunId()));

            frame[14]  = 0x00;   // MMV 0x00 is HPAV-1.0, which has a different MM header

            Assert.That(ManagementMessageEntry.TryDecode(frame), Is.Null);

        }

        #endregion

        #region TryDecode_ReturnsNullOnUnknownMmType()

        [Test]
        public void TryDecode_ReturnsNullOnUnknownMmType()
        {

            var frame  = ManagementMessageEntry.Encode(evseMac, evMac, new SLACParamReq(SampleRunId()));

            frame[15]  = 0xFF;   // MMTYPE 0xFFFF — not a SLAC message
            frame[16]  = 0xFF;

            Assert.That(ManagementMessageEntry.TryDecode(frame), Is.Null);

        }

        #endregion

        #region TryDecode_ReturnsNullOnShortFrames(...)

        [TestCase(0)]
        [TestCase(13)]
        [TestCase(16)]
        public void TryDecode_ReturnsNullOnShortFrames(Int32 Length)
        {

            Assert.That(ManagementMessageEntry.TryDecode(new Byte[Length]), Is.Null);

        }

        #endregion

        #region TryDecode_ReturnsNullOnATruncatedBody()

        /// <summary>
        /// A well-formed header followed by half a body. The per-message decoder throws
        /// InvalidDataException, which TryDecode is required to swallow into a null.
        /// </summary>
        [Test]
        public void TryDecode_ReturnsNullOnATruncatedBody()
        {

            var frame = ManagementMessageEntry.Encode(
                            evseMac,
                            evMac,
                            new SlacMatchCnf(Filled(17, 0x10), evMac, Filled(17, 0x30), evseMac, SampleRunId(),
                                             Filled(SLACConstants.NidLength, 0x50),
                                             Filled(SLACConstants.NmkLength, 0x60))
                        );

            Assert.Multiple(() => {
                Assert.That(ManagementMessageEntry.TryDecode(frame),               Is.Not.Null);
                Assert.That(ManagementMessageEntry.TryDecode(frame[..^1]),         Is.Null);
                Assert.That(ManagementMessageEntry.TryDecode(frame[..(17 + 20)]),  Is.Null);
            });

        }

        #endregion

        #region TryDecode_ReturnsNullForEveryTruncationOfEveryMessage(...)

        /// <summary>
        /// How many octets short of a complete body a decoder still accepts by design. Zero
        /// everywhere except CM_SET_KEY.CNF, whose trailing CCoCapability octet is explicitly
        /// optional (<c>body.Length > 15 ? body[15] : 0</c>) so that chips omitting it still parse.
        /// </summary>
        private static Int32 ToleratedShortfall(ISlacMessage Message)

            => Message.MmType == ManagementMessageType.CM_SET_KEY_CNF
                   ? 1
                   : 0;


        /// <summary>
        /// The systematic version: for each message, cut the frame at every length from the end
        /// of the MM header up to the shortest body its decoder accepts. None of those may throw,
        /// and none may decode — a decoder that reads past its own guard shows up here as an
        /// ArgumentOutOfRangeException escaping TryDecode.
        /// </summary>
        [TestCaseSource(nameof(AllMessages))]
        public void TryDecode_ReturnsNullForEveryTruncationOfEveryMessage(ISlacMessage Message)
        {

            var frame    = ManagementMessageEntry.Encode(evseMac, evMac, Message);
            var shortest = frame.Length - ToleratedShortfall(Message);

            Assert.Multiple(() => {

                for (var length = ManagementMessageEntry.EthernetHeaderLength + ManagementMessageEntry.MmHeaderLength;
                     length < shortest;
                     length++)
                {
                    Assert.That(ManagementMessageEntry.TryDecode(frame.AsSpan(0, length)),
                                Is.Null,
                                $"{Message.MmType} decoded from a frame truncated to {length} bytes");
                }

                // …and the shortest accepted length must still decode.
                Assert.That(ManagementMessageEntry.TryDecode(frame.AsSpan(0, shortest)),
                            Is.Not.Null,
                            $"{Message.MmType} did not decode at its shortest accepted length of {shortest} bytes");

            });

        }

        #endregion

        #region Encode_PutsTheBodyDirectlyAfterTheManagementHeader()

        [Test]
        public void Encode_PutsTheBodyDirectlyAfterTheManagementHeader()
        {

            var message  = new SLACParamReq(SampleRunId());
            var frame    = ManagementMessageEntry.Encode(MACAddress.Broadcast, evMac, message);

            Assert.That(frame[17..], Is.EqualTo(message.Encode()));

        }

        #endregion

    }

}
