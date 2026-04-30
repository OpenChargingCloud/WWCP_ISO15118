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

namespace cloud.charging.open.protocols.ISO15118.SLAC.Selection
{

    /// <summary>
    /// Default selector. Picks the candidate with the lowest average attenuation
    /// across all carrier groups — the EVSE that "hears the EV the loudest", i.e.
    /// the one the EV is most likely physically plugged into.
    /// </summary>
    public sealed class LowestAverageAttenuationSelector : IEVSESelector
    {
        public EVSECandidate? Select(IReadOnlyList<EVSECandidate> Candidates)
        {

            EVSECandidate? best     = null;
            var            bestAvg  = Double.MaxValue;

            foreach (var candidate in Candidates)
            {

                var avg = candidate.AverageAttenuation;

                if (avg is null)
                    continue;

                if (avg.Value < bestAvg) {
                    bestAvg  = avg.Value;
                    best     = candidate;
                }

            }

            return best;

        }
    }

}
