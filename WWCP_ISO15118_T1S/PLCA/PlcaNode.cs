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

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

using cloud.charging.open.protocols.ISO15118.T1S.Messages;

#endregion

namespace cloud.charging.open.protocols.ISO15118.T1S.PLCA
{

    /// <summary>
    /// One node as the coordinator knows it: who it is, what it was given,
    /// and how it has been behaving.
    /// </summary>
    /// <remarks>
    /// The coordinator's record, written by the coordinator and read by
    /// everybody else; the setters are internal for that reason. A node's own
    /// view of itself is <see cref="PlcaFollower"/>, and the two can disagree
    /// - a node that thinks it is attached and a coordinator that gave it up
    /// for lost three cycles ago - which is exactly the state a bench exists
    /// to make visible.
    /// </remarks>
    public sealed class PlcaNode
    {

        #region Properties

        /// <summary>The identifier the coordinator handed out.</summary>
        public Byte            NodeId              { get; }

        /// <summary>The node's address on the medium.</summary>
        public MACAddress      Mac                 { get; }

        /// <summary>What it said it was when it joined.</summary>
        public T1SNodeRole     Role                { get; }

        /// <summary>What to call it.</summary>
        public String          Name                { get; internal set; }

        /// <summary>How many transmit opportunities per cycle it gets.</summary>
        public Byte            Weight              { get; }

        /// <summary>When it was given its identifier.</summary>
        public DateTimeOffset  JoinedAt            { get; }

        /// <summary>When it last used or passed an opportunity.</summary>
        public DateTimeOffset  LastSeen            { get; internal set; }

        /// <summary>
        /// How many cycles in a row it has let pass in silence - not yielded,
        /// said nothing at all. Reset by any frame from it.
        /// </summary>
        public Int32           MissedCycles        { get; internal set; }

        /// <summary>Frames it has sent in its opportunities, yields not counted.</summary>
        public Int64           FramesReceived      { get; internal set; }

        /// <summary>Opportunities it passed.</summary>
        public Int64           Yields              { get; internal set; }

        /// <summary>The last reading it sent, where it is a sensor.</summary>
        public SensorReading?  LastReading         { get; internal set; }

        /// <summary>When that reading arrived.</summary>
        public DateTimeOffset? LastReadingAt       { get; internal set; }

        #endregion

        #region Constructor(s)

        internal PlcaNode(Byte            NodeId,
                          MACAddress      Mac,
                          T1SNodeRole     Role,
                          String          Name,
                          Byte            Weight,
                          DateTimeOffset  JoinedAt)
        {

            this.NodeId    = NodeId;
            this.Mac       = Mac;
            this.Role      = Role;
            this.Name      = Name;
            this.Weight    = Weight;
            this.JoinedAt  = JoinedAt;
            this.LastSeen  = JoinedAt;

        }

        #endregion


        #region (override) ToString()

        public override String ToString()
            => $"node {NodeId} '{Name}' ({Role}, {Mac}, weight {Weight})";

        #endregion

    }

}
