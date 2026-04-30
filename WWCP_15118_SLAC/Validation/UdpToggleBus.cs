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

using System.Net;
using System.Net.Sockets;

namespace cloud.charging.open.protocols.ISO15118.SLAC.Validation;

/// <summary>
/// UDP-bus simulation of the CP pilot wire for cross-process demos. The EV
/// "drives" toggles by calling <see cref="UdpToggleEmitter.Emit"/>, which
/// sends a single byte to a fixed UDP port; one or more EVSEs running a
/// <see cref="UdpToggleObserver"/> on that port see one toggle per byte.
///
/// This stands in for the physical CP wire in the demo. In production both
/// sides observe a real GPIO/ADC signal.
/// </summary>
public sealed class UdpToggleEmitter : IToggleSource, IDisposable
{
    private readonly UdpClient   _socket;
    private readonly IPEndPoint  _busEndpoint;
    private          byte        _produced;

    public UdpToggleEmitter(IPEndPoint busEndpoint)
    {
        _busEndpoint = busEndpoint;
        _socket      = new UdpClient();
    }

    /// <summary>Send one toggle on the simulated CP wire and bump the local count.</summary>
    public void Emit()
    {
        try
        {
            _socket.Send(new Byte[] { 0xCC }, 1, _busEndpoint);
        }
        catch { /* bus may not have a listener; OK */ }

        if (_produced < byte.MaxValue) _produced++;
    }

    public void Reset() => _produced = 0;
    public byte GetCount() => _produced;

    public void Dispose() => _socket.Dispose();
}

/// <summary>
/// EVSE-side counterpart. Listens on the CP-bus UDP port and increments its
/// observed count for every byte received during a validation window.
/// </summary>
public sealed class UdpToggleObserver : IToggleObserver, IDisposable
{
    private readonly UdpClient               _socket;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task                    _loop;
    private          byte                    _seen;
    private          int                     _baseline;
    private          int                     _absolute;

    public UdpToggleObserver(IPEndPoint listenOn)
    {
        _socket = new UdpClient(listenOn);
        _loop   = Task.Run(ReceiveLoopAsync);
    }

    public void Reset() => _baseline = Volatile.Read(ref _absolute);

    public byte GetCount()
    {
        var d = Volatile.Read(ref _absolute) - _baseline;
        return (byte) Math.Clamp(d, 0, 255);
    }

    private async Task ReceiveLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                _ = await _socket.ReceiveAsync(_cts.Token).ConfigureAwait(false);
                Interlocked.Increment(ref _absolute);
            }
            catch { return; }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _socket.Close();
        try { _loop.Wait(TimeSpan.FromSeconds(1)); } catch { }
        _socket.Dispose();
        _cts.Dispose();
    }
}
