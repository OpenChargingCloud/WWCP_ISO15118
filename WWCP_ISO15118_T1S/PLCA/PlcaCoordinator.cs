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

using System.Diagnostics;
using System.Collections.Concurrent;

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

using cloud.charging.open.protocols.ISO15118.T1S.Messages;
using cloud.charging.open.protocols.ISO15118.T1S.Transport;

#endregion

namespace cloud.charging.open.protocols.ISO15118.T1S.PLCA
{

    #region PlcaCoordinatorOptions

    /// <summary>
    /// How the coordinator runs its bus.
    /// </summary>
    /// <param name="Name">What the coordinator calls itself in a log.</param>
    /// <param name="TransmitOpportunityTimeout">How long a node has to use its opportunity.</param>
    /// <param name="DiscoveryWindow">How long the discovery opportunity stays open.</param>
    /// <param name="CycleGap">The pause between one cycle's end and the next BEACON.</param>
    /// <param name="LostAfterMissedCycles">How many silent cycles in a row give a node up for lost.</param>
    /// <param name="MaxNodes">How many nodes may hold an identifier at once, the coordinator not counted. Eight is what the PHY is specified for.</param>
    /// <param name="WeightPolicy">How many opportunities per cycle a node of a role gets, given what it asked for; the default puts a vehicle ahead of everything else.</param>
    public sealed record PlcaCoordinatorOptions(String                         Name                         = "EVSE",
                                                TimeSpan?                      TransmitOpportunityTimeout   = null,
                                                TimeSpan?                      DiscoveryWindow              = null,
                                                TimeSpan?                      CycleGap                     = null,
                                                Int32                          LostAfterMissedCycles        = T1SConstants.DefaultLostAfterMissedCycles,
                                                Byte                           MaxNodes                     = 8,
                                                Func<T1SNodeRole, Byte, Byte>? WeightPolicy                 = null)
    {

        /// <summary>
        /// The vehicle first. It asks for what it wants, up to the ceiling;
        /// everything else gets one opportunity per cycle whatever it asked
        /// for - a sensor has one number to say, and a sensor that could ask
        /// for the whole bus would be a sensor somebody would sooner or later
        /// misconfigure into asking for it.
        /// </summary>
        public static Byte DefaultWeightPolicy(T1SNodeRole Role, Byte Requested)

            => Role switch {
                   T1SNodeRole.Vehicle  => (Byte) Math.Clamp(Requested == 0 ? 3 : Requested, 1, T1SConstants.MaxWeight),
                   _                    => T1SConstants.DefaultWeight
               };

    }

    #endregion

    #region PlcaCycleReport

    /// <summary>
    /// What one cycle came to.
    /// </summary>
    /// <param name="Cycle">Which one.</param>
    /// <param name="Slots">How many opportunities it had, discovery included.</param>
    /// <param name="Answered">How many were used.</param>
    /// <param name="Yielded">How many were passed out loud.</param>
    /// <param name="Missed">How many passed in silence.</param>
    /// <param name="Joined">Whether a node got an identifier in the discovery opportunity.</param>
    /// <param name="Duration">How long the cycle took, gap not included.</param>
    public sealed record PlcaCycleReport(UInt32    Cycle,
                                         Int32     Slots,
                                         Int32     Answered,
                                         Int32     Yielded,
                                         Int32     Missed,
                                         Boolean   Joined,
                                         TimeSpan  Duration);

    #endregion


    /// <summary>
    /// Node 0. Sends the BEACON, hands out every transmit opportunity, keeps
    /// the register of who is on the bus, and gives up on nodes that stop
    /// answering.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the charging station's half of the bus, and it is the half that
    /// polls: every cycle, every node with an identifier is given its turn,
    /// and what it answers - a reading, a frame, a yield, or nothing - is
    /// what the coordinator knows about it. There is no other way to learn
    /// anything on this bus, which is the property that makes it safe: a node
    /// cannot talk out of turn, so a node that does is a node with a fault,
    /// and is reported as one rather than listened to.
    /// </para>
    /// <para>
    /// The cycle runs on one task, and everything the coordinator knows is
    /// written from that task or from the transport's receive thread with a
    /// dictionary between them. A transmit opportunity is a small waiter: the
    /// receive thread completes it when the node whose turn it is answers, and
    /// the cycle task either gets that answer or times out. The waiter is set
    /// <em>before</em> the opportunity goes out, because a node on the same
    /// machine answers faster than a task switches.
    /// </para>
    /// </remarks>
    public sealed class PlcaCoordinator : IAsyncDisposable
    {

        #region (class) Waiter

        /// <summary>
        /// One open transmit opportunity: whose it is, and where its answer
        /// goes.
        /// </summary>
        private sealed class Waiter(MACAddress? Mac, Boolean Discovery)
        {

            public MACAddress?                              Mac        { get; } = Mac;
            public Boolean                                  Discovery  { get; } = Discovery;
            public TaskCompletionSource<DecodedT1SFrame>    Answer     { get; } = new (TaskCreationOptions.RunContinuationsAsynchronously);

        }

        #endregion

        #region Data

        private readonly IT1STransport                              transport;
        private readonly PlcaCoordinatorOptions                     options;
        private readonly TimeProvider                               clock;
        private readonly ConcurrentDictionary<Byte,       PlcaNode>  nodes   = new ();
        private readonly ConcurrentDictionary<MACAddress, PlcaNode>  byMac   = new ();
        private readonly CancellationTokenSource                    stopping = new ();

        private          Waiter?                                    current;
        private          Task?                                      loop;
        private          UInt32                                     cycle;
        private          Int64                                      outOfTurn;
        private          Int64                                      collisions;

        #endregion

        #region Properties

        /// <summary>The medium this coordinator runs.</summary>
        public IT1STransport            Transport   => transport;

        /// <summary>How it runs it.</summary>
        public PlcaCoordinatorOptions   Options     => options;

        /// <summary>The coordinator's own address: node 0.</summary>
        public MACAddress               Mac         => transport.LocalMac;

        /// <summary>The cycle running now, or the last one run. Zero before the first.</summary>
        public UInt32                   Cycle       => Volatile.Read(ref cycle);

        /// <summary>Whether the cycle is running.</summary>
        public Boolean                  Running     => loop is not null && !loop.IsCompleted;

        /// <summary>Every node with an identifier, by identifier.</summary>
        public IReadOnlyList<PlcaNode>  Nodes       => [.. nodes.Values.OrderBy(node => node.NodeId)];

        /// <summary>Frames that arrived in an opportunity that was not their sender's.</summary>
        public Int64                    OutOfTurn   => Interlocked.Read(ref outOfTurn);

        /// <summary>
        /// Times two nodes asked to join in the same discovery opportunity -
        /// the one collision this bus can still have, and a number that says
        /// how crowded its arrivals are.
        /// </summary>
        public Int64                    Collisions  => Interlocked.Read(ref collisions);

        #endregion

        #region Events

        /// <summary>A node got an identifier.</summary>
        public event EventHandler<PlcaNode>?                                  NodeJoined;

        /// <summary>A node said it was leaving.</summary>
        public event EventHandler<PlcaNode>?                                  NodeLeft;

        /// <summary>A node stopped answering, and was given up.</summary>
        public event EventHandler<PlcaNode>?                                  NodeLost;

        /// <summary>A node used its opportunity for something other than a yield.</summary>
        public event EventHandler<(PlcaNode Node, DecodedT1SFrame Frame)>?    FrameReceived;

        /// <summary>A sensor sent a reading.</summary>
        public event EventHandler<(PlcaNode Node, SensorReading Reading)>?    ReadingReceived;

        /// <summary>A frame arrived in somebody else's opportunity - a node with a fault, or an intruder.</summary>
        public event EventHandler<DecodedT1SFrame>?                           OutOfTurnFrame;

        /// <summary>A cycle ended.</summary>
        public event EventHandler<PlcaCycleReport>?                           CycleCompleted;

        /// <summary>Things worth a line in a log.</summary>
        public event EventHandler<String>?                                    Log;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// A coordinator on a medium. Nothing happens until it is started.
        /// </summary>
        public PlcaCoordinator(IT1STransport            Transport,
                               PlcaCoordinatorOptions?  Options   = null,
                               TimeProvider?            Clock     = null)
        {
            this.transport  = Transport;
            this.options    = Options ?? new PlcaCoordinatorOptions();
            this.clock      = Clock   ?? TimeProvider.System;
        }

        #endregion


        #region StartAsync(CancellationToken = default)

        /// <summary>
        /// Start the cycle.
        /// </summary>
        public Task StartAsync(CancellationToken CancellationToken = default)
        {

            if (loop is not null)
                return Task.CompletedTask;

            transport.FrameReceived += OnFrame;

            loop = Task.Run(() => RunAsync(stopping.Token), CancellationToken);

            Log?.Invoke(this, $"{options.Name} is coordinating {transport.Description} as {Mac}: " +
                              $"up to {options.MaxNodes} node(s), an opportunity every {options.TransmitOpportunityTimeout ?? T1SConstants.DefaultTransmitOpportunityTimeout}, " +
                              $"a cycle every {options.CycleGap ?? T1SConstants.DefaultCycleGap} when the bus is quiet.");

            return Task.CompletedTask;

        }

        #endregion

        #region StopAsync()

        /// <summary>
        /// Stop the cycle. The nodes find out the way they would on a real bus:
        /// no BEACON comes.
        /// </summary>
        public async Task StopAsync()
        {

            if (loop is null)
                return;

            stopping.Cancel();

            transport.FrameReceived -= OnFrame;

            try { await loop.ConfigureAwait(false); }
            catch (OperationCanceledException) { }

            loop = null;

        }

        #endregion

        #region Remove(Node)

        /// <summary>
        /// Take a node off the bus from this side - for a node the operator
        /// knows is gone, or one that should not be here.
        /// </summary>
        public Boolean Remove(PlcaNode Node)
        {

            if (!nodes.TryRemove(Node.NodeId, out var removed))
                return false;

            byMac.TryRemove(removed.Mac, out _);

            Log?.Invoke(this, $"{removed} was removed from the bus.");

            return true;

        }

        #endregion


        #region (private) RunAsync(CancellationToken)

        private async Task RunAsync(CancellationToken CancellationToken)
        {

            var timeout    = options.TransmitOpportunityTimeout ?? T1SConstants.DefaultTransmitOpportunityTimeout;
            var window     = options.DiscoveryWindow            ?? T1SConstants.DefaultDiscoveryWindow;
            var gap        = options.CycleGap                   ?? T1SConstants.DefaultCycleGap;

            while (!CancellationToken.IsCancellationRequested)
            {

                var thisCycle  = ++cycle;
                var watch      = Stopwatch.StartNew();
                var schedule   = PlcaSchedule.Build(nodes.Values.Select(node => (node.NodeId, node.Weight)));
                var answered   = new HashSet<Byte>();
                var used       = 0;
                var yielded    = 0;
                var missed     = 0;
                var joined     = false;

                try
                {

                    await transport.SendAsync(MACAddress.Broadcast, new Beacon(thisCycle, (Byte) Math.Min(schedule.Count, Byte.MaxValue)), CancellationToken).ConfigureAwait(false);

                    for (var slot = 0; slot < schedule.Count; slot++)
                    {

                        var nodeId = schedule[slot];

                        #region The discovery opportunity

                        if (nodeId == T1SConstants.UnassignedNodeId)
                        {
                            joined |= await DiscoverAsync(thisCycle, (Byte) slot, window, CancellationToken).ConfigureAwait(false);
                            continue;
                        }

                        #endregion

                        // Left, or was removed, since the schedule was built.
                        if (!nodes.TryGetValue(nodeId, out var node))
                            continue;

                        #region One node's turn

                        var waiter = new Waiter(node.Mac, Discovery: false);

                        Volatile.Write(ref current, waiter);

                        await transport.SendAsync(MACAddress.Broadcast, new TransmitOpportunity(thisCycle, (Byte) slot, nodeId), CancellationToken).ConfigureAwait(false);

                        var frame = await AwaitAsync(waiter, timeout, CancellationToken).ConfigureAwait(false);

                        Volatile.Write(ref current, null);

                        if (frame is null)
                        {
                            missed++;
                            continue;
                        }

                        answered.Add(nodeId);

                        if (Answered(node, frame))
                            used++;
                        else
                            yielded++;

                        #endregion

                    }

                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    // One cycle's mishap - a socket hiccup, a handler that
                    // threw - is one line in the log, not the end of the bus.
                    Log?.Invoke(this, $"Cycle {thisCycle} did not complete: {e.Message}");
                }

                Volatile.Write(ref current, null);

                #region Who went quiet

                foreach (var node in nodes.Values)
                {

                    if (answered.Contains(node.NodeId))
                    {
                        node.MissedCycles = 0;
                        continue;
                    }

                    node.MissedCycles++;

                    if (node.MissedCycles >= options.LostAfterMissedCycles)
                    {

                        if (nodes.TryRemove(node.NodeId, out _))
                        {

                            byMac.TryRemove(node.Mac, out _);

                            Log?.Invoke(this, $"{node} has not answered for {node.MissedCycles} cycle(s) and is given up for lost.");

                            NodeLost?.Invoke(this, node);

                        }

                    }

                }

                #endregion

                watch.Stop();

                CycleCompleted?.Invoke(this, new PlcaCycleReport(thisCycle, schedule.Count, used, yielded, missed, joined, watch.Elapsed));

                try
                {
                    await Task.Delay(gap, CancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

            }

        }

        #endregion

        #region (private) DiscoverAsync(Cycle, Slot, Window, CancellationToken)

        /// <summary>
        /// The opportunity for nodes without an identifier. Answers whether
        /// one got one.
        /// </summary>
        /// <remarks>
        /// One node per cycle. Two asking in the same window is the collision
        /// this bus can still have: the first is answered, the second is not,
        /// and backs off a random number of cycles before asking again - the
        /// same arithmetic every shared medium since ALOHA has settled it with.
        /// </remarks>
        private async Task<Boolean> DiscoverAsync(UInt32             Cycle,
                                                  Byte               Slot,
                                                  TimeSpan           Window,
                                                  CancellationToken  CancellationToken)
        {

            var waiter = new Waiter(null, Discovery: true);
            var opened = Stopwatch.StartNew();

            Volatile.Write(ref current, waiter);

            await transport.SendAsync(MACAddress.Broadcast, new TransmitOpportunity(Cycle, Slot, T1SConstants.UnassignedNodeId), CancellationToken).ConfigureAwait(false);

            var frame = await AwaitAsync(waiter, Window, CancellationToken).ConfigureAwait(false);

            // The opportunity lasts its full time whether or not somebody
            // used it, exactly as the PHY's does. Closing it at the first JOIN
            // would leave every other node that heard the same opportunity -
            // and answered it microseconds later - arriving after the window
            // and being reported as talking out of turn, when what they did
            // was collide.
            if (frame is not null)
            {

                var remaining = Window - opened.Elapsed;

                if (remaining > TimeSpan.Zero)
                    await Task.Delay(remaining, CancellationToken).ConfigureAwait(false);

            }

            Volatile.Write(ref current, null);

            if (frame?.Message is not Join join)
                return false;

            var now = clock.GetUtcNow();

            #region A node this coordinator already knows: the same identifier again

            // A node that restarted, or missed its ASSIGN. Its identifier is
            // still its own - handing out a second one would leave the first
            // in the schedule, asked every cycle, answered by nobody.
            if (byMac.TryGetValue(frame.Source, out var known))
            {

                known.LastSeen = now;

                await transport.SendAsync(frame.Source, new Assign(join.Nonce, known.NodeId, known.Role, known.Weight), CancellationToken).ConfigureAwait(false);

                Log?.Invoke(this, $"{known} asked to join again and was told its identifier again.");

                return true;

            }

            #endregion

            #region A new one

            var nodeId = NextFreeNodeId();

            if (nodeId is null)
            {
                Log?.Invoke(this, $"'{join.Name}' ({join.Role}, {frame.Source}) asked to join, and the bus is full: {nodes.Count} of {options.MaxNodes}.");
                return false;
            }

            var weight  = (options.WeightPolicy ?? PlcaCoordinatorOptions.DefaultWeightPolicy)(join.Role, join.Weight);
            var node    = new PlcaNode(nodeId.Value, frame.Source, join.Role, join.Name, weight, now);

            nodes[node.NodeId]  = node;
            byMac[node.Mac]     = node;

            await transport.SendAsync(frame.Source, new Assign(join.Nonce, node.NodeId, node.Role, node.Weight), CancellationToken).ConfigureAwait(false);

            Log?.Invoke(this, $"{node} joined the bus" +
                              (weight != join.Weight ? $" (asked for weight {join.Weight}, given {weight})" : "") + ".");

            NodeJoined?.Invoke(this, node);

            return true;

            #endregion

        }

        #endregion

        #region (private) Answered(Node, Frame)

        /// <summary>
        /// What a node did with its opportunity. Answers whether it was used
        /// for anything but a yield.
        /// </summary>
        private Boolean Answered(PlcaNode         Node,
                                 DecodedT1SFrame  Frame)
        {

            Node.LastSeen = Frame.ReceivedAt;

            switch (Frame.Message)
            {

                case Yield:
                    Node.Yields++;
                    return false;

                case Announce announce:
                    Node.Name = announce.Name;
                    Node.FramesReceived++;
                    FrameReceived?.Invoke(this, (Node, Frame));
                    return true;

                case Leave:
                    Node.FramesReceived++;
                    if (nodes.TryRemove(Node.NodeId, out _))
                    {
                        byMac.TryRemove(Node.Mac, out _);
                        Log?.Invoke(this, $"{Node} left the bus.");
                        NodeLeft?.Invoke(this, Node);
                    }
                    return true;

                case SensorReading reading:
                    Node.FramesReceived++;
                    Node.LastReading    = reading;
                    Node.LastReadingAt  = Frame.ReceivedAt;
                    FrameReceived?.  Invoke(this, (Node, Frame));
                    ReadingReceived?.Invoke(this, (Node, reading));
                    return true;

                default:
                    Node.FramesReceived++;
                    FrameReceived?.Invoke(this, (Node, Frame));
                    return true;

            }

        }

        #endregion

        #region (private) OnFrame(Sender, Frame)

        /// <summary>
        /// Every frame off the medium: the answer to the open opportunity, or
        /// somebody talking out of turn.
        /// </summary>
        private void OnFrame(Object? Sender, DecodedT1SFrame Frame)
        {

            var waiter = Volatile.Read(ref current);

            if (waiter is not null)
            {

                if (waiter.Discovery && Frame.Message is Join)
                {

                    if (waiter.Answer.TrySetResult(Frame))
                        return;

                    // A second node asking in the same discovery opportunity.
                    // On a real bus their frames would have collided on the
                    // wire; here the first is answered and the other backs off
                    // and asks again. Counted as what it is - the discovery
                    // opportunity is the one place several nodes may try at
                    // once - and not as a node breaking the rules.
                    Interlocked.Increment(ref collisions);

                    return;

                }

                if (!waiter.Discovery && Frame.Source == waiter.Mac && waiter.Answer.TrySetResult(Frame))
                    return;

            }

            // A node leaving does not wait for its turn: it may be shutting
            // down right now, and the point of saying so is not to be waited
            // for.
            if (Frame.Message is Leave && byMac.TryGetValue(Frame.Source, out var leaving))
            {

                if (nodes.TryRemove(leaving.NodeId, out _))
                {
                    byMac.TryRemove(leaving.Mac, out _);
                    Log?.Invoke(this, $"{leaving} left the bus.");
                    NodeLeft?.Invoke(this, leaving);
                }

                return;

            }

            // On a real bus this is a collision. Here it is a node that
            // answered late, or one that does not know the rules - either way
            // something the coordinator should say rather than absorb.
            Interlocked.Increment(ref outOfTurn);

            OutOfTurnFrame?.Invoke(this, Frame);

        }

        #endregion

        #region (private) AwaitAsync(Waiter, Timeout, CancellationToken) / NextFreeNodeId()

        private static async Task<DecodedT1SFrame?> AwaitAsync(Waiter             Waiter,
                                                                TimeSpan           Timeout,
                                                                CancellationToken  CancellationToken)
        {

            var completed = await Task.WhenAny(Waiter.Answer.Task, Task.Delay(Timeout, CancellationToken)).ConfigureAwait(false);

            CancellationToken.ThrowIfCancellationRequested();

            return completed == Waiter.Answer.Task
                       ? Waiter.Answer.Task.Result
                       : null;

        }

        private Byte? NextFreeNodeId()
        {

            var last = Math.Min(T1SConstants.LastFollowerNodeId, T1SConstants.FirstFollowerNodeId + options.MaxNodes - 1);

            for (var id = T1SConstants.FirstFollowerNodeId; id <= last; id++)
                if (!nodes.ContainsKey((Byte) id))
                    return (Byte) id;

            return null;

        }

        #endregion


        #region DisposeAsync()

        public async ValueTask DisposeAsync()
        {

            await StopAsync().ConfigureAwait(false);

            stopping.Dispose();

        }

        #endregion

    }

}
