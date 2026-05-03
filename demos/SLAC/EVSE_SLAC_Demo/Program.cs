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
using System.Security.Cryptography;

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

using cloud.charging.open.protocols.ISO15118.SLAC;
using cloud.charging.open.protocols.ISO15118.SLAC.Avln;
using cloud.charging.open.protocols.ISO15118.SLAC.StateMachine;
using cloud.charging.open.protocols.ISO15118.SLAC.Transport;
using cloud.charging.open.protocols.ISO15118.SLAC.Validation;

#endregion

var localPort        = Cli.ParseOptional("--port",     args, 5119);
var evseIdAscii      = Cli.ParseOptional("--evse-id",  args, "DE*GEF*E12345678*1");
var evseMacStr       = Cli.ParseOptional("--mac",      args, "02:CA:FE:BA:BE:01");
var bias             = Cli.ParseOptional("--bias",     args, 0);
var validate         = args.Contains("--validate");
var toggles          = (Byte) Cli.ParseOptional("--toggles", args, 3);
var validateTimeout  = Cli.ParseOptional("--validate-timeout", args, 5);   // seconds per round

Ui.WriteHeader();

var evseMac  = MACAddress.Parse(evseMacStr);
var listenOn = new IPEndPoint(IPAddress.Loopback, localPort);
var evseId   = Cli.PadOrTruncate(Encoding.ASCII.GetBytes(evseIdAscii), SLACConstants.StationIdLength);

// EVSE has no bootstrap peers — it learns each PEV from the incoming
// CM_SLAC_PARM.REQ broadcast.
await using var transport = new UdpSlacTransport(evseMac, listenOn);
await transport.StartAsync();

// CP-toggle observer: listens on a UDP "bus" that simulates the CP pilot
// wire. The EV's UdpToggleEmitter sends bytes to the same port; one byte =
// one toggle. CP-bus port = SLAC port + 1000 by convention here.
UdpToggleObserver? toggleObserver = null;
if (validate)
{
    var cpBus = new IPEndPoint(IPAddress.Loopback, localPort + 1000);
    toggleObserver = new UdpToggleObserver(cpBus);
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"Validation ENABLED. CP-bus: {cpBus}, requiring {toggles} toggle(s).");
    Console.ResetColor();
}

await using var listener = new EvseSlacListener(transport, () => {

    // Fresh NMK / NID per matching session — forward secrecy on the
    // in-vehicle PLC link. A real EVSE rotates these per plug-in.
    var nid = new byte[SLACConstants.NidLength];
    var nmk = new byte[SLACConstants.NmkLength];
    RandomNumberGenerator.Fill(nid);
    RandomNumberGenerator.Fill(nmk);

    return new EvseSlacOptions {
        EvseId               = evseId,
        Nid                  = nid,
        Nmk                  = nmk,
        AttenuationBias      = bias,
        RequireValidation    = validate,
        ToggleObserver       = toggleObserver,
        RequiredToggles      = toggles,
        ValidateRoundTimeout = TimeSpan.FromSeconds(validateTimeout),
        MaxValidationRounds  = Math.Max(6, toggles * 3),
        // For the UDP-bus simulation a no-op chip controller is enough; the
        // simulated transport already delivers frames regardless of NMK
        // state. With real qca7000 hardware, swap in QcaChipController.
        ChipController       = new SimulatedChipController {
            AvlnReadyDelay = TimeSpan.FromMilliseconds(150)   // tiny delay to make the state visible
        }
    };

});

var sessionCounter = 0;

listener.SessionStarted += (_, session) => {

    var n   = Interlocked.Increment(ref sessionCounter);
    var tag = Ui.MakeTag(n, session.RunId);

    Ui.Print($"[NEW]  {tag}  PEV {session.PevMac}",                               ConsoleColor.Cyan);

    session.StateChanged += (_, s) => Ui.Print($"  {tag}  {s}",                   ConsoleColor.DarkCyan);
    session.Log          += (_, m) => Ui.Print($"  {tag}  {m}",                   ConsoleColor.DarkGray);

};

listener.SessionCompleted += (_, e) => {
    var tag = Ui.MakeTag(0, e.Session.RunId);
    Ui.Print("",                                                                  ConsoleColor.White);
    Ui.Print($"=== MATCHED  {tag} ===",                                           ConsoleColor.Green);
    Ui.Print($"  PEV MAC : {e.Session.PevMac}",                                   ConsoleColor.Green);
    Ui.Print($"  PEV_ID  : {Cli.SafeAscii(e.Result.PevId)}",                      ConsoleColor.Green);
    Ui.Print($"  RunID   : {e.Session.RunId}",                                    ConsoleColor.Green);
    Ui.Print($"  NID     : {Convert.ToHexString(e.Result.Nid)}",                  ConsoleColor.Green);
    Ui.Print($"  NMK     : {Convert.ToHexString(e.Result.Nmk)}",                  ConsoleColor.Green);
    Ui.Print("",                                                                  ConsoleColor.White);
};

listener.SessionFailed += (_, e) => {
    var tag = Ui.MakeTag(0, e.Session.RunId);
    Ui.Print($"=== FAILED  {tag}: {e.Error.GetType().Name}: {e.Error.Message}",   ConsoleColor.Red);
};

await listener.StartAsync();

Ui.Print($"EVSE MAC: {evseMac}",                                                  ConsoleColor.White);
Ui.Print($"Listen  : {listenOn}",                                                 ConsoleColor.White);
Ui.Print($"EVSE_ID : {evseIdAscii}",                                              ConsoleColor.White);
Ui.Print($"Bias    : {bias:+#;-#;0} dB  (synthetic attenuation offset)",          ConsoleColor.White);
Ui.Print("",                                                                      ConsoleColor.White);
Ui.Print("Listening for PEVs. Press Ctrl-C to stop.",                             ConsoleColor.Yellow);
Ui.Print("",                                                                      ConsoleColor.White);

// Wait for Ctrl-C.
var done = new TaskCompletionSource();
Console.CancelKeyPress += (_, ev) => {
    ev.Cancel = true;
    done.TrySetResult();
};
await done.Task;

Ui.Print("",                                                                      ConsoleColor.White);
Ui.Print("Shutting down ...",                                                     ConsoleColor.Yellow);
toggleObserver?.Dispose();
return 0;

// =================================================================== helpers

file static class Ui
{
    private static readonly object _lock = new();

    public static void Print(string s, ConsoleColor c)
    {
        lock (_lock)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = c;
            Console.WriteLine(s);
            Console.ForegroundColor = prev;
        }
    }

    public static void WriteHeader()
    {
        Print("",                                                                 ConsoleColor.White);
        Print("================================================================", ConsoleColor.Magenta);
        Print(" SLAC EVSE demo  -  HomePlug GreenPHY / ISO 15118-3 EVSE side    ", ConsoleColor.Magenta);
        Print(" (multi-PEV listener)                                            ", ConsoleColor.Magenta);
        Print("================================================================", ConsoleColor.Magenta);
        Print("",                                                                 ConsoleColor.White);
    }

    public static string MakeTag(int sessionNumber, RunId runId)
    {
        // Take the trailing 8 hex chars of the RunID for a stable, short visual ID.
        var s = runId.ToString();              // "0xAABBCCDDEEFF1122"
        var shortId = s.Length >= 10 ? s.Substring(s.Length - 8) : s;
        return sessionNumber > 0
            ? $"#{sessionNumber} {shortId}"
            : $"   {shortId}";
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
