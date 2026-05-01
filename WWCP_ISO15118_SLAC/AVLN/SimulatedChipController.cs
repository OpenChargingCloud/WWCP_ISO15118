/*
 * Copyright (c) 2014-2025 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

namespace cloud.charging.open.protocols.ISO15118.SLAC.Avln;

/// <summary>
/// "Empty" chip controller for the UDP-bus simulation. The simulated
/// transport delivers frames regardless of NMK state, so we just record
/// the call and treat the AVLN as immediately available.
/// </summary>
/// <remarks>
/// Useful as a default — and as a baseline for fuzzing where a custom
/// controller can simulate slow chip startup, intermittent CNF responses,
/// or refusal to join.
/// </remarks>
public sealed class SimulatedChipController : IPlcChipController
{
    /// <summary>The NMK passed to <see cref="SetKeyAsync"/>, or null if not yet called.</summary>
    public Byte[]? LastNmk { get; private set; }

    /// <summary>The NID passed to <see cref="SetKeyAsync"/>, or null if not yet called.</summary>
    public Byte[]? LastNid { get; private set; }

    /// <summary>Optional artificial delay before SetKey returns. Default: zero.</summary>
    public TimeSpan SetKeyDelay { get; init; } = TimeSpan.Zero;

    /// <summary>Optional artificial delay before WaitForAvlnReady returns. Default: zero.</summary>
    public TimeSpan AvlnReadyDelay { get; init; } = TimeSpan.Zero;

    public async Task SetKeyAsync(byte[] nid, byte[] nmk, CancellationToken cancellationToken = default)
    {
        if (SetKeyDelay > TimeSpan.Zero)
            await Task.Delay(SetKeyDelay, cancellationToken).ConfigureAwait(false);

        LastNid = (byte[]) nid.Clone();
        LastNmk = (byte[]) nmk.Clone();
    }

    public async Task WaitForAvlnReadyAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (AvlnReadyDelay > TimeSpan.Zero)
            await Task.Delay(AvlnReadyDelay, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
