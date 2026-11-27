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
using System.Security.Cryptography.X509Certificates;

using NUnit.Framework;

using cloud.charging.open.protocols.ISO15118.Metering;
using cloud.charging.open.protocols.ISO15118.Simulation;
using cloud.charging.open.protocols.ISO15118.StateMachines;
using cloud.charging.open.protocols.ISO15118.StateMachines.Iso2;
using cloud.charging.open.protocols.ISO15118.StateMachines.Iso20;

namespace cloud.charging.open.protocols.ISO15118.Tests.StateMachines;

/// <summary>
/// The -2 receipt request at a station with a signing meter, both ends in-process. Such a station
/// reports a reading in every status response. With <see cref="Secc2.DemandReceiptWithoutMeterInfo"/>
/// its second receipt request still carries none, the shape EVerest 2026.10.0 sends once its one-shot
/// <c>meter_info_is_used</c> flag is spent.
/// </summary>
/// <remarks>Until 2026-11-28 the installed meter put a reading into that request too, so the switch had
/// no effect at a metered station.</remarks>
[TestFixture]
public class Iso2ReceiptRequestTests
{

    #region (private static) RunPncSession(Mode, WithoutMeterInfo)

    /// <summary>A whole -2 Plug &amp; Charge session against a metered station; both ends as they ended it.</summary>
    private static async Task<(Evcc2 Vehicle, Secc2 Station)> RunPncSession(PowerMode  Mode,
                                                                            Boolean    WithoutMeterInfo)
    {

        using var contractKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var contract    = new CertificateRequest("CN=UKSWI123456791A", contractKey, HashAlgorithmName.SHA256)
                                    .CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddHours(1));

        var (vehicleEnd, stationEnd) = InMemoryStream.Pair();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var station = new Secc2(Mode, TimeSpan.FromSeconds(60), TimeProvider.System) {
                          InstalledMeter                = new SigningMeter("VAN*M1", ECDsa.Create(ECCurve.NamedCurves.nistP256), TimeProvider.System),
                          DemandReceiptWithoutMeterInfo = WithoutMeterInfo
                      };
        var vehicle = new Evcc2(vehicleEnd, Mode, TimeProvider.System, new NoDelay(), TimeSpan.FromSeconds(5)) {
                          Pnc = new PncEvccOptions(contract.RawData, [contract.RawData], contractKey)
                      };

        var running = station.RunAsync(stationEnd, timeout.Token);
        await vehicle.RunAsync(timeout.Token);
        vehicleEnd.Dispose();
        await running;

        return (vehicle, station);

    }

    #endregion


    #region AMeteredStationsSecondRequestCarriesNoMeterInfo(Mode)

    [TestCase(PowerMode.Ac)]
    [TestCase(PowerMode.Dc)]
    public async Task AMeteredStationsSecondRequestCarriesNoMeterInfo(PowerMode Mode)
    {

        var (vehicle, station) = await RunPncSession(Mode, WithoutMeterInfo: true);

        Assert.Multiple(() => {
            Assert.That(station.IsDone,                        Is.True, "the session reaches SessionStop");
            Assert.That(vehicle.MeteringReceiptsSent,          Is.EqualTo(2));
            Assert.That(vehicle.ReceiptsOverPreviousMeterInfo, Is.EqualTo(1),
                        "the second request came without a reading, although the station has a meter");
            Assert.That(station.MeteringReceipts,              Has.Count.EqualTo(2));
            Assert.That(station.MeteringReceipts,              Has.All.Matches<Iso2ReceiptResult>(receipt => receipt.DigestOk && receipt.SignatureOk));
        });

    }

    #endregion

    #region WithoutTheSwitchAMeteredStationAsksOnceWithAReading(Mode)

    [TestCase(PowerMode.Ac)]
    [TestCase(PowerMode.Dc)]
    public async Task WithoutTheSwitchAMeteredStationAsksOnceWithAReading(PowerMode Mode)
    {

        var (vehicle, station) = await RunPncSession(Mode, WithoutMeterInfo: false);

        Assert.Multiple(() => {
            Assert.That(station.IsDone,                        Is.True);
            Assert.That(vehicle.MeteringReceiptsSent,          Is.EqualTo(1));
            Assert.That(vehicle.ReceiptsOverPreviousMeterInfo, Is.Zero);
            Assert.That(station.MeteringReceipts,              Has.All.Matches<Iso2ReceiptResult>(receipt => receipt.DigestOk && receipt.SignatureOk));
        });

    }

    #endregion

}
