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

namespace cloud.charging.open.protocols.ISO15118.T1S
{

    /// <summary>
    /// What a node on the bus is, as it says so when it joins.
    /// </summary>
    /// <remarks>
    /// Clause 148 knows a coordinator and everybody else. The role is the
    /// emulation's, and it exists for one reason: the coordinator decides how
    /// much of the cycle a node gets by what it is, and a vehicle that asked
    /// for eight transmit opportunities without saying it was a vehicle would
    /// be a sensor asking for the bus.
    /// </remarks>
    public enum T1SNodeRole : Byte
    {

        /// <summary>
        /// Node 0: the charging station's coupler electronics, which sends the
        /// BEACON and hands out every transmit opportunity.
        /// </summary>
        Coordinator        = 0,

        /// <summary>
        /// The vehicle at the other end of the coupler, and the one node whose
        /// frames matter most: everything ISO 15118-20 says goes through it.
        /// </summary>
        Vehicle            = 1,

        /// <summary>
        /// A temperature sensor - in a pin of the coupler, in the cable, in
        /// the inlet. It has one number to report and reports it whenever it
        /// is asked.
        /// </summary>
        TemperatureSensor  = 2,

        /// <summary>
        /// A node that measures something else and says so in its reading's
        /// kind. What the bus carries, not what it understands.
        /// </summary>
        Sensor             = 3,

        /// <summary>
        /// Something that only listens: a protocol monitor, a frame capture.
        /// Given a node identifier so that the coordinator knows it is there,
        /// and no expectation that it ever uses an opportunity.
        /// </summary>
        Monitor            = 4

    }


    /// <summary>
    /// What a sensor reading measures, and therefore what its value means.
    /// </summary>
    /// <remarks>
    /// One byte on the wire, so that a sensor of a kind this library has not
    /// heard of still arrives as a reading - with a number nothing here knows
    /// how to read, which is the honest state of affairs.
    /// </remarks>
    public enum SensorKind : Byte
    {

        /// <summary>Degrees Celsius, in hundredths.</summary>
        Temperature  = 1,

        /// <summary>Amperes, in hundredths.</summary>
        Current      = 2,

        /// <summary>Volts, in hundredths.</summary>
        Voltage      = 3,

        /// <summary>Percent relative humidity, in hundredths.</summary>
        Humidity     = 4

    }


    /// <summary>
    /// What a sensor says about its own reading, beside the number.
    /// </summary>
    /// <remarks>
    /// The sensor's opinion, not the coordinator's: a smart sensor knows its
    /// own limits and says when it is past them, and a coordinator that
    /// disagreed with it should say so in its own log rather than overrule
    /// it silently. Flags, so that several can be true at once.
    /// </remarks>
    [Flags]
    public enum SensorFlags : Byte
    {

        None            = 0,

        /// <summary>The sensor believes this reading is above its warning limit.</summary>
        Warning         = 1,

        /// <summary>The sensor believes this reading is above its alarm limit.</summary>
        Alarm           = 2,

        /// <summary>The sensor is not sure of this reading - a self-test failed, a wire is loose.</summary>
        Unreliable      = 4,

        /// <summary>The sensor has not settled since it was powered.</summary>
        WarmingUp       = 8

    }

}
