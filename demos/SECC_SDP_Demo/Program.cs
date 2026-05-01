using Microsoft.Extensions.Logging;
using cloud.charging.open.protocols.ISO15118.NetworkInterfaces;
using cloud.charging.open.protocols.ISO15118.SDP.Server;

namespace Vanaheimr.V2G.SeccSdpDemo;

internal static class Program
{
    public static async Task<Int32> Main(String[] args)
    {

        var ifaceName = args.FirstOrDefault() ?? "eth0";

        using var loggerFactory = LoggerFactory.Create(b => b
            .SetMinimumLevel(LogLevel.Debug)
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

        var server = new SECC_SDPServer(
            new SECC_SDPServerOptions {
                Interface              = iface,
                SeccPort               = 64109,                  // typical 15118 TLS port
                OfferedSecurity        = cloud.charging.open.protocols.ISO15118.SDP.Messages.SDP_Security.TLS,
                RejectNoTlsRequests    = true,
            },
            loggerFactory.CreateLogger<SECC_SDPServer>());

        server.RequestReceived += e => Console.WriteLine(
            $"<-- SDP_Request from {e.RemoteEndpoint}: sec={e.Request.Security}, transport={e.Request.TransportProtocol}, accepted={e.Accepted} {e.RejectReason}");
        server.ResponseSent    += e => Console.WriteLine(
            $"--> SDP_Response to {e.RemoteEndpoint}: {e.Response} ({e.BytesSent} bytes)");
        server.MalformedRequestReceived += e => Console.WriteLine(
            $"!!! malformed SDP_Request from {e.RemoteEndpoint}: {e.Reason} ({e.RawBytes.Length} bytes)");

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, ev) => { ev.Cancel = true; cts.Cancel(); };

        await server.StartAsync(cts.Token);
        Console.WriteLine("SECC SDP server running. Ctrl-C to exit.");
        try { await Task.Delay(Timeout.Infinite, cts.Token); }
        catch (OperationCanceledException) { /* expected */ }

        await server.DisposeAsync();
        return 0;

    }

}
