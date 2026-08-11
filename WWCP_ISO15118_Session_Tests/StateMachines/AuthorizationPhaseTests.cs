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

using cloud.charging.open.protocols.ISO15118.StateMachines;
using cloud.charging.open.protocols.ISO15118.StateMachines.Iso2;
using cloud.charging.open.protocols.ISO15118_2.Generated;


using NUnit.Framework;

namespace cloud.charging.open.protocols.ISO15118.Session.Tests.StateMachines
{

    /// <summary>
    /// Authorization is a phase a station may need to stay in. Two answers bring an EV back to it: an
    /// <c>Ongoing</c>, which it polls on, and — in -20 — a <c>WARNING_*</c> code, the family that says
    /// <i>this credential</i> did not work rather than that the session is over, so the car may present
    /// another contract or fall back to EIM.
    /// </summary>
    /// <remarks>
    /// Both machines used to advance out of the phase the moment they answered, which turned the EV's
    /// next message into <c>FAILED_SequenceError</c>. Nothing noticed for as long as every station of
    /// ours answered <c>Finished</c> immediately — and a tux-evse replay of a real VW, polling
    /// <c>AuthorizationReq</c> twice at a charger answering <c>Ongoing_WaitingForCustomerInteraction</c>,
    /// is the field report of exactly that.
    /// </remarks>
    [TestFixture]
    public class AuthorizationPhaseTests
    {

        #region ISO 15118-2

        /// <summary>A -2 station that reports Ongoing on the first AuthorizationReq and Finished after.</summary>
        private sealed class SlowSecc2(PowerMode mode, TimeSpan sequenceTimeout, TimeProvider clock)
            : Secc2(mode, sequenceTimeout, clock)
        {

            private Boolean stillThinking = true;

            // Overriding Authorize rather than Handle on purpose: the phase is decided from what this
            // returns, so a station that rewrote the response afterwards would still have advanced.
            protected override BodyBaseType Authorize(AuthorizationReqType request)
            {

                var response = base.Authorize(request);

                if (stillThinking && response is AuthorizationResType res)
                {
                    stillThinking = false;
                    return new AuthorizationResType(res.ResponseCode, EVSEProcessing.Ongoing);
                }

                return response;

            }

        }

        private static V2G_Message Request(BodyBaseType body)
            => new (new MessageHeaderType(new Byte[8], Notification: null, Signature: null),
                    new BodyType(body));

        private static SlowSecc2 SeccAtAuthorization()
        {

            var secc = new SlowSecc2(PowerMode.Dc, TimeSpan.FromSeconds(30), TimeProvider.System);

            secc.Handle(Request(new SessionSetupReqType(EVCCID: new Byte[] { 0xAB, 0xCD, 0xEF, 0x01, 0x02, 0x03 })));
            secc.Handle(Request(new ServiceDiscoveryReqType(ServiceScope: null, ServiceCategory: null)));
            secc.Handle(Request(new PaymentServiceSelectionReqType(
                                    PaymentOption.ExternalPayment,
                                    new SelectedServiceListType(new[] { new SelectedServiceType(ServiceID: 1, ParameterSetID: null) }))));

            return secc;

        }

        [Test]
        public void Iso2_A_polling_EV_is_answered_rather_than_met_with_a_sequence_error()
        {

            var secc  = SeccAtAuthorization();

            var first  = secc.Handle(Request(new AuthorizationReqType(Id: null, GenChallenge: null)));
            var second = secc.Handle(Request(new AuthorizationReqType(Id: null, GenChallenge: null)));

            Assert.Multiple(() => {

                Assert.That(((AuthorizationResType) first.Body.BodyElement!).EVSEProcessing,
                            Is.EqualTo(EVSEProcessing.Ongoing),
                            "precondition: the station said it was not done yet");

                Assert.That(second.Body.BodyElement, Is.TypeOf<AuthorizationResType>(),
                            "the second poll must be answered, not refused as out of order");

                Assert.That(((AuthorizationResType) second.Body.BodyElement!).ResponseCode,
                            Is.Not.EqualTo(ResponseCode.FAILED_SequenceError));

                Assert.That(secc.SequenceErrorAt, Is.Null);

            });

        }

        [Test]
        public void Iso2_A_station_that_finishes_at_once_still_moves_on()
        {

            // The unchanged path: Authorize answers Finished, so the phase advances and the EV's
            // ChargeParameterDiscovery is in order.
            var secc = new Secc2(PowerMode.Dc, TimeSpan.FromSeconds(30), TimeProvider.System);

            secc.Handle(Request(new SessionSetupReqType(EVCCID: new Byte[] { 0xAB, 0xCD, 0xEF, 0x01, 0x02, 0x03 })));
            secc.Handle(Request(new ServiceDiscoveryReqType(ServiceScope: null, ServiceCategory: null)));
            secc.Handle(Request(new PaymentServiceSelectionReqType(
                                    PaymentOption.ExternalPayment,
                                    new SelectedServiceListType(new[] { new SelectedServiceType(ServiceID: 1, ParameterSetID: null) }))));

            var auth = secc.Handle(Request(new AuthorizationReqType(Id: null, GenChallenge: null)));

            Assert.That(((AuthorizationResType) auth.Body.BodyElement!).EVSEProcessing,
                        Is.EqualTo(EVSEProcessing.Finished));
            Assert.That(secc.SequenceErrorAt, Is.Null);

        }

        #endregion

    }

}
