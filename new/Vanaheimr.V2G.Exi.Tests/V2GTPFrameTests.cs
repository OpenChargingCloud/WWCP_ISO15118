/*
 * Copyright (c) 2021-2025 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP ISO/IEC 15118 <https://github.com/OpenChargingCloud/WWCP_ISO15118>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
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
    }
}
