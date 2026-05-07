# WWCP ISO/IEC 15118 V2GTP

`WWCP_ISO15118_V2GTP` implements the **Vehicle-to-Grid Transfer Protocol (V2GTP)** framing layer used by **ISO 15118-2** and **ISO 15118-20**.

V2GTP is the small but essential binary envelope that sits around higher-layer ISO 15118 payloads. It gives receivers enough information to understand:

- which kind of payload is being carried,
- how many bytes belong to that payload,
- and whether the frame header itself is structurally valid.

This library focuses on that framing layer only: **V2GTP headers, complete frames, payload type handling, and parser/validation errors**.

## What this library does

The library provides:

- the **8-byte V2GTP header** model
- a **complete V2GTP frame** model
- **encoding** to wire-format bytes
- **strict parsing** with protocol validation
- **lenient/raw parsing** for diagnostics and pentest tooling
- **payload type definitions** for ISO 15118-2 and ISO 15118-20
- typed **exceptions** for malformed or truncated frames

## What V2GTP is for

In ISO 15118, the application-layer payload is not sent alone. It is wrapped into a V2GTP frame.

That wrapper contains:

- `ProtocolVersion`
- `InverseProtocolVersion`
- `PayloadType`
- `PayloadLength`

This framing is used for traffic such as:

- **SDP** discovery messages
- **SAP / SupportedAppProtocol** negotiation
- **EXI-encoded V2G messages**
- ISO 15118-20 specific stream types such as AC, DC, ACDP, and WPT

So this library is the shared binary transport envelope for higher ISO 15118 layers.

## Scope of this library

This project handles only the **V2GTP framing layer**.

It does **not** implement:

- UDP or TCP socket handling
- SDP message semantics
- SAP negotiation logic
- EXI serialization/deserialization
- charging session state machines

Those concerns belong to surrounding protocol libraries.

## Main building blocks

### `V2GTP_Header`

Represents the fixed **8-byte V2GTP header** that prefixes every V2GTP payload.

Fields:

- `ProtocolVersion`
- `InverseProtocolVersion`
- `PayloadType`
- `PayloadLength`

Capabilities:

- build a standard header via `Standard(...)`
- encode into a span or byte array
- strict parse via `Parse(...)`
- lenient parse via `ParseRaw(...)`
- lightweight probe via `TryParseRaw(...)`
- validate the version pair via `IsVersionValid`

### `V2GTP_Frame`

Represents a full V2GTP message:

- one `V2GTP_Header`
- one payload buffer

Capabilities:

- create a frame from payload type and bytes via `Wrap(...)`
- convert a frame back to bytes via `ToArray()`
- strict parse via `Parse(...)`
- lenient/raw parse via `ParseRaw(...)`

This is the main type to use when a caller needs to round-trip a full V2GTP datagram.

### `V2GTP_PayloadType`

Defines the well-known payload types used by ISO 15118.

Included values cover:

- `ExiMainstream`
- `ExiAC`
- `ExiDC`
- `ExiACDP`
- `ExiWPT`
- `SdpRequest`
- `SdpResponse`
- `SdpRequestWireless`
- `SdpResponseWireless`

Helpers include:

- `IsKnown()`
- `IsSDP()`

### `V2GTP_ProtocolVersion`

Defines the protocol version constants used in the header:

- current version byte: `0x01`
- inverse version byte: `0xFE`

Both ISO 15118-2 and ISO 15118-20 use the same V2GTP version bytes. The revision is **not** distinguished at the V2GTP layer.

## Wire format

The V2GTP header is always 8 bytes long:

- byte 0: protocol version
- byte 1: inverse protocol version
- bytes 2-3: payload type, big-endian
- bytes 4-7: payload length, big-endian

The payload bytes follow immediately after the header.

## Strict vs lenient parsing

A useful feature of this library is that it supports both **strict** and **lenient** parsing modes.

### Strict parsing

Use strict parsing when implementing normal protocol handling.

`V2GTP_Header.Parse(...)` and `V2GTP_Frame.Parse(...)` enforce:

- minimum frame/header size
- correct protocol version/inverse-version pair
- payload length consistency with the received buffer

This is the right mode for production protocol stacks.

### Lenient/raw parsing

Use raw parsing when inspecting malformed traffic or building pentest/fuzz tooling.

`V2GTP_Header.ParseRaw(...)` and `V2GTP_Frame.ParseRaw(...)`:

- accept invalid version bytes
- do not require perfect length consistency
- allow callers to inspect whatever data is available

This is useful for:

- diagnostics
- fuzzing
- negative test cases
- protocol robustness testing

## Exceptions

The library exposes typed exceptions for common framing problems.

### `V2GTP_Exception`
Base type for V2GTP-related parse/encode failures.

### `V2GTP_TruncatedException`
Thrown when the input is shorter than the required 8-byte header.

### `V2GTP_ProtocolVersionException`
Thrown when the version byte and inverse-version byte do not match the expected V2GTP values.

### `V2GTP_PayloadLengthException`
Thrown when the header declares more payload bytes than are actually present in the buffer.

## Typical usage

### Build a frame

Typical flow:

1. prepare the payload bytes from a higher-level protocol
2. choose the matching `V2GTP_PayloadType`
3. call `V2GTP_Frame.Wrap(...)`
4. send the resulting bytes over UDP or TCP

### Parse a received frame

Typical flow:

1. receive bytes from the network
2. call `V2GTP_Frame.Parse(...)`
3. inspect `Header.PayloadType`
4. dispatch the payload to the appropriate higher-level decoder

For example:

- SDP libraries can expect `SdpRequest` or `SdpResponse`
- SAP or EXI layers can expect one of the `Exi*` payload types

## Where this library fits in the stack

This library is a low-level helper used by other ISO 15118 components.

Typical protocol layering looks like this:

1. transport layer provides bytes over UDP or TCP
2. **V2GTP** wraps or unwraps the message envelope
3. higher protocol layers parse the payload
   - SDP
   - SAP
   - EXI-based ISO 15118 messages

So this library is the **framing boundary** between raw network transport and actual ISO 15118 message handling.

## Related libraries in this workspace

This project is typically used together with:

- `WWCP_ISO15118_SDP` — uses V2GTP for SDP request/response frames
- other ISO 15118 messaging libraries that exchange EXI or SAP payloads
- networking code that reads or writes UDP/TCP datagrams

## Design notes

A few details are important:

- V2GTP does not decide which ISO 15118 revision is active.
- Both ISO 15118-2 and ISO 15118-20 share the same basic V2GTP header format.
- Payload interpretation is driven by `PayloadType` and the higher protocol layer.
- The library keeps framing concerns isolated from socket handling and message semantics.
- Raw parsing support makes it suitable for both production code and security testing.

## Project structure

Important files:

- `V2GTP_Header.cs` — header model and parser/encoder
- `V2GTP_Frame.cs` — complete frame model and parser/encoder
- `V2GTP_PayloadType.cs` — known payload types and helpers
- `V2GTP_ProtocolVersion.cs` — version constants
- `Exceptions/` — typed V2GTP parsing exceptions

## Target framework

This project targets **.NET 10**.

## License

The source files in this project state that the library is licensed under the **GNU Affero General Public License v3.0 (AGPL-3.0)**.

Check the repository license files and source headers if exact legal wording is required.

## Summary

`WWCP_ISO15118_V2GTP` is the **binary framing layer** for ISO 15118 communication.

It provides:

- the V2GTP header model
- complete frame wrapping/unwrapping
- payload type definitions
- strict validation for normal use
- lenient parsing for diagnostics and pentests

In short: it is the library that turns raw bytes into a validated **ISO 15118 V2GTP envelope**, and back again.