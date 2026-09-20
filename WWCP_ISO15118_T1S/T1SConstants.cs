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
using System.Net.Sockets;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

#endregion

namespace cloud.charging.open.protocols.ISO15118.T1S
{

    /// <summary>
    /// What the 10BASE-T1S emulation agrees on: the EtherType it speaks, the
    /// node identifiers PLCA hands out, and the timings of a cycle.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 10BASE-T1S (IEEE 802.3cg-2019, Clause 147) is the multidrop Ethernet
    /// the Megawatt Charging System runs ISO 15118-20 over: one twisted pair,
    /// up to eight nodes on it, 10 Mbit/s, half duplex. What keeps eight nodes
    /// off each other's frames is PLCA (Clause 148, Physical Layer Collision
    /// Avoidance): the node with identifier 0 - the coordinator, which here is
    /// the charging station - sends a BEACON, and after it every node in turn
    /// is given a transmit opportunity in which it may send one frame or let
    /// the opportunity pass. Then the next BEACON, and so on, forever.
    /// </para>
    /// <para>
    /// That is the whole of what makes this bus different from the powerline
    /// one that CCS uses, and it is exactly the property a charging station
    /// wants for the things beside the vehicle that a megawatt cable needs
    /// watching by: a temperature sensor in each pin of the coupler is a node
    /// on the same wire, asked every cycle whether it has anything to say.
    /// </para>
    /// <para>
    /// The numbers here are the emulation's and are named as such. The real
    /// PHY's transmit-opportunity timer is 32 bit times - 3.2 µs - and a BEACON
    /// is a signal below the MAC that no frame ever sees; over UDP on a laptop
    /// both have to become frames, and both have to wait for a process to be
    /// scheduled. The shape of the cycle is the standard's; the milliseconds
    /// are not.
    /// </para>
    /// </remarks>
    public static class T1SConstants
    {

        #region The wire

        /// <summary>
        /// The EtherType of everything this emulation puts on the medium.
        /// </summary>
        /// <remarks>
        /// 0x88B5 is one of the two EtherTypes IEEE 802 reserves for local
        /// experimental use, which is what a bench protocol that stands in for
        /// PHY signalling is. It is deliberately not 0x88E1 - that is HomePlug,
        /// and a SLAC listener that saw a BEACON in it would try to decode a
        /// management message out of it.
        /// </remarks>
        public const UInt16  EtherType                 = 0x88B5;

        /// <summary>
        /// The most bytes a frame may carry after its Ethernet header. Well
        /// under one UDP datagram, and far more than any message here needs.
        /// </summary>
        public const Int32   MaxPayloadLength          = 1500;

        /// <summary>
        /// The longest a node's name may be, in bytes of UTF-8.
        /// </summary>
        public const Int32   MaxNameLength             = 32;

        #endregion

        #region Node identifiers

        /// <summary>
        /// The coordinator's identifier. Clause 148 gives it to exactly one
        /// node, and here that node is the charging station.
        /// </summary>
        public const Byte    CoordinatorNodeId         = 0;

        /// <summary>
        /// The first identifier handed to a node that joined.
        /// </summary>
        public const Byte    FirstFollowerNodeId       = 1;

        /// <summary>
        /// The last identifier a node may have. Clause 148 allows 254; eight is
        /// what the PHY is specified for, and what a bench will have.
        /// </summary>
        public const Byte    LastFollowerNodeId        = 254;

        /// <summary>
        /// The identifier of a node that has none yet - Clause 148's value for
        /// an unconfigured node - and, in the emulation, the identifier of the
        /// transmit opportunity in which such a node may ask for one.
        /// </summary>
        public const Byte    UnassignedNodeId          = 255;

        /// <summary>
        /// How many transmit opportunities a node gets per cycle unless it is
        /// given more. One is what the standard gives everybody.
        /// </summary>
        public const Byte    DefaultWeight             = 1;

        /// <summary>
        /// The most transmit opportunities per cycle a node may ask for. Enough
        /// to put a vehicle between every other node on a bus of eight.
        /// </summary>
        public const Byte    MaxWeight                 = 8;

        #endregion

        #region The emulated cycle

        /// <summary>
        /// How long the coordinator waits for a node to use its transmit
        /// opportunity before treating it as passed.
        /// </summary>
        /// <remarks>
        /// The real timer is 3.2 µs. This is the time it takes a process on
        /// the same machine to be handed a datagram, decide, and answer -
        /// generous, so that a node scheduled late is not counted as absent.
        /// </remarks>
        public static readonly TimeSpan  DefaultTransmitOpportunityTimeout  = TimeSpan.FromMilliseconds(50);

        /// <summary>
        /// How long the discovery opportunity stays open for a node without an
        /// identifier to ask for one.
        /// </summary>
        public static readonly TimeSpan  DefaultDiscoveryWindow             = TimeSpan.FromMilliseconds(50);

        /// <summary>
        /// The pause after a cycle before the next BEACON.
        /// </summary>
        /// <remarks>
        /// Nothing in Clause 148 pauses; the next BEACON follows the last
        /// opportunity at once. On a bench a cycle every few milliseconds is a
        /// log nobody can read, and a bus that spins a core when everybody is
        /// silent, so there is a gap - and it is what sets how often a sensor
        /// is asked: once per cycle.
        /// </remarks>
        public static readonly TimeSpan  DefaultCycleGap                    = TimeSpan.FromMilliseconds(200);

        /// <summary>
        /// How many consecutive cycles a node may miss before the coordinator
        /// gives it up for lost.
        /// </summary>
        public const Int32   DefaultLostAfterMissedCycles  = 5;

        /// <summary>
        /// How long a follower waits without a BEACON before it decides the
        /// coordinator is gone. Clause 148 has the same timer, at 20 bit times
        /// past where the BEACON was due.
        /// </summary>
        public static readonly TimeSpan  DefaultBeaconTimeout               = TimeSpan.FromSeconds(3);

        /// <summary>
        /// The longest a joining node backs off, in cycles, when it asked and
        /// was not answered - two nodes asking in the same discovery
        /// opportunity is the one collision this bus can still have.
        /// </summary>
        public const Int32   MaxJoinBackoffCycles          = 4;

        #endregion

        #region The medium

        /// <summary>
        /// Where the emulated medium lives unless somebody says otherwise: an
        /// administratively scoped IPv4 multicast group, on a port that is
        /// nobody's.
        /// </summary>
        /// <remarks>
        /// Multicast rather than a list of peers, because that is what a bus
        /// is: every node hears every frame, and a node that joins needs to
        /// know nothing about who else is there. IPv4 rather than IPv6
        /// link-local, because a bench on a laptop has to work on the
        /// interface the operating system picks, and IPv6 multicast on a
        /// loopback or a virtual interface is the thing three operating
        /// systems each do differently.
        /// </remarks>
        public static readonly IPEndPoint  DefaultMulticastEndpoint  = new (IPAddress.Parse("239.151.18.1"), 16118);

        #endregion

        #region TryParseBus(Text, out Bus)

        /// <summary>
        /// The emulated medium written as a group and a port, the way a
        /// configuration file carries it: <c>239.151.18.1:16118</c>.
        /// </summary>
        /// <remarks>
        /// One rule in one place, because both ends of the cable read it out
        /// of their own configuration and a group that one of them accepted
        /// and the other refused would be a bench that half works. IPv4
        /// multicast only: that is what the emulated medium is, and an address
        /// that is not one is a bus nobody would ever hear a BEACON on.
        /// </remarks>
        public static Boolean TryParseBus(String?                                Text,
                                          [NotNullWhen(true)] out IPEndPoint?    Bus)
        {

            Bus = null;

            if (Text is null ||
                !IPEndPoint.TryParse(Text.Trim(), out var endpoint) ||
                endpoint.Port == 0 ||
                endpoint.Address.AddressFamily != AddressFamily.InterNetwork)
            {
                return false;
            }

            var first = endpoint.Address.GetAddressBytes()[0];

            if (first < 224 || first > 239)
                return false;

            Bus = endpoint;
            return true;

        }

        #endregion

        #region RandomLocalMac()

        /// <summary>
        /// A locally administered, unicast MAC address for a node of the
        /// emulation, with a prefix that says which emulation it came from.
        /// </summary>
        /// <remarks>
        /// 02:71:50 - locally administered, "T1S0" as near as hexadecimal
        /// allows - so that a frame capture says at a glance which addresses
        /// are this bus and which are the powerline one's.
        /// </remarks>
        public static MACAddress RandomLocalMac()
        {

            var bytes = new Byte[6];

            RandomNumberGenerator.Fill(bytes.AsSpan(3));

            bytes[0] = 0x02;
            bytes[1] = 0x71;
            bytes[2] = 0x50;

            return MACAddress.Parse(String.Join(':', bytes.Select(b => b.ToString("X2"))));

        }

        #endregion

    }

}
