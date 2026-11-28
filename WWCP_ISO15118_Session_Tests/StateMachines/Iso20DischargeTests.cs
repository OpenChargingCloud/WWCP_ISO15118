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

namespace cloud.charging.open.protocols.ISO15118.Tests.StateMachines;

/// <summary>
/// A -20 station that discharges the vehicle, both ends in-process. Negative is energy from the vehicle
/// to the station (`[V2G20-1034]`, `[V2G20-1035]`). DC: the station serves a negative current. AC: it asks
/// with a negative <c>EVSETargetActivePower</c>, and the vehicle follows (`[V2G20-1825]`).
/// </summary>
/// <remarks>Added 2026-11-28 with <see cref="Secc20Dc.DischargeCurrent_A"/> and
/// <see cref="Secc20Ac.DischargePowerW"/>: before them no station of this stack moved energy back.</remarks>
[TestFixture]
public class Iso20DischargeTests
{

    #region (private static) Meter()

    private static SigningMeter Meter()
        => new ("VAN*M1", ECDsa.Create(ECCurve.NamedCurves.nistP256), TimeProvider.System);

    #endregion

    #region (private static) Run(Station, Vehicle)

    private static async Task Run(Secc20Base Station, Evcc20Base Vehicle, Stream StationEnd)
    {

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var running = Station.RunAsync(StationEnd, timeout.Token);
        await Vehicle.RunAsync(timeout.Token);
        await running;

    }

    #endregion


    #region ADcStationDischargesTheVehicle()

    [Test]
    public async Task ADcStationDischargesTheVehicle()
    {

        var (vehicleEnd, stationEnd) = InMemoryStream.Pair();
        var meter   = Meter();
        var station = new Secc20Dc(TimeSpan.FromSeconds(60), TimeProvider.System) {
                          InstalledMeter     = meter,
                          DischargeCurrent_A = 100
                      };
        var vehicle = new Evcc20Dc(vehicleEnd, TimeProvider.System, new NoDelay(), TimeSpan.FromSeconds(5)) {
                          PreferBidirectionalService = true,
                          PreferDynamicControlMode   = true
                      };

        await Run(station, vehicle, stationEnd);

        var (charged, discharged, _) = meter.ReadBoth();

        Assert.Multiple(() => {
            Assert.That(vehicle.SelectedEnergyServiceId, Is.EqualTo(EnergyTransferService.DC_BPT));
            Assert.That(vehicle.Meter.Energy,            Is.Negative, "the vehicle counted energy in, not out");
            Assert.That(charged,                         Is.Zero);
            Assert.That(discharged,                      Is.GreaterThan(0UL), "the meter's discharge register did not move");
            Assert.That(vehicle.Meter.Energy,            Is.EqualTo((double) charged - discharged),
                        "the vehicle's count and the meter's two registers disagree");
        });

    }

    #endregion

    #region WithoutTheOptionTheSameSessionCharges()

    /// <summary>The control: the same bidirectional Dynamic session at a station not told to discharge
    /// charges, as every session before this option did.</summary>
    [Test]
    public async Task WithoutTheOptionTheSameSessionCharges()
    {

        var (vehicleEnd, stationEnd) = InMemoryStream.Pair();
        var meter   = Meter();
        var station = new Secc20Dc(TimeSpan.FromSeconds(60), TimeProvider.System) { InstalledMeter = meter };
        var vehicle = new Evcc20Dc(vehicleEnd, TimeProvider.System, new NoDelay(), TimeSpan.FromSeconds(5)) {
                          PreferBidirectionalService = true,
                          PreferDynamicControlMode   = true
                      };

        await Run(station, vehicle, stationEnd);

        var (charged, discharged, _) = meter.ReadBoth();

        Assert.Multiple(() => {
            Assert.That(vehicle.Meter.Energy, Is.Positive);
            Assert.That(charged,              Is.GreaterThan(0UL));
            Assert.That(discharged,           Is.Zero);
        });

    }

    #endregion

    #region AnAcVehicleFollowsTheStationsAskForEnergyBack()

    [Test]
    public async Task AnAcVehicleFollowsTheStationsAskForEnergyBack()
    {

        var (vehicleEnd, stationEnd) = InMemoryStream.Pair();
        var meter   = Meter();
        var station = new Secc20Ac(TimeSpan.FromSeconds(60), TimeProvider.System) {
                          InstalledMeter  = meter,
                          DischargePowerW = 11_000
                      };
        var vehicle = new Evcc20Ac(vehicleEnd, TimeProvider.System, new NoDelay(), TimeSpan.FromSeconds(5)) {
                          PreferBidirectionalService = true,
                          PreferDynamicControlMode   = true
                      };

        await Run(station, vehicle, stationEnd);

        var (charged, discharged, _) = meter.ReadBoth();

        // The first loop is answered before the vehicle has seen an ask, so it charges; it gives back
        // from the second on, at half the power it charged at.
        Assert.Multiple(() => {
            Assert.That(vehicle.SelectedEnergyServiceId, Is.EqualTo(EnergyTransferService.AC_BPT));
            Assert.That(charged,                         Is.GreaterThan(0UL), "the first loop charges");
            Assert.That(discharged,                      Is.GreaterThan(0UL), "the vehicle never followed the ask");
            Assert.That(vehicle.Meter.Energy,            Is.EqualTo((double) charged - discharged),
                        "the vehicle's count and the meter's two registers disagree");
        });

    }

    #endregion

    #region ABidirectionalMetersSignatureCoversBothRegisters()

    /// <summary>Layout 2 of <see cref="MeterSigningPayload"/>: the signature covers the discharge register
    /// too, so a reading with the register stripped or changed does not verify.</summary>
    [Test]
    public void ABidirectionalMetersSignatureCoversBothRegisters()
    {

        var meter     = Meter();
        var session   = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var signature = meter.Sign(20, session, 4200, discharged: 1300, timestamp: 1_700_000_000);

        Assert.Multiple(() => {
            Assert.That(SigningMeter.Verify(meter.PublicKey, 20, session, meter.MeterId, 4200, 1300UL, 1_700_000_000, signature), Is.True);
            Assert.That(SigningMeter.Verify(meter.PublicKey, 20, session, meter.MeterId, 4200,          1_700_000_000, signature), Is.False,
                        "the register stripped");
            Assert.That(SigningMeter.Verify(meter.PublicKey, 20, session, meter.MeterId, 4200, 1299UL, 1_700_000_000, signature), Is.False,
                        "another discharged figure");
            Assert.That(() => MeterSigningPayload.Build(2, session, meter.MeterId, 4200, 1300UL, null),
                        Throws.ArgumentException, "-2 has no discharge register");
        });

    }

    #endregion

}
