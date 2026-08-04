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

namespace Vanaheimr.V2G.Simulation.Timing
{
    /// <summary>
    /// The EVCC-side poll-loop backoff (e.g. while waiting for <c>EVSEProcessing.Finished</c> during
    /// Authorization/ChargeParameterDiscovery/ChargeLoop) goes through this seam instead of a hardcoded
    /// <c>Task.Delay</c>/<c>Thread.Sleep</c>, so tests can make polling loops run instantly instead of
    /// waiting on the real wall clock. See <see cref="System.TimeProvider"/> (constructor-injected
    /// directly, no wrapper needed) for the separate concern of elapsed-time/timeout checks.
    /// </summary>
    public interface IAsyncDelay
    {
        Task Wait(TimeSpan duration, CancellationToken ct = default);
    }
}
