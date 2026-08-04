# ISO 15118 SupportedAppProtocol schema — provenance

The SupportedAppProtocol schema — the handshake that chooses between ISO 15118-2 and -20
before either has been agreed, which is why it has a namespace of its own and predates both.

- **Source:** [SwitchEV/RISE-V2G](https://github.com/SwitchEV/RISE-V2G),
  `RISE-V2G-Shared/src/main/resources/schemas/`
- **Pinned commit:** `055806d22c591f843186579b9945255793d0800f`

The same tree the ISO 15118-2 schemas came from; see
[`../../Vanaheimr.V2G.Exi.Iso15118_2/Schemas/README.md`](../../Vanaheimr.V2G.Exi.Iso15118_2/Schemas/README.md)
and, for why they are checked in at all, [`../../SCHEMAS.md`](../../SCHEMAS.md).

This one is the diff reference for the whole generator: the AppProtocol codec beside it is
hand-written, and the generated one must agree with it byte for byte.


| file | targetNamespace |
|---|---|
| `V2G_CI_AppProtocol.xsd` | `urn:iso:15118:2:2010:AppProtocol` |
