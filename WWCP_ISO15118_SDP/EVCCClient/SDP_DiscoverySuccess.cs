/*
 * Copyright (c) 2014-2025 GraphDefined GmbH <achim.friedland@graphdefined.com>
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
    /// The SECC was discovered – use the contained TCP/TLS endpoint.
    /// </summary>
    public sealed record SDP_DiscoverySuccess : SDP_DiscoveryResult
    {
        public required SDP_Response  Response          { get; init; }
        public required IPEndPoint    RemoteEndpoint    { get; init; }

        /// <summary>
        /// All other valid responses that arrived during discovery (only
        /// populated if <see cref="DuplicateResponseStrategy.CollectAll"/> is
        /// used). Empty for AcceptFirst.
        /// </summary>
        public IReadOnlyList<SDP_Response>  AdditionalResponses    { get; init; } = [];

    }

}
