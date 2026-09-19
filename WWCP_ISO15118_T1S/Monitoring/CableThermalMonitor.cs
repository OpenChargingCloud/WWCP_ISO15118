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

using cloud.charging.open.protocols.ISO15118.T1S.PLCA;
using cloud.charging.open.protocols.ISO15118.T1S.Messages;

#endregion

namespace cloud.charging.open.protocols.ISO15118.T1S.Monitoring
{

    #region (enum) ThermalState

    /// <summary>
    /// What the monitor makes of one sensor.
    /// </summary>
    public enum ThermalState
    {

        /// <summary>Below the warning limit, and answering.</summary>
        Normal,

        /// <summary>Above the warning limit: hot, and heading somewhere.</summary>
        Warning,

        /// <summary>Above the overload limit: the coupler is being asked for more than it can carry.</summary>
        Overload,

        /// <summary>
        /// Not answering. Treated as seriously as an overload, because a
        /// sensor that has gone quiet is a sensor that cannot say the pin is
        /// melting - and the one thing a monitor must not do is read silence
        /// as "fine".
        /// </summary>
        Lost

    }

    #endregion

    #region ThermalStateChange

    /// <summary>
    /// One sensor going from one state to another.
    /// </summary>
    /// <param name="Node">The sensor.</param>
    /// <param name="From">What it was.</param>
    /// <param name="To">What it is now.</param>
    /// <param name="Temperature_C">The reading that did it, or null when the sensor was lost.</param>
    /// <param name="At">When.</param>
    public sealed record ThermalStateChange(PlcaNode        Node,
                                            ThermalState    From,
                                            ThermalState    To,
                                            Double?         Temperature_C,
                                            DateTimeOffset  At)
    {

        /// <summary>Whether this change is the kind that stops a charging session.</summary>
        public Boolean IsAlarm
            => To is ThermalState.Overload or ThermalState.Lost;

        /// <summary>Whether this change ends an alarm.</summary>
        public Boolean IsAllClear
            => From is ThermalState.Overload or ThermalState.Lost && To is ThermalState.Normal or ThermalState.Warning;

    }

    #endregion

    #region CableThermalMonitorOptions

    /// <summary>
    /// Where the lines are drawn.
    /// </summary>
    /// <remarks>
    /// Bench numbers, and named as such. IEC 62196 puts the surface of a
    /// coupler that somebody may touch at 60 °C or less, and the pins inside a
    /// megawatt coupler run hotter than the outside by design; the MCS
    /// connector specifications set their own limits for the pin, and a
    /// station in the field takes them from the coupler it was fitted with,
    /// not from here.
    /// </remarks>
    /// <param name="Warning_C">Above this a sensor is warm enough to say so.</param>
    /// <param name="Overload_C">Above this the coupler is overloaded and the session should stop.</param>
    /// <param name="Hysteresis_C">How far below a limit the reading has to fall before the state falls back. Without this a pin sitting on the line would alarm and clear on every reading.</param>
    public sealed record CableThermalMonitorOptions(Double  Warning_C     = 70,
                                                    Double  Overload_C    = 90,
                                                    Double  Hysteresis_C  = 5)
    {

        /// <summary>The defaults.</summary>
        public static CableThermalMonitorOptions Default { get; } = new ();

    }

    #endregion


    /// <summary>
    /// The coordinator's opinion of every temperature sensor on the bus:
    /// warm, overloaded, or gone.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Sits beside a <see cref="PlcaCoordinator"/> and is fed by it: every
    /// reading a sensor sends in its opportunity, and every node the
    /// coordinator gives up on. Out of those it keeps one state per sensor and
    /// says when a state changes - which is the only time anybody needs to
    /// hear from it. A station that logged every reading would have a log
    /// nobody reads; one that says "DC+ pin overloaded at 91.3 °C" once, and
    /// "DC+ pin back to 84.9 °C" once, has said everything.
    /// </para>
    /// <para>
    /// A sensor's own flags are not what decides. A sensor that says it is in
    /// alarm at 80 °C on a bus whose limit is 90 °C is noted and not obeyed:
    /// the monitor is the one place the limits are written, and two places
    /// that could disagree would be two places to check when they did.
    /// </para>
    /// </remarks>
    public sealed class CableThermalMonitor
    {

        #region Data

        private readonly ConcurrentDictionary<Byte, ThermalState>  states  = new ();
        private readonly CableThermalMonitorOptions                options;

        #endregion

        #region Properties

        /// <summary>Where the lines are drawn.</summary>
        public CableThermalMonitorOptions  Options
            => options;

        /// <summary>
        /// The state of every sensor the monitor has heard from, by node.
        /// </summary>
        public IReadOnlyDictionary<Byte, ThermalState>  States
            => states;

        /// <summary>
        /// The worst of them: the one state that says whether the coupler is
        /// fit to carry current right now.
        /// </summary>
        public ThermalState  Overall
            => states.IsEmpty
                   ? ThermalState.Normal
                   : states.Values.Max();

        /// <summary>Whether anything is overloaded or unaccounted for.</summary>
        public Boolean       InAlarm
            => Overall is ThermalState.Overload or ThermalState.Lost;

        #endregion

        #region Events

        /// <summary>
        /// A sensor changed state.
        /// </summary>
        public event EventHandler<ThermalStateChange>?  StateChanged;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// A monitor with the given limits, or the bench defaults.
        /// </summary>
        public CableThermalMonitor(CableThermalMonitorOptions? Options = null)
        {
            this.options = Options ?? CableThermalMonitorOptions.Default;
        }

        #endregion


        #region Observe(Node, Reading, At)

        /// <summary>
        /// A reading arrived. Answers the change it caused, or null when it
        /// caused none.
        /// </summary>
        public ThermalStateChange? Observe(PlcaNode        Node,
                                           SensorReading   Reading,
                                           DateTimeOffset  At)
        {

            // Only temperatures are anybody's business here. A current sensor
            // on the same bus is somebody else's monitor.
            if (Reading.Kind != SensorKind.Temperature)
                return null;

            var temperature = Reading.AsDouble;
            var before      = states.GetValueOrDefault(Node.NodeId, ThermalState.Normal);
            var after       = Next(before, temperature);

            states[Node.NodeId] = after;

            if (after == before)
                return null;

            var change = new ThermalStateChange(Node, before, after, temperature, At);

            StateChanged?.Invoke(this, change);

            return change;

        }

        #endregion

        #region Lost(Node, At) / Forget(Node)

        /// <summary>
        /// The coordinator gave this sensor up. Answers the change it caused,
        /// or null when the node was no temperature sensor.
        /// </summary>
        public ThermalStateChange? Lost(PlcaNode        Node,
                                        DateTimeOffset  At)
        {

            if (Node.Role != T1SNodeRole.TemperatureSensor)
                return null;

            var before = states.GetValueOrDefault(Node.NodeId, ThermalState.Normal);

            states[Node.NodeId] = ThermalState.Lost;

            if (before == ThermalState.Lost)
                return null;

            var change = new ThermalStateChange(Node, before, ThermalState.Lost, null, At);

            StateChanged?.Invoke(this, change);

            return change;

        }

        /// <summary>
        /// A sensor left on purpose, or was replaced: it is no longer one
        /// whose silence means anything.
        /// </summary>
        public void Forget(PlcaNode Node)
            => states.TryRemove(Node.NodeId, out _);

        #endregion


        #region (private) Next(State, Temperature_C)

        /// <summary>
        /// The state a reading puts a sensor in, given the one it was in.
        /// </summary>
        /// <remarks>
        /// Going up is immediate: a reading above a line crosses it. Coming
        /// down needs the hysteresis: a reading just under the line leaves
        /// the state where it is, so that a pin sitting at the limit does not
        /// alarm and clear with every cycle. A lost sensor that speaks again
        /// is judged by its reading like any other.
        /// </remarks>
        private ThermalState Next(ThermalState  State,
                                  Double        Temperature_C)
        {

            if (Temperature_C >= options.Overload_C)
                return ThermalState.Overload;

            if (Temperature_C >= options.Warning_C)
                return State == ThermalState.Overload && Temperature_C > options.Overload_C - options.Hysteresis_C
                           ? ThermalState.Overload
                           : ThermalState.Warning;

            return State switch {
                       ThermalState.Overload when Temperature_C > options.Overload_C - options.Hysteresis_C  => ThermalState.Overload,
                       ThermalState.Overload                                                                  => ThermalState.Warning,
                       ThermalState.Warning  when Temperature_C > options.Warning_C  - options.Hysteresis_C  => ThermalState.Warning,
                       _                                                                                      => ThermalState.Normal
                   };

        }

        #endregion

    }

}
