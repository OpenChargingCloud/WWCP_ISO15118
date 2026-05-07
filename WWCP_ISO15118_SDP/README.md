# WWCP ISO/IEC 15118 SDP

`WWCP_ISO15118_SDP` implements the **SECC Discovery Protocol (SDP)** used by **ISO 15118-2** and **ISO 15118-20**.

SDP is the discovery step that happens **after the PLC link is available** and before the higher-layer V2G application communication starts. Its job is simple but essential:

- the **EVCC** asks whether a charging station service is available on the current link,
- the **SECC** answers with the IPv6 address, TCP port, and security mode to use,
- the EV can then open the actual V2G connection and continue with SAP / ISO 15118 messaging.

This library provides the message model, a practical EV-side client, a practical SECC-side server, and additional hooks for malformed-frame testing and pentest scenarios.

## What this library does

The library provides:

- **SDP message types** for wired discovery
- **wireless SDP message types** for ISO 15118-20 scenarios
- an **EVCC-side SDP client**
- an **SECC-side SDP server**
- **V2GTP wrapping/unwrapping** integration for SDP payloads
- **policy knobs** for TLS / no-TLS acceptance
- **duplicate-response handling**
- **request/response filtering and transformation hooks**
- **malformed-frame and fuzzing helpers**

## What SDP is used for

Once SLAC and AVLN setup are complete, the EV still needs to know:

- which IPv6 address the SECC wants to be contacted on,
- which TCP port the SECC listens on,
- whether TLS is required or offered,
- which transport protocol is expected.

SDP answers exactly those questions.

In practical terms:

1. the EVCC sends a multicast UDP discovery request on the V2G interface,
2. the SECC listens on the standard SDP port,
3. the SECC replies with its endpoint information,
4. the EVCC validates the response against local policy,
5. the EVCC connects to the announced TCP endpoint.

## Scope of this library

This project implements the **discovery phase only**.

It does **not** implement:

- SLAC / PLC pairing
- SAP negotiation
- EXI session messaging
- charging control messages
- TLS session establishment itself

Those belong to later layers in the ISO 15118 stack.

## Main components

### `EVCC_SDPClient`

The EV-side discovery client.

It:

- sends `SDP_Request` datagrams to `FF02::1`
- waits for `SDP_Response` messages
- validates received responses against configured policy
- supports retry and timeout behavior
- can either accept the first valid response or collect multiple responses
- exposes events for sent requests, received responses, and malformed responses

Important behavior:

- designed for **sequential discoveries** on one socket
- not intended for concurrent discovery operations on the same instance
- rejects invalid or policy-disallowed responses before returning success

### `SECC_SDPServer`

The charging-station-side SDP responder.

It:

- binds a UDP IPv6 socket on port `15118`
- joins the link-local multicast group on the configured V2G interface
- listens for incoming `SDP_Request` frames
- validates requests against configured policy
- replies with an `SDP_Response` advertising the local SECC endpoint
- exposes events for request reception, malformed requests, and sent responses

Important behavior:

- advertises a configurable IPv6 address and TCP port
- can offer TLS or no-TLS depending on policy
- supports test-oriented behaviors like delayed or duplicated responses

## Message model

### Wired SDP messages

The core message types are in `Messages/`.

#### `SDP_Request`
The EVCC asks whether a SECC is available on the current link.

Payload fields:

- `Security`
- `TransportProtocol`
- `Version`

The wired request format is shared by ISO 15118-2 and ISO 15118-20.

#### `SDP_Response`
The SECC answers with the endpoint information the EVCC should connect to.

Payload fields:

- `SeccIPAddress`
- `SeccPort`
- `Security`
- `TransportProtocol`
- `Version`

The response includes the SECC's IPv6 address and TCP port, typically using a **link-local IPv6 address** on the V2G interface.

### Wireless SDP messages

The project also contains ISO 15118-20 wireless discovery message models:

- `SDP_RequestWireless`
- `SDP_ResponseWireless`

These extend discovery with additional wireless-specific fields such as coupling information and identifiers.

At the moment, the server implementation is centered on the **basic wired SDP flow**. The presence of the wireless message types makes the library ready for broader SDP handling and pentest or parser scenarios.

## Under the hood: V2GTP

SDP messages are not sent as plain payloads alone. They are wrapped inside **V2GTP frames**.

This library relies on the related V2GTP project for:

- payload type handling
- frame parsing
- frame serialization
- validation of V2GTP headers

That means this library works at the correct protocol boundary:

- build or parse `SDP_Request` / `SDP_Response`
- wrap or unwrap them in `V2GTP_Frame`
- send or receive them over UDP/IPv6 on the V2G interface

## Network behavior

### EVCC side

The client sends discovery requests to the standard SDP multicast destination on the configured interface and waits for responses.

The response is then validated for things like:

- TLS policy
- expected transport protocol
- non-zero SECC port
- link-local SECC address, if required by policy

### SECC side

The server listens on UDP port `15118`, joins the interface-local multicast group, and sends responses back to the requesting EVCC endpoint.

The server can be configured to:

- reject no-TLS requests
- limit accepted protocol versions
- filter which requests should be answered
- alter outgoing responses for testing
- delay or duplicate responses

## Public configuration points

### `EVCC_SDPClientOptions`

Important options include:

- `Interface` — the V2G network interface to use
- `RequestedSecurity` — usually TLS
- `RequestedTransport` — normally TCP
- `PerAttemptTimeout`
- `MaxRetries`
- `TotalDeadline`
- `RejectNoTlsResponses`
- `RequireLinkLocalSeccAddress`
- `DuplicateStrategy`
- `RawPayloadSerializer`
- `ResponseFilter`

This allows the EVCC behavior to be strict for production-like use, or intentionally flexible for testing.

### `SECC_SDPServerOptions`

Important options include:

- `Interface` — the V2G network interface to listen on
- `SeccIPAddressOverride`
- `SeccPort`
- `AcceptedVersions`
- `OfferedSecurity`
- `RejectNoTlsRequests`
- `RequestFilter`
- `ResponseTransformer`
- `RawPayloadSerializer`
- `ResponseDelay`
- `ResponseDuplicates`
- `AnswerUnicastRequests`

This allows the SECC side to behave standards-compliantly or in a deliberately abnormal way for interoperability and security testing.

## Discovery result model

The EVCC client returns typed results describing the outcome of discovery.

These include:

- `SDP_DiscoverySuccess`
- `SDP_DiscoveryRejected`
- `SDP_DiscoveryTimeout`

This makes it easy for callers to distinguish between:

- a valid SECC being found,
- responses arriving but being rejected by policy,
- or no valid answer arriving at all.

## Pentest and fuzzing support

A notable part of this library is that it is built not only for normal discovery, but also for **robustness testing**.

### `SDP_FrameInjector`

This component allows sending arbitrary SDP-related datagrams directly on the V2G interface.

Typical use cases:

- spoofed `SDP_Response` races
- TLS downgrade scenarios
- malformed V2GTP headers
- payload-length mismatch tests
- unknown payload-type tests
- discovery flooding and DoS experiments

In addition, both client and server offer hooks for:

- custom raw payload serialization
- filtering accepted messages
- mutating responses before serialization
- collecting malformed frame diagnostics

This makes the library useful not only for implementation, but also for validation, security reviews, and interoperability testing.

## Typical usage flow

### EVCC side

Typical integration steps:

1. identify the V2G / PLC network interface
2. create `EVCC_SDPClientOptions`
3. construct `EVCC_SDPClient`
4. call `Discover()`
5. if discovery succeeds, connect to the returned SECC IPv6 endpoint over TCP
6. continue with SAP / ISO 15118 session setup

### SECC side

Typical integration steps:

1. identify the V2G / PLC network interface
2. create `SECC_SDPServerOptions`
3. construct `SECC_SDPServer`
4. call `Start()`
5. wait for incoming requests
6. serve SDP responses that point to the actual ISO 15118 TCP endpoint

## Design notes

A few details of the implementation are important:

- The client and server use **IPv6 UDP sockets** bound to the selected V2G interface.
- The discovery step is intentionally **small and isolated**, which makes it easy to embed in larger EVCC or SECC implementations.
- The client includes **strict validation logic** to reduce the risk of accepting obviously invalid or off-link responses.
- The server is intentionally configurable enough for both **production-like operation** and **pentest simulation**.
- Wireless SDP messages are modeled separately from the basic wired messages.

## Project structure

Important folders:

- `Messages/` — SDP request/response models
- `EVCCClient/` — EV-side discovery client and result types
- `SECCServer/` — SECC-side discovery server and options/events
- `Pentests/` — low-level frame injection helpers

## Related libraries

This project depends on nearby ISO 15118 building blocks, especially:

- `WWCP_ISO15118_V2GTP` — V2GTP framing
- `WWCP_ISO15118_NetworkInterfaces` — V2G interface abstraction
- `WWCP_ISO15118_SLAC` — PLC pairing and AVLN setup before SDP

A normal end-to-end flow is therefore:

1. SLAC establishes the PLC relationship and AVLN
2. SDP discovers the SECC endpoint
3. higher ISO 15118 layers open the actual session

## Target framework

This project targets **.NET 10**.

## License

The source files in this project state that the library is licensed under the **GNU Affero General Public License v3.0 (AGPL-3.0)**.

Check the repository license files and source headers if exact legal wording is required.

## Summary

`WWCP_ISO15118_SDP` is the **discovery layer** of the ISO 15118 stack.

It answers the question:

> "Now that EV and EVSE share a V2G-capable link, where is the SECC service and how should the EV connect to it?"

To do that, the library provides:

- SDP message models
- an EVCC discovery client
- an SECC discovery server
- typed discovery results
- wireless SDP models
- pentest and fuzzing hooks

In short: this library is the **bridge from link-level connectivity to the actual ISO 15118 TCP session**.
