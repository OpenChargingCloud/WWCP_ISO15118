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

using System.Security.Cryptography;

using NUnit.Framework;

using cloud.charging.open.protocols.ISO15118.Metering;
using cloud.charging.open.protocols.ISO15118.Simulation;
using cloud.charging.open.protocols.ISO15118.StateMachines.Iso20;
using cloud.charging.open.protocols.ISO15118.Timing;

namespace cloud.charging.open.protocols.ISO15118.Tests.StateMachines;

/// <summary>
/// What a vehicle learns about a station's clock and meter over one `-20` DC session: the offset of
/// the station's clock from the first response on, and every meter reading paired with the vehicle's
/// own count of the same iteration.
/// </summary>
[TestFixture]
public class TransparencyTests
{

    /// <summary>A clock that is a fixed amount ahead of, or behind, the system's.</summary>
    private sealed class SkewedClock(TimeSpan Skew) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => System.GetUtcNow() + Skew;
    }

    #region (private static) RunSession(StationClock, Ask)

    private static async Task<Evcc20Dc> RunSession(TimeProvider StationClock, Boolean Ask = true)
    {

        var (vehicleEnd, stationEnd) = InMemoryStream.Pair();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var station  = new Secc20Dc(TimeSpan.FromSeconds(60), StationClock);
        var vehicle  = new Evcc20Dc(vehicleEnd, TimeProvider.System, new NoDelay(), TimeSpan.FromSeconds(5)) {
                           RequestMeterInfo = Ask
                       };

        var running  = station.RunAsync(stationEnd, timeout.Token);
        await vehicle.RunAsync(timeout.Token);
        await running;

        return vehicle;

    }

    #endregion


    #region TheOffsetAllowsForWhatAWholeSecondHides()

    [Test]
    public void TheOffsetAllowsForWhatAWholeSecondHides()
    {

        var sent      = new DateTimeOffset(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);
        var received  = sent + TimeSpan.FromMilliseconds(200);

        // The peer read 12:00:00.9 and stamped 12:00:00: nothing is wrong with either clock.
        var sample    = new ClockSample(sent, received, sent);

        Assert.Multiple(() => {
            Assert.That(sample.Offset,                 Is.EqualTo(TimeSpan.FromMilliseconds(400)));
            Assert.That(sample.Uncertainty,            Is.EqualTo(TimeSpan.FromMilliseconds(600)));
            Assert.That(sample.Offset.Duration(),      Is.LessThanOrEqualTo(sample.Uncertainty));
        });

    }

    #endregion

    #region ATimestampThatIsNoTimeIsCountedAndNotCompared()

    [Test]
    public void ATimestampThatIsNoTimeIsCountedAndNotCompared()
    {

        var clock = new ClockComparison();
        var now   = DateTimeOffset.UtcNow;

        Assert.Multiple(() => {
            Assert.That(clock.Note(now, now, UInt64.MaxValue), Is.Null);
            Assert.That(clock.Unreadable,                      Is.EqualTo(1));
            Assert.That(clock.Samples,                         Is.Zero);
            Assert.That(clock.Agrees(TimeSpan.FromSeconds(5)), Is.Null);
        });

    }

    #endregion

    #region TheStationsClockIsKnownBeforeAnythingIsAuthorised()

    [Test]
    public async Task TheStationsClockIsKnownBeforeAnythingIsAuthorised()
    {

        var vehicle = await RunSession(TimeProvider.System);

        Assert.Multiple(() => {
            // One sample per response, so the first is SessionSetupRes.
            Assert.That(vehicle.StationClock.Samples,                         Is.EqualTo(vehicle.Exchanges));
            Assert.That(vehicle.StationClock.Agrees(TimeSpan.FromSeconds(2)), Is.True);
            Assert.That(vehicle.StationClock.Drift,                           Is.Not.Null);
        });

    }

    #endregion

    #region AStationThatIsNinetySecondsAheadIsSeenToBe()

    [Test]
    public async Task AStationThatIsNinetySecondsAheadIsSeenToBe()
    {

        var vehicle = await RunSession(new SkewedClock(TimeSpan.FromSeconds(90)));
        var first   = vehicle.StationClock.First!.Value;

        Assert.Multiple(() => {
            Assert.That(first.Offset.TotalSeconds,                            Is.EqualTo(90).Within(first.Uncertainty.TotalSeconds + 1));
            Assert.That(vehicle.StationClock.Agrees(TimeSpan.FromSeconds(5)), Is.False);
        });

    }

    #endregion

    #region EveryReadingIsPairedWithTheVehiclesCountOfTheSameIteration()

    [Test]
    public async Task EveryReadingIsPairedWithTheVehiclesCountOfTheSameIteration()
    {

        var seen    = new List<StationReading>();

        var (vehicleEnd, stationEnd) = InMemoryStream.Pair();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // A station reports a reading only when it has a meter, and this stack's meter signs.
        var station = new Secc20Dc(TimeSpan.FromSeconds(60), TimeProvider.System) {
                          InstalledMeter = new SigningMeter("VAN*M1", ECDsa.Create(ECCurve.NamedCurves.nistP256), TimeProvider.System)
                      };
        var vehicle = new Evcc20Dc(vehicleEnd, TimeProvider.System, new NoDelay(), TimeSpan.FromSeconds(5)) {
                          RequestMeterInfo = true,
                          OnStationReading = seen.Add
                      };

        var running = station.RunAsync(stationEnd, timeout.Token);
        await vehicle.RunAsync(timeout.Token);
        await running;

        Assert.Multiple(() => {

            Assert.That(vehicle.StationReadings,       Is.Not.Empty);
            Assert.That(vehicle.StationReadings.Count, Is.EqualTo(vehicle.MeterInfoResponses));
            Assert.That(seen,                          Is.EqualTo(vehicle.StationReadings));

            // Both ends book the same declared period at the power they each know, so a clean
            // session lands on the same watt-hour at every reading.
            foreach (var reading in vehicle.StationReadings)
                Assert.That(reading.DifferenceWh, Is.Zero, $"sample {reading.Sample}");

            Assert.That(vehicle.StationReadings[^1].Sample, Is.EqualTo(vehicle.Meter.Samples));

            // The meter's own clock is a second clock, and it was read as well.
            Assert.That(vehicle.StationReadings,                            Has.All.Matches<StationReading>(reading => reading.Signed && reading.MeterTime is not null));
            Assert.That(vehicle.MeterClock.Samples,                         Is.EqualTo(vehicle.StationReadings.Count));
            Assert.That(vehicle.MeterClock.Agrees(TimeSpan.FromSeconds(2)), Is.True);

        });

    }

    #endregion

    #region AStationWithoutAMeterLeavesNothingToCompare()

    [Test]
    public async Task AStationWithoutAMeterLeavesNothingToCompare()
    {

        var vehicle = await RunSession(TimeProvider.System, Ask: false);

        // No meter at this station, so nothing to keep - and nothing invented either.
        Assert.Multiple(() => {
            Assert.That(vehicle.StationReadings.Count, Is.EqualTo(vehicle.MeterInfoResponses));
            Assert.That(vehicle.MeterClock.Samples,    Is.Zero);
        });

    }

    #endregion

}
