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

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

using cloud.charging.open.protocols.ISO15118.T1S.PLCA;
using cloud.charging.open.protocols.ISO15118.T1S.Messages;
using cloud.charging.open.protocols.ISO15118.T1S.Transport;

#endregion

namespace cloud.charging.open.protocols.ISO15118.T1S.Tests
{

    /// <summary>
    /// One follower, with the test as its coordinator: every frame it hears
    /// is handed to it by hand, and every frame it sends is written down, so
    /// what it does with each one is seen without a socket or a clock in
    /// between.
    /// </summary>
    [TestFixture]
    public class T1S_FollowerTests
    {

        #region (private) Data / helpers

        /// <summary>
        /// A medium the test speaks on. What the test says arrives at once, on
        /// the test's own thread, and what the follower sends is kept.
        /// </summary>
        private sealed class ScriptedMedium : IT1STransport
        {

            public MACAddress  LocalMac     { get; } = T1SConstants.RandomLocalMac();

            /// <summary>Whom the test speaks as, unless told otherwise.</summary>
            public MACAddress  Coordinator  { get; } = T1SConstants.RandomLocalMac();

            public String      Description  => "a scripted medium";

            public List<(MACAddress Destination, IT1SMessage Message)>  Sent  { get; } = [];

            public event EventHandler<DecodedT1SFrame>?  FrameReceived;

            public Task StartAsync(CancellationToken CancellationToken = default)
                => Task.CompletedTask;

            public Task SendAsync(MACAddress         Destination,
                                  IT1SMessage        Message,
                                  CancellationToken  CancellationToken = default)
            {
                Sent.Add((Destination, Message));
                return Task.CompletedTask;
            }

            public Task SendRawAsync(EthernetFrame      Frame,
                                     CancellationToken  CancellationToken = default)

                => throw new NotSupportedException("A follower sends messages, not frames.");

            /// <summary>
            /// The coordinator says something: to everybody, or to this node.
            /// </summary>
            public void Hear(IT1SMessage  Message,
                             Boolean      ToThisNode   = false,
                             MACAddress?  From         = null)

                => FrameReceived?.Invoke(
                       this,
                       new DecodedT1SFrame(
                           ToThisNode ? LocalMac : MACAddress.Broadcast,
                           From ?? Coordinator,
                           Message,
                           DateTimeOffset.UtcNow
                       )
                   );

            public ValueTask DisposeAsync()
                => ValueTask.CompletedTask;

        }

        /// <summary>
        /// A vehicle whose watchdog stays out of the way: these tests are
        /// about what it does with what it hears, not about silence.
        /// </summary>
        private static readonly PlcaFollowerOptions  car  = new (
                                                               T1SNodeRole.Vehicle,
                                                               "car",
                                                               BeaconTimeout: TimeSpan.FromHours(1)
                                                           );

        /// <summary>
        /// One cycle with nobody in it but the discovery opportunity. Answers
        /// what the follower sent in it.
        /// </summary>
        private static IT1SMessage[] DiscoveryCycle(ScriptedMedium  Medium,
                                                    UInt32          Cycle,
                                                    MACAddress?     From   = null)
        {

            var before = Medium.Sent.Count;

            Medium.Hear(new Beacon(Cycle, 1),                                              From: From);
            Medium.Hear(new TransmitOpportunity(Cycle, 0, T1SConstants.UnassignedNodeId),  From: From);

            return [.. Medium.Sent.Skip(before).Select(sent => sent.Message)];

        }

        /// <summary>
        /// Through the discovery opportunity of the given cycle to an
        /// identifier.
        /// </summary>
        private static void Attach(ScriptedMedium  Medium,
                                   PlcaFollower    Follower,
                                   UInt32          Cycle,
                                   Byte            NodeId,
                                   MACAddress?     From   = null)
        {

            var asked = DiscoveryCycle(Medium, Cycle, From);

            Assert.That(asked, Has.Length.EqualTo(1).And.All.InstanceOf<Join>(), "it asked for an identifier");

            Medium.Hear(new Assign(((Join) asked[0]).Nonce, NodeId, T1SNodeRole.Vehicle, 1), ToThisNode: true, From: From);

            Assert.That(Follower.State, Is.EqualTo(PlcaFollowerState.Attached));

        }

        #endregion


        #region AFollowerThatLeftDoesNotAskAgain()

        [Test]
        public async Task AFollowerThatLeftDoesNotAskAgain()
        {

            await using var medium   = new ScriptedMedium();
            await using var vehicle  = new PlcaFollower(medium, car);

            await vehicle.StartAsync();

            Attach(medium, vehicle, Cycle: 1, NodeId: 1);

            await vehicle.LeaveAsync();

            Assert.That(medium.Sent[^1], Is.EqualTo((medium.Coordinator, (IT1SMessage) new Leave(1))), "it said goodbye");

            // Three cycles, each with a discovery opportunity it could ask in.
            var said = Enumerable.Range(2, 3).SelectMany(cycle => DiscoveryCycle(medium, (UInt32) cycle)).ToArray();

            Assert.Multiple(() => {
                Assert.That(said,            Is.Empty, "a node that left stays off the bus");
                Assert.That(vehicle.State,   Is.EqualTo(PlcaFollowerState.Detached));
                Assert.That(vehicle.NodeId,  Is.Null);
            });

        }

        #endregion

        #region AFollowerStartedAgainAfterLeavingAsksAgain()

        [Test]
        public async Task AFollowerStartedAgainAfterLeavingAsksAgain()
        {

            await using var medium   = new ScriptedMedium();
            await using var vehicle  = new PlcaFollower(medium, car);

            await vehicle.StartAsync();

            Attach(medium, vehicle, Cycle: 1, NodeId: 1);

            await vehicle.LeaveAsync();

            Assert.That(DiscoveryCycle(medium, 2), Is.Empty, "not before it is told to");

            await vehicle.StartAsync();

            Attach(medium, vehicle, Cycle: 3, NodeId: 2);

            Assert.That(vehicle.NodeId, Is.EqualTo(2));

        }

        #endregion

        #region AnIdentifierThatComesAfterLeavingIsGivenBack()

        [Test]
        public async Task AnIdentifierThatComesAfterLeavingIsGivenBack()
        {

            await using var medium   = new ScriptedMedium();
            await using var vehicle  = new PlcaFollower(medium, car);

            var attached = 0;
            vehicle.Attached += (_, _) => attached++;

            await vehicle.StartAsync();

            var join = DiscoveryCycle(medium, 1).OfType<Join>().Single();

            Assert.That(vehicle.State, Is.EqualTo(PlcaFollowerState.Joining));

            // Asked, and leaving before the answer: there is no identifier yet
            // to say goodbye with.
            await vehicle.LeaveAsync();

            var sentBefore = medium.Sent.Count;

            Assert.That(vehicle.State, Is.EqualTo(PlcaFollowerState.Detached));

            // The coordinator wrote the node down before it answered; the
            // answer is the first moment the node can tell it otherwise.
            medium.Hear(new Assign(join.Nonce, 3, T1SNodeRole.Vehicle, 1), ToThisNode: true);

            Assert.Multiple(() => {
                Assert.That(medium.Sent.Skip(sentBefore), Is.EqualTo(new[] { (medium.Coordinator, (IT1SMessage) new Leave(3)) }), "given straight back");
                Assert.That(vehicle.State,                Is.EqualTo(PlcaFollowerState.Detached));
                Assert.That(vehicle.NodeId,               Is.Null);
                Assert.That(attached,                     Is.Zero);
                Assert.That(DiscoveryCycle(medium, 2),    Is.Empty, "and it still does not ask again");
            });

        }

        #endregion

        #region AFollowerThatDidNotLeaveAsksAgain()

        [Test]
        public async Task AFollowerThatDidNotLeaveAsksAgain()
        {

            await using var medium   = new ScriptedMedium();
            await using var vehicle  = new PlcaFollower(medium, car);

            var detached = new List<String>();
            vehicle.Detached += (_, reason) => detached.Add(reason);

            await vehicle.StartAsync();

            Attach(medium, vehicle, Cycle: 1, NodeId: 1);

            // Another coordinator: another bus, on which the old identifier
            // means nothing. Losing it this way is not leaving, and the node
            // asks the new coordinator for one of its own.
            var restarted = T1SConstants.RandomLocalMac();

            Attach(medium, vehicle, Cycle: 1, NodeId: 4, From: restarted);

            Assert.Multiple(() => {
                Assert.That(detached,             Has.Count.EqualTo(1));
                Assert.That(detached[0],          Does.Contain("instead of"));
                Assert.That(vehicle.NodeId,       Is.EqualTo(4));
                Assert.That(vehicle.Coordinator,  Is.EqualTo(restarted));
            });

        }

        #endregion

    }

}
