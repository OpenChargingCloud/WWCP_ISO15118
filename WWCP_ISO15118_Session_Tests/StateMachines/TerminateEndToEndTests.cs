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

using cloud.charging.open.protocols.ISO15118.Simulation;
using cloud.charging.open.protocols.ISO15118.StateMachines.Iso20;
using cloud.charging.open.protocols.ISO15118.Timing;

namespace cloud.charging.open.protocols.ISO15118.Tests.StateMachines;

/// <summary>
/// A whole `-20` MCS session, vehicle and station on the two ends of one in-memory connection: told
/// Terminate by the station, the vehicle leaves the charge loop after the iteration that said so,
/// stops power delivery and ends the session for good; not told, it charges as it always did.
/// </summary>
[TestFixture]
public class TerminateEndToEndTests
{

    #region (private static) RunSession(Station)

    /// <summary>
    /// A whole MCS session between a vehicle and the given station, and the vehicle as it ended it.
    /// </summary>
    private static async Task<Evcc20Mcs> RunSession(Secc20Mcs Station)
    {

        var (vehicleEnd, stationEnd) = InMemoryStream.Pair();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var vehicle   = new Evcc20Mcs(vehicleEnd, TimeProvider.System, new NoDelay(), TimeSpan.FromSeconds(5)) {
                            ChargeLoopMsgTimeout = TimeSpan.FromSeconds(5)
                        };

        var station   = Station.RunAsync(stationEnd, timeout.Token);

        await vehicle.RunAsync(timeout.Token);
        vehicleEnd.Dispose();

        await station;

        return vehicle;

    }

    #endregion


    [Test]
    public async Task NotToldTheVehicleChargesAsItAlwaysDid()
    {

        var station = new Secc20Mcs(TimeSpan.FromSeconds(60), TimeProvider.System);
        var vehicle = await RunSession(station);

        Assert.Multiple(() => {
            Assert.That(vehicle.TerminatedByStation, Is.False);
            Assert.That(vehicle.Meter.Energy,        Is.GreaterThan(0), "three iterations of current");
            Assert.That(station.IsDone,              Is.True);
            Assert.That(station.Paused,              Is.False);
        });

    }

    [Test]
    public async Task ToldTerminateTheVehicleEndsTheChargingForGood()
    {

        var station = new Secc20Mcs(TimeSpan.FromSeconds(60), TimeProvider.System);
        station.Terminate();

        // Asked to pause at the end - which the station's Terminate overrides.
        var (vehicleEnd, stationEnd) = InMemoryStream.Pair();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var vehicle = new Evcc20Mcs(vehicleEnd, TimeProvider.System, new NoDelay(), TimeSpan.FromSeconds(5)) {
                          ChargeLoopMsgTimeout = TimeSpan.FromSeconds(5),
                          StopMode             = cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated.ChargingSession.Pause
                      };

        var running = station.RunAsync(stationEnd, timeout.Token);
        await vehicle.RunAsync(timeout.Token);
        vehicleEnd.Dispose();
        await running;

        Assert.Multiple(() => {
            Assert.That(vehicle.TerminatedByStation, Is.True);
            Assert.That(vehicle.Meter.Energy,        Is.EqualTo(0), "the station served nothing once it said Terminate");
            Assert.That(station.IsDone,              Is.True);
            Assert.That(station.Paused,              Is.False, "a Terminate is not a pause");
        });

    }

    [Test]
    public async Task ABatteryToldTerminateSaysWhy()
    {

        var station = new Secc20Mcs(TimeSpan.FromSeconds(60), TimeProvider.System);
        station.Terminate();

        var (vehicleEnd, stationEnd) = InMemoryStream.Pair();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var vehicle = new Evcc20Mcs(vehicleEnd, TimeProvider.System, new NoDelay(), TimeSpan.FromSeconds(5)) {
                          ChargeLoopMsgTimeout = TimeSpan.FromSeconds(5),
                          Battery              = new EvBattery(capacityKWh: 500, startSoCPercent: 20)
                      };

        var running = station.RunAsync(stationEnd, timeout.Token);
        await vehicle.RunAsync(timeout.Token);
        vehicleEnd.Dispose();
        await running;

        Assert.That(vehicle.BatteryStop, Is.EqualTo(ChargeStop.StationTerminated));

    }

}
