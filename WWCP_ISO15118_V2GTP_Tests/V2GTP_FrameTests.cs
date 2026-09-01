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
    /// A complete V2GTP datagram: header plus payload. The interesting half is the split
    /// between <see cref="V2GTP_Frame.Parse"/>, which enforces that the declared payload
    /// length is actually present, and <see cref="V2GTP_Frame.ParseRaw"/>, which does not —
    /// a lying length field is the first thing a fuzzer sends.
    /// </summary>
    [TestFixture]
    public class V2GTP_FrameTests
    {

        #region Wrap_BuildsHeaderAutomatically()

        [Test]
        public void Wrap_BuildsHeaderAutomatically()
        {

            var payload  = new Byte[] { 0x00, 0x00 };          // SDP_Request, TLS, TCP
            var frame    = V2GTP_Frame.Wrap(V2GTP_PayloadType.SdpRequest, payload);

            Assert.Multiple(() => {
                Assert.That(frame.Header.PayloadType,    Is.EqualTo(V2GTP_PayloadType.SdpRequest));
                Assert.That(frame.Header.PayloadLength,  Is.EqualTo(2u));
                Assert.That(frame.Header.IsVersionValid, Is.True);
                Assert.That(frame.TotalLength,           Is.EqualTo(V2GTP_Header.Size + 2));
            });

        }

        #endregion

        #region Wrap_AcceptsEmptyPayload()

        [Test]
        public void Wrap_AcceptsEmptyPayload()
        {

            var frame = V2GTP_Frame.Wrap(V2GTP_PayloadType.SdpRequest, Array.Empty<Byte>());

            Assert.Multiple(() => {
                Assert.That(frame.Header.PayloadLength, Is.EqualTo(0u));
                Assert.That(frame.ToArray().Length,     Is.EqualTo(V2GTP_Header.Size));
            });

        }

        #endregion

        #region Parse_RoundTrips()

        [Test]
        public void Parse_RoundTrips()
        {

            var payload  = new Byte[] { 0x10, 0x00 };          // no-TLS, TCP
            var bytes    = V2GTP_Frame.Wrap(V2GTP_PayloadType.SdpRequest, payload).ToArray();

            var frame    = V2GTP_Frame.Parse(bytes);

            Assert.Multiple(() => {
                Assert.That(frame.Header.PayloadType, Is.EqualTo(V2GTP_PayloadType.SdpRequest));
                Assert.That(frame.Payload.ToArray(),  Is.EqualTo(payload));
            });

        }

        #endregion

        #region Parse_IgnoresTrailingBytesBeyondDeclaredLength()

        /// <summary>
        /// The payload slice is cut to the declared length, not to the buffer end, so a
        /// datagram carrying padding after the payload still decodes to the payload alone.
        /// </summary>
        [Test]
        public void Parse_IgnoresTrailingBytesBeyondDeclaredLength()
        {

            var bytes = new Byte[]
            {
                0x01, 0xFE, 0x90, 0x00, 0x00, 0x00, 0x00, 0x02, // header: SDP_Request, len=2
                0x10, 0x00,                                     // the payload
                0xCA, 0xFE                                      // trailing padding
            };

            var frame = V2GTP_Frame.Parse(bytes);

            Assert.Multiple(() => {
                Assert.That(frame.Payload.Length,    Is.EqualTo(2));
                Assert.That(frame.Payload.ToArray(), Is.EqualTo(new Byte[] { 0x10, 0x00 }));
            });

        }

        #endregion

        #region Parse_RejectsLengthMismatch()

        [Test]
        public void Parse_RejectsLengthMismatch()
        {

            // header claims 100 bytes, buffer has only 2
            var bytes = new Byte[]
            {
                0x01, 0xFE, 0x90, 0x00, 0x00, 0x00, 0x00, 0x64, // header: SDP_Request, len=100
                0x00, 0x00,
            };

            Assert.Throws<V2GTP_PayloadLengthException>(() => V2GTP_Frame.Parse(bytes));

        }

        #endregion

        #region Parse_RejectsBadInverseVersion()

        [Test]
        public void Parse_RejectsBadInverseVersion()
        {

            var bytes = new Byte[]
            {
                0x01, 0x00, 0x90, 0x00, 0x00, 0x00, 0x00, 0x02, // inverse version is 0x00, not 0xFE
                0x00, 0x00,
            };

            Assert.Throws<V2GTP_ProtocolVersionException>(() => V2GTP_Frame.Parse(bytes));

        }

        #endregion

        #region ParseRaw_TruncatesGracefully_ForPentest()

        [Test]
        public void ParseRaw_TruncatesGracefully_ForPentest()
        {

            var bytes = new Byte[]
            {
                0x01, 0xFE, 0x90, 0x00, 0x00, 0x00, 0x00, 0x64, // header: len=100
                0x00, 0x00,
            };

            var frame = V2GTP_Frame.ParseRaw(bytes);

            Assert.Multiple(() => {
                Assert.That(frame.Payload.Length,       Is.EqualTo(2));    // takes what is available
                Assert.That(frame.Header.PayloadLength, Is.EqualTo(100u)); // but reports the declared length unchanged
            });

        }

        #endregion

        #region ParseRaw_YieldsEmptyPayload_WhenNothingFollowsTheHeader()

        [Test]
        public void ParseRaw_YieldsEmptyPayload_WhenNothingFollowsTheHeader()
        {

            var bytes = new Byte[] { 0x01, 0xFE, 0x90, 0x00, 0x00, 0x00, 0x00, 0x64 };

            var frame = V2GTP_Frame.ParseRaw(bytes);

            Assert.Multiple(() => {
                Assert.That(frame.Payload.Length,       Is.EqualTo(0));
                Assert.That(frame.Header.PayloadLength, Is.EqualTo(100u));
            });

        }

        #endregion

        #region ParseRaw_SurvivesAnAbsurdDeclaredLength()

        /// <summary>
        /// A declared length of 0xFFFFFFFF does not fit an Int32 slice. The lenient parser has
        /// to clamp rather than overflow — this is the shape of a frame a fuzzer will send.
        /// </summary>
        [Test]
        public void ParseRaw_SurvivesAnAbsurdDeclaredLength()
        {

            var bytes = new Byte[]
            {
                0x01, 0xFE, 0x90, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, // len = 4294967295
                0xDE, 0xAD,
            };

            var frame = V2GTP_Frame.ParseRaw(bytes);

            Assert.Multiple(() => {
                Assert.That(frame.Payload.Length,       Is.EqualTo(2));
                Assert.That(frame.Header.PayloadLength, Is.EqualTo(UInt32.MaxValue));
            });

        }

        #endregion

        #region ParseRaw_AcceptsBadInverseVersion()

        [Test]
        public void ParseRaw_AcceptsBadInverseVersion()
        {

            var bytes = new Byte[]
            {
                0x01, 0x00, 0x90, 0x00, 0x00, 0x00, 0x00, 0x02,
                0x10, 0x00,
            };

            var frame = V2GTP_Frame.ParseRaw(bytes);

            Assert.Multiple(() => {
                Assert.That(frame.Header.IsVersionValid, Is.False);
                Assert.That(frame.Payload.ToArray(),     Is.EqualTo(new Byte[] { 0x10, 0x00 }));
            });

        }

        #endregion

        #region ToArray_PlacesThePayloadDirectlyAfterTheHeader()

        [Test]
        public void ToArray_PlacesThePayloadDirectlyAfterTheHeader()
        {

            var payload  = new Byte[] { 0xAA, 0xBB, 0xCC };
            var bytes    = V2GTP_Frame.Wrap(V2GTP_PayloadType.ExiMainstream, payload).ToArray();

            Assert.Multiple(() => {
                Assert.That(bytes.Length,                       Is.EqualTo(V2GTP_Header.Size + 3));
                Assert.That(bytes[2],                           Is.EqualTo(0x80));
                Assert.That(bytes[3],                           Is.EqualTo(0x01));
                Assert.That(bytes[7],                           Is.EqualTo(0x03));
                Assert.That(bytes[V2GTP_Header.Size..],         Is.EqualTo(payload));
            });

        }

        #endregion

    }

}
