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

using cloud.charging.open.protocols.ISO15118.AppProtocol;
using Vanaheimr.V2G.Simulation.Framing;
using Vanaheimr.V2G.Simulation.Session;
using Vanaheimr.V2G.Simulation.StateMachines;
using cloud.charging.open.protocols.ISO15118.EXI.Dispatch;

namespace Vanaheimr.V2G.Simulation.Sap
{
    /// <summary>
    /// One protocol a peer is prepared to run, in a SupportedAppProtocol offer: the variant, and for
    /// -20 the power mode that picks the application namespace (…-20:DC / …-20:AC).
    /// </summary>
    /// <remarks>
    /// The EVCC's list order is its preference: entry 0 is offered at Priority 1 (the highest) with
    /// SchemaID 1, entry 1 at Priority 2 with SchemaID 2, and so on. The SECC's list order carries no
    /// priority of its own — the <c>Priority</c> field exists so the <i>EV</i> can rank, and our
    /// station honours that ranking among what it supports. (Deliberately not cited to a requirement
    /// id: this project does not hold the ISO 15118-2 requirement text, and an unchecked
    /// <c>[V2G2-nnn]</c> would read as verified. EVerest's <c>IsoMux</c> does <b>not</b> honour it —
    /// see <c>docs/interop-runs/2026-08-03-everest-isomux-both/</c>.)
    /// </remarks>
    public sealed record SapOffer(ProtocolVariant Protocol, PowerMode Mode = PowerMode.Dc);

    /// <summary>
    /// The SupportedAppProtocol handshake every session starts with, before either side switches to the
    /// negotiated -2/-20 codec (see <see cref="MessageSet.AppProtocol"/>).
    /// </summary>
    /// <remarks>
    /// Two shapes per side. The single-protocol overloads negotiate a fixed protocol — the simulator
    /// usually knows in advance which one it is testing. The list overloads are the real thing: the EVCC
    /// offers every protocol it can run in <b>one</b> request and then runs whichever the station picked,
    /// and the SECC picks the highest-priority entry it supports — which is what a multiplexing station
    /// (EVerest's <c>IsoMux</c>) exists for, and until 2026-08-03 the one case this stack could not be.
    /// </remarks>
    public static class SapHandshake
    {
        private const string Iso2Namespace   = "urn:iso:15118:2:2013:MsgDef";
        private const string Iso20DcNamespace = "urn:iso:std:iso:15118:-20:DC";
        private const string Iso20AcNamespace = "urn:iso:std:iso:15118:-20:AC";

        // The SupportedAppProtocol ProtocolNamespace for -20 is the mode-specific application namespace
        // (…-20:DC / …-20:AC), NOT …-20:CommonMessages — a live Josev interop run rejected the CommonMessages
        // offer (Failed_NoNegotiation); Josev's own -20 DC EVCC offers …-20:DC (see docs/interop-runs/).
        private static string NamespaceFor(ProtocolVariant variant, PowerMode mode) => variant switch
        {
            ProtocolVariant.Iso15118_2  => Iso2Namespace,
            ProtocolVariant.Iso15118_20 => mode == PowerMode.Dc ? Iso20DcNamespace : Iso20AcNamespace,
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "unknown protocol variant"),
        };

        // Version numbers per protocol: ISO 15118-2:2013 MsgDef is protocol version 2.0, the -20 sets
        // are 1.0. A live Josev SECC matches namespace AND major version — offering -2 as "1.0" gets
        // Failed_NoNegotiation (found live 2026-07-22; our own SECC matches on namespace only).
        private static (uint Major, uint Minor) VersionFor(ProtocolVariant variant)
            => variant == ProtocolVariant.Iso15118_2 ? (2u, 0u) : (1u, 0u);

        /// <summary>EVCC side, one fixed protocol: offers exactly <paramref name="wanted"/> (for -20, the
        /// <paramref name="mode"/>-specific namespace), throws <see cref="SessionAborted"/> if the SECC
        /// rejects it.</summary>
        public static async Task RunEvccSideAsync(Stream stream, ProtocolVariant wanted, CancellationToken ct = default, PowerMode mode = PowerMode.Dc)
            => await RunEvccSideAsync(stream, new[] { new SapOffer(wanted, mode) }, ct).ConfigureAwait(false);

        /// <summary>
        /// EVCC side, the multi-protocol offer: every entry in one request, best first, and the state
        /// machine is chosen <b>after</b> the handshake — the caller runs whichever came back.
        /// </summary>
        /// <returns>The offer the SECC accepted, mapped back through the answered SchemaID.</returns>
        public static async Task<SapOffer> RunEvccSideAsync(Stream stream, IReadOnlyList<SapOffer> offers, CancellationToken ct = default)
        {
            if (offers.Count is < 1 or > 20)
                throw new ArgumentException("a SupportedAppProtocol offer carries 1..20 entries.", nameof(offers));

            var req = new SupportedAppProtocolReq(offers.Select((offer, i) =>
            {
                var (major, minor) = VersionFor(offer.Protocol);
                return new AppProtocolEntry(NamespaceFor(offer.Protocol, offer.Mode),
                                            VersionNumberMajor: major, VersionNumberMinor: minor,
                                            SchemaID: (byte) (i + 1), Priority: (byte) (i + 1));
            }).ToArray());

            // 4 KiB: the schema allows 20 entries of up to 100-character namespaces.
            var buf = new byte[4096];
            if (!SupportedAppProtocolCodec.TryEncodeRequest(req, buf, out int n))
                throw new InvalidOperationException("SAP: EXI encode failed (buffer too small?).");

            await V2GTPStream.WriteRawFrameAsync(stream, V2GTP.PayloadType_AppProtocol, buf.AsMemory(0, n), ct).ConfigureAwait(false);

            if (await ReadSapAsync(stream, ct).ConfigureAwait(false) is not SupportedAppProtocolRes res)
                throw new SessionAborted("SAP: expected a SupportedAppProtocolRes.");
            if (res.Code is not (ResponseCode.OK_SuccessfulNegotiation or ResponseCode.OK_SuccessfulNegotiationWithMinorDeviation))
                throw new SessionAborted($"SAP: SECC rejected the protocol offer ({res.Code}).");

            // The SchemaID says *which* of the offered protocols was accepted, and it was read by nobody
            // until the sweep of 2026-08-03: harmless while the offer was a single entry, and a silent
            // protocol mismatch now that it is not — the whole point of a multi-protocol offer is that the
            // answer decides which state machine runs next.
            if (res.SchemaID is not { } schemaId || schemaId < 1 || schemaId > offers.Count)
                throw new SessionAborted(
                    $"SAP: the SECC accepted SchemaID {res.SchemaID?.ToString() ?? "<none>"}, which is not "
                  + $"among the offered ({String.Join(", ", req.AppProtocols.Select(e => e.SchemaID))}).");

            return offers[schemaId - 1];
        }

        /// <summary>SECC side, one fixed protocol: accepts if the EVCC offered <paramref name="accepted"/>,
        /// otherwise replies Failed and throws.</summary>
        public static async Task RunSeccSideAsync(Stream stream, ProtocolVariant accepted, CancellationToken ct = default, PowerMode mode = PowerMode.Dc)
            => await RunSeccSideAsync(stream, new[] { new SapOffer(accepted, mode) }, ct).ConfigureAwait(false);

        /// <summary>
        /// SECC side: picks the highest-priority entry of the EVCC's offer that this station supports
        /// (Priority 1 is the highest), and answers with <b>that entry's</b> SchemaID.
        /// </summary>
        /// <remarks>
        /// The echo is the load-bearing part, and it used to be a literal <c>1</c> — indistinguishable
        /// from correct for as long as every EVCC this station met assigned SchemaID 1, which is exactly
        /// the assumed-value shape the 2026-08-03 sweep hunted: our own EVCC supplies what our own SECC
        /// assumes. A two-entry offer whose second entry wins is what makes it visible.
        /// </remarks>
        /// <returns>The supported offer the station settled on.</returns>
        public static async Task<SapOffer> RunSeccSideAsync(Stream stream, IReadOnlyList<SapOffer> supported, CancellationToken ct = default)
        {
            if (await ReadSapAsync(stream, ct).ConfigureAwait(false) is not SupportedAppProtocolReq req)
                throw new SessionAborted("SAP: expected a SupportedAppProtocolReq.");

            (AppProtocolEntry Entry, SapOffer Offer)? match = null;
            foreach (var entry in req.AppProtocols.OrderBy(e => e.Priority))   // stable: ties keep offer order
            {
                var offer = supported.FirstOrDefault(o => NamespaceFor(o.Protocol, o.Mode) == entry.ProtocolNamespace);
                if (offer is not null)
                {
                    match = (entry, offer);
                    break;
                }
            }

            var res = match is { } m
                ? new SupportedAppProtocolRes(ResponseCode.OK_SuccessfulNegotiation, m.Entry.SchemaID)
                : new SupportedAppProtocolRes(ResponseCode.Failed_NoNegotiation, SchemaID: null);

            var buf = new byte[16];
            if (!SupportedAppProtocolCodec.TryEncodeResponse(res, buf, out int n))
                throw new InvalidOperationException("SAP: EXI encode failed (buffer too small?).");
            await V2GTPStream.WriteRawFrameAsync(stream, V2GTP.PayloadType_AppProtocol, buf.AsMemory(0, n), ct).ConfigureAwait(false);

            if (match is not { } settled)
                throw new SessionAborted(
                    $"SAP: the EVCC offered none of {String.Join(", ", supported.Select(o => NamespaceFor(o.Protocol, o.Mode)))}.");

            return settled.Offer;
        }

        /// <summary>Reads one SupportedAppProtocol frame. SAP shares payload id 0x8001 with the -2 messages
        /// and so is decoded here explicitly, not through the payload-type dispatcher (see
        /// <see cref="V2GTP.PayloadType_AppProtocol"/>).</summary>
        private static async Task<object> ReadSapAsync(Stream stream, CancellationToken ct)
        {
            var (frame, payloadType) = await V2GTPStream.ReadRawFrameAsync(stream, ct).ConfigureAwait(false);
            if (payloadType != V2GTP.PayloadType_AppProtocol)
                throw new SessionAborted($"SAP: expected payload type 0x{V2GTP.PayloadType_AppProtocol:X4}, got 0x{payloadType:X4}.");

            return SupportedAppProtocolCodec.DecodeAny(frame.AsSpan(V2GTP.HeaderSize), out _);
        }
    }
}
