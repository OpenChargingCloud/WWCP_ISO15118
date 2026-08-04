/*
 * Copyright (c) 2021-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of EVSimulatorApp
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

using cloud.charging.open.protocols.ISO15118.SDP.Client;

namespace Vanaheimr.V2G.Simulation.Discovery
{
    /// <summary>
    /// Discovery via the real SECC Discovery Protocol: multicasts an <c>SDP_Request</c> on the configured
    /// V2G interface and maps the SECC's <c>SDP_Response</c> to a <see cref="SeccEndpoint"/>.
    /// <para>
    /// Note: SDP is UDP/IPv6 link-local multicast on a real interface, so the full exchange is exercised
    /// only in real/CLI runs — an EVCC and SECC in the same process on one host cannot hear each other's
    /// multicast (both disable multicast loopback), so CI covers the message layer and the result mapping
    /// (<see cref="MapResult"/>) instead. See <c>docs/pki-model.md</c>.
    /// </para>
    /// </summary>
    public sealed class SdpSeccDiscovery(EVCC_SDPClientOptions options) : ISeccDiscovery
    {
        public async Task<SeccEndpoint> DiscoverAsync(CancellationToken ct = default)
        {
            await using var client = new EVCC_SDPClient(options);
            var result = await client.Discover(ct).ConfigureAwait(false);
            return MapResult(result, options.Interface.Index);
        }

        /// <summary>Maps an SDP discovery result to a <see cref="SeccEndpoint"/>, or throws on reject/timeout.</summary>
        public static SeccEndpoint MapResult(SDP_DiscoveryResult result, int scopeId)
            => result switch
            {
                SDP_DiscoverySuccess ok
                    => SeccEndpoint.FromSdp(ok.Response, scopeId),

                SDP_DiscoveryRejected rejected
                    => throw new SeccDiscoveryException(
                           $"SDP discovery rejected {rejected.RejectedResponses.Count} response(s) after " +
                           $"{rejected.Attempts} attempt(s): {string.Join("; ", rejected.RejectedResponses.Select(r => r.Reason))}"),

                SDP_DiscoveryTimeout timeout
                    => throw new SeccDiscoveryException(
                           $"SDP discovery timed out after {timeout.Attempts} attempt(s) / {timeout.Elapsed.TotalSeconds:F1}s"),

                _ => throw new SeccDiscoveryException($"Unknown SDP discovery result: {result.GetType().Name}"),
            };
    }
}
