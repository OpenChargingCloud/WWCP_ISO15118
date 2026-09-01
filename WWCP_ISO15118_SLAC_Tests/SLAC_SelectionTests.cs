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

using cloud.charging.open.protocols.ISO15118.SLAC.Messages;
using cloud.charging.open.protocols.ISO15118.SLAC.Selection;

#endregion

namespace cloud.charging.open.protocols.ISO15118.SLAC.Tests
{

    /// <summary>
    /// EVSE selection: which station the EV decides it is actually plugged into.
    ///
    /// This is the security-relevant decision in the whole SLAC exchange. On a shared supply
    /// line — a parking deck, a row of wallboxes — several EVSEs hear the EV's sounding and
    /// answer. Per ISO 15118-3 the EV takes the one it hears loudest, i.e. the lowest average
    /// attenuation, because signal strength is the only proxy for "physically connected" that
    /// PLC offers. Pick wrong and the EV authorises a charging session at somebody else's
    /// station.
    /// </summary>
    [TestFixture]
    public class SLAC_SelectionTests
    {

        #region (private static) Data

        private static RunId SampleRunId()
            => new ([ 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 ]);

        private static MACAddress Mac(Byte Tail)
            => MACAddress.From([ 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, Tail ]);

        /// <summary>
        /// A candidate whose attenuation profile is flat at <paramref name="Attenuation"/> dB
        /// across all 58 carrier groups, which makes the average trivially predictable.
        /// </summary>
        private static EVSECandidate Candidate(Byte Tail, Byte? Attenuation)
        {

            var mac      = Mac(Tail);
            var parmCnf  = new SLACParmCnf(mac, 10, 6, 0x01, MACAddress.Zero, SampleRunId());

            AttenCharInd? attenChar = null;

            if (Attenuation is not null)
            {

                var profile = new Byte[SLACConstants.NumAttenGroups];
                Array.Fill(profile, Attenuation.Value);

                attenChar = new AttenCharInd(
                                mac,
                                SampleRunId(),
                                new Byte[17],
                                new Byte[17],
                                10,
                                profile
                            );

            }

            return new EVSECandidate(mac, parmCnf, attenChar);

        }

        #endregion


        #region AverageAttenuation

        [Test]
        public void AverageAttenuation_IsTheMeanAcrossTheCarrierGroups()
        {

            var candidate = new EVSECandidate(
                                Mac(0x01),
                                new SLACParmCnf(Mac(0x01), 10, 6, 0x01, MACAddress.Zero, SampleRunId()),
                                new AttenCharInd(Mac(0x01), SampleRunId(), new Byte[17], new Byte[17], 10,
                                                 [ 10, 20, 30, 40 ])
                            );

            Assert.Multiple(() => {
                Assert.That(candidate.AverageAttenuation,     Is.EqualTo(25.0));
                Assert.That(candidate.HasAttenuationProfile,  Is.True);
                Assert.That(candidate.AttenuationProfile,     Is.EqualTo(new Byte[] { 10, 20, 30, 40 }));
            });

        }

        [Test]
        public void AverageAttenuation_IsNullWithoutAnAttenCharInd()
        {

            var candidate = Candidate(0x01, null);

            Assert.Multiple(() => {
                Assert.That(candidate.AverageAttenuation,     Is.Null);
                Assert.That(candidate.HasAttenuationProfile,  Is.False);
                Assert.That(candidate.AttenuationProfile,     Is.Null);
                Assert.That(candidate.EvseId,                 Is.Null);
            });

        }

        [Test]
        public void AverageAttenuation_IsNullForAnEmptyProfile()
        {

            var candidate = new EVSECandidate(
                                Mac(0x01),
                                new SLACParmCnf(Mac(0x01), 10, 6, 0x01, MACAddress.Zero, SampleRunId()),
                                new AttenCharInd(Mac(0x01), SampleRunId(), new Byte[17], new Byte[17], 10, [])
                            );

            Assert.Multiple(() => {
                Assert.That(candidate.AverageAttenuation,     Is.Null);
                Assert.That(candidate.HasAttenuationProfile,  Is.True);   // the IND arrived …
                Assert.That(candidate.AttenuationProfile,     Is.Empty);  // … it just carried nothing
            });

        }

        [Test]
        public void AverageAttenuation_DoesNotOverflowOnAFullProfileOfMaximumValues()
        {

            var profile = new Byte[SLACConstants.NumAttenGroups];
            Array.Fill(profile, Byte.MaxValue);

            var candidate = new EVSECandidate(
                                Mac(0x01),
                                new SLACParmCnf(Mac(0x01), 10, 6, 0x01, MACAddress.Zero, SampleRunId()),
                                new AttenCharInd(Mac(0x01), SampleRunId(), new Byte[17], new Byte[17], 10, profile)
                            );

            Assert.That(candidate.AverageAttenuation, Is.EqualTo(255.0));

        }

        [Test]
        public void EvseId_ComesFromTheAttenCharInd()
        {

            var evseId     = new Byte[17];
            evseId[0]      = 0x44;

            var candidate  = new EVSECandidate(
                                 Mac(0x01),
                                 new SLACParmCnf(Mac(0x01), 10, 6, 0x01, MACAddress.Zero, SampleRunId()),
                                 new AttenCharInd(Mac(0x01), SampleRunId(), evseId, new Byte[17], 10,
                                                  new Byte[SLACConstants.NumAttenGroups])
                             );

            Assert.That(candidate.EvseId, Is.EqualTo(evseId));

        }

        #endregion

        #region LowestAverageAttenuationSelector

        [Test]
        public void Select_PicksTheLowestAverageAttenuation()
        {

            var quiet     = Candidate(0x02, 12);
            var candidates = new[] {
                                Candidate(0x01, 40),
                                quiet,
                                Candidate(0x03, 31)
                            };

            var winner = new LowestAverageAttenuationSelector().Select(candidates);

            Assert.That(winner, Is.EqualTo(quiet));

        }

        [Test]
        public void Select_PicksTheLowest_RegardlessOfArrivalOrder()
        {

            var quiet    = Candidate(0x02, 12);
            var selector = new LowestAverageAttenuationSelector();

            Assert.Multiple(() => {
                Assert.That(selector.Select([ quiet, Candidate(0x01, 40), Candidate(0x03, 31) ]), Is.EqualTo(quiet));
                Assert.That(selector.Select([ Candidate(0x01, 40), Candidate(0x03, 31), quiet ]), Is.EqualTo(quiet));
                Assert.That(selector.Select([ Candidate(0x03, 31), quiet, Candidate(0x01, 40) ]), Is.EqualTo(quiet));
            });

        }

        [Test]
        public void Select_IgnoresCandidatesWithoutAnAttenuationProfile()
        {

            var withProfile = Candidate(0x02, 60);

            // The silent one would win on any ranking that treated "no profile" as zero.
            var winner = new LowestAverageAttenuationSelector().Select([
                             Candidate(0x01, null),
                             withProfile,
                             Candidate(0x03, null)
                         ]);

            Assert.That(winner, Is.EqualTo(withProfile));

        }

        [Test]
        public void Select_ReturnsNullWhenNoCandidateHasAProfile()
        {

            var winner = new LowestAverageAttenuationSelector().Select([
                             Candidate(0x01, null),
                             Candidate(0x02, null)
                         ]);

            Assert.That(winner, Is.Null);

        }

        [Test]
        public void Select_ReturnsNullOnAnEmptyCandidateList()
        {

            Assert.That(new LowestAverageAttenuationSelector().Select([]), Is.Null);

        }

        [Test]
        public void Select_OfASingleCandidate_ReturnsIt()
        {

            var only = Candidate(0x01, 200);

            Assert.That(new LowestAverageAttenuationSelector().Select([ only ]), Is.EqualTo(only));

        }

        /// <summary>
        /// Ties keep the first candidate, because the comparison is strictly less-than. Two
        /// EVSEs reporting the identical average is not a physically meaningful situation —
        /// but the tie-break has to be deterministic, or the EV picks differently on retry.
        /// </summary>
        [Test]
        public void Select_OnATie_KeepsTheFirstCandidate()
        {

            var first   = Candidate(0x01, 25);
            var second  = Candidate(0x02, 25);

            Assert.Multiple(() => {
                Assert.That(new LowestAverageAttenuationSelector().Select([ first, second ]), Is.EqualTo(first));
                Assert.That(new LowestAverageAttenuationSelector().Select([ second, first ]), Is.EqualTo(second));
            });

        }

        [Test]
        public void Select_DistinguishesProfilesThatDifferOnlyInTheirMean()
        {

            // Same total spread, different mean: 0/50 averages 25, 24/25 averages 24.5.
            var spread = new EVSECandidate(
                             Mac(0x01),
                             new SLACParmCnf(Mac(0x01), 10, 6, 0x01, MACAddress.Zero, SampleRunId()),
                             new AttenCharInd(Mac(0x01), SampleRunId(), new Byte[17], new Byte[17], 10, [ 0, 50 ])
                         );

            var tight  = new EVSECandidate(
                             Mac(0x02),
                             new SLACParmCnf(Mac(0x02), 10, 6, 0x01, MACAddress.Zero, SampleRunId()),
                             new AttenCharInd(Mac(0x02), SampleRunId(), new Byte[17], new Byte[17], 10, [ 24, 25 ])
                         );

            Assert.Multiple(() => {
                Assert.That(spread.AverageAttenuation,                                          Is.EqualTo(25.0));
                Assert.That(tight.AverageAttenuation,                                           Is.EqualTo(24.5));
                Assert.That(new LowestAverageAttenuationSelector().Select([ spread, tight ]),   Is.EqualTo(tight));
            });

        }

        #endregion

        #region EVSLACMatchingResult

        [Test]
        public void MatchingResult_KeepsEveryCandidate_NotJustTheWinner()
        {

            var winner      = Candidate(0x02, 12);
            var candidates  = new[] { Candidate(0x01, 40), winner, Candidate(0x03, null) };

            var result      = new EVSLACMatchingResult(
                                  winner,
                                  new SlacMatchCnf(new Byte[17], Mac(0x01), new Byte[17], Mac(0x02), SampleRunId(),
                                                   new Byte[SLACConstants.NidLength],
                                                   new Byte[SLACConstants.NmkLength]),
                                  candidates,
                                  SampleRunId()
                              );

            Assert.Multiple(() => {
                Assert.That(result.Winner,         Is.EqualTo(winner));
                Assert.That(result.AllCandidates,  Has.Count.EqualTo(3));
                Assert.That(result.AllCandidates,  Contains.Item(winner));
                Assert.That(result.RunId,          Is.EqualTo(SampleRunId()));
                Assert.That(result.MatchCnf.Nmk,   Has.Length.EqualTo(SLACConstants.NmkLength));
            });

        }

        #endregion

    }

}
