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
    /// Where a station reports the energy it delivered, when it reports anywhere.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One method, because one method is all a state machine ever calls. Whatever is on the other side —
    /// a recorder that writes a corpus file, or a real CSMS-facing stack — is none of the session's
    /// business, and the point of this interface is to keep it that way.
    /// </para>
    /// <para>
    /// <b>Why it exists.</b> The station's <c>Backend</c> used to be
    /// <c>Func&lt;string, OcppTransactionRecorder&gt;</c> — a delegate whose <em>return type</em> named a
    /// concrete OCPP class, so the ISO 15118 session code depended on a different protocol's recorder to
    /// compile. That is not a seam, it only looks like one. OCPP is a backend concern and belongs outside
    /// an ISO 15118 implementation; in real operation what hangs here is a real OCPP project, and what
    /// hangs here in this project's tests is a stub that writes the third leg of the metering comparison.
    /// </para>
    /// </remarks>
    public interface ISessionBackend
    {

        /// <summary>
        /// Books one reading against the transaction this backend was opened for.
        /// </summary>
        /// <param name="wattHours">The meter's reading, not a delta.</param>
        /// <param name="unixSeconds">When it was taken.</param>
        /// <param name="signature">
        /// The signature over that reading — and it must be the very bytes that went out on the wire.
        /// A re-signed value would still verify and would silently destroy the only comparison this
        /// record exists to enable.
        /// </param>
        /// <param name="meterPublicKey">The meter's public key, so the record can be checked without it.</param>
        void Sample(ulong wattHours,
                    long unixSeconds,
                    string? signature = null,
                    string? meterPublicKey = null);

    }

}
