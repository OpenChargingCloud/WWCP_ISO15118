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
using Vanaheimr.V2G.Simulation.Framing;
using Vanaheimr.V2G.Simulation.Session;
using cloud.charging.open.protocols.ISO15118.EXI.Dispatch;

namespace Vanaheimr.V2G.Simulation.StateMachines.Iso20
{
    /// <summary>Outcome of validating a live Plug &amp; Charge <c>AuthorizationReq</c>: whether the EV echoed our
    /// <c>GenChallenge</c>, whether the signed-element digest matched its reference, and whether the ECDSA
    /// signature verified against the contract leaf — plus what was observed (contract subject, signature method,
    /// and which <c>SignedInfo</c> grammar the signature verified under: <c>iso20-commonmessages</c> for our/cbV2G
    /// combined-schema form, <c>xmldsig-standalone</c> for the Josev-style standalone-xmldsig form, or
    /// <c>none</c>).</summary>
    public sealed record PnCAuthResult(bool ChallengeOk, bool DigestOk, bool SignatureOk, string ContractSubject,
                                       string SignatureMethod, string SignatureGrammar);

    /// <summary>Outcome of a live -20 <c>CertificateInstallationReq</c> (contract provisioning): whether the
    /// OEM-provisioning-chain digest and ECDSA signature verified (and under which SignedInfo grammar — see
    /// <see cref="PnCAuthResult"/>), the OEM leaf's subject, and whether the issued contract key could actually
    /// be <b>encrypted for</b> that OEM key (requires a P-521 OEM-provisioning key; a -2-era P-256 OEM cert —
    /// what Josev ships — cannot take part in the -20 secp521r1 ECDH, so the response is well-formed but
    /// undecryptable for the EV).</summary>
    public sealed record CertInstallResult(bool DigestOk, bool SignatureOk, string SignatureGrammar,
                                           string OemSubject, bool EncryptedForOem);

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
        /// (<c>ResponseCode.OK_OldSessionJoined</c>); anything else starts fresh.</summary>
        public byte[]? ResumeSessionId { get; set; }

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
        public Func<string, Ocpp.OcppTransactionRecorder>? Backend { get; init; }

        private Ocpp.OcppTransactionRecorder? _backend;
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
            if (Phase is not Phase20.SessionSetup && now - _lastSeen > sequenceTimeout)
                throw new SessionAborted($"SECC sequence timeout: EV silent for > {sequenceTimeout.TotalSeconds:0}s");
            _lastSeen = now;

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

            var (respSet, response, next) = (Phase, set, request) switch
            {
                (Phase20.SessionSetup, MessageSet.Iso20CommonMessages, SessionSetupReq r) =>
                    Step(MessageSet.Iso20CommonMessages, SessionSetup(r), Phase20.AuthorizationSetup),

                (Phase20.AuthorizationSetup, MessageSet.Iso20CommonMessages, AuthorizationSetupReq r) =>
                    Step(MessageSet.Iso20CommonMessages, AuthSetup(r), Phase20.Authorization),

                // Contract provisioning: an EV that needs a contract cert sends a CertificateInstallationReq
                // *instead of* its first AuthorizationReq (we announced CertificateInstallationService=true);
                // answer it and stay in Authorization — the AuthorizationReq follows.
                (Phase20.Authorization, MessageSet.Iso20CommonMessages, CertificateInstallationReq r) =>
                    Step(MessageSet.Iso20CommonMessages, CertInstallation(r), Phase20.Authorization),

                (Phase20.Authorization, MessageSet.Iso20CommonMessages, AuthorizationReq r) =>
                    Step(MessageSet.Iso20CommonMessages, Auth(r), Phase20.ServiceDiscovery),

                (Phase20.ServiceDiscovery, MessageSet.Iso20CommonMessages, ServiceDiscoveryReq r) =>
                    Step(MessageSet.Iso20CommonMessages, SvcDiscovery(r), Phase20.ServiceDetail),

                (Phase20.ServiceDetail, MessageSet.Iso20CommonMessages, ServiceDetailReq r) =>
                    Step(MessageSet.Iso20CommonMessages, SvcDetail(r), Phase20.ServiceSelection),

                (Phase20.ServiceSelection, MessageSet.Iso20CommonMessages, ServiceSelectionReq r) =>
                    Step(MessageSet.Iso20CommonMessages, SvcSelection(r), Phase20.ChargeParams),

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

                _ => throw new SessionAborted(
                    $"SECC sequence guard: {request.GetType().Name} not allowed in phase {Phase} " +
                    "(would be ResponseCode.FAILED_SequenceError)"),
            };

            Phase = next;
            return (respSet, response);
        }

        /// <summary>Reads/handles/replies over <paramref name="stream"/> until the session reaches <see cref="Phase20.Done"/>.</summary>
        public async Task RunAsync(Stream stream, CancellationToken ct = default)
        {
            // 8 KiB: a CertificateInstallationRes (contract chain + encrypted key + CPS chain + signature)
            // outgrows the 1 KiB the plain charge messages need.
            var buf = new byte[8192];
            while (!IsDone)
            {
                var (set, message) = await ReadFrame20Async(stream, ct).ConfigureAwait(false);
                var (replySet, reply) = Handle(set, message);

                int n = EncodeAny(replySet, reply, buf);
                await V2GTPStream.WriteFrameAsync(stream, replySet, buf.AsMemory(0, n), ct).ConfigureAwait(false);
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

            if (payloadType == V2GTP.PayloadType_DinIso2Main)   // 0x8001 — see doc comment
                return (MessageSet.Iso20CommonMessages,
                        CommonMessagesCodec.DecodeAny(frame.AsSpan(V2GTP.HeaderSize), out _));

            if (!V2GTPDispatcher.TryDecode(frame, out var set, out var message, out var error))
                throw new InvalidDataException($"V2GTP frame: {error}");
            return (set, message!);
        }

        /// <summary>Encodes a reply of any of the three message sets this project drives — implemented per concrete subclass since only it knows which DC/AC types it produces.</summary>
        protected abstract int EncodeAny(MessageSet set, object message, byte[] dest);

        private static (MessageSet, object, Phase20) Step(MessageSet set, object response, Phase20 next) => (set, response, next);
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
        private SessionSetupRes SessionSetup(SessionSetupReq req)
        {
            // Resume: a SessionSetupReq whose header carries a paused predecessor's session id rejoins that
            // session (ISO 15118-20 §8.4 — same OldSessionJoined mechanic as -2); anything else starts fresh.
            if (ResumeSessionId is not null && req.Header.SessionID.AsSpan().SequenceEqual(ResumeSessionId))
            {
                SessionCtx.SessionId = ResumeSessionId;
                return new SessionSetupRes(SessionCtx.ToCommonHeader(), ResponseCode.OK_OldSessionJoined, "DE*ABC*E1");
            }

            SessionCtx.SessionId = FixedSessionId ?? System.Security.Cryptography.RandomNumberGenerator.GetBytes(8);
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
        /// (<c>docs/interop-runs/2026-08-01-edf-iso20-dc-dynamic-reverse/</c>). Offering less is how a
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

        private AuthorizationRes Auth(AuthorizationReq req)
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
                return new PnCAuthResult(challengeOk, DigestOk: false, SignatureOk: false, "?", "fragment-encode-failed", "none");
            var fragment = buf.AsSpan(0, n);

            if (req.Header.Signature is not { } sig || sig.SignedInfo.Reference.Count == 0)
                return new PnCAuthResult(challengeOk, false, false, "?", "no-signature", "none");

            var reference = sig.SignedInfo.Reference[0];
            bool digestOk = HashOf(reference.DigestMethod.Algorithm, fragment).AsSpan().SequenceEqual(reference.DigestValue);

            string subject = "?";
            bool signatureOk = false;
            string grammar = "none";
            try
            {
                using var contract = X509CertificateLoader.LoadCertificate(pnc.ContractCertificateChain.Certificate);
                subject = contract.Subject;
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
                                     sig.SignedInfo.SignatureMethod.Algorithm, grammar);
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

        private ServiceDiscoveryRes SvcDiscovery(ServiceDiscoveryReq req) =>
            new(SessionCtx.ToCommonHeader(), ResponseCode.OK, ServiceRenegotiationSupported: true,
                new ServiceListType(EnergyServiceIds.Select(id => new ServiceType(id, FreeService: true)).ToArray()), VASList: null);

        private ServiceDetailRes SvcDetail(ServiceDetailReq req)
        {
            // The standard -20 energy-transfer parameter sets. A live Josev EVCC requires at least the
            // ControlMode parameter ("Control mode parameter missing" otherwise). We offer both control
            // modes — set 1: Scheduled (ControlMode=1), set 2: Dynamic (ControlMode=2) — ordered by
            // PreferDynamicControlMode, since a Josev EVCC adopts the *first* offered set's ControlMode.
            // MobilityNeedsMode=1 (mobility needs provided by the EVCC) is legal for both modes
            // ([V2G20-2663] only restricts MobilityNeedsMode=2 to Dynamic).
            ParameterSetType ParamSet(ushort id, int controlMode) => new(id, new[]
            {
                new ParameterType("Connector", null, null, null, IntValue: ConnectorParameter, null, null),
                new ParameterType("ControlMode", null, null, null, IntValue: controlMode, null, null),
                new ParameterType("MobilityNeedsMode", null, null, null, IntValue: 1, null, null),
                new ParameterType("Pricing", null, null, null, IntValue: 0, null, null),
            });
            var scheduled = ParamSet(1, controlMode: 1);
            var dynamic   = ParamSet(2, controlMode: 2);
            return new(SessionCtx.ToCommonHeader(), ResponseCode.OK, req.ServiceID,
                new ServiceParameterListType(PreferDynamicControlMode
                    ? new[] { dynamic, scheduled }
                    : new[] { scheduled, dynamic }));
        }

        private ServiceSelectionRes SvcSelection(ServiceSelectionReq req) =>
            new(SessionCtx.ToCommonHeader(), ResponseCode.OK);

        private ScheduleExchangeRes ScheduleExchange(ScheduleExchangeReq req)
        {
            // Answer in kind ([V2G20-1600]): a Dynamic-mode EV sends Dynamic_SEReqControlMode and must get a
            // Dynamic res (all fields optional — Processing=Finished is the actual signal); a Scheduled-mode
            // EV gets the schedule-tuple offer below.
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
            ECDsa? oemVerifyKey = null;
            try
            {
                using var oemLeaf = X509CertificateLoader.LoadCertificate(chain.Certificate);
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

            CertInstall = new CertInstallResult(digestOk, signatureOk, grammar, oemSubject, encryptedForOem);

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
