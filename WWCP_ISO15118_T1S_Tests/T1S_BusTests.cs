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
using System.Security.Cryptography;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

using cloud.charging.open.protocols.ISO15118.T1S.PLCA;
using cloud.charging.open.protocols.ISO15118.T1S.Nodes;
using cloud.charging.open.protocols.ISO15118.T1S.Messages;
using cloud.charging.open.protocols.ISO15118.T1S.Transport;
using cloud.charging.open.protocols.ISO15118.T1S.Monitoring;

#endregion

namespace cloud.charging.open.protocols.ISO15118.T1S.Tests
{

    /// <summary>
    /// A whole bus, over real sockets: a coordinator, a vehicle and two
    /// sensors on one multicast group, joining, being polled, overheating,
    /// going quiet.
    /// </summary>
    /// <remarks>
    /// Each fixture takes a group and port of its own, so that two test runs
    /// on one machine are two buses and not one. What they need from the
    /// machine is IPv4 multicast with loopback on whatever interface the
    /// operating system picks - which every laptop has, and which a
    /// container without a multicast route does not; a bus that never comes
    /// up here is a machine that cannot run the bench either.
    /// </remarks>
    [TestFixture]
    public class T1S_BusTests
    {

        #region (private) Data / helpers

        private IPEndPoint  group  = null!;

        /// <summary>
        /// Timings for a test: a cycle a few times a second, and a node given
        /// up after half a second of silence.
        /// </summary>
        private static readonly PlcaCoordinatorOptions  quick  = new (
                                                                     Name:                        "test EVSE",
                                                                     TransmitOpportunityTimeout:  TimeSpan.FromMilliseconds(100),
                                                                     DiscoveryWindow:             TimeSpan.FromMilliseconds(100),
                                                                     CycleGap:                    TimeSpan.FromMilliseconds(50),
                                                                     LostAfterMissedCycles:       3
                                                                 );

        [SetUp]
        public void EachTestItsOwnBus()
            => group = new IPEndPoint(
                           IPAddress.Parse("239.151.18.2"),
                           TestPorts.Free()
                       );

        private UdpMulticastT1STransport Medium()
            => new (T1SConstants.RandomLocalMac(), group);

        private static async Task Eventually(Func<Boolean>  Condition,
                                             String         What,
                                             TimeSpan?      Within   = null)
        {

            var deadline = DateTime.UtcNow + (Within ?? TimeSpan.FromSeconds(10));

            while (!Condition())
            {

                if (DateTime.UtcNow > deadline)
                    Assert.Fail($"Waited {Within ?? TimeSpan.FromSeconds(10)} for: {What}");

                await Task.Delay(20);

            }

        }

        #endregion


        #region NodesJoinAndArePolledEveryCycle()

        [Test]
        public async Task NodesJoinAndArePolledEveryCycle()
        {

            await using var evseMedium  = Medium();
            await using var evMedium    = Medium();
            await using var s1Medium    = Medium();
            await using var s2Medium    = Medium();

            await using var coordinator = new PlcaCoordinator(evseMedium, quick);
            await using var vehicle     = new PlcaFollower(evMedium, new PlcaFollowerOptions(T1SNodeRole.Vehicle, "truck", RequestedWeight: 3));
            await using var pin1        = new TemperatureSensorNode(s1Medium, new TemperatureSensorOptions("DC+ pin"));
            await using var pin2        = new TemperatureSensorNode(s2Medium, new TemperatureSensorOptions("DC- pin"));

            var cycles = new List<PlcaCycleReport>();
            coordinator.CycleCompleted += (_, report) => cycles.Add(report);

            await evseMedium.StartAsync();
            await evMedium.  StartAsync();
            await s1Medium.  StartAsync();
            await s2Medium.  StartAsync();

            await coordinator.StartAsync();
            await vehicle.    StartAsync();
            await pin1.       StartAsync();
            await pin2.       StartAsync();

            #region Everybody gets an identifier - one per cycle, so this takes a few

            Assert.That(await vehicle.      WaitUntilAttachedAsync(TimeSpan.FromSeconds(10)), Is.True, "the vehicle never attached");
            Assert.That(await pin1.Follower.WaitUntilAttachedAsync(TimeSpan.FromSeconds(10)), Is.True, "DC+ never attached");
            Assert.That(await pin2.Follower.WaitUntilAttachedAsync(TimeSpan.FromSeconds(10)), Is.True, "DC- never attached");

            Assert.Multiple(() => {
                Assert.That(coordinator.Nodes,                                         Has.Count.EqualTo(3));
                Assert.That(coordinator.Nodes.Select(node => node.NodeId),             Is.EquivalentTo(new Byte[] { 1, 2, 3 }), "identifiers are handed out from 1");
                Assert.That(vehicle.Weight,                                            Is.EqualTo(3), "the vehicle got what it asked for");
                Assert.That(pin1.Follower.Weight,                                      Is.EqualTo(1), "a sensor gets one whatever it asks for");
                Assert.That(coordinator.Nodes.Single(node => node.Role == T1SNodeRole.Vehicle).Name, Is.EqualTo("truck"));
            });

            #endregion

            #region And is polled: every sensor has a reading within a cycle or two

            await Eventually(() => coordinator.Nodes.Where(node => node.Role == T1SNodeRole.TemperatureSensor)
                                                    .All(node => node.LastReading is not null),
                             "a reading from both sensors");

            var cyclesBefore = cycles.Count;

            await Eventually(() => cycles.Count >= cyclesBefore + 3, "three more cycles");

            // A full cycle: 3 vehicle + 1 + 1 sensor opportunities, and discovery.
            var full = cycles.Last(report => report.Slots == 6);

            Assert.Multiple(() => {
                Assert.That(full.Missed,   Is.EqualTo(0),  "nobody was silent");
                Assert.That(full.Answered, Is.EqualTo(2),  "the two sensors sent readings");
                Assert.That(full.Yielded,  Is.EqualTo(3),  "the vehicle, with nothing queued, yielded its three");
            });

            var sensor = coordinator.Nodes.First(node => node.Role == T1SNodeRole.TemperatureSensor);

            Assert.That(sensor.LastReading!.Kind,      Is.EqualTo(SensorKind.Temperature));
            Assert.That(sensor.LastReading.AsDouble,   Is.EqualTo(20).Within(0.5), "a cold pin reads ambient");
            Assert.That(coordinator.OutOfTurn,         Is.EqualTo(0), "nobody spoke out of turn");

            #endregion

        }

        #endregion

        #region AVehicleWithSomethingToSayIsHeardInItsTurn()

        [Test]
        public async Task AVehicleWithSomethingToSayIsHeardInItsTurn()
        {

            await using var evseMedium  = Medium();
            await using var evMedium    = Medium();

            await using var coordinator = new PlcaCoordinator(evseMedium, quick);
            await using var vehicle     = new PlcaFollower(evMedium, new PlcaFollowerOptions(T1SNodeRole.Vehicle, "car"));

            var heard = new List<(PlcaNode Node, DecodedT1SFrame Frame)>();
            coordinator.FrameReceived += (_, pair) => heard.Add(pair);

            await evseMedium.StartAsync();
            await evMedium.  StartAsync();
            await coordinator.StartAsync();
            await vehicle.    StartAsync();

            Assert.That(await vehicle.WaitUntilAttachedAsync(TimeSpan.FromSeconds(10)), Is.True);

            vehicle.Enqueue(new Data([ 1, 2, 3, 4 ]));

            await Eventually(() => heard.Any(pair => pair.Frame.Message is Data), "the vehicle's data frame");

            var data = heard.Select(pair => pair.Frame.Message).OfType<Data>().Single();

            Assert.Multiple(() => {
                Assert.That(data.Payload,           Is.EqualTo(new Byte[] { 1, 2, 3, 4 }));
                Assert.That(heard[0].Node.Role,     Is.EqualTo(T1SNodeRole.Vehicle));
                Assert.That(heard.Select(pair => pair.Frame.Message).OfType<Announce>().Count(), Is.EqualTo(1), "it announced itself once, first");
                Assert.That(coordinator.OutOfTurn,  Is.EqualTo(0));
            });

        }

        #endregion

        #region AnOverloadIsDetectedAndCleared()

        [Test]
        public async Task AnOverloadIsDetectedAndCleared()
        {

            await using var evseMedium  = Medium();
            await using var s1Medium    = Medium();

            // A pin that reacts in half a second rather than a minute, so the
            // test does not take a minute.
            var pin = new ThermalModel(Ambient_C: 20, TimeConstant_s: 0.5, RatedCurrent_A: 500, RatedRise_C: 40);

            await using var coordinator = new PlcaCoordinator(evseMedium, quick);
            await using var sensor      = new TemperatureSensorNode(s1Medium, new TemperatureSensorOptions("DC+ pin", Model: pin));

            var monitor  = new CableThermalMonitor(new CableThermalMonitorOptions(Warning_C: 70, Overload_C: 90, Hysteresis_C: 5));
            var changes  = new List<ThermalStateChange>();

            monitor.StateChanged        += (_, change) => changes.Add(change);
            coordinator.ReadingReceived += (_, pair)   => monitor.Observe(pair.Node, pair.Reading, DateTimeOffset.UtcNow);
            coordinator.NodeLost        += (_, node)   => monitor.Lost(node, DateTimeOffset.UtcNow);

            await evseMedium.StartAsync();
            await s1Medium.  StartAsync();
            await coordinator.StartAsync();
            await sensor.     StartAsync();

            Assert.That(await sensor.Follower.WaitUntilAttachedAsync(TimeSpan.FromSeconds(10)), Is.True);

            // 800 A through a 500 A pin: settles 102 K above the room.
            sensor.Current_A = 800;

            await Eventually(() => changes.Any(change => change.To == ThermalState.Overload), "the overload");

            var overload = changes.First(change => change.To == ThermalState.Overload);

            Assert.Multiple(() => {
                Assert.That(overload.IsAlarm,        Is.True);
                Assert.That(overload.Temperature_C,  Is.GreaterThanOrEqualTo(90));
                Assert.That(overload.Node.Name,      Is.EqualTo("DC+ pin"));
                Assert.That(monitor.InAlarm,         Is.True);
                Assert.That(sensor.LastReading!.Flags.HasFlag(SensorFlags.Alarm), Is.True, "the sensor thought so too");
            });

            // Passed through warning on the way up, and said so once.
            Assert.That(changes.Select(change => change.To).Take(2), Is.EqualTo(new[] { ThermalState.Warning, ThermalState.Overload }));

            // The current goes away: the pin cools, and the alarm clears.
            sensor.Current_A = 0;

            await Eventually(() => monitor.Overall == ThermalState.Normal, "the all-clear");

            Assert.That(changes.Last().To,   Is.EqualTo(ThermalState.Normal));
            Assert.That(changes.Any(change => change.IsAllClear), Is.True);

        }

        #endregion

        #region AQuietSensorIsGivenUpForLost()

        [Test]
        public async Task AQuietSensorIsGivenUpForLost()
        {

            await using var evseMedium  = Medium();
            var             s1Medium    = Medium();

            await using var coordinator = new PlcaCoordinator(evseMedium, quick);
            var             sensor      = new TemperatureSensorNode(s1Medium, new TemperatureSensorOptions("DC+ pin"));

            var monitor  = new CableThermalMonitor();
            var lost     = new List<PlcaNode>();

            coordinator.NodeLost        += (_, node) => { lost.Add(node); monitor.Lost(node, DateTimeOffset.UtcNow); };
            coordinator.ReadingReceived += (_, pair) => monitor.Observe(pair.Node, pair.Reading, DateTimeOffset.UtcNow);

            await evseMedium.StartAsync();
            await s1Medium.  StartAsync();
            await coordinator.StartAsync();
            await sensor.     StartAsync();

            Assert.That(await sensor.Follower.WaitUntilAttachedAsync(TimeSpan.FromSeconds(10)), Is.True);
            await Eventually(() => monitor.States.ContainsKey(sensor.Follower.NodeId!.Value), "a first reading");

            // The sensor dies: its socket is closed without a LEAVE, so the
            // coordinator finds out the only way it can - by silence.
            await s1Medium.DisposeAsync();

            await Eventually(() => lost.Count == 1, "the coordinator giving the sensor up");

            Assert.Multiple(() => {
                Assert.That(lost[0].Name,           Is.EqualTo("DC+ pin"));
                Assert.That(lost[0].MissedCycles,   Is.EqualTo(quick.LostAfterMissedCycles));
                Assert.That(coordinator.Nodes,      Is.Empty);
                Assert.That(monitor.Overall,        Is.EqualTo(ThermalState.Lost), "a lost sensor is an alarm");
                Assert.That(monitor.InAlarm,        Is.True);
            });

            await sensor.DisposeAsync();

        }

        #endregion

        #region ALeavingNodeIsRemovedAtOnce()

        [Test]
        public async Task ALeavingNodeIsRemovedAtOnce()
        {

            await using var evseMedium  = Medium();
            await using var evMedium    = Medium();

            await using var coordinator = new PlcaCoordinator(evseMedium, quick);
            await using var vehicle     = new PlcaFollower(evMedium, new PlcaFollowerOptions(T1SNodeRole.Vehicle, "car"));

            var left = new List<PlcaNode>();
            coordinator.NodeLeft += (_, node) => left.Add(node);

            await evseMedium.StartAsync();
            await evMedium.  StartAsync();
            await coordinator.StartAsync();
            await vehicle.    StartAsync();

            Assert.That(await vehicle.WaitUntilAttachedAsync(TimeSpan.FromSeconds(10)), Is.True);

            await vehicle.LeaveAsync();

            await Eventually(() => left.Count == 1, "the coordinator noticing the vehicle left");

            Assert.Multiple(() => {
                Assert.That(coordinator.Nodes,  Is.Empty);
                Assert.That(vehicle.State,      Is.EqualTo(PlcaFollowerState.Detached));
                Assert.That(vehicle.NodeId,     Is.Null);
            });

        }

        #endregion

        #region AFrameOutOfTurnIsReportedNotObeyed()

        [Test]
        public async Task AFrameOutOfTurnIsReportedNotObeyed()
        {

            await using var evseMedium   = Medium();
            await using var rogueMedium  = Medium();

            await using var coordinator  = new PlcaCoordinator(evseMedium, quick with { CycleGap = TimeSpan.FromSeconds(1) });

            var outOfTurn = new List<DecodedT1SFrame>();
            coordinator.OutOfTurnFrame += (_, frame) => outOfTurn.Add(frame);

            await evseMedium. StartAsync();
            await rogueMedium.StartAsync();
            await coordinator.StartAsync();

            // Let a cycle go by, then speak into the gap: no opportunity is
            // open, so this is a collision on a real bus and a report here.
            await Task.Delay(400);

            await rogueMedium.SendAsync(MACAddress.Broadcast, SensorReading.Temperature(9, 0, 999));

            await Eventually(() => outOfTurn.Count == 1, "the out-of-turn report");

            Assert.Multiple(() => {
                Assert.That(outOfTurn[0].Source,   Is.EqualTo(rogueMedium.LocalMac));
                Assert.That(coordinator.Nodes,     Is.Empty, "a node that never joined is not a node");
                Assert.That(coordinator.OutOfTurn, Is.EqualTo(1));
            });

        }

        #endregion

        #region AFollowerNoticesTheCoordinatorGoing()

        [Test]
        public async Task AFollowerNoticesTheCoordinatorGoing()
        {

            await using var evseMedium  = Medium();
            await using var evMedium    = Medium();

            var coordinator = new PlcaCoordinator(evseMedium, quick);

            await using var vehicle = new PlcaFollower(
                                          evMedium,
                                          new PlcaFollowerOptions(T1SNodeRole.Vehicle, "car", BeaconTimeout: TimeSpan.FromMilliseconds(600))
                                      );

            var detached = new List<String>();
            vehicle.Detached += (_, reason) => detached.Add(reason);

            await evseMedium.StartAsync();
            await evMedium.  StartAsync();
            await coordinator.StartAsync();
            await vehicle.    StartAsync();

            Assert.That(await vehicle.WaitUntilAttachedAsync(TimeSpan.FromSeconds(10)), Is.True);

            // The station goes down. No LEAVE comes from a coordinator; the
            // BEACON just stops.
            await coordinator.DisposeAsync();

            await Eventually(() => detached.Count == 1, "the vehicle noticing the missing BEACON", TimeSpan.FromSeconds(5));

            Assert.Multiple(() => {
                Assert.That(detached[0],        Does.Contain("no BEACON"));
                Assert.That(vehicle.State,      Is.EqualTo(PlcaFollowerState.Detached));
                Assert.That(vehicle.NodeId,     Is.Null);
            });

        }

        #endregion

    }

}
