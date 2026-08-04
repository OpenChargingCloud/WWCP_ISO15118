/*
 * Copyright (c) 2021-2025 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP ISO/IEC 15118 <https://github.com/OpenChargingCloud/WWCP_ISO15118>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using NUnit.Framework;
using cloud.charging.open.protocols.ISO15118_20.AC.Generated;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{
    /// <summary>
    /// Substitution members are dispatched by type pattern, which matches derived instances — so a
    /// type the schema set does not know can take its nearest ancestor's branch and be written with
    /// that member's event code and encoder. The generated records are left unsealed exactly where
    /// that is possible, because the schema derives from them; nothing stops a consumer doing the
    /// same.
    /// </summary>
    /// <remarks>
    /// Every derived type in a schema set is itself a member, so this is unreachable from generated
    /// code — only from outside, which is why it is tested from outside. The Kotlin back end carries
    /// the same guard, checked by <c>SubstitutionGuardTest</c> in <c>exi-iso20-ac</c>.
    /// </remarks>
    [TestFixture]
    public class SubstitutionGuardTests
    {
        private static RationalNumberType Rational => new(Exponent: 0, Value: 1);

        private static AC_CPDReqEnergyTransferModeType Mode() =>
            new(EVMaximumChargePower   : Rational,
                EVMaximumChargePower_L2: null,
                EVMaximumChargePower_L3: null,
                EVMinimumChargePower   : Rational,
                EVMinimumChargePower_L2: null,
                EVMinimumChargePower_L3: null);

        /// <summary>What a consumer might write: a member type extended outside the schema.</summary>
        private sealed record HomeGrownMode()
            : AC_CPDReqEnergyTransferModeType(Rational, null, null, Rational, null, null);

        private static AC_ChargeParameterDiscoveryReq Request(AC_CPDReqEnergyTransferModeType mode) =>
            new(Header: new MessageHeaderType(SessionID: new byte[8], TimeStamp: 1_700_000_000, Signature: null),
                AC_CPDReqEnergyTransferMode: mode);

        [Test]
        public void AMemberTypeEncodes()
        {
            // The guard must not stand in the way of the thing it guards.
            Span<byte> buf = stackalloc byte[1024];
            Assert.That(Request(Mode()).TryEncode(buf, out var written), Is.True);
            Assert.That(written, Is.GreaterThan(0));
        }

        [Test]
        public void ATypeOutsideTheSubstitutionGroupIsRefused()
        {
            // Without the guard this would encode as AC_CPDReqEnergyTransferMode and say nothing.
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                var buf = new byte[1024];
                Request(new HomeGrownMode()).TryEncode(buf, out _);
            });

            Assert.That(ex!.Message,
                        Is.EqualTo("AC_CPDReqEnergyTransferMode: HomeGrownMode is not a substitution member"),
                        "the message must name the field and the offending type");
        }

        [Test]
        public void ARealMemberSubtypeStillTakesItsOwnBranch()
        {
            // BPT_ is a member and derives from the head: the guard must not mistake it for an
            // intruder, and it must keep its own event code rather than the head's.
            var bpt = new BPT_AC_CPDReqEnergyTransferModeType(
                EVMaximumChargePower      : Rational, EVMaximumChargePower_L2   : null,
                EVMaximumChargePower_L3   : null,     EVMinimumChargePower      : Rational,
                EVMinimumChargePower_L2   : null,     EVMinimumChargePower_L3   : null,
                EVMaximumDischargePower   : Rational, EVMaximumDischargePower_L2: null,
                EVMaximumDischargePower_L3: null,     EVMinimumDischargePower   : Rational,
                EVMinimumDischargePower_L2: null,     EVMinimumDischargePower_L3: null);

            var bptBuf  = new byte[1024];
            var headBuf = new byte[1024];
            Assert.That(Request(bpt).TryEncode(bptBuf, out var bptLen), Is.True);
            Assert.That(Request(Mode()).TryEncode(headBuf, out var headLen), Is.True);

            Assert.That(bptBuf.AsSpan(0, bptLen).SequenceEqual(headBuf.AsSpan(0, headLen)), Is.False,
                        "the two members must not encode identically");
        }
    }
}
