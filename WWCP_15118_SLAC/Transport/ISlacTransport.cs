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

using cloud.charging.open.protocols.ISO15118.SLAC.Messages;
using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

namespace cloud.charging.open.protocols.ISO15118.SLAC.Transport;

/// <summary>
/// Abstract SLAC transport. The "real" implementation would use a raw L2 socket
/// (Linux: AF_PACKET, Windows: WinPcap/Npcap) to send / receive HomePlug AV
/// management messages directly on the PLC interface. The demo implementation
/// in <see cref="UdpSlacTransport"/> uses UDP so two processes can talk to each
/// other without raw-socket privileges.
/// </summary>
public interface ISlacTransport : IAsyncDisposable
{

    /// <summary>
    /// Local MAC address of this station (PEV or EVSE).
    /// </summary>
    MACAddress LocalMac { get; }

    /// <summary>Send a SLAC message. <paramref name="destination"/> may be the broadcast MAC.</summary>
    Task SendAsync(MACAddress destination, ISlacMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a verbatim L2 frame, including Ethernet header, MMV, MMTYPE, and body.
    /// Used by tools (fuzzers, replayers) that need to put bytes on the wire
    /// exactly as supplied, without any re-encoding by the transport.
    /// Implementations bypass <see cref="ManagementMessageEntry.Encode"/>.
    /// </summary>
    Task SendRawAsync(Byte[] frame, CancellationToken cancellationToken = default);

    /// <summary>Raised whenever a frame addressed to this station (or to broadcast) arrives.</summary>
    event EventHandler<DecodedFrame>? FrameReceived;

    /// <summary>Start receiving frames in the background.</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

}
