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

using cloud.charging.open.protocols.ISO15118.SDP.Client;
using cloud.charging.open.protocols.ISO15118.SDP.Messages;

using cloud.charging.open.protocols.ISO15118.NetworkInterfaces;

#endregion

namespace cloud.charging.open.protocols.ISO15118.SDP.EVCC_SDP_Demo
{

    /// <summary>
    /// A simple console application to demonstrate the use of the EVCC_SDPClient
    /// for discovering SECCs in the local network using the ISO/IEC 15118
    /// Service Discovery Protocol (SDP).
    /// </summary>
    public static class Program
    {
        public static async Task<Int32> Main(String[] Arguments)
        {

            var ifaceName = Arguments.FirstOrDefault() ?? "eth0";

            using var loggerFactory = LoggerFactory.Create(
                                          loggingBuilder => loggingBuilder.
                                                                SetMinimumLevel(LogLevel.Debug).
                                                                AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss.fff "; })
                                      );

            var provider  = new SystemV2GNetworkInterfaceProvider();
            var iface     = provider.FindByName(ifaceName);
            if (iface is null)
            {

                Console.Error.WriteLine($"No V2G-capable interface named '{ifaceName}'. Available:");

                foreach (var networkInterface in provider.Discover())
                    Console.Error.WriteLine($"  - {networkInterface}");

                return 1;

            }

            Console.WriteLine($"Selected interface: {iface}");

            await using var client = new EVCC_SDPClient(
                                         new EVCC_SDPClientOptions {
                                             Interface                    = iface,
                                             RequestedSecurity            = SDP_Security.TLS,
                                             RequestedTransport           = SDP_TransportProtocol.TCP,
                                             PerAttemptTimeout            = TimeSpan.FromMilliseconds(250),
                                             MaxRetries                   = 50,
                                             TotalDeadline                = TimeSpan.FromSeconds(60),
                                             RejectNoTlsResponses         = true,
                                             RequireLinkLocalSeccAddress  = true,
                                         },
                                         loggerFactory.CreateLogger<EVCC_SDPClient>()
                                     );

            client.RequestSent               += sdpRequest                        => Console.WriteLine(
                $"--> SDP_Request sec={sdpRequest.Security} transport={sdpRequest.TransportProtocol}"
            );

            client.ResponseReceived          += (sdpResponse, ipEndPoint)         => Console.WriteLine(
                $"<-- SDP_Response from {ipEndPoint}: {sdpResponse}"
            );

            client.MalformedResponseReceived += (bytes, ipEndPoint, description)  => Console.WriteLine(
                $"!!! malformed response from {ipEndPoint}: {description} ({bytes.Length} bytes)"
            );

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(65));
            var result = await client.Discover(cts.Token);

            switch (result)
            {

                case SDP_DiscoverySuccess sdpDiscoverySuccess:
                    Console.WriteLine($"Discovered SECC at [{sdpDiscoverySuccess.Response.SeccIPAddress}]:{sdpDiscoverySuccess.Response.SeccPort} (sec={sdpDiscoverySuccess.Response.Security}) after {sdpDiscoverySuccess.Attempts} attempts in {sdpDiscoverySuccess.Elapsed.TotalMilliseconds:F0} ms");
                    return 0;

                case SDP_DiscoveryRejected sdpDiscoveryRejected:
                    Console.WriteLine($"All {sdpDiscoveryRejected.RejectedResponses.Count} responses rejected after {sdpDiscoveryRejected.Attempts} attempts:");
                    foreach (var (sdpResponse, description) in sdpDiscoveryRejected.RejectedResponses)
                        Console.WriteLine($"  - {sdpResponse} : {description}");
                    return 2;

                case SDP_DiscoveryTimeout sdpDiscoveryTimeout:
                    Console.WriteLine($"Discovery timed out after {sdpDiscoveryTimeout.Attempts} attempts ({sdpDiscoveryTimeout.Elapsed.TotalSeconds:F1} s).");
                    return 3;

                default:
                    return 99;

            }

        }

    }

}
