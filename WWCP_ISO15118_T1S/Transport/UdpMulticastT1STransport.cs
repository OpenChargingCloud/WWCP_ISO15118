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

using System.Net;
using System.Net.Sockets;

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

using cloud.charging.open.protocols.ISO15118.T1S.Messages;

#endregion

namespace cloud.charging.open.protocols.ISO15118.T1S.Transport
{

    /// <summary>
    /// The emulated medium: one UDP multicast group that every node joins, so
    /// that every frame reaches every node - which is what a bus is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The SLAC library's simulated medium sends to a list of peers, and a
    /// node has to be told where the others are. That is the wrong shape for
    /// a multidrop bus: a temperature sensor screwed into a coupler knows
    /// nothing about who else is on the wire, and should not have to. A
    /// multicast group is a wire. Join it and you hear everything; send to it
    /// and everybody hears you.
    /// </para>
    /// <para>
    /// Filtering is the receiver's, exactly as on Ethernet: every datagram is
    /// a frame, every node sees every frame, and each node keeps the ones
    /// addressed to it or to everybody and drops the rest. Its own frames
    /// come back too - multicast loopback is on, because that is what lets
    /// two nodes in one process hear each other - and are dropped by source
    /// address, since no node needs to hear itself.
    /// </para>
    /// <para>
    /// Every socket binds the same port with address reuse, and every socket
    /// joins the group. That is what makes a whole bus in one process work,
    /// and it is also what makes two benches on one machine collide: two
    /// coordinators on one group are two coordinators on one wire. Give the
    /// second bench its own port.
    /// </para>
    /// </remarks>
    public sealed class UdpMulticastT1STransport : IT1STransport
    {

        #region Data

        private readonly Socket                   socket;
        private readonly IPEndPoint               group;
        private readonly SemaphoreSlim            sendLock  = new (1, 1);
        private readonly CancellationTokenSource  stopping  = new ();
        private readonly TimeProvider             clock;
        private          Task?                    receiveLoop;

        private          Int64                    framesSent;
        private          Int64                    framesReceived;
        private          Int64                    framesDropped;

        #endregion

        #region Properties

        /// <summary>
        /// This node's MAC address on the medium.
        /// </summary>
        public MACAddress  LocalMac     { get; }

        /// <summary>
        /// The group and port this medium is.
        /// </summary>
        public IPEndPoint  Group        => group;

        /// <summary>
        /// The interface the group was joined on, or null for whichever one
        /// the operating system chose.
        /// </summary>
        public IPAddress?  Interface    { get; }

        /// <summary>
        /// What this medium is, for a log.
        /// </summary>
        public String      Description
            => $"UDP multicast {group}" + (Interface is null ? "" : $" via {Interface}");

        /// <summary>Frames this node put on the wire.</summary>
        public Int64       FramesSent      => Interlocked.Read(ref framesSent);

        /// <summary>Frames off the wire that were ours and for this node.</summary>
        public Int64       FramesReceived  => Interlocked.Read(ref framesReceived);

        /// <summary>Datagrams off the wire that were not: another node's, another EtherType's, or not a frame at all.</summary>
        public Int64       FramesDropped   => Interlocked.Read(ref framesDropped);

        #endregion

        #region Events

        /// <summary>
        /// Every frame of ours for this node.
        /// </summary>
        public event EventHandler<DecodedT1SFrame>?  FrameReceived;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Attach a node to the emulated medium.
        /// </summary>
        /// <param name="LocalMac">This node's address on it.</param>
        /// <param name="Group">The group and port that are the medium; the default one when null.</param>
        /// <param name="Interface">The IPv4 address of the interface to join on, or null for the one the operating system picks - which on a laptop is the right answer.</param>
        /// <param name="Clock">Where received frames get their timestamp from.</param>
        public UdpMulticastT1STransport(MACAddress     LocalMac,
                                        IPEndPoint?    Group       = null,
                                        IPAddress?     Interface   = null,
                                        TimeProvider?  Clock       = null)
        {

            this.LocalMac   = LocalMac;
            this.group      = Group ?? T1SConstants.DefaultMulticastEndpoint;
            this.Interface  = Interface;
            this.clock      = Clock ?? TimeProvider.System;

            if (!this.group.Address.IsIPv4Multicast())
                throw new ArgumentException($"'{this.group.Address}' is not an IPv4 multicast address.", nameof(Group));

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // Every node on this machine binds the same port: that is what
            // makes them one bus. Without this the second node cannot bind
            // at all.
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            socket.Bind(new IPEndPoint(IPAddress.Any, this.group.Port));

            socket.SetSocketOption(
                SocketOptionLevel.IP,
                SocketOptionName.AddMembership,
                Interface is null
                    ? new MulticastOption(this.group.Address)
                    : new MulticastOption(this.group.Address, Interface)
            );

            if (Interface is not null)
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, Interface.GetAddressBytes());

            // On, and on is the point: the nodes of a bench are processes -
            // or objects - on one machine, and without loopback none of them
            // would hear any other. A frame's own sender drops it by address.
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback,   true);

            // A bus does not leave the building.
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 1);

        }

        #endregion


        #region StartAsync(CancellationToken = default)

        public Task StartAsync(CancellationToken CancellationToken = default)
        {

            if (receiveLoop is not null)
                return Task.CompletedTask;

            receiveLoop = Task.Run(() => ReceiveLoop(stopping.Token), CancellationToken);

            return Task.CompletedTask;

        }

        #endregion

        #region SendAsync(Destination, Message, CancellationToken = default)

        public Task SendAsync(MACAddress         Destination,
                              IT1SMessage        Message,
                              CancellationToken  CancellationToken = default)

            => SendRawAsync(
                   T1SCodec.Frame(Destination, LocalMac, Message),
                   CancellationToken
               );

        #endregion

        #region SendRawAsync(Frame, CancellationToken = default)

        public async Task SendRawAsync(EthernetFrame      Frame,
                                       CancellationToken  CancellationToken = default)
        {

            var bytes = Frame.Encode();

            // One send at a time: the socket is shared by everything this node
            // says, and a datagram interleaved with another is two frames
            // nobody reads.
            await sendLock.WaitAsync(CancellationToken).ConfigureAwait(false);

            try
            {
                await socket.SendToAsync(bytes, SocketFlags.None, group, CancellationToken).ConfigureAwait(false);
                Interlocked.Increment(ref framesSent);
            }
            finally
            {
                sendLock.Release();
            }

        }

        #endregion


        #region (private) ReceiveLoop(CancellationToken)

        private async Task ReceiveLoop(CancellationToken CancellationToken)
        {

            var buffer  = new Byte[EthernetFrame.HeaderLength + T1SConstants.MaxPayloadLength + 64];
            var anyone  = new IPEndPoint(IPAddress.Any, 0);

            while (!CancellationToken.IsCancellationRequested)
            {

                SocketReceiveFromResult result;

                try
                {
                    result = await socket.ReceiveFromAsync(buffer, SocketFlags.None, anyone, CancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }
                catch (ObjectDisposedException)    { break; }
                catch (SocketException)
                {
                    // A datagram too large for the buffer, a port unreachable
                    // reported back for somebody else's send: neither is a
                    // reason for the bus to stop listening.
                    Interlocked.Increment(ref framesDropped);
                    continue;
                }

                var frame = EthernetFrame.TryDecode(buffer.AsSpan(0, result.ReceivedBytes));

                // Not a frame, our own frame coming back, somebody else's
                // conversation, or another EtherType altogether - dropped
                // exactly the way a network card drops them, silently and
                // counted.
                if (frame is null ||
                    frame.Source == LocalMac ||
                    (frame.Destination != LocalMac && !frame.Destination.IsBroadcast && !frame.Destination.IsMulticast))
                {
                    Interlocked.Increment(ref framesDropped);
                    continue;
                }

                var message = T1SCodec.TryDecode(frame);

                if (message is null)
                {
                    Interlocked.Increment(ref framesDropped);
                    continue;
                }

                Interlocked.Increment(ref framesReceived);

                FrameReceived?.Invoke(
                    this,
                    new DecodedT1SFrame(
                        frame.Destination,
                        frame.Source,
                        message,
                        clock.GetUtcNow()
                    )
                );

            }

        }

        #endregion


        #region DisposeAsync()

        public async ValueTask DisposeAsync()
        {

            stopping.Cancel();

            try
            {
                socket.Close();
            }
            catch { }

            if (receiveLoop is not null)
            {
                try { await receiveLoop.ConfigureAwait(false); }
                catch { }
            }

            socket.  Dispose();
            sendLock.Dispose();
            stopping.Dispose();

        }

        #endregion


        #region (override) ToString()

        public override String ToString()
            => $"{LocalMac} on {Description}";

        #endregion

    }


    /// <summary>
    /// The one question about an address this transport asks.
    /// </summary>
    internal static class IPAddressExtensions
    {

        /// <summary>
        /// Whether this is an IPv4 multicast address: 224.0.0.0/4.
        /// </summary>
        public static Boolean IsIPv4Multicast(this IPAddress Address)
        {

            if (Address.AddressFamily != AddressFamily.InterNetwork)
                return false;

            var first = Address.GetAddressBytes()[0];

            return first >= 224 && first <= 239;

        }

    }

}
