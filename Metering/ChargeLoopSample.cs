/*
 * Copyright (c) 2021-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of EVSimulatorApp
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

namespace Vanaheimr.V2G.Simulation.Metering;

/// <summary>
/// How much charging one charge-loop iteration stands for.
/// </summary>
/// <remarks>
/// <para>
/// A property of the simulation's charge loop rather than of either meter, which is why it lives on
/// its own: the vehicle's <see cref="EvMeter"/> and the station's <see cref="SigningMeter"/> have to
/// agree about it or their two readings are not measurements of one process, and a comparison
/// between them means nothing. Two constants that had to match would eventually not.
/// </para>
/// <para>
/// The number is arbitrary and therefore stated rather than buried. A simulator's loop runs three
/// iterations where a real session runs for an hour, so something has to declare what an iteration
/// is worth; one minute puts a 48 kW DC session at 800 Wh per sample, which is a figure a person can
/// check in their head — the property that matters most in a demo.
/// </para>
/// <para>
/// It is deliberately not a clock reading. The corpus is recorded with a pinned clock so -20's
/// per-message timestamps stay stable, so anything integrating over wall time would count zero
/// there and something different on every live run. A declared sample makes the same session
/// produce the same energy in C#, Kotlin, Swift and in the app.
/// </para>
/// </remarks>
public static class ChargeLoopSample
{

    /// <summary>What one charge-loop iteration stands for: one minute.</summary>
    public static readonly TimeSpan Period = TimeSpan.FromMinutes(1);

    /// <summary>The energy one iteration at <paramref name="watts"/> represents, in watt-hours.</summary>
    public static double WattHours(double watts) => watts * Period.TotalHours;

    /// <summary>
    /// What a meter register actually takes on for one iteration: whole watt-hours, signed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Rounded per sample, and that is the load-bearing decision here.</b> Integrating precisely
    /// and rounding once at the end is the better numerical answer, and it is the wrong one: the
    /// figure this is compared against lives in <c>MeterReading</c> / <c>ChargedEnergyReadingWh</c>,
    /// an <c>xs:unsignedLong</c> register that can hold nothing finer. A model more precise than the
    /// register it is checked against reports a difference that is an artefact of the model.
    /// </para>
    /// <para>
    /// Found by measurement rather than by reasoning: an AC session came out 549 Wh at the station
    /// and 548 Wh in the vehicle — three samples of 182.67 Wh, rounded three times against rounded
    /// once. One watt-hour, and exactly the kind of difference a screen saying "these agree" must
    /// never show.
    /// </para>
    /// </remarks>
    public static double RegisterWattHours(double watts) =>
        Math.Round(WattHours(watts), MidpointRounding.AwayFromZero);

    /// <summary>The same for a station's import-only register, which cannot hold a negative.</summary>
    public static ulong WattHoursRounded(double watts) => (ulong) RegisterWattHours(watts);

}
