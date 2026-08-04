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

using cloud.charging.open.protocols.ISO15118.SDP.Server;

namespace Vanaheimr.V2G.Simulation.Discovery
{
    /// <summary>
    /// SECC-side counterpart to <see cref="ISeccDiscovery"/>: runs an <see cref="SECC_SDPServer"/> that
    /// answers SDP_Request frames with the SECC's TCP/TLS endpoint. A thin lifetime wrapper (start on
    /// construction-via-<see cref="StartAsync"/>, stop on dispose) so the CLI can advertise while it waits
    /// for the TCP connection.
    /// </summary>
    public sealed class SeccSdpAdvertiser(SECC_SDPServer server) : IAsyncDisposable
    {
        public Task StartAsync(CancellationToken ct = default) => server.Start(ct);

        public ValueTask DisposeAsync() => server.DisposeAsync();
    }
}
