/*
 * Copyright (c) 2021-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using cloud.charging.open.protocols.ISO15118_2;
using cloud.charging.open.protocols.ISO15118_2.Generated;
using cloud.charging.open.protocols.ISO15118.Framing;
using cloud.charging.open.protocols.ISO15118.Metering;
using cloud.charging.open.protocols.ISO15118.Security;
using cloud.charging.open.protocols.ISO15118.Session;
using cloud.charging.open.protocols.ISO15118.EXI.Dispatch;

namespace cloud.charging.open.protocols.ISO15118.StateMachines.Iso2
{
    /// <summary>Outcome of validating a -2 Plug &amp; Charge signed <c>AuthorizationReq</c>: GenChallenge echo,
    /// reference digest over the body-element fragment, and the ECDSA signature against the contract leaf
    /// stored at PaymentDetails — with the <c>SignedInfo</c> grammar it verified under
    /// (<c>iso2-msgdef</c> = our/cbV2G combined form, <c>xmldsig-standalone</c> = the Josev form).</summary>
    /// <param name="Chain">How the contract chain fared against the configured V2G roots, or
    /// <c>ChainResult.NotConfigured</c> when no roots were given — which is not the same as a pass and must
    /// never be reported as one.</param>
    public sealed record Iso2PnCResult(bool ChallengeOk, bool DigestOk, bool SignatureOk,
                                       string SignatureGrammar, string ContractSubject,
                                       ChainResult Chain);

    /// <summary>Outcome of validating one signed -2 <c>MeteringReceiptReq</c> (same dual-grammar dance).</summary>
    public sealed record Iso2ReceiptResult(bool DigestOk, bool SignatureOk, string SignatureGrammar);

    /// <summary>Outcome of validating the EV's <c>PowerDeliveryReq(Start)</c> against the SASchedule offer:
    /// the chosen tuple id must be one we offered ([V2G2-773], else <c>FAILED_TariffSelectionInvalid</c>)
    /// and every ChargingProfile entry must stay within the PMax active at its start time ([V2G2-761],
    /// else <c>FAILED_ChargingProfileInvalid</c>).</summary>
    public sealed record Iso2ProfileResult(bool TupleIdOk, bool WithinPMax, byte TupleId, int ProfileEntries);

    /// <summary>
    /// Outcome of a live -2 contract-provisioning exchange (§7.9.2.4): whether the EV's signature over its
    /// own request verified (and under which <c>SignedInfo</c> grammar — see <see cref="Iso2PnCResult"/>),
    /// whose certificate signed it, and whether the issued contract key could be <b>encrypted for</b> that
    /// certificate's key.
    /// </summary>
    /// <param name="ReceiverSubject">
    /// The subject of the certificate the answer is wrapped for: the OEM provisioning certificate for an
    /// installation, the <em>expiring contract certificate</em> for an update. That difference is what
    /// makes an update self-authenticating — only the holder of the old contract key can open the new one.
    /// </param>
    /// <param name="Chain">How that certificate fared against the configured V2G roots, or
    /// <c>ChainResult.NotConfigured</c> when none were given. Note that -2 sends the OEM certificate
    /// <b>alone</b>, with no sub-certificates, where -20 sends a chain — so there is less to validate here
    /// and a station without the exact issuing CA cannot build a path at all.</param>
    /// <param name="IsUpdate">Whether this was a <c>CertificateUpdateReq</c> rather than an installation.</param>
    public sealed record Iso2CertInstallResult(bool DigestOk, bool SignatureOk, string SignatureGrammar,
                                               string ReceiverSubject, bool EncryptedForReceiver,
                                               ChainResult Chain, bool IsUpdate);

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
    /// <para>
    /// Unsealed for the same reason <see cref="Iso20.Secc20Dc"/> and <see cref="Iso20.Secc20Ac"/> are: a
    /// test that has to assert on what the car <em>sent</em> — rather than on the session completing —
    /// needs a station that keeps its requests, and the -20 side has had one since 2026-08-03.
    /// </para>
    /// </summary>
    public class Secc2(PowerMode mode, TimeSpan sequenceTimeout, TimeProvider clock)
    {
        private enum Phase
        {
            SessionSetup, ServiceDiscovery, PaymentSelection, CertificateProvisioning, PaymentDetails,
            Authorization, ChargeParams,
            CableCheck, PreCharge, PowerOn, Charging, WeldingDetection, SessionStop, Done,
        }

        private Phase _phase = Phase.SessionSetup;
        private byte[] _sessionId = new byte[8];
        private DateTimeOffset _lastSeen = clock.GetUtcNow();

        // ── Plug & Charge session state (set by PaymentServiceSelection/PaymentDetails) ─
        /// <summary>When set, the EV's contract chain is validated against these roots at PaymentDetails
        /// and the verdict is carried in <see cref="PnCAuth"/>. Unset (default) means no chain check — the
        /// signature still verifies against the presented leaf, which proves the message is well-formed and
        /// nothing about who issued the certificate.</summary>
        public V2GChainValidator? ContractChainValidator { get; set; }

        private ChainResult _contractChain = ChainResult.NotConfigured;
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

        /// <summary>The name of the last request refused with <c>FAILED_UnknownSession</c> ([V2G2-460]), or
        /// null if every request carried this session's id. Unlike <see cref="SequenceErrorAt"/> this does
        /// <b>not</b> end the session — a car that echoes the right id next time charges — so it is the only
        /// way a caller can tell that the guard fired at all.</summary>
        public string? UnknownSessionAt { get; private set; }

        /// <summary>How many requests this session refused with <c>FAILED_UnknownSession</c>. Counted rather
        /// than flagged because the guard is non-fatal: a peer may send several.</summary>
        public int UnknownSessionRefusals { get; private set; }

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

        /// <summary>DC only: how many times this session ran <c>CableCheck</c>. One on the way in, and one
        /// more per renegotiation — which is the whole of what
        /// <see cref="RenegotiationNeedsIsolationSequence"/> changes, made visible, since a refusal that
        /// never happens and a phase that is never entered look identical from outside.</summary>
        public int IsolationSequences { get; private set; }

        /// <summary>
        /// DC only: after a renegotiation's <c>ChargeParameterDiscoveryRes</c>, expect
        /// <c>CableCheckReq</c> and then <c>PreChargeReq</c> before <c>PowerDeliveryReq</c> — the same
        /// path as on the way in. <b>True</b> by default, which is what the standard's DC state table
        /// says; set false to restore the behaviour this station had until 2026-08-15.
        /// </summary>
        /// <remarks>
        /// <para>
        /// ISO 15118-2's SECC state table for DC gives <c>Process ChargeParameterDiscoveryReq</c> exactly
        /// one successor — *Wait for CableCheckReq*, `[V2G2-565]` and `[V2G2-582]` — and carries no
        /// renegotiation exception. This station used to hand a renegotiated session straight to
        /// <c>PowerOn</c>, and <see cref="Evcc2"/> used to send exactly that, so the loopback agreed with
        /// itself and neither side was ever asked the question.
        /// </para>
        /// <para>
        /// <b>It took a counterparty being right to find it.</b> EVerest's <c>EvseV2G</c> answered our
        /// short sequence <c>FAILED_SequenceError</c> on 2026-08-11; that was written up as their defect
        /// and withdrawn on 2026-08-15 when the filing's own document gate was worked — see
        /// <c>docs/reports/everest-evsev2g-renegotiation-cablecheck.md</c> and
        /// <c>docs/normative-basis.md</c>. Their station had been conformant the whole time.
        /// </para>
        /// <para>
        /// The switch exists so a test can put the old behaviour back and watch the new one fail, which is
        /// the only way to know a guard is load-bearing. It is <b>not</b> a leniency knob for interop: a
        /// station that accepts the short sequence is accepting a message its state table cannot admit.
        /// </para>
        /// </remarks>
        public bool RenegotiationNeedsIsolationSequence { get; init; } = true;

        private bool _renegotiationSignalled;   // the notification is sent exactly once
        private bool _renegotiated;             // set by Renegotiate(); only RenegotiationNeedsIsolationSequence=false reads it

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

        // ── contract provisioning (§7.9.2.4) ──

        /// <summary>The ServiceID this station gives its certificate service. 2 by convention — 1 is the
        /// charge service — and the number the EV names again in its PaymentServiceSelection.</summary>
        public const ushort CertificateServiceId = 2;

        /// <summary>Parameter set 1 of the certificate service: <c>Installation</c>, for a car with no
        /// contract yet.</summary>
        public const short CertificateInstallationParameterSetId = 1;

        /// <summary>Parameter set 2: <c>Update</c>, for a car whose contract is running out.</summary>
        public const short CertificateUpdateParameterSetId = 2;

        /// <summary>
        /// Whether this station offers contract provisioning at all — <b>off by default</b>.
        /// </summary>
        /// <remarks>
        /// Opt-in for two reasons. A station that advertises a certificate service it cannot fulfil is
        /// lying to every car that reads its ServiceDiscoveryRes, and the base implementation here issues a
        /// throwaway dev contract, which is a demo answer rather than a real one. And left off, the wire
        /// output is byte-for-byte what it always was: the ServiceList stays absent, so nothing that
        /// records or replays a -2 session has to be regenerated for a feature it does not use.
        /// </remarks>
        public bool OfferCertificateService { get; set; }

        /// <summary>Whether the EV took the certificate service up in its PaymentServiceSelection.</summary>
        public bool CertificateServiceSelected { get; private set; }

        /// <summary>The result of handling a <c>CertificateInstallationReq</c> or <c>CertificateUpdateReq</c>,
        /// if the EV sent one; null otherwise.</summary>
        public Iso2CertInstallResult? CertInstall { get; private set; }

        /// <summary>Virtual for the same reason <see cref="Iso20.Secc20Base.Handle"/> is: a test that has to
        /// assert on what the car <em>sent</em>, rather than on the session merely completing, needs a
        /// station that keeps its requests. The -20 side has had one since 2026-08-03; -2 had no way to
        /// build one.</summary>
        public virtual V2G_Message Handle(V2G_Message request)
        {
            var now = clock.GetUtcNow();
            if (_phase is not Phase.SessionSetup && now - _lastSeen > sequenceTimeout)
                throw new SessionAborted($"SECC sequence timeout: EV silent for > {sequenceTimeout.TotalSeconds:0}s");
            _lastSeen = now;

            _requestHeader = request.Header;   // the PnC verify paths need the header signature
            _responseSignature = null;         // a response builder (ChargeParams) may set one

            var element = request.Body.BodyElement!;

            // [V2G2-460]: any request except SessionSetupReq whose SessionID is not the one stored for the
            // active session is answered FAILED_UnknownSession. SessionSetupReq is excluded by the
            // requirement itself — that is where zero means "new session" and an old id means "resume".
            //
            // The phase is left alone, which is this station's -2 policy for the whole FAILED family (see
            // Dispatch's remarks): -2 obliges nobody to end the session over a response code, so a car that
            // echoes the right id next time goes on charging. -20 would end it; that asymmetry is the
            // standards', and it is why this guard lives here rather than in shared code.
            if (element is not SessionSetupReqType &&
                !request.Header.SessionID.AsSpan().SequenceEqual(_sessionId))
            {
                UnknownSessionAt = element.GetType().Name.Replace("Type", "");
                UnknownSessionRefusals++;
                return new V2G_Message(new MessageHeaderType(_sessionId, Notification: null, Signature: null),
                                       new BodyType(Refuse(element, ResponseCode.FAILED_UnknownSession)));
            }

            var (body, next) = Dispatch(element);
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

        /// <summary>
        /// What a refusal does to the session, which is the one place this station differs from
        /// <see cref="Iso20.Secc20Base"/> on purpose: <b>here a <c>FAILED_*</c> response leaves the phase
        /// where it was</b>, so a car that corrects itself charges. There a <c>FAILED_*</c> ends the
        /// session outright.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The two standards say different things, and each state machine follows its own. <c>-20</c>'s
        /// §8.6 states that a <c>FAILED</c> response is a fatal error and that both SECC and EVCC terminate
        /// the communication session after sending or receiving it — a plain statement in the ResponseCode
        /// description, carrying no requirement ID of its own. <c>-2</c>'s §8.8.2 has the parallel
        /// description <em>without</em> that sentence: `[V2G2-735]` obliges the EVCC to ignore the response's
        /// other parameters and nothing more, `[V2G2-734]` covers the OK case, and `[V2G2-736]` requires the
        /// SECC to fill the mandatory fields with schema-conformant values anyway. Nothing in <c>-2</c>
        /// obliges either side to end the session.
        /// </para>
        /// <para>
        /// So the asymmetry is the standards', not an oversight — which is the opposite of what the note in
        /// <c>Secc20Base</c> claimed until 2026-08-09, and the reason unifying the behaviour was dropped
        /// after the documents were read rather than before. Making <c>-20</c> lenient would contradict
        /// §8.6 outright; making <c>-2</c> strict would remove a behaviour it permits, for no requirement.
        /// Carries the <c>-2</c> document caveat in <c>docs/normative-basis.md</c>: the text to hand is the
        /// 2022 DIS revision, while this stack targets ISO 15118-2:2014.
        /// </para>
        /// </remarks>
        /// <remarks>
        /// The guard as well: only the (phase, request) pairs below are legal, and the wildcard arm rejects
        /// the rest. Each arm names the phase to move to, which is how the refusal policy above is applied
        /// — the arms that refuse name the phase they are already in.
        /// </remarks>
        private (BodyBaseType Body, Phase Next) Dispatch(BodyBaseType req) => (_phase, req) switch
        {
            (Phase.SessionSetup, SessionSetupReqType) =>
                (NewSession(), Phase.ServiceDiscovery),

            (Phase.ServiceDiscovery, ServiceDiscoveryReqType) =>
                (Discovery(), Phase.PaymentSelection),

            // ServiceDetail is optional and repeatable, and it is how an EV learns what a value-added
            // service offers before selecting it — here, that parameter set 1 installs a contract and 2
            // updates one. It answers without moving: the EV may ask about several services, or none.
            (Phase.PaymentSelection, ServiceDetailReqType r) =>
                (ServiceDetail(r), Phase.PaymentSelection),

            // Contract (Plug & Charge) inserts the PaymentDetails exchange before Authorization;
            // ExternalPayment (EIM) goes straight to Authorization. A car that also took up the
            // certificate service provisions first — it has no contract to present until it has.
            (Phase.PaymentSelection, PaymentServiceSelectionReqType r) =>
                PaymentSelection(r),

            // §7.9.2.4: the two provisioning messages are alternatives, sent once, between the service
            // selection and PaymentDetails. Installation is for a car that has no contract; Update renews
            // one that is running out.
            (Phase.CertificateProvisioning, CertificateInstallationReqType r) =>
                (CertificateInstallation(r), AfterProvisioning()),

            (Phase.CertificateProvisioning, CertificateUpdateReqType r) =>
                (CertificateUpdate(r), AfterProvisioning()),

            // A car that asked for the service and then changed its mind is not out of sequence: the
            // service is an offer, not an obligation ([V2G2-680] makes the exchange optional even once
            // selected). Let it carry on with the contract it already had.
            (Phase.CertificateProvisioning, PaymentDetailsReqType r) =>
                (PaymentDetails(r), Phase.Authorization),

            (Phase.PaymentDetails, PaymentDetailsReqType r) =>
                (PaymentDetails(r), Phase.Authorization),

            // Self-looping poll phase, the same shape as PowerOn below: while the station answers
            // Ongoing the EV repeats its AuthorizationReq, and real ones do — the tux-evse replay in
            // SequenceError's remarks is exactly that, a VW polling twice at a charger answering
            // Ongoing_WaitingForCustomerInteraction. Answering Finished at once was the only reason the
            // second poll ever arrived a phase late. Advance only when we have actually finished.
            (Phase.Authorization, AuthorizationReqType r) =>
                AuthorizeStep(r),

            // DC goes to CableCheck after ChargeParameterDiscovery — after a renegotiation too, since
            // 2026-08-15. The comment that stood here said "after a renegotiation the cable is already
            // checked … a Josev EVCC does exactly that", and both halves were wrong: the state table has
            // no such exception, and the Josev runs it appealed to were AC. See
            // RenegotiationNeedsIsolationSequence.
            (Phase.ChargeParams, ChargeParameterDiscoveryReqType) =>
                (ChargeParams(), mode == PowerMode.Dc && (RenegotiationNeedsIsolationSequence || !_renegotiated)
                                     ? Phase.CableCheck
                                     : Phase.PowerOn),

            // ── DC-only pre-charge sequence ──
            (Phase.CableCheck, CableCheckReqType) =>
                (CableCheck(), Phase.PreCharge),
            (Phase.PreCharge, PreChargeReqType) =>
                (new PreChargeResType(ResponseCode.OK, DcEvseStatus(), Volt(390)), Phase.PowerOn),

            (Phase.PowerOn, PowerDeliveryReqType { ChargeProgress: ChargeProgress.Start } r) =>
                PowerOn(r),

            // ── charging loop (mode-specific request) ──
            (Phase.Charging, CurrentDemandReqType r) when mode == PowerMode.Dc =>
                (CurrentDemand(r), Phase.Charging),
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
            return Refuse(req, ResponseCode.FAILED_SequenceError);
        }


        /// <summary>
        /// The response that <b>pairs with</b> <paramref name="req"/>, carrying <paramref name="code"/> and
        /// nothing this station cannot mean. Split out of <see cref="SequenceError"/> on 2026-08-11, when
        /// `[V2G2-460]` gained a second reason to refuse a request whose own response type is the only legal
        /// answer — `[V2G2-538]` asks for *the corresponding response message*, and there is exactly one
        /// table of those.
        /// </summary>
        private BodyBaseType Refuse(BodyBaseType req, ResponseCode code)
        {

            return req switch
            {
                SessionSetupReqType             => new SessionSetupResType(code, "DE*ABC*E1", Now()),
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
                // than their sequence: a CertificateInstallationRes/CertificateUpdateRes is a contract
                // chain, an encrypted private key, a DH public key and an eMAID, none of which can be
                // fabricated to carry a refusal — the schema makes all four mandatory. So a provisioning
                // message that arrives in the wrong phase, or at a station not offering the service, ends
                // the session rather than being answered with something untrue. Anything else reaching
                // here is not a request at all.
                _ => throw new SessionAborted(
                         $"SECC refusal guard: {req.GetType().Name.Replace("Type", "")} cannot be answered " +
                         $"in phase {_phase}, because no response of its own type can be built to carry {code}."),
            };

        }

        // ── response builders ─────────────────────────────────────────────────

        /// <summary>The station's own clock in Unix seconds, for every <c>EVSETimeStamp</c> this session
        /// sends — <c>SessionSetupRes</c> and <c>PaymentDetailsRes</c> — which is what an EV without a clock
        /// of its own takes as the time ([V2G2-748]).</summary>
        /// <remarks>
        /// <c>SessionSetupRes</c> carried the literal <c>1_600_000_000</c> — 13 September 2020 — in every
        /// session this station ever answered, while <c>PaymentDetailsRes</c> and every metering receipt read
        /// the clock two messages later. A foreign decoder printing the date is what made it visible
        /// (tux-evse, 2026-08-06); this helper is here so the two cannot drift apart again. Nothing needed a
        /// recording seam for it: the corpus recorder already drives a <c>ManualTimeProvider</c> pinned to
        /// <c>RecordedAt</c>, so a re-recorded session stays byte-identical for the same reason those other
        /// messages always did.
        /// </remarks>
        private long Now() => clock.GetUtcNow().ToUnixTimeSeconds();

        private BodyBaseType NewSession()
        {
            // Resume ([V2G2-740]): a SessionSetupReq whose header carries a paused predecessor's session id
            // rejoins that session; any other id (normally all-zero) starts a fresh one.
            if (ResumeSessionId is not null && _requestHeader!.SessionID.AsSpan().SequenceEqual(ResumeSessionId))
            {
                _sessionId = ResumeSessionId;
                return new SessionSetupResType(ResponseCode.OK_OldSessionJoined, "DE*ABC*E1", Now());
            }

            _sessionId = FixedSessionId ?? RandomNumberGenerator.GetBytes(8);
            // A transaction begins when the session does, and is bound to it from the first byte:
            // an OCPP record that cannot be tied to one ISO 15118 session is a record of some
            // transaction or other, and any agreement found against it is luck.
            _backend = Backend?.Invoke(Convert.ToHexString(_sessionId).ToLowerInvariant());
            return new SessionSetupResType(ResponseCode.OK_NewSessionEstablished, "DE*ABC*E1", Now());
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
                // Contract provisioning is a *value-added service* in -2, not part of the charge service —
                // which is why offering it takes a ServiceList entry the EV then selects by id, where -20
                // simply sets a boolean in its AuthorizationSetupRes. Absent unless switched on, so the
                // frame is byte-identical to every session this station answered before.
                ServiceList: OfferCertificateService
                                 ? new ServiceListType(new[]
                                   {
                                       new ServiceType(CertificateServiceId, "Certificate",
                                                       ServiceCategory.ContractCertificate,
                                                       ServiceScope: null, FreeService: true),
                                   })
                                 : null);

        /// <summary>
        /// What the certificate service offers, as the two parameter sets §7.9.2.4 names: 1 installs a
        /// contract, 2 updates one. Any other service id is answered <c>FAILED_ServiceIDInvalid</c> —
        /// the EV asked about something this station never listed.
        /// </summary>
        private BodyBaseType ServiceDetail(ServiceDetailReqType request)
        {

            if (!OfferCertificateService || request.ServiceID != CertificateServiceId)
                return new ServiceDetailResType(ResponseCode.FAILED_ServiceIDInvalid, request.ServiceID,
                                                ServiceParameterList: null);

            return new ServiceDetailResType(ResponseCode.OK, request.ServiceID,
                new ServiceParameterListType(new[]
                {
                    new ParameterSetType(CertificateInstallationParameterSetId, new[] { ServiceParameter("Installation") }),
                    new ParameterSetType(CertificateUpdateParameterSetId,       new[] { ServiceParameter("Update") }),
                }));

            static ParameterType ServiceParameter(string value) =>
                new("Service", BoolValue: null, ByteValue: null, ShortValue: null, IntValue: null,
                    PhysicalValue: null, StringValue: value);

        }

        /// <summary>
        /// The payment option and, with it, whether this car is going to provision a contract first.
        /// </summary>
        private (BodyBaseType, Phase) PaymentSelection(PaymentServiceSelectionReqType request)
        {

            _contract = request.SelectedPaymentOption == PaymentOption.Contract;

            CertificateServiceSelected =
                OfferCertificateService &&
                request.SelectedServiceList.SelectedService.Any(s => s.ServiceID == CertificateServiceId);

            return (new PaymentServiceSelectionResType(ResponseCode.OK),
                    CertificateServiceSelected ? Phase.CertificateProvisioning
                  : _contract                  ? Phase.PaymentDetails
                                               : Phase.Authorization);

        }

        /// <summary>Where a provisioned car goes next — the same place it would have gone without the
        /// detour, since provisioning replaces no step, it only precedes one.</summary>
        private Phase AfterProvisioning() => _contract ? Phase.PaymentDetails : Phase.Authorization;

        /// <summary>
        /// One <c>CertificateInstallationReq</c>: verify that the car is the car its manufacturer built,
        /// then hand out a contract wrapped for that car's OEM key.
        /// </summary>
        private BodyBaseType CertificateInstallation(CertificateInstallationReqType request)
        {

            using var oemLeaf = LoadOrNull(request.OEMProvisioningCert);
            using var verifyKey = oemLeaf?.GetECDsaPublicKey();

            var buf = new byte[4096];
            bool fragOk = Iso2Codec.EncodeFragment_CertificateInstallationReq(request, buf, out int n);
            var (digestOk, signatureOk, grammar) = VerifyBodySignature(fragOk ? buf.AsSpan(0, n) : default, verifyKey);

            // -2 sends the OEM certificate alone — there are no sub-certificates in the message — so the
            // most a validator can do is check that leaf against the configured roots directly.
            var chain = oemLeaf is not null && ContractChainValidator is not null
                            ? ContractChainValidator.Validate(oemLeaf, null)
                            : ChainResult.NotConfigured;

            // Recorded before the seam is asked, not after: what an override most needs to know is whether
            // the car proved anything, and a backend that only learns it afterwards has already answered.
            CertInstall = new Iso2CertInstallResult(digestOk, signatureOk, grammar,
                                                    oemLeaf?.Subject ?? "?",
                                                    EncryptedForReceiver(oemLeaf),
                                                    chain, IsUpdate: false);

            var (response, signature) = IssueContract(request, oemLeaf);
            _responseSignature = signature;
            return response;

        }

        /// <summary>
        /// One <c>CertificateUpdateReq</c>: the same shape, with the expiring contract in place of the OEM
        /// certificate. That substitution is the whole security argument for an update — the answer is
        /// wrapped for the old contract's key, so only the car that already held that contract can open it.
        /// </summary>
        private BodyBaseType CertificateUpdate(CertificateUpdateReqType request)
        {

            using var contractLeaf = LoadOrNull(request.ContractSignatureCertChain.Certificate);
            using var verifyKey = contractLeaf?.GetECDsaPublicKey();

            var buf = new byte[4096];
            bool fragOk = Iso2Codec.EncodeFragment_CertificateUpdateReq(request, buf, out int n);
            var (digestOk, signatureOk, grammar) = VerifyBodySignature(fragOk ? buf.AsSpan(0, n) : default, verifyKey);

            var chain = contractLeaf is not null && ContractChainValidator is not null
                            ? ContractChainValidator.Validate(contractLeaf, request.ContractSignatureCertChain.SubCertificates?.Certificate)
                            : ChainResult.NotConfigured;

            CertInstall = new Iso2CertInstallResult(digestOk, signatureOk, grammar,
                                                    contractLeaf?.Subject ?? "?",
                                                    EncryptedForReceiver(contractLeaf),
                                                    chain, IsUpdate: true);

            var (response, signature) = RenewContract(request, contractLeaf);
            _responseSignature = signature;
            return response;

        }

        /// <summary>
        /// The contract a <c>CertificateInstallationReq</c> is answered with, and the header signature that
        /// goes with it — <b>override to let a backend issue it</b>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A charging station has no business minting contract certificates: a contract belongs to a
        /// Mobility Operator, and the answer is really assembled by a Certificate Provisioning Service on
        /// the far side of the operator's backend. The station is in the path only because it is the one
        /// thing the car can reach. What this class does by default is therefore a <em>dev</em> contract —
        /// fresh keys, self-signed — which is right for a loopback demo and right for nothing else.
        /// </para>
        /// <para>
        /// Response and signature together, because they cannot be produced apart: the signature covers
        /// four elements <em>of the response</em> (§7.9.2.4.2), so whoever builds the one has to build the
        /// other. An override returning a backend's message brings the backend's signature with it.
        /// </para>
        /// </remarks>
        /// <param name="receiverCertificate">The OEM provisioning certificate the answer must be wrapped
        /// for, or <c>null</c> if it could not be read — in which case an implementation has nobody to wrap
        /// for and can only fill the mandatory fields.</param>
        protected virtual (CertificateInstallationResType Response, SignatureType Signature) IssueContract(
            CertificateInstallationReqType request, X509Certificate2? receiverCertificate)
        {
            var (chain, encryptedKey, dhPublicKey, emaid) = DevContract(receiverCertificate);
            return SignInstallation(new CertificateInstallationResType(
                       ResponseCode.OK, ProvisioningCertificateChain(), chain, encryptedKey, dhPublicKey, emaid));
        }

        /// <summary>The renewal counterpart of <see cref="IssueContract"/>; see there for why this is a
        /// seam at all.</summary>
        /// <param name="receiverCertificate">The <em>expiring contract</em> certificate — an update is
        /// wrapped for the credential it replaces.</param>
        protected virtual (CertificateUpdateResType Response, SignatureType Signature) RenewContract(
            CertificateUpdateReqType request, X509Certificate2? receiverCertificate)
        {
            var (chain, encryptedKey, dhPublicKey, emaid) = DevContract(receiverCertificate, request.EMAID);
            return SignUpdate(new CertificateUpdateResType(
                       ResponseCode.OK, ProvisioningCertificateChain(), chain, encryptedKey, dhPublicKey, emaid,
                       // Nothing here tracks how often a car has renewed, and inventing a countdown would
                       // be inventing a policy. Absent says "no opinion", which is the truth.
                       RetryCounter: null));
        }

        /// <summary>
        /// Mints a throwaway contract and wraps its private key for <paramref name="receiverCertificate"/>.
        /// The four signed elements of either response, in the order §7.9.2.4.2 signs them.
        /// </summary>
        private (CertificateChainType Chain, ContractSignatureEncryptedPrivateKeyType Key,
                 DiffieHellmanPublickeyType DhPublicKey, EMAIDType Emaid) DevContract(
            X509Certificate2? receiverCertificate, string? emaid = null)
        {

            emaid ??= "DE-VAN-C00000001-6";

            using var contractKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var contractCert = new CertificateRequest($"CN={emaid}, O=Vanaheimr (dev)", contractKey,
                                                            HashAlgorithmName.SHA256)
                                         .CreateSelfSigned(clock.GetUtcNow().AddMinutes(-5),
                                                           clock.GetUtcNow().AddYears(2));

            byte[] dhPublicKey, wrapped;
            var receiverAgreement = ReceiverAgreement(receiverCertificate);
            if (receiverAgreement is not null)
            {
                using (receiverAgreement)
                    (dhPublicKey, wrapped) = ContractProvisioning.EncryptContractKey(receiverAgreement.PublicKey, contractKey);
            }
            else
            {
                // No usable P-256 key agreement on the receiving certificate. The fields are mandatory, so
                // they are filled for a throwaway recipient and the car cannot open them — recorded in
                // CertInstall.EncryptedForReceiver rather than left for it to discover.
                using var throwaway = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
                (dhPublicKey, wrapped) = ContractProvisioning.EncryptContractKey(throwaway.PublicKey, contractKey);
            }

            return (new CertificateChainType("id1", contractCert.RawData, SubCertificates: null),
                    new ContractSignatureEncryptedPrivateKeyType("id2", wrapped),
                    new DiffieHellmanPublickeyType("id3", dhPublicKey),
                    new EMAIDType("id4", emaid));

        }

        /// <summary>
        /// The Secondary Actor's own chain, which the car checks the response's signature against. Self-signed
        /// here for the same reason the contract is: a real one belongs to a provisioning service.
        /// </summary>
        private CertificateChainType ProvisioningCertificateChain()
        {
            _provisioningKey ??= ECDsa.Create(ECCurve.NamedCurves.nistP256);
            _provisioningCert ??= new CertificateRequest("CN=Vanaheimr SA (dev)", _provisioningKey,
                                                         HashAlgorithmName.SHA256)
                                      .CreateSelfSigned(clock.GetUtcNow().AddMinutes(-5),
                                                        clock.GetUtcNow().AddYears(2));
            return new CertificateChainType(Id: null, _provisioningCert.RawData, SubCertificates: null);
        }

        private ECDsa? _provisioningKey;
        private X509Certificate2? _provisioningCert;

        /// <summary>Signs the four elements of a CertificateInstallationRes with the provisioning key.</summary>
        private (CertificateInstallationResType, SignatureType) SignInstallation(CertificateInstallationResType response) =>
            (response, SignProvisioning(response.ContractSignatureCertChain,
                                        response.ContractSignatureEncryptedPrivateKey,
                                        response.DHpublickey, response.EMAID));

        private (CertificateUpdateResType, SignatureType) SignUpdate(CertificateUpdateResType response) =>
            (response, SignProvisioning(response.ContractSignatureCertChain,
                                        response.ContractSignatureEncryptedPrivateKey,
                                        response.DHpublickey, response.EMAID));

        /// <summary>
        /// The provisioning signature: one header signature over <b>four</b> references — the contract
        /// chain, the encrypted key, the DH public key and the eMAID (§7.9.2.4.2). Four, where every other
        /// signed message in -2 has one, and each element carries its own <c>Id</c> for the reference URI.
        /// </summary>
        private SignatureType SignProvisioning(CertificateChainType chain,
                                               ContractSignatureEncryptedPrivateKeyType key,
                                               DiffieHellmanPublickeyType dhPublicKey,
                                               EMAIDType emaid)
        {

            _provisioningKey ??= ECDsa.Create(ECCurve.NamedCurves.nistP256);

            var buf = new byte[4096];
            var references = new List<(string, byte[])>();

            if (Iso2Codec.EncodeFragment_ContractSignatureCertChain(chain, buf, out int n1))
                references.Add((chain.Id!, V2GSignature.Digest(buf.AsSpan(0, n1))));
            if (Iso2Codec.EncodeFragment_ContractSignatureEncryptedPrivateKey(key, buf, out int n2))
                references.Add((key.Id, V2GSignature.Digest(buf.AsSpan(0, n2))));
            if (Iso2Codec.EncodeFragment_DHpublickey(dhPublicKey, buf, out int n3))
                references.Add((dhPublicKey.Id, V2GSignature.Digest(buf.AsSpan(0, n3))));
            if (Iso2Codec.EncodeFragment_eMAID(emaid, buf, out int n4))
                references.Add((emaid.Id, V2GSignature.Digest(buf.AsSpan(0, n4))));

            if (references.Count != 4)
                throw new InvalidOperationException("provisioning response: not all four signed elements encoded.");

            var signedInfo = V2GSignature.BuildSignedInfo(references, includeExiTransform: true);
            return V2GSignature.BuildSignature(signedInfo, V2GSignature.Sign(signedInfo, _provisioningKey));

        }

        /// <summary>The receiving certificate's key-agreement handle, but only on the curve -2 provisioning
        /// uses; <c>null</c> for any other curve, a non-EC key, or a certificate that would not load.</summary>
        private static ECDiffieHellman? ReceiverAgreement(X509Certificate2? certificate)
        {
            var agreement = certificate?.GetECDiffieHellmanPublicKey();
            if (agreement is null)
                return null;
            if (agreement.KeySize == 256)
                return agreement;

            agreement.Dispose();
            return null;
        }

        private static bool EncryptedForReceiver(X509Certificate2? certificate)
        {
            var agreement = ReceiverAgreement(certificate);
            agreement?.Dispose();
            return agreement is not null;
        }

        private static X509Certificate2? LoadOrNull(byte[] der)
        {
            try { return X509CertificateLoader.LoadCertificate(der); }
            catch (CryptographicException) { return null; }
        }

        private BodyBaseType ChargeParams() =>
            new ChargeParameterDiscoveryResType(ResponseCode.OK, EVSEProcessing.Finished,
                                                Schedules(), EvseChargeParameter());

        /// <summary>What this station can deliver, in the mode it was constructed for — the one part of a
        /// ChargeParameterDiscoveryRes that is a property of the hardware rather than of the answer, which is
        /// why a refusal (<see cref="SequenceError"/>) carries it too.</summary>
        private EVSEChargeParameterType EvseChargeParameter() =>
            mode == PowerMode.Dc
                ? new DC_EVSEChargeParameterType(DcEvseStatus(),
                      EVSEMaximumCurrentLimit: PhysicalValue.Of((decimal) (DcAdvertisedMaxAmps ?? 200), UnitSymbol.A),
                      EVSEMaximumPowerLimit: Watt(150_000),
                      EVSEMaximumVoltageLimit: Volt(500), EVSEMinimumCurrentLimit: Amp(0),
                      EVSEMinimumVoltageLimit: Volt(200), EVSECurrentRegulationTolerance: null,
                      EVSEPeakCurrentRipple: Amp(1), EVSEEnergyToBeDelivered: null)
                : new AC_EVSEChargeParameterType(AcEvseStatus(),
                      EVSENominalVoltage: Volt(230), EVSEMaxCurrent: Amp(32));

        /// <summary>The SASchedule offer: with EVSEProcessing=Finished the response must carry a
        /// SAScheduleList ([V2G2-905]) — a live Josev EVCC crashes on its absence (found 2026-07-22; our
        /// loopback EVCC never read it, which masked the gap). Without a <see cref="TariffSignKey"/>: one
        /// tuple, one 1-hour <see cref="ThreePhase16A"/> PMax entry. With one: a two-tuple smart-charging
        /// offer whose SalesTariffs are digitally signed into this response's header (§7.9.2.5).</summary>
        private SAScheduleListType Schedules()
        {
            var schedules = OfferedSchedules();
            _offeredSchedules = schedules;
            if (TariffSignKey is not null)
                _responseSignature = SignTariffs(schedules);
            return schedules;
        }

        /// <summary>
        /// The schedule offer itself — override to let something other than this station decide it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <b>SA</b> in SAScheduleList is <i>Secondary Actor</i>: ISO 15118-2 has the list come to the
        /// SECC from the actor that sells the energy, not from the charger's own opinion. This class
        /// invents one, which is right for a demo station and is all a demo station can do — but a station
        /// with a backend has to be able to offer what the backend granted, and could not.
        /// </para>
        /// <para>
        /// Overridable rather than settable, and the difference matters: the offer is made fresh at each
        /// ChargeParameterDiscovery, including after a renegotiation ([V2G2-841]), so a property set once
        /// would go stale exactly when the EV re-asked. And it has to be produced <em>here</em>, inside the
        /// answer, because what is offered is also what <c>PowerDeliveryReq(Start)</c> is validated against
        /// ([V2G2-761]): a subclass that rewrote the response afterwards would offer one PMax and enforce
        /// another, and would refuse a car for exceeding a limit it was never shown.
        /// </para>
        /// </remarks>
        protected virtual SAScheduleListType OfferedSchedules()
            => TariffSignKey is null ? PlainSchedule() : TariffSchedules();

        /// <summary>
        /// What the ordinary European three-phase charge point actually offers: 3 × 230 V × 16 A.
        /// </summary>
        /// <remarks>
        /// <para>
        /// It used to be a round 11 kW, and the round number is a trap. A car recorded at a real 16 A
        /// charge point asks for exactly this figure, which is 40 W — 0.4 % — above 11,000, so
        /// [V2G2-761] refuses the whole ChargingProfile and the session dies at
        /// <c>PowerDelivery</c>: the last message before charging would have begun, after everything
        /// else has gone right. Found 2026-08-07 against both sides of a captured Porsche Taycan 4S
        /// (<c>ISO15118ConformanceTests/docs/interop-runs/2026-08-07-tux-porsche-ac</c>); the VW
        /// capture before it asked 4,100 W and sailed through, which is why it took a second car.
        /// </para>
        /// <para>
        /// Nothing about 11,000 was a protocol error — a station may offer what it likes and must
        /// then enforce it. But this station exists to be interoperated with, and a rounded PMax
        /// manufactures a refusal no real charger would produce.
        /// </para>
        /// <para>
        /// It is also consistent with what the same response advertises two fields earlier:
        /// <c>EVSENominalVoltage</c> 230 V and <c>EVSEMaxCurrent</c> 32 A, whose three-phase product
        /// is 22,080 W. The schedule offers half of that, which is a scheduling decision rather than
        /// a physical limit, and now at least a physical number.
        /// </para>
        /// </remarks>
        private const int ThreePhase16A = 11_040;

        /// <summary>
        /// The other two steps this station offers, at the same 230 V it advertises: 32 A on one phase and
        /// 32 A on three.
        /// </summary>
        /// <remarks>
        /// These were <c>7_400</c> and <c>22_000</c> until 2026-08-09, and the second of the two is the
        /// Taycan refusal above waiting to happen one tariff step higher: a car asking for its physical
        /// 22,080 W against a 22,000 W offer is refused by [V2G2-761] exactly as it was at 11,040 against
        /// 11,000. The fix went into <see cref="ThreePhase16A"/> and stopped there, which is the shape of
        /// mistake worth naming — a rounded number is not one bug, it is a habit, and the tuple nobody was
        /// looking at kept it.
        /// <para>
        /// 7,360 is the milder half: it is <em>below</em> the 7,400 it replaces, so no car was ever refused
        /// by it. What it fixes is our own side, since an EV that shapes its profile to PMax was committing
        /// to 7,400 W that 32 A on one phase at 230 V does not deliver.
        /// </para>
        /// </remarks>
        private const int SinglePhase32A =  7_360;
        private const int ThreePhase32A  = 22_080;

        /// <summary>What this DC outlet presents, and the most it will push — the ceiling on the current a
        /// vehicle's <c>CurrentDemandReq</c> can ask it for.</summary>
        private const short DcVolts   = 400;
        private const short DcMaxAmps = 120;

        /// <summary>
        /// The DC current this station <b>announces</b> at ChargeParameterDiscovery, in amps. Null — the
        /// default — announces the 200 A it always has.
        /// </summary>
        /// <remarks>A station may announce more than one outlet can push, and this one does: 200 A
        /// announced against <see cref="DcMaxAmps"/> served. That is not a contradiction — the envelope is
        /// the equipment's, the outlet is this connector's — but it does mean the announcement alone never
        /// constrained a car here, which is part of why nothing on this side ever exercised an EV reading
        /// it. Setting this makes the announcement bite.</remarks>
        public Double? DcAdvertisedMaxAmps { get; set; }

        /// <summary>
        /// A <b>running</b> DC current limit, in amps: the ceiling this station restates in every
        /// <c>CurrentDemandRes</c> and serves under. Null — the default — restates nothing, exactly as
        /// before.
        /// </summary>
        /// <remarks>
        /// <para>
        /// -2 lets the SECC carry <c>EVSEMaximumCurrentLimit</c>, <c>EVSEMaximumPowerLimit</c> and
        /// <c>EVSEMaximumVoltageLimit</c> in every charge-loop response, which is how a real station
        /// derates mid-session. This one sent all three as <c>null</c> for its whole life, so no loopback
        /// session ever put a running limit in front of our own EVCC — and when a live EVerest station did,
        /// on 2026-08-10, the car ignored it and asked for 120 A against a stated 55.2 A, three times out
        /// of three.
        /// </para>
        /// <para>
        /// <b>Opt-in on purpose.</b> Left null the wire output is byte-for-byte what the session corpus
        /// records, so this costs no vector regeneration; set, the station announces a ceiling, serves
        /// under it, and reports <c>EVSECurrentLimitAchieved</c> truthfully. The default path still reports
        /// that flag as <c>false</c> even when serving at <see cref="DcMaxAmps"/>, which is the one
        /// inaccuracy deliberately left alone here: correcting it would rewrite every recorded trace to
        /// settle a question no counterparty has asked.
        /// </para>
        /// </remarks>
        public Double? DcRunningMaxAmps { get; set; }

        private static SAScheduleListType PlainSchedule() =>
            new(new[]
            {
                new SAScheduleTupleType(SAScheduleTupleID: 1,
                    new PMaxScheduleType(new[]
                    {
                        new PMaxScheduleEntryType(new RelativeTimeIntervalType(Start: 0, Duration: 3600), PMax: Watt(ThreePhase16A)),
                    }),
                    SalesTariff: null),
            });

        /// <summary>
        /// The smart-charging offer: tuple 1 is the flat <see cref="ThreePhase16A"/> at price levels 2→3;
        /// tuple 2 starts capped at <see cref="SinglePhase32A"/> on level 1 and opens to
        /// <see cref="ThreePhase32A"/> on level 2 after 30 min — a price-aware EV picks tuple 2 (average
        /// level 1.5 vs 2.5) and shapes its ChargingProfile to those two steps. Tuple 1 carries the same
        /// figure as the plain offer for the same reason: a car that is not price-aware picks it, and it
        /// would meet the same wall.
        /// <para>
        /// This said "the 7.4/22 kW steps" until the constants were named, and kept saying it for one
        /// commit after — the round numbers are what the whole change is about, so a summary still
        /// reaching for them is how the drift starts again. Named, not spelled out, for the same reason.
        /// </para>
        /// </summary>
        private static SAScheduleListType TariffSchedules() =>
            new(new[]
            {
                new SAScheduleTupleType(SAScheduleTupleID: 1,
                    new PMaxScheduleType(new[]
                    {
                        new PMaxScheduleEntryType(new RelativeTimeIntervalType(Start: 0, Duration: 3600), PMax: Watt(ThreePhase16A)),
                    }),
                    new SalesTariffType(Id: "salesTariff1", SalesTariffID: 1, SalesTariffDescription: "standard",
                        NumEPriceLevels: 3, SalesTariffEntry: new[] { TariffEntry(0, level: 2), TariffEntry(1800, level: 3) })),
                new SAScheduleTupleType(SAScheduleTupleID: 2,
                    new PMaxScheduleType(new[]
                    {
                        new PMaxScheduleEntryType(new RelativeTimeIntervalType(Start: 0, Duration: 1800), PMax: Watt(SinglePhase32A)),
                        new PMaxScheduleEntryType(new RelativeTimeIntervalType(Start: 1800, Duration: 1800), PMax: Watt(ThreePhase32A)),
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

        /// <summary>
        /// One DC charge-loop iteration. The station serves the current the EV asked for, up to the
        /// <see cref="DcMaxAmps"/> this outlet can push — the -2 half of what <see cref="Iso20.Secc20Dc"/>
        /// does for -20, and for the same reason: a station announcing a flat 120 A whatever was requested
        /// makes the EV's setpoint invisible on this side of the wire, and leaves the reading it signs
        /// disagreeing with the counter the car kept. A vehicle asking for the full 120 A — every recorded
        /// run — is served exactly what it always was.
        /// </summary>
        private BodyBaseType CurrentDemand(CurrentDemandReqType req)
        {
            // The ceiling this iteration is served under: the outlet's own, lowered by a running limit if
            // one was set. A station that announces a limit and then serves past it is worse than one that
            // announces none, so the clamp and the announcement come from the same figure.
            var cap        = DcRunningMaxAmps is { } running ? Math.Min(running, DcMaxAmps) : (double) DcMaxAmps;
            var servedAmps = Math.Clamp((double) req.EVTargetCurrent.ToDecimal(), 0, cap);

            // The station's own view of this iteration: what it is presenting at the outlet. The same
            // volts x amps it reports below, so the number it signs is the number it announces.
            var measured = Deliver(DcVolts * servedAmps);

            bool receipt = DemandReceipt();
            // A station with a real meter reports it every cycle, not only when it wants a receipt
            // signed back: the signed reading is the point on its own (EVSimulatorApp/docs/CONCEPT.md §4.3), and
            // MeterInfo is optional here. Without a meter installed, nothing changes.
            bool reading = receipt || InstalledMeter is not null;
            return new CurrentDemandResType(ResponseCode.OK, DcEvseStatus(Notification()),
                EVSEPresentVoltage: Volt(DcVolts),
                EVSEPresentCurrent: PhysicalValue.Of((decimal) servedAmps, UnitSymbol.A),
                EVSECurrentLimitAchieved: DcRunningMaxAmps is not null && servedAmps >= cap,
                EVSEVoltageLimitAchieved: false, EVSEPowerLimitAchieved: false,
                EVSEMaximumVoltageLimit: DcRunningMaxAmps is null ? null : Volt(DcVolts),
                EVSEMaximumCurrentLimit: DcRunningMaxAmps is null ? null : PhysicalValue.Of((decimal) cap, UnitSymbol.A),
                EVSEMaximumPowerLimit:   DcRunningMaxAmps is null ? null : PhysicalValue.Of((decimal) (DcVolts * cap), UnitSymbol.W),
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

        /// <summary>The DC isolation test, answered <c>Finished</c> in one step by this simulated station,
        /// and counted — a second one in a session is the renegotiation return path having been walked.</summary>
        private BodyBaseType CableCheck()
        {
            IsolationSequences++;
            return new CableCheckResType(ResponseCode.OK, DcEvseStatus(), EVSEProcessing.Finished);
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

                // The chain the EV actually sent: leaf plus the MO sub-CAs it put in SubCertificates. Until
                // there was a validator, those sub-certificates were parsed by nobody.
                if (ContractChainValidator is not null)
                    _contractChain = ContractChainValidator.Validate(
                                         contract,
                                         req.ContractSignatureCertChain.SubCertificates?.Certificate);
            }
            catch (Exception ex) { _contractSubject = $"cert-error: {ex.Message}"; }

            return new PaymentDetailsResType(ResponseCode.OK, _genChallenge, Now());
        }

        /// <summary>
        /// One AuthorizationReq, and the phase that follows it: <see cref="Phase.ChargeParams"/> once the
        /// station reports <see cref="EVSEProcessing.Finished"/>, otherwise this phase again so the EV's
        /// next poll is answered rather than met with FAILED_SequenceError.
        /// </summary>
        /// <remarks>
        /// <see cref="Authorize"/> itself always finishes, so nothing changes for a station that does not
        /// override it. A station that has to wait on something slow — an operator's confirmation, a
        /// backend round trip — can now answer Ongoing without the session falling apart on the next
        /// message.
        /// </remarks>
        private (BodyBaseType Body, Phase Next) AuthorizeStep(AuthorizationReqType request)
        {
            var response = Authorize(request);
            return (response, response is AuthorizationResType { EVSEProcessing: EVSEProcessing.Finished }
                                  ? Phase.ChargeParams
                                  : Phase.Authorization);
        }

        /// <summary>EIM: plain OK. Contract: validate the <b>signed</b> AuthorizationReq — challenge echo,
        /// reference digest over the re-encoded body-element fragment, and the ECDSA signature under our
        /// combined -2 grammar or (Josev) the standalone-xmldsig one.</summary>
        protected virtual BodyBaseType Authorize(AuthorizationReqType req)
        {
            if (_contract)
            {
                bool challengeOk = _genChallenge is not null && req.GenChallenge is not null
                    && req.GenChallenge.AsSpan().SequenceEqual(_genChallenge);

                var buf = new byte[1024];
                bool fragOk = Iso2Codec.EncodeFragment_AuthorizationReq(req, buf, out int n);
                var (digestOk, signatureOk, grammar) = VerifyBodySignature(fragOk ? buf.AsSpan(0, n) : default);
                PnCAuth = new Iso2PnCResult(challengeOk, digestOk, signatureOk, grammar, _contractSubject, _contractChain);
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
        private (bool DigestOk, bool SignatureOk, string Grammar) VerifyBodySignature(ReadOnlySpan<byte> fragment) =>
            VerifyBodySignature(fragment, _contractKey);

        /// <summary>
        /// The same check against a key that is not the contract's. Contract provisioning needs it: a car
        /// asking for its first contract signs with the <em>OEM</em> key, and one asking for a renewal signs
        /// with the expiring contract's — neither of which is <c>_contractKey</c>, which is only stored at
        /// PaymentDetails and so does not exist yet at that point in the session.
        /// </summary>
        private (bool DigestOk, bool SignatureOk, string Grammar) VerifyBodySignature(ReadOnlySpan<byte> fragment,
                                                                                      ECDsa? verifyKey)
        {
            if (fragment.IsEmpty || _requestHeader?.Signature is not { } sig
                || sig.SignedInfo.Reference.Count == 0 || verifyKey is null)
                return (false, false, "none");

            bool digestOk = V2GSignature.VerifyReference(sig.SignedInfo.Reference[0], fragment);

            if (V2GSignature.Verify(sig.SignedInfo, sig.SignatureValue.Value, verifyKey))
                return (digestOk, true, "iso2-msgdef");
            if (XmlDsigInterop2.VerifyStandaloneXmldsig(sig.SignedInfo, sig.SignatureValue.Value, verifyKey))
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
        public Func<string, ISessionBackend>? Backend { get; init; }

        private ISessionBackend? _backend;

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
        /// should populate it (<c>EVSimulatorApp/docs/CONCEPT.md</c> §4.3). What is signed is our own layout, since
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
