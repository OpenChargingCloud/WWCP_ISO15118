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

using System.Net;

using cloud.charging.open.protocols.ISO15118.SDP.Messages;

#endregion

namespace cloud.charging.open.protocols.ISO15118.SDP.Client
{

    /// <summary>
    /// Discovery aborted because every received response was filtered out.
    /// </summary>
    public sealed record SDP_DiscoveryRejected : SDP_DiscoveryResult
    {
        /// <summary>
        /// Every answer that was refused, and why, in the order they were
        /// first heard - an answer heard again on a later request only once.
        /// </summary>
        public required IReadOnlyList<(SDP_Response Response, String Reason)> RejectedResponses { get; init; }

        /// <summary>
        /// Where each of <see cref="RejectedResponses"/> came from, in the
        /// same order. Empty for a result made without them.
        /// </summary>
        public IReadOnlyList<IPEndPoint> RejectedRemoteEndpoints { get; init; } = [];

    }

}
