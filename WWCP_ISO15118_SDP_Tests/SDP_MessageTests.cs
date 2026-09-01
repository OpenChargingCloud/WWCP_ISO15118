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

using System.Net;

using NUnit.Framework;

using cloud.charging.open.protocols.ISO15118.SDP.Messages;
using cloud.charging.open.protocols.ISO15118.V2GTP;

#endregion

namespace cloud.charging.open.protocols.ISO15118.SDP.Tests
{

    /// <summary>
    /// The basic (wired) SDP_Request. Two bytes, and both -2 and -20 encode them the same
    /// way under V2GTP payload type 0x9000 — so these assertions are wire format, not API shape.
    /// </summary>
    [TestFixture]
    public class SDP_RequestTests
    {

        #region Encode_TlsTcp_TwoBytes_00_00()

        [Test]
        public void Encode_TlsTcp_TwoBytes_00_00()
        {

            var request = new SDP_Request(SDP_Security.TLS, SDP_TransportProtocol.TCP);

            Assert.That(request.EncodePayload(), Is.EqualTo(new Byte[] { 0x00, 0x00 }));

        }

        #endregion

        #region Encode_NoTlsTcp_TwoBytes_10_00()

        [Test]
        public void Encode_NoTlsTcp_TwoBytes_10_00()
        {

            var request = new SDP_Request(SDP_Security.NoTLS, SDP_TransportProtocol.TCP);

            Assert.That(request.EncodePayload(), Is.EqualTo(new Byte[] { 0x10, 0x00 }));

        }

        #endregion

        #region Decode_RoundTrips()

        [Test]
        public void Decode_RoundTrips()
        {

            var request = SDP_Request.Decode(new Byte[] { 0x10, 0x00 });

            Assert.Multiple(() => {
                Assert.That(request.Security,          Is.EqualTo(SDP_Security.NoTLS));
                Assert.That(request.TransportProtocol, Is.EqualTo(SDP_TransportProtocol.TCP));
            });

        }

        #endregion

        #region Decode_RejectsWrongLength()

        [Test]
        public void Decode_RejectsWrongLength()
        {

            Assert.Multiple(() => {
                Assert.Throws<ArgumentException>(() => SDP_Request.Decode(new Byte[] { 0x00 }));
                Assert.Throws<ArgumentException>(() => SDP_Request.Decode(new Byte[] { 0x00, 0x00, 0x00 }));
            });

        }

        #endregion

        #region DecodeLenient_AcceptsTrailingBytes()

        [Test]
        public void DecodeLenient_AcceptsTrailingBytes()
        {

            var request = SDP_Request.DecodeLenient(new Byte[] { 0x00, 0x00, 0xCA, 0xFE });

            Assert.That(request.Security, Is.EqualTo(SDP_Security.TLS));

        }

        #endregion

        #region DecodeLenient_StillRejectsShortPayloads()

        [Test]
        public void DecodeLenient_StillRejectsShortPayloads()
        {

            Assert.Throws<ArgumentException>(() => SDP_Request.DecodeLenient(new Byte[] { 0x00 }));

        }

        #endregion

        #region Version_DefaultsToIso15118_2()

        /// <summary>
        /// The wired request is identical on the wire in both revisions, so the codec cannot
        /// tell them apart. It carries the revision as metadata and defaults to -2.
        /// </summary>
        [Test]
        public void Version_DefaultsToIso15118_2()
        {

            Assert.Multiple(() => {
                Assert.That(new SDP_Request(SDP_Security.TLS, SDP_TransportProtocol.TCP).Version, Is.EqualTo(SDP_Version.ISO_15118_2));
                Assert.That(SDP_Request.Decode(new Byte[] { 0x00, 0x00 }).Version,                Is.EqualTo(SDP_Version.ISO_15118_2));
            });

        }

        #endregion

        #region Decode_PreservesTheRequestedVersion()

        [Test]
        public void Decode_PreservesTheRequestedVersion()
        {

            var request = SDP_Request.Decode(new Byte[] { 0x00, 0x00 }, SDP_Version.ISO_15118_20);

            Assert.That(request.Version, Is.EqualTo(SDP_Version.ISO_15118_20));

        }

        #endregion

        #region Decode_AcceptsNonCompliantSecurityValues_ForPentest()

        /// <summary>
        /// 0x42 is not a defined Security value. The codec must not reject it: pentest tooling
        /// needs to dissect exactly the frames a compliant stack would refuse.
        /// </summary>
        [Test]
        public void Decode_AcceptsNonCompliantSecurityValues_ForPentest()
        {

            var request = SDP_Request.Decode(new Byte[] { 0x42, 0x99 });

            Assert.Multiple(() => {
                Assert.That((Byte) request.Security,          Is.EqualTo(0x42));
                Assert.That((Byte) request.TransportProtocol, Is.EqualTo(0x99));
            });

        }

        #endregion

        #region EncodeFrame_HasV2GTPHeader_PayloadType9000()

        [Test]
        public void EncodeFrame_HasV2GTPHeader_PayloadType9000()
        {

            var frame = new SDP_Request(SDP_Security.TLS, SDP_TransportProtocol.TCP).EncodeFrame();

            Assert.Multiple(() => {
                Assert.That(frame.Length, Is.EqualTo(10));         // 8 header + 2 payload
                Assert.That(frame[2],     Is.EqualTo(0x90));
                Assert.That(frame[3],     Is.EqualTo(0x00));
            });

        }

        #endregion

        #region PayloadType_IsSdpRequest()

        [Test]
        public void PayloadType_IsSdpRequest()
        {

            Assert.That(new SDP_Request(SDP_Security.TLS, SDP_TransportProtocol.TCP).PayloadType,
                        Is.EqualTo(V2GTP_PayloadType.SdpRequest));

        }

        #endregion

    }


    /// <summary>
    /// The basic (wired) SDP_Response: 20 bytes announcing the SECC endpoint, under V2GTP
    /// payload type 0x9001.
    /// </summary>
    [TestFixture]
    public class SDP_ResponseTests
    {

        #region Encode_LinkLocalSecc_RoundTrips()

        [Test]
        public void Encode_LinkLocalSecc_RoundTrips()
        {

            var address   = IPAddress.Parse("fe80::1234:5678:9abc:def0");
            var response  = new SDP_Response(address, 64109, SDP_Security.TLS, SDP_TransportProtocol.TCP);
            var bytes     = response.EncodePayload();

            Assert.That(bytes.Length, Is.EqualTo(SDP_Response.PayloadSize));

            var parsed = SDP_Response.Decode(bytes);

            Assert.Multiple(() => {
                Assert.That(parsed.SeccIPAddress,     Is.EqualTo(address));
                Assert.That(parsed.SeccPort,          Is.EqualTo((UInt16) 64109));
                Assert.That(parsed.Security,          Is.EqualTo(SDP_Security.TLS));
                Assert.That(parsed.TransportProtocol, Is.EqualTo(SDP_TransportProtocol.TCP));
            });

        }

        #endregion

        #region Encode_PortBigEndian()

        [Test]
        public void Encode_PortBigEndian()
        {

            var response  = new SDP_Response(IPAddress.Parse("fe80::1"), 0xFA01, SDP_Security.TLS, SDP_TransportProtocol.TCP);
            var bytes     = response.EncodePayload();

            Assert.Multiple(() => {
                Assert.That(bytes[16], Is.EqualTo(0xFA));
                Assert.That(bytes[17], Is.EqualTo(0x01));
            });

        }

        #endregion

        #region Encode_PlacesTheAddressInTheFirstSixteenOctets()

        [Test]
        public void Encode_PlacesTheAddressInTheFirstSixteenOctets()
        {

            var address   = IPAddress.Parse("fe80::1234:5678:9abc:def0");
            var bytes     = new SDP_Response(address, 15118, SDP_Security.NoTLS, SDP_TransportProtocol.TCP).EncodePayload();

            Assert.Multiple(() => {
                Assert.That(bytes[..16], Is.EqualTo(address.GetAddressBytes()));
                Assert.That(bytes[18],   Is.EqualTo((Byte) SDP_Security.NoTLS));
                Assert.That(bytes[19],   Is.EqualTo((Byte) SDP_TransportProtocol.TCP));
            });

        }

        #endregion

        #region Encode_RejectsIPv4()

        [Test]
        public void Encode_RejectsIPv4()
        {

            var response = new SDP_Response(IPAddress.Parse("192.0.2.1"), 1234, SDP_Security.TLS, SDP_TransportProtocol.TCP);

            Assert.Throws<InvalidOperationException>(() => response.EncodePayload());

        }

        #endregion

        #region Decode_RejectsWrongLength()

        [Test]
        public void Decode_RejectsWrongLength()
        {

            Assert.Multiple(() => {
                Assert.Throws<ArgumentException>(() => SDP_Response.Decode(new Byte[19]));
                Assert.Throws<ArgumentException>(() => SDP_Response.Decode(new Byte[21]));
            });

        }

        #endregion

        #region DecodeLenient_AcceptsTrailingBytes()

        [Test]
        public void DecodeLenient_AcceptsTrailingBytes()
        {

            var address   = IPAddress.Parse("fe80::1");
            var payload   = new SDP_Response(address, 15118, SDP_Security.TLS, SDP_TransportProtocol.TCP).EncodePayload();
            var padded    = payload.Concat(new Byte[] { 0xDE, 0xAD }).ToArray();

            var parsed    = SDP_Response.DecodeLenient(padded);

            Assert.Multiple(() => {
                Assert.That(parsed.SeccIPAddress, Is.EqualTo(address));
                Assert.That(parsed.SeccPort,      Is.EqualTo((UInt16) 15118));
            });

        }

        #endregion

        #region EncodeFrame_HasV2GTPHeader_PayloadType9001_Length20()

        [Test]
        public void EncodeFrame_HasV2GTPHeader_PayloadType9001_Length20()
        {

            var response  = new SDP_Response(IPAddress.Parse("fe80::1"), 15119, SDP_Security.TLS, SDP_TransportProtocol.TCP);
            var frame     = response.EncodeFrame();

            Assert.Multiple(() => {
                Assert.That(frame.Length, Is.EqualTo(28));         // 8 + 20
                Assert.That(frame[2],     Is.EqualTo(0x90));
                Assert.That(frame[3],     Is.EqualTo(0x01));
                Assert.That(frame[7],     Is.EqualTo(0x14));       // big-endian length = 20 = 0x14
            });

            // round-trip through V2GTP_Frame
            var v2gtpFrame  = V2GTP_Frame.Parse(frame);
            var parsed      = SDP_Response.Decode(v2gtpFrame.Payload.Span);

            Assert.Multiple(() => {
                Assert.That(v2gtpFrame.Header.PayloadType, Is.EqualTo(V2GTP_PayloadType.SdpResponse));
                Assert.That(parsed.SeccPort,               Is.EqualTo((UInt16) 15119));
            });

        }

        #endregion

        #region ToString_NamesTheEndpoint()

        [Test]
        public void ToString_NamesTheEndpoint()
        {

            var response = new SDP_Response(IPAddress.Parse("fe80::1"), 15118, SDP_Security.TLS, SDP_TransportProtocol.TCP);

            Assert.That(response.ToString(), Does.Contain("fe80::1").And.Contains("15118"));

        }

        #endregion

    }

}
