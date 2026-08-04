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

using Vanaheimr.V2G.Simulation.Session;

namespace Vanaheimr.V2G.Simulation.Timing
{

    /// <summary>
    /// A deadline for a phase the station answers with <c>EVSEProcessing = Ongoing</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Found live, not by reasoning.</b> Every poll loop in both EVCCs used to be
    /// <c>while (… != Finished)</c> with no counter and no deadline. Against EVerest's <c>EvseV2G</c> on
    /// 2026-08-02 that meant 1 170 <c>AuthorizationReq</c> in three minutes: their station answered
    /// <c>OK</c> with <c>Ongoing</c> every time, correctly — nothing had authorized the session — and our
    /// car had nothing that would ever make it stop
    /// (<c>docs/interop-runs/2026-08-02-everest-iso2-dc-notls/</c>).
    /// </para>
    /// <para>
    /// The gap was between two timeouts that both looked like they covered it. The per-message timeout
    /// fires when a response is late, and every one of those 1 170 responses was fast. The caller's
    /// cancellation token ends the whole session, which is a stop-everything, not a phase deadline. What
    /// was missing is the one in the middle: ISO 15118's EVCC-side <i>ongoing</i> timeout, which ends a
    /// phase that is being answered promptly and never finishes.
    /// </para>
    /// <para>
    /// <b>Why the loopback suite could not see it.</b> Our own SECC answers <c>Finished</c> within a poll
    /// or two, so no recorded session and no replay contains a station that keeps saying <c>Ongoing</c>.
    /// The corpus can only hold behaviour our own station exhibits — the same blind spot that hid the
    /// unread response codes.
    /// </para>
    /// <para>
    /// The default is 60 s, the value ISO 15118 gives for the EVCC ongoing timeout and the one Josev
    /// uses. It is a property rather than a constant because a test needs it short and a slow live peer
    /// may need it long.
    /// </para>
    /// </remarks>
    /// <param name="clock">The same clock the session is timed with, so a pinned clock pins this too.</param>
    /// <param name="limit">How long the phase may stay <c>Ongoing</c>.</param>
    /// <param name="phase">Named in the error: a live run is read from that line.</param>
    public sealed class OngoingGuard(TimeProvider clock, TimeSpan limit, String phase)
    {

        private readonly DateTimeOffset started = clock.GetUtcNow();

        /// <summary>Called once per poll. Throws when the phase has outlived its deadline.</summary>
        public void Tick()
        {

            var waited = clock.GetUtcNow() - started;

            if (waited > limit)
                throw new SessionAborted(
                    $"{phase}: the station kept answering 'Ongoing' for {waited.TotalSeconds:0.#} s " +
                    $"(limit {limit.TotalSeconds:0.#} s); the session ends here.");

        }

    }

}
