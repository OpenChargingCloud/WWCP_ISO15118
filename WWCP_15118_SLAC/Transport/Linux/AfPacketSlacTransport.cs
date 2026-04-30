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

using System.Runtime.Versioning;
using cloud.charging.open.protocols.ISO15118.SLAC.Messages;
using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

namespace cloud.charging.open.protocols.ISO15118.SLAC.Transport.Linux;

/// <summary>
/// SLAC transport over a real Linux PLC interface using AF_PACKET.
/// One process = one station. Suitable for running against actual HPGP
/// hardware (qca7000-based adapters, Codico modules, …) or against a
/// veth pair for hardware-free integration tests.
/// </summary>
/// <remarks>
/// Compared to <see cref="UdpSlacTransport"/>:
/// <list type="bullet">
///   <item>Frames travel on the actual L2 medium with EtherType 0x88E1.</item>
///   <item>No bootstrap peer list — the PLC line is a shared medium, like
///         a hub, so broadcasts reach every station automatically.</item>
///   <item>Requires root or <c>CAP_NET_RAW</c>.</item>
///   <item>Linux only. On Windows, write a Pcap-backed alternative.</item>
/// </list>
/// </remarks>
[SupportedOSPlatform("linux")]
public sealed class AfPacketSlacTransport : ISlacTransport
{
    private readonly AfPacketSocket _socket;
    private readonly Thread         _receiveThread;
    private          bool           _started;

    public MACAddress LocalMac => _socket.LocalMac;

    public event EventHandler<DecodedFrame>? FrameReceived;

    /// <param name="interfaceName">Linux interface name, e.g. "eth1" or "veth0".</param>
    /// <param name="localMacOverride">
    /// Optional MAC override (mainly useful for tests). If null, the interface's
    /// real MAC is used.
    /// </param>
    public AfPacketSlacTransport(string interfaceName, MACAddress? localMacOverride = null)
    {

        _socket = new AfPacketSocket(interfaceName, localMacOverride);

        _receiveThread = new Thread(ReceiveLoop) {
            IsBackground = true,
            Name         = $"SLAC L2 receive ({interfaceName})"
        };

    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_started) throw new InvalidOperationException("Already started.");
        _started = true;
        _receiveThread.Start();
        return Task.CompletedTask;
    }

    public Task SendAsync(MACAddress destination, ISlacMessage message, CancellationToken cancellationToken = default)
    {
        // L2 is a shared medium: a single send reaches every station whose
        // hardware accepts the destination MAC (NIC filter for unicast,
        // everyone for broadcast). No per-peer fanout needed.
        var frame = ManagementMessageEntry.Encode(destination, LocalMac, message);
        _socket.Send(frame);
        return Task.CompletedTask;
    }

    public Task SendRawAsync(byte[] frame, CancellationToken cancellationToken = default)
    {
        if (frame.Length < ManagementMessageEntry.EthernetHeaderLength)
            throw new ArgumentException("Frame too short for an Ethernet header.", nameof(frame));
        _socket.Send(frame);
        return Task.CompletedTask;
    }

    private void ReceiveLoop()
    {
        var buffer = new Byte[2048];

        while (true)
        {
            int n;
            try
            {
                n = _socket.Receive(buffer);
            }
            catch (Exception)
            {
                // Socket was disposed or hit a fatal error — exit cleanly.
                return;
            }

            if (n < 0) return;          // shutdown signal
            if (n < ManagementMessageEntry.EthernetHeaderLength) continue;

            var decoded = ManagementMessageEntry.TryDecode(buffer.AsSpan(0, n));
            if (decoded is null) continue;

            // The kernel already filters to ETH_P_HPAV via the protocol arg
            // we passed to socket(). The NIC also filters unicast frames not
            // addressed to its MAC (unless promiscuous). We re-check anyway
            // so promiscuous-mode captures don't leak into the state machine.
            if (decoded.Destination != LocalMac && !decoded.Destination.IsBroadcast)
                continue;

            try   { FrameReceived?.Invoke(this, decoded); }
            catch { /* user handler threw — keep the loop alive */ }
        }
    }

    public ValueTask DisposeAsync()
    {
        _socket.Dispose();              // unblocks the receive thread
        if (_started) _receiveThread.Join(TimeSpan.FromSeconds(1));
        return ValueTask.CompletedTask;
    }
}
