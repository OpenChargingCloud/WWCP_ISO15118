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

using System.Net;

using cloud.charging.open.protocols.ISO15118.SDP.Messages;

namespace Vanaheimr.V2G.Simulation.Discovery
{
    /// <summary>
    /// The SECC's TCP endpoint an EVCC should connect to, as produced by <see cref="ISeccDiscovery"/>:
    /// address, port, and whether TLS is expected. This is the hand-off point between the discovery
    /// stage (SDP or a fixed endpoint) and <see cref="Transport.TcpV2GClient"/>.
    /// </summary>
    public sealed record SeccEndpoint(IPAddress Address, int Port, bool Tls)
    {
        /// <summary>Address as a string, including the IPv6 scope-id if present (e.g. <c>fe80::1%12</c>).</summary>
        public string Host => Address.ToString();

        /// <summary>
        /// Maps an <see cref="SDP_Response"/> to a <see cref="SeccEndpoint"/>. For a link-local SECC
        /// address without a scope-id, <paramref name="scopeId"/> (the discovery interface index) is
        /// attached so the OS can route the connection back through the same link.
        /// </summary>
        public static SeccEndpoint FromSdp(SDP_Response response, int scopeId = 0)
        {
            var address = response.SeccIPAddress;

            if (scopeId != 0 && address.IsIPv6LinkLocal && address.ScopeId == 0)
                address = new IPAddress(address.GetAddressBytes(), scopeId);

            return new SeccEndpoint(address, response.SeccPort, response.Security == SDP_Security.TLS);
        }
    }
}
