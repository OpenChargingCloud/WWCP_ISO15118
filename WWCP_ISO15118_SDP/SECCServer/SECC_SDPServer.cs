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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using cloud.charging.open.protocols.ISO15118.V2GTP;
using cloud.charging.open.protocols.ISO15118.SDP.Messages;
using cloud.charging.open.protocols.ISO15118.NetworkInterfaces;

#endregion

namespace cloud.charging.open.protocols.ISO15118.SDP.Server;

/// <summary>
/// SECC-side SDP server. Listens on UDP port 15118 of the configured PLC
/// interface, joins the FF02::1 link-local multicast group, and replies to
/// SDP_Request frames with an SDP_Response advertising the local TCP/TLS
/// endpoint.
///
/// Lifecycle: construct, then <see cref="StartAsync"/>; the server runs
/// until the supplied <see cref="CancellationToken"/> is signalled or
/// <see cref="DisposeAsync"/> is called.
/// </summary>
public sealed class SECC_SDPServer : IAsyncDisposable
{

    private readonly SECC_SDPServerOptions      _options;
    private readonly ILogger<SECC_SDPServer>   _logger;
    private          Socket?                   _socket;
    private          CancellationTokenSource?  _cts;
    private          Task?                     _receiveLoop;

    public event Action<SDP_RequestReceivedEventArgs>?   RequestReceived;
    public event Action<SDP_ResponseSentEventArgs>?      ResponseSent;
    public event Action<SDP_MalformedRequestEventArgs>?  MalformedRequestReceived;

    public SECC_SDPServer(SECC_SDPServerOptions     options,
                         ILogger<SECC_SDPServer>?  logger = null)
    {
        _options = options;
        _logger  = logger ?? NullLogger<SECC_SDPServer>.Instance;
    }

    /// <summary>Open the UDP socket, join the multicast group, start the receive loop.</summary>
    public Task StartAsync(CancellationToken ct = default)
    {

        if (_socket is not null)
            throw new InvalidOperationException("The SECC SDPServer is already running.");

        var sock = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp) {
                       DualMode = false,
                   };

        // Allow other listeners (e.g. another SECC instance for tests) to share the port.
        sock.SetSocketOption(SocketOptionLevel.Socket,  SocketOptionName.ReuseAddress, true);

        // Bind to ::, port 15118.
        sock.Bind(V2GMulticast.SDPListenEndpoint);

        // Join FF02::1 on the configured interface.
        var join = new IPv6MulticastOption(V2GMulticast.SDPMulticastGroup, _options.Interface.Index);
        sock.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, join);

        // Pin outgoing multicast to our interface.
        sock.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastInterface, _options.Interface.Index);

        // We do not loopback our own multicasts.
        sock.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastLoopback, false);

        _socket       = sock;
        _cts          = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _receiveLoop  = Task.Run(() => ReceiveLoopAsync(_cts.Token), ct);

        _logger.LogInformation(
            "SECC SDPServer listening on [::]:{Port} via {Interface} (link-local {Addr}, scope {Scope}); offered TCP endpoint [{SeccAddr}]:{SeccPort} sec={Sec}",
            V2GMulticast.V2GUdpPort,
            _options.Interface.Name,
            _options.Interface.LinkLocalIPAddress,
            _options.Interface.Index,
            _options.SeccIPAddressOverride ?? _options.Interface.LinkLocalIPAddress,
            _options.SeccPort,
            _options.OfferedSecurity
        );

        return Task.CompletedTask;

    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {

        var buffer  = new Byte[1500];
        var remote  = new IPEndPoint(IPAddress.IPv6Any, 0);

        while (!ct.IsCancellationRequested)
        {

            SocketReceiveFromResult rx;

            try
            {
                rx = await _socket!.ReceiveFromAsync(buffer, SocketFlags.None, remote, ct).
                                    ConfigureAwait  (false);
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException)    { break; }
            catch (SocketException ex)
            {
                _logger.LogWarning(ex, "SECC SDPServer socket receive failed");
                continue;
            }

            var datagram  = buffer.AsMemory(0, rx.ReceivedBytes);
            var from      = (IPEndPoint) rx.RemoteEndPoint;

            try
            {
                await HandleDatagramAsync(datagram, from, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error processing SDP datagram from {Remote}", from);
            }

        }

    }

    private async Task HandleDatagramAsync(ReadOnlyMemory<Byte>  Datagram,
                                           IPEndPoint            SourceEndPoint,
                                           CancellationToken     CancellationToken)
    {

        #region 1. V2GTP header parse

        V2GTP_Frame v2gTPFrame;
        try
        {
            v2gTPFrame = V2GTP_Frame.Parse(Datagram);
        }
        catch (V2GTP_Exception ex)
        {

            MalformedRequestReceived?.Invoke(
                new SDP_MalformedRequestEventArgs(
                    DateTimeOffset.UtcNow,
                    SourceEndPoint,
                    Datagram.ToArray(),
                    $"V2GTP parse: {ex.Message}"
                )
            );

            return;

        }

        #endregion

        #region 2. Payload type check

        if (v2gTPFrame.Header.PayloadType != V2GTP_PayloadType.SdpRequest)
        {

            // 15118-20 also defines 0x9002 (SDP_RequestWireless) for ACDP/WPT –
            // this server only handles the basic wired SDP_Request. A separate
            // SeccSdpWirelessServer can be added later if needed.
            _logger.LogDebug(
                "Ignoring V2GTP frame with payload type {Pt:X4} from {Remote}",
                (UInt16) v2gTPFrame.Header.PayloadType,
                SourceEndPoint
            );

            return;

        }

        #endregion

        #region 3. SDP_Request decode

        SDP_Request req;

        try
        {

            req = SDP_Request.Decode(
                      v2gTPFrame.Payload.Span,
                      SDP_Version.ISO_15118_2
                  );

        }
        catch (Exception ex)
        {

            MalformedRequestReceived?.Invoke(
                new SDP_MalformedRequestEventArgs(
                    DateTimeOffset.UtcNow,
                    SourceEndPoint,
                    Datagram.ToArray(),
                    $"SDP_Request decode: {ex.Message}"
                )
            );

            return;

        }

        #endregion

        #region 4. Acceptance policy

        String? rejectReason = null;

        if (_options.RejectNoTlsRequests && req.Security == SDP_Security.NoTLS)
            rejectReason = "no-TLS request rejected by policy";

        if (rejectReason is null && _options.RequestFilter is not null && !_options.RequestFilter(SourceEndPoint, req))
            rejectReason = "rejected by RequestFilter";

        // Note: Basic SDP_Request bytes are identical in 15118-2 and 15118-20,
        // so we can't filter by version here. The AcceptedVersions knob takes
        // effect once Wireless SDP (0x9002/0x9003) handling is added.
        var inferredVersion = SDP_Version.ISO_15118_2;
        if (rejectReason is null && _options.AcceptedVersions.Count == 0)
            rejectReason = "no SDP versions configured for acceptance";

        RequestReceived?.Invoke(
            new SDP_RequestReceivedEventArgs(
                DateTimeOffset.UtcNow,
                SourceEndPoint,
                v2gTPFrame.Header,
                req,
                rejectReason is null,
                rejectReason
            )
        );

        if (rejectReason is not null)
        {
            _logger.LogInformation("SDP_Request from {Remote} rejected: {Reason}", SourceEndPoint, rejectReason);
            return;
        }

        #endregion

        #region 5. Build response

        var response = new SDP_Response(
                           SeccIPAddress:      _options.SeccIPAddressOverride ?? _options.Interface.LinkLocalIPAddress,
                           SeccPort:           _options.SeccPort,
                           Security:           _options.OfferedSecurity,
                           TransportProtocol:  SDP_TransportProtocol.TCP,
                           Version:            inferredVersion
                       );

        if (_options.ResponseTransformer is not null)
            response = _options.ResponseTransformer(response);

        #endregion

        #region 6. Serialize

        var frameBytes  = _options.RawPayloadSerializer is not null
                              ? V2GTP_Frame.Wrap(V2GTP_PayloadType.SdpResponse, _options.RawPayloadSerializer(response)).ToArray()
                              : response.EncodeFrame();

        #endregion

        #region 7. (Optional) Injected delay

        if (_options.ResponseDelay > TimeSpan.Zero)
            await Task.Delay(
                      _options.ResponseDelay,
                      CancellationToken
                  ).ConfigureAwait(false);

        #endregion

        #region 8. Send to EVCC's unicast endpoint

        // The EVCC sent its multicast SDP_Request from a link-local address;
        // we have to reply to that exact endpoint so the kernel routes back
        // through the same scope.
        var replyTo = EnsureScoped(SourceEndPoint);

        for (var i = 0; i < Math.Max(1, _options.ResponseDuplicates); i++)
        {

            Int32 sent;
            try
            {

                sent = await _socket!.SendToAsync(
                                 frameBytes,
                                 SocketFlags.None,
                                 replyTo,
                                 CancellationToken
                             ).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send SDP_Response to {Remote}", replyTo);
                return;
            }

            ResponseSent?.Invoke(
                new SDP_ResponseSentEventArgs(
                    DateTimeOffset.UtcNow,
                    replyTo,
                    response,
                    sent
                )
            );

        }

        #endregion

    }

    /// <summary>
    /// Ensure the IPv6 endpoint we reply to carries our interface's scope-id;
    /// some kernels deliver link-local source addresses without it.
    /// </summary>
    private IPEndPoint EnsureScoped(IPEndPoint SourceEndPoint)
    {

        if (SourceEndPoint.Address.IsIPv6LinkLocal && SourceEndPoint.Address.ScopeId == 0)
            return new IPEndPoint(
                       new IPAddress(
                           SourceEndPoint.Address.GetAddressBytes(),
                           _options.Interface.Index
                       ),
                       SourceEndPoint.Port
                   );

        return SourceEndPoint;

    }

    public async ValueTask DisposeAsync()
    {

        try
        {
            _cts?.Cancel();
        }
        catch { }

        if (_receiveLoop is not null)
        {
            try
            {
                await _receiveLoop.ConfigureAwait(false);
            }
            catch { }
        }

        _socket?.Dispose();
        _cts?.   Dispose();

    }

}
