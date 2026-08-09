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

namespace cloud.charging.open.protocols.ISO15118.Simulation
{

    /// <summary>Why a charge loop stopped, or <see cref="Running"/> while it has not.</summary>
    public enum ChargeStop
    {
        /// <summary>Not finished — keep looping.</summary>
        Running,
        /// <summary>The battery reached 100 %.</summary>
        Full,
        /// <summary>The requested state of charge was reached.</summary>
        TargetSoC,
        /// <summary>The requested amount of energy has been delivered.</summary>
        TargetEnergy,
        /// <summary>The charging-time limit ran out.</summary>
        TimeLimit,
        /// <summary>The departure time arrived.</summary>
        Departure,
        /// <summary>The iteration ceiling was hit — a guard, not a goal.</summary>
        LoopLimit,
    }

    /// <summary>
    /// A battery filling up, and the goals that end a charging session.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It runs on simulated time, and that is not a shortcut.</b> One charge-loop iteration stands for
    /// <see cref="Metering.ChargeLoopSample.Period"/> — one minute — which is the convention
    /// <see cref="Metering.EvMeter"/> already counts by, so a battery filled here and a meter reading
    /// compared against a station's are measuring the same process. A 60 kWh pack from 20 % at 11 kW is
    /// then some 260 iterations rather than four and a half hours.
    /// </para>
    /// <para>
    /// <b>It is charged from what was metered, not from what was asked for.</b> The caller feeds it the
    /// energy the meter counted for that iteration, so in DC the station's delivered current fills the
    /// pack and a station that gives less than requested is visible as a slower charge. The one thing
    /// this cannot model is the station giving more than the car allowed.
    /// </para>
    /// <para>
    /// <b>What it is not.</b> Linear: no constant-voltage taper above the usual 80 % knee, no
    /// temperature, no losses, no ageing. A real pack's last fifth takes disproportionately long and this
    /// one does not, so a run that ends "at 100 %" reports the arithmetic and not a charging curve. That
    /// matters for anything claiming a duration and not at all for a conformance run, which is what this
    /// is for.
    /// </para>
    /// </remarks>
    public sealed class EvBattery
    {

        /// <summary>A mainstream mid-size EV, and the default when only <c>--soc</c> or a goal is given.</summary>
        public const double DefaultCapacityKWh = 60.0;

        /// <summary>
        /// A ceiling on iterations, so a goal that cannot be reached — a station delivering nothing, a
        /// target above capacity — ends the run instead of hanging on a live counterparty.
        /// </summary>
        public const int DefaultMaxIterations = 5000;


        public EvBattery(double capacityKWh, double startSoCPercent)
        {
            if (capacityKWh <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacityKWh), capacityKWh, "a battery has to hold something");
            if (startSoCPercent is < 0 or > 100)
                throw new ArgumentOutOfRangeException(nameof(startSoCPercent), startSoCPercent, "a state of charge is 0..100 %");

            CapacityWh = capacityKWh * 1000.0;
            StartSoC   = startSoCPercent;
            EnergyWh   = CapacityWh * startSoCPercent / 100.0;
        }

        /// <summary>Usable capacity, in watt-hours.</summary>
        public double CapacityWh { get; }

        /// <summary>The state of charge the session started at, in percent.</summary>
        public double StartSoC { get; }

        /// <summary>What the pack holds now, in watt-hours.</summary>
        public double EnergyWh { get; private set; }

        /// <summary>What has gone in since the session started, in watt-hours.</summary>
        public double DeliveredWh { get; private set; }

        /// <summary>The state of charge now, in percent.</summary>
        public double SoC => 100.0 * EnergyWh / CapacityWh;

        /// <summary>Iterations run so far.</summary>
        public int Iterations { get; private set; }

        /// <summary>Simulated time spent charging.</summary>
        public TimeSpan Elapsed => Iterations * Metering.ChargeLoopSample.Period;


        /// <summary>Stop when the pack reaches this state of charge, in percent.</summary>
        public double? TargetSoC { get; init; }

        /// <summary>Stop when this much energy has been delivered, in watt-hours — <c>EAmount</c> in
        /// ISO 15118-2, <c>EVTargetEnergyRequest</c> in -20.</summary>
        public double? TargetEnergyWh { get; init; }

        /// <summary>Stop after this much simulated charging time.</summary>
        public TimeSpan? MaxDuration { get; init; }

        /// <summary>Stop when the departure time arrives — the same value the wire carries as
        /// <c>DepartureTime</c>, seconds from the session's anchor.</summary>
        public TimeSpan? DepartureIn { get; init; }

        /// <summary>
        /// The state of charge the driver needs to have when leaving, in percent.
        /// </summary>
        /// <remarks>
        /// <b>Not a stop condition, and deliberately not one.</b> A floor cannot extend a session — you
        /// cannot charge after you have driven off — so this neither prolongs the loop past a departure
        /// time nor past a charging-time limit. What it does is turn "the session ended" into "the session
        /// ended and the car had enough / did not", which is the question a driver actually asked and the
        /// one a scheduling station should be judged on. <see cref="Describe"/> says which.
        /// <para>
        /// It also has nowhere to go on the -20 DC request side: the Dynamic charge-loop request carries
        /// <c>DepartureTime</c> and the three energy requests, and <c>MinimumSOC</c> exists only in the
        /// station's response. So this is a simulator-side expectation, not a declaration.
        /// </para>
        /// </remarks>
        public double? MinimumSoC { get; init; }

        /// <summary>Whether the minimum was asked for and missed. False when none was asked for.</summary>
        public bool MinimumSoCMissed => MinimumSoC is { } m && SoC < m;

        /// <summary>The power the car asks for, in watts. Zero means "whatever the station offers".</summary>
        public double RequestedPowerW { get; init; }

        /// <summary>The iteration ceiling; see <see cref="DefaultMaxIterations"/>.</summary>
        public int MaxIterations { get; init; } = DefaultMaxIterations;


        /// <summary>
        /// Counts one iteration and the energy it delivered. Charge is clamped at capacity — a pack does
        /// not take more than it holds, and a station that keeps pushing is simply not counted.
        /// </summary>
        public void Add(double wattHours)
        {
            Iterations++;

            if (wattHours > 0)
            {
                var accepted = Math.Min(wattHours, CapacityWh - EnergyWh);
                EnergyWh    += accepted;
                DeliveredWh += accepted;
            }
            else
            {
                // Negative is a bidirectional session exporting: the pack goes down, and down to empty.
                var released = Math.Min(-wattHours, EnergyWh);
                EnergyWh    -= released;
                DeliveredWh += wattHours;
            }
        }

        /// <summary>
        /// Whether a goal has been met, and which. Evaluated in the order a driver would care about:
        /// a full battery ends the session whatever else was asked for.
        /// </summary>
        public ChargeStop Stop
        {
            get
            {
                // 0.5 Wh rather than exact equality: the meter counts whole watt-hours, so a pack filled by
                // it lands next to capacity and never precisely on it.
                if (EnergyWh >= CapacityWh - 0.5)               return ChargeStop.Full;
                if (TargetSoC      is { } s && SoC >= s)        return ChargeStop.TargetSoC;
                if (TargetEnergyWh is { } e && DeliveredWh >= e) return ChargeStop.TargetEnergy;
                if (MaxDuration    is { } d && Elapsed >= d)    return ChargeStop.TimeLimit;
                if (DepartureIn    is { } t && Elapsed >= t)    return ChargeStop.Departure;
                if (Iterations >= MaxIterations)                return ChargeStop.LoopLimit;
                return ChargeStop.Running;
            }
        }

        /// <summary>One line for the console: where the pack ended up and why it stopped.</summary>
        public string Describe(ChargeStop stop)
            => $"Battery: {SoC:F1} % of {CapacityWh / 1000:F1} kWh " +
               $"(started at {StartSoC:F1} %, {DeliveredWh / 1000:F2} kWh delivered) " +
               $"after {Elapsed.TotalMinutes:F0} min simulated in {Iterations} iteration(s) — " +
               stop switch
               {
                   ChargeStop.Full         => "full.",
                   ChargeStop.TargetSoC    => $"target {TargetSoC:F0} % reached.",
                   ChargeStop.TargetEnergy => $"target {TargetEnergyWh / 1000:F1} kWh delivered.",
                   ChargeStop.TimeLimit    => "charging-time limit reached.",
                   ChargeStop.Departure    => "departure time reached.",
                   ChargeStop.LoopLimit    => $"stopped at the {MaxIterations}-iteration ceiling — the goal was not reachable.",
                   _                       => "still running.",
               }
             + (MinimumSoC is { } m
                    ? MinimumSoCMissed
                          ? $" NOT ENOUGH: {m:F0} % was asked for and the car leaves at {SoC:F1} %."
                          : $" The {m:F0} % minimum was met."
                    : "");

    }

}
