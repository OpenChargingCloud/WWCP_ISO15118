/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

#region Usings

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

using cloud.charging.open.protocols.ISO15118.T1S.Messages;

#endregion

namespace cloud.charging.open.protocols.ISO15118.T1S.Transport
{

    /// <summary>
    /// One node's attachment to the medium: a MAC address, a way to put a
    /// frame on the wire, and every frame off the wire that was addressed
    /// to this node or to everybody.
    /// </summary>
    /// <remarks>
    /// The same seam the SLAC library has between its state machines and
    /// its media, and for the same reason: everything above this speaks
    /// Ethernet frames and nothing above this knows what carries them. The
    /// emulation carries them over UDP multicast; a real 10BASE-T1S adapter
    /// would carry them over AF_PACKET, and would be another implementation
    /// of this, with nothing above it to change.
    /// </remarks>
    public interface IT1STransport : IAsyncDisposable
    {

        /// <summary>
        /// This node's MAC address on the medium.
        /// </summary>
        MACAddress  LocalMac     { get; }

        /// <summary>
        /// What this medium is, for a log: "UDP multicast 239.151.18.1:16118".
        /// </summary>
        String      Description  { get; }

        /// <summary>
        /// Every frame of ours off the medium that this node should see:
        /// addressed to it, or to everybody. Frames this node sent itself are
        /// not among them, however the medium delivers them.
        /// </summary>
        event EventHandler<DecodedT1SFrame>?  FrameReceived;

        /// <summary>
        /// Attach to the medium and start receiving.
        /// </summary>
        Task StartAsync(CancellationToken CancellationToken = default);

        /// <summary>
        /// Put one message on the wire, framed from this node.
        /// </summary>
        /// <param name="Destination">A node, or the broadcast address.</param>
        /// <param name="Message">What to say.</param>
        Task SendAsync(MACAddress         Destination,
                       IT1SMessage        Message,
                       CancellationToken  CancellationToken = default);

        /// <summary>
        /// Put a frame on the wire exactly as given - for tools that need the
        /// bytes untouched: a fuzzer, a replayer, a node with a wrong source
        /// address on purpose.
        /// </summary>
        Task SendRawAsync(EthernetFrame      Frame,
                          CancellationToken  CancellationToken = default);

    }

}
