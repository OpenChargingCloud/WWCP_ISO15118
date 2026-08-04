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

using cloud.charging.open.protocols.ISO15118_2.Generated;

namespace cloud.charging.open.protocols.ISO15118_2
{
    /// <summary>
    /// Ergonomics for the generated <see cref="PhysicalValueType"/>, whose wire form is a scaled
    /// integer: <c>amount = Value · 10^Multiplier</c>, with <c>Multiplier ∈ [-3, 3]</c> and
    /// <c>Value ∈ short</c> (ISO 15118-2 §8.5, unitMultiplierType / PhysicalValueType).
    ///
    /// <para><b>Rounding.</b> <see cref="Of"/> picks the <em>natural</em> multiplier: the one closest to
    /// zero that still captures the amount exactly as an integer <see cref="short"/> — i.e. it goes just
    /// negative enough for the fractional digits, or just positive enough to keep the value in range.
    /// The representable granularity is <c>10^-3</c> of the unit; digits finer than that are rounded
    /// half-to-even. A magnitude too large even at multiplier 3 throws.</para>
    /// </summary>
    public static class PhysicalValue
    {
        /// <summary>Builds a <see cref="PhysicalValueType"/> from a decimal amount and unit, choosing the
        /// multiplier in [-3, 3] closest to zero whose value is an exact-as-possible <see cref="short"/>.</summary>
        public static PhysicalValueType Of(decimal amount, UnitSymbol unit)
        {
            sbyte multiplier = 0;
            decimal scaled = amount;

            // Go finer (more negative) only as far as needed to clear the fractional part, capped at -3.
            while (multiplier > -3 && scaled != Math.Truncate(scaled))
            {
                multiplier--;
                scaled = amount / Pow10(multiplier);
            }
            // Go coarser (more positive) while the value would overflow short, capped at 3.
            while (multiplier < 3 && (scaled > short.MaxValue || scaled < short.MinValue))
            {
                multiplier++;
                scaled = amount / Pow10(multiplier);
            }
            if (scaled > short.MaxValue || scaled < short.MinValue)
                throw new OverflowException(
                    $"PhysicalValue {amount} does not fit a multiplier in [-3, 3] (|value| would exceed {short.MaxValue}).");

            return new PhysicalValueType(multiplier, unit, (short)Math.Round(scaled, MidpointRounding.ToEven));
        }

        /// <summary>The exact decimal amount this physical value represents (<c>Value · 10^Multiplier</c>).</summary>
        public static decimal ToDecimal(this PhysicalValueType value) =>
            value.Value * Pow10(value.Multiplier);

        private static decimal Pow10(int exponent)
        {
            decimal p = 1m;
            for (int i = 0; i < (exponent < 0 ? -exponent : exponent); i++) p *= 10m;
            return exponent >= 0 ? p : 1m / p;
        }
    }
}
