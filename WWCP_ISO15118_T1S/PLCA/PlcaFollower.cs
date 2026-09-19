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

using System.Collections.Concurrent;
using System.Security.Cryptography;

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

using cloud.charging.open.protocols.ISO15118.T1S.Messages;
using cloud.charging.open.protocols.ISO15118.T1S.Transport;

#endregion

namespace cloud.charging.open.protocols.ISO15118.T1S.PLCA
{

    #region (enum) PlcaFollowerState

    /// <summary>
    /// Where a follower stands with the coordinator.
    /// </summary>
    public enum PlcaFollowerState
    {

        /// <summary>No identifier: listening for a BEACON, and for a discovery opportunity to ask in.</summary>
        Detached,

        /// <summary>Asked for an identifier, and waiting to be told one.</summary>
        Joining,

        /// <summary>Has an identifier, and is asked every cycle.</summary>
        Attached

    }

    #endregion

    #region PlcaFollowerOptions

    /// <summary>
    /// What a follower is and what it asks for.
    /// </summary>
    /// <param name="Role">What this node is.</param>
    /// <param name="Name">What to call it, up to <see cref="T1SConstants.MaxNameLength"/> bytes of UTF-8.</param>
    /// <param name="RequestedWeight">How many opportunities per cycle it asks for. The coordinator decides.</param>
    /// <param name="BeaconTimeout">How long without a BEACON before the coordinator is taken to be gone.</param>
    /// <param name="JoinTimeoutCycles">How many cycles to wait for an answer to a JOIN before backing off and asking again.</param>
    public sealed record PlcaFollowerOptions(T1SNodeRole  Role,
                                             String       Name,
                                             Byte         RequestedWeight     = T1SConstants.DefaultWeight,
                                             TimeSpan?    BeaconTimeout       = null,
                                             Int32        JoinTimeoutCycles   = 2);

    #endregion


    /// <summary>
    /// Any node that is not the coordinator: it waits for a BEACON, asks for
    /// an identifier, and from then on answers every opportunity it is given.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A follower never speaks unasked. It sends in exactly two moments: the
    /// discovery opportunity, while it has no identifier, and its own
    /// opportunity, once it has one. What it sends in its own is, in order of
    /// preference, whatever was queued for it, whatever its
    /// <see cref="Supplier"/> produces on the spot, an announcement of who it
    /// is the first time round, and a yield. A sensor is a follower with a
    /// supplier; a vehicle is a follower with a queue.
    /// </para>
    /// <para>
    /// It also never trusts the coordinator to be there. A BEACON that stops
    /// coming is noticed by a watchdog, and the follower goes back to
    /// listening with no identifier - which is what Clause 148 has a node do,
    /// and what makes a coordinator restart something the bus recovers from
    /// rather than something it has to be told about.
    /// </para>
    /// </remarks>
    public sealed class PlcaFollower : IAsyncDisposable
    {

        #region Data

        private readonly IT1STransport                     transport;
        private readonly PlcaFollowerOptions               options;
        private readonly TimeProvider                      clock;
        private readonly ConcurrentQueue<IT1SMessage>      outgoing  = new ();
        private readonly CancellationTokenSource           stopping  = new ();
        private readonly Lock                              padlock   = new ();

        private          PlcaFollowerState                 state;
        private          Byte?                             nodeId;
        private          Byte                              weight;
        private          MACAddress?                       coordinator;
        private          DateTimeOffset?                   lastBeaconAt;
        private          UInt32                            cycle;
        private          UInt32                            joinNonce;
        private          UInt32                            joinCycle;
        private          Int32                             backoffCycles;
        private          Boolean                           announced;
        private          Boolean                           started;
        private          Task?                             watchdog;
        private          TaskCompletionSource              attached  = new (TaskCreationOptions.RunContinuationsAsynchronously);

        #endregion

        #region Properties

        /// <summary>The medium this follower is on.</summary>
        public IT1STransport        Transport      => transport;

        /// <summary>What it is and what it asked for.</summary>
        public PlcaFollowerOptions  Options        => options;

        /// <summary>Its address on the medium.</summary>
        public MACAddress           Mac            => transport.LocalMac;

        /// <summary>Where it stands with the coordinator.</summary>
        public PlcaFollowerState    State          { get { lock (padlock) return state; } }

        /// <summary>The identifier it was given, or null while it has none.</summary>
        public Byte?                NodeId         { get { lock (padlock) return nodeId; } }

        /// <summary>How many opportunities per cycle the coordinator gave it.</summary>
        public Byte                 Weight         { get { lock (padlock) return weight; } }

        /// <summary>The coordinator's address, once a BEACON has been heard.</summary>
        public MACAddress?          Coordinator    { get { lock (padlock) return coordinator; } }

        /// <summary>When the last BEACON was heard.</summary>
        public DateTimeOffset?      LastBeaconAt   { get { lock (padlock) return lastBeaconAt; } }

        /// <summary>The cycle the coordinator is in, as far as this node has heard.</summary>
        public UInt32               Cycle          { get { lock (padlock) return cycle; } }

        /// <summary>
        /// What to send when the queue is empty: asked in this node's own
        /// opportunity, and its answer goes out at once. A sensor's reading.
        /// Null, or a null answer, means there is nothing.
        /// </summary>
        public Func<IT1SMessage?>?  Supplier       { get; set; }

        #endregion

        #region Events

        /// <summary>Told an identifier.</summary>
        public event EventHandler<Assign>?                Attached;

        /// <summary>Lost it - the coordinator went away, or this node left.</summary>
        public event EventHandler<String>?                Detached;

        /// <summary>Given an opportunity, and about to use it.</summary>
        public event EventHandler<TransmitOpportunity>?   OpportunityGranted;

        /// <summary>Sent something in an opportunity - a yield included.</summary>
        public event EventHandler<IT1SMessage>?           Sent;

        /// <summary>Things worth a line in a log.</summary>
        public event EventHandler<String>?                Log;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// A follower on a medium. Nothing happens until it is started.
        /// </summary>
        public PlcaFollower(IT1STransport        Transport,
                            PlcaFollowerOptions  Options,
                            TimeProvider?        Clock   = null)
        {
            this.transport  = Transport;
            this.options    = Options;
            this.clock      = Clock ?? TimeProvider.System;
        }

        #endregion


        #region StartAsync(CancellationToken = default)

        /// <summary>
        /// Start listening for the coordinator.
        /// </summary>
        public Task StartAsync(CancellationToken CancellationToken = default)
        {

            if (started)
                return Task.CompletedTask;

            started = true;

            transport.FrameReceived += OnFrame;

            watchdog = Task.Run(() => WatchBeaconAsync(stopping.Token), CancellationToken);

            Log?.Invoke(this, $"'{options.Name}' ({options.Role}, {Mac}) is listening on {transport.Description} for a coordinator.");

            return Task.CompletedTask;

        }

        #endregion

        #region WaitUntilAttachedAsync(Timeout, CancellationToken = default)

        /// <summary>
        /// Wait to be given an identifier. Answers false when the time ran out
        /// first.
        /// </summary>
        public async Task<Boolean> WaitUntilAttachedAsync(TimeSpan           Timeout,
                                                          CancellationToken  CancellationToken = default)
        {

            Task waiting;

            lock (padlock)
            {

                if (state == PlcaFollowerState.Attached)
                    return true;

                waiting = attached.Task;

            }

            var completed = await Task.WhenAny(waiting, Task.Delay(Timeout, CancellationToken)).ConfigureAwait(false);

            CancellationToken.ThrowIfCancellationRequested();

            return completed == waiting;

        }

        #endregion

        #region Enqueue(Message)

        /// <summary>
        /// Something to send in this node's next opportunity. Queued, not sent:
        /// a follower speaks only when asked.
        /// </summary>
        public void Enqueue(IT1SMessage Message)
            => outgoing.Enqueue(Message);

        #endregion

        #region LeaveAsync(CancellationToken = default)

        /// <summary>
        /// Say goodbye, and go back to having no identifier.
        /// </summary>
        /// <remarks>
        /// Sent at once rather than in the next opportunity, because a node
        /// that is leaving may be about to be disposed of, and the coordinator
        /// takes a LEAVE out of turn for exactly this reason.
        /// </remarks>
        public async Task LeaveAsync(CancellationToken CancellationToken = default)
        {

            Byte        id;
            MACAddress  to;

            lock (padlock)
            {

                if (state != PlcaFollowerState.Attached || nodeId is null || coordinator is null)
                    return;

                id  = nodeId.Value;
                to  = coordinator.Value;

                Reset("left the bus");

            }

            await transport.SendAsync(to, new Leave(id), CancellationToken).ConfigureAwait(false);

        }

        #endregion


        #region (private) OnFrame(Sender, Frame)

        private void OnFrame(Object? Sender, DecodedT1SFrame Frame)
        {

            switch (Frame.Message)
            {

                case Beacon beacon:
                    OnBeacon(Frame.Source, beacon, Frame.ReceivedAt);
                    break;

                case TransmitOpportunity opportunity:
                    OnOpportunity(opportunity);
                    break;

                case Assign assign:
                    OnAssign(Frame, assign);
                    break;

                // Everything else on the bus is between the coordinator and
                // somebody else, or from somebody who should not be talking.
                default:
                    break;

            }

        }

        #endregion

        #region (private) OnBeacon(Source, Beacon, At)

        private void OnBeacon(MACAddress Source, Beacon Beacon, DateTimeOffset At)
        {

            lock (padlock)
            {

                if (coordinator is null || coordinator.Value != Source)
                {

                    // A different coordinator is a different bus, and an
                    // identifier from the old one means nothing on it.
                    if (coordinator is not null && state == PlcaFollowerState.Attached)
                        Reset($"the BEACON now comes from {Source} instead of {coordinator}");

                    coordinator = Source;

                    Log?.Invoke(this, $"'{options.Name}' hears a coordinator at {Source}.");

                }

                lastBeaconAt  = At;
                cycle         = Beacon.Cycle;

                // Asked, and not answered within the time: somebody else was
                // asking in the same opportunity. Back off a random number of
                // cycles and ask again.
                if (state == PlcaFollowerState.Joining && Beacon.Cycle - joinCycle > (UInt32) options.JoinTimeoutCycles)
                {

                    backoffCycles  = RandomNumberGenerator.GetInt32(1, T1SConstants.MaxJoinBackoffCycles + 1);
                    state          = PlcaFollowerState.Detached;

                    Log?.Invoke(this, $"'{options.Name}' was not answered and backs off for {backoffCycles} cycle(s).");

                }

                else if (state == PlcaFollowerState.Detached && backoffCycles > 0)
                    backoffCycles--;

            }

        }

        #endregion

        #region (private) OnOpportunity(Opportunity)

        private void OnOpportunity(TransmitOpportunity Opportunity)
        {

            MACAddress   to;
            IT1SMessage  message;

            lock (padlock)
            {

                if (coordinator is null)
                    return;

                to = coordinator.Value;

                #region The discovery opportunity: ask, if this node has no identifier

                if (Opportunity.IsDiscovery)
                {

                    if (state != PlcaFollowerState.Detached || backoffCycles > 0)
                        return;

                    joinNonce  = (UInt32) RandomNumberGenerator.GetInt32(Int32.MaxValue);
                    joinCycle  = Opportunity.Cycle;
                    state      = PlcaFollowerState.Joining;

                    message    = new Join(options.Role, joinNonce, options.RequestedWeight, options.Name);

                }

                #endregion

                #region This node's own: use it

                else
                {

                    if (state != PlcaFollowerState.Attached || Opportunity.NodeId != nodeId)
                        return;

                    OpportunityGranted?.Invoke(this, Opportunity);

                    // Who this is, before anything it has to say: the first
                    // opportunity after joining is the introduction, and a
                    // frame queued before it does not jump the queue - the
                    // coordinator should have a name beside the identifier
                    // before it has to make sense of the identifier's data.
                    if (!announced)
                    {
                        message    = new Announce(nodeId.Value, options.Role, options.Name);
                        announced  = true;
                    }

                    else if (outgoing.TryDequeue(out var queued))
                        message = queued;

                    else if (Supplier?.Invoke() is { } supplied)
                        message = supplied;

                    else
                        message = new Yield(Opportunity.Cycle, nodeId.Value);

                }

                #endregion

            }

            _ = SendAsync(to, message);

        }

        #endregion

        #region (private) OnAssign(Frame, Assign)

        private void OnAssign(DecodedT1SFrame Frame, Assign Assign)
        {

            lock (padlock)
            {

                if (state != PlcaFollowerState.Joining || Assign.Nonce != joinNonce || Frame.Destination != Mac)
                    return;

                nodeId     = Assign.NodeId;
                weight     = Assign.Weight;
                state      = PlcaFollowerState.Attached;
                announced  = false;

                attached.TrySetResult();

                Log?.Invoke(this, $"'{options.Name}' is node {Assign.NodeId} on the bus, with {Assign.Weight} opportunit{(Assign.Weight == 1 ? "y" : "ies")} per cycle.");

            }

            Attached?.Invoke(this, Assign);

        }

        #endregion

        #region (private) WatchBeaconAsync(CancellationToken)

        /// <summary>
        /// Notice a coordinator that has stopped.
        /// </summary>
        private async Task WatchBeaconAsync(CancellationToken CancellationToken)
        {

            var timeout = options.BeaconTimeout ?? T1SConstants.DefaultBeaconTimeout;

            while (!CancellationToken.IsCancellationRequested)
            {

                try
                {
                    await Task.Delay(timeout / 2, CancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                String? reason = null;

                lock (padlock)
                {

                    if (coordinator is not null &&
                        lastBeaconAt is { } heard &&
                        clock.GetUtcNow() - heard > timeout)
                    {

                        reason = $"no BEACON from {coordinator} for {timeout}";

                        Reset(reason);

                        coordinator    = null;
                        lastBeaconAt   = null;
                        backoffCycles  = 0;

                    }

                }

                if (reason is not null)
                    Log?.Invoke(this, $"'{options.Name}' lost the coordinator: {reason}.");

            }

        }

        #endregion

        #region (private) Reset(Reason) / SendAsync(To, Message)

        /// <summary>
        /// Back to having no identifier. Called under the lock.
        /// </summary>
        private void Reset(String Reason)
        {

            var wasAttached = state == PlcaFollowerState.Attached;

            state      = PlcaFollowerState.Detached;
            nodeId     = null;
            weight     = 0;
            announced  = false;
            attached   = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            if (wasAttached)
                Detached?.Invoke(this, Reason);

        }

        private async Task SendAsync(MACAddress To, IT1SMessage Message)
        {

            try
            {

                await transport.SendAsync(To, Message, stopping.Token).ConfigureAwait(false);

                Sent?.Invoke(this, Message);

            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Log?.Invoke(this, $"'{options.Name}' could not send its {Message.Type}: {e.Message}");
            }

        }

        #endregion


        #region DisposeAsync()

        public async ValueTask DisposeAsync()
        {

            try
            {
                await LeaveAsync().ConfigureAwait(false);
            }
            catch { }

            stopping.Cancel();

            transport.FrameReceived -= OnFrame;

            if (watchdog is not null)
            {
                try { await watchdog.ConfigureAwait(false); }
                catch { }
            }

            stopping.Dispose();

        }

        #endregion

    }

}
