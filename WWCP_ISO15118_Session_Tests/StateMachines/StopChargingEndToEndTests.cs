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

using cloud.charging.open.protocols.ISO15118_2.Generated;
using cloud.charging.open.protocols.ISO15118.Simulation;
using cloud.charging.open.protocols.ISO15118.StateMachines;
using cloud.charging.open.protocols.ISO15118.StateMachines.Iso2;

namespace cloud.charging.open.protocols.ISO15118.Tests.StateMachines;

/// <summary>
/// A whole ISO 15118-2 session, AC and DC, vehicle and station on the two ends of one in-memory
/// connection: told StopCharging by the station - the -2 counterpart of -20's Terminate - the vehicle
/// leaves the charge loop after the iteration that said so, stops power delivery and ends the session
/// for good; not told, it charges as it always did.
/// </summary>
[TestFixture]
public class StopChargingEndToEndTests
{

    #region (private static) RunSession(Station, Mode, Vehicle)

    /// <summary>
    /// A whole -2 session between a vehicle, set up as given, and the given station; the vehicle as it
    /// ended it.
    /// </summary>
    private static async Task<Evcc2> RunSession(Secc2           Station,
                                                PowerMode       Mode,
                                                Action<Evcc2>?  Vehicle = null)
    {

        var (vehicleEnd, stationEnd) = InMemoryStream.Pair();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var vehicle = new Evcc2(vehicleEnd, Mode, TimeProvider.System, new NoDelay(), TimeSpan.FromSeconds(5));
        Vehicle?.Invoke(vehicle);

        var station = Station.RunAsync(stationEnd, timeout.Token);

        await vehicle.RunAsync(timeout.Token);
        vehicleEnd.Dispose();

        await station;

        return vehicle;

    }

    #endregion


    [TestCase(PowerMode.Dc)]
    [TestCase(PowerMode.Ac)]
    public async Task NotToldTheVehicleChargesAsItAlwaysDid(PowerMode Mode)
    {

        var station = new Secc2(Mode, TimeSpan.FromSeconds(60), TimeProvider.System);
        var battery = new EvBattery(capacityKWh: 500, startSoCPercent: 20) { MaxIterations = 3 };
        var vehicle = await RunSession(station, Mode, v => v.Battery = battery);

        Assert.Multiple(() => {
            Assert.That(vehicle.TerminatedByStation, Is.False);
            Assert.That(vehicle.BatteryStop,         Is.EqualTo(ChargeStop.LoopLimit));
            Assert.That(battery.Iterations,          Is.EqualTo(3));
            Assert.That(station.IsDone,              Is.True);
        });

    }

    [TestCase(PowerMode.Dc)]
    [TestCase(PowerMode.Ac)]
    public async Task ToldStopChargingTheVehicleEndsTheChargingForGood(PowerMode Mode)
    {

        var station = new Secc2(Mode, TimeSpan.FromSeconds(60), TimeProvider.System);
        station.StopCharging();

        // Asked to pause at the end - which the station's StopCharging overrides.
        var battery = new EvBattery(capacityKWh: 500, startSoCPercent: 20);
        var vehicle = await RunSession(station, Mode, v => {
                                                          v.Battery  = battery;
                                                          v.StopMode = ChargingSession.Pause;
                                                      });

        Assert.Multiple(() => {
            Assert.That(vehicle.TerminatedByStation, Is.True);
            Assert.That(vehicle.BatteryStop,         Is.EqualTo(ChargeStop.StationTerminated));
            Assert.That(battery.Iterations,          Is.EqualTo(1), "the loop ends after the iteration that said StopCharging");
            Assert.That(station.IsDone,              Is.True);
            Assert.That(station.SequenceErrorAt,     Is.Null);
            Assert.That(station.Paused,              Is.False, "a StopCharging is not a pause");
        });

    }

    [Test]
    public async Task ToldStopChargingWithoutABatteryTheVehicleStopsAfterOneIteration()
    {

        var station = new Secc2(PowerMode.Dc, TimeSpan.FromSeconds(60), TimeProvider.System);
        station.StopCharging();

        var vehicle = await RunSession(station, PowerMode.Dc);

        Assert.Multiple(() => {
            Assert.That(vehicle.TerminatedByStation, Is.True);
            Assert.That(vehicle.BatteryStop,         Is.Null, "no battery, no reason of one");
            Assert.That(station.IsDone,              Is.True);
            Assert.That(station.Paused,              Is.False);
        });

    }

    [Test]
    public async Task StopChargingGoesBeforeARenegotiation()
    {

        var station = new Secc2(PowerMode.Dc, TimeSpan.FromSeconds(60), TimeProvider.System) {
                          RequestRenegotiation = true
                      };
        station.StopCharging();

        var vehicle = await RunSession(station, PowerMode.Dc);

        Assert.Multiple(() => {
            Assert.That(vehicle.TerminatedByStation, Is.True);
            Assert.That(vehicle.Renegotiations,      Is.Zero, "nothing is renegotiated in a charging told to stop");
            Assert.That(station.Renegotiations,      Is.Zero);
            Assert.That(station.IsDone,              Is.True);
        });

    }

}
