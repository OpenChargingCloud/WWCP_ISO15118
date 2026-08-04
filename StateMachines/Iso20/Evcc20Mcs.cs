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

namespace Vanaheimr.V2G.Simulation.StateMachines.Iso20
{
    /// <summary>
    /// EVCC for the ISO 15118-20 <b>Megawatt Charging System</b> (MCS) — a truck or bus.
    /// <para>
    /// The counterpart to <see cref="Secc20Mcs"/>, and equally thin: MCS reuses the DC message set
    /// unchanged, so the only difference from <see cref="Evcc20Dc"/> is which energy-transfer services this
    /// vehicle asks for — <b>8 (MCS)</b> and <b>9 (MCS_BPT)</b> rather than DC's 2 / 6.
    /// </para>
    /// <para>
    /// A megawatt truck is a DC vehicle as far as the protocol is concerned: it still runs
    /// DC_ChargeParameterDiscovery → CableCheck → PreCharge → ChargeLoop → WeldingDetection, and still
    /// reports <see cref="StateMachines.PowerMode.Dc"/>. Only the catalogue entry and the power envelope
    /// differ, which is exactly why MCS needed no codec work.
    /// </para>
    /// </summary>
    public sealed class Evcc20Mcs(Stream stream, TimeProvider clock, IAsyncDelay pollDelay, TimeSpan perMessageTimeout)
        : Evcc20Dc(stream, clock, pollDelay, perMessageTimeout)
    {
        /// <summary>MCS = 8, MCS_BPT = 9. Falls back to whatever the SECC offers if it advertises neither,
        /// via the base class's selection logic — a plain DC charger is still usable, just not at MCS power.</summary>
        protected override IReadOnlyList<ushort> PreferredEnergyServiceIds => new ushort[] { 8, 9 };
    }
}
