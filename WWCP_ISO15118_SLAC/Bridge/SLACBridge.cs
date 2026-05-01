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

#region Usings

using System.Net;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Collections.Concurrent;

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

using cloud.charging.open.protocols.ISO15118.SLAC.Transport.Linux;

#endregion

namespace cloud.charging.open.protocols.ISO15118.SLAC.Bridge
{

    /// <summary>
    /// Hybrid bridge between a real PLC interface (AF_PACKET / EtherType 0x88E1)
    /// and a UDP "bus" populated by simulated stations. Mixes real and simulated
    /// SLAC participants on a single logical medium — useful for pentesting,
    /// fuzzing, regression testing real EVSEs / PEVs, and protocol monitoring.
    /// </summary>
    /// <remarks>
    /// <para>Forwarding rules:</para>
    /// <list type="bullet">
    ///   <item>L2 frame received → forwarded to every known UDP peer.</item>
    ///   <item>UDP frame received → written to L2, AND forwarded to every other
    ///         known UDP peer (so simulated stations talk to each other through
    ///         the bridge in hub mode). The sender is never reflected back.</item>
    /// </list>
    ///
    /// <para>Peer discovery on the UDP side:</para>
    /// <list type="bullet">
    ///   <item><b>Configured peers</b>: passed in to the constructor. Use these
    ///         for long-lived sim stations (e.g. a sim EVSE listening on a
    ///         well-known port) that don't initiate traffic.</item>
    ///   <item><b>Learned peers</b>: any UDP source that sends a frame is
    ///         remembered (source MAC → source UDP endpoint). Sim stations that
    ///         initiate (e.g. a sim EV broadcasting CM_SLAC_PARM.REQ) are picked
    ///         up automatically.</item>
    /// </list>
    ///
    /// <para>Loop prevention: outgoing AF_PACKET frames are not echoed back to
    /// the sender by the kernel. UDP reflection skips the source endpoint.
    /// There is no L2→L2 or UDP→same-UDP path.</para>
    /// </remarks>
    [SupportedOSPlatform("linux")]
    public sealed class SLACBridge : IAsyncDisposable
    {

        private readonly AfPacketSocket                                _l2;
        private readonly UdpClient                                     _udp;
        private readonly IPEndPoint                                    _udpListenOn;
        private readonly List<IPEndPoint>                              _configuredPeers;
        private readonly ConcurrentDictionary<MACAddress, IPEndPoint>  _learnedPeers = new();
        private readonly CancellationTokenSource                       _cts          = new();
        private          Thread?                                       _l2Thread;
        private          Task?                                         _udpTask;
        private          int                                           _disposed;

        /// <summary>The interface name (e.g. "eth1") this bridge is attached to.</summary>
        public string InterfaceName => _l2.InterfaceName;

        /// <summary>The UDP endpoint the bridge listens on for sim stations.</summary>
        public IPEndPoint UdpEndpoint => _udpListenOn;

        /// <summary>Snapshot of currently learned (sim MAC → UDP endpoint) mappings.</summary>
        public IReadOnlyDictionary<MACAddress, IPEndPoint> LearnedPeers
            => _learnedPeers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        /// <summary>Fired for each frame the bridge forwards. Useful for monitoring/sniffing.</summary>
        public event EventHandler<SLACBridgedFrameEventArgs>? FrameForwarded;

        /// <summary>Fired for plain-text status messages.</summary>
        public event EventHandler<string>? Log;

        public SLACBridge(string interfaceName, IPEndPoint udpListenOn, IEnumerable<IPEndPoint>? configuredPeers = null)
        {
            _l2              = new AfPacketSocket(interfaceName);
            _udp             = new UdpClient(udpListenOn);
            _udpListenOn     = udpListenOn;
            _configuredPeers = configuredPeers?.ToList() ?? new List<IPEndPoint>();
        }

        public Task StartAsync()
        {
            _l2Thread = new Thread(L2ReceiveLoop)
            {
                IsBackground = true,
                Name         = $"SLAC bridge L2 rx ({InterfaceName})"
            };
            _l2Thread.Start();

            _udpTask = Task.Run(() => UdpReceiveLoopAsync(_cts.Token));

            Info($"Bridge active. Iface: {InterfaceName} (MAC {_l2.LocalMac})  UDP: {_udpListenOn}  " +
                 $"configured peers: {_configuredPeers.Count}");
            return Task.CompletedTask;
        }

        // ----------------------------------------------------------- L2 → UDP --

        private void L2ReceiveLoop()
        {
            var buffer = new Byte[2048];

            while (!_cts.IsCancellationRequested)
            {
                int n;
                try               { n = _l2.Receive(buffer); }
                catch (Exception) { return; }

                if (n < 0) return;
                if (n < ManagementMessageEntry.EthernetHeaderLength) continue;

                // Copy out so the buffer is reusable while async sends complete.
                var frame   = buffer.AsSpan(0, n).ToArray();
                var decoded = ManagementMessageEntry.TryDecode(frame);

                foreach (var ep in SnapshotPeers())
                {
                    try   { _udp.Send(frame, frame.Length, ep); }
                    catch { /* ignore individual send failures */ }
                }

                if (decoded is not null)
                {
                    FrameForwarded?.Invoke(this, new SLACBridgedFrameEventArgs(
                        SLACBridgeDirection.L2ToUdp, decoded, Source: null));
                    Trace($"L2→UDP  {decoded.Source} → {decoded.Destination}  {decoded.Message.MmType}");
                }
            }
        }

        // ----------------------------------------------------------- UDP → L2 --

        private async Task UdpReceiveLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                UdpReceiveResult res;
                try
                {
                    res = await _udp.ReceiveAsync(token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }
                catch (ObjectDisposedException)    { break; }

                if (res.Buffer.Length < ManagementMessageEntry.EthernetHeaderLength) continue;

                // Learn: source MAC of this frame currently lives at this UDP endpoint.
                var srcMac = MACAddress.From(res.Buffer.AsSpan(6, 6));
                _learnedPeers[srcMac] = res.RemoteEndPoint;

                var decoded = ManagementMessageEntry.TryDecode(res.Buffer);

                // 1. Cross to L2.
                try   { _l2.Send(res.Buffer); }
                catch (Exception ex) { Info($"UDP→L2 send failed: {ex.Message}"); }

                // 2. Hub-mode reflection to every OTHER UDP peer.
                foreach (var ep in SnapshotPeers())
                {
                    if (ep.Equals(res.RemoteEndPoint)) continue;
                    try   { await _udp.SendAsync(res.Buffer, ep, token).ConfigureAwait(false); }
                    catch { /* ignore */ }
                }

                if (decoded is not null)
                {
                    FrameForwarded?.Invoke(this, new SLACBridgedFrameEventArgs(
                        SLACBridgeDirection.UdpToL2, decoded, Source: res.RemoteEndPoint));
                    Trace($"UDP→L2  {decoded.Source} → {decoded.Destination}  {decoded.Message.MmType}  " +
                          $"(from {res.RemoteEndPoint})");
                }
            }
        }


        #region Helpers

        private List<IPEndPoint> SnapshotPeers()
        {
            // Dedupe in case a configured peer also got learned.
            var set = new HashSet<IPEndPoint>(_configuredPeers);
            foreach (var ep in _learnedPeers.Values) set.Add(ep);
            return set.ToList();
        }

        private void Info (string msg) => Log?.Invoke(this, msg);
        private void Trace(string msg) => Log?.Invoke(this, msg);

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

            _cts.Cancel();
            try { _udp.Close();  } catch { }
            _l2.Dispose();

            if (_l2Thread is not null) _l2Thread.Join(TimeSpan.FromSeconds(1));
            if (_udpTask  is not null)
            {
                try { await _udpTask.ConfigureAwait(false); } catch { }
            }

            try { _udp.Dispose(); } catch { }
            _cts.Dispose();
        }

        #endregion

    }

}
