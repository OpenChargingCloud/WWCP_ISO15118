/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

namespace cloud.charging.open.protocols.ISO15118.T1S.Nodes
{

    /// <summary>
    /// How warm a coupler pin gets: one lump of metal with a current through
    /// it and the air around it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// First-order, and that is the right order. The pin heats with the
    /// square of the current - Joule heating, I²R - and loses heat to its
    /// surroundings in proportion to how much warmer than them it is. That is
    /// one differential equation with two constants, and it gives the shape
    /// every thermal trace of a connector actually has: a rise that flattens
    /// towards a steady state, and a fall that flattens towards ambient.
    /// </para>
    /// <para>
    /// The two constants are stated the way a data sheet would state them
    /// rather than as a resistance and a capacity: the <em>rated current</em>
    /// and the <em>rise</em> the pin settles at when carrying it, and the
    /// <em>time constant</em> it takes to get about two thirds of the way
    /// there. A pin rated 500 A for a 40 K rise reaches 102 K above ambient at
    /// 800 A - which is why an overload shows up as a temperature, and why a
    /// sensor in the pin is how it is detected.
    /// </para>
    /// <para>
    /// Advanced by the caller with the time that has passed, not by a clock of
    /// its own, so that a test can run an hour of heating in a millisecond and
    /// a bench can run it in real time.
    /// </para>
    /// </remarks>
    public sealed class ThermalModel
    {

        #region Properties

        /// <summary>The temperature the pin cools towards, in °C.</summary>
        public Double  Ambient_C         { get; set; }

        /// <summary>How long it takes to close about 63 % of the way to a new steady state.</summary>
        public Double  TimeConstant_s    { get; }

        /// <summary>The current the rise below is stated for.</summary>
        public Double  RatedCurrent_A    { get; }

        /// <summary>How far above ambient the pin settles at the rated current.</summary>
        public Double  RatedRise_C       { get; }

        /// <summary>The current through the pin right now, in A. Set it, and the next advance heats or cools accordingly.</summary>
        public Double  Current_A         { get; set; }

        /// <summary>The pin's temperature right now, in °C.</summary>
        public Double  Temperature_C     { get; private set; }

        /// <summary>
        /// Where the temperature would settle if the current stayed as it is.
        /// </summary>
        public Double  SteadyState_C
            => Ambient_C + RiseAt(Current_A);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// A pin at ambient, carrying nothing.
        /// </summary>
        /// <param name="Ambient_C">The surroundings, in °C.</param>
        /// <param name="TimeConstant_s">How sluggish the pin is.</param>
        /// <param name="RatedCurrent_A">The current the rise is stated for.</param>
        /// <param name="RatedRise_C">The steady-state rise above ambient at that current.</param>
        public ThermalModel(Double  Ambient_C        = 20,
                            Double  TimeConstant_s   = 60,
                            Double  RatedCurrent_A   = 500,
                            Double  RatedRise_C      = 40)
        {

            if (TimeConstant_s <= 0)
                throw new ArgumentOutOfRangeException(nameof(TimeConstant_s), "A time constant has to be positive.");

            if (RatedCurrent_A <= 0)
                throw new ArgumentOutOfRangeException(nameof(RatedCurrent_A), "A rated current has to be positive.");

            this.Ambient_C       = Ambient_C;
            this.TimeConstant_s  = TimeConstant_s;
            this.RatedCurrent_A  = RatedCurrent_A;
            this.RatedRise_C     = RatedRise_C;
            this.Temperature_C   = Ambient_C;

        }

        #endregion


        #region RiseAt(Current_A)

        /// <summary>
        /// How far above ambient the pin settles at a given current.
        /// </summary>
        public Double RiseAt(Double Current_A)
            => RatedRise_C * (Current_A / RatedCurrent_A) * (Current_A / RatedCurrent_A);

        #endregion

        #region Advance(Elapsed)

        /// <summary>
        /// Let this much time pass at the present current.
        /// </summary>
        /// <remarks>
        /// The exact solution of the first-order equation over the step, not a
        /// forward-Euler step: a test that advances an hour at once gets the
        /// steady state, not a number that has run away.
        /// </remarks>
        public void Advance(TimeSpan Elapsed)
        {

            if (Elapsed <= TimeSpan.Zero)
                return;

            var target  = SteadyState_C;
            var decay   = Math.Exp(-Elapsed.TotalSeconds / TimeConstant_s);

            Temperature_C = target + (Temperature_C - target) * decay;

        }

        #endregion

        #region Reset(Temperature_C = null)

        /// <summary>
        /// Put the pin at a temperature - ambient unless one is given.
        /// </summary>
        public void Reset(Double? Temperature_C = null)
            => this.Temperature_C = Temperature_C ?? Ambient_C;

        #endregion


        #region (override) ToString()

        public override String ToString()
            => $"{Temperature_C:F1} °C at {Current_A:F0} A (ambient {Ambient_C:F0} °C, settling at {SteadyState_C:F1} °C)";

        #endregion

    }

}
