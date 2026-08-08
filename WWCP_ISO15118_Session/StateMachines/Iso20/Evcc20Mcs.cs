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

using Vanaheimr.V2G.Simulation.Timing;

using Dc20 = cloud.charging.open.protocols.ISO15118_20.DC.Generated;

namespace Vanaheimr.V2G.Simulation.StateMachines.Iso20
{
    /// <summary>
    /// EVCC for the ISO 15118-20 <b>Megawatt Charging System</b> (MCS) — a truck or bus.
    /// <para>
    /// The counterpart to <see cref="Secc20Mcs"/>, and equally thin: MCS reuses the DC message set
    /// unchanged, so the only differences from <see cref="Evcc20Dc"/> are which energy-transfer services
    /// this vehicle asks for — <b>8 (MCS)</b> and <b>9 (MCS_BPT)</b> rather than DC's 2 / 6 — and the
    /// envelope it declares under them, the mirror of what <see cref="Secc20Mcs"/> offers.
    /// </para>
    /// <para>
    /// A megawatt truck is a DC vehicle as far as the protocol is concerned: it still runs
    /// DC_ChargeParameterDiscovery → CableCheck → PreCharge → ChargeLoop → WeldingDetection, and still
    /// reports <see cref="StateMachines.PowerMode.Dc"/>. Only the catalogue entry and the power envelope
    /// differ, which is exactly why MCS needed no codec work.
    /// </para>
    /// <para>
    /// <b>Open rather than sealed, and the reason is a scar.</b> A megawatt vehicle that differs from this
    /// one in some small way — a different service ranking, a reduced connector — is a subclass of *it*,
    /// not of <see cref="Evcc20Dc"/>. While this class was <c>sealed</c> the conformance harness's MCS_BPT
    /// probe had to derive from <see cref="Evcc20Dc"/> instead and repeat the envelope below by hand. It
    /// drifted, and the first complete MCS_BPT run against EVerest caught it declaring 50 kW under service
    /// 9 — the very contradiction this class exists to prevent. Sealing bought nothing and cost that.
    /// </para>
    /// </summary>
    public class Evcc20Mcs(Stream stream, TimeProvider clock, IAsyncDelay pollDelay, TimeSpan perMessageTimeout)
        : Evcc20Dc(stream, clock, pollDelay, perMessageTimeout)
    {
        /// <summary>MCS = 8, MCS_BPT = 9. Falls back to whatever the SECC offers if it advertises neither,
        /// via the base class's selection logic — a plain DC charger is still usable, just not at MCS power.</summary>
        protected override IReadOnlyList<ushort> PreferredEnergyServiceIds => new ushort[] { 8, 9 };

        // The same headline envelope Secc20Mcs offers, declared from the other side:
        // 1250 V × 3000 A ≈ 3.75 MW. RationalNumberType is (sbyte exponent, short value), so the megawatt
        // figures need an exponent — 3750 × 10³ W and 3000 × 10⁰ A both fit the short range, which DC's
        // plain 50 kW / 200 A did not need.
        //
        // Until 2026-08-05 these were inherited from Evcc20Dc, so a megawatt truck selected service 8 and
        // then declared an ordinary DC envelope: EVerest's EvseManager read back
        // "dc_ev_maximum_power_limit: 50000.0" under an MCS service. Nothing failed — their SIL clamps to
        // its own 22 kW either way — but the declaration contradicted the service.
        protected override Dc20.RationalNumberType MaxPower   => new(3, 3750);   // 3.75 MW
        protected override Dc20.RationalNumberType MaxCurrent => new(0, 3000);   // 3000 A
        protected override Dc20.RationalNumberType MaxVoltage => new(0, 1250);   // 1250 V
        protected override Dc20.RationalNumberType MinVoltage => new(0,  150);   //  150 V

        // In the loop the truck asks for the whole envelope and lets the station clamp — 3000 A at 1250 V
        // is exactly the 3.75 MW above. DC asks for less than it declared (125 A of 200 A); nothing says a
        // truck must, and asking in full keeps the two declarations from disagreeing.
        protected override Dc20.RationalNumberType LoopMaxPower   => MaxPower;
        protected override Dc20.RationalNumberType LoopMaxCurrent => MaxCurrent;
    }
}
