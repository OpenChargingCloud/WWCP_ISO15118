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

using Microsoft.Extensions.Logging;

using cloud.charging.open.protocols.ISO15118.SDP.Server;
using cloud.charging.open.protocols.ISO15118.NetworkInterfaces;

#endregion

namespace cloud.charging.open.protocols.ISO15118.SDP.SECC_SDP_Demo
{

    /// <summary>
    /// An ISO/IEC 15118 Service Discovery Protocol (SDP) SECC Server demo application.
    /// It listens for incoming SDP requests on the specified network interface
    /// and logs the received requests and sent responses to the console.
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

            var server = new SECC_SDPServer(
                             new SECC_SDPServerOptions {
                                 Interface            = iface,
                                 SeccPort             = 64109,  // typical 15118 TLS port
                                 OfferedSecurity      = Messages.SDP_Security.TLS,
                                 RejectNoTlsRequests  = true,
                             },
                             loggerFactory.CreateLogger<SECC_SDPServer>()
                         );

            server.RequestReceived += e => Console.WriteLine(
                $"<-- SDP_Request from {e.RemoteEndpoint}: sec={e.Request.Security}, transport={e.Request.TransportProtocol}, accepted={e.Accepted} {e.RejectReason}"
            );

            server.ResponseSent    += e => Console.WriteLine(
                $"--> SDP_Response to {e.RemoteEndpoint}: {e.Response} ({e.BytesSent} bytes)"
            );

            server.MalformedRequestReceived += e => Console.WriteLine(
                $"!!! malformed SDP_Request from {e.RemoteEndpoint}: {e.Reason} ({e.RawBytes.Length} bytes)"
            );

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, ev) => { ev.Cancel = true; cts.Cancel(); };

            await server.Start(cts.Token);

            Console.WriteLine("SECC SDP server running. Ctrl-C to exit.");

            try
            {

                await Task.Delay(
                          Timeout.Infinite,
                          cts.Token
                      );

            }
            catch (OperationCanceledException)
            { }

            await server.DisposeAsync();

            return 0;

        }

    }

}
