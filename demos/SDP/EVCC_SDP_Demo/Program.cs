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

            // --loopback may stand anywhere; what is left over is the interface.
            var loopback   = Arguments.Any(argument => argument.Equals("--loopback", StringComparison.OrdinalIgnoreCase));
            var ifaceName  = Arguments.FirstOrDefault(argument => !argument.StartsWith("--")) ?? "eth0";

            if (Arguments.Any(argument => argument is "-h" or "--help"))
            {
                Console.WriteLine("Usage: EVCC_SDP_Demo [<interface>] [--loopback]");
                Console.WriteLine();
                Console.WriteLine("  --loopback  let this request also reach a SECC on this same machine, for a");
                Console.WriteLine("              bench where both run here. Off by default, because on real");
                Console.WriteLine("              hardware a vehicle must not hear itself.");
                Console.WriteLine();
                Console.WriteLine("              Set it on the SECC too. The two platforms disagree about which");
                Console.WriteLine("              socket this switch belongs to - POSIX the sender's, Windows the");
                Console.WriteLine("              receiver's - so a bench needs both sides to say yes.");
                return 0;
            }

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

            if (loopback)
                Console.WriteLine("Reaching a SECC on this machine as well (--loopback).");

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
                                             MulticastLoopback            = loopback,
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
