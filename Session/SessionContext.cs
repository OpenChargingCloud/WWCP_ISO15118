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

using CommonHeader = cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated.MessageHeaderType;
using DcHeader = cloud.charging.open.protocols.ISO15118_20.DC.Generated.MessageHeaderType;
using AcHeader = cloud.charging.open.protocols.ISO15118_20.AC.Generated.MessageHeaderType;

namespace Vanaheimr.V2G.Simulation.Session
{
    /// <summary>
    /// ISO 15118-20's per-schema-set assemblies are self-contained (no cross-references between
    /// CommonMessages/DC/AC), so <c>MessageHeaderType</c> is a structurally identical but distinct CLR
    /// type per message set. This holds the session's actual state (the SECC-assigned SessionID, a
    /// monotonic TimeStamp) and renders it into whichever namespace's header the next outgoing message
    /// needs — the concrete fix for that duplication when a -20 session crosses from a CommonMessages
    /// phase to a DC/AC phase and back.
    /// </summary>
    public sealed class SessionContext(TimeProvider clock)
    {
        public byte[] SessionId { get; set; } = new byte[8];

        private ulong NextTimeStamp() => (ulong)clock.GetUtcNow().ToUnixTimeSeconds();

        public CommonHeader ToCommonHeader() => new(SessionId, NextTimeStamp(), Signature: null);
        public DcHeader ToDcHeader() => new(SessionId, NextTimeStamp(), Signature: null);
        public AcHeader ToAcHeader() => new(SessionId, NextTimeStamp(), Signature: null);
    }
}
