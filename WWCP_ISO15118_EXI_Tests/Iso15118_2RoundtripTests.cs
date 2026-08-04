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
using cloud.charging.open.protocols.ISO15118_2.Generated;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{
    /// <summary>
    /// Self-consistency tests for the generated ISO 15118-2 codec: every target message must survive an
    /// encode → decode → re-encode cycle byte-for-byte. This exercises the whole generated pipeline
    /// (document grammar, header with hexBinary SessionID, BodyElement substitution dispatch, and each
    /// message body) on real messages. Byte conformance against cbV2G is a separate, vector-driven step;
    /// re-encoding sidesteps the reference-equality of the records' <c>byte[]</c> fields.
    /// </summary>
    [TestFixture]
    public class Iso15118_2RoundtripTests
    {
        private static MessageHeaderType Header() =>
            new(SessionID: new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                Notification: null, Signature: null);

        private static IEnumerable<TestCaseData> Messages()
        {
            yield return new TestCaseData(new V2G_Message(Header(),
                new BodyType(new SessionSetupReqType(EVCCID: new byte[] { 0xAB, 0xCD, 0xEF, 0x01, 0x02, 0x03 }))))
                .SetName("SessionSetupReq");

            yield return new TestCaseData(new V2G_Message(Header(),
                new BodyType(new SessionSetupResType(ResponseCode.OK_NewSessionEstablished, "DE*ABC*E12345*1", 1_600_000_000L))))
                .SetName("SessionSetupRes(with timestamp)");

            yield return new TestCaseData(new V2G_Message(Header(),
                new BodyType(new SessionSetupResType(ResponseCode.OK, "EVSE1", EVSETimeStamp: null))))
                .SetName("SessionSetupRes(no timestamp)");

            yield return new TestCaseData(new V2G_Message(Header(),
                new BodyType(new ServiceDiscoveryReqType(ServiceScope: null, ServiceCategory: null))))
                .SetName("ServiceDiscoveryReq(both absent)");

            yield return new TestCaseData(new V2G_Message(Header(),
                new BodyType(new ServiceDiscoveryReqType("urn:scope:test", ServiceCategory.EVCharging))))
                .SetName("ServiceDiscoveryReq(both present)");

            yield return new TestCaseData(new V2G_Message(Header(),
                new BodyType(new ServiceDiscoveryResType(
                    ResponseCode.OK,
                    new PaymentOptionListType(new[] { PaymentOption.Contract, PaymentOption.ExternalPayment }),
                    new ChargeServiceType(ServiceID: 1, ServiceName: "AC", ServiceCategory.EVCharging,
                        ServiceScope: null, FreeService: true,
                        new SupportedEnergyTransferModeType(new[] { EnergyTransferMode.AC_single_phase_core, EnergyTransferMode.AC_three_phase_core })),
                    ServiceList: null))))
                .SetName("ServiceDiscoveryRes");
        }

        [TestCaseSource(nameof(Messages))]
        public void Roundtrip_ReEncodesToTheSameBytes(V2G_Message message)
        {
            var buf1 = new byte[512];
            Assert.That(message.TryEncode(buf1, out int n1), Is.True, "encode failed");

            var decoded = (V2G_Message)Iso2Codec.DecodeAny(buf1.AsSpan(0, n1), out int consumed);
            Assert.That(consumed, Is.EqualTo(n1), "decoder did not consume all encoded bytes");

            var buf2 = new byte[512];
            Assert.That(decoded.TryEncode(buf2, out int n2), Is.True, "re-encode failed");

            Assert.That(buf2.AsSpan(0, n2).ToArray(), Is.EqualTo(buf1.AsSpan(0, n1).ToArray()),
                "decode∘encode is not the identity on the wire");
        }
    }
}
