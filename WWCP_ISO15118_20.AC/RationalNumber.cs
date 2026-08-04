/*
 * Copyright (c) 2021-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

using cloud.charging.open.protocols.ISO15118.EXI;
using cloud.charging.open.protocols.ISO15118_20.AC.Generated;

namespace cloud.charging.open.protocols.ISO15118_20.AC
{
    /// <summary>Ergonomics for this assembly's <see cref="RationalNumberType"/>; math shared via
    /// <see cref="RationalNumberMath"/> (see there for rounding behaviour).</summary>
    public static class RationalNumber
    {
        public static RationalNumberType Of(decimal amount)
        {
            var (exponent, value) = RationalNumberMath.Decompose(amount);
            return new RationalNumberType(exponent, value);
        }

        public static decimal ToDecimal(this RationalNumberType value) =>
            RationalNumberMath.Compose(value.Exponent, value.Value);
    }
}
