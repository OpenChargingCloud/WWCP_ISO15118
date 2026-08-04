/*
 * Copyright (c) 2021-2025 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP ISO/IEC 15118 <https://github.com/OpenChargingCloud/WWCP_ISO15118>
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

using System.Diagnostics;

using cloud.charging.open.protocols.ISO15118.EXI.Simulation;

// Usage: dotnet run [ac|dc] [--slow] [--break-sequence]
//   ac | dc          which energy-transfer mode to simulate (default: dc)
//   --slow           SECC stalls on AuthorizationReq → trips the EV message timeout
//   --break-sequence EV sends PowerDeliveryReq out of order → trips the SECC sequence guard

var mode = args.Contains("ac", StringComparer.OrdinalIgnoreCase) ? ChargingMode.Ac : ChargingMode.Dc;
var slow = args.Contains("--slow");
var breakSeq = args.Contains("--break-sequence");

Console.WriteLine($"ISO 15118-2 {mode.ToString().ToUpperInvariant()} charging session — every line is a real EXI round-trip\n");

var secc = new Secc(mode,
    sequenceTimeout: TimeSpan.FromSeconds(60),
    authDelay: slow ? TimeSpan.FromSeconds(3) : TimeSpan.Zero);
var wire = new Wire(secc);
var evcc = new Evcc(wire, mode, breakSequence: breakSeq);

var sw = Stopwatch.StartNew();
try
{
    evcc.Run();
    Console.WriteLine(
        $"\n✓ Session complete — {wire.Exchanges} exchanges, {wire.BytesOnWire} bytes on the wire, {sw.ElapsedMilliseconds} ms.");
}
catch (SessionAborted ex)
{
    Console.WriteLine($"\n✗ Session aborted: {ex.Message}");
    Environment.ExitCode = 1;
}
