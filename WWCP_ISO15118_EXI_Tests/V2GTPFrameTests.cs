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
using cloud.charging.open.protocols.ISO15118.EXI.Dispatch;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{
    // Inside the namespace declaration on purpose. The merged V2GTP project puts a namespace
    // cloud.charging.open.protocols.ISO15118.V2GTP in scope here, and name lookup walks the
    // enclosing namespace declarations outward before it ever reaches a file-level using-alias —
    // so an alias at the top of the file loses to it, and one declared in here wins.
    using V2GTP = cloud.charging.open.protocols.ISO15118.EXI.Dispatch.V2GTP;

    [TestFixture]
    public class V2GTPFrameTests
    {
        [Test]
        public void Header_Roundtrip()
        {
            // Use a heap byte[] (not stackalloc) so the buffer isn't a ref struct;
            // ref structs can't be captured by Assert.Multiple's closure.
            var buf = new byte[V2GTP.HeaderSize];
            V2GTP.WriteHeader(buf, V2GTP.PayloadType_AppProtocol, 42);   // 0x8001 (SAP / -2 EXI payload id)

            // Compare the wire bytes in one shot. This is both clearer than 8
            // individual asserts and dodges the ref-struct-in-lambda issue entirely.
            var expected = new byte[] { 0x01, 0xFE, 0x80, 0x01, 0x00, 0x00, 0x00, 0x2A };
            Assert.That(buf, Is.EqualTo(expected));

            Assert.That(V2GTP.TryReadHeader(buf, out var pt, out var plen), Is.True);
            Assert.That(pt,   Is.EqualTo(V2GTP.PayloadType_AppProtocol));
            Assert.That(plen, Is.EqualTo(42u));
        }

        [Test]
        public void Header_Rejects_WrongVersion()
        {
            var buf = new byte[8];
            buf[0] = 0x02; buf[1] = 0xFE; // bad version
            Assert.That(V2GTP.TryReadHeader(buf, out _, out _), Is.False);
        }

        [Test]
        public void Header_Rejects_TooShortBuffer()
        {
            var buf = new byte[7];
            Assert.That(V2GTP.TryReadHeader(buf, out _, out _), Is.False);
        }

        /// <summary>
        /// The payload ids, against the numbers rather than against ourselves.
        /// </summary>
        /// <remarks>
        /// Every other test here round-trips a frame through the dispatcher, which proves the ids
        /// are used *consistently* and nothing more: shift the whole -20 block by one and they all
        /// still pass. That is not hypothetical. The V2GTP implementation this project was merged
        /// with had exactly that defect — no id for the -20 mainstream at all, and AC, DC, ACDP and
        /// WPT each sitting on the value belonging to the one below, so WPT wrote ACDP's id. It
        /// survived for years because nothing outside that file used the constants.
        ///
        /// So these are written out as literals. A literal is the only assertion an off-by-one
        /// cannot satisfy.
        /// </remarks>
        [Test]
        public void ThePayloadIdsAreTheOnesTheStandardGives()
        {
            Assert.Multiple(() =>
            {
                Assert.That(V2GTP.PayloadType_AppProtocol, Is.EqualTo((ushort) 0x8001));
                Assert.That(V2GTP.PayloadType_DinIso2Main, Is.EqualTo((ushort) 0x8001),
                            "the handshake and -2 share an id — SAP has none of its own");
                Assert.That(V2GTP.PayloadType_Iso20Main,   Is.EqualTo((ushort) 0x8002));
                Assert.That(V2GTP.PayloadType_Iso20AC,     Is.EqualTo((ushort) 0x8003));
                Assert.That(V2GTP.PayloadType_Iso20DC,     Is.EqualTo((ushort) 0x8004));
                Assert.That(V2GTP.PayloadType_Iso20ACDP,   Is.EqualTo((ushort) 0x8005));
                Assert.That(V2GTP.PayloadType_Iso20WPT,    Is.EqualTo((ushort) 0x8006));
            });
        }
    }
}
