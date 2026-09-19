# WWCP ISO/IEC 15118 — 10BASE-T1S

The link below a Megawatt Charging System coupler, emulated: a multidrop bus
with the charging station as coordinator, the vehicle as one node on it, and
the temperature sensors in the coupler's pins as the others.

```
  EVSE (node 0)  ──BEACON──▶  ┌─────────┬─────────┬─────────┬─────────┬─────────┬───────────┐
                              │ TO → EV │ TO → S1 │ TO → EV │ TO → S2 │ TO → EV │ discovery │  ... BEACON ...
                              └─────────┴─────────┴─────────┴─────────┴─────────┴───────────┘
                                 frame    reading    yield    reading    frame     JOIN?
```

CCS runs ISO 15118 over a powerline modem, and SLAC — the pairing stage the
[SLAC library](../WWCP_ISO15118_SLAC/README.md) implements — is how a vehicle
and a station find each other on it. MCS has no powerline. Its link is IEEE
802.3cg **10BASE-T1S**: one twisted pair, up to eight nodes, 10 Mbit/s, half
duplex, and **PLCA** (Physical Layer Collision Avoidance, Clause 148) keeping
the eight off each other's frames. That is what this library emulates, and it
is a different shape from SLAC in three ways that matter:

- **It is a bus, not a pair.** A sensor screwed into the DC+ pin is a node on the
  same wire as the vehicle, and the station talks to both the same way.
- **It polls.** Node 0 sends a BEACON and then hands every node a transmit
  opportunity in turn; a node may send one frame in its own and never
  otherwise. So every cycle, every node is asked — which is exactly what a
  station wants of a temperature sensor.
- **It has priorities, by weight.** A node of weight 3 gets three opportunities
  per cycle, interleaved — `EV S1 EV S2 EV` — so the vehicle, which carries the
  whole of ISO 15118-20, is asked more often without any sensor being asked
  less than once.


## What is emulated, and what is not

Frames are real Ethernet II frames (EtherType `0x88B5`, the IEEE local-experimental
one), and the emulated medium is one **UDP multicast group** every node joins:
every frame reaches every node, nobody needs a list of peers, and a node that
joins knows nothing about who else is there. The real medium is a 10BASE-T1S
adapter through **AF_PACKET** on Linux, the way the SLAC library reaches a
powerline modem — the other `IT1STransport`, under the same coordinator and
followers, with nothing above it to change.

The PLCA signalling — BEACON, transmit opportunity — happens below the MAC on the
real PHY and no frame ever sees it. Over UDP it has to become frames, and they
are: three message types that a capture reads as the cycle they record. Two
further departures from the standard, both deliberate and both named in the
code:

| The standard | The emulation | Why |
|---|---|---|
| Node identifiers are configured by hand | A **discovery opportunity** at the end of each cycle, in which a node without an identifier may JOIN and be ASSIGNed one | A bench bus has nodes that come and go. Two asking at once is the one collision left, settled by random backoff. |
| A node with nothing to send lets its opportunity expire (32 bit times) | An idle node sends a **YIELD** | Over UDP a silence is fifty milliseconds nobody can tell from a node that has gone. The coordinator still times out, for the nodes that really have. |

The timings are the emulation's — an opportunity of 50 ms, a cycle every 200 ms
when the bus is quiet — and are stated as such. The shape of the cycle is the
standard's.


## Two media, one decision

A node is configured with four things — a kind, an interface, a group and an
address — and `T1STransports.Open` turns them into a medium, or into the one
sentence that says why there is none:

| `T1STransportKind` | What it is |
|---|---|
| `none` | no bus: a CCS vehicle, a station without an MCS coupler |
| `auto` | a real adapter where there is one — AF_PACKET on Linux, on the named interface — and **nothing anywhere else** |
| `afpacket` | a real adapter, by interface name. Linux, and CAP_NET_RAW: root, or `setcap cap_net_raw=eip` on the binary |
| `udp` | the emulated medium, on a group and port. Asked for explicitly, never chosen by itself |

The rule behind `auto` is the SLAC side's: only the real medium is ever chosen
by itself, because a node that quietly joined a multicast group on a machine
without an adapter would look attached and be talking to nothing a coupler has.
So `auto` on a laptop *declines* — no medium, a reason for the log, no error —
where `afpacket` on the same laptop *fails*, which is the difference between
"there is none here" and "you asked for one that cannot be had". `T1SMedium`
carries all three answers: `IsOpen`, a `Reason`, and an `Error` only on the
third.

On a real segment the PHY does the PLCA and the adapter has a node identifier it
was configured with; the BEACON and opportunity frames this library sends over
such a segment are then an application protocol on top of a medium that already
keeps the nodes off each other — redundant there, harmless, and the polling of
the sensors is exactly what a station still needs.

The vehicle carries the four as session settings — `t1sTransport`, `t1sBus`,
`t1sInterface`, `t1sWeight` — with two defaults worth knowing: a bus named
without a transport means `udp`, because a group is a thing only the emulated
medium has; and the adapter is the V2G interface unless another is named,
because on a real MCS the T1S link *is* the link. The station carries them in
`V2GOptions.T1S`.


## The pieces

| | |
|---|---|
| `EthernetFrame`, `Messages/` | the framing and the nine message types, with a codec that answers null for anything malformed — it is the first thing every datagram off a shared medium meets |
| `Transport/T1STransports`, `T1STransportOptions` | the decision: which of the two media, from four fields, and what to say when it cannot be had |
| `Transport/UdpMulticastT1STransport` | the emulated medium: join a group, hear everything, filter by destination like a network card |
| `Transport/Linux/AfPacketT1STransport` | the real one: a raw socket on the adapter, bound to our EtherType, with the same filtering |
| `PLCA/PlcaCoordinator` | node 0: BEACON, opportunities in weighted order, the register of nodes, lost-node detection, and what arrived out of turn |
| `PLCA/PlcaFollower` | every other node: wait for a BEACON, JOIN, answer every opportunity — from a queue, from a supplier, or with a YIELD — and notice a coordinator that stops |
| `PLCA/PlcaSchedule` | the smooth weighted round-robin that spreads a heavy node out rather than bunching it |
| `Nodes/TemperatureSensorNode`, `Nodes/ThermalModel` | a sensor with one number to report, and the lump of metal that number is the temperature of: heats with I², cools towards ambient, first order |
| `Monitoring/CableThermalMonitor` | the coordinator's opinion of every sensor — normal, warning, overload, lost — said once per change, with hysteresis, and a lost sensor counted as an alarm |


## Using it

A station:

```csharp
var opened = T1STransports.Open(new T1STransportOptions(T1STransportKind.UDP));   // or Auto, AfPacket + "eth1"
if (!opened.IsOpen) { Log(opened.Reason); return; }                                // declined, or failed with opened.Error

await using var medium      = opened.Transport!;
await using var coordinator = new PlcaCoordinator(medium, new PlcaCoordinatorOptions(Name: "EVSE"));

var monitor = new CableThermalMonitor();          // 70 °C warning, 90 °C overload, 5 K hysteresis

coordinator.ReadingReceived += (_, r) => monitor.Observe(r.Node, r.Reading, DateTimeOffset.UtcNow);
coordinator.NodeLost        += (_, n) => monitor.Lost(n, DateTimeOffset.UtcNow);
monitor.StateChanged        += (_, c) => { if (c.IsAlarm) StopCharging(c.Node.Name); };

await medium.StartAsync();
await coordinator.StartAsync();
```

A sensor in a pin:

```csharp
await using var pin = new TemperatureSensorNode(new UdpMulticastT1STransport(T1SConstants.RandomLocalMac()),
                                                new TemperatureSensorOptions("DC+ pin"));
await pin.StartAsync();
pin.Current_A = 800;                              // 800 A through a 500 A pin: overloaded within a minute
```

A vehicle:

```csharp
var ev = new PlcaFollower(medium, new PlcaFollowerOptions(T1SNodeRole.Vehicle, "truck", RequestedWeight: 3));
await ev.StartAsync();
await ev.WaitUntilAttachedAsync(TimeSpan.FromSeconds(10));
ev.Enqueue(new Data(bytes));                      // goes out in the vehicle's next opportunity
```

The [EV](https://github.com/OpenChargingCloud/EV) library's `V2GLink.AttachAsync`
and the [ChargingStation](https://github.com/OpenChargingCloud/ChargingStation)
library's `V2GOptions.T1S` are the two ends of this wired into the two programs,
and [EVChargingTestEnvironment](https://github.com/OpenChargingCloud/EVChargingTestEnvironment)'s
`--mcs` runs the whole bench: station, vehicle, pins, and an overload.


## The tests

```bash
dotnet test WWCP_ISO15118_T1S_Tests
```

The framing and the schedule are tested as arithmetic; the thermal model and
the monitor as a pin heated for an hour in a millisecond. `T1S_BusTests` runs a
whole bus over real sockets — join, poll, overload and clear, a sensor that dies
without saying so, a node that leaves, a rogue that speaks out of turn, and a
coordinator that stops — each on a group and port of its own. They need IPv4
multicast with loopback on whatever interface the operating system picks, which
every laptop has and a container without a multicast route does not.
`T1S_TransportFactoryTests` opens the emulated medium for real and asks for the
adapter where there is none, which on every machine without one — and on every
Linux without CAP_NET_RAW — is the answer worth testing; a bus over a real
adapter has not been run, for want of one.
