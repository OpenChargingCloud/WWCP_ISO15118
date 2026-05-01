using System.Net;
using cloud.charging.open.protocols.ISO15118.SLAC.Bridge;

namespace cloud.charging.open.protocols.ISO15118.SLAC.BridgeDemo
{

    public static class Program
    {
        public static async Task<Int32> Main(String[] args)
        {

            var ifname  = Cli.ParseRequired("--iface", args);
            var udpPort = Cli.ParseOptional("--udp-port", args, 5500);
            var verbose = args.Contains("--verbose") || args.Contains("-v");

            var configuredPeers = Cli.ParseMultiple("--peer", args)
                .Select(ParseEndpoint)
                .ToList();

            if (!OperatingSystem.IsLinux())
            {
                Console.Error.WriteLine("This bridge requires Linux (uses AF_PACKET).");
                return 1;
            }

            WriteHeader(ifname, udpPort, configuredPeers);

            var listenOn = new IPEndPoint(IPAddress.Any, udpPort);

            await using var bridge = new SLACBridge(ifname, listenOn, configuredPeers);

            bridge.Log += (_, m) => Console.WriteLine($"[bridge] {m}");

            bridge.FrameForwarded += (_, e) =>
            {
                if (!verbose) return;

                var arrow = e.Direction == SLACBridgeDirection.L2ToUdp ? "L2→UDP" : "UDP→L2";
                var src   = e.Source is null ? "" : $" from {e.Source}";
                Console.WriteLine(
                    $"[fwd]    {arrow}  {e.Frame.Source} → {e.Frame.Destination}  {e.Frame.Message.MmType}{src}");
            };

            try
            {
                await bridge.StartAsync();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to start bridge: {ex.Message}");
                return 1;
            }

            Console.WriteLine();
            Console.WriteLine("Bridge running. Press Ctrl-C to stop.");
            Console.WriteLine();

            var done = new TaskCompletionSource();
            Console.CancelKeyPress += (_, ev) =>
            {
                ev.Cancel = true;
                done.TrySetResult();
            };
            await done.Task;

            Console.WriteLine();
            Console.WriteLine("Shutting down ...");
            return 0;

            // =================================================================== helpers

            static IPEndPoint ParseEndpoint(String s)
            {
                var parts = s.Split(':');
                if (parts.Length != 2)
                    throw new FormatException($"Invalid endpoint '{s}', expected host:port");
                return new IPEndPoint(IPAddress.Parse(parts[0]), int.Parse(parts[1]));
            }

            static void WriteHeader(String ifname, Int32 udpPort, List<IPEndPoint> peers)
            {

                var prev = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine();
                Console.WriteLine("================================================================");
                Console.WriteLine(" SLAC bridge  -  AF_PACKET (HPAV / 0x88E1)  ⇆  UDP simulation   ");
                Console.WriteLine("================================================================");
                Console.ForegroundColor = prev;

                Console.WriteLine();
                Console.WriteLine($"  Interface         : {ifname}");
                Console.WriteLine($"  UDP listen port   : {udpPort}");
                Console.WriteLine($"  Configured peers  : {peers.Count}");
                foreach (var ep in peers)
                    Console.WriteLine($"                       - {ep}");
                Console.WriteLine();
            }

        }

        private static class Cli
        {
            public static String ParseRequired(String key, String[] args)
            {
                for (var i = 0; i < args.Length - 1; i++)
                    if (args[i] == key)
                        return args[i + 1];
                throw new ArgumentException($"Missing required flag: {key}");
            }

            public static T ParseOptional<T>(String key, String[] args, T fallback)
            {
                for (var i = 0; i < args.Length - 1; i++)
                    if (args[i] == key)
                        return (T) Convert.ChangeType(args[i + 1], typeof(T))!;
                return fallback;
            }

            public static IEnumerable<String> ParseMultiple(String key, String[] args)
            {
                for (var i = 0; i < args.Length - 1; i++)
                    if (args[i] == key)
                        yield return args[i + 1];
            }

        }

    }

}
