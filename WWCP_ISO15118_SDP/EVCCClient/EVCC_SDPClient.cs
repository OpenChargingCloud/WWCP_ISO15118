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
using System.Net.Sockets;
using System.Diagnostics;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using cloud.charging.open.protocols.ISO15118.V2GTP;
using cloud.charging.open.protocols.ISO15118.SDP.Messages;
using cloud.charging.open.protocols.ISO15118.NetworkInterfaces;

#endregion

namespace cloud.charging.open.protocols.ISO15118.SDP.Client
{

    /// <summary>
    /// EVCC-side SDP client. Multicasts SDP_Request to FF02::1 on the
    /// configured PLC interface and waits for an SDP_Response from the SECC.
    ///
    /// One <see cref="EVCC_SDPClient"/> instance can run multiple discoveries
    /// sequentially via <see cref="DiscoverAsync"/>. The instance is *not*
    /// safe for concurrent discoveries on the same socket.
    /// </summary>
    public sealed class EVCC_SDPClient : IAsyncDisposable
    {

        private readonly EVCC_SDPClientOptions    clientOptions;
        private readonly ILogger<EVCC_SDPClient>  logger;
        private          Socket?                  socket;

        public event Action<SDP_Request>?                      RequestSent;
        public event Action<SDP_Response, IPEndPoint>?         ResponseReceived;
        public event Action<Byte[],      IPEndPoint, String>?  MalformedResponseReceived;

        public EVCC_SDPClient(EVCC_SDPClientOptions     ClientOptions,
                              ILogger<EVCC_SDPClient>?  logger   = null)
        {

            this.clientOptions  = ClientOptions;
            this.logger         = logger ?? NullLogger<EVCC_SDPClient>.Instance;

        }

        /// <summary>
        /// Run the SDP discovery cycle.
        /// </summary>
        public async Task<SDP_DiscoveryResult> DiscoverAsync(CancellationToken ct = default)
        {

            EnsureSocketOpen();

            var sw         = Stopwatch.StartNew();
            var deadline   = DateTimeOffset.UtcNow + clientOptions.TotalDeadline;
            var attempts   = 0;
            var rejects    = new List<(SDP_Response, String)>();

            // Build the request once – its bytes do not change between retries.
            var request    = new SDP_Request(clientOptions.RequestedSecurity, clientOptions.RequestedTransport);
            var reqBytes   = clientOptions.RawPayloadSerializer is { } custom
                                 ? V2GTP_Frame.Wrap(V2GTP_PayloadType.SdpRequest, custom(request)).ToArray()
                                 : request.EncodeFrame();

            var multicast  = V2GMulticast.SDPSendEndpoint;
            var collected  = new List<(SDP_Response, IPEndPoint)>();

            while (attempts < clientOptions.MaxRetries && DateTimeOffset.UtcNow < deadline)
            {

                attempts++;
                ct.ThrowIfCancellationRequested();

                // ---- send ---------------------------------------------------
                try
                {

                    if (socket is not null)
                    {

                        await socket.SendToAsync(
                                  reqBytes,
                                  SocketFlags.None,
                                  multicast,
                                  ct
                              ).ConfigureAwait(false);

                        logger.LogDebug(
                            "Sent SDP_Request #{Attempt} to {Multicast} via {Iface}",
                            attempts,
                            multicast,
                            clientOptions.Interface.Name
                        );

                        RequestSent?.Invoke(request);

                    }

                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Sending SDP_Request failed (attempt {Attempt})", attempts);
                    continue;
                }

                // ---- await response (within per-attempt timeout) ----------
                using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                attemptCts.CancelAfter(clientOptions.PerAttemptTimeout);

                try
                {
                    await foreach (var (resp, from) in ReceiveResponsesAsync(attemptCts.Token).ConfigureAwait(false))
                    {

                        var rejectReason = Validate(resp);
                        if (rejectReason is not null)
                        {
                            rejects.Add((resp, rejectReason));
                            logger.LogInformation("SDP_Response from {Remote} rejected: {Reason}", from, rejectReason);
                            continue;
                        }

                        if (clientOptions.ResponseFilter is not null && !clientOptions.ResponseFilter(resp))
                        {
                            rejects.Add((resp, "rejected by ResponseFilter"));
                            continue;
                        }

                        ResponseReceived?.Invoke(resp, from);

                        if (clientOptions.DuplicateStrategy == DuplicateResponseStrategy.AcceptFirst)
                            return new SDP_DiscoverySuccess {
                                       Response        = resp,
                                       RemoteEndpoint  = from,
                                       Attempts        = attempts,
                                       Elapsed         = sw.Elapsed,
                                   };

                        collected.Add((resp, from));

                    }
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    // per-attempt timeout – fall through to next retry
                }

                if (collected.Count > 0 && clientOptions.DuplicateStrategy == DuplicateResponseStrategy.CollectAll)
                    return new SDP_DiscoverySuccess {
                               Response             = collected[0].Item1,
                               RemoteEndpoint       = collected[0].Item2,
                               Attempts             = attempts,
                               Elapsed              = sw.Elapsed,
                               AdditionalResponses  = [.. collected.Skip(1).Select(x => x.Item1)],
                           };

            }

            if (rejects.Count > 0)
                return new SDP_DiscoveryRejected {
                           Attempts           = attempts,
                           Elapsed            = sw.Elapsed,
                           RejectedResponses  = rejects,
                       };

            return new SDP_DiscoveryTimeout {
                       Attempts  = attempts,
                       Elapsed   = sw.Elapsed,
                   };

        }

        private async IAsyncEnumerable<(SDP_Response, IPEndPoint)> ReceiveResponsesAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {

            var buffer = new byte[1500];
            EndPoint remote = new IPEndPoint(IPAddress.IPv6Any, 0);

            while (!ct.IsCancellationRequested)
            {

                SocketReceiveFromResult rx;
                try
                {
                    rx = await socket!.ReceiveFromAsync(buffer, SocketFlags.None, remote, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { yield break; }
                catch (ObjectDisposedException)    { yield break; }

                var datagram = buffer.AsMemory(0, rx.ReceivedBytes);
                var from     = (IPEndPoint)rx.RemoteEndPoint;

                V2GTP_Frame frame;
                try
                {
                    frame = V2GTP_Frame.Parse(datagram);
                }
                catch (V2GTP_Exception ex)
                {
                    MalformedResponseReceived?.Invoke(datagram.ToArray(), from, $"V2GTP parse: {ex.Message}");
                    continue;
                }

                if (frame.Header.PayloadType != V2GTP_PayloadType.SdpResponse)
                {
                    logger.LogDebug("Discarding V2GTP frame with payload type {Pt:X4} from {Remote}",
                        (ushort)frame.Header.PayloadType, from);
                    continue;
                }

                SDP_Response? resp = null;
                string?      err  = null;
                try { resp = SDP_Response.Decode(frame.Payload.Span); }
                catch (Exception ex) { err = ex.Message; }

                if (resp is null)
                {
                    MalformedResponseReceived?.Invoke(datagram.ToArray(), from, $"SDP_Response decode: {err}");
                    continue;
                }

                yield return (resp, from);

            }

        }

        private String? Validate(SDP_Response resp)
        {

            if (clientOptions.RejectNoTlsResponses && resp.Security == SDP_Security.NoTLS)
                return "no-TLS response rejected by policy (RejectNoTlsResponses=true)";

            if (resp.TransportProtocol != SDP_TransportProtocol.TCP)
                return $"unsupported transport protocol 0x{(byte)resp.TransportProtocol:X2}";

            if (clientOptions.RequireLinkLocalSeccAddress && !resp.SeccIPAddress.IsIPv6LinkLocal)
                return $"SECC address {resp.SeccIPAddress} is not link-local";

            if (resp.SeccPort == 0)
                return "SECC port is zero";

            return null;

        }

        private void EnsureSocketOpen()
        {

            if (socket is not null)
                return;

            var sock = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp)
            {
                DualMode = false,
            };
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            // Bind to an ephemeral port on the V2G interface; that's where the
            // SECC will unicast its response back.
            var local = new IPEndPoint(
                new IPAddress(clientOptions.Interface.LinkLocalIPAddress.GetAddressBytes(), clientOptions.Interface.Index),
                port: 0);
            sock.Bind(local);

            // Pin outgoing multicast to our interface.
            sock.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastInterface, clientOptions.Interface.Index);
            sock.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastTimeToLive, 1);
            sock.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastLoopback, false);

            socket = sock;
            logger.LogInformation("EVCC SDP client ready on [{Addr}%{Scope}]:{Port}",
                local.Address, clientOptions.Interface.Index, ((IPEndPoint)sock.LocalEndPoint!).Port);

        }

        public ValueTask DisposeAsync()
        {
            socket?.Dispose();
            socket = null;
            return ValueTask.CompletedTask;
        }

    }

}
