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

namespace Vanaheimr.V2G.Simulation.Session
{

    /// <summary>
    /// What one connection hands the next when a session pauses rather than terminates.
    /// </summary>
    /// <param name="SessionId">
    /// The paused session's id, which the resuming SessionSetupReq carries in its header.
    /// </param>
    /// <param name="Binding">
    /// Proof of <em>who</em> paused it — ISO 15118-20 only, and null for `-2`, which has no such concept:
    /// there the session id alone is the whole rule. See <c>StateMachines.Iso20.SessionBinding20</c>.
    /// </param>
    /// <param name="EnergyServiceId">
    /// The energy-transfer service the session had settled on. A resumed `-20` session does not repeat
    /// service negotiation, so this is the only way the next connection learns it; `-2` renegotiates it
    /// anyway and leaves this at 0.
    /// </param>
    /// <remarks>
    /// The three travel together because they are only meaningful together, and because keeping them apart
    /// is how the session id came to be offered without anything to authenticate it — the defect this type
    /// exists downstream of.
    /// </remarks>
    public sealed record ResumableSession(byte[] SessionId, byte[]? Binding, ushort EnergyServiceId);

}
