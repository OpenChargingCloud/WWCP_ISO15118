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

namespace cloud.charging.open.protocols.ISO15118.SLAC.Validation;

/// <summary>
/// Abstraction for the EV-side source of CP-toggle counts. The state
/// machine asks the toggle source how many toggles the EV has produced
/// since the last CM_VALIDATE round started, and this count is reported
/// back to the EVSE in the next CM_VALIDATE.CNF.
/// </summary>
/// <remarks>
/// Production implementation: reads the actual CP pilot signal state
/// changes (state B ↔ state C transitions) via the GPIO / ADC of the
/// vehicle's charger.
///
/// Demo implementation: simulated, see <see cref="SimulatedToggleSource"/>.
/// </remarks>
public interface IToggleSource
{
    /// <summary>
    /// Tell the source to start counting. Any toggles that occurred before
    /// this call must not contribute to subsequent <see cref="GetCount"/>
    /// returns.
    /// </summary>
    void Reset();

    /// <summary>The number of toggles observed since the last <see cref="Reset"/>.</summary>
    byte GetCount();
}

/// <summary>
/// Abstraction for the EVSE-side observer of CP-toggle counts. The EVSE
/// observes the EV's CP-signal state changes, and this observer reports
/// how many it has counted since validation started.
/// </summary>
/// <remarks>
/// Production: reads the actual CP signal at the EVSE's pilot input.
/// Demo: simulated, see <see cref="SimulatedToggleObserver"/>.
/// </remarks>
public interface IToggleObserver
{
    /// <summary>Reset the observed count to zero.</summary>
    void Reset();

    /// <summary>The number of toggles observed since the last <see cref="Reset"/>.</summary>
    byte GetCount();

    /// <summary>
    /// Wait until either the observer believes the EV has finished toggling,
    /// or the timeout expires. The default no-op returns immediately and lets
    /// the EVSE-side state machine drive timing through its own polling.
    /// </summary>
    Task WaitForToggleActivityAsync(TimeSpan timeout, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
