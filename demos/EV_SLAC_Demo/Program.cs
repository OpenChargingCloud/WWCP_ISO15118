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

// See also: https://github.com/qca/open-plc-utils/tree/master/slac

#region Usings

using System.Net;
using System.Text;

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

using cloud.charging.open.protocols.ISO15118.SLAC;
using cloud.charging.open.protocols.ISO15118.SLAC.Avln;
using cloud.charging.open.protocols.ISO15118.SLAC.StateMachine;
using cloud.charging.open.protocols.ISO15118.SLAC.Transport;
using cloud.charging.open.protocols.ISO15118.SLAC.Validation;

#endregion

var localPort   =        Cli.ParseOptional("--port",        args, 5118);
var pevIdAscii  =        Cli.ParseOptional("--pev-id",      args, "DE*EV*0000000001");
var pevMacStr   =        Cli.ParseOptional("--mac",         args, "02:DE:AD:BE:EF:01");
var evsesArg    =        Cli.ParseOptional("--evses",       args, "127.0.0.1:5119");
var toggleMode  =        Cli.ParseOptional("--toggle-mode", args, "auto");      // "auto" | "interactive"
var toggleTgt   = (Byte) Cli.ParseOptional("--toggles",     args, 3);
var cpBusPort   =        Cli.ParseOptional("--cp-bus-port", args, 0);           // 0 = derive from EVSE port

Ui.WriteHeader();

var pevMac      = MACAddress.Parse(pevMacStr);
var listenOn    = new IPEndPoint(IPAddress.Loopback, localPort);
var pevId       = Cli.PadOrTruncate(Encoding.ASCII.GetBytes(pevIdAscii), SLACConstants.StationIdLength);

// --evses accepts a comma-separated list "host:port,host:port,..." so the EV
// can broadcast its CM_SLAC_PARM.REQ to several EVSEs at once. After the first
// frame the transport learns the MAC↔endpoint mapping and unicasts properly.
var evseEndpoints = evsesArg
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Select(ParseEndpoint)
    .ToList();

// CP-toggle bus: by convention the EVSE listens on (its SLAC port + 1000).
// If the EV may face multiple EVSEs, --cp-bus-port can override this.
var firstEvsePort = evseEndpoints[0].Port;
var cpBus         = new IPEndPoint(
                        IPAddress.Loopback,
                        cpBusPort == 0
                            ? firstEvsePort + 1000
                            : cpBusPort
                    );
using var emitter = new UdpToggleEmitter(cpBus);

await using var transport = new UdpSlacTransport(pevMac, listenOn, evseEndpoints);
await transport.StartAsync();

var options = new EvSlacOptions {
    PevId        = pevId,
    ToggleSource = emitter,
    // Interactive mode = human pressing keys; let them take their time.
    ValidateRoundTimeout = toggleMode == "interactive"
        ? TimeSpan.FromSeconds(30)
        : TimeSpan.FromMilliseconds(800),
    MaxValidationRounds  = toggleMode == "interactive" ? 30 : 6,
    // For the UDP-bus simulation a no-op chip controller is enough; the
    // simulated transport already delivers frames regardless of NMK state.
    // With real qca7000 hardware, swap in QcaChipController.
    ChipController       = new SimulatedChipController {
        AvlnReadyDelay = TimeSpan.FromMilliseconds(150)
    }
};
await using var session = new EvSlacSession(transport, options);

session.StateChanged             += (_, s) => Ui.Print($"[STATE] {s}",                                 ConsoleColor.Cyan);
session.Log                      += (_, m) => Ui.Print($"        {m}",                                 ConsoleColor.DarkGray);

// When the session enters the Validating state the EVSE has just asked for
// CP-toggles. Drive the emitter accordingly. Auto-mode produces the target
// number of toggles with a small spread; interactive-mode prints a prompt
// and lets the user press <Enter> to fire each toggle manually.
session.StateChanged += async (_, state) =>
{
    if (state != EvSlacState.Validating) return;

    if (toggleMode == "auto")
    {
        Ui.Print($"        [auto] generating {toggleTgt} CP-toggle(s) ...", ConsoleColor.Yellow);
        for (var i = 0; i < toggleTgt; i++)
        {
            await Task.Delay(120);
            emitter.Emit();
            Ui.Print($"        [auto] toggle {i + 1}/{toggleTgt}",          ConsoleColor.DarkYellow);
        }
    }
    else  // interactive
    {
        Ui.Print($"        [interactive] press <Enter> to fire a toggle, 'd' for done.", ConsoleColor.Yellow);
        _ = Task.Run(() =>
        {
            while (session.State == EvSlacState.Validating)
            {
                var line = Console.ReadLine();
                if (line is null || line.StartsWith('d')) break;
                emitter.Emit();
                Ui.Print($"        [interactive] toggle (count = {((IToggleSource) emitter).GetCount()})",
                    ConsoleColor.DarkYellow);
            }
        });
    }
};
session.CandidateAdded           += (_, c) => Ui.Print($"[+EVSE] {c.EVSEMACAddress}",                                     ConsoleColor.Yellow);
session.CandidateProfileReceived += (_, c) => Ui.Print($"[PROF]  {c.EVSEMACAddress}: avg {c.AverageAttenuation,5:F1} dB", ConsoleColor.Yellow);

Ui.Print($"PEV MAC : {pevMac}",                                       ConsoleColor.White);
Ui.Print($"Listen  : {listenOn}",                                     ConsoleColor.White);
Ui.Print($"EVSEs   : {string.Join(", ", evseEndpoints)}",             ConsoleColor.White);
Ui.Print($"PEV_ID  : {pevIdAscii}",                                   ConsoleColor.White);
Ui.Print("",                                                          ConsoleColor.White);
Ui.Print("Starting SLAC matching ...",                                ConsoleColor.Yellow);
Ui.Print("",                                                          ConsoleColor.White);

try
{

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
    var result = await session.RunAsync(cts.Token);

    Ui.Print("",                                                      ConsoleColor.White);
    Ui.Print("=== ALL CANDIDATES ===",                                ConsoleColor.White);
    Ui.Print("",                                                      ConsoleColor.White);
    Ui.Print("    EVSE MAC             EVSE_ID                Avg dB     Bar",       ConsoleColor.White);
    Ui.Print("    ───────────────────────────────────────────────────────────────",  ConsoleColor.DarkGray);

    var ranked = result.AllCandidates
        .OrderBy(c => c.AverageAttenuation ?? double.MaxValue)
        .ToList();

    foreach (var c in ranked)
    {
        var marker = c.EVSEMACAddress == result.Winner.EVSEMACAddress ? "►" : " ";
        var color  = c.EVSEMACAddress == result.Winner.EVSEMACAddress ? ConsoleColor.Green : ConsoleColor.Gray;
        var avg    = c.AverageAttenuation;
        var avgStr = avg is null ? "  n/a" : $"{avg:F1}";
        var idStr  = c.EvseId is null ? "(no profile)" : Cli.SafeAscii(c.EvseId);
        var bar    = avg is null ? "" : Bar(avg.Value);

        Ui.Print($"  {marker} {c.EVSEMACAddress}  {idStr,-22} {avgStr,6}     {bar}", color);
    }

    Ui.Print("",                                                               ConsoleColor.White);
    Ui.Print("=== MATCHING SUCCESSFUL ===",                                    ConsoleColor.Green);
    Ui.Print($"  EVSE MAC : {result.Winner.EVSEMACAddress}",                   ConsoleColor.Green);
    Ui.Print($"  EVSE_ID  : {Cli.SafeAscii(result.MatchCnf.EvseId)}",          ConsoleColor.Green);
    Ui.Print($"  RunID    : {result.RunId}",                                   ConsoleColor.Green);
    Ui.Print($"  NID      : {Convert.ToHexString(result.MatchCnf.Nid)}",       ConsoleColor.Green);
    Ui.Print($"  NMK      : {Convert.ToHexString(result.MatchCnf.Nmk)}",       ConsoleColor.Green);
    Ui.Print("",                                                               ConsoleColor.White);
    Ui.Print($"PEV picked the EVSE with the lowest average attenuation",       ConsoleColor.White);
    Ui.Print($"and ignored {ranked.Count - 1} other candidate(s).",            ConsoleColor.White);

    return 0;

}
catch (Exception ex)
{
    Ui.Print("",                                                               ConsoleColor.White);
    Ui.Print($"=== MATCHING FAILED ===",                                       ConsoleColor.Red);
    Ui.Print($"  {ex.GetType().Name}: {ex.Message}",                           ConsoleColor.Red);

    if (session.Candidates.Count > 0)
    {
        Ui.Print("",                                                           ConsoleColor.White);
        Ui.Print($"  Saw {session.Candidates.Count} EVSE(s) before failure:",  ConsoleColor.Red);
        foreach (var c in session.Candidates.Values)
        {
            var avgStr = c.AverageAttenuation is null ? "no profile" : $"{c.AverageAttenuation:F1} dB";
            Ui.Print($"    {c.EVSEMACAddress}  ({avgStr})",                    ConsoleColor.Red);
        }
    }
    return 1;
}

static IPEndPoint ParseEndpoint(string s)
{
    var parts = s.Split(':');
    if (parts.Length != 2)
        throw new FormatException($"Invalid endpoint '{s}', expected host:port");
    return new IPEndPoint(IPAddress.Parse(parts[0]), int.Parse(parts[1]));
}

static string Bar(double avgDb)
{
    // Map ~30..70 dB to a 0..30-cell bar so the comparison is visually obvious.
    var clamped = Math.Clamp(avgDb, 25, 75);
    var cells   = (int) Math.Round((clamped - 25) / 50.0 * 30);
    return new string('█', cells);
}

// =================================================================== helpers

file static class Ui
{
    public static void Print(string s, ConsoleColor c)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = c;
        Console.WriteLine(s);
        Console.ForegroundColor = prev;
    }

    public static void WriteHeader()
    {
        Print("",                                                                 ConsoleColor.White);
        Print("================================================================", ConsoleColor.Magenta);
        Print(" SLAC EV demo  -  HomePlug GreenPHY / ISO 15118-3 PEV side       ", ConsoleColor.Magenta);
        Print(" (multi-EVSE selection)                                          ", ConsoleColor.Magenta);
        Print("================================================================", ConsoleColor.Magenta);
        Print("",                                                                 ConsoleColor.White);
    }
}

file static class Cli
{
    public static T ParseOptional<T>(string key, string[] args, T fallback)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == key)
                return (T) Convert.ChangeType(args[i + 1], typeof(T))!;
        return fallback;
    }

    public static byte[] PadOrTruncate(byte[] input, int length)
    {
        var result = new byte[length];
        Array.Copy(input, result, Math.Min(input.Length, length));
        return result;
    }

    public static string SafeAscii(byte[] data)
    {
        var sb = new StringBuilder();
        foreach (var b in data)
        {
            if (b == 0) break;
            sb.Append(b >= 0x20 && b < 0x7F ? (char) b : '.');
        }
        return sb.ToString();
    }

}
