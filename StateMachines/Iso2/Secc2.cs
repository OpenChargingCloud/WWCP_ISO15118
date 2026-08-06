/*
 * Copyright (c) 2021-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of EVSimulatorApp
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

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using cloud.charging.open.protocols.ISO15118_2;
using cloud.charging.open.protocols.ISO15118_2.Generated;
using Vanaheimr.V2G.Simulation.Framing;
using Vanaheimr.V2G.Simulation.Metering;
using Vanaheimr.V2G.Simulation.Ocpp;
using Vanaheimr.V2G.Simulation.Session;
using cloud.charging.open.protocols.ISO15118.EXI.Dispatch;

namespace Vanaheimr.V2G.Simulation.StateMachines.Iso2
{
    /// <summary>Outcome of validating a -2 Plug &amp; Charge signed <c>AuthorizationReq</c>: GenChallenge echo,
    /// reference digest over the body-element fragment, and the ECDSA signature against the contract leaf
    /// stored at PaymentDetails — with the <c>SignedInfo</c> grammar it verified under
    /// (<c>iso2-msgdef</c> = our/cbV2G combined form, <c>xmldsig-standalone</c> = the Josev form).</summary>
    public sealed record Iso2PnCResult(bool ChallengeOk, bool DigestOk, bool SignatureOk,
                                       string SignatureGrammar, string ContractSubject);

    /// <summary>Outcome of validating one signed -2 <c>MeteringReceiptReq</c> (same dual-grammar dance).</summary>
    public sealed record Iso2ReceiptResult(bool DigestOk, bool SignatureOk, string SignatureGrammar);

    /// <summary>Outcome of validating the EV's <c>PowerDeliveryReq(Start)</c> against the SASchedule offer:
    /// the chosen tuple id must be one we offered ([V2G2-773], else <c>FAILED_TariffSelectionInvalid</c>)
    /// and every ChargingProfile entry must stay within the PMax active at its start time ([V2G2-761],
    /// else <c>FAILED_ChargingProfileInvalid</c>).</summary>
    public sealed record Iso2ProfileResult(bool TupleIdOk, bool WithinPMax, byte TupleId, int ProfileEntries);

    /// <summary>
    /// The charge point (SECC) side of an ISO 15118-2 session — a <b>sequence-guarded</b> responder. It
    /// advances through the charging state machine and only accepts the request expected next; anything
    /// out of order is answered with <c>ResponseCode.FAILED_SequenceError</c> in that request's own
    /// response, and the session then ends (<see cref="SequenceErrorAt"/>). It also enforces the SECC
    /// <i>sequence timeout</i>: if the EV goes quiet mid-session for too long, the session is torn down.
    /// Payment: both <c>ExternalPayment</c> (EIM) and <c>Contract</c> (Plug &amp; Charge) are offered — a
    /// Contract EV runs PaymentDetails (contract chain in, GenChallenge out), a <b>signed</b>
    /// AuthorizationReq (verified dual-grammar, see <see cref="Iso2PnCResult"/>), and gets
    /// <c>ReceiptRequired</c> in its charging-status responses, so each loop cycle carries a <b>signed</b>
    /// MeteringReceiptReq (verified the same way).
    /// <see cref="Handle"/> is a pure, synchronous state transition — directly unit-testable without a
    /// socket; <see cref="RunAsync"/> is the thin loop that drives it from a real <see cref="Stream"/>.
    /// </summary>
    public sealed class Secc2(PowerMode mode, TimeSpan sequenceTimeout, TimeProvider clock)
    {
        private enum Phase
        {
            SessionSetup, ServiceDiscovery, PaymentSelection, PaymentDetails, Authorization, ChargeParams,
            CableCheck, PreCharge, PowerOn, Charging, WeldingDetection, SessionStop, Done,
        }

        private Phase _phase = Phase.SessionSetup;
        private byte[] _sessionId = new byte[8];
        private DateTimeOffset _lastSeen = clock.GetUtcNow();

        // ── Plug & Charge session state (set by PaymentServiceSelection/PaymentDetails) ─
        private bool _contract;
        private bool _receiptRequested;   // demand exactly ONE receipt per session (see ChargingStatus)
        private byte[]? _genChallenge;
        private ECDsa? _contractKey;
        private string _contractSubject = "?";
        private MessageHeaderType? _requestHeader;   // the header of the request currently in Dispatch

        /// <summary>The signed-AuthorizationReq verdict, if the EV paid via Contract (null for EIM).</summary>
        public Iso2PnCResult? PnCAuth { get; private set; }

        /// <summary>One verdict per signed MeteringReceiptReq received (Contract sessions only).</summary>
        public List<Iso2ReceiptResult> MeteringReceipts { get; } = new();

        /// <summary>True once the session has reached its terminal (post-SessionStop) phase.</summary>
        public bool IsDone => _phase == Phase.Done;

        /// <summary>The name of the request this session refused as out-of-sequence, or null if it ended the
        /// normal way. <see cref="IsDone"/> is true for both endings, so anything that reports on a session
        /// — a test, an interop fixture — needs this to tell a completed charge from a refused message.
        /// It is what used to live in the <see cref="SessionAborted"/> message the guard threw.</summary>
        public string? SequenceErrorAt { get; private set; }

        /// <summary>True when the session ended with <c>ChargingSession.Pause</c> rather than Terminate —
        /// the caller should keep <see cref="SessionId"/> and offer it as <see cref="ResumeSessionId"/> to
        /// the next <see cref="Secc2"/> instance so the EV can rejoin ([V2G2-740]).</summary>
        public bool Paused { get; private set; }

        /// <summary>The session id this SECC assigned (or rejoined).</summary>
        public byte[] SessionId => _sessionId;

        /// <summary>A paused predecessor's session id: a SessionSetupReq carrying it rejoins the old session
        /// (<c>ResponseCode.OK_OldSessionJoined</c>); anything else starts a fresh one.</summary>
        public byte[]? ResumeSessionId { get; set; }

        /// <summary>When set, a new session is given this id instead of a fresh random one — the seam that
        /// makes a <b>recorded</b> session reproducible (<c>Tests/Traces</c>). The session id travels in every
        /// message header, so a re-recorded trace would otherwise differ in every single frame, and a corpus
        /// whose diff is total is a corpus nobody can review. Null by default and meant to stay null outside
        /// a recording: a predictable session id is a real weakness, not a convenience.</summary>
        public byte[]? FixedSessionId { get; set; }

        /// <summary>Likewise for the Plug &amp; Charge challenge in <c>PaymentDetailsRes</c>, so a recorded
        /// PnC session can be regenerated and diffed. Unlike -20 this never appears in an EIM session, so
        /// today it only matters to a trace that does not exist yet — it is here because the recording seam
        /// belongs next to the thing it pins, not because something needs it now. Null by default: a
        /// predictable challenge defeats what the challenge is for.</summary>
        public byte[]? FixedGenChallenge { get; set; }

        /// <summary>When set, the SECC requests a <b>renegotiation</b> once: the first charging-status
        /// response carries <c>EVSENotification.ReNegotiation</c>, the EV answers
        /// <c>PowerDeliveryReq(Renegotiate)</c> and re-runs ChargeParameterDiscovery ([V2G2-841]).</summary>
        public bool RequestRenegotiation { get; set; }

        /// <summary>How many renegotiation cycles this session ran (EV-initiated or SECC-requested).</summary>
        public int Renegotiations { get; private set; }

        private bool _renegotiationSignalled;   // the notification is sent exactly once
        private bool _renegotiated;             // post-renegotiation: CPD hands to PowerOn (no new CableCheck)

        // ── smart-charging state (see Schedules()) ──
        private SAScheduleListType? _offeredSchedules;   // what ChargeParameterDiscoveryRes offered
        private SignatureType? _responseSignature;       // set by a builder for THIS response's header
        private byte _chosenTupleId = 1;                 // the tuple the EV's PowerDeliveryReq(Start) picked

        /// <summary>When set, the SASchedule offer becomes a two-tuple choice whose SalesTariffs are
        /// <b>digitally signed</b> with this key (the Mobility Operator's, §7.9.2.5 — one header signature,
        /// one reference per tariff). Null (default): the plain single unsigned tuple.</summary>
        public ECDsa? TariffSignKey { get; set; }

        /// <summary>The <c>PowerDeliveryReq(Start)</c> validation verdict (tuple choice + ChargingProfile
        /// against PMax); null until the EV sends one.</summary>
        public Iso2ProfileResult? ChargingProfileCheck { get; private set; }

        public V2G_Message Handle(V2G_Message request)
        {
            var now = clock.GetUtcNow();
            if (_phase is not Phase.SessionSetup && now - _lastSeen > sequenceTimeout)
                throw new SessionAborted($"SECC sequence timeout: EV silent for > {sequenceTimeout.TotalSeconds:0}s");
            _lastSeen = now;

            _requestHeader = request.Header;   // the PnC verify paths need the header signature
            _responseSignature = null;         // a response builder (ChargeParams) may set one
            var (body, next) = Dispatch(request.Body.BodyElement!);
            _phase = next;
            return new V2G_Message(new MessageHeaderType(_sessionId, Notification: null, Signature: _responseSignature), new BodyType(body));
        }

        /// <summary>Reads/handles/replies over <paramref name="stream"/> until the session reaches <see cref="Phase.Done"/>.</summary>
        public async Task RunAsync(Stream stream, CancellationToken ct = default)
        {
            // 4 KiB: a signed two-tuple SASchedule offer (tariffs + header signature) tops ~1 KiB.
            var buf = new byte[4096];
            while (!IsDone)
            {
                var (set, message) = await V2GTPStream.ReadFrameAsync(stream, ct).ConfigureAwait(false);
                if (set != MessageSet.Iso15118_2 || message is not V2G_Message request)
                    throw new SessionAborted($"SECC: expected an ISO 15118-2 frame, got {set}.");

                var reply = Handle(request);
                if (!reply.TryEncode(buf, out int n))
                    throw new InvalidOperationException("EXI encode failed (buffer too small?).");
                await V2GTPStream.WriteFrameAsync(stream, MessageSet.Iso15118_2, buf.AsMemory(0, n), ct).ConfigureAwait(false);
            }
        }

        // The guard: only the (phase, request) pairs below are legal. The wildcard arm rejects the rest.
        private (BodyBaseType Body, Phase Next) Dispatch(BodyBaseType req) => (_phase, req) switch
        {
            (Phase.SessionSetup, SessionSetupReqType) =>
                (NewSession(), Phase.ServiceDiscovery),

            (Phase.ServiceDiscovery, ServiceDiscoveryReqType) =>
                (Discovery(), Phase.PaymentSelection),

            // Contract (Plug & Charge) inserts the PaymentDetails exchange before Authorization;
            // ExternalPayment (EIM) goes straight to Authorization.
            (Phase.PaymentSelection, PaymentServiceSelectionReqType r) =>
                (new PaymentServiceSelectionResType(ResponseCode.OK),
                 (_contract = r.SelectedPaymentOption == PaymentOption.Contract) ? Phase.PaymentDetails : Phase.Authorization),

            (Phase.PaymentDetails, PaymentDetailsReqType r) =>
                (PaymentDetails(r), Phase.Authorization),

            (Phase.Authorization, AuthorizationReqType r) =>
                (Authorize(r), Phase.ChargeParams),

            // After a renegotiation the cable is already checked — the EV proceeds straight from the new
            // ChargeParameterDiscovery to PowerDelivery(Start), DC included (a Josev EVCC does exactly that).
            (Phase.ChargeParams, ChargeParameterDiscoveryReqType) =>
                (ChargeParams(), mode == PowerMode.Dc && !_renegotiated ? Phase.CableCheck : Phase.PowerOn),

            // ── DC-only pre-charge sequence ──
            (Phase.CableCheck, CableCheckReqType) =>
                (new CableCheckResType(ResponseCode.OK, DcEvseStatus(), EVSEProcessing.Finished), Phase.PreCharge),
            (Phase.PreCharge, PreChargeReqType) =>
                (new PreChargeResType(ResponseCode.OK, DcEvseStatus(), Volt(390)), Phase.PowerOn),

            (Phase.PowerOn, PowerDeliveryReqType { ChargeProgress: ChargeProgress.Start } r) =>
                PowerOn(r),

            // ── charging loop (mode-specific request) ──
            (Phase.Charging, CurrentDemandReqType) when mode == PowerMode.Dc =>
                (CurrentDemand(), Phase.Charging),
            (Phase.Charging, ChargingStatusReqType) when mode == PowerMode.Ac =>
                (ChargingStatus(), Phase.Charging),
            // Contract sessions: our charging-status responses set ReceiptRequired, so each loop cycle the
            // EV answers with a signed MeteringReceiptReq — verify it and stay in the charging loop.
            (Phase.Charging, MeteringReceiptReqType r) when _contract =>
                (MeteringReceipt(r), Phase.Charging),

            // Renegotiation ([V2G2-841]): the EV re-opens ChargeParameterDiscovery mid-loop — either on its
            // own or because our charging-status response carried EVSENotification.ReNegotiation.
            (Phase.Charging, PowerDeliveryReqType { ChargeProgress: ChargeProgress.Renegotiate }) =>
                (Renegotiate(), Phase.ChargeParams),

            (Phase.Charging, PowerDeliveryReqType { ChargeProgress: ChargeProgress.Stop }) =>
                (PowerOnOrOff(), mode == PowerMode.Dc ? Phase.WeldingDetection : Phase.SessionStop),

            (Phase.WeldingDetection, WeldingDetectionReqType) =>
                (new WeldingDetectionResType(ResponseCode.OK, DcEvseStatus(), Volt(5)), Phase.SessionStop),

            // A SessionStopReq is legal in *any* phase (ISO 15118-2 §8.4): the EV may abort the session at any
            // time, and the SECC answers gracefully and ends the session rather than raising the sequence
            // guard. Typed on the request, so it only ever matches a SessionStopReq (never the normal flow).
            // ChargingSession=Pause parks the session instead of terminating it (Paused + SessionId let the
            // caller resume it on the next connection).
            (_, SessionStopReqType r) =>
                (SessionStop(r), Phase.Done),

            // Everything else is out of sequence: answered with FAILED_SequenceError, then the session ends.
            _ => (SequenceError(req), Phase.Done),
        };


        /// <summary>
        /// The answer to a request that is legal ISO 15118-2 but not legal <i>now</i>: the response that
        /// pairs with it, carrying <c>FAILED_SequenceError</c> ([V2G2-539]). <see cref="Dispatch"/> pairs it
        /// with <see cref="Phase.Done"/>, so <see cref="RunAsync"/> writes this response and then leaves the
        /// loop — which is the whole of the rule: answer, then terminate.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This used to throw, and the exception message named the very code it was not sending. Nothing
        /// noticed for as long as the only cars on the wire were ours (which never send anything out of
        /// order) and peers that poll only while we answer <c>Ongoing</c>. A tux-evse run then replayed a
        /// real VW that had polled <c>AuthorizationReq</c> twice at a charger answering
        /// <c>Ongoing_WaitingForCustomerInteraction</c>; ours answers <c>Finished</c> at once, so the second
        /// poll arrived a phase late and the connection just closed. A car looking at silence cannot tell a
        /// sequence error from a dead station.
        /// </para>
        /// <para>
        /// The mandatory fields of these responses are the schema's, not ours — a ServiceDiscoveryRes carries
        /// a PaymentOptionList and a ChargeService whatever its response code says — so a refusal repeats
        /// what this session already offered rather than inventing a second story, and states nothing it
        /// cannot mean: no schedules, no challenge, no measurements.
        /// </para>
        /// </remarks>
        private BodyBaseType SequenceError(BodyBaseType req)
        {

            SequenceErrorAt = req.GetType().Name.Replace("Type", "");

            const ResponseCode code = ResponseCode.FAILED_SequenceError;

            return req switch
            {
                SessionSetupReqType             => new SessionSetupResType(code, "DE*ABC*E1", 1_600_000_000L),
                ServiceDiscoveryReqType         => Discovery(code),
                ServiceDetailReqType r          => new ServiceDetailResType(code, r.ServiceID, ServiceParameterList: null),
                PaymentServiceSelectionReqType  => new PaymentServiceSelectionResType(code),
                // A refusal hands out no challenge: the 16 zero bytes are the schema's mandatory field and
                // not an invitation to sign anything.
                PaymentDetailsReqType           => new PaymentDetailsResType(code, new byte[16],
                                                                             clock.GetUtcNow().ToUnixTimeSeconds()),
                AuthorizationReqType            => new AuthorizationResType(code, EVSEProcessing.Finished),
                // No SASchedules with it: the offer is what a *successful* discovery makes, and [V2G2-905]
                // ties the list to EVSEProcessing=Finished on an OK response, not to a refusal.
                ChargeParameterDiscoveryReqType => new ChargeParameterDiscoveryResType(code, EVSEProcessing.Finished,
                                                                                        SASchedules: null,
                                                                                        EvseChargeParameter()),
                CableCheckReqType               => new CableCheckResType(code, DcEvseStatus(), EVSEProcessing.Finished),
                PreChargeReqType                => new PreChargeResType(code, DcEvseStatus(), Volt(0)),
                PowerDeliveryReqType            => PowerDeliveryRes(code),
                CurrentDemandReqType            => new CurrentDemandResType(code, DcEvseStatus(),
                                                       EVSEPresentVoltage: Volt(0), EVSEPresentCurrent: Amp(0),
                                                       EVSECurrentLimitAchieved: false, EVSEVoltageLimitAchieved: false,
                                                       EVSEPowerLimitAchieved: false,
                                                       EVSEMaximumVoltageLimit: null, EVSEMaximumCurrentLimit: null,
                                                       EVSEMaximumPowerLimit: null,
                                                       EVSEID: "DE*ABC*E1", SAScheduleTupleID: _chosenTupleId,
                                                       MeterInfo: null, ReceiptRequired: null),
                ChargingStatusReqType           => new ChargingStatusResType(code, "DE*ABC*E1",
                                                       SAScheduleTupleID: _chosenTupleId, EVSEMaxCurrent: null,
                                                       MeterInfo: null, ReceiptRequired: null, AcEvseStatus()),
                MeteringReceiptReqType          => new MeteringReceiptResType(code, EvseStatus()),
                WeldingDetectionReqType         => new WeldingDetectionResType(code, DcEvseStatus(), Volt(0)),

                // The two certificate messages are the exception, and the reason is their content rather
                // than their sequence: a CertificateInstallationRes/CertificateUpdateRes is a contract chain,
                // an encrypted private key, a DH public key and an eMAID, none of which can be fabricated to
                // carry a refusal. This station advertises no certificate service either (ServiceList is
                // null in Discovery), so an EV asking for one is outside what it was offered — which is a
                // stronger objection than "not now" and is raised as one. Anything else reaching here is not
                // a request at all.
                _ => throw new SessionAborted(
                         $"SECC sequence guard: {SequenceErrorAt} not allowed in phase {_phase}, and no " +
                         $"response of its own type can be built to carry FAILED_SequenceError."),
            };

        }

        // ── response builders ─────────────────────────────────────────────────
        private BodyBaseType NewSession()
        {
            // Resume ([V2G2-740]): a SessionSetupReq whose header carries a paused predecessor's session id
            // rejoins that session; any other id (normally all-zero) starts a fresh one.
            if (ResumeSessionId is not null && _requestHeader!.SessionID.AsSpan().SequenceEqual(ResumeSessionId))
            {
                _sessionId = ResumeSessionId;
                return new SessionSetupResType(ResponseCode.OK_OldSessionJoined, "DE*ABC*E1", 1_600_000_000L);
            }

            _sessionId = FixedSessionId ?? RandomNumberGenerator.GetBytes(8);
            // A transaction begins when the session does, and is bound to it from the first byte:
            // an OCPP record that cannot be tied to one ISO 15118 session is a record of some
            // transaction or other, and any agreement found against it is luck.
            _backend = Backend?.Invoke(Convert.ToHexString(_sessionId).ToLowerInvariant());
            return new SessionSetupResType(ResponseCode.OK_NewSessionEstablished, "DE*ABC*E1", 1_600_000_000L);
        }

        private BodyBaseType SessionStop(SessionStopReqType req)
        {
            Paused = req.ChargingSession == ChargingSession.Pause;
            return new SessionStopResType(ResponseCode.OK);
        }

        private BodyBaseType Discovery(ResponseCode code = ResponseCode.OK) =>
            new ServiceDiscoveryResType(code,
                // Contract first: a Josev EVCC picks Plug & Charge whenever Contract is offered AND the
                // session runs over TLS ([V2G2-828]); an EIM EV simply selects ExternalPayment.
                new PaymentOptionListType(new[] { PaymentOption.Contract, PaymentOption.ExternalPayment }),
                new ChargeServiceType(ServiceID: 1, ServiceName: mode == PowerMode.Dc ? "DC" : "AC",
                    ServiceCategory.EVCharging, ServiceScope: null, FreeService: true,
                    new SupportedEnergyTransferModeType(new[]
                    {
                        mode == PowerMode.Dc ? EnergyTransferMode.DC_extended : EnergyTransferMode.AC_three_phase_core,
                    })),
                ServiceList: null);

        private BodyBaseType ChargeParams() =>
            new ChargeParameterDiscoveryResType(ResponseCode.OK, EVSEProcessing.Finished,
                                                Schedules(), EvseChargeParameter());

        /// <summary>What this station can deliver, in the mode it was constructed for — the one part of a
        /// ChargeParameterDiscoveryRes that is a property of the hardware rather than of the answer, which is
        /// why a refusal (<see cref="SequenceError"/>) carries it too.</summary>
        private EVSEChargeParameterType EvseChargeParameter() =>
            mode == PowerMode.Dc
                ? new DC_EVSEChargeParameterType(DcEvseStatus(),
                      EVSEMaximumCurrentLimit: Amp(200), EVSEMaximumPowerLimit: Watt(150_000),
                      EVSEMaximumVoltageLimit: Volt(500), EVSEMinimumCurrentLimit: Amp(0),
                      EVSEMinimumVoltageLimit: Volt(200), EVSECurrentRegulationTolerance: null,
                      EVSEPeakCurrentRipple: Amp(1), EVSEEnergyToBeDelivered: null)
                : new AC_EVSEChargeParameterType(AcEvseStatus(),
                      EVSENominalVoltage: Volt(230), EVSEMaxCurrent: Amp(32));

        /// <summary>The SASchedule offer: with EVSEProcessing=Finished the response must carry a
        /// SAScheduleList ([V2G2-905]) — a live Josev EVCC crashes on its absence (found 2026-07-22; our
        /// loopback EVCC never read it, which masked the gap). Without a <see cref="TariffSignKey"/>: one
        /// tuple, one 1-hour 11-kW PMax entry. With one: a two-tuple smart-charging offer whose SalesTariffs
        /// are digitally signed into this response's header (§7.9.2.5).</summary>
        private SAScheduleListType Schedules()
        {
            var schedules = TariffSignKey is null ? PlainSchedule() : TariffSchedules();
            _offeredSchedules = schedules;
            if (TariffSignKey is not null)
                _responseSignature = SignTariffs(schedules);
            return schedules;
        }

        private static SAScheduleListType PlainSchedule() =>
            new(new[]
            {
                new SAScheduleTupleType(SAScheduleTupleID: 1,
                    new PMaxScheduleType(new[]
                    {
                        new PMaxScheduleEntryType(new RelativeTimeIntervalType(Start: 0, Duration: 3600), PMax: Watt(11_000)),
                    }),
                    SalesTariff: null),
            });

        /// <summary>The smart-charging offer: tuple 1 is a flat 11 kW at price levels 2→3, tuple 2 starts
        /// capped at 7.4 kW on level 1 and opens to 22 kW on level 2 after 30 min — a price-aware EV picks
        /// tuple 2 (average level 1.5 vs 2.5) and shapes its ChargingProfile to the 7.4/22 kW steps.</summary>
        private static SAScheduleListType TariffSchedules() =>
            new(new[]
            {
                new SAScheduleTupleType(SAScheduleTupleID: 1,
                    new PMaxScheduleType(new[]
                    {
                        new PMaxScheduleEntryType(new RelativeTimeIntervalType(Start: 0, Duration: 3600), PMax: Watt(11_000)),
                    }),
                    new SalesTariffType(Id: "salesTariff1", SalesTariffID: 1, SalesTariffDescription: "standard",
                        NumEPriceLevels: 3, SalesTariffEntry: new[] { TariffEntry(0, level: 2), TariffEntry(1800, level: 3) })),
                new SAScheduleTupleType(SAScheduleTupleID: 2,
                    new PMaxScheduleType(new[]
                    {
                        new PMaxScheduleEntryType(new RelativeTimeIntervalType(Start: 0, Duration: 1800), PMax: Watt(7_400)),
                        new PMaxScheduleEntryType(new RelativeTimeIntervalType(Start: 1800, Duration: 1800), PMax: Watt(22_000)),
                    }),
                    new SalesTariffType(Id: "salesTariff2", SalesTariffID: 2, SalesTariffDescription: "off-peak boost",
                        NumEPriceLevels: 3, SalesTariffEntry: new[] { TariffEntry(0, level: 1), TariffEntry(1800, level: 2) })),
            });

        private static SalesTariffEntryType TariffEntry(uint start, byte level) =>
            new(new RelativeTimeIntervalType(Start: start, Duration: null), EPriceLevel: level,
                ConsumptionCost: Array.Empty<ConsumptionCostType>());

        /// <summary>ONE header signature covering ALL offered SalesTariffs (one reference per tariff Id,
        /// §7.9.2.5), in the spec/cbV2G combined-grammar form. Fachlich this is the Mobility Operator's
        /// signature relayed by the SECC; here the configured tariff key signs directly. NOTE the honest
        /// validation limit: a Josev EVCC carries tariff verification only as a code TODO and ignores the
        /// signature — only our own EVCC (loopback/CI) actually verifies it.</summary>
        private SignatureType SignTariffs(SAScheduleListType schedules)
        {
            var references = new List<(string ReferenceId, byte[] Digest)>();
            var buf = new byte[2048];
            foreach (var tuple in schedules.SAScheduleTuple)
            {
                if (tuple.SalesTariff?.Id is not { } id) continue;
                if (!Iso2Codec.EncodeFragment_SalesTariff(tuple.SalesTariff, buf, out int n))
                    throw new InvalidOperationException("SalesTariff fragment encode failed.");
                references.Add((id, V2GSignature.Digest(buf.AsSpan(0, n))));
            }
            // Transforms=[EXI C14N] is schema-optional but included: a live Josev EVCC fails V2G message
            // validation on any Reference without it (its pydantic model requires the field) and drops the
            // session in ChargeParameterDiscovery — found live 2026-07-22, same bug class as its -20
            // CertificateInstallation counterpart.
            var signedInfo = V2GSignature.BuildSignedInfo(references, includeExiTransform: true);
            return V2GSignature.BuildSignature(signedInfo, V2GSignature.Sign(signedInfo, TariffSignKey!));
        }

        /// <summary>Validates the <c>PowerDeliveryReq(Start)</c> against the offer: unknown tuple id →
        /// <c>FAILED_TariffSelectionInvalid</c>; a ChargingProfile entry above the PMax active at its start
        /// → <c>FAILED_ChargingProfileInvalid</c> ([V2G2-761]). Invalid requests do NOT advance the phase —
        /// the EV may retry with a corrected choice.</summary>
        private (BodyBaseType, Phase) PowerOn(PowerDeliveryReqType req)
        {
            var tuple = _offeredSchedules?.SAScheduleTuple.FirstOrDefault(t => t.SAScheduleTupleID == req.SAScheduleTupleID);
            bool withinPMax = tuple is not null && ProfileWithinPMax(req.ChargingProfile, tuple.PMaxSchedule);
            ChargingProfileCheck = new Iso2ProfileResult(tuple is not null, withinPMax, req.SAScheduleTupleID,
                                                         req.ChargingProfile?.ProfileEntry.Count ?? 0);

            if (tuple is null)  return (PowerDeliveryRes(ResponseCode.FAILED_TariffSelectionInvalid), Phase.PowerOn);
            if (!withinPMax)    return (PowerDeliveryRes(ResponseCode.FAILED_ChargingProfileInvalid), Phase.PowerOn);
            _chosenTupleId = req.SAScheduleTupleID;   // the charging-status responses echo the EV's choice
            // AC puts no power on the wire in either direction, so the profile the EV committed to
            // here is also what the station's meter has to measure — see Meter().
            _acCommittedPowerW = req.ChargingProfile is { ProfileEntry.Count: > 0 } profile
                                     ? (double) profile.ProfileEntry[0].ChargingProfileEntryMaxPower.ToDecimal()
                                     : 0;
            return (PowerOnOrOff(), Phase.Charging);
        }

        /// <summary>Each profile entry's max power must stay within the PMax entry active at its start time
        /// (the last PMax entry whose start ≤ the profile entry's start). A profile-free request is fine —
        /// the ChargingProfile is schema-optional, and an EV without one simply follows PMax.</summary>
        private static bool ProfileWithinPMax(ChargingProfileType? profile, PMaxScheduleType pmaxSchedule)
        {
            if (profile is null) return true;
            foreach (var entry in profile.ProfileEntry)
            {
                decimal? pmax = null;
                foreach (var p in pmaxSchedule.PMaxScheduleEntry)
                    if (p.TimeInterval is RelativeTimeIntervalType rti && rti.Start <= entry.ChargingProfileEntryStart)
                        pmax = p.PMax.ToDecimal();
                if (pmax is null || entry.ChargingProfileEntryMaxPower.ToDecimal() > pmax)
                    return false;
            }
            return true;
        }

        private BodyBaseType PowerOnOrOff() => PowerDeliveryRes(ResponseCode.OK);

        private BodyBaseType PowerDeliveryRes(ResponseCode code) =>
            new PowerDeliveryResType(code, EvseStatus());

        private BodyBaseType CurrentDemand()
        {
            // The station's own view of this iteration: what it is presenting at the outlet. The same
            // 400 V x 120 A it reports below, so the number it signs is the number it announces.
            var measured = Deliver(400d * 120d);

            bool receipt = DemandReceipt();
            // A station with a real meter reports it every cycle, not only when it wants a receipt
            // signed back: the signed reading is the point on its own (docs/CONCEPT.md §4.3), and
            // MeterInfo is optional here. Without a meter installed, nothing changes.
            bool reading = receipt || InstalledMeter is not null;
            return new CurrentDemandResType(ResponseCode.OK, DcEvseStatus(Notification()),
                EVSEPresentVoltage: Volt(400), EVSEPresentCurrent: Amp(120),
                EVSECurrentLimitAchieved: false, EVSEVoltageLimitAchieved: false, EVSEPowerLimitAchieved: false,
                EVSEMaximumVoltageLimit: null, EVSEMaximumCurrentLimit: null, EVSEMaximumPowerLimit: null,
                EVSEID: "DE*ABC*E1", SAScheduleTupleID: _chosenTupleId,
                MeterInfo: reading ? measured : null, ReceiptRequired: receipt ? true : null);
        }

        private BodyBaseType ChargingStatus()
        {
            var measured = Deliver(_acCommittedPowerW);   // see CurrentDemand(); AC's only power is the profile

            // A Contract session gets ReceiptRequired + the MeterInfo the EV echoes back inside its
            // signed MeteringReceiptReq (a Josev EVCC only honours this over TLS).
            bool receipt = DemandReceipt();
            bool reading = receipt || InstalledMeter is not null;   // see CurrentDemand()
            return new ChargingStatusResType(ResponseCode.OK, "DE*ABC*E1", SAScheduleTupleID: _chosenTupleId,
                EVSEMaxCurrent: null,
                MeterInfo: reading ? measured : null, ReceiptRequired: receipt ? true : null,
                AcEvseStatus(Notification()));
        }

        /// <summary>Whether THIS status response demands a receipt: exactly once per Contract session. A
        /// live Josev EVCC re-enters ChargingStatus after every MeteringReceiptRes and only counts down its
        /// charge-loop cycles on receipt-free responses — demanding one every cycle loops the session
        /// forever (found live 2026-07-22: 1789 receipts before we pulled the plug).</summary>
        private bool DemandReceipt()
        {
            if (!_contract || _receiptRequested) return false;
            _receiptRequested = true;
            return true;
        }

        /// <summary>Whether THIS status response carries <c>EVSENotification.ReNegotiation</c>: exactly once
        /// (same once-per-session logic as <see cref="DemandReceipt"/> — the EV answers every notified
        /// response with a renegotiation, so repeating it would loop).</summary>
        private EVSENotification Notification()
        {
            if (!RequestRenegotiation || _renegotiationSignalled) return EVSENotification.None;
            _renegotiationSignalled = true;
            return EVSENotification.ReNegotiation;
        }

        private BodyBaseType Renegotiate()
        {
            _renegotiated = true;
            Renegotiations++;
            return PowerOnOrOff();
        }

        // ── Plug & Charge handlers ────────────────────────────────────────────

        /// <summary>Stores the EV's contract leaf (its public key verifies the signatures that follow) and
        /// hands out the 16-byte GenChallenge the signed AuthorizationReq must echo ([V2G2-825]).</summary>
        private BodyBaseType PaymentDetails(PaymentDetailsReqType req)
        {
            _genChallenge = FixedGenChallenge ?? RandomNumberGenerator.GetBytes(16);
            try
            {
                using var contract = X509CertificateLoader.LoadCertificate(req.ContractSignatureCertChain.Certificate);
                _contractSubject = contract.Subject;
                _contractKey = contract.GetECDsaPublicKey();
            }
            catch (Exception ex) { _contractSubject = $"cert-error: {ex.Message}"; }

            return new PaymentDetailsResType(ResponseCode.OK, _genChallenge, clock.GetUtcNow().ToUnixTimeSeconds());
        }

        /// <summary>EIM: plain OK. Contract: validate the <b>signed</b> AuthorizationReq — challenge echo,
        /// reference digest over the re-encoded body-element fragment, and the ECDSA signature under our
        /// combined -2 grammar or (Josev) the standalone-xmldsig one.</summary>
        private BodyBaseType Authorize(AuthorizationReqType req)
        {
            if (_contract)
            {
                bool challengeOk = _genChallenge is not null && req.GenChallenge is not null
                    && req.GenChallenge.AsSpan().SequenceEqual(_genChallenge);

                var buf = new byte[1024];
                bool fragOk = Iso2Codec.EncodeFragment_AuthorizationReq(req, buf, out int n);
                var (digestOk, signatureOk, grammar) = VerifyBodySignature(fragOk ? buf.AsSpan(0, n) : default);
                PnCAuth = new Iso2PnCResult(challengeOk, digestOk, signatureOk, grammar, _contractSubject);
            }
            return new AuthorizationResType(ResponseCode.OK, EVSEProcessing.Finished);
        }

        /// <summary>Validates one signed MeteringReceiptReq (same digest + dual-grammar signature check,
        /// no challenge) and acknowledges with the mode's EVSE status.</summary>
        private BodyBaseType MeteringReceipt(MeteringReceiptReqType req)
        {
            var buf = new byte[1024];
            bool fragOk = Iso2Codec.EncodeFragment_MeteringReceiptReq(req, buf, out int n);
            var (digestOk, signatureOk, grammar) = VerifyBodySignature(fragOk ? buf.AsSpan(0, n) : default);
            MeteringReceipts.Add(new Iso2ReceiptResult(digestOk, signatureOk, grammar));

            return new MeteringReceiptResType(ResponseCode.OK,
                mode == PowerMode.Dc ? DcEvseStatus() : AcEvseStatus());
        }

        /// <summary>The shared verify half: reference digest of <paramref name="fragment"/> against the
        /// request header's signature, then ECDSA over the SignedInfo — first our combined -2 grammar
        /// (<c>V2GSignature</c>), then the Josev standalone-xmldsig fallback.</summary>
        private (bool DigestOk, bool SignatureOk, string Grammar) VerifyBodySignature(ReadOnlySpan<byte> fragment)
        {
            if (fragment.IsEmpty || _requestHeader?.Signature is not { } sig
                || sig.SignedInfo.Reference.Count == 0 || _contractKey is null)
                return (false, false, "none");

            bool digestOk = V2GSignature.VerifyReference(sig.SignedInfo.Reference[0], fragment);

            if (V2GSignature.Verify(sig.SignedInfo, sig.SignatureValue.Value, _contractKey))
                return (digestOk, true, "iso2-msgdef");
            if (XmlDsigInterop2.VerifyStandaloneXmldsig(sig.SignedInfo, sig.SignatureValue.Value, _contractKey))
                return (digestOk, true, "xmldsig-standalone");
            return (digestOk, false, "none");
        }

        /// <summary>
        /// A signing meter, if one was installed. Without it the readings stay unsigned — which is
        /// what every station in the field does, and is therefore the honest default.
        /// </summary>
        public SigningMeter? InstalledMeter { get; init; }

        /// <summary>The power the EV committed to in its accepted ChargingProfile; AC's only power.</summary>
        private double _acCommittedPowerW;

        /// <summary>
        /// Books one charge-loop iteration of <paramref name="watts"/> onto the installed meter.
        /// </summary>
        /// <remarks>
        /// This is what makes the station's reading and the vehicle's <see cref="EvMeter"/> two views
        /// of one process rather than two unrelated numbers. Before it existed the meter held a fixed
        /// figure, and any comparison against the EV would have shown a difference that meant nothing
        /// — the most confusing possible outcome for a screen whose whole point is agreement.
        /// <para>
        /// Both sides count the same <see cref="ChargeLoopSample.Period"/> at the same power, so in a
        /// clean session they land on the same watt-hour. That is not a proof about real meters; it
        /// is two implementations of one arithmetic agreeing, which is what catches a wrong field or
        /// a wrong unit.
        /// </para>
        /// </remarks>
        private MeterInfoType Deliver(double watts)
        {
            var wattHours = ChargeLoopSample.WattHoursRounded(watts);
            _deliveredWh += wattHours;
            InstalledMeter?.Add(wattHours);

            // Read and signed exactly once per iteration, and the one value is what both the wire
            // and the backend record get. Signing twice would produce two different signatures over
            // one reading — legitimate ECDSA, and it would quietly destroy the only comparison the
            // OCPP record makes possible (see OcppTransactionRecord).
            var measured = Meter();

            _backend?.Sample(measured.MeterReading ?? 0, measured.TMeter ?? 0,
                             measured.SigMeterReading is { } signature
                                 ? Convert.ToHexString(signature).ToLowerInvariant() : null,
                             MeterPublicKeyHex());

            return measured;
        }

        /// <summary>The installed meter's public key as hex <c>X‖Y</c>, for the backend record.</summary>
        private string? MeterPublicKeyHex()
        {
            if (InstalledMeter is null) return null;

            using var key = InstalledMeter.PublicKey;
            var q = key.ExportParameters(includePrivateParameters: false).Q;
            return (Convert.ToHexString(q.X!) + Convert.ToHexString(q.Y!)).ToLowerInvariant();
        }

        /// <summary>
        /// Where this station reports to, when it reports anywhere.
        /// </summary>
        /// <remarks>
        /// A factory rather than a recorder, because the transaction cannot exist before the session
        /// id it is bound to. Null by default: a station with no backend behaves exactly as it did.
        /// <para>
        /// It books every iteration, including the ones whose reading never reaches the car — an EIM
        /// session at a station with no signing meter shows the driver nothing and bills the backend
        /// all the same, and a record that only kept what was displayed would hide precisely that.
        /// </para>
        /// </remarks>
        public Func<string, OcppTransactionRecorder>? Backend { get; init; }

        private OcppTransactionRecorder? _backend;

        /// <summary>What this session has delivered, counted whether or not a meter is fitted.</summary>
        /// <remarks>
        /// A station without a <em>signing</em> meter still has a meter — almost all of them do; what
        /// they lack is one that signs. Reporting the same figure unsigned is truer to the field and
        /// more useful: every session then has two counts to compare, and only some have a signature
        /// over the station's. (Until 2026-08-03 this path reported a literal 42 Wh, which was
        /// harmless placeholder data right up until something started comparing it with the
        /// vehicle's count.)
        /// </remarks>
        private ulong _deliveredWh;

        /// <summary>
        /// The station's meter reading, signed into <c>SigMeterReading</c> when a meter is installed.
        /// </summary>
        /// <remarks>
        /// The field is a standard one, <c>maxLength 64</c> — exactly one raw ECDSA P-256 <c>r‖s</c>
        /// pair — and it exists so the <em>meter</em> can sign what it measured rather than the SECC
        /// asserting it. It is almost never populated in practice, which is precisely why a simulator
        /// should populate it (<c>docs/CONCEPT.md</c> §4.3). What is signed is our own layout, since
        /// the standard defines the field and not its content: see <see cref="MeterSigningPayload"/>.
        /// </remarks>
        private MeterInfoType Meter()
        {
            if (InstalledMeter is null)
                return new("VAN*M1", MeterReading: _deliveredWh, SigMeterReading: null, MeterStatus: null,
                           TMeter: clock.GetUtcNow().ToUnixTimeSeconds());

            var (wh, timestamp) = InstalledMeter.Read();
            return new(InstalledMeter.MeterId, MeterReading: wh,
                       SigMeterReading: InstalledMeter.Sign(2, _sessionId, wh, timestamp),
                       MeterStatus: null, TMeter: timestamp);
        }

        private static DC_EVSEStatusType DcEvseStatus(EVSENotification notification = EVSENotification.None) =>
            new(NotificationMaxDelay: 0, notification, EVSEIsolationStatus: null, DC_EVSEStatusCode.EVSE_Ready);
        private static AC_EVSEStatusType AcEvseStatus(EVSENotification notification = EVSENotification.None) =>
            new(NotificationMaxDelay: 0, notification, RCD: false);

        /// <summary>This station's status in whichever of the two shapes its mode calls for — the choice
        /// every response carrying the schema's abstract <c>EVSEStatusType</c> has to make.</summary>
        private EVSEStatusType EvseStatus(EVSENotification notification = EVSENotification.None) =>
            mode == PowerMode.Dc ? DcEvseStatus(notification) : AcEvseStatus(notification);

        private static PhysicalValueType Volt(short v) => new(0, UnitSymbol.V, v);
        private static PhysicalValueType Amp(short a)  => new(0, UnitSymbol.A, a);
        private static PhysicalValueType Watt(int w)   => PhysicalValue.Of(w, UnitSymbol.W);
    }
}
