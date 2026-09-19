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

using cloud.charging.open.protocols.ISO15118.T1S.PLCA;

#endregion

namespace cloud.charging.open.protocols.ISO15118.T1S.Tests
{

    /// <summary>
    /// The order the coordinator asks the nodes in: every node as often as
    /// its weight says, spread out, discovery last.
    /// </summary>
    [TestFixture]
    public class T1S_ScheduleTests
    {

        #region AnEmptyBusHasOnlyTheDiscoveryOpportunity()

        [Test]
        public void AnEmptyBusHasOnlyTheDiscoveryOpportunity()
        {

            var schedule = PlcaSchedule.Build([]);

            Assert.That(schedule, Is.EqualTo(new Byte[] { T1SConstants.UnassignedNodeId }));

        }

        #endregion

        #region EqualWeightsGoRoundInOrder()

        [Test]
        public void EqualWeightsGoRoundInOrder()
        {

            var schedule = PlcaSchedule.Build([ (3, 1), (1, 1), (2, 1) ]);

            Assert.That(schedule, Is.EqualTo(new Byte[] { 1, 2, 3, 255 }));

        }

        #endregion

        #region AHeavierNodeIsSpreadOutNotBunched()

        /// <summary>
        /// A vehicle of weight three between two sensors gets asked between
        /// them, not three times in a row - three opportunities in a row are
        /// one long one, and a sensor at the end would wait through all of
        /// them.
        /// </summary>
        [Test]
        public void AHeavierNodeIsSpreadOutNotBunched()
        {

            var schedule = PlcaSchedule.Build([ (1, 3), (2, 1), (3, 1) ]);

            Assert.Multiple(() => {

                Assert.That(schedule.Count,                        Is.EqualTo(6), "3 + 1 + 1 opportunities, and discovery");
                Assert.That(schedule.Count(id => id == 1),         Is.EqualTo(3));
                Assert.That(schedule.Count(id => id == 2),         Is.EqualTo(1));
                Assert.That(schedule.Count(id => id == 3),         Is.EqualTo(1));
                Assert.That(schedule[^1],                          Is.EqualTo(T1SConstants.UnassignedNodeId), "discovery last");

                // No two of the vehicle's in a row.
                for (var i = 1; i < schedule.Count - 1; i++)
                    Assert.That(schedule[i] == 1 && schedule[i - 1] == 1, Is.False, $"slots {i - 1} and {i} are both the vehicle's");

            });

            Assert.That(schedule, Is.EqualTo(new Byte[] { 1, 2, 1, 3, 1, 255 }));

        }

        #endregion

        #region AWeightOfZeroIsNotAsked()

        [Test]
        public void AWeightOfZeroIsNotAsked()
        {

            var schedule = PlcaSchedule.Build([ (1, 1), (2, 0) ]);

            Assert.That(schedule, Is.EqualTo(new Byte[] { 1, 255 }));

        }

        #endregion

        #region EveryNodeGetsItsWeightOverOneCycle()

        [Test]
        public void EveryNodeGetsItsWeightOverOneCycle()
        {

            var nodes    = new (Byte, Byte)[] { (1, 5), (2, 2), (3, 1), (4, 1), (5, 3) };
            var schedule = PlcaSchedule.Build(nodes);

            Assert.That(schedule.Count, Is.EqualTo(5 + 2 + 1 + 1 + 3 + 1));

            foreach (var (id, weight) in nodes)
                Assert.That(schedule.Count(slot => slot == id), Is.EqualTo(weight), $"node {id}");

        }

        #endregion

    }

}
