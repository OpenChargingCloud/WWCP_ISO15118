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

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

#endregion

namespace cloud.charging.open.protocols.ISO15118.T1S.Transport.Linux
{

    /// <summary>
    /// A raw Linux socket bound to one interface and to our EtherType:
    /// blocking send and receive of whole Ethernet frames, header included.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The SLAC library has the same forty lines of P/Invoke for the powerline
    /// interface, and they are repeated here rather than referenced, because
    /// a 10BASE-T1S library that depended on the SLAC library would be saying
    /// MCS has something to do with HomePlug, which is the one thing about it
    /// that is not true.
    /// </para>
    /// <para>
    /// Needs CAP_NET_RAW: run as root, or give a published binary the
    /// capability with <c>setcap cap_net_raw=eip</c>.
    /// </para>
    /// </remarks>
    [SupportedOSPlatform("linux")]
    internal sealed partial class AfPacketT1SSocket : IDisposable
    {

        #region Data

        private const Int32   AF_PACKET  = 17;
        private const Int32   SOCK_RAW   = 3;
        private const Int32   SHUT_RDWR  = 2;

        private const Int32   EBADF      = 9;
        private const Int32   EINTR      = 4;
        private const Int32   EPERM      = 1;
        private const Int32   EACCES     = 13;

        private readonly Int32   fd;
        private readonly Lock    sendLock  = new ();
        private          Int32   disposed;

        #endregion

        #region P/Invoke

        [StructLayout(LayoutKind.Sequential)]
        private struct sockaddr_ll
        {
            public UInt16  sll_family;
            public UInt16  sll_protocol;     // network byte order
            public Int32   sll_ifindex;
            public UInt16  sll_hatype;
            public Byte    sll_pkttype;
            public Byte    sll_halen;
            public Byte    sll_addr0, sll_addr1, sll_addr2, sll_addr3;
            public Byte    sll_addr4, sll_addr5, sll_addr6, sll_addr7;
        }

        [LibraryImport("libc", SetLastError = true, EntryPoint = "socket")]
        private static partial Int32 socket(Int32 domain, Int32 type, Int32 protocol);

        [LibraryImport("libc", SetLastError = true, EntryPoint = "bind")]
        private static partial Int32 bind(Int32 sockfd, in sockaddr_ll addr, UInt32 addrlen);

        [LibraryImport("libc", SetLastError = true, EntryPoint = "close")]
        private static partial Int32 close(Int32 fd);

        [LibraryImport("libc", SetLastError = true, EntryPoint = "shutdown")]
        private static partial Int32 shutdown(Int32 sockfd, Int32 how);

        [LibraryImport("libc", SetLastError = true, EntryPoint = "if_nametoindex", StringMarshalling = StringMarshalling.Utf8)]
        private static partial UInt32 if_nametoindex(String ifname);

        [LibraryImport("libc", SetLastError = true, EntryPoint = "recv")]
        private static unsafe partial nint recv(Int32 sockfd, Byte* buf, nuint len, Int32 flags);

        [LibraryImport("libc", SetLastError = true, EntryPoint = "send")]
        private static unsafe partial nint send(Int32 sockfd, Byte* buf, nuint len, Int32 flags);

        #endregion

        #region Properties

        /// <summary>The interface this socket is bound to.</summary>
        public String      InterfaceName  { get; }

        /// <summary>The address frames go out with: the interface's own, unless overridden.</summary>
        public MACAddress  LocalMac       { get; }

        /// <summary>Whether <see cref="Dispose"/> has been called.</summary>
        public Boolean     IsClosed
            => Volatile.Read(ref disposed) != 0;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Open a raw socket on the interface, for our EtherType only.
        /// </summary>
        /// <param name="InterfaceName">The interface the adapter is, e.g. "eth1".</param>
        /// <param name="LocalMac">An address to send with instead of the interface's own. The kernel does not rewrite the source address of a raw frame, so it goes out as given.</param>
        public AfPacketT1SSocket(String       InterfaceName,
                                 MACAddress?  LocalMac  = null)
        {

            if (!OperatingSystem.IsLinux())
                throw new PlatformNotSupportedException("AF_PACKET is Linux only.");

            this.InterfaceName  = InterfaceName;
            this.LocalMac       = LocalMac ?? ReadInterfaceMac(InterfaceName);

            var index = (Int32) if_nametoindex(InterfaceName);

            if (index == 0)
                throw new IOException($"There is no network interface '{InterfaceName}' (if_nametoindex: errno {Marshal.GetLastPInvokeError()}). Try 'ip link show'.");

            // Both the protocol given to socket() and the one in the bind
            // address are in network byte order.
            var protocol = BitConverter.IsLittleEndian
                               ? BinaryPrimitives.ReverseEndianness(T1SConstants.EtherType)
                               : T1SConstants.EtherType;

            fd = socket(AF_PACKET, SOCK_RAW, protocol);

            if (fd < 0)
            {

                var errno = Marshal.GetLastPInvokeError();

                throw new IOException(
                          errno is EPERM or EACCES
                              ? $"A raw socket on '{InterfaceName}' needs CAP_NET_RAW: run as root, or 'setcap cap_net_raw=eip' on the binary (errno {errno})."
                              : $"socket(AF_PACKET, SOCK_RAW, 0x{T1SConstants.EtherType:X4}) failed with errno {errno}."
                      );

            }

            var address = new sockaddr_ll {
                              sll_family    = AF_PACKET,
                              sll_protocol  = protocol,
                              sll_ifindex   = index
                          };

            if (bind(fd, address, (UInt32) Marshal.SizeOf<sockaddr_ll>()) < 0)
            {
                var errno = Marshal.GetLastPInvokeError();
                close(fd);
                throw new IOException($"bind() to '{InterfaceName}' failed with errno {errno}.");
            }

        }

        #endregion


        #region Send(Frame)

        /// <summary>
        /// Put one whole frame on the wire. Sends are serialised.
        /// </summary>
        public void Send(ReadOnlySpan<Byte> Frame)
        {

            if (Frame.Length < EthernetFrame.HeaderLength)
                throw new ArgumentException("Too short for an Ethernet header.", nameof(Frame));

            if (IsClosed)
                throw new ObjectDisposedException(nameof(AfPacketT1SSocket));

            lock (sendLock)
            {
                unsafe
                {
                    fixed (Byte* p = Frame)
                    {

                        if (send(fd, p, (nuint) Frame.Length, 0) < 0)
                            throw new IOException($"send() on '{InterfaceName}' failed with errno {Marshal.GetLastPInvokeError()}.");

                    }
                }
            }

        }

        #endregion

        #region Receive(Buffer)

        /// <summary>
        /// Block until a frame arrives. The number of bytes copied into the
        /// buffer, or -1 when the socket was shut down while waiting.
        /// </summary>
        public Int32 Receive(Span<Byte> Buffer)
        {

            while (true)
            {

                Int32 n;

                unsafe
                {
                    fixed (Byte* p = Buffer)
                    {
                        n = (Int32) recv(fd, p, (nuint) Buffer.Length, 0);
                    }
                }

                if (n >= 0)
                    return n;

                var errno = Marshal.GetLastPInvokeError();

                if (errno == EINTR)
                    continue;

                if (IsClosed || errno == EBADF)
                    return -1;

                throw new IOException($"recv() on '{InterfaceName}' failed with errno {errno}.");

            }

        }

        #endregion

        #region Dispose()

        public void Dispose()
        {

            if (Interlocked.Exchange(ref disposed, 1) != 0)
                return;

            // shutdown() wakes a blocked recv(), which is how the receive
            // thread finds out.
            try { shutdown(fd, SHUT_RDWR); } catch { }
            try { close(fd);               } catch { }

        }

        #endregion


        #region (private static) ReadInterfaceMac(InterfaceName)

        private static MACAddress ReadInterfaceMac(String InterfaceName)
        {

            var path = $"/sys/class/net/{InterfaceName}/address";

            if (!File.Exists(path))
                throw new IOException($"There is no network interface '{InterfaceName}': {path} does not exist.");

            var text = File.ReadAllText(path).Trim();

            if (String.IsNullOrEmpty(text) || text == "00:00:00:00:00:00")
                throw new IOException($"'{InterfaceName}' has no hardware address; give one to send with.");

            return MACAddress.Parse(text);

        }

        #endregion

    }

}
