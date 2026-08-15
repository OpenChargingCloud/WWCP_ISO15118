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

using cloud.charging.open.protocols.ISO15118_20.CommonMessages;
using cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated;
using cloud.charging.open.protocols.ISO15118.Framing;
using cloud.charging.open.protocols.ISO15118.Security;
using cloud.charging.open.protocols.ISO15118.Session;
using cloud.charging.open.protocols.ISO15118.EXI.Dispatch;

// Each -20 message set carries its own generated ResponseCode and V2GResponseType; IsFailure has to see
// all three — the same reason Evcc20Base.RefuseOnFailure aliases them.
using Ac20 = cloud.charging.open.protocols.ISO15118_20.AC.Generated;
using Dc20 = cloud.charging.open.protocols.ISO15118_20.DC.Generated;


namespace cloud.charging.open.protocols.ISO15118.StateMachines.Iso20
{
    /// <summary>Outcome of validating a live Plug &amp; Charge <c>AuthorizationReq</c>: whether the EV echoed our
    /// <c>GenChallenge</c>, whether the signed-element digest matched its reference, and whether the ECDSA
    /// signature verified against the contract leaf — plus what was observed (contract subject, signature method,
    /// and which <c>SignedInfo</c> grammar the signature verified under: <c>iso20-commonmessages</c> for our/cbV2G
    /// combined-schema form, <c>xmldsig-standalone</c> for the Josev-style standalone-xmldsig form, or
    /// <c>none</c>).</summary>
    /// <param name="Chain">How the contract chain fared against the configured V2G roots, or
    /// <c>ChainResult.NotConfigured</c> when none were given — not the same as a pass.</param>
    public sealed record PnCAuthResult(bool ChallengeOk, bool DigestOk, bool SignatureOk, string ContractSubject,
                                       string SignatureMethod, string SignatureGrammar, ChainResult Chain);

    /// <summary>Outcome of a live -20 <c>CertificateInstallationReq</c> (contract provisioning): whether the
    /// OEM-provisioning-chain digest and ECDSA signature verified (and under which SignedInfo grammar — see
    /// <see cref="PnCAuthResult"/>), the OEM leaf's subject, and whether the issued contract key could actually
    /// be <b>encrypted for</b> that OEM key (requires a P-521 OEM-provisioning key; a -2-era P-256 OEM cert —
    /// what Josev ships — cannot take part in the -20 secp521r1 ECDH, so the response is well-formed but
    /// undecryptable for the EV).</summary>
    /// <param name="Chain">How the OEM provisioning chain fared against the configured V2G roots, or
    /// <c>ChainResult.NotConfigured</c> when none were given. This is the one a real CPO would care about
    /// most: it decides whether the car asking for a contract is a car the OEM actually built.</param>
    public sealed record CertInstallResult(bool DigestOk, bool SignatureOk, string SignatureGrammar,
                                           string OemSubject, bool EncryptedForOem, ChainResult Chain);

    /// <summary>
    /// The SECC side of an ISO 15118-20 session, shared between AC and DC: the CommonMessages phases
    /// (SessionSetup..ServiceSelection, PowerDelivery, SessionStop) live here — it offers both EIM and Plug &amp;
    /// Charge and, for a PnC EV, validates the signed AuthorizationReq (see <see cref="PnCAuth"/>). The
    /// diverging middle (charge-parameter discovery, the DC-only CableCheck/PreCharge sequence,
    /// one charge-loop iteration, the DC-only WeldingDetection) is delegated to <see cref="Secc20Dc"/>/
    /// <see cref="Secc20Ac"/> via the <c>protected virtual</c> hooks below. Unlike -2, -20's messages
    /// interleave <em>three</em> distinct codecs (CommonMessages/DC/AC) within one session — each self
    /// contained per <c>WWCP_ISO15118_20.*.csproj</c> (no cross-references), so
    /// <see cref="Session.SessionContext"/> renders the header type each phase actually needs.
    /// </summary>
    public abstract class Secc20Base(TimeSpan sequenceTimeout, TimeProvider clock)
    {
        protected enum Phase20
        {
            SessionSetup, AuthorizationSetup, Authorization, ServiceDiscovery, ServiceDetail, ServiceSelection,
            ChargeParams, ScheduleExchange, CableCheck, PreCharge, PowerOn, Charging, WeldingDetection, SessionStop, Done,
        }

        protected Phase20 Phase { get; private set; } = Phase20.SessionSetup;
        protected readonly SessionContext SessionCtx = new(clock);
        private DateTimeOffset _lastSeen = clock.GetUtcNow();

        // True once the last response this station sent was a charge-loop response, so the *next* request
        // is governed by the tight charge-loop sequence timeout rather than the 60 s baseline. Reset every
        // Handle from the phase it actually dispatched in, so it cannot go stale across a phase change.
        private bool _lastResponseWasChargeLoop;

        /// <summary>
        /// How long the SECC waits for the next request once it has answered a charge-loop response — the
        /// phase in which the contactor is closed. ISO 15118-20 Tables 216 (AC) and 217 (DC), obliged by
        /// <c>[V2G20-1500]</c> and <c>[V2G20-1502]</c>, put <c>V2G_SECC_Sequence_Timeout</c> at <b>0,5 s</b>
        /// after <c>AC_ChargeLoopRes</c>/<c>DC_ChargeLoopRes</c>, where Table 215's baseline (the
        /// <c>sequenceTimeout</c> constructor argument) is 60 s for every other message.
        /// </summary>
        /// <remarks>
        /// The whole point of the 0,5 s is safety: a silent EV must not hold a closed contactor for a
        /// minute. This is the value we <i>measured EVerest's <c>libiso15118</c> flatten to 60 s</i>
        /// (<c>docs/reports/everest-d20-sequence-timeout.md</c>) — and until now our own station flattened
        /// it too. Settable (init-only) so a test can restore the old flat behaviour and watch this fail.
        /// Only the charge loop is tightened here; the DC-only CableCheck/PreCharge/WeldingDetection
        /// sub-timeouts of Table 217 remain at the baseline (noted in <c>docs/open-work.md</c>).
        /// </remarks>
        public TimeSpan ChargeLoopSequenceTimeout { get; init; } = TimeSpan.FromMilliseconds(500);

        /// <summary>The sequence timeout that governs the wait for the next request right now: the tight
        /// charge-loop value once a charge-loop response has gone out, the baseline otherwise.</summary>
        private TimeSpan CurrentSequenceTimeout => _lastResponseWasChargeLoop ? ChargeLoopSequenceTimeout : sequenceTimeout;

        /// <summary>When set, the EV's contract chain (at AuthorizationReq) and OEM provisioning chain (at
        /// CertificateInstallationReq) are validated against these roots, and the verdicts ride in
        /// <see cref="PnCAuth"/> and <see cref="CertInstall"/>. Unset means the signatures still verify
        /// against the presented leaf and nothing decides who issued it.</summary>
        public V2GChainValidator? ContractChainValidator { get; set; }

        /// <summary>The 16-byte PnC challenge we offered in AuthorizationSetupRes; the EV must echo it in a PnC AuthorizationReq.</summary>
        private byte[]? _genChallenge;

        /// <summary>The result of validating a PnC AuthorizationReq, if the EV authenticated via Plug &amp; Charge (null for EIM).</summary>
        public PnCAuthResult? PnCAuth { get; private set; }

        /// <summary>The result of handling a CertificateInstallationReq, if the EV requested contract provisioning.</summary>
        public CertInstallResult? CertInstall { get; private set; }

        /// <summary>True when the session ended with <c>ChargingSession.Pause</c> — keep <see cref="SessionId"/>
        /// and hand it to the next instance as <see cref="ResumeSessionId"/> so the EV can rejoin.</summary>
        public bool Paused { get; private set; }

        /// <summary>The session id this SECC assigned (or rejoined).</summary>
        public byte[] SessionId => SessionCtx.SessionId;

        /// <summary>A paused predecessor's session id: a SessionSetupReq carrying it rejoins the old session
        /// (<c>ResponseCode.OK_OldSessionJoined</c>) <em>if</em> the requester also proves it is the same EV
        /// (<see cref="ResumeBinding"/>); anything else starts fresh.</summary>
        public byte[]? ResumeSessionId { get; set; }

        /// <summary>
        /// The paused predecessor's <see cref="SessionBinding"/>. A resume must present the same one, or it
        /// is not the EV that paused and gets a new session.
        /// </summary>
        /// <remarks>
        /// Set together with <see cref="ResumeSessionId"/> — the two travel as a pair, and offering the id
        /// without the binding offers a session nobody can claim, which is the intended failure direction.
        /// </remarks>
        public byte[]? ResumeBinding { get; set; }

        /// <summary>
        /// The vehicle's TLS leaf certificate (DER). Resolved from the stream in
        /// <see cref="RunAsync(Stream, CancellationToken)"/> when it is an authenticated <c>SslStream</c>;
        /// settable for callers that drive <see cref="Handle"/> directly.
        /// </summary>
        public byte[]? VehicleLeafCertificate { get; set; }

        /// <summary>The binding of the session in effect — hand it to the next connection together with
        /// <see cref="SessionId"/> when this session pauses. Null when no peer certificate was available.</summary>
        public byte[]? SessionBinding { get; private set; }

        /// <summary>What this station answered the opening SessionSetupReq with: a new session, or a rejoined
        /// one. The mirror of <c>Evcc20Base.SessionSetupCode</c>, and what a test asserts a resume on.</summary>
        public ResponseCode SessionSetupCode { get; private set; }

        /// <summary>The energy-transfer service the paused session had settled on. A resumed session does not
        /// repeat service negotiation, so without this the station would forget which service it is running
        /// and stop expecting the <c>BPT_*</c> types a bidirectional one requires.</summary>
        public ushort ResumeEnergyServiceId { get; set; }

        /// <summary>What to hand the next connection, or <c>null</c> when this session did not pause.</summary>
        public ResumableSession? PausedSession

            => Paused
                   ? new ResumableSession(SessionId, SessionBinding, SelectedEnergyServiceId)
                   : null;

        /// <summary>Offer a paused predecessor to this connection — all three values at once, because a
        /// session id offered without its binding is a session nobody can claim.</summary>
        public void OfferResume(ResumableSession? paused)
        {
            ResumeSessionId       = paused?.SessionId;
            ResumeBinding         = paused?.Binding;
            ResumeEnergyServiceId = paused?.EnergyServiceId ?? 0;
        }

        /// <summary>When set, a new session is given this id instead of a fresh random one — the seam that
        /// makes a <b>recorded</b> session reproducible (<c>Tests/Traces</c>). The session id travels in every
        /// message header, so a re-recorded trace would otherwise differ in every single frame, and a corpus
        /// whose diff is total is a corpus nobody can review. Null by default and meant to stay null outside
        /// a recording: a predictable session id is a real weakness, not a convenience.</summary>
        public byte[]? FixedSessionId { get; set; }

        /// <summary>Likewise for the PnC challenge, and for the same reason: it is 16 random bytes in an
        /// <c>AuthorizationSetupRes</c> that every session sends, EIM ones included, so without pinning it
        /// no recorded -20 session can be regenerated and diffed. Null by default — a predictable challenge
        /// defeats what the challenge is for.</summary>
        public byte[]? FixedGenChallenge { get; set; }

        /// <summary>
        /// A signing meter, if one is installed. Without it the charge-loop readings stay absent,
        /// which is what every station in the field does and is therefore the honest default.
        /// </summary>
        /// <remarks>
        /// <c>MeterSignature</c> is <c>maxLength 64</c> here exactly as <c>SigMeterReading</c> is in
        /// -2 — one raw ECDSA P-256 <c>r‖s</c> pair — so -20's stronger signature suite does not
        /// apply: that governs XMLDSig over message fragments, not a fixed-width slot in a data type.
        /// The payload signed is the same layout as -2's with the protocol byte set to 20, so a
        /// reading cannot be presented as belonging to the other protocol
        /// (<see cref="Metering.MeterSigningPayload"/>).
        /// </remarks>
        public Metering.SigningMeter? InstalledMeter { get; init; }

        /// <summary>
        /// This station's meter reading for the charge loop, signed when a meter is fitted, or null.
        /// </summary>
        /// <remarks>
        /// Returns the <em>values</em> rather than a <c>MeterInfoType</c> because each -20 message
        /// set generates its own — the same independence that forces a separate <c>V2GSignature</c>
        /// per set. The signing is shared here; the construction belongs to whichever set is
        /// answering, which means AC and DC are two call sites that can drift apart. Both are driven
        /// to the charge loop and verified in <c>Secc20SignedMeterTests</c>.
        /// </remarks>
        /// <summary>
        /// Books one charge-loop iteration of <paramref name="watts"/> onto the installed meter.
        /// </summary>
        /// <remarks>
        /// The -20 half of what <c>Secc2.Deliver</c> does, and for the same reason: the station's
        /// signed reading and the vehicle's <c>EvMeter</c> have to be measuring one process, or a
        /// comparison between them shows a difference that means nothing. Each set calls this from
        /// its own charge-loop response, where it knows the power it is announcing.
        /// </remarks>
        protected void Deliver(double watts)
        {
            var wattHours = Metering.ChargeLoopSample.WattHoursRounded(watts);
            _deliveredWh += wattHours;
            InstalledMeter?.Add(wattHours);

            // Read and signed exactly once per iteration, so the wire and the backend record carry
            // one signature over one reading rather than two legitimate signatures over the same
            // number — which would quietly destroy the comparison the record exists for.
            _lastReading = MeasureNow();

            _backend?.Sample(_lastReading?.Wh ?? _deliveredWh,
                             (long) (_lastReading?.Timestamp ?? (ulong) clock.GetUtcNow().ToUnixTimeSeconds()),
                             _lastReading is { } r ? Convert.ToHexString(r.Signature).ToLowerInvariant() : null,
                             MeterPublicKeyHex());
        }

        /// <summary>Where this station reports to, when it reports anywhere. See <c>Secc2.Backend</c>.</summary>
        public Func<string, ISessionBackend>? Backend { get; init; }

        private ISessionBackend? _backend;
        private ulong _deliveredWh;
        private (string Id, ulong Wh, ulong Timestamp, byte[] Signature)? _lastReading;

        private string? MeterPublicKeyHex()
        {
            if (InstalledMeter is null) return null;

            using var key = InstalledMeter.PublicKey;
            var q = key.ExportParameters(includePrivateParameters: false).Q;
            return (Convert.ToHexString(q.X!) + Convert.ToHexString(q.Y!)).ToLowerInvariant();
        }

        /// <summary>The reading this iteration took, or null without a meter. Built by
        /// <see cref="Deliver"/>, never here, so one iteration yields exactly one signature.</summary>
        protected (string Id, ulong Wh, ulong Timestamp, byte[] Signature)? MeterReading() => _lastReading;

        /// <summary>Whether the last charge-loop request set <c>MeterInfoRequested</c> — `[V2G20-1081]`,
        /// the EV asking to be told the meter reading.</summary>
        /// <remarks>
        /// Recorded rather than acted on, because this station already answers with <c>MeterInfo</c>
        /// whenever it has a reading, asked or not — which `[V2G20-1833]` wants of a metering EVSE and
        /// which satisfies `[V2G20-1082]` a fortiori. What the flag buys is that a loopback can see the
        /// request field at all: it is otherwise invisible from either end, and a car that stopped asking
        /// would look exactly like one that never had.
        /// </remarks>
        public Boolean MeterInfoRequestedByEv { get; protected set; }

        private (string Id, ulong Wh, ulong Timestamp, byte[] Signature)? MeasureNow()
        {
            if (InstalledMeter is null)
                return null;

            var (wh, timestamp) = InstalledMeter.Read();
            return (InstalledMeter.MeterId, wh, (ulong) timestamp,
                    InstalledMeter.Sign(20, SessionId, wh, timestamp));
        }

        /// <summary>When set, the SECC requests a <b>service renegotiation</b> once: the first charge-loop
        /// response carries <c>EvseNotification.ServiceRenegotiation</c> in its EVSEStatus; the EV then stops
        /// power delivery, sends <c>SessionStopReq(ServiceRenegotiation)</c>, and the session re-enters
        /// ServiceDiscovery instead of terminating ([V2G20-1477]).</summary>
        public bool RequestRenegotiation { get; set; }

        /// <summary>How many service-renegotiation cycles this session ran.</summary>
        public int Renegotiations { get; private set; }

        /// <summary>When set, the Scheduled-mode ScheduleExchangeRes carries the rich, <b>digitally
        /// signed</b> <c>AbsolutePriceSchedule</c> (fachlich the eMSP's tariff) instead of the compact flat
        /// PriceLevelSchedule. P-521 — the -20 mandatory suite is ECDSA-P521/SHA-512.</summary>
        public ECDsa? TariffSignKey { get; set; }

        private bool _renegotiationSignalled;   // the notification goes out exactly once

        /// <summary>For the DC/AC charge-loop hooks: whether THIS response should carry the
        /// ServiceRenegotiation notification (fires once, see <see cref="RequestRenegotiation"/>).</summary>
        protected bool SignalRenegotiationOnce()
        {
            if (!RequestRenegotiation || _renegotiationSignalled) return false;
            _renegotiationSignalled = true;
            return true;
        }

        /// <summary>Advertise the Dynamic (ControlMode=2) parameter set ahead of Scheduled in ServiceDetailRes.
        /// Both modes are always offered ([V2G20-2656]: the SECC shall support Scheduled and Dynamic); the order
        /// only decides which one an EV that simply takes the first offered set (e.g. Josev) actually runs —
        /// the SECC itself answers whatever control mode the EV's requests carry, in kind.</summary>
        public bool PreferDynamicControlMode { get; set; }

        /// <summary>Which control mode the EV actually ran — <c>true</c> Dynamic, <c>false</c> Scheduled,
        /// <c>null</c> until its <c>ScheduleExchangeReq</c> has arrived.</summary>
        /// <remarks>
        /// <para>
        /// <see cref="PreferDynamicControlMode"/> decides what this station <i>offers first</i>; this is what
        /// the car did with the offer, and the two are different facts. Both modes are always advertised
        /// (<c>[V2G20-2656]</c>), so an EV is free to take either — which means a session configured for
        /// Dynamic completes exactly as happily when the peer runs Scheduled, and nothing on the wire
        /// afterwards distinguishes the two. This station branched on the answer already
        /// (<c>ScheduleExchange</c>, answering in kind per <c>[V2G20-1600]</c>) and had no way to say so.
        /// </para>
        /// <para>
        /// Added 2026-08-14 for the first reverse Dynamic run, and it is the fourth instance that week of one
        /// shape: <i>a value this side already held that no caller could reach</i>.
        /// </para>
        /// </remarks>
        public bool? EvControlModeIsDynamic { get; private set; }

        public bool IsDone => Phase == Phase20.Done;

        /// <summary>DC: CableCheck+PreCharge run between ScheduleExchange and PowerDelivery(Start). AC: skipped.</summary>
        protected abstract bool HasPreChargeSequence { get; }
        /// <summary>DC: WeldingDetection runs between PowerDelivery(Stop) and SessionStop. AC: skipped.</summary>
        protected abstract bool HasPostChargeSequence { get; }

        protected abstract (MessageSet Set, object Response) HandleChargeParameterDiscovery(object request);

        /// <summary>Is <paramref name="request"/> another poll of the self-looping <paramref name="phase"/> — so the
        /// SECC answers it and stays put — rather than the next-phase message that ends the loop? The
        /// <see cref="Phase20.PowerOn"/> poll (a real EV, e.g. Josev, repeats <c>PowerDeliveryReq(Start)</c> with
        /// <c>EVProcessing=Ongoing</c> until it starts the charge loop) is a CommonMessages request the base can
        /// name; <see cref="Secc20Dc"/> additionally classifies the DC-only poll phases
        /// (CableCheck/PreCharge/WeldingDetection), whose request types live in a separate, colliding namespace.</summary>
        protected virtual bool IsPollFor(Phase20 phase, object request) =>
            phase == Phase20.PowerOn && request is PowerDeliveryReq { ChargeProgress: ChargeProgress.Start };

        protected virtual (MessageSet Set, object Response) HandleCableCheck(object request) =>
            throw new NotSupportedException("CableCheck has no handler for this energy-transfer mode.");
        protected virtual (MessageSet Set, object Response) HandlePreCharge(object request) =>
            throw new NotSupportedException("PreCharge has no handler for this energy-transfer mode.");
        protected abstract (MessageSet Set, object Response) HandleChargeLoop(object request);
        protected virtual (MessageSet Set, object Response) HandleWeldingDetection(object request) =>
            throw new NotSupportedException("WeldingDetection has no handler for this energy-transfer mode.");

        /// <summary>One request in, one response out, and the next phase — the -20 analogue of <see cref="Iso2.Secc2.Handle"/>.</summary>
        /// <remarks>Virtual so a test station can answer as a foreign one does — a FAILED response code,
        /// for instance, which no station of ours ever sends and which the EVCC therefore never met until
        /// a live peer sent one (see <c>Evcc20FailureHandlingTests</c>).</remarks>
        public virtual (MessageSet Set, object Response) Handle(MessageSet set, object request)
        {
            var now = clock.GetUtcNow();
            var budget = CurrentSequenceTimeout;
            if (Phase is not Phase20.SessionSetup && now - _lastSeen > budget)
                throw new SessionAborted(
                    $"SECC sequence timeout: EV silent for > {budget.TotalMilliseconds:0} ms" +
                    (_lastResponseWasChargeLoop ? " in the charge loop" : ""));
            _lastSeen = now;

            // [V2G20-460]: any request except SessionSetupReq whose SessionID is not the one stored for the
            // active session is answered FAILED_UnknownSession. Ahead of everything else, the SessionStop
            // arm below included — the requirement excepts exactly one message type, and a stop is not it.
            //
            // Confined to a session that exists. Before SessionSetup there is no stored id to be wrong
            // against, and a request arriving there is out of *sequence* rather than out of session, which
            // is the sequence guard's case and the more accurate of the two answers.
            //
            // Unlike -2, this ends the session: a FAILED response is terminal in -20 (§8.6), so there is no
            // "echo the right id next time" here. Handle's own IsFailure does it; nothing is set here.
            if (Phase is not Phase20.SessionSetup &&
                request is not SessionSetupReq &&
                SessionIdOf(request) is { } incoming &&
                !incoming.AsSpan().SequenceEqual(SessionId))
            {
                UnknownSessionAt = request.GetType().Name;
                UnknownSessionRefusals++;

                var refusal = Refuse(set, request, Refusal.UnknownSession);
                Phase = Phase20.Done;
                _lastResponseWasChargeLoop = false;
                return refusal;
            }

            // A SessionStopReq is legal in *any* phase (ISO 15118-20 §7.9.2.4): the EV may abort the session at
            // any time, and the SECC answers gracefully and ends the session rather than raising the sequence
            // guard. Handled ahead of the phase switch so it wins over the wildcard poll / charge-loop arms
            // (which would otherwise mis-cast it to a DC/AC request). A live Josev reverse run showed an early
            // abort logging FAILED_SequenceError instead of a clean stop.
            if (set == MessageSet.Iso20CommonMessages && request is SessionStopReq stopReq)
            {
                // Service renegotiation ([V2G20-1477]): the session does NOT end — it re-enters
                // ServiceDiscovery, and the EV re-runs service selection / charge parameters / the loop.
                if (stopReq.ChargingSession == ChargingSession.ServiceRenegotiation)
                {
                    Renegotiations++;
                    Phase = Phase20.ServiceDiscovery;
                    // Cleared, so the claim on SelectedEnergyServiceId is true. It said "re-set from scratch
                    // by a service renegotiation" while this path left the old value standing — harmless
                    // when nothing checked the id, and not harmless now that an unadvertised one is refused:
                    // a stale id would be a *validated* stale id, surviving into a session that never
                    // selected it.
                    SelectedEnergyServiceId = 0;
                    return (MessageSet.Iso20CommonMessages, SessionStop(stopReq));
                }

                Paused = stopReq.ChargingSession == ChargingSession.Pause;
                Phase = Phase20.Done;
                return (MessageSet.Iso20CommonMessages, SessionStop(stopReq));
            }

            // A real EV *polls* the DC self-looping phases (CableCheck/PreCharge/WeldingDetection) — sending
            // the same request until it decides the step is done, then sending the next-phase message. Answer
            // each poll and stay put (the switch cases below map these phases onto themselves); when a non-poll
            // message arrives, advance through the self-loop phases *without consuming it* and re-evaluate it in
            // the phase it belongs to. So e.g. the first DC_PreChargeReq ends the CableCheck loop and is handled
            // by the PreCharge phase, and PowerDeliveryReq(Start) ends PreCharge and is handled by PowerOn.
            while (IsSelfLoopPhase(Phase) && !IsPollFor(Phase, request))
                Phase = NextAfter(Phase);

            // The phase the switch below actually dispatches in — captured after the self-loop advance, so
            // the first ChargeLoopReq (which advances PowerOn→Charging here) is seen as Charging. A response
            // is a charge-loop response exactly when we dispatch in Charging on something other than the
            // PowerDeliveryReq(Stop) that ends the loop; that is what arms the tight next-request timeout.
            var dispatchPhase = Phase;

            var (respSet, response, next) = (Phase, set, request) switch
            {
                (Phase20.SessionSetup, MessageSet.Iso20CommonMessages, SessionSetupReq r) =>
                    SessionSetupStep(r),

                (Phase20.AuthorizationSetup, MessageSet.Iso20CommonMessages, AuthorizationSetupReq r) =>
                    Step(MessageSet.Iso20CommonMessages, AuthSetup(r), Phase20.Authorization),

                // Contract provisioning: an EV that needs a contract cert sends a CertificateInstallationReq
                // *instead of* its first AuthorizationReq (we announced CertificateInstallationService=true);
                // answer it and stay in Authorization — the AuthorizationReq follows.
                (Phase20.Authorization, MessageSet.Iso20CommonMessages, CertificateInstallationReq r) =>
                    Step(MessageSet.Iso20CommonMessages, CertInstallation(r), Phase20.Authorization),

                // Self-looping poll phase, like PowerOn below. Two things bring an EV back here: an
                // Ongoing answer, which it polls on, and a WARNING — the family that says *this*
                // credential did not work rather than that the session is over, so the car may offer
                // another contract or fall back to EIM. Advancing on either answered its next message
                // with FAILED_SequenceError.
                (Phase20.Authorization, MessageSet.Iso20CommonMessages, AuthorizationReq r) =>
                    AuthStep(r),

                (Phase20.ServiceDiscovery, MessageSet.Iso20CommonMessages, ServiceDiscoveryReq r) =>
                    Step(MessageSet.Iso20CommonMessages, SvcDiscovery(r), Phase20.ServiceDetail),

                (Phase20.ServiceDetail, MessageSet.Iso20CommonMessages, ServiceDetailReq r) =>
                    SvcDetailStep(r),

                (Phase20.ServiceSelection, MessageSet.Iso20CommonMessages, ServiceSelectionReq r) =>
                    SvcSelectionStep(r),

                (Phase20.ChargeParams, _, _) =>
                    Append(HandleChargeParameterDiscovery(request), Phase20.ScheduleExchange),

                (Phase20.ScheduleExchange, MessageSet.Iso20CommonMessages, ScheduleExchangeReq r) =>
                    Step(MessageSet.Iso20CommonMessages, ScheduleExchange(r), HasPreChargeSequence ? Phase20.CableCheck : Phase20.PowerOn),

                // Self-looping poll phases: stay put and answer each poll. The pre-switch loop above guarantees
                // that if we're still in one of these phases, the request IS a poll for it (a next-phase message
                // would already have advanced Phase past here), and advances out when the loop ends.
                (Phase20.CableCheck, _, _) when HasPreChargeSequence =>
                    Append(HandleCableCheck(request), Phase20.CableCheck),

                (Phase20.PreCharge, _, _) when HasPreChargeSequence =>
                    Append(HandlePreCharge(request), Phase20.PreCharge),

                // Self-looping poll phase: a real EV repeats PowerDeliveryReq(Start) (EVProcessing=Ongoing)
                // until it begins the charge loop; answer each and stay. The pre-switch loop advances to
                // Charging (without consuming) once the first charge-loop message arrives.
                (Phase20.PowerOn, MessageSet.Iso20CommonMessages, PowerDeliveryReq { ChargeProgress: ChargeProgress.Start } r) =>
                    Step(MessageSet.Iso20CommonMessages, PowerDelivery(r), Phase20.PowerOn),

                (Phase20.Charging, MessageSet.Iso20CommonMessages, PowerDeliveryReq { ChargeProgress: ChargeProgress.Stop } r) =>
                    Step(MessageSet.Iso20CommonMessages, PowerDelivery(r), HasPostChargeSequence ? Phase20.WeldingDetection : Phase20.SessionStop),

                (Phase20.Charging, _, _) =>
                    Append(HandleChargeLoop(request), Phase20.Charging),

                (Phase20.WeldingDetection, _, _) when HasPostChargeSequence =>
                    Append(HandleWeldingDetection(request), Phase20.WeldingDetection),

                // SessionStopReq (in the normal SessionStop phase *and* any early-abort phase) is handled
                // ahead of this switch — see the top of Handle.

                // [V2G20-459]: not a request this phase permits, so it is answered with the corresponding
                // response carrying FAILED_SequenceError. Until 2026-08-11 this arm *threw* — the session
                // died without an answer, and the comment here said what the code would have sent if it
                // could. The -2 twin of that defect is the one tux-evse's VW capture found in us on
                // 2026-08-06 and that was fixed the same day; this is the half that never was.
                _ => RefuseStep(set, request, Refusal.SequenceError),
            };

            // A response from the FAILED family ends the session, whichever handler produced it: the station
            // has just said it is done, and there is no phase to advance to. The mirror of the EVCC's
            // Evcc20Base.RefuseOnFailure, and it arrived at the same time as the first handler that can
            // actually answer FAILED — until then no station of ours ever sent one, so the rule had nothing
            // to govern. Same >= FAILED family comparison, over all three generated ResponseCode enums.
            Phase = IsFailure(response) ? Phase20.Done : next;

            // After a charge-loop response the contactor is closed, so the next request must come inside
            // the tight ChargeLoopSequenceTimeout — Tables 216/217. Any other response leaves the baseline.
            _lastResponseWasChargeLoop = dispatchPhase == Phase20.Charging && request is not PowerDeliveryReq;

            return (respSet, response);
        }

        /// <summary>Whether a response carries a code from the <c>FAILED</c> family (OK 0–4, WARNING 5–20,
        /// FAILED 21 and up — the schema orders the enumeration by family, which
        /// <c>Evcc20FailureHandlingTests.TheResponseCodeFamiliesAreContiguousAndOrdered</c> pins).</summary>
        private static bool IsFailure(object response) => response switch
        {
            V2GResponseType      r => r.ResponseCode >= ResponseCode.FAILED,
            Ac20.V2GResponseType r => r.ResponseCode >= Ac20.ResponseCode.FAILED,
            Dc20.V2GResponseType r => r.ResponseCode >= Dc20.ResponseCode.FAILED,
            _                      => false,
        };

        // ── Refusals: the response that pairs with a request ────────────────────────────────────────

        /// <summary>Why a request is being refused — one reason per requirement, and the only two this
        /// station can raise before it has looked at the request's contents at all.</summary>
        /// <remarks>
        /// A reason rather than a <c>ResponseCode</c>, because there is no such thing as *the* -20 response
        /// code: CommonMessages, AC and DC each generate their own enum (the same three
        /// <see cref="IsFailure"/> has to switch over), and a value from one will not compile against
        /// another. Each arm of <see cref="Refuse"/> maps the reason into its own set's enum, where the two
        /// members happen to carry the same numbers in all three (34 and 38).
        /// </remarks>
        protected enum Refusal
        {
            /// <summary>`[V2G20-459]`: the request is not one this phase permits.</summary>
            SequenceError,

            /// <summary>`[V2G20-460]`: the request's SessionID is not the one this session was given.</summary>
            UnknownSession,
        }

        /// <summary>The name of the last request refused with <c>FAILED_UnknownSession</c> ([V2G20-460]), or
        /// null. The -20 twin of <c>Secc2.UnknownSessionAt</c>, and there for the same reason: a refusal is
        /// otherwise invisible from a run.</summary>
        public string? UnknownSessionAt { get; private set; }

        /// <summary>How many requests this session refused with <c>FAILED_UnknownSession</c>.</summary>
        public int UnknownSessionRefusals { get; private set; }

        /// <summary>The name of the last request refused with <c>FAILED_SequenceError</c> ([V2G20-459]), or
        /// null. Until 2026-08-11 this station <em>threw</em> instead of answering, so the only record of an
        /// out-of-sequence request was an aborted session.</summary>
        public string? SequenceErrorAt { get; private set; }

        /// <summary>How many requests this session refused with <c>FAILED_SequenceError</c>.</summary>
        public int SequenceErrorRefusals { get; private set; }

        /// <summary>The SessionID in <paramref name="request"/>'s header, or null when it is not a request
        /// of any of the three -20 sets — in which case there is nothing to compare and the
        /// <c>[V2G20-460]</c> guard stands down rather than refusing what it cannot read.</summary>
        private static byte[]? SessionIdOf(object request) => request switch
        {
            V2GRequestType      r => r.Header.SessionID,
            Ac20.V2GRequestType r => r.Header.SessionID,
            Dc20.V2GRequestType r => r.Header.SessionID,
            _                     => null,
        };

        /// <summary>
        /// The response that <b>pairs with</b> <paramref name="request"/>, carrying the code
        /// <paramref name="reason"/> names and nothing this station cannot mean.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The -20 counterpart of <c>Secc2.Refuse</c>, and the thing whose absence kept <c>[V2G20-459]</c>
        /// and <c>[V2G20-460]</c> open here long after the <c>-2</c> half was closed: this station used to
        /// <c>throw SessionAborted</c> on an out-of-sequence request — killing the connection without
        /// answering — because there was no table of corresponding responses to answer *from*. EVerest's
        /// <c>d20/context_helper.cpp</c> is the same table in C++, dispatched across all sixteen of their
        /// -20 message types, and is the worked example this was written against.
        /// </para>
        /// <para>
        /// <b>Every mandatory element is filled with something schema-conformant and meaningless</b>, which
        /// is <c>-20</c>'s equivalent of what <c>[V2G2-736]</c> spells out for <c>-2</c>: a refusal must
        /// still be a valid message. Where the schema forces content that would otherwise be a promise —
        /// the contract chain of a <c>CertificateInstallationRes</c>, the challenge of an
        /// <c>AuthorizationSetupRes</c> — it is filled with empty or zero material and never with a real
        /// credential. A refusal hands nothing out.
        /// </para>
        /// <para>
        /// <b>The phase is not this method's business.</b> <see cref="Handle"/> ends the session on any
        /// response from the FAILED family, so every refusal here is terminal — the opposite of
        /// <c>-2</c>, where a refusal leaves the phase alone and a car that corrects itself charges. The
        /// asymmetry is the two standards' (<c>-20</c> §8.6 against <c>-2</c> §8.8.2); the citations are at
        /// <c>Secc2.Dispatch</c>.
        /// </para>
        /// </remarks>
        protected (MessageSet Set, object Response) Refuse(MessageSet set, object request, Refusal reason)
        {

            var code = reason == Refusal.UnknownSession
                           ? ResponseCode.FAILED_UnknownSession
                           : ResponseCode.FAILED_SequenceError;

            var hdr = SessionCtx.ToCommonHeader();

            return request switch
            {
                SessionSetupReq             => (MessageSet.Iso20CommonMessages, new SessionSetupRes(hdr, code, "DE*ABC*E1")),

                // EIM with an empty mode block: the schema's choice is mandatory, and the PnC alternative
                // is the one that would carry a GenChallenge. A refusal issues no challenge — the same
                // reasoning as the 16 zero bytes in Secc2.Refuse's PaymentDetails arm — and advertises no
                // certificate installation, since it is not going to install one.
                AuthorizationSetupReq       => (MessageSet.Iso20CommonMessages, new AuthorizationSetupRes(hdr, code,
                                                   new[] { Authorization.EIM }, CertificateInstallationService: false,
                                                   new EIM_ASResAuthorizationModeType(), PnC_ASResAuthorizationMode: null)),

                AuthorizationReq            => (MessageSet.Iso20CommonMessages, new AuthorizationRes(hdr, code, Processing.Finished)),

                // The service list is what this station serves either way, so stating it costs nothing and
                // an empty EnergyTransferServiceList would not be schema-conformant (minOccurs is 1).
                ServiceDiscoveryReq         => (MessageSet.Iso20CommonMessages, new ServiceDiscoveryRes(hdr, code,
                                                   ServiceRenegotiationSupported: true,
                                                   new ServiceListType(EnergyServiceIds.Select(id => new ServiceType(id, FreeService: true)).ToArray()),
                                                   VASList: null)),

                // ServiceParameterList is mandatory and needs at least one ParameterSet, so the refusal
                // carries the Scheduled set this station really offers. Deliberately *not* recorded in
                // offeredParameterSets: [V2G20-433] holds a later selection against what was offered, and
                // nothing was offered here — the session is over.
                ServiceDetailReq r          => (MessageSet.Iso20CommonMessages, new ServiceDetailRes(hdr, code, r.ServiceID,
                                                   new ServiceParameterListType(new[] { ParamSet(1, controlMode: 1) }))),

                ServiceSelectionReq         => (MessageSet.Iso20CommonMessages, new ServiceSelectionRes(hdr, code)),

                // The one response whose mandatory payload would otherwise be a contract. Empty certificate
                // material and a SECP521 curve label satisfy the schema without issuing anything; the EV
                // aborts on the code and never reads it. Nothing here is signed, and nothing here is a key.
                CertificateInstallationReq  => (MessageSet.Iso20CommonMessages, new CertificateInstallationRes(hdr, code,
                                                   Processing.Finished,
                                                   new CertificateChainType([], SubCertificates: null),
                                                   new SignedInstallationDataType("refused",
                                                       new ContractCertificateChainType([], new SubCertificatesType([[]])),
                                                       EcdhCurve.SECP521, DHPublicKey: [],
                                                       SECP521_EncryptedPrivateKey: [], X448_EncryptedPrivateKey: null,
                                                       TPM_EncryptedPrivateKey: null),
                                                   RemainingContractCertificateChains: 0)),

                // The control-mode choice is mandatory; Dynamic is the one whose every element is optional,
                // so an empty one is the least this station can legally say. No schedule goes out with a
                // refusal — a ScheduleTuple is an offer.
                ScheduleExchangeReq         => (MessageSet.Iso20CommonMessages, new ScheduleExchangeRes(hdr, code,
                                                   Processing.Finished, GoToPause: null,
                                                   new Dynamic_SEResControlModeType(null, null, null, null, null),
                                                   Scheduled_SEResControlMode: null)),

                PowerDeliveryReq            => (MessageSet.Iso20CommonMessages, new PowerDeliveryRes(hdr, code, EVSEStatus: null)),
                SessionStopReq              => (MessageSet.Iso20CommonMessages, new SessionStopRes(hdr, code)),
                MeteringConfirmationReq     => (MessageSet.Iso20CommonMessages, new MeteringConfirmationRes(hdr, code)),

                // WPT/ACDP. This station runs no session state machine for either, so these only ever
                // arrive out of sequence — but they are CommonMessages requests, they decode, and a table
                // that answered every type except two would be the gap this one exists to close.
                VehicleCheckInReq           => (MessageSet.Iso20CommonMessages, new VehicleCheckInRes(hdr, code,
                                                   ParkingSpace: null, DeviceLocation: null, TargetDistance: null)),
                VehicleCheckOutReq          => (MessageSet.Iso20CommonMessages, new VehicleCheckOutRes(hdr, code,
                                                   EvseCheckOutStatus.Completed)),

                _                           => RefuseInEnergyTransferSet(set, request, reason),
            };

        }

        /// <summary>
        /// The AC/DC half of <see cref="Refuse"/>: the seven energy-transfer requests whose response types
        /// live in a message set this base class cannot name.
        /// </summary>
        /// <remarks>
        /// Same seam, and the same reason, as <see cref="HandleChargeLoop"/> and its siblings —
        /// <c>AC.Generated</c> and <c>DC.Generated</c> redeclare <c>ResponseCode</c>,
        /// <c>MessageHeaderType</c> and the rest as distinct CLR types, so only the subclass that owns a
        /// set can build a response in it. Throwing here rather than returning something is right: a base
        /// with no energy-transfer set has no answer to give, and a subclass that forgot to override this
        /// should fail loudly rather than silently drop a request type.
        /// </remarks>
        protected virtual (MessageSet Set, object Response) RefuseInEnergyTransferSet(MessageSet set, object request, Refusal reason) =>
            throw new NotSupportedException(
                $"no refusal response for {request.GetType().Name} — {GetType().Name} names no energy-transfer message set for it.");

        /// <summary>A refusal, plus the bookkeeping that makes it visible from a run and the phase the
        /// switch in <see cref="Handle"/> has to be handed. The phase is nominal: <see cref="Handle"/>
        /// re-decides it from <see cref="IsFailure"/> and lands on <see cref="Phase20.Done"/>.</summary>
        private (MessageSet, object, Phase20) RefuseStep(MessageSet set, object request, Refusal reason)
        {
            SequenceErrorAt = request.GetType().Name;
            SequenceErrorRefusals++;
            var (s, r) = Refuse(set, request, reason);
            return (s, r, Phase20.Done);
        }

        /// <summary>Reads/handles/replies over <paramref name="stream"/> until the session reaches <see cref="Phase20.Done"/>.</summary>
        public async Task RunAsync(Stream stream, CancellationToken ct = default)
        {
            // The vehicle's certificate comes from the handshake that already happened, so nothing above has
            // to pass it in. `-20` permits only full-handshake TLS, initial or resumed, so in a conformant
            // session this is always present; over plain TCP it stays null and a resume cannot be verified,
            // which SessionSetup then treats as a new session.
            VehicleLeafCertificate ??= SessionBinding20.PeerLeafOf(stream);

            // 8 KiB: a CertificateInstallationRes (contract chain + encrypted key + CPS chain + signature)
            // outgrows the 1 KiB the plain charge messages need.
            var buf = new byte[8192];
            while (!IsDone)
            {
                // Bound the wait for the next request by the sequence timeout that applies right now — 0,5 s
                // once a charge-loop response is out (contactor closed), the baseline otherwise. A silent EV
                // sends nothing, so this is what actually enforces the timeout; the reactive check in Handle
                // only catches a request that arrives late. Mirrors the EVCC's own read-timeout pattern.
                var readBudget = CurrentSequenceTimeout;
                MessageSet set;
                object message;
                using (var readCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    readCts.CancelAfter(readBudget);
                    try
                    {
                        (set, message) = await ReadFrame20Async(stream, readCts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (readCts.IsCancellationRequested && !ct.IsCancellationRequested)
                    {
                        throw new SessionAborted(
                            $"SECC sequence timeout: EV silent for > {readBudget.TotalMilliseconds:0} ms" +
                            (_lastResponseWasChargeLoop ? " in the charge loop" : ""));
                    }
                }

                // The mirror instrument: stop answering once a charge loop is running, holding the socket
                // open, so the *car's* V2G_EVCC_Msg_Timeout is what decides. Checked before Handle so the
                // request is swallowed whole rather than answered into the void.
                if (GoSilentInChargeLoop is { } silence && _lastResponseWasChargeLoop)
                {
                    SilenceEndedAfter = await WaitForPeerToEndSessionAsync(stream, silence, ct).ConfigureAwait(false);
                    return;
                }

                var (replySet, reply) = Handle(set, message);

                int n = EncodeAny(replySet, reply, buf);
                await V2GTPStream.WriteFrameAsync(stream, replySet, buf.AsMemory(0, n), ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Answer the charge loop once, then stop answering — with the connection <b>open</b> — and wait up
        /// to this budget for the EV to give up on its own. Null (the default) serves normally.
        /// <see cref="SilenceEndedAfter"/> carries the answer.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The exact mirror of <see cref="Evcc20Base.GoSilentInChargeLoop"/>, and it exists for the same
        /// reason one message set over: a station that hangs up is an EOF and tells you nothing about a
        /// timer, while one that goes quiet holding the socket is what <c>V2G_EVCC_Msg_Timeout</c> is
        /// defined against. Nothing here could do that until 2026-08-11, so no run had ever measured
        /// <b>any</b> EV's per-message timeout — ours included, which is how we found ours was checked only
        /// after the read returned and therefore never fired against a silent peer at all.
        /// </para>
        /// <para>
        /// The charge loop is the interesting place for the same reason it is on the other side: Tables 216,
        /// 217 and 218 tighten <c>V2G_EVCC_Msg_Timeout</c> to <b>0,5 s</b> there against Table 215's 2 s
        /// (<c>[V2G20-1499]</c>, <c>[V2G20-1501]</c>, <c>[V2G20-5069]</c>), and it is the phase in which the
        /// contactor is closed. It is also usable against a foreign EV in an interop run, which is the point
        /// of building it here rather than as a fixture.
        /// </para>
        /// </remarks>
        public TimeSpan? GoSilentInChargeLoop { get; set; }

        /// <summary>How long the EV kept the session after this station stopped answering, or null when it
        /// was still waiting at the end of <see cref="GoSilentInChargeLoop"/>.</summary>
        public TimeSpan? SilenceEndedAfter { get; private set; }

        /// <summary>Hold the connection open and read nothing but the peer's disconnect, up to
        /// <paramref name="budget"/>. Returns how long that took, or null if it never came.</summary>
        private async Task<TimeSpan?> WaitForPeerToEndSessionAsync(Stream stream, TimeSpan budget, CancellationToken ct)
        {

            var start = clock.GetUtcNow();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(budget);

            var scratch = new byte[256];
            try
            {
                while (true)
                {
                    int read = await stream.ReadAsync(scratch, cts.Token).ConfigureAwait(false);
                    if (read == 0)
                        return clock.GetUtcNow() - start;   // the EV closed: it gave up, and when
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                return null;                                 // still waiting when the budget ran out
            }
            catch (IOException)
            {
                return clock.GetUtcNow() - start;            // reset rather than a clean close
            }

        }

        /// <summary>
        /// <see cref="V2GTPStream.ReadFrameAsync"/> plus one interop tolerance: a Josev EVCC frames its -20
        /// <c>CertificateInstallationReq</c> with V2GTP payload type <b>0x8001</b> — the ISO 15118-<b>2</b>
        /// <c>EXI_ENCODED</c> value — because its <c>create_next_message</c> defaults the payload type and the
        /// cert-install call site forgets to pass <c>ISOV20PayloadTypes.MAINSTREAM</c> (0x8002). The regular
        /// dispatcher would route 0x8001 to the -2 codec ("Unknown document index 14", found live 2026-07-22).
        /// Inside a -20 session a 0x8001 frame can only be such a mis-framed message, so decode it as
        /// CommonMessages.
        /// </summary>
        private static async Task<(MessageSet Set, object Message)> ReadFrame20Async(Stream stream, CancellationToken ct)
        {
            var (frame, payloadType) = await V2GTPStream.ReadRawFrameAsync(stream, ct).ConfigureAwait(false);

            if (payloadType == V2GTPCodec.PayloadType_DinIso2Main)   // 0x8001 — see doc comment
                return (MessageSet.Iso20CommonMessages,
                        CommonMessagesCodec.DecodeAny(frame.AsSpan(V2GTPCodec.HeaderSize), out _));

            if (!V2GTPDispatcher.TryDecode(frame, out var set, out var message, out var error))
                throw new InvalidDataException($"V2GTP frame: {error}");
            return (set, message!);
        }

        /// <summary>Encodes a reply of any of the three message sets this project drives — implemented per concrete subclass since only it knows which DC/AC types it produces.</summary>
        protected abstract int EncodeAny(MessageSet set, object message, byte[] dest);

        private static (MessageSet, object, Phase20) Step(MessageSet set, object response, Phase20 next) => (set, response, next);

        /// <summary>
        /// SessionSetup, plus the phase its own answer implies: a <b>resumed</b> session opens at
        /// ChargeParameterDiscovery, a new one at AuthorizationSetup.
        /// </summary>
        /// <remarks>
        /// A resumed `-20` session repeats neither authorization — the paused session's stays valid for the
        /// whole service session — nor service negotiation: the standard names exactly one message the EV
        /// may send next, and it is the charge-parameter one. `-2` requires the opposite, both sides
        /// supplying the same values again, and this used to step unconditionally to AuthorizationSetup
        /// because it was written from `-2`. EVerest's station answered our resumed
        /// <c>AuthorizationSetupReq</c> with <c>FAILED_SequenceError</c> on 2026-08-08, which is how the
        /// difference surfaced. Deciding the phase from the response code keeps the two branches from
        /// drifting apart again.
        /// </remarks>
        private (MessageSet, object, Phase20) SessionSetupStep(SessionSetupReq request)
        {

            var response = SessionSetup(request);

            return Step(MessageSet.Iso20CommonMessages,
                        response,
                        response.ResponseCode == ResponseCode.OK_OldSessionJoined
                            ? Phase20.ChargeParams
                            : Phase20.AuthorizationSetup);

        }
        private static (MessageSet, object, Phase20) Append((MessageSet Set, object Response) result, Phase20 next) =>
            (result.Set, result.Response, next);

        /// <summary>The phases an EV polls (repeats) until it decides the step is done. PowerOn (PowerDelivery
        /// start) applies to both AC and DC; CableCheck/PreCharge/WeldingDetection are DC-only.</summary>
        private bool IsSelfLoopPhase(Phase20 p) =>
            p is Phase20.PowerOn
            || ((p is Phase20.CableCheck or Phase20.PreCharge) && HasPreChargeSequence)
            || (p is Phase20.WeldingDetection && HasPostChargeSequence);

        /// <summary>Where a self-looping phase hands off once its poll loop ends (the next-phase message arrives).</summary>
        private static Phase20 NextAfter(Phase20 p) => p switch
        {
            Phase20.CableCheck       => Phase20.PreCharge,
            Phase20.PreCharge        => Phase20.PowerOn,
            Phase20.PowerOn          => Phase20.Charging,
            Phase20.WeldingDetection => Phase20.SessionStop,
            _                        => p,
        };

        // ── CommonMessages phase handlers (identical for AC and DC — EIM only) ─
        /// <summary>
        /// Rejoins a paused session, or starts a new one.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Two conditions, not one. The session id has to match — and the requester has to be the EV that
        /// paused it, which `-20` makes a *shall* and leaves the method of proving open. We use the
        /// standard's own example, the TLS vehicle certificate hashed into the session id
        /// (<see cref="SessionBinding20"/>). Until 2026-08-08 only the first condition was checked, because
        /// this was written by analogy to `-2`, where the first condition is the whole rule; the comment
        /// that said so is what eventually gave the bug away.
        /// </para>
        /// <para>
        /// A failed check is not an error to report: it *is* a new session, with a fresh id and
        /// <c>OK_NewSessionEstablished</c>, and the EV that asked to resume learns nothing about why. That
        /// is deliberate in the standard and worth keeping — a distinguishable rejection would tell an
        /// attacker that the session id it guessed was real.
        /// </para>
        /// </remarks>
        private SessionSetupRes SessionSetup(SessionSetupReq req)
        {

            if (ResumeSessionId is not null &&
                req.Header.SessionID.AsSpan().SequenceEqual(ResumeSessionId) &&
                SessionBinding20.Matches(ResumeBinding, SessionBinding20.Compute(ResumeSessionId, VehicleLeafCertificate)))
            {
                SessionCtx.SessionId    = ResumeSessionId;
                SessionBinding          = ResumeBinding;
                SessionSetupCode        = ResponseCode.OK_OldSessionJoined;
                // The service session continues, so the service it settled on continues with it.
                SelectedEnergyServiceId = ResumeEnergyServiceId;
                return new SessionSetupRes(SessionCtx.ToCommonHeader(), ResponseCode.OK_OldSessionJoined, "DE*ABC*E1");
            }

            // A new session's id must differ from the one that was offered for the resume, so an EV whose
            // resume was refused cannot mistake the answer for an acceptance. FixedSessionId is the
            // recording seam and would otherwise be able to collide with it.
            var sessionId = FixedSessionId ?? System.Security.Cryptography.RandomNumberGenerator.GetBytes(8);
            while (ResumeSessionId is not null && sessionId.AsSpan().SequenceEqual(ResumeSessionId))
                sessionId = System.Security.Cryptography.RandomNumberGenerator.GetBytes(8);

            SessionCtx.SessionId = sessionId;
            SessionBinding       = SessionBinding20.Compute(sessionId, VehicleLeafCertificate);
            SessionSetupCode     = ResponseCode.OK_NewSessionEstablished;

            // A transaction begins when the session does, bound to it from the first byte — see
            // Secc2 for why an unbound OCPP record is worth nothing to compare against.
            _backend = Backend?.Invoke(Convert.ToHexString(SessionCtx.SessionId).ToLowerInvariant());
            return new SessionSetupRes(SessionCtx.ToCommonHeader(), ResponseCode.OK_NewSessionEstablished, "DE*ABC*E1");
        }

        /// <summary>
        /// Whether this station advertises Plug &amp; Charge alongside EIM. Default <c>true</c>, which is
        /// what every recorded session and the whole corpus contain.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Set to <c>false</c> and the station offers EIM only, with the EIM authorization-mode block and
        /// no challenge, and stops advertising contract installation. Both forms are legal; which one a
        /// station sends is a deployment choice, not a protocol one.
        /// </para>
        /// <para>
        /// <b>Why the switch exists.</b> An EV is supposed to pick the service it can use out of the list
        /// and ignore the rest. Not all of them do: eVDriveFlow's EVCC raises on the first entry it does
        /// not support — <c>NotImplementedError</c> — even when the EIM it does support is the next entry
        /// in the same list, which ends the session at authorization
        /// (<c>ISO15118ConformanceTests/docs/interop-runs/2026-08-01-edf-iso20-dc-dynamic-reverse/</c>). Offering less is how a
        /// station gets past a car like that, and it is the only way to reach anything behind
        /// authorization with such a peer.
        /// </para>
        /// <para>
        /// Same shape and same reason as <see cref="PreferDynamicControlMode"/>: a legal choice among
        /// legal alternatives, made switchable because a real counterparty forced the question. The
        /// default is unchanged, so nothing that exists today sees a different wire.
        /// </para>
        /// </remarks>
        public bool OfferPlugAndCharge { get; set; } = true;


        private AuthorizationSetupRes AuthSetup(AuthorizationSetupReq req)
        {
            // The challenge is a fresh 16 bytes the EV must echo back in a PnC AuthorizationReq
            // (ISO 15118-20 Table 62). Generated either way so the pinning seam behaves identically.
            _genChallenge = FixedGenChallenge ?? RandomNumberGenerator.GetBytes(16);

            // The response's authorization-mode params are a *choice* (exactly one of EIM/PnC).
            if (!OfferPlugAndCharge)
                return new(SessionCtx.ToCommonHeader(), ResponseCode.OK,
                    new[] { Authorization.EIM },
                    // No PnC on offer, so no contract provisioning either: an EV that took the offer up
                    // would be installing a certificate for an authorization method this station just
                    // said it does not do.
                    CertificateInstallationService: false,
                    EIM_ASResAuthorizationMode: new EIM_ASResAuthorizationModeType(),
                    PnC_ASResAuthorizationMode: null);

            // Offer both EIM and Plug & Charge. A PnC-capable EV (e.g. a Josev EVCC with a contract cert) will
            // pick PnC and sign its AuthorizationReq over this GenChallenge; our own loopback EVCC uses EIM.
            // To enable PnC we send the PnC mode (with the challenge) and leave EIM null — while still
            // advertising both in AuthorizationServices. An EIM-only EV (our loopback EVCC) ignores the mode
            // block and sends EIM regardless; a PnC EV (Josev with a contract cert) reads the challenge and signs.
            return new(SessionCtx.ToCommonHeader(), ResponseCode.OK,
                new[] { Authorization.PnC, Authorization.EIM },
                // Contract provisioning is offered: an EV that needs a contract cert (e.g. a Josev EVCC with
                // isCertInstallNeeded=true) sends a CertificateInstallationReq before its AuthorizationReq.
                CertificateInstallationService: true,
                EIM_ASResAuthorizationMode: null,
                new PnC_ASResAuthorizationModeType(_genChallenge, SupportedProviders: null));
        }

        /// <summary>
        /// One AuthorizationReq, and the phase that follows it: <see cref="Phase20.ServiceDiscovery"/>
        /// only once the station has actually granted — an <c>OK</c>-family code together with
        /// <see cref="Processing.Finished"/> — and this phase again otherwise.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="Auth"/> itself always grants, so nothing changes for a station that does not
        /// override it. What this opens is the two answers -20 provides and the machine could not
        /// previously survive: <c>Ongoing</c>, for a station waiting on something slow, and the
        /// <c>WARNING_*</c> family, which exists so a car whose credential was refused can try another
        /// one — <c>WARNING_AuthorizationSelectionInvalid</c> only means anything if it may choose
        /// again. A <c>FAILED</c> code still ends the session, decided after this returns.
        /// </para>
        /// <para>
        /// The family comparison is the one <see cref="IsFailure"/> uses, from the other end: the schema
        /// orders the enumeration OK (0–4), WARNING (5–20), FAILED (21 and up), which
        /// <c>Evcc20FailureHandlingTests.TheResponseCodeFamiliesAreContiguousAndOrdered</c> pins.
        /// </para>
        /// </remarks>
        private (MessageSet, object, Phase20) AuthStep(AuthorizationReq request)
        {
            var response = Auth(request);
            var granted = response.ResponseCode < ResponseCode.WARNING_AuthorizationSelectionInvalid
                          && response.EVSEProcessing == Processing.Finished;

            return Step(MessageSet.Iso20CommonMessages, response,
                        granted ? Phase20.ServiceDiscovery : Phase20.Authorization);
        }

        protected virtual AuthorizationRes Auth(AuthorizationReq req)
        {
            // Plug & Charge: validate the EV's signed AuthorizationReq (challenge echo + reference digest +
            // ECDSA signature over the contract leaf). We record the outcome rather than aborting, so a live
            // interop session completes and the verdict is observable; EIM carries no signature.
            if (req.PnC_AReqAuthorizationMode is { } pnc)
                PnCAuth = VerifyPnc(req, pnc);
            return new(SessionCtx.ToCommonHeader(), ResponseCode.OK, Processing.Finished);
        }

        /// <summary>Validates a PnC <see cref="AuthorizationReq"/>: the EV must echo our GenChallenge, the header
        /// signature's reference digest must match the re-encoded <c>PnC_AReqAuthorizationMode</c> fragment, and
        /// the SignedInfo signature must verify against the contract leaf's public key. Hashes are chosen from the
        /// message's own SignatureMethod/DigestMethod URIs (SHA-256 or SHA-512), so it works whatever the peer's
        /// contract-cert curve is (a real Josev PKI is P-256, not the -20-nominal secp521r1).</summary>
        private PnCAuthResult VerifyPnc(AuthorizationReq req, PnC_AReqAuthorizationModeType pnc)
        {
            bool challengeOk = _genChallenge is not null && pnc.GenChallenge.AsSpan().SequenceEqual(_genChallenge);

            var buf = new byte[8192];
            if (!CommonMessagesCodec.EncodeFragment_PnC_AReqAuthorizationMode(pnc, buf, out int n))
                return new PnCAuthResult(challengeOk, DigestOk: false, SignatureOk: false, "?", "fragment-encode-failed", "none", ChainResult.NotConfigured);
            var fragment = buf.AsSpan(0, n);

            if (req.Header.Signature is not { } sig || sig.SignedInfo.Reference.Count == 0)
                return new PnCAuthResult(challengeOk, false, false, "?", "no-signature", "none", ChainResult.NotConfigured);

            var reference = sig.SignedInfo.Reference[0];
            bool digestOk = HashOf(reference.DigestMethod.Algorithm, fragment).AsSpan().SequenceEqual(reference.DigestValue);

            string subject = "?";
            bool signatureOk = false;
            string grammar = "none";
            var chain = ChainResult.NotConfigured;
            try
            {
                using var contract = X509CertificateLoader.LoadCertificate(pnc.ContractCertificateChain.Certificate);
                subject = contract.Subject;
                if (ContractChainValidator is not null)
                    chain = ContractChainValidator.Validate(contract, pnc.ContractCertificateChain.SubCertificates?.Certificate);
                using var ecdsa = contract.GetECDsaPublicKey();
                if (ecdsa is not null)
                {
                    var hashName = HashNameFor(sig.SignedInfo.SignatureMethod.Algorithm);

                    // 1. Our production grammar: SignedInfo as a fragment of the full CommonMessages schema set
                    //    (byte-exact vs cbV2G). This is what our own EVCC signs.
                    if (ecdsa.VerifyData(V2GSignature.SignedInfoFragment(sig.SignedInfo), sig.SignatureValue.Value,
                                         hashName, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
                        (signatureOk, grammar) = (true, "iso20-commonmessages");

                    // 2. Interop fallback: SignedInfo over the standalone xmldsig grammar (what Josev's stack
                    //    signs — see XmlDsigInteropVerify). Verify-only; we never sign this form.
                    else if (XmlDsigInteropVerify.VerifyStandaloneXmldsig(sig.SignedInfo, sig.SignatureValue.Value, ecdsa, hashName))
                        (signatureOk, grammar) = (true, "xmldsig-standalone");
                }
            }
            catch (Exception ex) { subject = $"cert-error: {ex.Message}"; }

            return new PnCAuthResult(challengeOk, digestOk, signatureOk, subject,
                                     sig.SignedInfo.SignatureMethod.Algorithm, grammar, chain);
        }

        private static byte[] HashOf(string algorithmUri, ReadOnlySpan<byte> data) =>
            algorithmUri.Contains("sha256") ? SHA256.HashData(data) : SHA512.HashData(data);

        private static HashAlgorithmName HashNameFor(string algorithmUri) =>
            algorithmUri.Contains("sha256") ? HashAlgorithmName.SHA256 : HashAlgorithmName.SHA512;

        /// <summary>The ISO 15118-20 energy-transfer service ids this SECC advertises (Table 204): DC=2, AC=1,
        /// plus their bidirectional (BPT) variants DC_BPT=6, AC_BPT=5. A live Josev EVCC rejects a session with
        /// <c>WrongServiceID</c> if the mode it wants (e.g. DC_BPT) is not offered, so each subclass advertises
        /// both its unidirectional and BPT service; the actual direction is driven per-message by whether the
        /// EV sends a BPT energy-transfer-mode / control-mode (see the DC/AC charge-parameter and charge-loop
        /// hooks).</summary>
        protected abstract IReadOnlyList<ushort> EnergyServiceIds { get; }

        /// <summary>The <c>Connector</c> value advertised in the ServiceDetail parameter sets. The enum is
        /// per energy-transfer service: for AC/DC it is the connector type, for MCS it is the MCS connector
        /// family (1 = MCS, 4 = rMCS, 5 = xMCS). Value 1 is the plain connector in every case, so the
        /// default suits AC, DC and MCS alike; override for the reduced/extended MCS variants.</summary>
        protected virtual int ConnectorParameter => 1;

        /// <summary>The <c>SupportedServiceIDs</c> the EV sent in <c>ServiceDiscoveryReq</c>, or <c>null</c>
        /// when it sent none — which Table 38 of `[V2G20-1248]` makes the ordinary case and defines as
        /// *"send me everything"*.</summary>
        /// <remarks>
        /// Recorded rather than acted on, and the requirement side says that is right: Table 38 describes
        /// the element as a filter the EV <i>can</i> use, and the sentence about returning a filtered list
        /// sits on <c>VASList</c> in Table 39 (`[V2G20-1249]`) rather than on
        /// <c>EnergyTransferServiceList</c> — and this station sends no VAS list at all. So answering with
        /// the full energy-transfer catalogue is conformant with or without a filter.
        /// <para>The same shape and the same reason as <see cref="MeterInfoRequestedByEv"/>: an optional
        /// request field is invisible from both ends of a loopback unless the station writes down that it
        /// arrived.</para>
        /// </remarks>
        public IReadOnlyList<ushort>? SupportedServiceIdsRequestedByEv { get; protected set; }

        private ServiceDiscoveryRes SvcDiscovery(ServiceDiscoveryReq req)
        {

            SupportedServiceIdsRequestedByEv = req.SupportedServiceIDs?.ServiceID;

            return new(SessionCtx.ToCommonHeader(), ResponseCode.OK, ServiceRenegotiationSupported: true,
                       new ServiceListType(EnergyServiceIds.Select(id => new ServiceType(id, FreeService: true)).ToArray()),
                       VASList: null);

        }

        /// <summary>
        /// The standard -20 energy-transfer parameter sets. A live Josev EVCC requires at least the
        /// ControlMode parameter ("Control mode parameter missing" otherwise). MobilityNeedsMode=1
        /// (mobility needs provided by the EVCC) is legal for both modes ([V2G20-2663] only restricts
        /// MobilityNeedsMode=2 to Dynamic).
        /// </summary>
        /// <remarks>
        /// Lifted out of <see cref="SvcDetail"/> on 2026-08-11 because <see cref="Refuse"/> needs one too:
        /// -20 makes <c>ServiceParameterList</c> mandatory with at least one <c>ParameterSet</c>, so even a
        /// refusal has to carry a set — and it should carry one this station actually offers rather than an
        /// invented number. What the refusal does *not* do is record it: see the note at
        /// <see cref="offeredParameterSets"/>.
        /// </remarks>
        private ParameterSetType ParamSet(ushort id, int controlMode) => new(id, new[]
        {
            new ParameterType("Connector", null, null, null, IntValue: ConnectorParameter, null, null),
            new ParameterType("ControlMode", null, null, null, IntValue: controlMode, null, null),
            new ParameterType("MobilityNeedsMode", null, null, null, IntValue: 1, null, null),
            new ParameterType("Pricing", null, null, null, IntValue: 0, null, null),
        });

        private ServiceDetailRes SvcDetail(ServiceDetailReq req)
        {
            // We offer both control modes — set 1: Scheduled (ControlMode=1), set 2: Dynamic
            // (ControlMode=2) — ordered by PreferDynamicControlMode, since a Josev EVCC adopts the *first*
            // offered set's ControlMode.
            var scheduled = ParamSet(1, controlMode: 1);
            var dynamic   = ParamSet(2, controlMode: 2);
            var res = new ServiceDetailRes(SessionCtx.ToCommonHeader(), ResponseCode.OK, req.ServiceID,
                new ServiceParameterListType(PreferDynamicControlMode
                    ? new[] { dynamic, scheduled }
                    : new[] { scheduled, dynamic }));

            // Remember the pairs as they leave, rather than re-deriving them at selection time: what the EV
            // may name later is what this station actually put on the wire ([V2G20-1216]), and reading it off
            // the response is the only version of that which cannot drift from it.
            foreach (var set in res.ServiceParameterList.ParameterSet)
                offeredParameterSets.Add((req.ServiceID, set.ParameterSetID));

            return res;
        }

        /// <summary>Every <c>(ServiceID, ParameterSetID)</c> this station has offered in a
        /// <c>ServiceDetailRes</c> this session — the set <c>[V2G20-433]</c> holds a selection against, and
        /// the set <c>[V2G20-1216]</c> confines the EVCC to.</summary>
        /// <remarks>Not cleared by a service renegotiation: the requirement is about what was provided during
        /// the <i>session</i>, and a renegotiation stays inside one.</remarks>
        private readonly HashSet<(UInt16 ServiceId, UInt16 ParameterSetId)> offeredParameterSets = [];

        /// <summary>The energy-transfer service the EV selected in ServiceSelection; 0 before that phase (and
        /// re-set from scratch by a service renegotiation, which re-enters ServiceDiscovery). Kept because the
        /// selected service governs what the rest of the session may say — see
        /// <see cref="BidirectionalServiceSelected"/>.
        /// <para>
        /// <b>Public, like <c>Evcc20Base.SelectedEnergyServiceId</c>, and for the same reason.</b> Which
        /// service a session settled on is what distinguishes MCS from DC — the two are identical on the
        /// wire otherwise — and in the <i>reverse</i> direction the station is the only side that can
        /// report it. While this was <c>protected</c>, a run driven by a foreign EV could not say which
        /// entry of our own catalogue that EV had picked.
        /// </para></summary>
        public ushort SelectedEnergyServiceId { get; private set; }

        /// <summary>
        /// Whether the selected service is bidirectional, and so whether the charge-parameter and
        /// control-mode types the EV sends from here on must be the <c>BPT_*</c> ones.
        /// </summary>
        /// <remarks>
        /// The SECC's half of the same rule the EVCC applies to decide what to <i>send</i>; here it decides
        /// what the station will <i>accept</i>. Consulted by both charge-parameter handlers —
        /// see <see cref="Secc20Dc.HandleChargeParameterDiscovery"/> for what a mismatch costs and why the
        /// check is there rather than nowhere.
        /// </remarks>
        protected bool BidirectionalServiceSelected
            => EnergyTransferService.IsBidirectional(SelectedEnergyServiceId);

        /// <summary>
        /// ServiceDetail and ServiceSelection, both refusing an id this station never advertised.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Both used to echo whatever id arrived: <c>SvcDetail</c> answered <c>OK</c> with the EV's own
        /// number in it, and <c>SvcSelection</c> assigned it to <see cref="SelectedEnergyServiceId"/> and
        /// answered <c>OK</c>. So an EV could select a service that was never in the catalogue — service 99,
        /// or DC against an AC station — and the session went on with it.
        /// </para>
        /// <para>
        /// <b>That is not cosmetic, because the value is load-bearing.</b>
        /// <see cref="BidirectionalServiceSelected"/> reads it to decide whether the charge-parameter and
        /// control-mode types the EV sends next must be the <c>BPT_*</c> ones, so an unadvertised id decides
        /// a conformance check with a number the station never stood behind.
        /// </para>
        /// <para>
        /// It is also the mirror of the defect EVerest found in <em>us</em> on 2026-08-03: our EVCC named an
        /// energy transfer mode instead of reading what <c>ServiceDiscoveryRes</c> offered, and their station
        /// was right to refuse it (<c>docs/interop-runs/2026-08-03-everest-ac/</c>). We read their catalogue
        /// from that day on and did not check our own.
        /// </para>
        /// <para>
        /// <b>Both refusals are required, and the standard names these two codes for exactly these two
        /// cases.</b> <c>[V2G20-425]</c> and <c>[V2G20-464]</c>: <c>FAILED_ServiceIDInvalid</c> when the
        /// <c>ServiceDetailReq</c> carries a ServiceID that was not in the offered
        /// <c>EnergyTransferServiceList</c> or <c>VASList</c>. <c>[V2G20-433]</c> and <c>[V2G20-467]</c>:
        /// <c>FAILED_ServiceSelectionInvalid</c> for the same in <c>ServiceSelectionReq</c>.
        /// <c>[V2G20-1216]</c> is the EVCC's mirror — it may only use ids the SECC gave it this session.
        /// This comment said until 2026-08-09 that no requirement text about service selection was
        /// available here; it is (see <c>ISO15118ConformanceTests/docs/normative-basis.md</c> for what may
        /// be cited from where), and it turns out the behaviour written here on the grounds that echoing
        /// an unadvertised id is *surprising* is the behaviour the standard obliges.
        /// </para>
        /// <para>
        /// <b>The selection is held to the pair, not to the id.</b> <c>[V2G20-433]</c> speaks of a
        /// <i>ServiceID, ParameterSetID</i> pair the SECC never offered, and until 2026-08-10 this looked
        /// only at the id. <see cref="offeredParameterSets"/> now records what each
        /// <c>ServiceDetailRes</c> actually carried, and a selection is checked against that — which also
        /// refuses a service whose detail was never requested, since ParameterSetIDs exist nowhere else and
        /// a car naming one it was never given is naming a value it invented (<c>[V2G20-1216]</c>).
        /// </para>
        /// <para>
        /// <b><c>[V2G20-1618]</c> is deliberately not implemented, because this station cannot produce the
        /// case.</b> It wants <c>FAILED_NoEnergyTransferServiceSelected</c> where the selection names no
        /// energy transfer service at all. Two things rule that out here: the schema makes
        /// <c>SelectedEnergyTransferService</c> a mandatory single element, so a request always names one;
        /// and the only way to name a non-energy-transfer service is a VAS id, while
        /// <see cref="SvcDiscovery"/> sends <c>VASList: null</c>. An id that is neither is simply
        /// unadvertised, which is <c>[V2G20-467]</c>'s case and answered above. Writing the branch anyway
        /// would be unreachable code that reads as coverage. Worth revisiting the day this station
        /// advertises a value-added service, and not before.
        /// </para>
        /// <para>
        /// The phase these return is not the one that takes effect: <see cref="Handle"/> ends the session on
        /// any failure response, whatever the handler asked for. So a refusal here is terminal, unlike its
        /// <c>-2</c> counterpart, which stays put and lets a corrected request through — and that
        /// difference is the two standards', not an accident. <c>Secc2.Dispatch</c> carries the citations;
        /// the short of it is that <c>-20</c> §8.6 calls a <c>FAILED</c> response a fatal error and has both
        /// sides terminate the session, while <c>-2</c> §8.8.2 obliges only that the EVCC ignore the other
        /// parameters (`[V2G2-735]`).
        /// </para>
        /// </remarks>
        private (MessageSet, object, Phase20) SvcDetailStep(ServiceDetailReq req)
            => Advertised(req.ServiceID)
                   ? Step(MessageSet.Iso20CommonMessages, SvcDetail(req), Phase20.ServiceSelection)
                   : (MessageSet.Iso20CommonMessages,
                      new ServiceDetailRes(SessionCtx.ToCommonHeader(), ResponseCode.FAILED_ServiceIDInvalid,
                                           req.ServiceID, new ServiceParameterListType(Array.Empty<ParameterSetType>())),
                      Phase20.ServiceDetail);

        private (MessageSet, object, Phase20) SvcSelectionStep(ServiceSelectionReq req)
        {
            var selected = req.SelectedEnergyTransferService;
            var id       = selected.ServiceID;

            // [V2G20-433] is about the *pair*, not the id: a parameter set this station never offered for
            // this service is as unadvertised as an id it never offered at all. Selecting a service whose
            // detail was never asked for lands here too, and correctly — the ParameterSetIDs exist only in a
            // ServiceDetailRes, so a car naming one it was not given is naming a value it invented.
            if (!Advertised(id) || !offeredParameterSets.Contains((id, selected.ParameterSetID)))
                return (MessageSet.Iso20CommonMessages,
                        new ServiceSelectionRes(SessionCtx.ToCommonHeader(), ResponseCode.FAILED_ServiceSelectionInvalid),
                        Phase20.ServiceSelection);

            SelectedEnergyServiceId = id;
            return Step(MessageSet.Iso20CommonMessages,
                        new ServiceSelectionRes(SessionCtx.ToCommonHeader(), ResponseCode.OK),
                        Phase20.ChargeParams);
        }

        /// <summary>Whether <paramref name="serviceId"/> is one this station put in its own
        /// <c>ServiceDiscoveryRes</c> — which is <see cref="EnergyServiceIds"/> and nothing else, since
        /// <see cref="SvcDiscovery"/> advertises no value-added services.</summary>
        private bool Advertised(ushort serviceId) => EnergyServiceIds.Contains(serviceId);

        private ScheduleExchangeRes ScheduleExchange(ScheduleExchangeReq req)
        {
            // Answer in kind ([V2G20-1600]): a Dynamic-mode EV sends Dynamic_SEReqControlMode and must get a
            // Dynamic res (all fields optional — Processing=Finished is the actual signal); a Scheduled-mode
            // EV gets the schedule-tuple offer below.
            // What the car chose, recorded before it is answered — see EvControlModeIsDynamic. This is the
            // one message that carries the decision, so it is the only place it can be read.
            EvControlModeIsDynamic = req.Dynamic_SEReqControlMode is not null;

            if (req.Dynamic_SEReqControlMode is not null)
                return new(SessionCtx.ToCommonHeader(), ResponseCode.OK, Processing.Finished, GoToPause: false,
                    Dynamic_SEResControlMode: new Dynamic_SEResControlModeType(
                        DepartureTime: req.Dynamic_SEReqControlMode.DepartureTime,
                        MinimumSOC: null, TargetSOC: null, AbsolutePriceSchedule: null, PriceLevelSchedule: null),
                    Scheduled_SEResControlMode: null);

            var powerSchedule = new PowerScheduleType(TimeAnchor: 0, AvailableEnergy: null, PowerTolerance: null,
                new PowerScheduleEntryListType(new[] { new PowerScheduleEntryType(Duration: 3600, Power: new RationalNumberType(0, 100), null, null) }));

            // A ChargingSchedule must carry a price schedule (either PriceLevel or AbsolutePrice) — a live
            // Josev EVCC rejects the tuple otherwise. Default: PriceLevelSchedule, the compact form (one
            // flat level). With a TariffSignKey: the rich, digitally signed AbsolutePriceSchedule instead.
            ChargingScheduleType chargingSchedule;
            var header = SessionCtx.ToCommonHeader();
            if (TariffSignKey is null)
            {
                var priceLevelSchedule = new PriceLevelScheduleType(Id: null, TimeAnchor: 0, PriceScheduleID: 1,
                    PriceScheduleDescription: null, NumberOfPriceLevels: 1,
                    new PriceLevelScheduleEntryListType(new[] { new PriceLevelScheduleEntryType(Duration: 3600, PriceLevel: 0) }));
                chargingSchedule = new ChargingScheduleType(powerSchedule, AbsolutePriceSchedule: null, priceLevelSchedule);
            }
            else
            {
                var priceSchedule = AbsolutePriceSchedule();
                chargingSchedule = new ChargingScheduleType(powerSchedule, priceSchedule, PriceLevelSchedule: null);
                header = header with { Signature = SignPriceSchedule(priceSchedule) };
            }

            var scheduleTuple = new ScheduleTupleType(ScheduleTupleID: 1,
                ChargingSchedule: chargingSchedule,
                DischargingSchedule: null);

            return new(header, ResponseCode.OK, Processing.Finished, GoToPause: false,
                Dynamic_SEResControlMode: null,
                Scheduled_SEResControlMode: new Scheduled_SEResControlModeType(new[] { scheduleTuple }));
        }

        /// <summary>The rich -20 tariff: energy fees per power band (≤ 11 kW cheap, above pricier), stepping
        /// up after 30 min — the -20 analogue of the -2 SalesTariff offer. Fees are EUR/kWh scaled 10^-2
        /// (0.25 → RationalNumber(-2, 25)).</summary>
        private static AbsolutePriceScheduleType AbsolutePriceSchedule() =>
            new(Id: "absolutePriceSchedule1", TimeAnchor: 0, PriceScheduleID: 1,
                PriceScheduleDescription: "off-peak first",
                Currency: "EUR", Language: "en",
                PriceAlgorithm: "urn:iso:std:iso:15118:-20:PriceAlgorithm:1-Power",
                MinimumCost: null, MaximumCost: null, TaxRules: null,
                PriceRuleStacks: new PriceRuleStackListType(new[]
                {
                    new PriceRuleStackType(Duration: 1800, new[]
                    {
                        PriceRule(feeCents: 25, powerRangeStartKw: 0),
                        PriceRule(feeCents: 35, powerRangeStartKw: 11),
                    }),
                    new PriceRuleStackType(Duration: 1800, new[]
                    {
                        PriceRule(feeCents: 30, powerRangeStartKw: 0),
                        PriceRule(feeCents: 45, powerRangeStartKw: 11),
                    }),
                }),
                OverstayRules: null, AdditionalSelectedServices: null);

        private static PriceRuleType PriceRule(short feeCents, short powerRangeStartKw) =>
            new(EnergyFee: new RationalNumberType(-2, feeCents),
                ParkingFee: null, ParkingFeePeriod: null,
                CarbonDioxideEmission: null, RenewableGenerationPercentage: null,
                PowerRangeStart: new RationalNumberType(3, powerRangeStartKw));

        /// <summary>Signs the AbsolutePriceSchedule into the response header (one reference over the
        /// schedule's EXI fragment, ECDSA-P521/SHA-512 — the -20 mandatory suite; fachlich the eMSP's
        /// signature, here the configured tariff key). Transforms=[EXI C14N] is included because Josev's
        /// pydantic Reference model requires it to even parse the message. NOTE the honest validation
        /// limit: no external implementation verifies -20 price-schedule signatures — only our own EVCC
        /// (loopback/CI) checks this.</summary>
        private SignatureType SignPriceSchedule(AbsolutePriceScheduleType priceSchedule)
        {
            var buf = new byte[4096];
            if (!CommonMessagesCodec.EncodeFragment_AbsolutePriceSchedule(priceSchedule, buf, out int n))
                throw new InvalidOperationException("AbsolutePriceSchedule fragment encode failed.");

            var signedInfo = V2GSignature.BuildSignedInfo(priceSchedule.Id!,
                V2GSignature.Digest(buf.AsSpan(0, n)), includeExiTransform: true);
            return V2GSignature.BuildSignature(signedInfo, V2GSignature.Sign(signedInfo, TariffSignKey!));
        }

        /// <summary>
        /// Handles a live -20 <c>CertificateInstallationReq</c>: (1) verifies the OEM-provisioning-chain
        /// signature — reference digest over the <c>OEMProvisioningCertificateChain</c> EXI fragment, then the
        /// ECDSA signature over the SignedInfo under our combined grammar or the Josev standalone-xmldsig one
        /// (same dual-grammar dance as <see cref="VerifyPnc"/>) — and (2) issues a throwaway dev contract:
        /// fresh P-521 contract + CPS certs, the contract private scalar ECDH/AES-GCM-wrapped for the EV's OEM
        /// key (<see cref="ContractProvisioning"/>; a P-256 OEM cert — Josev's — cannot join the secp521r1
        /// ECDH, so the blob is then well-formed but undecryptable and <c>EncryptedForOem</c> is false), and
        /// the <c>SignedInstallationData</c> signed with the CPS leaf via <c>V2GSignature</c>.
        /// </summary>
        private CertificateInstallationRes CertInstallation(CertificateInstallationReq req)
        {
            // ── 1. verify the EV's signature over the OEM provisioning chain ─
            var chain = req.OEMProvisioningCertificateChain;
            var fragBuf = new byte[8192];
            bool fragOk = CommonMessagesCodec.EncodeFragment_OEMProvisioningCertificateChain(chain, fragBuf, out int fragLen);

            bool digestOk = false, signatureOk = false;
            string grammar = "none", oemSubject = "?";
            var oemChain = ChainResult.NotConfigured;
            ECDsa? oemVerifyKey = null;
            try
            {
                using var oemLeaf = X509CertificateLoader.LoadCertificate(chain.Certificate);
                // Whether the car asking for a contract is a car its OEM actually built. The sub-CAs were
                // carried in the message all along and read by nobody until there was a validator.
                if (ContractChainValidator is not null)
                    oemChain = ContractChainValidator.Validate(oemLeaf, chain.SubCertificates?.Certificate);
                oemSubject = oemLeaf.Subject;
                oemVerifyKey = oemLeaf.GetECDsaPublicKey();

                if (fragOk && req.Header.Signature is { } sig && sig.SignedInfo.Reference.Count > 0 && oemVerifyKey is not null)
                {
                    var reference = sig.SignedInfo.Reference[0];
                    digestOk = HashOf(reference.DigestMethod.Algorithm, fragBuf.AsSpan(0, fragLen))
                        .AsSpan().SequenceEqual(reference.DigestValue);

                    var hashName = HashNameFor(sig.SignedInfo.SignatureMethod.Algorithm);
                    if (oemVerifyKey.VerifyData(V2GSignature.SignedInfoFragment(sig.SignedInfo), sig.SignatureValue.Value,
                                                hashName, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
                        (signatureOk, grammar) = (true, "iso20-commonmessages");
                    else if (XmlDsigInteropVerify.VerifyStandaloneXmldsig(sig.SignedInfo, sig.SignatureValue.Value, oemVerifyKey, hashName))
                        (signatureOk, grammar) = (true, "xmldsig-standalone");
                }
            }
            catch (Exception ex) { oemSubject = $"cert-error: {ex.Message}"; }
            finally { oemVerifyKey?.Dispose(); }

            // ── 2. issue a dev contract, key-wrapped for the EV's OEM key where possible ─
            using var contractKey = ECDsa.Create(ECCurve.NamedCurves.nistP521);
            using var contractCert = new CertificateRequest("CN=DE*VAN*C*000001, O=Vanaheimr (dev)", contractKey, HashAlgorithmName.SHA512)
                .CreateSelfSigned(clock.GetUtcNow().AddMinutes(-5), clock.GetUtcNow().AddYears(2));
            using var cpsKey = ECDsa.Create(ECCurve.NamedCurves.nistP521);
            using var cpsCert = new CertificateRequest("CN=Vanaheimr CPS (dev)", cpsKey, HashAlgorithmName.SHA512)
                .CreateSelfSigned(clock.GetUtcNow().AddMinutes(-5), clock.GetUtcNow().AddYears(2));

            bool encryptedForOem = false;
            byte[] dhPub, wrapped;
            using (var oemLeaf = X509CertificateLoader.LoadCertificate(chain.Certificate))
            {
                var oemEcdh = TryGetP521KeyAgreement(oemLeaf);
                if (oemEcdh is not null)
                {
                    using (oemEcdh)
                        (dhPub, wrapped) = ContractProvisioning.EncryptContractKey(oemEcdh.PublicKey, contractKey);
                    encryptedForOem = true;
                }
                else
                {
                    // Non-P-521 OEM key (e.g. Josev's -2-era P-256 provisioning cert): no shared secp521r1
                    // ECDH exists. Fill the mandatory choice with a well-formed blob wrapped for a throwaway
                    // recipient so the message stays schema-valid; the EV cannot unwrap it (recorded above).
                    using var throwaway = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP521);
                    (dhPub, wrapped) = ContractProvisioning.EncryptContractKey(throwaway.PublicKey, contractKey);
                }
            }

            var installData = new SignedInstallationDataType("sid1",
                new ContractCertificateChainType(contractCert.RawData, new SubCertificatesType(new[] { cpsCert.RawData })),
                EcdhCurve.SECP521, dhPub,
                SECP521_EncryptedPrivateKey: wrapped, X448_EncryptedPrivateKey: null, TPM_EncryptedPrivateKey: null);

            var dataBuf = new byte[8192];
            if (!CommonMessagesCodec.EncodeFragment_SignedInstallationData(installData, dataBuf, out int dataLen))
                throw new InvalidOperationException("SignedInstallationData fragment encode failed.");
            // includeExiTransform: Josev's pydantic Reference model treats the (schema-optional) Transforms
            // as mandatory — without it, its EVCC rejects the whole res with a V2GMessageValidationError
            // before even reaching its own (unimplemented) cert-install handling. Found live 2026-07-22.
            var signedInfo = V2GSignature.BuildSignedInfo("sid1", V2GSignature.Digest(dataBuf.AsSpan(0, dataLen)),
                                                          includeExiTransform: true);
            var resSignature = V2GSignature.BuildSignature(signedInfo, V2GSignature.Sign(signedInfo, cpsKey));

            CertInstall = new CertInstallResult(digestOk, signatureOk, grammar, oemSubject, encryptedForOem, oemChain);

            return new CertificateInstallationRes(
                SessionCtx.ToCommonHeader() with { Signature = resSignature },
                ResponseCode.OK, Processing.Finished,
                CPSCertificateChain: new CertificateChainType(cpsCert.RawData, SubCertificates: null),
                SignedInstallationData: installData,
                RemainingContractCertificateChains: 0);
        }

        /// <summary>The OEM leaf's key-agreement handle, but only if it is the P-521 key -20 provisioning
        /// requires; <c>null</c> for any other curve (or a non-EC key).</summary>
        private static ECDiffieHellman? TryGetP521KeyAgreement(X509Certificate2 oemLeaf)
        {
            var ecdh = oemLeaf.GetECDiffieHellmanPublicKey();
            if (ecdh is null) return null;
            if (ecdh.KeySize == 521) return ecdh;
            ecdh.Dispose();
            return null;
        }

        private PowerDeliveryRes PowerDelivery(PowerDeliveryReq req) =>
            new(SessionCtx.ToCommonHeader(), ResponseCode.OK, EVSEStatus: null);

        private SessionStopRes SessionStop(SessionStopReq req) =>
            new(SessionCtx.ToCommonHeader(), ResponseCode.OK);
    }
}
