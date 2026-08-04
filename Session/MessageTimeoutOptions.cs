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

namespace Vanaheimr.V2G.Simulation.Session
{
    /// <summary>
    /// Simplified timeout budgets for a simulated session — not the exact per-message values from the
    /// ISO 15118 performance-timeout tables, just workable defaults for a loopback/interop session.
    /// </summary>
    public sealed record MessageTimeoutOptions(TimeSpan PerMessageTimeout, TimeSpan SequenceTimeout)
    {
        public static MessageTimeoutOptions Default { get; } = new(
            PerMessageTimeout: TimeSpan.FromSeconds(2),
            SequenceTimeout: TimeSpan.FromSeconds(60));
    }
}
