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
using cloud.charging.open.protocols.ISO15118.Session;
using cloud.charging.open.protocols.ISO15118.Simulation;
using cloud.charging.open.protocols.ISO15118.StateMachines.Iso20;
using cloud.charging.open.protocols.ISO15118.Timing;
using cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated;

namespace cloud.charging.open.protocols.ISO15118.Tests.StateMachines;

/// <summary>
/// The -20 MeteringConfirmation exchange, both ends of it in-process: the station asks in a charge-loop
/// response, the vehicle signs what it was shown with its contract key and sends it on the side stream,
/// and the station checks it against what it asked and against the contract certificate.
/// </summary>
[TestFixture]
public class MeteringConfirmationTests
{

    #region (private static) Contract(SignWith)

    /// <summary>A self-signed P-256 contract certificate, and the key the vehicle is given to sign with -
    /// its own, unless <paramref name="SignWith"/> says otherwise.</summary>
    private static PncEvccOptions Contract(ECDsa? SignWith = null)
    {

        // The schema wants at least one sub-CA certificate, so the leaf is issued by one.
        using var caKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var caRequest   = new CertificateRequest("CN=MO Sub-CA", caKey, HashAlgorithmName.SHA256);
        caRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        using var ca    = caRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(2));

        var key         = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request     = new CertificateRequest("CN=DEABCC12345678", key, HashAlgorithmName.SHA256);
        using var leaf  = request.Create(ca, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1), [1, 2, 3, 4]);

        return new PncEvccOptions(leaf.RawData, [ca.RawData], SignWith ?? key);

    }

    #endregion

    #region (private static) Meter()

    private static SigningMeter Meter()
        => new ("VAN*M1", ECDsa.Create(ECCurve.NamedCurves.nistP256), TimeProvider.System);

    #endregion

    #region (private static) RunDc(Station, Vehicle)

    private static async Task RunDc(Secc20Dc Station, Evcc20Dc Vehicle, Stream StationEnd)
    {

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var running = Station.RunAsync(StationEnd, timeout.Token);
        await Vehicle.RunAsync(timeout.Token);
        await running;

    }

    #endregion


    #region APlugAndChargeVehicleSignsWhatTheStationAskedAbout()

    [Test]
    public async Task APlugAndChargeVehicleSignsWhatTheStationAskedAbout()
    {

        var (vehicleEnd, stationEnd) = InMemoryStream.Pair();

        var station = new Secc20Dc(TimeSpan.FromSeconds(60), TimeProvider.System) {
                          InstalledMeter            = Meter(),
                          MeteringConfirmationEvery = 1
                      };
        var vehicle = new Evcc20Dc(vehicleEnd, TimeProvider.System, new NoDelay(), TimeSpan.FromSeconds(5)) {
                          Pnc = Contract()
                      };

        await RunDc(station, vehicle, stationEnd);

        Assert.Multiple(() => {

            Assert.That(vehicle.AuthorizationMode,                Is.EqualTo("pnc-signed"));

            // Three charge-loop iterations, and never the first ([V2G20-1622]).
            Assert.That(station.MeteringConfirmationsRequested,   Is.EqualTo(2));
            Assert.That(station.MeteringConfirmations,            Has.Count.EqualTo(2));
            Assert.That(vehicle.MeteringConfirmations,            Has.Count.EqualTo(2));
            Assert.That(station.MeteringConfirmationsMissed,      Is.Zero);
            Assert.That(vehicle.MeteringConfirmationsImpossible,  Is.Zero);

            foreach (var check in station.MeteringConfirmations)
            {
                Assert.That(check.Valid,                          Is.True, check.ToString());
                Assert.That(check.Grammar,                        Is.EqualTo("xmldsig-standalone"));
                Assert.That(check.ResponseCode,                   Is.EqualTo(ResponseCode.OK));
            }

            Assert.That(vehicle.MeteringConfirmations.Select(sent => sent.ChargedWh),
                        Is.EqualTo(station.MeteringConfirmations.Select(check => check.ChargedWh)));

            Assert.That(vehicle.MeteringConfirmations,            Has.All.Matches<MeteringConfirmationSent>(sent => sent.ResponseCode == ResponseCode.OK &&
                                                                                                                       sent.MeterId      == "VAN*M1" &&
                                                                                                                      !sent.WithReceipt));

        });

    }

    #endregion

    #region TheSameOnAc()

    [Test]
    public async Task TheSameOnAc()
    {

        var (vehicleEnd, stationEnd) = InMemoryStream.Pair();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var station = new Secc20Ac(TimeSpan.FromSeconds(60), TimeProvider.System) {
                          InstalledMeter            = Meter(),
                          MeteringConfirmationEvery = 2
                      };
        var vehicle = new Evcc20Ac(vehicleEnd, TimeProvider.System, new NoDelay(), TimeSpan.FromSeconds(5)) {
                          Pnc = Contract()
                      };

        var running = station.RunAsync(stationEnd, timeout.Token);
        await vehicle.RunAsync(timeout.Token);
        await running;

        Assert.Multiple(() => {
            Assert.That(station.MeteringConfirmations,  Has.Count.EqualTo(1));
            Assert.That(station.MeteringConfirmations,  Has.All.Matches<MeteringConfirmationCheck>(check => check.Valid));
            Assert.That(vehicle.MeteringConfirmations,  Has.Count.EqualTo(1));
        });

    }

    #endregion

    #region AnEimVehicleIsNeverAsked()

    [Test]
    public async Task AnEimVehicleIsNeverAsked()
    {

        var (vehicleEnd, stationEnd) = InMemoryStream.Pair();

        var station = new Secc20Dc(TimeSpan.FromSeconds(60), TimeProvider.System) {
                          InstalledMeter            = Meter(),
                          MeteringConfirmationEvery = 1
                      };
        var vehicle = new Evcc20Dc(vehicleEnd, TimeProvider.System, new NoDelay(), TimeSpan.FromSeconds(5));

        await RunDc(station, vehicle, stationEnd);

        // [V2G20-2704]: there is no contract key to sign with.
        Assert.Multiple(() => {
            Assert.That(vehicle.AuthorizationMode,               Is.EqualTo("eim"));
            Assert.That(station.MeteringConfirmationsRequested,  Is.Zero);
            Assert.That(vehicle.MeteringConfirmations,           Is.Empty);
        });

    }

    #endregion

    #region AStationWithoutAMeterHasNothingToAskAbout()

    [Test]
    public async Task AStationWithoutAMeterHasNothingToAskAbout()
    {

        var (vehicleEnd, stationEnd) = InMemoryStream.Pair();

        var station = new Secc20Dc(TimeSpan.FromSeconds(60), TimeProvider.System) {
                          MeteringConfirmationEvery = 1
                      };
        var vehicle = new Evcc20Dc(vehicleEnd, TimeProvider.System, new NoDelay(), TimeSpan.FromSeconds(5)) {
                          Pnc = Contract()
                      };

        await RunDc(station, vehicle, stationEnd);

        // [V2G20-1083]: asking carries the reading.
        Assert.That(station.MeteringConfirmationsRequested, Is.Zero);

    }

    #endregion

    #region ASilentVehicleIsNotAskedAgainWithinTheDelay()

    [Test]
    public async Task ASilentVehicleIsNotAskedAgainWithinTheDelay()
    {

        var (vehicleEnd, stationEnd) = InMemoryStream.Pair();

        var station = new Secc20Dc(TimeSpan.FromSeconds(60), TimeProvider.System) {
                          InstalledMeter            = Meter(),
                          MeteringConfirmationEvery = 1
                      };
        var vehicle = new Evcc20Dc(vehicleEnd, TimeProvider.System, new NoDelay(), TimeSpan.FromSeconds(5)) {
                          Pnc             = Contract(),
                          ConfirmMetering = false
                      };

        await RunDc(station, vehicle, stationEnd);

        // [V2G20-2105]: asked at the second response, and not again at the third, which comes well
        // inside the five seconds the first one allowed.
        Assert.Multiple(() => {
            Assert.That(station.MeteringConfirmationsRequested,  Is.EqualTo(1));
            Assert.That(station.MeteringConfirmations,           Is.Empty);
            Assert.That(station.MeteringConfirmationsMissed,     Is.Zero);
        });

    }

    #endregion

    #region ASignatureByAnotherKeyIsRecordedAsSuch()

    [Test]
    public async Task ASignatureByAnotherKeyIsRecordedAsSuch()
    {

        var (vehicleEnd, stationEnd) = InMemoryStream.Pair();

        var station = new Secc20Dc(TimeSpan.FromSeconds(60), TimeProvider.System) {
                          InstalledMeter            = Meter(),
                          MeteringConfirmationEvery = 1
                      };
        var vehicle = new Evcc20Dc(vehicleEnd, TimeProvider.System, new NoDelay(), TimeSpan.FromSeconds(5)) {
                          Pnc = Contract(SignWith: ECDsa.Create(ECCurve.NamedCurves.nistP256))
                      };

        await RunDc(station, vehicle, stationEnd);

        // The signature covers the right octets, so the digest holds, and it was made with a key that
        // is not the contract certificate's. Recorded, and the session goes on.
        Assert.Multiple(() => {
            Assert.That(station.MeteringConfirmations,  Is.Not.Empty);
            Assert.That(station.MeteringConfirmations,  Has.All.Matches<MeteringConfirmationCheck>(check =>  check.DigestOk     &&
                                                                                                             check.ReadingOk    &&
                                                                                                            !check.SignatureOk  &&
                                                                                                            !check.Valid        &&
                                                                                                             check.ResponseCode == ResponseCode.OK));
        });

    }

    #endregion

    #region AStationThatRequiresAValidSignatureEndsTheSession()

    [Test]
    public async Task AStationThatRequiresAValidSignatureEndsTheSession()
    {

        var (vehicleEnd, stationEnd) = InMemoryStream.Pair();

        var station = new Secc20Dc(TimeSpan.FromSeconds(60), TimeProvider.System) {
                          InstalledMeter                   = Meter(),
                          MeteringConfirmationEvery        = 1,
                          RequireValidMeteringConfirmation = true
                      };
        var vehicle = new Evcc20Dc(vehicleEnd, TimeProvider.System, new NoDelay(), TimeSpan.FromSeconds(5)) {
                          Pnc = Contract(SignWith: ECDsa.Create(ECCurve.NamedCurves.nistP256))
                      };

        // [V2G20-1990], and a FAILED response is terminal.
        var aborted = await Assert.ThrowsAsync<SessionAborted>(() => RunDc(station, vehicle, stationEnd));

        Assert.Multiple(() => {
            Assert.That(aborted!.Message,                                 Does.Contain("FAILED_MeteringSignatureNotValid"));
            Assert.That(station.MeteringConfirmations.Single().ResponseCode, Is.EqualTo(ResponseCode.FAILED_MeteringSignatureNotValid));
            Assert.That(station.IsDone,                                   Is.True);
        });

    }

    #endregion


    #region AReadingIsTheSameOnlyWithTheSameMeterSignature()

    [Test]
    public void AReadingIsTheSameOnlyWithTheSameMeterSignature()
    {

        var reading = new MeterInfoType("VAN*M1", 4200, null, null, null, [1, 2, 3], null, 1_800_000_000);

        Assert.Multiple(() => {
            Assert.That(MeteringConfirmation.SameReading(reading, reading with { MeterSignature = [1, 2, 3] }), Is.True);
            Assert.That(MeteringConfirmation.SameReading(reading, reading with { MeterSignature = [1, 2, 4] }), Is.False);
            Assert.That(MeteringConfirmation.SameReading(reading, reading with { MeterSignature = null }),      Is.False);
            Assert.That(MeteringConfirmation.SameReading(reading, reading with { ChargedEnergyReadingWh = 4201 }), Is.False);
        });

    }

    #endregion

}
