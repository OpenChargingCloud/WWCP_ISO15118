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

#endregion

namespace cloud.charging.open.protocols.ISO15118.V2GTP.Tests
{

    /// <summary>
    /// The 8-byte V2GTP header (ISO 15118-2 §7.8.2, ISO 15118-20 §A.1): the version
    /// complement check, big-endian field order, and the strict/lenient parse split
    /// that lets pentest tooling read frames the strict parser rejects.
    /// </summary>
    [TestFixture]
    public class V2GTP_HeaderTests
    {

        #region Standard_RoundTrips()

        [Test]
        public void Standard_RoundTrips()
        {

            var header  = V2GTP_Header.Standard(V2GTP_PayloadType.SdpRequest, 2);
            var buffer  = new Byte[V2GTP_Header.Size];

            header.WriteTo(buffer);

            // 01 FE 90 00 00 00 00 02
            Assert.That(buffer, Is.EqualTo(new Byte[] { 0x01, 0xFE, 0x90, 0x00, 0x00, 0x00, 0x00, 0x02 }));

            var parsed = V2GTP_Header.Parse(buffer);

            Assert.Multiple(() => {
                Assert.That(parsed,                Is.EqualTo(header));
                Assert.That(parsed.IsVersionValid, Is.True);
            });

        }

        #endregion

        #region SdpResponse_PayloadLength_IsTwenty_OnTheWire()

        [Test]
        public void SdpResponse_PayloadLength_IsTwenty_OnTheWire()
        {

            var buffer = V2GTP_Header.Standard(V2GTP_PayloadType.SdpResponse, 20).ToArray();

            // 01 FE 90 01 00 00 00 14
            Assert.Multiple(() => {
                Assert.That(buffer[0], Is.EqualTo(0x01));
                Assert.That(buffer[1], Is.EqualTo(0xFE));
                Assert.That(buffer[2], Is.EqualTo(0x90));
                Assert.That(buffer[3], Is.EqualTo(0x01));
                Assert.That(buffer[4], Is.EqualTo(0x00));
                Assert.That(buffer[5], Is.EqualTo(0x00));
                Assert.That(buffer[6], Is.EqualTo(0x00));
                Assert.That(buffer[7], Is.EqualTo(0x14));
            });

        }

        #endregion

        #region PayloadLength_IsBigEndian_AcrossAllFourOctets()

        [Test]
        public void PayloadLength_IsBigEndian_AcrossAllFourOctets()
        {

            var buffer = V2GTP_Header.Standard(V2GTP_PayloadType.ExiMainstream, 0x01020304).ToArray();

            Assert.Multiple(() => {
                Assert.That(buffer[4], Is.EqualTo(0x01));
                Assert.That(buffer[5], Is.EqualTo(0x02));
                Assert.That(buffer[6], Is.EqualTo(0x03));
                Assert.That(buffer[7], Is.EqualTo(0x04));
            });

        }

        #endregion

        #region WriteTo_RejectsUndersizedDestination()

        [Test]
        public void WriteTo_RejectsUndersizedDestination()
        {

            var header = V2GTP_Header.Standard(V2GTP_PayloadType.SdpRequest, 2);

            Assert.Throws<ArgumentException>(() => header.WriteTo(new Byte[V2GTP_Header.Size - 1]));

        }

        #endregion

        #region Parse_RejectsBadInverseVersion()

        [Test]
        public void Parse_RejectsBadInverseVersion()
        {

            var bad = new Byte[] { 0x01, 0x00, 0x90, 0x00, 0x00, 0x00, 0x00, 0x02 };

            Assert.Throws<V2GTP_ProtocolVersionException>(() => V2GTP_Header.Parse(bad));

        }

        #endregion

        #region ParseRaw_AcceptsBadInverseVersion_ForPentest()

        [Test]
        public void ParseRaw_AcceptsBadInverseVersion_ForPentest()
        {

            var bad     = new Byte[] { 0x01, 0x00, 0x90, 0x00, 0x00, 0x00, 0x00, 0x02 };
            var header  = V2GTP_Header.ParseRaw(bad);

            Assert.Multiple(() => {
                Assert.That(header.IsVersionValid, Is.False);
                Assert.That(header.PayloadType,    Is.EqualTo(V2GTP_PayloadType.SdpRequest));
                Assert.That(header.PayloadLength,  Is.EqualTo(2u));
            });

        }

        #endregion

        #region Parse_ThrowsOnTruncatedBuffer()

        [Test]
        public void Parse_ThrowsOnTruncatedBuffer()
        {

            Assert.Throws<V2GTP_TruncatedException>(() => V2GTP_Header.Parse(new Byte[] { 0x01, 0xFE, 0x90 }));

        }

        #endregion

        #region TryParseRaw_ReturnsFalseOnTruncatedBuffer()

        [Test]
        public void TryParseRaw_ReturnsFalseOnTruncatedBuffer()
        {

            Assert.Multiple(() => {
                Assert.That(V2GTP_Header.TryParseRaw(new Byte[V2GTP_Header.Size - 1], out _), Is.False);
                Assert.That(V2GTP_Header.TryParseRaw(new Byte[V2GTP_Header.Size],     out _), Is.True);
            });

        }

        #endregion

        #region TryParseRaw_DoesNotEnforceVersionValidity()

        [Test]
        public void TryParseRaw_DoesNotEnforceVersionValidity()
        {

            var bad = new Byte[] { 0x01, 0x00, 0x90, 0x00, 0x00, 0x00, 0x00, 0x02 };

            Assert.Multiple(() => {
                Assert.That(V2GTP_Header.TryParseRaw(bad, out var header), Is.True);
                Assert.That(header.IsVersionValid,                         Is.False);
            });

        }

        #endregion

        #region PayloadTypes_HaveExpectedNumericValues(...)

        // ISO 15118-20 §A.1. Note that 0x8002 is the -20 *mainstream* stream and AC only starts
        // at 0x8003 — an earlier revision of these tests mapped 0x8002 to AC, which matches
        // neither V2GTP_PayloadType nor the standard.
        [TestCase((UInt16) 0x8001, V2GTP_PayloadType.ExiMainstream)]
        [TestCase((UInt16) 0x8002, V2GTP_PayloadType.ExiIso20Mainstream)]
        [TestCase((UInt16) 0x8003, V2GTP_PayloadType.ExiAC)]
        [TestCase((UInt16) 0x8004, V2GTP_PayloadType.ExiDC)]
        [TestCase((UInt16) 0x8005, V2GTP_PayloadType.ExiACDP)]
        [TestCase((UInt16) 0x8006, V2GTP_PayloadType.ExiWPT)]
        [TestCase((UInt16) 0x9000, V2GTP_PayloadType.SdpRequest)]
        [TestCase((UInt16) 0x9001, V2GTP_PayloadType.SdpResponse)]
        [TestCase((UInt16) 0x9002, V2GTP_PayloadType.SdpRequestWireless)]
        [TestCase((UInt16) 0x9003, V2GTP_PayloadType.SdpResponseWireless)]
        public void PayloadTypes_HaveExpectedNumericValues(UInt16 Wire, V2GTP_PayloadType Expected)
        {

            Assert.Multiple(() => {
                Assert.That((V2GTP_PayloadType) Wire, Is.EqualTo(Expected));
                Assert.That(Expected.IsKnown(),       Is.True);
            });

        }

        #endregion

        #region SupportedAppProtocol_SharesTheWireValueOfExiMainstream()

        /// <summary>
        /// One id for two things, and that is the standard rather than an oversight: the SAP
        /// handshake runs before a protocol has been agreed, so it cannot have an id of its own.
        /// </summary>
        [Test]
        public void SupportedAppProtocol_SharesTheWireValueOfExiMainstream()
        {

            Assert.Multiple(() => {
                Assert.That(V2GTP_PayloadType.ExiSupportedAppProtocol,          Is.EqualTo(V2GTP_PayloadType.ExiMainstream));
                Assert.That((UInt16) V2GTP_PayloadType.ExiSupportedAppProtocol, Is.EqualTo((UInt16) 0x8001));
            });

        }

        #endregion

        #region UnknownPayloadType_IsNotKnown()

        [Test]
        public void UnknownPayloadType_IsNotKnown()
        {

            Assert.Multiple(() => {
                // 0xA000..0xFFFF is the manufacturer-specific range, used here for pentest cases.
                Assert.That(((V2GTP_PayloadType) 0xA000).IsKnown(), Is.False);
                // 0x0000..0x7FFF is reserved.
                Assert.That(((V2GTP_PayloadType) 0x0000).IsKnown(), Is.False);
                // 0x8101 is ScheduleRenegotiation — a real payload type, but not modelled here.
                Assert.That(((V2GTP_PayloadType) 0x8101).IsKnown(), Is.False);
            });

        }

        #endregion

        #region IsSDP_HoldsForTheFourSdpTypesOnly(...)

        [TestCase(V2GTP_PayloadType.SdpRequest,          true)]
        [TestCase(V2GTP_PayloadType.SdpResponse,         true)]
        [TestCase(V2GTP_PayloadType.SdpRequestWireless,  true)]
        [TestCase(V2GTP_PayloadType.SdpResponseWireless, true)]
        [TestCase(V2GTP_PayloadType.ExiMainstream,       false)]
        [TestCase(V2GTP_PayloadType.ExiIso20Mainstream,  false)]
        [TestCase(V2GTP_PayloadType.ExiWPT,              false)]
        public void IsSDP_HoldsForTheFourSdpTypesOnly(V2GTP_PayloadType PayloadType, Boolean Expected)
        {

            Assert.That(PayloadType.IsSDP(), Is.EqualTo(Expected));

        }

        #endregion

    }

}
