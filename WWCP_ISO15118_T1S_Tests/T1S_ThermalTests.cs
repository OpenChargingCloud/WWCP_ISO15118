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
using cloud.charging.open.protocols.ISO15118.T1S.Nodes;
using cloud.charging.open.protocols.ISO15118.T1S.Messages;
using cloud.charging.open.protocols.ISO15118.T1S.Monitoring;

#endregion

namespace cloud.charging.open.protocols.ISO15118.T1S.Tests
{

    /// <summary>
    /// The pin and the monitor: a pin that heats with the square of the
    /// current and settles, and a monitor that says "overload" once and
    /// "clear" once rather than flickering on the line.
    /// </summary>
    [TestFixture]
    public class T1S_ThermalTests
    {

        #region (private static) ANode(NodeId, Role)

        private static PlcaNode ANode(Byte NodeId, T1SNodeRole Role = T1SNodeRole.TemperatureSensor)
        {

            // The registry entry is the coordinator's to make; a test makes
            // one the way the coordinator would, through its constructor.
            var constructor = typeof(PlcaNode).GetConstructors(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Single();

            return (PlcaNode) constructor.Invoke([
                       NodeId,
                       MACAddress.Parse($"02:71:50:00:00:{NodeId:X2}"),
                       Role,
                       $"sensor {NodeId}",
                       (Byte) 1,
                       DateTimeOffset.UtcNow
                   ]);

        }

        #endregion


        #region APinAtRatedCurrentSettlesAtTheRatedRise()

        [Test]
        public void APinAtRatedCurrentSettlesAtTheRatedRise()
        {

            var pin = new ThermalModel(Ambient_C: 20, TimeConstant_s: 60, RatedCurrent_A: 500, RatedRise_C: 40) {
                          Current_A = 500
                      };

            Assert.That(pin.SteadyState_C, Is.EqualTo(60).Within(1e-9));

            // One time constant: about 63 % of the way.
            pin.Advance(TimeSpan.FromSeconds(60));
            Assert.That(pin.Temperature_C, Is.EqualTo(20 + 40 * (1 - Math.Exp(-1))).Within(1e-6));

            // An hour: there.
            pin.Advance(TimeSpan.FromHours(1));
            Assert.That(pin.Temperature_C, Is.EqualTo(60).Within(0.01));

        }

        #endregion

        #region HeatingGoesWithTheSquareOfTheCurrent()

        /// <summary>
        /// 800 A through a pin rated for 500 A and 40 K is 2.56 times the
        /// heat, and a pin that settles 102 K above the room - which is why
        /// an overload is a temperature.
        /// </summary>
        [Test]
        public void HeatingGoesWithTheSquareOfTheCurrent()
        {

            var pin = new ThermalModel(Ambient_C: 20, RatedCurrent_A: 500, RatedRise_C: 40);

            Assert.Multiple(() => {
                Assert.That(pin.RiseAt(250), Is.EqualTo(10).   Within(1e-9));
                Assert.That(pin.RiseAt(500), Is.EqualTo(40).   Within(1e-9));
                Assert.That(pin.RiseAt(800), Is.EqualTo(102.4).Within(1e-9));
            });

        }

        #endregion

        #region APinCoolsBackToAmbient()

        [Test]
        public void APinCoolsBackToAmbient()
        {

            var pin = new ThermalModel(Ambient_C: 20) { Current_A = 800 };

            pin.Advance(TimeSpan.FromHours(1));
            Assert.That(pin.Temperature_C, Is.GreaterThan(100));

            pin.Current_A = 0;
            pin.Advance(TimeSpan.FromHours(1));

            Assert.That(pin.Temperature_C, Is.EqualTo(20).Within(0.01));

        }

        #endregion

        #region AStepOfAnyLengthNeverOvershoots()

        /// <summary>
        /// The exact solution, not Euler: one enormous step lands on the
        /// steady state rather than past it.
        /// </summary>
        [Test]
        public void AStepOfAnyLengthNeverOvershoots()
        {

            var pin = new ThermalModel(Ambient_C: 20, RatedCurrent_A: 500, RatedRise_C: 40) { Current_A = 500 };

            pin.Advance(TimeSpan.FromDays(365));

            Assert.That(pin.Temperature_C, Is.EqualTo(60).Within(1e-6));

        }

        #endregion


        #region TheMonitorSaysOverloadOnceAndClearOnce()

        [Test]
        public void TheMonitorSaysOverloadOnceAndClearOnce()
        {

            var monitor  = new CableThermalMonitor(new CableThermalMonitorOptions(Warning_C: 70, Overload_C: 90, Hysteresis_C: 5));
            var node     = ANode(2);
            var changes  = new List<ThermalStateChange>();
            var now      = DateTimeOffset.UtcNow;

            monitor.StateChanged += (_, change) => changes.Add(change);

            UInt16 sequence = 0;

            void Read(Double Celsius)
                => monitor.Observe(node, SensorReading.Temperature(2, sequence++, Celsius), now);

            Read(25);        // normal
            Read(69.9);      // still normal
            Read(70);        // warning
            Read(75);        // still warning: no second event
            Read(90);        // overload
            Read(91);        // still overload
            Read(88);        // still overload: within the hysteresis
            Read(84.9);      // back to warning
            Read(66);        // still warning: within the hysteresis
            Read(64.9);      // normal

            Assert.That(changes.Select(change => change.To), Is.EqualTo(new[] {
                ThermalState.Warning,
                ThermalState.Overload,
                ThermalState.Warning,
                ThermalState.Normal
            }));

            Assert.Multiple(() => {
                Assert.That(changes[1].IsAlarm,        Is.True);
                Assert.That(changes[1].Temperature_C,  Is.EqualTo(90));
                Assert.That(changes[2].IsAllClear,     Is.True);
                Assert.That(monitor.Overall,           Is.EqualTo(ThermalState.Normal));
                Assert.That(monitor.InAlarm,           Is.False);
            });

        }

        #endregion

        #region AJumpStraightIntoOverloadIsOneEvent()

        [Test]
        public void AJumpStraightIntoOverloadIsOneEvent()
        {

            var monitor  = new CableThermalMonitor();
            var node     = ANode(3);
            var changes  = new List<ThermalStateChange>();

            monitor.StateChanged += (_, change) => changes.Add(change);

            monitor.Observe(node, SensorReading.Temperature(3, 0, 120), DateTimeOffset.UtcNow);

            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].From, Is.EqualTo(ThermalState.Normal));
            Assert.That(changes[0].To,   Is.EqualTo(ThermalState.Overload));

        }

        #endregion

        #region ALostSensorIsAnAlarm()

        [Test]
        public void ALostSensorIsAnAlarm()
        {

            var monitor  = new CableThermalMonitor();
            var node     = ANode(4);
            var changes  = new List<ThermalStateChange>();

            monitor.StateChanged += (_, change) => changes.Add(change);

            monitor.Observe(node, SensorReading.Temperature(4, 0, 30), DateTimeOffset.UtcNow);
            monitor.Lost(node, DateTimeOffset.UtcNow);
            monitor.Lost(node, DateTimeOffset.UtcNow);   // said once

            Assert.Multiple(() => {
                Assert.That(changes,                    Has.Count.EqualTo(1));
                Assert.That(changes[0].To,              Is.EqualTo(ThermalState.Lost));
                Assert.That(changes[0].IsAlarm,         Is.True);
                Assert.That(changes[0].Temperature_C,   Is.Null);
                Assert.That(monitor.InAlarm,            Is.True);
                Assert.That(monitor.Overall,            Is.EqualTo(ThermalState.Lost));
            });

            // Back, and cool: cleared.
            monitor.Observe(node, SensorReading.Temperature(4, 1, 30), DateTimeOffset.UtcNow);

            Assert.That(changes[^1].To, Is.EqualTo(ThermalState.Normal));
            Assert.That(monitor.InAlarm, Is.False);

        }

        #endregion

        #region TheWorstSensorIsTheOverallState()

        [Test]
        public void TheWorstSensorIsTheOverallState()
        {

            var monitor = new CableThermalMonitor();
            var now     = DateTimeOffset.UtcNow;

            monitor.Observe(ANode(1), SensorReading.Temperature(1, 0, 30), now);
            monitor.Observe(ANode(2), SensorReading.Temperature(2, 0, 75), now);
            monitor.Observe(ANode(3), SensorReading.Temperature(3, 0, 30), now);

            Assert.That(monitor.Overall, Is.EqualTo(ThermalState.Warning));

            monitor.Observe(ANode(3), SensorReading.Temperature(3, 1, 95), now);

            Assert.That(monitor.Overall, Is.EqualTo(ThermalState.Overload));
            Assert.That(monitor.InAlarm, Is.True);

        }

        #endregion

        #region OnlyTemperaturesAreJudged()

        [Test]
        public void OnlyTemperaturesAreJudged()
        {

            var monitor = new CableThermalMonitor();

            // 3000 A is not a temperature, and not this monitor's business.
            var change = monitor.Observe(ANode(5, T1SNodeRole.Sensor), new SensorReading(5, SensorKind.Current, 0, 30000, SensorFlags.None), DateTimeOffset.UtcNow);

            Assert.That(change,          Is.Null);
            Assert.That(monitor.States,  Is.Empty);

            // And a lost current sensor is somebody else's alarm.
            Assert.That(monitor.Lost(ANode(5, T1SNodeRole.Sensor), DateTimeOffset.UtcNow), Is.Null);

        }

        #endregion

    }

}
