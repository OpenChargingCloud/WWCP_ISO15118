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
using cloud.charging.open.protocols.ISO15118.StateMachines.Iso20;
using cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated;

namespace cloud.charging.open.protocols.ISO15118.Tests.StateMachines;

/// <summary>
/// The station's half of the `-20` schedule anchor: our <see cref="Secc20Base"/> offers its schedule
/// <em>now</em> in SECC time, and our car, which follows the offer, then projects the power the offer
/// allows instead of zero.
/// </summary>
/// <remarks>
/// Until 2026-11-27 every anchor in <c>ScheduleExchangeRes</c> was 0. By <c>[V2G20-1811]</c> such a
/// schedule ended in 1970 and offers zero power at any present time, so the car corrected that day could
/// only answer it with a 0-W profile. A whole DC session in memory, station and car of ours, with the
/// station's clock pinned.
/// </remarks>
[TestFixture]
public class OfferedScheduleAnchorTests
{

    /// <summary>2026-01-01T00:00:00Z.</summary>
    private const UInt64 Now = 1_767_225_600;

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.FromUnixTimeSeconds((Int64) Now);
    }

    /// <summary>A station of ours that remembers what it offered and what the car asked for.</summary>
    private sealed class Watched(TimeProvider Clock) : Secc20Dc(TimeSpan.FromSeconds(60), Clock)
    {

        public ScheduleExchangeRes? Offer     { get; private set; }
        public PowerDeliveryReq?    PowerOnBy { get; private set; }

        public override (MessageSet Set, object Response) Handle(MessageSet Set, object Request)
        {
            if (Request is PowerDeliveryReq { ChargeProgress: ChargeProgress.Start } start)
                PowerOnBy = start;

            var answer = base.Handle(Set, Request);

            if (answer.Response is ScheduleExchangeRes offer)
                Offer = offer;

            return answer;
        }

    }

    private static async Task<Watched> RunSession()
    {

        var clock                    = new FixedClock();
        var (vehicleEnd, stationEnd) = InMemoryStream.Pair();
        var station                  = new Watched(clock);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var vehicle = new Evcc20Dc(vehicleEnd, clock, new NoDelay(), TimeSpan.FromSeconds(5));
        var running = station.RunAsync(stationEnd, timeout.Token);

        await vehicle.RunAsync(timeout.Token);
        vehicleEnd.Dispose();
        await running;

        return station;

    }


    [Test]
    public async Task TheOfferedScheduleStartsNow_InEachAnchorsOwnUnit()
    {

        var offer = (await RunSession()).Offer!;
        var tuple = offer.Scheduled_SEResControlMode!.ScheduleTuple.Single();

        Assert.Multiple(() => {
            Assert.That(offer.Header.TimeStamp, Is.EqualTo(Now), "the response states SECC time");
            Assert.That(tuple.ChargingSchedule.PowerSchedule.TimeAnchor,        Is.EqualTo(Now * 1_000),
                        "Table 112: a PowerSchedule's TimeAnchor is in milliseconds");
            Assert.That(tuple.ChargingSchedule.PriceLevelSchedule!.TimeAnchor,  Is.EqualTo(Now * 1_000_000),
                        "Table 156: a PriceLevelSchedule's TimeAnchor is in microseconds");
        });

    }

    [Test]
    public async Task TheCarThenProjectsWhatTheOfferAllows_NotZero()
    {

        var station = await RunSession();
        var profile = station.PowerOnBy!.EVPowerProfile!;
        var slot    = station.Offer!.Scheduled_SEResControlMode!.ScheduleTuple.Single()
                             .ChargingSchedule.PowerSchedule.PowerScheduleEntries.PowerScheduleEntry.Single();
        var entry   = profile.EVPowerProfileEntries.EVPowerProfileEntry.Single();

        Assert.Multiple(() => {
            Assert.That(profile.TimeAnchor, Is.EqualTo(Now * 1_000_000), "anchored at the offer's start");
            Assert.That(entry.Duration,     Is.EqualTo(slot.Duration),   "the whole offered slot, from now");
            Assert.That(Iso20EvPowerProfile.Watts(entry.Power), Is.EqualTo(Iso20EvPowerProfile.Watts(slot.Power)),
                        "the slot allows less than the car wants, so the car asks for the slot's power");
            Assert.That(Iso20EvPowerProfile.Watts(entry.Power), Is.GreaterThan(0),
                        "an offer anchored at 1970 left the car only zero");
        });

    }

}
