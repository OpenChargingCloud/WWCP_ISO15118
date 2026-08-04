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

namespace Vanaheimr.V2G.Simulation.Metering;

/// <summary>
/// The vehicle's own energy counter: what the EV thinks it took, kept independently of what the
/// station says it delivered.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why the EV needs one at all.</b> Without it every number the app can show about energy is the
/// station's, and the EV's signed <c>MeteringReceiptReq</c> is a countersignature on somebody else's
/// figure — the EV attesting to a reading it has no way to dispute. <c>docs/CONCEPT.md</c> §4.2 asks
/// for exactly this: a local model *"so the signature covers the EV's own view, not only an echo of
/// the station's"*. It is also the first of §4.3's three legs, and the only one that needs no
/// cryptography whatsoever — which is worth noticing, because a disagreement it finds is the kind no
/// signature can protect against.
/// </para>
/// <para>
/// <b>Sampled, not clock-driven, and that is a design decision rather than a shortcut.</b> Integrating
/// over a wall clock would make every recorded session unreproducible: the corpus is recorded with a
/// pinned clock precisely so -20's per-message timestamps stay stable, so a meter reading it would
/// count zero, and one reading a real clock would count something different on every run. Instead
/// each charge-loop iteration <em>is</em> a sample and carries a declared duration. Deterministic,
/// replayable, portable to three other languages, and honest about what a simulator's charge loop is:
/// three iterations standing in for a charging session, not three milliseconds of one.
/// </para>
/// <para>
/// <b>Rectangles, not trapezoids.</b> A trapezoidal rule would be the better numerical choice for a
/// sampled signal — and it would make the first sample depend on a previous one that does not exist,
/// which is a decision (half a sample? none?) with no right answer here and a different plausible
/// answer in each port. A power held constant across its own sample is what a charge loop actually
/// models, and it is the same arithmetic in every language.
/// </para>
/// <para>
/// <b>Whole watt-hours per sample</b>, for the reason <see cref="ChargeLoopSample.RegisterWattHours"/>
/// gives: the station's reading lives in a register that holds nothing finer, and a counter more
/// precise than the one it is compared against reports differences it invented itself.
/// </para>
/// <para>
/// <b>What it is not.</b> Not a battery model, not an efficiency model, and not a measurement: there
/// are no losses, no ramping, and no sensor noise. It counts what the EV declared at its inlet. When
/// this and a station's signed reading agree, that is two implementations of one arithmetic agreeing
/// — good for catching a wrong field or a wrong unit, and no evidence at all about a real meter.
/// </para>
/// </remarks>
public sealed class EvMeter
{

    /// <param name="samplePeriod">What one charge-loop iteration stands for. Defaults to
    /// <see cref="ChargeLoopSample.Period"/>, which is what the station's meter uses too — the two
    /// have to agree or their readings are not measurements of one process.</param>
    public EvMeter(TimeSpan? samplePeriod = null)
    {
        SamplePeriod = samplePeriod ?? ChargeLoopSample.Period;

        if (SamplePeriod <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(samplePeriod), SamplePeriod,
                                                  "a sample has to cover some time, or nothing is ever counted");
    }

    /// <summary>What one sample stands for.</summary>
    public TimeSpan SamplePeriod { get; }

    /// <summary>How many samples were taken — one per charge-loop iteration.</summary>
    public int Samples { get; private set; }

    /// <summary>The energy counted so far, in watt-hours — the form the wire carries.</summary>
    public ulong EnergyWh => (ulong) Math.Max(0, Energy);

    /// <summary>The running total, signed, so an exporting session can go back down.</summary>
    public double Energy { get; private set; }


    /// <summary>
    /// Counts one charge-loop iteration at <paramref name="watts"/>.
    /// </summary>
    /// <remarks>
    /// Negative power is accepted and subtracts: a bidirectional session (BPT) exporting to the grid
    /// is energy leaving the battery, and a counter that clamped it at zero would report a V2H
    /// session as pure consumption. Nothing in this repository drives that yet, and refusing it here
    /// would be a decision made by omission.
    /// </remarks>
    public void Sample(double watts)
    {
        // Whole watt-hours per sample, matching ChargeLoopSample.RegisterWattHours for the default
        // period — see there for why the *less* precise rule is the right one.
        Energy += Math.Round(watts * SamplePeriod.TotalHours, MidpointRounding.AwayFromZero);
        Samples++;
    }

    /// <summary>Counts one iteration at <paramref name="volts"/> × <paramref name="amperes"/>.</summary>
    public void Sample(double volts, double amperes) => Sample(volts * amperes);

}
