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
using cloud.charging.open.protocols.ISO15118_2;
using cloud.charging.open.protocols.ISO15118_2.Generated;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{
    /// <summary>Rounding / range behaviour of the <see cref="PhysicalValue"/> helper.</summary>
    [TestFixture]
    public class PhysicalValueTests
    {
        [TestCase("400", (sbyte)0, (short)400)]      // fits at multiplier 0
        [TestCase("0.001", (sbyte)-3, (short)1)]     // finest scale
        [TestCase("230.5", (sbyte)-1, (short)2305)]  // one fractional digit -> multiplier -1
        [TestCase("40000", (sbyte)1, (short)4000)]   // too big for short at 0 -> multiplier 1
        [TestCase("400000", (sbyte)2, (short)4000)]  // multiplier 2
        [TestCase("-12.34", (sbyte)-2, (short)-1234)]// negative, two fractional digits
        public void Of_PicksFinestFittingMultiplier(string amount, sbyte multiplier, short value)
        {
            var pv = PhysicalValue.Of(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), UnitSymbol.V);
            Assert.That(pv.Multiplier, Is.EqualTo(multiplier));
            Assert.That(pv.Value, Is.EqualTo(value));
            Assert.That(pv.Unit, Is.EqualTo(UnitSymbol.V));
        }

        [Test]
        public void ToDecimal_IsTheInverseOfExactlyRepresentableAmounts()
        {
            foreach (var amount in new[] { 400m, 0.001m, 230.5m, 40000m, -12.34m, 0m })
                Assert.That(PhysicalValue.Of(amount, UnitSymbol.A).ToDecimal(), Is.EqualTo(amount),
                    $"round-trip failed for {amount}");
        }

        [Test]
        public void Of_RoundsBelowTheRepresentableGranularity_HalfToEven()
        {
            // Granularity is 10^-3; 0.0005 rounds half-to-even -> 0, 0.0015 -> 0.002.
            Assert.That(PhysicalValue.Of(0.0005m, UnitSymbol.V).ToDecimal(), Is.EqualTo(0m));
            Assert.That(PhysicalValue.Of(0.0015m, UnitSymbol.V).ToDecimal(), Is.EqualTo(0.002m));
        }

        [Test]
        public void Of_ThrowsWhenTooLargeForAnyMultiplier()
        {
            // 32767 * 10^3 is the max; beyond that no multiplier in [-3,3] fits.
            Assert.That(() => PhysicalValue.Of(33_000_000m, UnitSymbol.W), Throws.TypeOf<System.OverflowException>());
        }
    }
}
