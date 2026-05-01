/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP OICP <https://github.com/OpenChargingCloud/WWCP_OICP>
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

using Microsoft.Extensions.Logging;

using cloud.charging.open.protocols.ISO15118.NetworkInterfaces;

using cloud.charging.open.protocols.ISO15118.SDP.Client;
using cloud.charging.open.protocols.ISO15118.SDP.Messages;

#endregion

namespace Vanaheimr.V2G.EvccSdpDemo
{

    public class Program
    {
        public static async Task<Int32> Main(String[] Arguments)
        {

            var ifaceName = Arguments.FirstOrDefault() ?? "eth0";

            using var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Debug)
                                                                 .AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss.fff "; }));

            var provider = new SystemV2GNetworkInterfaceProvider();
            var iface    = provider.FindByName(ifaceName);
            if (iface is null)
            {
                Console.Error.WriteLine($"No V2G-capable interface named '{ifaceName}'. Available:");
                foreach (var i in provider.Discover()) Console.Error.WriteLine($"  - {i}");
                return 1;
            }

            Console.WriteLine($"Selected interface: {iface}");

            await using var client = new EVCC_SDPClient(
                new EVCC_SDPClientOptions
                {
                    Interface                   = iface,
                    RequestedSecurity           = SDP_Security.TLS,
                    RequestedTransport          = SDP_TransportProtocol.TCP,
                    PerAttemptTimeout           = TimeSpan.FromMilliseconds(250),
                    MaxRetries                  = 50,
                    TotalDeadline               = TimeSpan.FromSeconds(60),
                    RejectNoTlsResponses        = true,
                    RequireLinkLocalSeccAddress = true,
                },
                loggerFactory.CreateLogger<EVCC_SDPClient>());

            client.RequestSent              += req     => Console.WriteLine($"--> SDP_Request sec={req.Security} transport={req.TransportProtocol}");
            client.ResponseReceived         += (r, ep) => Console.WriteLine($"<-- SDP_Response from {ep}: {r}");
            client.MalformedResponseReceived+= (b, ep, why) => Console.WriteLine($"!!! malformed response from {ep}: {why} ({b.Length} bytes)");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(65));
            var result = await client.DiscoverAsync(cts.Token);

            switch (result)
            {

                case SDP_DiscoverySuccess s:
                    Console.WriteLine($"Discovered SECC at [{s.Response.SeccIPAddress}]:{s.Response.SeccPort} (sec={s.Response.Security}) after {s.Attempts} attempts in {s.Elapsed.TotalMilliseconds:F0} ms");
                    return 0;

                case SDP_DiscoveryRejected r:
                    Console.WriteLine($"All {r.RejectedResponses.Count} responses rejected after {r.Attempts} attempts:");
                    foreach (var (resp, why) in r.RejectedResponses)
                        Console.WriteLine($"  - {resp} : {why}");
                    return 2;

                case SDP_DiscoveryTimeout t:
                    Console.WriteLine($"Discovery timed out after {t.Attempts} attempts ({t.Elapsed.TotalSeconds:F1} s).");
                    return 3;

                default:
                    return 99;

            }

        }

    }

}
