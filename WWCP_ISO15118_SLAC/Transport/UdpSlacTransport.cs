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

using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

using cloud.charging.open.protocols.ISO15118.SLAC.Messages;

namespace cloud.charging.open.protocols.ISO15118.SLAC.Transport;

/// <summary>
/// UDP-based SLAC transport for the demo. Each station listens on its own UDP
/// port; "broadcasts" go to every known peer endpoint, "unicasts" to a single
/// peer looked up by destination MAC. Filtering by destination MAC happens at
/// the receiver, exactly the way an L2 station would filter frames on the PLC
/// line.
///
/// Endpoints are learned automatically from incoming frames (source MAC →
/// source UDP endpoint), so an EVSE can start with no peers configured and
/// pick up multiple PEVs as their CM_SLAC_PARM.REQ broadcasts arrive.
/// A bootstrap peer list is supported for the initiating side (the PEV needs
/// to know where to send its first broadcast).
///
/// This keeps the on-the-wire byte format identical to the real HomePlug AV
/// MME format — only the carrier (UDP instead of raw L2) differs.
/// </summary>
public sealed class UdpSlacTransport : ISlacTransport
{

    private readonly UdpClient                                    _socket;
    private readonly List<IPEndPoint>                             _bootstrapPeers;
    private readonly ConcurrentDictionary<MACAddress, IPEndPoint> _learnedPeers = new();
    private readonly SemaphoreSlim                                _sendLock     = new(1, 1);
    private readonly CancellationTokenSource                      _cts          = new();
    private          Task?                                        _receiveLoop;

    public MACAddress LocalMac { get; }

    public event EventHandler<DecodedFrame>? FrameReceived;

    public UdpSlacTransport(
        MACAddress localMac,
        IPEndPoint listenOn,
        IEnumerable<IPEndPoint>? bootstrapPeers = null)
    {
        LocalMac        = localMac;
        _bootstrapPeers = bootstrapPeers?.ToList() ?? new List<IPEndPoint>();
        _socket         = new UdpClient(listenOn);
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _receiveLoop = Task.Run(() => ReceiveLoopAsync(_cts.Token), _cts.Token);
        return Task.CompletedTask;
    }

    public async Task SendAsync(MACAddress destination, ISlacMessage message, CancellationToken cancellationToken = default)
    {
        var frame = ManagementMessageEntry.Encode(destination, LocalMac, message);
        await SendBytesAsync(destination, frame, cancellationToken).ConfigureAwait(false);
    }

    public Task SendRawAsync(byte[] frame, CancellationToken cancellationToken = default)
    {
        if (frame.Length < ManagementMessageEntry.EthernetHeaderLength)
            throw new ArgumentException("Frame too short for an Ethernet header.", nameof(frame));

        // Pull the destination MAC straight from the frame so the transport
        // routes to the correct UDP endpoint(s) — without altering the bytes.
        var dst = MACAddress.From(frame.AsSpan(0, 6));
        return SendBytesAsync(dst, frame, cancellationToken);
    }

    private async Task SendBytesAsync(MACAddress destination, byte[] frame, CancellationToken cancellationToken)
    {
        var targets = ResolveTargets(destination);

        // UdpClient is not safe for concurrent sends — serialise across all sessions.
        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            foreach (var ep in targets)
                await _socket.SendAsync(frame, ep, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private List<IPEndPoint> ResolveTargets(MACAddress destination)
    {
        if (destination.IsBroadcast)
        {
            // All known peers: bootstrap (configured) plus everyone we have heard from.
            var all = new HashSet<IPEndPoint>(_bootstrapPeers);
            foreach (var ep in _learnedPeers.Values) all.Add(ep);
            return all.ToList();
        }

        // Unicast: prefer a learned mapping; fall back to bootstrap peers if we
        // have not yet seen a frame from this MAC.
        if (_learnedPeers.TryGetValue(destination, out var single))
            return new List<IPEndPoint> { single };

        return _bootstrapPeers.ToList();
    }

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            UdpReceiveResult result;
            try
            {
                result = await _socket.ReceiveAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException)    { break; }

            var decoded = ManagementMessageEntry.TryDecode(result.Buffer);
            if (decoded is null) continue;

            // Filter exactly like an L2 station: accept frames addressed to
            // our MAC or to the broadcast MAC; drop everything else.
            if (decoded.Destination != LocalMac && !decoded.Destination.IsBroadcast)
                continue;

            // Learn: this peer's MAC currently lives at this UDP endpoint.
            // Last-write-wins on roaming, which mirrors how a real L2 fabric
            // would update its forwarding tables.
            _learnedPeers[decoded.Source] = result.RemoteEndPoint;

            FrameReceived?.Invoke(this, decoded);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _socket.Close();

        if (_receiveLoop is not null)
        {
            try { await _receiveLoop.ConfigureAwait(false); } catch { /* ignore */ }
        }

        _cts.Dispose();
        _sendLock.Dispose();
        _socket.Dispose();
    }

}
