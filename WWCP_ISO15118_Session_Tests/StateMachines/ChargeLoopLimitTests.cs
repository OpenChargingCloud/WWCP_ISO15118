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

using Dc20         = cloud.charging.open.protocols.ISO15118_20.DC.Generated;
using Dc20Rational = cloud.charging.open.protocols.ISO15118_20.DC.RationalNumber;

namespace cloud.charging.open.protocols.ISO15118.Tests.StateMachines;

/// <summary>
/// The DC charge loop of `-20` under a limit set from outside the session - the thermal monitor of an
/// MCS coupler: what is served never exceeds it, a Dynamic control mode says it as the EVSE's maximum,
/// and a Terminate tells the vehicle to end the charging and serves nothing more.
/// </summary>
[TestFixture]
public class ChargeLoopLimitTests
{

    #region (private) Probe

    /// <summary>
    /// A DC station whose charge loop a test may call directly, and whose maximum current is that of a
    /// megawatt coupler when asked - the envelope a station's own limits have to reach the loop with.
    /// </summary>
    private sealed class Probe(Int16 MaxAmperes = 200) : Secc20Dc(TimeSpan.FromSeconds(60), TimeProvider.System)
    {

        protected override Dc20.RationalNumberType MaxCurrent
            => new (0, MaxAmperes);

        public Dc20.DC_ChargeLoopRes Loop(Dc20.CLReqControlModeType Mode)
            => (Dc20.DC_ChargeLoopRes) HandleChargeLoop(new Dc20.DC_ChargeLoopReq(
                                                            SessionCtx.ToDcHeader(),
                                                            DisplayParameters:   null,
                                                            MeterInfoRequested:  false,
                                                            EVPresentVoltage:    new (0, 400),
                                                            CLReqControlMode:    Mode)).Response;

    }

    #endregion

    #region (private static) Scheduled(Amperes), Dynamic()

    private static Dc20.CLReqControlModeType Scheduled(Int16 Amperes)

        => new Dc20.Scheduled_DC_CLReqControlModeType(
               EVTargetEnergyRequest:  null,
               EVMaximumEnergyRequest: null,
               EVMinimumEnergyRequest: null,
               EVTargetCurrent:        new (0, Amperes),
               EVTargetVoltage:        new (0, 400),
               null, null, null, null, null);

    private static Dc20.CLReqControlModeType Dynamic()

        => new Dc20.Dynamic_DC_CLReqControlModeType(
               DepartureTime:          3600,
               EVTargetEnergyRequest:  new (3, 100),
               EVMaximumEnergyRequest: new (3, 100),
               EVMinimumEnergyRequest: new (0, 0),
               EVMaximumChargePower:   new (3, 1000),
               EVMinimumChargePower:   new (3, 1),
               EVMaximumChargeCurrent: new (0, 3000),
               EVMaximumVoltage:       new (0, 1000),
               EVMinimumVoltage:       new (0, 200));

    private static Double Amperes(Dc20.RationalNumberType Value)
        => (Double) Dc20Rational.ToDecimal(Value);

    #endregion


    [Test]
    public void WithoutALimitTheVehicleGetsWhatItAsksFor()
    {

        var response = new Probe().Loop(Scheduled(150));

        Assert.Multiple(() => {
            Assert.That(Amperes(response.EVSEPresentCurrent), Is.EqualTo(150));
            Assert.That(response.EVSECurrentLimitAchieved,    Is.False);
            Assert.That(response.EVSEStatus,                  Is.Null);
        });

    }

    [Test]
    public void ALimitCapsWhatIsServedAndSaysSo()
    {

        var station  = new Probe { CurrentLimit_A = 80 };
        var response = station.Loop(Scheduled(150));

        Assert.Multiple(() => {
            Assert.That(Amperes(response.EVSEPresentCurrent), Is.EqualTo(80));
            Assert.That(response.EVSECurrentLimitAchieved,    Is.True);
        });

    }

    [Test]
    public void ALimitAboveTheMaximumIsTheMaximum()
    {

        var response = new Probe { CurrentLimit_A = 5000 }.Loop(Scheduled(250));

        Assert.That(Amperes(response.EVSEPresentCurrent), Is.EqualTo(200));

    }

    [Test]
    public void ALimitLiftedIsTheMaximumAgain()
    {

        var station = new Probe { CurrentLimit_A = 80 };
        station.Loop(Scheduled(150));

        station.CurrentLimit_A = null;

        Assert.That(Amperes(station.Loop(Scheduled(150)).EVSEPresentCurrent), Is.EqualTo(150));

    }

    [Test]
    public void ADynamicControlModeSaysTheLimitAsTheEVSEsMaximum()
    {

        var station = new Probe(MaxAmperes: 3000) { CurrentLimit_A = 1500 };
        var mode    = (Dc20.Dynamic_DC_CLResControlModeType) station.Loop(Dynamic()).CLResControlMode;

        Assert.That(Amperes(mode.EVSEMaximumChargeCurrent), Is.EqualTo(1500));

    }

    [Test]
    public void ADynamicControlModeSaysTheStationsOwnMaximumNotDCs200A()
    {

        // The MCS envelope reaches the loop: 3000 A, where the loop used to say DC's 200 A whatever the station was.
        var mode = (Dc20.Dynamic_DC_CLResControlModeType) new Probe(MaxAmperes: 3000).Loop(Dynamic()).CLResControlMode;

        Assert.That(Amperes(mode.EVSEMaximumChargeCurrent), Is.EqualTo(3000));

    }

    [Test]
    public void TerminateTellsTheVehicleToEndAndServesNothingMore()
    {

        var station = new Probe();
        Assert.That(station.Loop(Scheduled(150)).EVSEStatus, Is.Null);

        station.Terminate();
        var response = station.Loop(Scheduled(150));

        Assert.Multiple(() => {
            Assert.That(station.TerminateRequested,                     Is.True);
            Assert.That(response.EVSEStatus?.EVSENotification,          Is.EqualTo(Dc20.EvseNotification.Terminate));
            Assert.That(response.EVSEStatus?.NotificationMaxDelay,      Is.EqualTo(0));
            Assert.That(Amperes(response.EVSEPresentCurrent),           Is.EqualTo(0));
        });

    }

}
