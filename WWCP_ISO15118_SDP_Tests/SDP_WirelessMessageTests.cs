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

using cloud.charging.open.protocols.ISO15118.SDP.Messages;
using cloud.charging.open.protocols.ISO15118.V2GTP;

#endregion

namespace cloud.charging.open.protocols.ISO15118.SDP.Tests
{

    /// <summary>
    /// Wireless SDP (ISO 15118-20 only, payload types 0x9002/0x9003) for the ACDP and WPT
    /// scenarios. Unlike the wired pair these are fixed-size records with ASCII identifier
    /// fields, so the padding rules are part of the wire format and are asserted here.
    /// </summary>
    [TestFixture]
    public class SDP_RequestWirelessTests
    {

        #region Encode_HasFixedSize62()

        [Test]
        public void Encode_HasFixedSize62()
        {

            var request = new SDP_RequestWireless(
                              Security:           SDP_Security.TLS,
                              TransportProtocol:  SDP_TransportProtocol.TCP,
                              P2psPpd:            0x1234,
                              CouplingType:       0x01,
                              Evid:               "ZZ00000",
                              Evseid:             "DE*ABC*E12345*1"
                          );

            var bytes = request.EncodePayload();

            Assert.Multiple(() => {
                Assert.That(bytes.Length, Is.EqualTo(SDP_RequestWireless.PayloadSize));
                Assert.That(bytes.Length, Is.EqualTo(62));
                Assert.That(bytes[0],     Is.EqualTo(0x00));   // TLS
                Assert.That(bytes[1],     Is.EqualTo(0x00));   // TCP
                Assert.That(bytes[2],     Is.EqualTo(0x12));   // P2PS/PPD high byte
                Assert.That(bytes[3],     Is.EqualTo(0x34));   // P2PS/PPD low byte
                Assert.That(bytes[4],     Is.EqualTo(0x01));   // CouplingType
            });

        }

        #endregion

        #region RoundTrip_PreservesAllFields()

        [Test]
        public void RoundTrip_PreservesAllFields()
        {

            var original = new SDP_RequestWireless(
                               Security:           SDP_Security.TLS,
                               TransportProtocol:  SDP_TransportProtocol.TCP,
                               P2psPpd:            0xCAFE,
                               CouplingType:       0x02,
                               Evid:               "ZZ12345",
                               Evseid:             "DE*ABC*E12345*1"
                           );

            var decoded = SDP_RequestWireless.Decode(original.EncodePayload());

            Assert.Multiple(() => {
                Assert.That(decoded.Security,          Is.EqualTo(original.Security));
                Assert.That(decoded.TransportProtocol, Is.EqualTo(original.TransportProtocol));
                Assert.That(decoded.P2psPpd,           Is.EqualTo(original.P2psPpd));
                Assert.That(decoded.CouplingType,      Is.EqualTo(original.CouplingType));
                Assert.That(decoded.Evid,              Is.EqualTo(original.Evid));
                Assert.That(decoded.Evseid,            Is.EqualTo(original.Evseid));
            });

        }

        #endregion

        #region EncodeFrame_UsesPayloadType9002()

        [Test]
        public void EncodeFrame_UsesPayloadType9002()
        {

            var request = new SDP_RequestWireless(
                              SDP_Security.TLS, SDP_TransportProtocol.TCP,
                              0, 0, "", ""
                          );

            var frame = request.EncodeFrame();

            Assert.Multiple(() => {
                Assert.That(frame.Length,          Is.EqualTo(V2GTP_Header.Size + SDP_RequestWireless.PayloadSize));
                Assert.That(frame[2],              Is.EqualTo(0x90));
                Assert.That(frame[3],              Is.EqualTo(0x02));
                Assert.That(request.PayloadType,   Is.EqualTo(V2GTP_PayloadType.SdpRequestWireless));
            });

        }

        #endregion

        #region Decode_RejectsWrongLength()

        [Test]
        public void Decode_RejectsWrongLength()
        {

            Assert.Multiple(() => {
                Assert.Throws<ArgumentException>(() => SDP_RequestWireless.Decode(new Byte[61]));
                Assert.Throws<ArgumentException>(() => SDP_RequestWireless.Decode(new Byte[63]));
            });

        }

        #endregion

        #region DecodeLenient_AcceptsTrailingBytes()

        [Test]
        public void DecodeLenient_AcceptsTrailingBytes()
        {

            var payload  = new SDP_RequestWireless(
                               SDP_Security.TLS, SDP_TransportProtocol.TCP,
                               0xBEEF, 0x03, "ZZ00000", "DE*ABC*E12345*1"
                           ).EncodePayload();

            var decoded  = SDP_RequestWireless.DecodeLenient(payload.Concat(new Byte[] { 0xDE, 0xAD }).ToArray());

            Assert.Multiple(() => {
                Assert.That(decoded.P2psPpd,      Is.EqualTo((UInt16) 0xBEEF));
                Assert.That(decoded.CouplingType, Is.EqualTo((Byte) 0x03));
                Assert.That(decoded.Evid,         Is.EqualTo("ZZ00000"));
            });

        }

        #endregion

        #region DefaultVersion_IsIso15118_20()

        /// <summary>
        /// Wireless SDP exists only in -20, so unlike the wired pair the version is not a
        /// coin flip: it defaults to -20 and there is no -2 reading of these payload types.
        /// </summary>
        [Test]
        public void DefaultVersion_IsIso15118_20()
        {

            var request = new SDP_RequestWireless(
                              SDP_Security.TLS, SDP_TransportProtocol.TCP, 0, 0, "", ""
                          );

            Assert.That(request.Version, Is.EqualTo(SDP_Version.ISO_15118_20));

        }

        #endregion

        #region Evid_IsPaddedToTwentyBytes()

        [Test]
        public void Evid_IsPaddedToTwentyBytes()
        {

            var bytes = new SDP_RequestWireless(
                            SDP_Security.TLS, SDP_TransportProtocol.TCP, 0, 0, "X", ""
                        ).EncodePayload();

            // EVID lives at offset 5..24; the first byte is 'X' = 0x58, the rest must be 0x00.
            Assert.Multiple(() => {

                Assert.That(bytes[5], Is.EqualTo((Byte) 'X'));

                for (var i = 6; i < 25; i++)
                    Assert.That(bytes[i], Is.EqualTo(0x00), $"byte at offset {i} not zero");

            });

        }

        #endregion

        #region Evseid_IsPaddedToThirtySevenBytes()

        [Test]
        public void Evseid_IsPaddedToThirtySevenBytes()
        {

            var bytes = new SDP_RequestWireless(
                            SDP_Security.TLS, SDP_TransportProtocol.TCP, 0, 0, "", "Y"
                        ).EncodePayload();

            // EVSEID lives at offset 25..61.
            Assert.Multiple(() => {

                Assert.That(bytes[25], Is.EqualTo((Byte) 'Y'));

                for (var i = 26; i < 62; i++)
                    Assert.That(bytes[i], Is.EqualTo(0x00), $"byte at offset {i} not zero");

            });

        }

        #endregion

        #region Decode_StripsThePaddingBackOff()

        [Test]
        public void Decode_StripsThePaddingBackOff()
        {

            var payload  = new SDP_RequestWireless(
                               SDP_Security.TLS, SDP_TransportProtocol.TCP, 0, 0, "ZZ00000", "DE*ABC*E12345*1"
                           ).EncodePayload();

            var decoded  = SDP_RequestWireless.Decode(payload);

            Assert.Multiple(() => {
                Assert.That(decoded.Evid,   Is.EqualTo("ZZ00000"));
                Assert.That(decoded.Evseid, Is.EqualTo("DE*ABC*E12345*1"));
            });

        }

        #endregion

    }


    /// <summary>
    /// The wireless SDP_Response (payload type 0x9003), 59 bytes. Note that it carries the
    /// SECC address as a raw 16-byte array rather than an IPAddress, because the diagnostic
    /// status can be reported before an address has been assigned at all.
    /// </summary>
    [TestFixture]
    public class SDP_ResponseWirelessTests
    {

        #region (private static) LinkLocalIPv6(Tail)

        private static Byte[] LinkLocalIPv6(Byte Tail)
        {

            var bytes = new Byte[16];

            bytes[ 0] = 0xFE;
            bytes[ 1] = 0x80;
            bytes[15] = Tail;

            return bytes;

        }

        #endregion


        #region Encode_HasFixedSize59()

        [Test]
        public void Encode_HasFixedSize59()
        {

            var response = new SDP_ResponseWireless(
                               SeccIpAddress:      LinkLocalIPv6(0x01),
                               SeccTcpPort:        64109,
                               Security:           SDP_Security.TLS,
                               TransportProtocol:  SDP_TransportProtocol.TCP,
                               DiagStatus:         0x01,
                               CouplingType:       0x02,
                               Evseid:             "DE*ABC*E12345*1"
                           );

            var bytes = response.EncodePayload();

            Assert.Multiple(() => {
                Assert.That(bytes.Length, Is.EqualTo(SDP_ResponseWireless.PayloadSize));
                Assert.That(bytes.Length, Is.EqualTo(59));
                Assert.That(bytes[ 0],    Is.EqualTo(0xFE));                                // link-local prefix
                Assert.That(bytes[ 1],    Is.EqualTo(0x80));
                Assert.That(bytes[16],    Is.EqualTo(0xFA));                                // 64109 high byte
                Assert.That(bytes[17],    Is.EqualTo(0x6D));                                // 64109 low byte
                Assert.That(bytes[18],    Is.EqualTo((Byte) SDP_Security.TLS));
                Assert.That(bytes[19],    Is.EqualTo((Byte) SDP_TransportProtocol.TCP));
                Assert.That(bytes[20],    Is.EqualTo(0x01));                                // DiagStatus
                Assert.That(bytes[21],    Is.EqualTo(0x02));                                // CouplingType
            });

        }

        #endregion

        #region RoundTrip_PreservesAllFields()

        [Test]
        public void RoundTrip_PreservesAllFields()
        {

            var original = new SDP_ResponseWireless(
                               SeccIpAddress:      LinkLocalIPv6(0x42),
                               SeccTcpPort:        15119,
                               Security:           SDP_Security.TLS,
                               TransportProtocol:  SDP_TransportProtocol.TCP,
                               DiagStatus:         0x02,
                               CouplingType:       0x01,
                               Evseid:             "DE*XYZ*E99999*9"
                           );

            var decoded = SDP_ResponseWireless.Decode(original.EncodePayload());

            Assert.Multiple(() => {
                Assert.That(decoded.SeccIpAddress,     Is.EqualTo(original.SeccIpAddress));
                Assert.That(decoded.SeccTcpPort,       Is.EqualTo(original.SeccTcpPort));
                Assert.That(decoded.Security,          Is.EqualTo(original.Security));
                Assert.That(decoded.TransportProtocol, Is.EqualTo(original.TransportProtocol));
                Assert.That(decoded.DiagStatus,        Is.EqualTo(original.DiagStatus));
                Assert.That(decoded.CouplingType,      Is.EqualTo(original.CouplingType));
                Assert.That(decoded.Evseid,            Is.EqualTo(original.Evseid));
            });

        }

        #endregion

        #region EncodeFrame_UsesPayloadType9003_TotalLength67()

        [Test]
        public void EncodeFrame_UsesPayloadType9003_TotalLength67()
        {

            var response = new SDP_ResponseWireless(
                               LinkLocalIPv6(0x01), 64109, SDP_Security.TLS, SDP_TransportProtocol.TCP,
                               0x00, 0x00, ""
                           );

            var frame = response.EncodeFrame();

            Assert.Multiple(() => {
                Assert.That(frame.Length,         Is.EqualTo(V2GTP_Header.Size + SDP_ResponseWireless.PayloadSize));
                Assert.That(frame.Length,         Is.EqualTo(67));       // 8 + 59
                Assert.That(frame[2],             Is.EqualTo(0x90));
                Assert.That(frame[3],             Is.EqualTo(0x03));
                Assert.That(frame[7],             Is.EqualTo(0x3B));     // length = 59 = 0x3B
                Assert.That(response.PayloadType, Is.EqualTo(V2GTP_PayloadType.SdpResponseWireless));
            });

        }

        #endregion

        #region EncodeFrame_RoundTripsThroughV2GTPFrame()

        [Test]
        public void EncodeFrame_RoundTripsThroughV2GTPFrame()
        {

            var response    = new SDP_ResponseWireless(
                                  LinkLocalIPv6(0x07), 15118, SDP_Security.TLS, SDP_TransportProtocol.TCP,
                                  0x01, 0x02, "DE*ABC*E12345*1"
                              );

            var v2gtpFrame  = V2GTP_Frame.Parse(response.EncodeFrame());
            var parsed      = SDP_ResponseWireless.Decode(v2gtpFrame.Payload.Span);

            Assert.Multiple(() => {
                Assert.That(v2gtpFrame.Header.PayloadType, Is.EqualTo(V2GTP_PayloadType.SdpResponseWireless));
                Assert.That(parsed.SeccTcpPort,            Is.EqualTo((UInt16) 15118));
                Assert.That(parsed.Evseid,                 Is.EqualTo("DE*ABC*E12345*1"));
            });

        }

        #endregion

        #region Encode_RejectsBadIpAddressLength()

        [Test]
        public void Encode_RejectsBadIpAddressLength()
        {

            var response = new SDP_ResponseWireless(
                               new Byte[15], 64109, SDP_Security.TLS, SDP_TransportProtocol.TCP,
                               0x00, 0x00, ""
                           );

            Assert.Throws<ArgumentException>(() => response.EncodePayload());

        }

        #endregion

        #region Decode_RejectsWrongLength()

        [Test]
        public void Decode_RejectsWrongLength()
        {

            Assert.Multiple(() => {
                Assert.Throws<ArgumentException>(() => SDP_ResponseWireless.Decode(new Byte[58]));
                Assert.Throws<ArgumentException>(() => SDP_ResponseWireless.Decode(new Byte[60]));
            });

        }

        #endregion

        #region DecodeLenient_AcceptsTrailingBytes()

        [Test]
        public void DecodeLenient_AcceptsTrailingBytes()
        {

            var payload  = new SDP_ResponseWireless(
                               LinkLocalIPv6(0x09), 64109, SDP_Security.TLS, SDP_TransportProtocol.TCP,
                               0x10, 0x01, "DE*ABC*E12345*1"
                           ).EncodePayload();

            var decoded  = SDP_ResponseWireless.DecodeLenient(payload.Concat(new Byte[] { 0xDE, 0xAD }).ToArray());

            Assert.Multiple(() => {
                Assert.That(decoded.SeccTcpPort, Is.EqualTo((UInt16) 64109));
                Assert.That(decoded.DiagStatus,  Is.EqualTo((Byte) 0x10));   // 0x10 = error
            });

        }

        #endregion

    }

}
