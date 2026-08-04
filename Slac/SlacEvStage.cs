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

using cloud.charging.open.protocols.ISO15118.SLAC.Avln;
using cloud.charging.open.protocols.ISO15118.SLAC.Selection;
using cloud.charging.open.protocols.ISO15118.SLAC.StateMachine;
using cloud.charging.open.protocols.ISO15118.SLAC.Transport;

namespace Vanaheimr.V2G.Simulation.Slac
{
    /// <summary>
    /// EV-side SLAC pairing stage: runs the full <see cref="EvSlacSession"/> matching sequence over the
    /// given transport (a <c>UdpSlacTransport</c> in simulation, an AF_PACKET transport on real hardware),
    /// then — if a <paramref name="chip"/> is supplied — programs the local PLC chip with the negotiated
    /// NID/NMK and waits for the AVLN. This is the front stage of the ISO 15118 flow (SLAC → SDP → TLS → session).
    /// </summary>
    public sealed class SlacEvStage(ISlacTransport      transport,
                                    EvSlacOptions       options,
                                    IEVSESelector?      selector          = null,
                                    IPlcChipController?  chip              = null,
                                    TimeSpan?           avlnReadyTimeout  = null)
    {
        public async Task<SlacResult> PairAsync(CancellationToken ct = default)
        {
            await using var session = new EvSlacSession(transport, options, selector);
            await transport.StartAsync(ct).ConfigureAwait(false); // begin receiving after the session subscribed

            var result = await session.RunAsync(ct).ConfigureAwait(false);
            var slac   = new SlacResult(result.MatchCnf.Nid, result.MatchCnf.Nmk);

            await SlacChip.ProgramAsync(chip, slac, avlnReadyTimeout ?? SlacChip.DefaultAvlnReadyTimeout, ct).ConfigureAwait(false);
            return slac;
        }
    }
}
