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

using System.Globalization;

using NUnit.Framework;

using cloud.charging.open.protocols.ISO15118.Simulation;

namespace cloud.charging.open.protocols.ISO15118.Tests.Simulation;

/// <summary>
/// What a battery says of itself once its session is over.
/// </summary>
public class EvBatteryTests
{

    /// <summary>
    /// Its numbers are written with a point whatever the culture of the machine, as the switches they
    /// came from are read: on a German Windows it said "Battery: 50,5 % of 60,5 kWh", where
    /// <c>--battery 60.5 --soc 50.5</c> had been given.
    /// </summary>
    [Test]
    public void ItsNumbersHaveAPointWhateverTheCulture()
    {

        var culture = CultureInfo.CurrentCulture;
        string said;

        try
        {

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

            var battery = new EvBattery(60.5, 50.5) { TargetEnergyWh = 12_500, MinimumSoC = 80 };

            said = battery.Describe(ChargeStop.TargetEnergy);

        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }

        Assert.Multiple(() => {
            Assert.That(said, Does.StartWith("Battery: 50.5 % of 60.5 kWh (started at 50.5 %, 0.00 kWh delivered)"), said);
            Assert.That(said, Does.Contain("target 12.5 kWh delivered."), said);
            Assert.That(said, Does.Contain("NOT ENOUGH: 80 % was asked for and the car leaves at 50.5 %."), said);
        });

    }

}
