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

using cloud.charging.open.protocols.ISO15118.SLAC.Avln;
using cloud.charging.open.protocols.ISO15118.SLAC.StateMachine;
using cloud.charging.open.protocols.ISO15118.SLAC.Transport;

namespace Vanaheimr.V2G.Simulation.Slac
{
    /// <summary>
    /// SECC/EVSE-side SLAC pairing stage. <see cref="StartAsync"/> begins listening for a PEV (so the
    /// EVSE is ready before the EV sends its first CM_SLAC_PARM.REQ); <see cref="WaitForMatchAsync"/>
    /// completes when the first PEV finishes matching, programs the local PLC chip (if a <paramref name="chip"/>
    /// is supplied) with the negotiated NID/NMK, waits for the AVLN, and returns the <see cref="SlacResult"/>.
    /// </summary>
    public sealed class SlacEvseStage(ISlacTransport      transport,
                                      EvseSlacOptions     options,
                                      IPlcChipController?  chip              = null,
                                      TimeSpan?           avlnReadyTimeout  = null) : IAsyncDisposable
    {
        private readonly TaskCompletionSource<SlacResult> _matched = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private EvseSlacListener? _listener;

        /// <summary>Start listening for a PEV. Call this before the EV begins pairing.</summary>
        public async Task StartAsync(CancellationToken ct = default)
        {
            _listener = new EvseSlacListener(transport, () => options);
            _listener.SessionCompleted += (_, e) => _matched.TrySetResult(new SlacResult(e.Result.Nid, e.Result.Nmk));
            _listener.SessionFailed    += (_, e) => _matched.TrySetException(e.Error);

            await _listener.StartAsync(ct).ConfigureAwait(false); // subscribes to transport.FrameReceived
            await transport.StartAsync(ct).ConfigureAwait(false); // begin receiving
        }

        /// <summary>Await the first completed SLAC match, then program the local PLC chip.</summary>
        public async Task<SlacResult> WaitForMatchAsync(CancellationToken ct = default)
        {
            using (ct.Register(() => _matched.TrySetCanceled(ct)))
            {
                var result = await _matched.Task.ConfigureAwait(false);
                await SlacChip.ProgramAsync(chip, result, avlnReadyTimeout ?? SlacChip.DefaultAvlnReadyTimeout, ct).ConfigureAwait(false);
                return result;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_listener is not null)
                await _listener.DisposeAsync().ConfigureAwait(false);
        }
    }
}
