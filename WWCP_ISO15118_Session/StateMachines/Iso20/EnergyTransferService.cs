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

namespace Vanaheimr.V2G.Simulation.StateMachines.Iso20
{
    /// <summary>
    /// The ISO 15118-20 energy-transfer service ids (Table 204; 8/9 from the MCS amendment, taken from
    /// EVerest's <c>libiso15118</c> — see <see cref="Secc20Mcs"/>), and the one question the simulation
    /// asks about them: whether a service is <b>bidirectional</b>.
    /// </summary>
    /// <remarks>
    /// Named here rather than as literals at each site because both sides of the session have to agree on
    /// the answer, and they are in different class hierarchies. The EVCC asks it to decide which
    /// charge-parameter and control-mode types to <i>send</i>; the SECC asks it to decide which ones it will
    /// <i>accept</i>. Those two readings of one rule disagreeing is exactly the defect the
    /// <c>2026-08-05-everest-mcs-bpt</c> run found on our side, so the rule gets one home.
    /// </remarks>
    public static class EnergyTransferService
    {
        public const ushort AC      = 1;
        public const ushort DC      = 2;
        public const ushort AC_BPT  = 5;
        public const ushort DC_BPT  = 6;
        public const ushort MCS     = 8;
        public const ushort MCS_BPT = 9;

        /// <summary>
        /// Whether <paramref name="serviceId"/> is a bidirectional (BPT) service — the ones whose sessions
        /// must carry the polymorphic <c>BPT_*</c> charge-parameter and control-mode types.
        /// </summary>
        /// <remarks>
        /// ISO 15118-20 carries the direction in the type, not in a flag, so the selected service and every
        /// subsequent charge-parameter / control-mode element have to agree. everest-core 2026.02.1 enforces
        /// this against the EV — it answers <c>FAILED_WrongChargeParameter</c> to a charge-only
        /// <c>DC_ChargeParameterDiscoveryReq</c> under service 9 — which is the first external confirmation
        /// this project has that the coupling binds both ends.
        /// </remarks>
        public static bool IsBidirectional(ushort serviceId)
            => serviceId is AC_BPT or DC_BPT or MCS_BPT;
    }
}
