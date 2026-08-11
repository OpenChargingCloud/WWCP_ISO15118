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

namespace cloud.charging.open.protocols.ISO15118.Session
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

        /// <summary>
        /// When set, outgoing headers carry <b>this</b> SessionID instead of <see cref="SessionId"/>.
        /// Null (the default) is what every conformant peer does and what every recorded session holds.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Only the <b>car</b> ever sets it — <c>Evcc20Base.SendSessionId</c>, applied after SessionSetup
        /// so the opening request still carries the "new session" or resume id that `[V2G20-460]` excludes
        /// from the rule. No station of ours touches it, which is why it lives here rather than being
        /// duplicated across the three header shapes at every call site.
        /// </para>
        /// <para>
        /// It exists so a station's duty to refuse a foreign SessionID is reachable from a session at all.
        /// The `-2` twin (<c>Evcc2.SendSessionId</c>) was added on 2026-08-11 and immediately found a
        /// station that serves the all-zero id; this is the same instrument for `-20`.
        /// </para>
        /// </remarks>
        public byte[]? SendSessionIdOverride { get; set; }

        private byte[] OutgoingId => SendSessionIdOverride ?? SessionId;

        private ulong NextTimeStamp() => (ulong)clock.GetUtcNow().ToUnixTimeSeconds();

        public CommonHeader ToCommonHeader() => new(OutgoingId, NextTimeStamp(), Signature: null);
        public DcHeader ToDcHeader() => new(OutgoingId, NextTimeStamp(), Signature: null);
        public AcHeader ToAcHeader() => new(OutgoingId, NextTimeStamp(), Signature: null);
    }
}
