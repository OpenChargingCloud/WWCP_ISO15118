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
using cloud.charging.open.protocols.ISO15118_2;
using cloud.charging.open.protocols.ISO15118_2.Generated;


using NUnit.Framework;

namespace cloud.charging.open.protocols.ISO15118.Session.Tests.StateMachines
{

    /// <summary>
    /// The <b>SA</b> in SAScheduleList is <i>Secondary Actor</i>: the list is meant to come to the SECC from
    /// whoever sells the energy. <see cref="Secc2"/> invents one, which is what a demo station can do — and a
    /// station with a backend must be able to offer what the backend granted instead.
    /// </summary>
    /// <remarks>
    /// The second test is the one that says why the seam is <em>inside</em> the answer rather than a rewrite
    /// of it: the offer is also what <c>PowerDeliveryReq(Start)</c> is validated against ([V2G2-761]). A
    /// subclass that replaced the response afterwards would show the EV one PMax and enforce another — and
    /// the failure would be the worst kind, a car refused for exceeding a limit it was never shown.
    /// </remarks>
    [TestFixture]
    public class ScheduleOfferTests
    {

        private const Int32 BackendPMax  = 5_000;    // what the "backend" grants
        private const Int32 StationPMax  = 11_040;   // what Secc2 offers on its own (ThreePhase16A)

        /// <summary>A station whose schedule comes from somewhere else.</summary>
        private sealed class BackedSecc2(PowerMode mode, TimeSpan sequenceTimeout, TimeProvider clock, Int32 pmaxWatts)
            : Secc2(mode, sequenceTimeout, clock)
        {

            protected override SAScheduleListType OfferedSchedules()

                => new (new[]
                       {
                           new SAScheduleTupleType(SAScheduleTupleID: 1,
                               new PMaxScheduleType(new[]
                               {
                                   new PMaxScheduleEntryType(new RelativeTimeIntervalType(Start: 0, Duration: 3600),
                                                             PMax: PhysicalValue.Of(pmaxWatts, UnitSymbol.W)),
                               }),
                               SalesTariff: null),
                       });

        }

        // A real car echoes the SessionID the station assigned ([V2G2-460]); before SessionSetup that is
        // still the all-zero id a SessionSetupReq carries.
        private static V2G_Message Request(Secc2 secc, BodyBaseType body)
            => new (new MessageHeaderType(secc.SessionId, Notification: null, Signature: null),
                    new BodyType(body));

        /// <summary>An AC session run up to (and including) ChargeParameterDiscovery. AC on purpose: DC
        /// inserts CableCheck/PreCharge between the offer and PowerDelivery, and neither has anything to do
        /// with what is being tested here.</summary>
        private static (Secc2 Secc, ChargeParameterDiscoveryResType Offer) SeccAtPowerOn(Secc2 secc)
        {

            secc.Handle(Request(secc, new SessionSetupReqType(EVCCID: new Byte[] { 0xAB, 0xCD, 0xEF, 0x01, 0x02, 0x03 })));
            secc.Handle(Request(secc, new ServiceDiscoveryReqType(ServiceScope: null, ServiceCategory: null)));
            secc.Handle(Request(secc, new PaymentServiceSelectionReqType(
                                    PaymentOption.ExternalPayment,
                                    new SelectedServiceListType(new[] { new SelectedServiceType(ServiceID: 1, ParameterSetID: null) }))));
            secc.Handle(Request(secc, new AuthorizationReqType(Id: null, GenChallenge: null)));

            var discovery = secc.Handle(Request(secc, new ChargeParameterDiscoveryReqType(
                                            MaxEntriesSAScheduleTuple: 4,
                                            EnergyTransferMode.AC_three_phase_core,
                                            new AC_EVChargeParameterType(
                                                DepartureTime:  null,
                                                EAmount:        PhysicalValue.Of(20_000, UnitSymbol.Wh),
                                                EVMaxVoltage:   PhysicalValue.Of(   230, UnitSymbol.V),
                                                EVMaxCurrent:   PhysicalValue.Of(    32, UnitSymbol.A),
                                                EVMinCurrent:   PhysicalValue.Of(     6, UnitSymbol.A)))));

            return (secc, (ChargeParameterDiscoveryResType) discovery.Body.BodyElement!);

        }

        private static V2G_Message PowerDeliveryAt(Secc2 secc, Int32 watts)
            => secc.Handle(Request(secc, new PowerDeliveryReqType(
                               ChargeProgress.Start,
                               SAScheduleTupleID: 1,
                               new ChargingProfileType(new[]
                               {
                                   new ProfileEntryType(ChargingProfileEntryStart: 0,
                                                        PhysicalValue.Of(watts, UnitSymbol.W),
                                                        ChargingProfileEntryMaxNumberOfPhasesInUse: null),
                               }),
                               EVPowerDeliveryParameter: null)));

        [Test]
        public void An_overridden_offer_is_what_the_EV_is_shown()
        {

            var (_, offer) = SeccAtPowerOn(new BackedSecc2(PowerMode.Ac, TimeSpan.FromSeconds(30), TimeProvider.System, BackendPMax));

            var pmax = ((SAScheduleListType) offer.SASchedules!).SAScheduleTuple[0].PMaxSchedule.PMaxScheduleEntry[0].PMax;

            Assert.That(pmax.ToDecimal(), Is.EqualTo((Decimal) BackendPMax));

        }

        [Test]
        public void The_offer_is_also_what_the_EVs_profile_is_measured_against()
        {

            // 8 kW: comfortably under the station's own 11.04 kW, comfortably over the 5 kW granted. The
            // unchanged machine would wave it through, and the car would draw more than was granted.
            var (secc, _) = SeccAtPowerOn(new BackedSecc2(PowerMode.Ac, TimeSpan.FromSeconds(30), TimeProvider.System, BackendPMax));

            var powerDelivery = PowerDeliveryAt(secc, 8_000);

            Assert.Multiple(() => {

                Assert.That(((PowerDeliveryResType) powerDelivery.Body.BodyElement!).ResponseCode,
                            Is.EqualTo(ResponseCode.FAILED_ChargingProfileInvalid));

                Assert.That(secc.ChargingProfileCheck!.WithinPMax, Is.False);

                Assert.That(8_000, Is.LessThan(StationPMax),
                            "precondition: the same profile is within what this station offers unaided");

            });

        }

        [Test]
        public void A_station_that_overrides_nothing_offers_what_it_always_did()
        {

            var (secc, offer) = SeccAtPowerOn(new Secc2(PowerMode.Ac, TimeSpan.FromSeconds(30), TimeProvider.System));

            var powerDelivery = PowerDeliveryAt(secc, 8_000);

            Assert.Multiple(() => {

                Assert.That(((SAScheduleListType) offer.SASchedules!).SAScheduleTuple[0].PMaxSchedule.PMaxScheduleEntry[0].PMax.ToDecimal(),
                            Is.EqualTo((Decimal) StationPMax));

                Assert.That(((PowerDeliveryResType) powerDelivery.Body.BodyElement!).ResponseCode,
                            Is.EqualTo(ResponseCode.OK));

            });

        }

    }

}
