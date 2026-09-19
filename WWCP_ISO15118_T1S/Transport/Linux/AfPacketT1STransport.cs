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

using System.Runtime.Versioning;

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

using cloud.charging.open.protocols.ISO15118.T1S.Messages;

#endregion

namespace cloud.charging.open.protocols.ISO15118.T1S.Transport.Linux
{

    /// <summary>
    /// The real medium: a 10BASE-T1S adapter on Linux, spoken to through a
    /// raw socket bound to our EtherType.
    /// </summary>
    /// <remarks>
    /// <para>
    /// On a real segment the PHY does the PLCA: BEACON and transmit
    /// opportunities are signalling below the MAC, and the adapter has a node
    /// identifier it was configured with. What this library sends over such
    /// a segment is therefore an application protocol on top of a medium that
    /// already keeps the nodes off each other - its own BEACON and opportunity
    /// frames are redundant there and harmless, and the polling of the
    /// sensors is exactly what a station still needs. Nothing above this
    /// class knows the difference.
    /// </para>
    /// <para>
    /// Frames this node sent do not normally come back off a real adapter,
    /// but do off a bridge or a veth pair, so they are dropped by source
    /// address as on the emulated medium. Everything else the kernel already
    /// filtered to our EtherType; the destination is checked again so that
    /// an interface in promiscuous mode does not feed a neighbour's unicast
    /// into the state machines.
    /// </para>
    /// </remarks>
    [SupportedOSPlatform("linux")]
    public sealed class AfPacketT1STransport : IT1STransport
    {

        #region Data

        private readonly AfPacketT1SSocket  socket;
        private readonly Thread             receiveThread;
        private readonly TimeProvider       clock;
        private          Boolean            started;

        private          Int64              framesSent;
        private          Int64              framesReceived;
        private          Int64              framesDropped;

        #endregion

        #region Properties

        /// <summary>
        /// This node's address on the medium: the adapter's own, unless the
        /// constructor was given another.
        /// </summary>
        public MACAddress  LocalMac
            => socket.LocalMac;

        /// <summary>
        /// The interface the adapter is.
        /// </summary>
        public String      InterfaceName
            => socket.InterfaceName;

        /// <summary>
        /// What this medium is, for a log.
        /// </summary>
        public String      Description
            => $"AF_PACKET on {InterfaceName} (EtherType 0x{T1SConstants.EtherType:X4})";

        /// <summary>Frames this node put on the wire.</summary>
        public Int64       FramesSent      => Interlocked.Read(ref framesSent);

        /// <summary>Frames off the wire that were ours and for this node.</summary>
        public Int64       FramesReceived  => Interlocked.Read(ref framesReceived);

        /// <summary>Frames off the wire that were not: this node's own coming back, another node's unicast, or not a message.</summary>
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
        /// Open the adapter. Needs CAP_NET_RAW, and throws an
        /// <see cref="IOException"/> saying so when it has not got it.
        /// </summary>
        /// <param name="InterfaceName">The interface the adapter is, e.g. "eth1".</param>
        /// <param name="LocalMac">An address to send with instead of the adapter's own - for a test, not for a vehicle.</param>
        /// <param name="Clock">Where received frames get their timestamp from.</param>
        public AfPacketT1STransport(String         InterfaceName,
                                    MACAddress?    LocalMac   = null,
                                    TimeProvider?  Clock      = null)
        {

            socket         = new AfPacketT1SSocket(InterfaceName, LocalMac);
            clock          = Clock ?? TimeProvider.System;

            receiveThread  = new Thread(ReceiveLoop) {
                                 IsBackground  = true,
                                 Name          = $"10BASE-T1S receive ({InterfaceName})"
                             };

        }

        #endregion


        #region StartAsync(CancellationToken = default)

        public Task StartAsync(CancellationToken CancellationToken = default)
        {

            if (!started)
            {
                started = true;
                receiveThread.Start();
            }

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

        public Task SendRawAsync(EthernetFrame      Frame,
                                 CancellationToken  CancellationToken = default)
        {

            CancellationToken.ThrowIfCancellationRequested();

            socket.Send(Frame.Encode());
            Interlocked.Increment(ref framesSent);

            return Task.CompletedTask;

        }

        #endregion


        #region (private) ReceiveLoop()

        private void ReceiveLoop()
        {

            var buffer = new Byte[EthernetFrame.HeaderLength + T1SConstants.MaxPayloadLength + 64];

            while (true)
            {

                Int32 n;

                try
                {
                    n = socket.Receive(buffer);
                }
                catch (Exception)
                {
                    // Disposed, or an error the socket cannot recover from:
                    // the medium is gone either way.
                    return;
                }

                if (n < 0)
                    return;

                var frame = EthernetFrame.TryDecode(buffer.AsSpan(0, n));

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

                try
                {
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
                catch
                {
                    // A handler that throws is that handler's problem, not
                    // the wire's.
                }

            }

        }

        #endregion


        #region DisposeAsync()

        public ValueTask DisposeAsync()
        {

            socket.Dispose();

            if (started)
                receiveThread.Join(TimeSpan.FromSeconds(1));

            return ValueTask.CompletedTask;

        }

        #endregion


        #region (override) ToString()

        public override String ToString()
            => $"{LocalMac} on {Description}";

        #endregion

    }

}
