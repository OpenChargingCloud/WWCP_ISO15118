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
    /// The SLAC message bodies (HomePlug GreenPHY Annex A / ISO 15118-3): encoded length,
    /// field offsets, and the decode guards.
    ///
    /// Every body starts with APPLICATION_TYPE (0x00) and SECURITY_TYPE (0x00) — the two
    /// octets that say "PEV-EVSE matching, no security" — so the payload proper begins at
    /// offset 2 throughout. The lengths asserted here are the ones the encoder produces;
    /// where they disagree with a decoder guard, that is a bug and the test says so.
    /// </summary>
    [TestFixture]
    public class SLAC_MessageTests
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


        #region CM_SLAC_PARM.REQ

        [Test]
        public void SLACParamReq_EncodesTenBytes_WithRunIdAtOffsetTwo()
        {

            var runId  = SampleRunId();
            var bytes  = new SLACParamReq(runId).Encode();

            Assert.Multiple(() => {
                Assert.That(bytes.Length,   Is.EqualTo(10));
                Assert.That(bytes[0],       Is.EqualTo(SLACConstants.ApplicationType_PevEvseMatching));
                Assert.That(bytes[1],       Is.EqualTo(SLACConstants.SecurityType_None));
                Assert.That(bytes[2..10],   Is.EqualTo(runId.ToArray()));
            });

        }

        [Test]
        public void SLACParamReq_RoundTrips()
        {

            var original = new SLACParamReq(SampleRunId());
            var decoded  = SLACParamReq.Decode(original.Encode());

            Assert.Multiple(() => {
                Assert.That(decoded.RunId,            Is.EqualTo(original.RunId));
                Assert.That(decoded.ApplicationType,  Is.EqualTo(original.ApplicationType));
                Assert.That(decoded.SecurityType,     Is.EqualTo(original.SecurityType));
                Assert.That(decoded.MmType,           Is.EqualTo(ManagementMessageType.CM_SLAC_PARM_REQ));
            });

        }

        [Test]
        public void SLACParamReq_Decode_RejectsTruncated()
        {

            Assert.Throws<InvalidDataException>(() => SLACParamReq.Decode(new Byte[9]));

        }

        [Test]
        public void SLACParamReq_Decode_RejectsForeignApplicationOrSecurityType()
        {

            var wrongApplication  = new Byte[10];
            wrongApplication[0]   = 0x01;

            var wrongSecurity     = new Byte[10];
            wrongSecurity[1]      = 0x01;

            Assert.Multiple(() => {
                Assert.Throws<InvalidDataException>(() => SLACParamReq.Decode(wrongApplication));
                Assert.Throws<InvalidDataException>(() => SLACParamReq.Decode(wrongSecurity));
            });

        }

        #endregion

        #region CM_SLAC_PARM.CNF

        [Test]
        public void SLACParmCnf_EncodesTwentyFiveBytes_WithTheDocumentedLayout()
        {

            var runId  = SampleRunId();
            var bytes  = new SLACParmCnf(
                             MSoundTarget:   evMac,
                             NumSounds:      SLACConstants.DefaultNumSounds,
                             TimeOut:        SLACConstants.DefaultTimeOut100ms,
                             RespType:       0x01,
                             ForwardingSta:  MACAddress.Zero,
                             RunId:          runId
                         ).Encode();

            Assert.Multiple(() => {
                Assert.That(bytes.Length,   Is.EqualTo(25));
                Assert.That(bytes[0..6],    Is.EqualTo(evMac.GetBytes()));
                Assert.That(bytes[6],       Is.EqualTo(SLACConstants.DefaultNumSounds));
                Assert.That(bytes[7],       Is.EqualTo(SLACConstants.DefaultTimeOut100ms));
                Assert.That(bytes[8],       Is.EqualTo(0x01));
                Assert.That(bytes[9..15],   Is.EqualTo(MACAddress.Zero.GetBytes()));
                // APPLICATION_TYPE / SECURITY_TYPE sit at 15/16 here, not at 0/1.
                Assert.That(bytes[15],      Is.EqualTo(SLACConstants.ApplicationType_PevEvseMatching));
                Assert.That(bytes[16],      Is.EqualTo(SLACConstants.SecurityType_None));
                Assert.That(bytes[17..25],  Is.EqualTo(runId.ToArray()));
            });

        }

        [Test]
        public void SLACParmCnf_RoundTrips()
        {

            var original = new SLACParmCnf(evMac, 10, 6, 0x01, evseMac, SampleRunId());
            var decoded  = SLACParmCnf.Decode(original.Encode());

            Assert.Multiple(() => {
                Assert.That(decoded.MSoundTarget,   Is.EqualTo(original.MSoundTarget));
                Assert.That(decoded.NumSounds,      Is.EqualTo(original.NumSounds));
                Assert.That(decoded.TimeOut,        Is.EqualTo(original.TimeOut));
                Assert.That(decoded.RespType,       Is.EqualTo(original.RespType));
                Assert.That(decoded.ForwardingSta,  Is.EqualTo(original.ForwardingSta));
                Assert.That(decoded.RunId,          Is.EqualTo(original.RunId));
                Assert.That(decoded.MmType,         Is.EqualTo(ManagementMessageType.CM_SLAC_PARM_CNF));
            });

        }

        [Test]
        public void SLACParmCnf_Decode_RejectsTruncated()
        {

            Assert.Throws<InvalidDataException>(() => SLACParmCnf.Decode(new Byte[24]));

        }

        #endregion

        #region CM_START_ATTEN_CHAR.IND

        [Test]
        public void StartAttenCharInd_EncodesNineteenBytes()
        {

            var runId  = SampleRunId();
            var bytes  = new StartAttenCharInd(10, 6, 0x01, MACAddress.Zero, runId).Encode();

            Assert.Multiple(() => {
                Assert.That(bytes.Length,   Is.EqualTo(19));
                Assert.That(bytes[0],       Is.EqualTo(SLACConstants.ApplicationType_PevEvseMatching));
                Assert.That(bytes[1],       Is.EqualTo(SLACConstants.SecurityType_None));
                Assert.That(bytes[2],       Is.EqualTo(10));
                Assert.That(bytes[3],       Is.EqualTo(6));
                Assert.That(bytes[4],       Is.EqualTo(0x01));
                Assert.That(bytes[5..11],   Is.EqualTo(MACAddress.Zero.GetBytes()));
                Assert.That(bytes[11..19],  Is.EqualTo(runId.ToArray()));
            });

        }

        [Test]
        public void StartAttenCharInd_RoundTrips()
        {

            var original = new StartAttenCharInd(10, 6, 0x01, evseMac, SampleRunId());
            var decoded  = StartAttenCharInd.Decode(original.Encode());

            Assert.Multiple(() => {
                Assert.That(decoded.NumSounds,      Is.EqualTo(original.NumSounds));
                Assert.That(decoded.TimeOut,        Is.EqualTo(original.TimeOut));
                Assert.That(decoded.RespType,       Is.EqualTo(original.RespType));
                Assert.That(decoded.ForwardingSta,  Is.EqualTo(original.ForwardingSta));
                Assert.That(decoded.RunId,          Is.EqualTo(original.RunId));
                Assert.That(decoded.MmType,         Is.EqualTo(ManagementMessageType.CM_START_ATTEN_CHAR_IND));
            });

        }

        [Test]
        public void StartAttenCharInd_Decode_RejectsTruncated()
        {

            Assert.Throws<InvalidDataException>(() => StartAttenCharInd.Decode(new Byte[18]));

        }

        #endregion

        #region CM_MNBC_SOUND.IND

        [Test]
        public void MnbcSoundInd_EncodesFiftyTwoBytes_WithEightReservedOctets()
        {

            var runId     = SampleRunId();
            var senderId  = Filled(17, 0x40);
            var random16  = Filled(16, 0x80);

            var bytes     = new MnbcSoundInd(senderId, 5, runId, random16).Encode();

            Assert.Multiple(() => {
                Assert.That(bytes.Length,   Is.EqualTo(52));
                Assert.That(bytes[2..19],   Is.EqualTo(senderId));
                Assert.That(bytes[19],      Is.EqualTo(5));
                Assert.That(bytes[20..28],  Is.EqualTo(runId.ToArray()));
                Assert.That(bytes[28..36],  Is.EqualTo(new Byte[8]));   // reserved, must stay zero
                Assert.That(bytes[36..52],  Is.EqualTo(random16));
            });

        }

        [Test]
        public void MnbcSoundInd_RoundTrips()
        {

            var original = new MnbcSoundInd(Filled(17, 0x40), 3, SampleRunId(), Filled(16, 0x80));
            var decoded  = MnbcSoundInd.Decode(original.Encode());

            Assert.Multiple(() => {
                Assert.That(decoded.SenderId,  Is.EqualTo(original.SenderId));
                Assert.That(decoded.Cnt,       Is.EqualTo(original.Cnt));
                Assert.That(decoded.RunId,     Is.EqualTo(original.RunId));
                Assert.That(decoded.Random16,  Is.EqualTo(original.Random16));
                Assert.That(decoded.MmType,    Is.EqualTo(ManagementMessageType.CM_MNBC_SOUND_IND));
            });

        }

        [Test]
        public void MnbcSoundInd_Encode_RejectsWronglySizedFields()
        {

            Assert.Multiple(() => {
                Assert.Throws<ArgumentException>(() => new MnbcSoundInd(Filled(16, 0), 1, SampleRunId(), Filled(16, 0)).Encode());
                Assert.Throws<ArgumentException>(() => new MnbcSoundInd(Filled(17, 0), 1, SampleRunId(), Filled(15, 0)).Encode());
            });

        }

        [Test]
        public void MnbcSoundInd_Decode_RejectsTruncated()
        {

            Assert.Throws<InvalidDataException>(() => MnbcSoundInd.Decode(new Byte[51]));

        }

        #endregion

        #region CM_ATTEN_CHAR.IND

        [Test]
        public void AttenCharInd_EncodesFiftyTwoBytesPlusOneOctetPerGroup()
        {

            var runId    = SampleRunId();
            var profile  = Filled(SLACConstants.NumAttenGroups, 0x20);

            var bytes    = new AttenCharInd(evMac, runId, Filled(17, 0x10), Filled(17, 0x30), 10, profile).Encode();

            Assert.Multiple(() => {
                Assert.That(bytes.Length,   Is.EqualTo(52 + SLACConstants.NumAttenGroups));
                Assert.That(bytes[2..8],    Is.EqualTo(evMac.GetBytes()));
                Assert.That(bytes[8..16],   Is.EqualTo(runId.ToArray()));
                Assert.That(bytes[50],      Is.EqualTo(10));                                // NumSounds
                Assert.That(bytes[51],      Is.EqualTo(SLACConstants.NumAttenGroups));      // NumGroups
                Assert.That(bytes[52..],    Is.EqualTo(profile));
            });

        }

        [Test]
        public void AttenCharInd_RoundTrips_WithTheFullFiftyEightGroupProfile()
        {

            var original = new AttenCharInd(
                               evMac,
                               SampleRunId(),
                               Filled(17, 0x10),
                               Filled(17, 0x30),
                               10,
                               Filled(SLACConstants.NumAttenGroups, 0x20)
                           );

            var decoded  = AttenCharInd.Decode(original.Encode());

            Assert.Multiple(() => {
                Assert.That(decoded.SourceAddress,  Is.EqualTo(original.SourceAddress));
                Assert.That(decoded.RunId,          Is.EqualTo(original.RunId));
                Assert.That(decoded.SourceId,       Is.EqualTo(original.SourceId));
                Assert.That(decoded.ResponseId,     Is.EqualTo(original.ResponseId));
                Assert.That(decoded.NumSounds,      Is.EqualTo(original.NumSounds));
                Assert.That(decoded.AttenProfile,   Is.EqualTo(original.AttenProfile));
                Assert.That(decoded.MmType,         Is.EqualTo(ManagementMessageType.CM_ATTEN_CHAR_IND));
            });

        }

        [Test]
        public void AttenCharInd_Decode_RejectsAProfileShorterThanNumGroupsClaims()
        {

            var bytes = new AttenCharInd(
                            evMac, SampleRunId(), Filled(17, 0x10), Filled(17, 0x30), 10,
                            Filled(SLACConstants.NumAttenGroups, 0x20)
                        ).Encode();

            // Keep the header, drop half the profile: NumGroups still claims 58.
            Assert.Throws<InvalidDataException>(() => AttenCharInd.Decode(bytes[..(52 + 20)]));

        }

        [Test]
        public void AttenCharInd_Decode_RejectsTruncated()
        {

            Assert.Throws<InvalidDataException>(() => AttenCharInd.Decode(new Byte[51]));

        }

        [Test]
        public void AttenCharInd_Encode_RejectsWronglySizedIdentifiers()
        {

            Assert.Multiple(() => {
                Assert.Throws<ArgumentException>(() => new AttenCharInd(evMac, SampleRunId(), Filled(16, 0), Filled(17, 0), 10, Filled(58, 0)).Encode());
                Assert.Throws<ArgumentException>(() => new AttenCharInd(evMac, SampleRunId(), Filled(17, 0), Filled(18, 0), 10, Filled(58, 0)).Encode());
            });

        }

        #endregion

        #region CM_ATTEN_CHAR.RSP

        [Test]
        public void AttenCharRsp_EncodesFiftyOneBytes()
        {

            var runId  = SampleRunId();
            var bytes  = new AttenCharRsp(evMac, runId, Filled(17, 0x10), Filled(17, 0x30), 0x00).Encode();

            Assert.Multiple(() => {
                Assert.That(bytes.Length,   Is.EqualTo(51));
                Assert.That(bytes[2..8],    Is.EqualTo(evMac.GetBytes()));
                Assert.That(bytes[8..16],   Is.EqualTo(runId.ToArray()));
                Assert.That(bytes[50],      Is.EqualTo(0x00));                              // Result
            });

        }

        [Test]
        public void AttenCharRsp_RoundTrips()
        {

            var original = new AttenCharRsp(evMac, SampleRunId(), Filled(17, 0x10), Filled(17, 0x30), 0x00);
            var decoded  = AttenCharRsp.Decode(original.Encode());

            Assert.Multiple(() => {
                Assert.That(decoded.SourceAddress,  Is.EqualTo(original.SourceAddress));
                Assert.That(decoded.RunId,          Is.EqualTo(original.RunId));
                Assert.That(decoded.SourceId,       Is.EqualTo(original.SourceId));
                Assert.That(decoded.ResponseId,     Is.EqualTo(original.ResponseId));
                Assert.That(decoded.Result,         Is.EqualTo(original.Result));
                Assert.That(decoded.MmType,         Is.EqualTo(ManagementMessageType.CM_ATTEN_CHAR_RSP));
            });

        }

        [Test]
        public void AttenCharRsp_Decode_RejectsTruncated()
        {

            Assert.Throws<InvalidDataException>(() => AttenCharRsp.Decode(new Byte[50]));

        }

        #endregion

        #region CM_VALIDATE.REQ / .CNF

        [Test]
        public void ValidateReq_EncodesFiveBytes_AndRoundTrips()
        {

            var original = new ValidateReq(SignalType.PevS2Toggles, 3, ValidateResult.Ready);
            var bytes    = original.Encode();
            var decoded  = ValidateReq.Decode(bytes);

            Assert.Multiple(() => {
                Assert.That(bytes.Length,       Is.EqualTo(5));
                Assert.That(bytes[2],           Is.EqualTo((Byte) SignalType.PevS2Toggles));
                Assert.That(bytes[3],           Is.EqualTo(3));
                Assert.That(bytes[4],           Is.EqualTo((Byte) ValidateResult.Ready));
                Assert.That(decoded.SignalType, Is.EqualTo(original.SignalType));
                Assert.That(decoded.ToggleNum,  Is.EqualTo(original.ToggleNum));
                Assert.That(decoded.Result,     Is.EqualTo(original.Result));
                Assert.That(decoded.MmType,     Is.EqualTo(ManagementMessageType.CM_VALIDATE_REQ));
            });

        }

        [Test]
        public void ValidateCnf_EncodesFiveBytes_AndRoundTrips()
        {

            var original = new ValidateCnf(SignalType.PevS2Toggles, 7, ValidateResult.Success);
            var decoded  = ValidateCnf.Decode(original.Encode());

            Assert.Multiple(() => {
                Assert.That(original.Encode().Length, Is.EqualTo(5));
                Assert.That(decoded.SignalType,       Is.EqualTo(original.SignalType));
                Assert.That(decoded.ToggleNum,        Is.EqualTo(original.ToggleNum));
                Assert.That(decoded.Result,           Is.EqualTo(original.Result));
                Assert.That(decoded.MmType,           Is.EqualTo(ManagementMessageType.CM_VALIDATE_CNF));
            });

        }

        [Test]
        public void Validate_Decode_RejectsTruncated()
        {

            Assert.Multiple(() => {
                Assert.Throws<InvalidDataException>(() => ValidateReq.Decode(new Byte[4]));
                Assert.Throws<InvalidDataException>(() => ValidateCnf.Decode(new Byte[4]));
            });

        }

        [TestCase(ValidateResult.Ready,    (Byte) 0x00)]
        [TestCase(ValidateResult.Success,  (Byte) 0x01)]
        [TestCase(ValidateResult.Failure,  (Byte) 0x02)]
        [TestCase(ValidateResult.NotReady, (Byte) 0x03)]
        public void ValidateResult_HasTheWireValuesFromHPGP(ValidateResult Result, Byte Expected)
        {

            Assert.That((Byte) Result, Is.EqualTo(Expected));

        }

        #endregion

        #region CM_SLAC_MATCH.REQ

        [Test]
        public void SlacMatchReq_EncodesSixtySixBytes_WithLittleEndianMvfLength()
        {

            var runId  = SampleRunId();
            var bytes  = new SlacMatchReq(Filled(17, 0x10), evMac, Filled(17, 0x30), evseMac, runId).Encode();

            Assert.Multiple(() => {

                Assert.That(bytes.Length,   Is.EqualTo(66));

                // MVFLength = 0x003E, little-endian on the wire
                Assert.That(bytes[2],       Is.EqualTo(0x3E));
                Assert.That(bytes[3],       Is.EqualTo(0x00));
                Assert.That(SlacMatchReq.MatchVarFieldLength, Is.EqualTo((UInt16) 0x003E));

                Assert.That(bytes[21..27],  Is.EqualTo(evMac.GetBytes()));
                Assert.That(bytes[44..50],  Is.EqualTo(evseMac.GetBytes()));
                Assert.That(bytes[50..58],  Is.EqualTo(runId.ToArray()));
                Assert.That(bytes[58..66],  Is.EqualTo(new Byte[8]));   // reserved, must stay zero

            });

        }

        [Test]
        public void SlacMatchReq_RoundTrips()
        {

            var original = new SlacMatchReq(Filled(17, 0x10), evMac, Filled(17, 0x30), evseMac, SampleRunId());
            var decoded  = SlacMatchReq.Decode(original.Encode());

            Assert.Multiple(() => {
                Assert.That(decoded.PevId,    Is.EqualTo(original.PevId));
                Assert.That(decoded.PevMac,   Is.EqualTo(original.PevMac));
                Assert.That(decoded.EvseId,   Is.EqualTo(original.EvseId));
                Assert.That(decoded.EvseMac,  Is.EqualTo(original.EvseMac));
                Assert.That(decoded.RunId,    Is.EqualTo(original.RunId));
                Assert.That(decoded.MmType,   Is.EqualTo(ManagementMessageType.CM_SLAC_MATCH_REQ));
            });

        }

        [Test]
        public void SlacMatchReq_Decode_RejectsTruncated()
        {

            Assert.Throws<InvalidDataException>(() => SlacMatchReq.Decode(new Byte[65]));

        }

        #endregion

        #region CM_SLAC_MATCH.CNF

        [Test]
        public void SlacMatchCnf_EncodesNinetyBytes_CarryingNidAndNmk()
        {

            var runId  = SampleRunId();
            var nid    = Filled(SLACConstants.NidLength, 0x50);
            var nmk    = Filled(SLACConstants.NmkLength, 0x60);

            var bytes  = new SlacMatchCnf(Filled(17, 0x10), evMac, Filled(17, 0x30), evseMac, runId, nid, nmk).Encode();

            Assert.Multiple(() => {

                Assert.That(bytes.Length,   Is.EqualTo(90));

                // MVFLength = 0x0056, little-endian
                Assert.That(bytes[2],       Is.EqualTo(0x56));
                Assert.That(bytes[3],       Is.EqualTo(0x00));
                Assert.That(SlacMatchCnf.MatchVarFieldLength, Is.EqualTo((UInt16) 0x0056));

                Assert.That(bytes[50..58],  Is.EqualTo(runId.ToArray()));
                Assert.That(bytes[58..66],  Is.EqualTo(new Byte[8]));   // reserved
                Assert.That(bytes[66..73],  Is.EqualTo(nid));
                Assert.That(bytes[73],      Is.EqualTo(0x00));          // reserved
                Assert.That(bytes[74..90],  Is.EqualTo(nmk));

            });

        }

        [Test]
        public void SlacMatchCnf_RoundTrips()
        {

            var original = new SlacMatchCnf(
                               Filled(17, 0x10), evMac, Filled(17, 0x30), evseMac, SampleRunId(),
                               Filled(SLACConstants.NidLength, 0x50),
                               Filled(SLACConstants.NmkLength, 0x60)
                           );

            var decoded  = SlacMatchCnf.Decode(original.Encode());

            Assert.Multiple(() => {
                Assert.That(decoded.PevId,    Is.EqualTo(original.PevId));
                Assert.That(decoded.PevMac,   Is.EqualTo(original.PevMac));
                Assert.That(decoded.EvseId,   Is.EqualTo(original.EvseId));
                Assert.That(decoded.EvseMac,  Is.EqualTo(original.EvseMac));
                Assert.That(decoded.RunId,    Is.EqualTo(original.RunId));
                Assert.That(decoded.Nid,      Is.EqualTo(original.Nid));
                Assert.That(decoded.Nmk,      Is.EqualTo(original.Nmk));
                Assert.That(decoded.MmType,   Is.EqualTo(ManagementMessageType.CM_SLAC_MATCH_CNF));
            });

        }

        [Test]
        public void SlacMatchCnf_Encode_RejectsWronglySizedNidOrNmk()
        {

            Assert.Multiple(() => {
                Assert.Throws<ArgumentException>(() => new SlacMatchCnf(Filled(17, 0), evMac, Filled(17, 0), evseMac, SampleRunId(), Filled(6,  0), Filled(16, 0)).Encode());
                Assert.Throws<ArgumentException>(() => new SlacMatchCnf(Filled(17, 0), evMac, Filled(17, 0), evseMac, SampleRunId(), Filled(7,  0), Filled(15, 0)).Encode());
            });

        }

        [Test]
        public void SlacMatchCnf_Decode_RejectsTruncated()
        {

            Assert.Throws<InvalidDataException>(() => SlacMatchCnf.Decode(new Byte[89]));

        }

        #endregion

        #region CM_SET_KEY.REQ / .CNF

        [Test]
        public void SetKeyReq_EncodesFortyBytes_WithLittleEndianNonces()
        {

            var nid    = Filled(SLACConstants.NidLength, 0x50);
            var nmk    = Filled(SLACConstants.NmkLength, 0x60);

            var bytes  = new SetKeyReq(0x01, 0x11223344, 0, 0x04, 0x0000, 0x00, 0x00, nid, 0x01, nmk).Encode();

            Assert.Multiple(() => {
                Assert.That(bytes.Length,   Is.EqualTo(40));
                Assert.That(bytes[2],       Is.EqualTo(0x01));          // KeyType = NMK
                Assert.That(bytes[3],       Is.EqualTo(0x44));          // MyNonce, little-endian
                Assert.That(bytes[4],       Is.EqualTo(0x33));
                Assert.That(bytes[5],       Is.EqualTo(0x22));
                Assert.That(bytes[6],       Is.EqualTo(0x11));
                Assert.That(bytes[11],      Is.EqualTo(0x04));          // PID = HLE Protocol
                Assert.That(bytes[16..23],  Is.EqualTo(nid));
                Assert.That(bytes[23],      Is.EqualTo(0x01));          // NewEKS = NMK
                Assert.That(bytes[24..40],  Is.EqualTo(nmk));
            });

        }

        [Test]
        public void SetKeyReq_RoundTrips()
        {

            var original = new SetKeyReq(
                               0x01, 0x11223344, 0x55667788, 0x04, 0x0102, 0x03, 0x02,
                               Filled(SLACConstants.NidLength, 0x50), 0x01,
                               Filled(SLACConstants.NmkLength, 0x60)
                           );

            var decoded  = SetKeyReq.Decode(original.Encode());

            Assert.Multiple(() => {
                Assert.That(decoded.KeyType,        Is.EqualTo(original.KeyType));
                Assert.That(decoded.MyNonce,        Is.EqualTo(original.MyNonce));
                Assert.That(decoded.YourNonce,      Is.EqualTo(original.YourNonce));
                Assert.That(decoded.Pid,            Is.EqualTo(original.Pid));
                Assert.That(decoded.Prn,            Is.EqualTo(original.Prn));
                Assert.That(decoded.Pmn,            Is.EqualTo(original.Pmn));
                Assert.That(decoded.CCoCapability,  Is.EqualTo(original.CCoCapability));
                Assert.That(decoded.Nid,            Is.EqualTo(original.Nid));
                Assert.That(decoded.NewEks,         Is.EqualTo(original.NewEks));
                Assert.That(decoded.NewKey,         Is.EqualTo(original.NewKey));
                Assert.That(decoded.MmType,         Is.EqualTo(ManagementMessageType.CM_SET_KEY_REQ));
            });

        }

        /// <summary>
        /// The encoder produces 40 bytes and the decoder reads NewKey from 24..39, so a body
        /// one octet short must be refused as truncated — the same way every other decoder here
        /// refuses one. Guarding at 39 instead of 40 lets a 39-byte body past the check and then
        /// throws ArgumentOutOfRangeException out of the slice, which
        /// <see cref="ManagementMessageEntry.TryDecode"/> does not catch.
        /// </summary>
        [Test]
        public void SetKeyReq_Decode_RejectsABodyOneOctetShort()
        {

            Assert.Throws<InvalidDataException>(() => SetKeyReq.Decode(new Byte[39]));

        }

        [Test]
        public void SetKeyReq_ForNmk_BuildsATypicalFreshJoin()
        {

            var nid  = Filled(SLACConstants.NidLength, 0x50);
            var nmk  = Filled(SLACConstants.NmkLength, 0x60);

            var req  = SetKeyReq.ForNmk(nid, nmk);

            Assert.Multiple(() => {
                Assert.That(req.KeyType,    Is.EqualTo((Byte) 0x01));   // NMK
                Assert.That(req.YourNonce,  Is.EqualTo(0u));            // first set
                Assert.That(req.Pid,        Is.EqualTo((Byte) 0x04));   // HLE Protocol
                Assert.That(req.NewEks,     Is.EqualTo((Byte) 0x01));
                Assert.That(req.Nid,        Is.EqualTo(nid));
                Assert.That(req.NewKey,     Is.EqualTo(nmk));
            });

        }

        [Test]
        public void SetKeyReq_ForNmk_CopiesItsInputs()
        {

            var nid  = Filled(SLACConstants.NidLength, 0x50);
            var nmk  = Filled(SLACConstants.NmkLength, 0x60);
            var req  = SetKeyReq.ForNmk(nid, nmk);

            nid[0] = 0xFF;
            nmk[0] = 0xFF;

            Assert.Multiple(() => {
                Assert.That(req.Nid[0],    Is.EqualTo(0x50));
                Assert.That(req.NewKey[0], Is.EqualTo(0x60));
            });

        }

        [Test]
        public void SetKeyReq_ForNmk_RejectsWronglySizedKeyMaterial()
        {

            Assert.Multiple(() => {
                Assert.Throws<ArgumentException>(() => SetKeyReq.ForNmk(Filled(6, 0), Filled(16, 0)));
                Assert.Throws<ArgumentException>(() => SetKeyReq.ForNmk(Filled(7, 0), Filled(15, 0)));
            });

        }

        [Test]
        public void SetKeyCnf_RoundTrips_AndReportsSuccess()
        {

            var original = new SetKeyCnf(0x01, 0x11223344, 0x55667788, 0x04, 0x0102, 0x03, 0x02);
            var decoded  = SetKeyCnf.Decode(original.Encode());

            Assert.Multiple(() => {
                Assert.That(original.Encode().Length, Is.EqualTo(16));
                Assert.That(decoded.Result,           Is.EqualTo(original.Result));
                Assert.That(decoded.MyNonce,          Is.EqualTo(original.MyNonce));
                Assert.That(decoded.YourNonce,        Is.EqualTo(original.YourNonce));
                Assert.That(decoded.Pid,              Is.EqualTo(original.Pid));
                Assert.That(decoded.Prn,              Is.EqualTo(original.Prn));
                Assert.That(decoded.Pmn,              Is.EqualTo(original.Pmn));
                Assert.That(decoded.CCoCapability,    Is.EqualTo(original.CCoCapability));
                Assert.That(decoded.IsSuccess,        Is.True);
                Assert.That(decoded.MmType,           Is.EqualTo(ManagementMessageType.CM_SET_KEY_CNF));
            });

        }

        [Test]
        public void SetKeyCnf_IsSuccess_OnlyForResultOne()
        {

            Assert.Multiple(() => {
                Assert.That(new SetKeyCnf(0x00, 0, 0, 0, 0, 0, 0).IsSuccess, Is.False);
                Assert.That(new SetKeyCnf(0x01, 0, 0, 0, 0, 0, 0).IsSuccess, Is.True);
                Assert.That(new SetKeyCnf(0x02, 0, 0, 0, 0, 0, 0).IsSuccess, Is.False);
            });

        }

        /// <summary>
        /// CM_SET_KEY.CNF is deliberately lenient about its last octet: chips that omit
        /// CCoCapability still parse, with the field defaulting to zero.
        /// </summary>
        [Test]
        public void SetKeyCnf_Decode_ToleratesAMissingCCoCapabilityOctet()
        {

            var decoded = SetKeyCnf.Decode(new Byte[15]);

            Assert.That(decoded.CCoCapability, Is.EqualTo((Byte) 0));

        }

        [Test]
        public void SetKeyCnf_Decode_RejectsTruncated()
        {

            Assert.Throws<InvalidDataException>(() => SetKeyCnf.Decode(new Byte[14]));

        }

        #endregion

        #region ManagementMessageType

        /// <summary>
        /// The lower two bits of an MMTYPE encode the subtype: 00=REQ, 01=CNF, 10=IND, 11=RSP.
        /// Getting one of these wrong turns a request into a confirmation on the wire.
        /// </summary>
        [TestCase(ManagementMessageType.CM_SLAC_PARM_REQ,        (UInt16) 0x6064, 0)]
        [TestCase(ManagementMessageType.CM_SLAC_PARM_CNF,        (UInt16) 0x6065, 1)]
        [TestCase(ManagementMessageType.CM_START_ATTEN_CHAR_IND, (UInt16) 0x606A, 2)]
        [TestCase(ManagementMessageType.CM_ATTEN_CHAR_IND,       (UInt16) 0x606E, 2)]
        [TestCase(ManagementMessageType.CM_ATTEN_CHAR_RSP,       (UInt16) 0x606F, 3)]
        [TestCase(ManagementMessageType.CM_MNBC_SOUND_IND,       (UInt16) 0x6076, 2)]
        [TestCase(ManagementMessageType.CM_VALIDATE_REQ,         (UInt16) 0x6078, 0)]
        [TestCase(ManagementMessageType.CM_VALIDATE_CNF,         (UInt16) 0x6079, 1)]
        [TestCase(ManagementMessageType.CM_SLAC_MATCH_REQ,       (UInt16) 0x607C, 0)]
        [TestCase(ManagementMessageType.CM_SLAC_MATCH_CNF,       (UInt16) 0x607D, 1)]
        [TestCase(ManagementMessageType.CM_SET_KEY_REQ,          (UInt16) 0x6008, 0)]
        [TestCase(ManagementMessageType.CM_SET_KEY_CNF,          (UInt16) 0x6009, 1)]
        public void ManagementMessageTypes_HaveTheirHomePlugValuesAndSubtypeBits(ManagementMessageType MmType,
                                                                                 UInt16                Expected,
                                                                                 Int32                 ExpectedSubtype)
        {

            Assert.Multiple(() => {
                Assert.That((UInt16) MmType,       Is.EqualTo(Expected));
                Assert.That((UInt16) MmType & 0x3, Is.EqualTo(ExpectedSubtype));
            });

        }

        #endregion

    }

}
