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

#region Usings

using cloud.charging.open.protocols.ISO15118.SLAC.Messages;

#endregion

namespace cloud.charging.open.protocols.ISO15118.SLAC.Selection
{

    /// <summary>
    /// Result of an EV-side SLAC matching attempt.
    /// </summary>
    /// <param name="Winner">
    /// The EVSE that was selected by the <see cref="IEVSESelector"/> and that
    /// successfully completed the matching protocol with the EV.
    /// </param>
    /// <param name="MatchCnf">
    /// The CM_SLAC_MATCH.CNF received from <paramref name="Winner"/>, containing
    /// the network credentials (NID + NMK) the EV uses to join the AVLN.
    /// </param>
    /// <param name="AllCandidates">
    /// Every EVSE that answered CM_SLAC_PARM.REQ within the collection window,
    /// in the order they responded. Includes both the winner and the EVSEs that
    /// were NOT selected — useful for debugging multi-EVSE setups (e.g. parking
    /// lots with several stations on the same supply line).
    /// </param>
    /// <param name="RunId">The 8-byte RunID the EV used for this matching attempt.</param>
    public sealed record EVSLACMatchingResult(EVSECandidate                 Winner,
                                              SlacMatchCnf                  MatchCnf,
                                              IReadOnlyList<EVSECandidate>  AllCandidates,
                                              RunId                         RunId);

}
