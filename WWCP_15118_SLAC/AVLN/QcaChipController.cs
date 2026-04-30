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
using cloud.charging.open.protocols.ISO15118.SLAC.Transport.Linux;
using org.GraphDefined.Vanaheimr.Hermod.Ethernet;
using System.Runtime.Versioning;
using System.Threading.Channels;

namespace cloud.charging.open.protocols.ISO15118.SLAC.Avln;

/// <summary>
/// Real-hardware implementation of <see cref="IPlcChipController"/> for
/// qca7000-class HomePlug GreenPHY chips on Linux. Programs the negotiated
/// NMK/NID into the chip via the standard CM_SET_KEY.REQ / CM_SET_KEY.CNF
/// MME pair, addressed to the chip's own MAC address.
/// </summary>
/// <remarks>
/// <para>The host CPU and the PLC chip exchange this MME locally — it
/// never goes onto the actual powerline. The chip then leaves its initial
/// solo-AVLN and joins the EV-EVSE AVLN whose NMK was just negotiated by
/// SLAC. After CNF the host should wait briefly for the chip to complete
/// the join (CCo negotiation, beacon sync) before exchanging IPv6 traffic.</para>
///
/// <para>Linux only; requires CAP_NET_RAW (same as <c>AfPacketSocket</c>).</para>
///
/// <para>Tested chips: any HPGP module that accepts standard CM_SET_KEY
/// (the QCA reference firmware does, and so do most derivatives like
/// Codico and i2SE modules). The chip's MAC is auto-detected from the
/// interface; pass <paramref name="chipMacOverride"/> if your hardware
/// uses a different convention.</para>
/// </remarks>
[SupportedOSPlatform("linux")]

public sealed class QcaChipController : IPlcChipController
{
    private readonly AfPacketSocket           _socket;
    private readonly MACAddress               _chipMac;
    private readonly Channel<SetKeyCnf>       _cnfInbox = Channel.CreateUnbounded<SetKeyCnf>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
    private readonly CancellationTokenSource  _cts      = new();
    private readonly Thread                   _receiveThread;
    private          int                      _disposed;

    /// <param name="interfaceName">Linux interface name, e.g. "eth1".</param>
    /// <param name="chipMacOverride">
    /// Optional override for the chip's MAC address. By default the chip MAC
    /// is taken to be the interface MAC (true on most qca7000 boards where
    /// the chip's MAC IS the interface MAC).
    /// </param>
    public QcaChipController(string interfaceName, MACAddress? chipMacOverride = null)
    {
        _socket  = new AfPacketSocket(interfaceName);
        _chipMac = chipMacOverride ?? _socket.LocalMac;

        _receiveThread = new Thread(ReceiveLoop)
        {
            IsBackground = true,
            Name         = $"QCA chip controller rx ({interfaceName})"
        };
        _receiveThread.Start();
    }

    public async Task SetKeyAsync(byte[] nid, byte[] nmk, CancellationToken cancellationToken = default)
    {
        var req   = SetKeyReq.ForNmk(nid, nmk);

        // The MME is addressed to the chip itself. The kernel delivers it
        // through the ethN device to the chip's local management endpoint.
        var frame = ManagementMessageEntry.Encode(_chipMac, _socket.LocalMac, req);
        _socket.Send(frame);

        // Wait for CNF. A real chip responds within milliseconds.
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        SetKeyCnf cnf;
        try
        {
            cnf = await _cnfInbox.Reader.ReadAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                "Chip did not respond with CM_SET_KEY.CNF within 2 s. " +
                "Is the interface up? Try `sudo ip link show " + _socket.InterfaceName + "`.");
        }

        if (!cnf.IsSuccess)
            throw new InvalidOperationException(
                $"Chip rejected CM_SET_KEY.REQ (Result=0x{cnf.Result:X2}).");
    }

    public async Task WaitForAvlnReadyAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        // After CM_SET_KEY.CNF the chip still has to leave its old AVLN, do
        // CCo negotiation with the peer, and synchronise to beacons. There
        // is no single "AVLN ready" MME exposed by the QCA firmware. The
        // pragmatic approach (also used by open-plc-utils' `int6kkey`) is
        // to wait a fixed grace period — typically 1–2 seconds is enough.
        //
        // Production code should additionally probe with CM_AMP_MAP.REQ
        // (or a similar status MME) to confirm the peer is reachable, but
        // for a "transition layer to SDP" the time-based wait suffices.
        var grace = timeout < TimeSpan.FromSeconds(2) ? timeout : TimeSpan.FromSeconds(2);
        await Task.Delay(grace, cancellationToken).ConfigureAwait(false);
    }

    private void ReceiveLoop()
    {
        var buffer = new Byte[2048];
        while (!_cts.IsCancellationRequested)
        {
            int n;
            try               { n = _socket.Receive(buffer); }
            catch (Exception) { return; }

            if (n < 0) return;

            var decoded = ManagementMessageEntry.TryDecode(buffer.AsSpan(0, n));
            if (decoded?.Message is SetKeyCnf cnf)
                _cnfInbox.Writer.TryWrite(cnf);
            // Other frames belong to the SLAC transport on the same iface
            // (which has its own AfPacketSocket and will see them too).
        }
    }

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return ValueTask.CompletedTask;

        _cts.Cancel();
        _cnfInbox.Writer.TryComplete();
        _socket.Dispose();
        try { _receiveThread.Join(TimeSpan.FromSeconds(1)); } catch { }
        _cts.Dispose();
        return ValueTask.CompletedTask;
    }
}
