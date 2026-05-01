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

namespace cloud.charging.open.protocols.ISO15118.SLAC.Avln
{

    /// <summary>
    /// Configures the local PLC chip after a successful SLAC match: programs
    /// the negotiated NMK and NID into the chip so it can leave its initial
    /// AVLN and join the EV-EVSE AVLN. Once <see cref="WaitForAvlnReadyAsync"/>
    /// returns, IPv6 traffic across the PLC link is possible — at that point
    /// the next protocol layer (typically SDP / ISO 15118) can take over.
    /// </summary>
    /// <remarks>
    /// <para>The two production-relevant implementations:</para>
    /// <list type="bullet">
    ///   <item><see cref="SimulatedChipController"/> — for UDP-bus simulation.
    ///         Treats the AVLN as "always ready" because the simulated transport
    ///         delivers frames regardless of NMK state.</item>
    ///   <item>QcaChipController (not part of this library; see README.md
    ///         §"From SLAC to SDP") — sends CM_SET_KEY.REQ over AF_PACKET to a
    ///         qca7000-class chip and waits for CM_SET_KEY.CNF.</item>
    /// </list>
    ///
    /// <para>Both EV and EVSE call this on their own chip after the SLAC match
    /// completes — they do not exchange CM_SET_KEY messages between each other.</para>
    /// </remarks>
    public interface IPlcChipController : IAsyncDisposable
    {

        /// <summary>
        /// Tell the local PLC chip to switch to the given NMK / NID. Must complete
        /// before <see cref="WaitForAvlnReadyAsync"/> can succeed.
        /// </summary>
        /// <param name="nid">7-byte HomePlug AV Network Identifier.</param>
        /// <param name="nmk">16-byte HomePlug AV Network Membership Key.</param>
        /// <param name="cancellationToken">Cancellation.</param>
        Task SetKeyAsync(byte[] nid, byte[] nmk, CancellationToken cancellationToken = default);

        /// <summary>
        /// Block until the chip reports that the AVLN is up and IPv6 traffic
        /// across the PLC link is possible. Implementations may poll the chip's
        /// link-status MMEs, watch for beacons, or simply return immediately if
        /// the underlying transport doesn't model AVLNs.
        /// </summary>
        Task WaitForAvlnReadyAsync(TimeSpan timeout, CancellationToken cancellationToken = default);

    }

}
