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

using cloud.charging.open.protocols.ISO15118.SLAC.Avln;

namespace Vanaheimr.V2G.Simulation.Slac
{
    /// <summary>
    /// Programs the local PLC chip with the SLAC-negotiated credentials and waits until the AVLN is up —
    /// the step after a match completes. In simulation a <c>SimulatedChipController</c> records the key and
    /// reports the AVLN ready immediately (optionally after a configurable delay).
    /// </summary>
    internal static class SlacChip
    {
        internal static readonly TimeSpan DefaultAvlnReadyTimeout = TimeSpan.FromSeconds(5);

        internal static async Task ProgramAsync(IPlcChipController? chip,
                                                SlacResult          result,
                                                TimeSpan            avlnReadyTimeout,
                                                CancellationToken   ct)
        {
            if (chip is null)
                return;

            await chip.SetKeyAsync(result.Nid, result.Nmk, ct).ConfigureAwait(false);
            await chip.WaitForAvlnReadyAsync(avlnReadyTimeout, ct).ConfigureAwait(false);
        }
    }
}
