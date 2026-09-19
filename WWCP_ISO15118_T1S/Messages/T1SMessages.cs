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

namespace cloud.charging.open.protocols.ISO15118.T1S.Messages
{

    /// <summary>
    /// The first byte of every payload: what kind of frame this is.
    /// </summary>
    /// <remarks>
    /// Three families. The PLCA ones - BEACON, transmit opportunity, yield -
    /// stand in for signalling that the real PHY does below the MAC and that
    /// no frame ever carries; over UDP they have to be frames, and they are
    /// kept apart from everything else by their numbers so that a capture
    /// reads like the cycle it records. The membership ones exist because a
    /// bench bus has nodes that come and go, where the standard's are
    /// configured by hand. And the data ones are what the bus is for.
    /// </remarks>
    public enum T1SMessageType : Byte
    {

        // PLCA: the cycle itself
        Beacon               = 0x01,
        TransmitOpportunity  = 0x02,
        Yield                = 0x03,

        // Membership: who is on the bus
        Join                 = 0x10,
        Assign               = 0x11,
        Announce             = 0x12,
        Leave                = 0x13,

        // Data: what the nodes have to say
        SensorReading        = 0x20,
        Data                 = 0x21

    }


    /// <summary>
    /// One frame's worth of meaning, decoded from or about to be encoded into
    /// the payload of an <see cref="EthernetFrame"/>.
    /// </summary>
    public interface IT1SMessage
    {

        /// <summary>What kind of frame this is.</summary>
        T1SMessageType  Type  { get; }

    }


    #region PLCA

    /// <summary>
    /// The coordinator's BEACON: a cycle begins.
    /// </summary>
    /// <param name="Cycle">Which cycle, counting from the coordinator's start. The standard's BEACON carries nothing; this carries a number so that a log can be read.</param>
    /// <param name="NodeCount">How many transmit opportunities this cycle has, the discovery one included.</param>
    public sealed record Beacon(UInt32  Cycle,
                                Byte    NodeCount) : IT1SMessage
    {
        public T1SMessageType Type => T1SMessageType.Beacon;
    }


    /// <summary>
    /// The coordinator's grant: one node may send one frame now.
    /// </summary>
    /// <param name="Cycle">The cycle this opportunity belongs to.</param>
    /// <param name="Slot">Which opportunity of the cycle, counting from 0.</param>
    /// <param name="NodeId">Whose it is - or <see cref="T1SConstants.UnassignedNodeId"/> for the discovery opportunity, in which any node without an identifier may ask for one.</param>
    public sealed record TransmitOpportunity(UInt32  Cycle,
                                             Byte    Slot,
                                             Byte    NodeId) : IT1SMessage
    {

        public T1SMessageType Type => T1SMessageType.TransmitOpportunity;

        /// <summary>Whether this is the opportunity for nodes that have no identifier yet.</summary>
        public Boolean IsDiscovery
            => NodeId == T1SConstants.UnassignedNodeId;

    }


    /// <summary>
    /// A node's "nothing to say" - said out loud rather than by silence.
    /// </summary>
    /// <remarks>
    /// On the real PHY a node with nothing to send lets its opportunity
    /// expire, and the coordinator moves on after 32 bit times. Over UDP a
    /// silence is fifty milliseconds nobody can tell from a node that has
    /// gone, so an idle node says so instead and the cycle stays quick. The
    /// coordinator still times out, for the nodes that really have gone.
    /// </remarks>
    /// <param name="Cycle">The cycle whose opportunity is being passed.</param>
    /// <param name="NodeId">Who is passing it.</param>
    public sealed record Yield(UInt32  Cycle,
                               Byte    NodeId) : IT1SMessage
    {
        public T1SMessageType Type => T1SMessageType.Yield;
    }

    #endregion

    #region Membership

    /// <summary>
    /// A node without an identifier asking for one, in the discovery
    /// opportunity.
    /// </summary>
    /// <remarks>
    /// The nonce is what ties the answer to the question. Two nodes can ask
    /// in the same opportunity - the one collision this bus can still have -
    /// and the coordinator answers one of them by MAC address; the nonce is
    /// how a node whose MAC somebody else is also using, which a bench full
    /// of generated addresses makes possible, still knows whether it was
    /// the one answered.
    /// </remarks>
    /// <param name="Role">What the node is.</param>
    /// <param name="Nonce">A number the node made up for this request.</param>
    /// <param name="Weight">How many transmit opportunities per cycle it asks for. The coordinator may give fewer.</param>
    /// <param name="Name">What to call it in a log, up to <see cref="T1SConstants.MaxNameLength"/> bytes.</param>
    public sealed record Join(T1SNodeRole  Role,
                              UInt32       Nonce,
                              Byte         Weight,
                              String       Name) : IT1SMessage
    {
        public T1SMessageType Type => T1SMessageType.Join;
    }


    /// <summary>
    /// The coordinator's answer to a <see cref="Join"/>: this is your
    /// identifier, and this is what you get.
    /// </summary>
    /// <param name="Nonce">The request this answers.</param>
    /// <param name="NodeId">The identifier handed out.</param>
    /// <param name="Role">The role as the coordinator recorded it.</param>
    /// <param name="Weight">How many opportunities per cycle the node actually gets.</param>
    public sealed record Assign(UInt32       Nonce,
                                Byte         NodeId,
                                T1SNodeRole  Role,
                                Byte         Weight) : IT1SMessage
    {
        public T1SMessageType Type => T1SMessageType.Assign;
    }


    /// <summary>
    /// A node saying who it is, in an opportunity it had nothing else for.
    /// </summary>
    /// <remarks>
    /// Sent once after being assigned, so that the coordinator's registry
    /// has a name beside every identifier, and again whenever a node wants
    /// to be sure it is still known - after a coordinator restart, say.
    /// </remarks>
    /// <param name="NodeId">The sender's identifier.</param>
    /// <param name="Role">What it is.</param>
    /// <param name="Name">What to call it.</param>
    public sealed record Announce(Byte         NodeId,
                                  T1SNodeRole  Role,
                                  String       Name) : IT1SMessage
    {
        public T1SMessageType Type => T1SMessageType.Announce;
    }


    /// <summary>
    /// A node going away on purpose, so that the coordinator need not wait
    /// five cycles to find out.
    /// </summary>
    /// <param name="NodeId">Who is leaving.</param>
    public sealed record Leave(Byte NodeId) : IT1SMessage
    {
        public T1SMessageType Type => T1SMessageType.Leave;
    }

    #endregion

    #region Data

    /// <summary>
    /// One measurement from one sensor.
    /// </summary>
    /// <remarks>
    /// The value is a signed sixteen-bit number of hundredths, whatever the
    /// kind: −327.68 to 327.67 of the unit. Enough for a coupler pin at 250 °C
    /// and a bus bar at 3 000 A alike, in two bytes, without a floating-point
    /// format that every node on a ten-megabit wire would have to agree on.
    /// </remarks>
    /// <param name="NodeId">Which sensor.</param>
    /// <param name="Kind">What was measured.</param>
    /// <param name="Sequence">A counter the sensor increments per reading, so that a repeated frame is seen as one.</param>
    /// <param name="Value">The reading, in hundredths of the kind's unit.</param>
    /// <param name="Flags">The sensor's own opinion of the reading.</param>
    public sealed record SensorReading(Byte         NodeId,
                                       SensorKind   Kind,
                                       UInt16       Sequence,
                                       Int16        Value,
                                       SensorFlags  Flags) : IT1SMessage
    {

        public T1SMessageType Type => T1SMessageType.SensorReading;

        /// <summary>The value in the kind's unit, e.g. degrees Celsius.</summary>
        public Double AsDouble
            => Value / 100.0;

        /// <summary>A reading of the given temperature.</summary>
        public static SensorReading Temperature(Byte         NodeId,
                                                UInt16       Sequence,
                                                Double       Celsius,
                                                SensorFlags  Flags   = SensorFlags.None)

            => new (NodeId,
                    SensorKind.Temperature,
                    Sequence,
                    (Int16) Math.Clamp(Math.Round(Celsius * 100), Int16.MinValue, Int16.MaxValue),
                    Flags);

    }


    /// <summary>
    /// Anything else: bytes the bus carries and does not look into.
    /// </summary>
    /// <remarks>
    /// The escape hatch that keeps the emulation honest about what it is - a
    /// medium. A vehicle that wants to run an IPv6 packet over it puts it
    /// here, exactly as the real bus would carry one, and the coordinator
    /// counts it as the node using its opportunity and nothing more.
    /// </remarks>
    /// <param name="Payload">The bytes.</param>
    public sealed record Data(Byte[] Payload) : IT1SMessage
    {
        public T1SMessageType Type => T1SMessageType.Data;
    }

    #endregion

}
