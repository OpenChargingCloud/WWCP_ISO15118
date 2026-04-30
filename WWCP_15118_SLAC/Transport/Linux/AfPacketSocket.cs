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

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace cloud.charging.open.protocols.ISO15118.SLAC.Transport.Linux;

/// <summary>
/// Thin Linux-only wrapper around an <c>AF_PACKET / SOCK_RAW</c> socket bound
/// to a specific network interface and a specific EtherType. Provides
/// blocking <see cref="Send"/> and <see cref="Receive"/> operations on raw
/// L2 frames (the caller supplies and consumes the complete Ethernet
/// frame including DST/SRC MAC and EtherType).
/// </summary>
/// <remarks>
/// Used both by <see cref="AfPacketSlacTransport"/> (per-station SLAC
/// transport) and by <c>SlacBridge</c> (which forwards raw frames between
/// L2 and a UDP bus without doing any per-station filtering).
///
/// Requires <c>CAP_NET_RAW</c>: either run with sudo, or grant the
/// capability to a published self-contained binary with
/// <c>setcap cap_net_raw=eip ./SLAC.Demo.Bridge</c>.
/// </remarks>
[SupportedOSPlatform("linux")]
public sealed partial class AfPacketSocket : IDisposable
{
    // ------------------------------------------------------------------ const

    private const int    AF_PACKET   = 17;
    private const int    SOCK_RAW    = 3;
    private const int    SHUT_RDWR   = 2;
    private const ushort ETH_P_HPAV  = 0x88E1;

    private const int    EBADF       = 9;
    private const int    EINTR       = 4;

    // -------------------------------------------------------------- P/Invoke

    [StructLayout(LayoutKind.Sequential)]
    private struct sockaddr_ll
    {
        public ushort sll_family;
        public ushort sll_protocol;     // network byte order
        public int    sll_ifindex;
        public ushort sll_hatype;
        public byte   sll_pkttype;
        public byte   sll_halen;
        // unsigned char sll_addr[8] — laid out as 8 separate bytes
        public byte   sll_addr0, sll_addr1, sll_addr2, sll_addr3;
        public byte   sll_addr4, sll_addr5, sll_addr6, sll_addr7;
    }

    [LibraryImport("libc", SetLastError = true, EntryPoint = "socket")]
    private static partial int socket(int domain, int type, int protocol);

    [LibraryImport("libc", SetLastError = true, EntryPoint = "bind")]
    private static partial int bind(int sockfd, in sockaddr_ll addr, uint addrlen);

    [LibraryImport("libc", SetLastError = true, EntryPoint = "close")]
    private static partial int close(int fd);

    [LibraryImport("libc", SetLastError = true, EntryPoint = "shutdown")]
    private static partial int shutdown(int sockfd, int how);

    [LibraryImport("libc", SetLastError = true, EntryPoint = "if_nametoindex",
        StringMarshalling = StringMarshalling.Utf8)]
    private static partial uint if_nametoindex(string ifname);

    [LibraryImport("libc", SetLastError = true, EntryPoint = "recv")]
    private static unsafe partial nint recv(int sockfd, byte* buf, nuint len, int flags);

    [LibraryImport("libc", SetLastError = true, EntryPoint = "send")]
    private static unsafe partial nint send(int sockfd, byte* buf, nuint len, int flags);

    // ------------------------------------------------------------- instance

    private readonly int    _fd;
    private readonly object _sendLock = new();
    private          int    _disposed; // 0 = open, 1 = disposed

    /// <summary>The interface name this socket is bound to (e.g. "eth1").</summary>
    public string InterfaceName { get; }

    /// <summary>The interface's hardware MAC address.</summary>
    public MACAddress LocalMac { get; }

    /// <summary>True after <see cref="Dispose"/> has been called.</summary>
    public bool IsClosed => Volatile.Read(ref _disposed) != 0;

    /// <param name="interfaceName">Linux interface name, e.g. "eth1" or "veth0".</param>
    /// <param name="localMacOverride">
    /// If non-null, used as the station's MAC address (e.g. for testing) instead
    /// of the interface's actual MAC. The kernel does NOT rewrite the source MAC
    /// in outgoing frames, so the override is honoured on the wire.
    /// </param>
    public AfPacketSocket(string interfaceName, MACAddress? localMacOverride = null)
    {
        if (!OperatingSystem.IsLinux())
            throw new PlatformNotSupportedException(
                "AfPacketSocket requires Linux. On Windows, write a Pcap-based ISlacTransport instead.");

        InterfaceName = interfaceName;
        LocalMac      = localMacOverride ?? ReadInterfaceMac(interfaceName);

        var ifIndex = (int) if_nametoindex(interfaceName);
        if (ifIndex == 0)
        {
            var err = Marshal.GetLastPInvokeError();
            throw new IOException(
                $"if_nametoindex(\"{interfaceName}\") failed: errno={err}. " +
                $"Does the interface exist? Try `ip link show`.");
        }

        // Both `protocol` arg to socket() and `sll_protocol` in bind() are big-endian.
        var protoNbo = HostToNetworkOrder(ETH_P_HPAV);

        _fd = socket(AF_PACKET, SOCK_RAW, protoNbo);
        if (_fd < 0)
        {
            var err = Marshal.GetLastPInvokeError();
            throw new IOException(
                $"socket(AF_PACKET, SOCK_RAW, ETH_P_HPAV) failed: errno={err}. " +
                $"Need CAP_NET_RAW (try `sudo` or `setcap cap_net_raw=eip <binary>`).");
        }

        var addr = new sockaddr_ll
        {
            sll_family   = AF_PACKET,
            sll_protocol = protoNbo,
            sll_ifindex  = ifIndex
        };

        var rc = bind(_fd, addr, (uint) Marshal.SizeOf<sockaddr_ll>());
        if (rc < 0)
        {
            var err = Marshal.GetLastPInvokeError();
            close(_fd);
            throw new IOException(
                $"bind() to {interfaceName} failed: errno={err}.");
        }
    }

    /// <summary>
    /// Send one complete L2 frame (must include DST/SRC MAC and EtherType
    /// 0x88E1). Thread-safe; sends are serialised by an internal lock.
    /// </summary>
    public void Send(ReadOnlySpan<Byte> frame)
    {
        if (frame.Length < 14)
            throw new ArgumentException("Frame too short for an Ethernet header.", nameof(frame));

        ThrowIfClosed();

        lock (_sendLock)
        {
            unsafe
            {
                fixed (byte* p = frame)
                {
                    var sent = send(_fd, p, (nuint) frame.Length, 0);
                    if (sent < 0)
                    {
                        var err = Marshal.GetLastPInvokeError();
                        throw new IOException($"send() failed: errno={err}.");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Block until a frame is received. Returns the number of bytes copied
    /// into <paramref name="buffer"/>, or -1 if the socket was shut down
    /// while we were blocked (clean shutdown signal).
    /// </summary>
    public int Receive(Span<byte> buffer)
    {
        while (true)
        {
            int n;
            unsafe
            {
                fixed (byte* p = buffer)
                {
                    n = (int) recv(_fd, p, (nuint) buffer.Length, 0);
                }
            }

            if (n >= 0) return n;

            var err = Marshal.GetLastPInvokeError();
            if (err == EINTR) continue;     // interrupted by signal — retry
            if (IsClosed || err == EBADF) return -1;

            throw new IOException($"recv() failed: errno={err}.");
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

        // shutdown() unblocks any pending recv() so the receive thread exits.
        try { shutdown(_fd, SHUT_RDWR); } catch { }
        try { close(_fd);                } catch { }
    }

    // ------------------------------------------------------------- helpers

    private void ThrowIfClosed()
    {
        if (IsClosed) throw new ObjectDisposedException(nameof(AfPacketSocket));
    }

    private static ushort HostToNetworkOrder(ushort value)
        => BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReverseEndianness(value)
            : value;

    private static MACAddress ReadInterfaceMac(string ifname)
    {
        // sysfs is the cleanest way to read a NIC MAC without ioctl P/Invoke.
        var path = $"/sys/class/net/{ifname}/address";
        if (!File.Exists(path))
            throw new IOException($"Interface '{ifname}' has no MAC at {path}.");

        var line = File.ReadAllText(path).Trim();
        return MACAddress.Parse(line);
    }
}
