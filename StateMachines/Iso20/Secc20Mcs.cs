/*
 * Copyright (c) 2021-2025 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP ISO/IEC 15118 <https://github.com/OpenChargingCloud/WWCP_ISO15118>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Dc20 = cloud.charging.open.protocols.ISO15118_20.DC.Generated;

namespace Vanaheimr.V2G.Simulation.StateMachines.Iso20
{
    /// <summary>
    /// SECC for the ISO 15118-20 <b>Megawatt Charging System</b> (MCS) — a truck/bus charger.
    /// <para>
    /// MCS is <b>not a separate message set</b>. It rides entirely on the existing <b>DC</b> schema: the
    /// same <c>DC_ChargeParameterDiscovery</c> / <c>DC_CableCheck</c> / <c>DC_PreCharge</c> /
    /// <c>DC_ChargeLoop</c> / <c>DC_WeldingDetection</c> messages, over the same V2GTP payload type. What
    /// distinguishes it is the <b>service catalogue</b> and the <b>power envelope</b>, which is why this is a
    /// thin subclass of <see cref="Secc20Dc"/> rather than new codec work:
    /// </para>
    /// <list type="bullet">
    ///   <item>service ids <b>8 (MCS)</b> and <b>9 (MCS_BPT)</b> instead of DC's 2 / 6 — <c>serviceIDType</c>
    ///         is a plain <c>xs:unsignedShort</c>, so no schema carries these as an enum;</item>
    ///   <item>the ServiceDetail parameter set is structurally identical (Connector / ControlMode /
    ///         MobilityNeedsMode / Pricing), with <c>Connector</c> naming the MCS connector family;</item>
    ///   <item>megawatt-scale limits in the charge-parameter response.</item>
    /// </list>
    /// <para>
    /// Service ids and the connector enum are taken from EVerest's <c>libiso15118</c>
    /// (<c>ServiceCategory::MCS = 8</c>, <c>MCS_BPT = 9</c>; <c>McsConnector</c> 1 = MCS, 4 = rMCS, 5 = xMCS),
    /// whose surrounding values (AC=1, DC=2, DC_BPT=6) match ours exactly — see <c>docs/roadmap.md</c>.
    /// <b>Not validated against a counterpart:</b> the numbers agree with the one maintained implementation
    /// that has MCS, but nothing here has been byte-diffed or run live, and the physical limits below are
    /// plausible headline figures, not values read out of the amendment text.
    /// </para>
    /// </summary>
    public sealed class Secc20Mcs(TimeSpan sequenceTimeout, TimeProvider clock) : Secc20Dc(sequenceTimeout, clock)
    {
        /// <summary>MCS = 8, MCS_BPT = 9 (Table 204 continuation; EVerest <c>ServiceCategory</c>).</summary>
        protected override IReadOnlyList<ushort> EnergyServiceIds => new ushort[] { 8, 9 };

        /// <summary>MCS connector family: 1 = MCS (the full megawatt coupler). 4 = rMCS and 5 = xMCS are the
        /// reduced/extended variants; a charger offering those would override this.</summary>
        protected override int ConnectorParameter => 1;

        // MCS headline envelope: 1250 V × 3000 A ≈ 3.75 MW. RationalNumberType is (sbyte exponent,
        // short value), so the megawatt figures need an exponent — 3750 × 10³ W and 3000 × 10⁰ A both fit
        // the short range, which DC's plain 50 kW / 200 A did not need.
        protected override Dc20.RationalNumberType MaxPower   => new(3, 3750);   // 3.75 MW
        protected override Dc20.RationalNumberType MaxCurrent => new(0, 3000);   // 3000 A
        protected override Dc20.RationalNumberType MaxVoltage => new(0, 1250);   // 1250 V
        protected override Dc20.RationalNumberType MinVoltage => new(0,  150);
    }
}
