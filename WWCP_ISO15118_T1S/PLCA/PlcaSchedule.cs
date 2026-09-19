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

namespace cloud.charging.open.protocols.ISO15118.T1S.PLCA
{

    /// <summary>
    /// The order the coordinator hands out transmit opportunities in, for one
    /// cycle: every node as often as its weight says, and spread out rather
    /// than bunched up.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Clause 148 gives every node one opportunity per cycle, in order of
    /// identifier. That is fair, and fairness is not what a charging station
    /// wants: the vehicle carries the whole of ISO 15118-20 and a sensor
    /// carries one number a second. So a node has a weight, and a node of
    /// weight three appears three times in the cycle - which is how a vehicle
    /// is asked more often without any sensor being asked less than once.
    /// </para>
    /// <para>
    /// Spread out, not bunched: three opportunities in a row are one long
    /// opportunity, and a sensor at the end of the cycle would wait through
    /// all three. The arithmetic is the smooth weighted round-robin that nginx
    /// balances upstreams with - each round, every node's credit grows by its
    /// weight, the richest node is served and pays the total back - and it
    /// produces <c>EV S1 EV S2 EV</c> for weights 3/1/1 rather than
    /// <c>EV EV EV S1 S2</c>.
    /// </para>
    /// <para>
    /// The discovery opportunity comes last, once, so that a node without an
    /// identifier can get one within a cycle of arriving - and after
    /// everybody who has one, so that a burst of new nodes cannot delay a
    /// sensor that is already there.
    /// </para>
    /// </remarks>
    public static class PlcaSchedule
    {

        #region Build(Nodes)

        /// <summary>
        /// The opportunities of one cycle, in order, ending with the discovery
        /// opportunity.
        /// </summary>
        /// <param name="Nodes">Every node with an identifier, and its weight.</param>
        public static IReadOnlyList<Byte> Build(IEnumerable<(Byte NodeId, Byte Weight)> Nodes)
        {

            var nodes = Nodes.Where(node => node.Weight > 0).
                              OrderBy(node => node.NodeId).
                              ToArray();

            var slots = new List<Byte>();

            if (nodes.Length > 0)
            {

                var total   = nodes.Sum(node => (Int32) node.Weight);
                var credit  = new Int32[nodes.Length];

                for (var slot = 0; slot < total; slot++)
                {

                    var richest = 0;

                    for (var i = 0; i < nodes.Length; i++)
                    {

                        credit[i] += nodes[i].Weight;

                        if (credit[i] > credit[richest])
                            richest = i;

                    }

                    credit[richest] -= total;

                    slots.Add(nodes[richest].NodeId);

                }

            }

            slots.Add(T1SConstants.UnassignedNodeId);

            return slots;

        }

        #endregion

    }

}
