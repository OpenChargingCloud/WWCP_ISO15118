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

using cloud.charging.open.protocols.ISO15118.T1S.PLCA;
using cloud.charging.open.protocols.ISO15118.T1S.Messages;
using cloud.charging.open.protocols.ISO15118.T1S.Transport;

#endregion

namespace cloud.charging.open.protocols.ISO15118.T1S.Nodes
{

    #region TemperatureSensorOptions

    /// <summary>
    /// What a sensor is, and when it starts to worry on its own account.
    /// </summary>
    /// <param name="Name">What it is called - "DC+ pin", "DC- pin", "PE".</param>
    /// <param name="Warning_C">Above this it flags its readings as a warning.</param>
    /// <param name="Alarm_C">Above this it flags them as an alarm.</param>
    /// <param name="Model">The metal it sits in; a default pin when null.</param>
    public sealed record TemperatureSensorOptions(String         Name,
                                                  Double         Warning_C   = 70,
                                                  Double         Alarm_C     = 90,
                                                  ThermalModel?  Model       = null);

    #endregion


    /// <summary>
    /// A temperature sensor in a pin of the coupler: a follower on the bus
    /// with one number to report, and a lump of metal whose temperature that
    /// number is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every time the coordinator gives it an opportunity it answers with a
    /// reading - never a yield, because a sensor always has its number, and a
    /// sensor that went quiet would be a sensor the coordinator should worry
    /// about. The number comes from a <see cref="ThermalModel"/> that is
    /// advanced to the present moment just before it is read, so the pin
    /// heats and cools in real time with whatever current it is told it is
    /// carrying.
    /// </para>
    /// <para>
    /// The current is told, not measured: nothing here knows what the station
    /// is delivering. Whoever runs the bench sets <see cref="Current_A"/>, and
    /// the pin does what a pin does with it. That is the whole of what a
    /// simulated sensor needs to be for an overload to be detected at the
    /// other end - the detection is the coordinator's, and it is the same
    /// code whether the number came from a model or from a thermistor.
    /// </para>
    /// </remarks>
    public sealed class TemperatureSensorNode : IAsyncDisposable
    {

        #region Data

        private readonly TimeProvider   clock;
        private readonly Lock           padlock  = new ();
        private          DateTimeOffset lastAdvance;
        private          UInt16         sequence;

        #endregion

        #region Properties

        /// <summary>What it is, and its limits.</summary>
        public TemperatureSensorOptions  Options   { get; }

        /// <summary>The bus side of it.</summary>
        public PlcaFollower              Follower  { get; }

        /// <summary>The metal side of it.</summary>
        public ThermalModel              Model     { get; }

        /// <summary>
        /// The current through the pin, in A. Set by whoever is simulating the
        /// load; the pin heats towards what that current settles at.
        /// </summary>
        public Double Current_A
        {
            get { lock (padlock) return Model.Current_A; }
            set { lock (padlock) { AdvanceToNow(); Model.Current_A = value; } }
        }

        /// <summary>The pin's temperature right now, in °C.</summary>
        public Double Temperature_C
        {
            get { lock (padlock) { AdvanceToNow(); return Model.Temperature_C; } }
        }

        /// <summary>The last reading sent, or null before the first.</summary>
        public SensorReading?  LastReading  { get; private set; }

        #endregion

        #region Events

        /// <summary>A reading went out.</summary>
        public event EventHandler<SensorReading>?  ReadingSent;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// A sensor on a medium. Nothing happens until it is started.
        /// </summary>
        public TemperatureSensorNode(IT1STransport             Transport,
                                     TemperatureSensorOptions  Options,
                                     TimeProvider?             Clock   = null)
        {

            this.Options      = Options;
            this.clock        = Clock ?? TimeProvider.System;
            this.Model        = Options.Model ?? new ThermalModel();
            this.lastAdvance  = clock.GetUtcNow();

            this.Follower     = new PlcaFollower(
                                    Transport,
                                    new PlcaFollowerOptions(T1SNodeRole.TemperatureSensor, Options.Name),
                                    clock
                                ) {
                                    Supplier = Reading
                                };

        }

        #endregion


        #region StartAsync(CancellationToken = default)

        /// <summary>
        /// Start listening for the coordinator.
        /// </summary>
        public Task StartAsync(CancellationToken CancellationToken = default)
            => Follower.StartAsync(CancellationToken);

        #endregion


        #region (private) Reading() / AdvanceToNow()

        /// <summary>
        /// What this sensor says when asked: the pin's temperature now, with
        /// its own opinion of it.
        /// </summary>
        private IT1SMessage? Reading()
        {

            var nodeId = Follower.NodeId;

            if (nodeId is null)
                return null;

            SensorReading reading;

            lock (padlock)
            {

                AdvanceToNow();

                var temperature  = Model.Temperature_C;

                var flags        = temperature >= Options.Alarm_C   ? SensorFlags.Alarm
                                 : temperature >= Options.Warning_C ? SensorFlags.Warning
                                 :                                    SensorFlags.None;

                reading = SensorReading.Temperature(nodeId.Value, sequence++, temperature, flags);

                LastReading = reading;

            }

            ReadingSent?.Invoke(this, reading);

            return reading;

        }

        /// <summary>
        /// Let the time since the last look pass through the model. Called
        /// under the lock.
        /// </summary>
        private void AdvanceToNow()
        {

            var now = clock.GetUtcNow();

            Model.Advance(now - lastAdvance);

            lastAdvance = now;

        }

        #endregion


        #region DisposeAsync()

        public ValueTask DisposeAsync()
            => Follower.DisposeAsync();

        #endregion


        #region (override) ToString()

        public override String ToString()
            => $"'{Options.Name}': {Model}" + (Follower.NodeId is { } id ? $", node {id}" : ", not on the bus");

        #endregion

    }

}
