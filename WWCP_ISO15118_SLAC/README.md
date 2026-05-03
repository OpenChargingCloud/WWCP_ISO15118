# WWCP ISO/IEC 15118 SLAC

`WWCP_ISO15118_SLAC` implements the **Signal Level Attenuation Characterization (SLAC)** part of **ISO/IEC 15118-3 / HomePlug Green PHY**.

In an EV charging setup, SLAC is the protocol phase that allows an **electric vehicle (EV / PEV)** and an **EVSE / charging station** to find each other on the power line, measure link quality, select the best partner, and negotiate the PLC network credentials needed to bring up the **AVLN**. Once that is done, higher layers such as **SDP** and the rest of **ISO 15118** can start.

This library focuses on that transition layer between:

- raw HomePlug Green PHY management messages,
- EV / EVSE SLAC state machines,
- optional validation via CP toggling,
- AVLN join setup,
- and test/simulation transports.

## What this library does

The library provides:

- **EV-side SLAC matching** via `EvSlacSession`
- **EVSE-side SLAC matching** via `EvseSlacSession`
- **multi-session EVSE listener support** via `EvseSlacListener`
- **encoding/decoding** of SLAC / HPGP management messages
- **candidate ranking/selection** for multi-EVSE environments
- optional **CM_VALIDATE** support for anti-theft / network-mode validation
- **AVLN key programming hooks** via `IPlcChipController`
- a **UDP-based simulation transport**
- a **Linux AF_PACKET transport** for real raw L2 / PLC interfaces
- a **hybrid bridge** between real PLC traffic and simulated UDP peers

## What it does not do

This library does **not** implement the complete ISO 15118 communication stack.

Its responsibility ends once SLAC has successfully finished and the local PLC chip has joined the negotiated AVLN. After that, the next layers typically take over:

- IPv6 over PLC
- SDP
- ISO 15118 application protocols

## Main protocol flow

At a high level, the implemented flow is:

1. **EV broadcasts `CM_SLAC_PARM.REQ`**
2. **One or more EVSEs answer with `CM_SLAC_PARM.CNF`**
3. **EV broadcasts `CM_START_ATTEN_CHAR.IND`**
4. **EV broadcasts `CM_MNBC_SOUND.IND`** multiple times
5. **EVSEs compute attenuation and broadcast `CM_ATTEN_CHAR.IND`**
6. **EV selects the best EVSE**
7. **EV unicasts `CM_ATTEN_CHAR.RSP`** to the winner
8. Optional **`CM_VALIDATE.REQ/CNF`** exchange
9. **EV sends `CM_SLAC_MATCH.REQ`**
10. **EVSE returns `CM_SLAC_MATCH.CNF`** with `NID` and `NMK`
11. Both sides can **program their local PLC chip** and wait until the **AVLN** is ready
12. Higher protocols can continue over IPv6

## Core building blocks

### State machines

#### `EvSlacSession`
Implements the EV-side matching workflow.

Notable behavior:

- collects `CM_SLAC_PARM.CNF` from multiple EVSEs
- collects `CM_ATTEN_CHAR.IND` profiles
- selects the best EVSE using an `IEVSESelector`
- optionally performs `CM_VALIDATE`
- receives `NID`/`NMK` via `CM_SLAC_MATCH.CNF`
- can program the local PLC chip via `IPlcChipController`

#### `EvseSlacSession`
Implements the EVSE-side matching workflow for a single PEV.

Notable behavior:

- responds to one EV's `RunId`
- sends `CM_SLAC_PARM.CNF`
- collects sounding messages
- computes or simulates attenuation data
- optionally runs validation
- returns `CM_SLAC_MATCH.CNF`
- can program the local PLC chip via `IPlcChipController`

#### `EvseSlacListener`
Long-running listener for real EVSE scenarios with multiple vehicles.

It:

- subscribes to an `ISlacTransport`
- watches for incoming `CM_SLAC_PARM.REQ`
- creates one `EvseSlacSession` per `RunId`
- dispatches subsequent frames to the correct session
- exposes events for session start, completion and failure

### Message model

The `Messages/` folder contains the managed representations of the HomePlug Green PHY SLAC messages, including:

- `SLACParamReq` / `SLACParmCnf`
- `StartAttenCharInd`
- `MnbcSoundInd`
- `AttenCharInd` / `AttenCharRsp`
- `ValidateReq` / `ValidateCnf`
- `SlacMatchReq` / `SlacMatchCnf`
- `SetKeyReq` / `SetKeyCnf`

These types are encoded and decoded using `ManagementMessageEntry` and exposed as `DecodedFrame` instances by the transports.

### Transport abstraction

#### `ISlacTransport`
Common abstraction used by both EV and EVSE state machines.

#### `UdpSlacTransport`
A simulation-friendly transport that keeps the SLAC byte format intact while carrying frames over UDP instead of raw Ethernet.

Useful for:

- local development
- automated tests
- multi-process demos
- fuzzing and regression scenarios

#### `AfPacketSlacTransport` (Linux only)
A raw L2 transport using `AF_PACKET` and EtherType `0x88E1`.

Useful for:

- real HomePlug Green PHY hardware
- qca7000-class PLC adapters
- Linux-based integration tests
- direct interaction with real SLAC traffic

This transport requires Linux and typically root privileges or `CAP_NET_RAW`.

### EVSE selection

When multiple EVSEs respond, the EV may need to choose the best partner.

The library provides:

- `IEVSESelector`
- `LowestAverageAttenuationSelector`
- result and candidate models such as `EVSECandidate` and `EVSLACMatchingResult`

The default behavior is to select the EVSE with the **lowest average attenuation**.

### Validation support

Some deployments use **CM_VALIDATE** to verify physical coupling by counting control-pilot toggles.

The library supports this using:

- `IToggleSource`
- `IToggleObserver`
- `SimulatedToggleChannel`
- `UdpToggleEmitter`
- `UdpToggleObserver`
- `AutoToggleSource`
- `AutoToggleObserver`

This is especially useful for:

- simulation
- integration testing
- anti-theft validation experiments
- cross-process demos

### AVLN / PLC chip control

After SLAC completes successfully, both sides may need to program their **local PLC chip** with the negotiated network key.

This is abstracted via `IPlcChipController`.

Provided implementations:

- `SimulatedChipController`
  For UDP simulation. Records the configured `NID` / `NMK` and reports the AVLN as ready immediately or after optional artificial delays.

- `QcaChipController`
  Linux-only implementation for qca7000-class hardware. Sends `CM_SET_KEY.REQ` / `CM_SET_KEY.CNF` to the local chip and waits briefly for the AVLN to come up.

## Hybrid bridge

`SLACBridge` connects a **real Linux PLC interface** with a **UDP simulation bus**.

This allows mixed environments such as:

- one real EVSE and several simulated EVs
- one real EV and simulated EVSEs
- protocol monitoring / sniffing setups
- fuzzing against real hardware
- regression testing with a hardware-in-the-loop component

The bridge forwards:

- **L2 -> UDP** to all known UDP peers
- **UDP -> L2** onto the real interface
- **UDP -> UDP** to other peers in hub mode

## Typical usage modes

### 1. Pure simulation
Use `UdpSlacTransport` on both sides.

Good for:

- fast local development
- demos without PLC hardware
- unit/integration testing
- multi-EVSE selection scenarios

### 2. Real hardware on Linux
Use `AfPacketSlacTransport` and optionally `QcaChipController`.

Good for:

- charging station prototypes
- real PLC lab setups
- conformance-oriented testing
- packet-level troubleshooting

### 3. Mixed mode
Use `SLACBridge` to combine a real raw interface with simulated participants over UDP.

Good for:

- hardware-in-the-loop testing
- fuzzing a real target
- observing real and simulated devices on one logical medium

## Public configuration points

### EV-side options: `EvSlacOptions`
Important settings include:

- `PevId`
- collection windows for `CM_SLAC_PARM.CNF` and `CM_ATTEN_CHAR.IND`
- timeouts for `CM_SLAC_MATCH`
- inter-frame delays for sounding
- optional `ToggleSource` for validation
- optional `ChipController` for AVLN setup
- `AvlnReadyTimeout`

### EVSE-side options: `EvseSlacOptions`
Important settings include:

- `EvseId`
- `Nid`
- `Nmk`
- `NumSounds`
- `TimeOut100Ms`
- message-specific timeouts
- `AttenuationBias` for simulation scenarios
- `RequireValidation`
- `ToggleObserver`
- `RequiredToggles`
- `AbortOnValidationFailure`
- optional `ChipController`
- `AvlnReadyTimeout`

## Minimal integration outline

### EV side

Typical steps:

1. create an `ISlacTransport`
2. start the transport
3. create `EvSlacOptions`
4. construct `EvSlacSession`
5. call `RunAsync()`
6. continue with SDP / ISO 15118 once the AVLN is ready

### EVSE side

Typical steps:

1. create an `ISlacTransport`
2. start the transport
3. create `EvseSlacListener`
4. provide an options factory that returns a fresh `EvseSlacOptions`
5. start the listener
6. observe `SessionStarted`, `SessionCompleted`, and `SessionFailed`

## Design notes

A few implementation details are worth knowing:

- The EV side supports **multiple responding EVSEs** and selects one winner.
- The EVSE listener is designed for **multiple concurrent PEV sessions**.
- The transport layer emits decoded frames through events and keeps the state machines independent from the concrete carrier.
- The simulation transport preserves the original SLAC frame format, so only the medium changes, not the message encoding.
- The AVLN setup is intentionally separated behind an interface, because chip control differs by hardware platform.

## Project structure

Important folders:

- `Messages/` — SLAC and related HomePlug management messages
- `StateMachine/` — EV and EVSE protocol state machines
- `Transport/` — transport abstractions and implementations
- `Transport/Linux/` — Linux raw-socket transport
- `Selection/` — EVSE candidate ranking
- `Validation/` — toggle-based validation helpers
- `AVLN/` — PLC chip control abstractions and implementations
- `Bridge/` — real/simulated network bridging utilities
- `Util/` — helpers

## Target framework

This project targets **.NET 10**.

## License

The source files in this project state that the library is licensed under the **GNU Affero General Public License v3.0 (AGPL-3.0)**.

Check the repository's license files and headers if you need exact legal details before redistribution or integration.

## Summary

If you need the ISO 15118-3 / HomePlug Green PHY **pairing and network bootstrap layer** between EV and EVSE, this library provides the required protocol pieces:

- frame models
- state machines
- transport implementations
- validation helpers
- AVLN key programming hooks
- simulation and bridging support

In short: it is the **SLAC and AVLN bring-up layer** that prepares the PLC link so the rest of ISO 15118 can start.

