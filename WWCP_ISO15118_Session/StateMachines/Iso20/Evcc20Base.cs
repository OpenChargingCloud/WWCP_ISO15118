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

using System.Linq;
using System.Security.Cryptography.X509Certificates;

using cloud.charging.open.protocols.ISO15118_20.CommonMessages;
using cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated;
using cloud.charging.open.protocols.ISO15118.Framing;
using cloud.charging.open.protocols.ISO15118.Session;
using cloud.charging.open.protocols.ISO15118.Simulation;
using cloud.charging.open.protocols.ISO15118.Timing;
using cloud.charging.open.protocols.ISO15118.EXI.Dispatch;

// Each -20 message set carries its own generated ResponseCode and V2GResponseType; RefuseOnFailure
// has to see all three.
using Ac20 = cloud.charging.open.protocols.ISO15118_20.AC.Generated;
using Dc20 = cloud.charging.open.protocols.ISO15118_20.DC.Generated;


namespace cloud.charging.open.protocols.ISO15118.StateMachines.Iso20
{
    /// <summary>The EV's verdict over a signed -20 <c>AbsolutePriceSchedule</c> in ScheduleExchangeRes:
    /// whether the header carried a signature, the reference digest matched the schedule's re-encoded EXI
    /// fragment, and the ECDSA-P521/SHA-512 signature verified.</summary>
    public sealed record Iso20TariffResult(bool SignaturePresent, bool DigestOk, bool SignatureOk);

    /// <summary>
    /// The EVCC side of an ISO 15118-20 session, shared between AC and DC: drives the CommonMessages
    /// phases directly (EIM by default; Plug &amp; Charge with a signed AuthorizationReq when
    /// <see cref="Pnc"/> is set and the SECC offers it), and calls the <c>protected abstract</c> hooks below
    /// for the diverging middle — implemented by <see cref="Evcc20Dc"/>/<see cref="Evcc20Ac"/>, which know
    /// which DC/AC codec and concrete request/response types their energy-transfer mode actually uses.
    /// </summary>
    public abstract class Evcc20Base(
        Stream stream, TimeProvider clock, IAsyncDelay pollDelay, TimeSpan perMessageTimeout)
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(50);

        protected readonly SessionContext SessionCtx = new(clock);
        protected IAsyncDelay PollDelay => pollDelay;
        // 8 KiB: a signed PnC AuthorizationReq carries a 3-cert contract chain (~2 KiB) — 1 KiB is too small.
        private readonly byte[] _buf = new byte[8192];

        public int Exchanges { get; private set; }
        public long BytesOnWire { get; private set; }

        /// <summary>The energy-transfer service id actually negotiated in ServiceDiscovery/ServiceSelection
        /// (Table 204: AC=1, DC=2, AC_BPT=5, DC_BPT=6, MCS=8, MCS_BPT=9); 0 before that phase. Exposed
        /// because which service a session settled on is otherwise invisible from the outside — it is what
        /// distinguishes an MCS session from a DC one, the two being identical on the wire otherwise.</summary>
        public ushort SelectedEnergyServiceId { get; private set; }

        /// <summary>
        /// Whether this session negotiated a <b>bidirectional</b> service, and so whether the DC/AC hooks
        /// below must build the <c>BPT_*</c> charge-parameter and control-mode types instead of the
        /// charge-only ones.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Asking in kind</b>, the same rule <see cref="RunChargeLoopIterationAsync"/> already applies to
        /// Scheduled vs. Dynamic, read for the other axis: -20 carries the direction in the polymorphic type,
        /// so the service the session selected decides which type every subsequent message may use. It is a
        /// property of the session, not a setting — hence a derived value off
        /// <see cref="SelectedEnergyServiceId"/> and not a knob, and hence no way to select MCS_BPT and then
        /// charge one way under it.
        /// </para>
        /// <para>
        /// It reads <see cref="SelectedEnergyServiceId"/>, so it is only meaningful from
        /// <see cref="RunChargeParameterDiscoveryAsync"/> onwards — service selection is two exchanges
        /// earlier in <see cref="RunAsync"/>, and every hook that consults it runs after.
        /// </para>
        /// <para>
        /// <b>Found live.</b> Until this existed no <c>Evcc20*</c> built a <c>BPT_*</c> request at all: the
        /// bidirectional work had been done from the station side, where the direction is driven by what the
        /// EV sends, and what our EV sent was always charge-only. So our EVCC could select MCS_BPT from
        /// everest-core 2026.02.1's catalogue and was then refused <c>FAILED_WrongChargeParameter</c> at
        /// <c>DC_ChargeParameterDiscoveryRes</c> — correctly
        /// (<c>ISO15118ConformanceTests/docs/interop-runs/2026-08-05-everest-mcs-bpt/</c>).
        /// </para>
        /// </remarks>
        protected bool BidirectionalService
            => EnergyTransferService.IsBidirectional(SelectedEnergyServiceId);

        /// <summary>Contract credentials enabling Plug &amp; Charge; <c>null</c> (default) authorizes via EIM.</summary>
        public PncEvccOptions? Pnc { get; set; }

        /// <summary>
        /// The vehicle's own energy counter — what this EV thinks it took, kept independently of what
        /// the station reports (<c>EVSimulatorApp/docs/CONCEPT.md</c> §4.2/§4.3).
        /// </summary>
        /// <remarks>
        /// On the base rather than per set: the counter is the vehicle's, and AC and DC differ only in
        /// what a sample is worth. Each subclass takes its own sample in
        /// <see cref="RunChargeLoopIterationAsync"/>, where it knows which field carries the EV's view.
        /// </remarks>
        public Metering.EvMeter Meter { get; init; } = new();

        /// <summary>How this session actually authorized: <c>"eim"</c>, or <c>"pnc-signed"</c> when a signed
        /// PnC AuthorizationReq was sent (requires <see cref="Pnc"/> set and the SECC offering PnC).</summary>
        public string AuthorizationMode { get; private set; } = "eim";

        /// <summary>OEM-provisioning credentials; when set (and the SECC offers the service), the EVCC runs a
        /// contract-provisioning exchange before authorization. <c>null</c> (default) skips it.</summary>
        public CertInstallEvccOptions? CertInstallRequest { get; set; }

        /// <summary>The contract certificate (DER) installed via CertificateInstallation, once recovered —
        /// with <see cref="InstalledContractKey"/> proving the ECDH/AES-GCM key unwrap round-tripped.</summary>
        public byte[]? InstalledContractCertificate { get; private set; }

        /// <summary>The unwrapped contract private key (P-521); the caller owns disposal.</summary>
        public System.Security.Cryptography.ECDsa? InstalledContractKey { get; private set; }

        /// <summary>Whether the CertificateInstallationRes header signature (CPS leaf over the
        /// SignedInstallationData fragment) verified.</summary>
        public bool InstalledContractSignatureOk { get; private set; }

        /// <summary>How to end the session: <c>Terminate</c> (default) or <c>Pause</c> — after a pause the
        /// caller reconnects and resumes via <see cref="ResumeSessionId"/>.</summary>
        public ChargingSession StopMode { get; set; } = ChargingSession.Terminate;

        /// <summary>A paused predecessor's session id: the opening SessionSetupReq carries it so the SECC
        /// rejoins the old session instead of assigning a new one.</summary>
        public byte[]? ResumeSessionId { get; set; }

        /// <summary>
        /// The paused session's binding to the <em>station</em> — the car's half of the same mechanism the
        /// SECC applies to the car, and its obligation rather than a courtesy: an EV that resumes has to
        /// establish it is still talking to the SECC it paused with.
        /// </summary>
        /// <remarks>
        /// Asymmetric with <c>Secc20Base.ResumeBinding</c> on purpose. Where the station cannot verify a
        /// resume it must refuse it, because failing open there hands one EV's authorization to another.
        /// Here failing open only risks the car continuing at a station it cannot confirm, a risk it bears
        /// itself — so an unverifiable resume proceeds and is recorded in
        /// <see cref="ResumedStationVerified"/>, while an actual <em>mismatch</em> terminates the session.
        /// The distinction matters to this harness, which deliberately speaks plain TCP to peers that offer
        /// nothing else, and where no binding can exist on either side.
        /// </remarks>
        public byte[]? ResumeBinding { get; set; }

        /// <summary>The energy-transfer service the paused session settled on; a resumed session does not
        /// repeat service negotiation and would otherwise forget it.</summary>
        public ushort ResumeEnergyServiceId { get; set; }

        /// <summary>The binding of the session in effect — keep it with <see cref="SessionId"/> for a resume.</summary>
        public byte[]? SessionBinding { get; private set; }

        /// <summary>The station's TLS leaf certificate (DER); resolved from the stream when it is an
        /// authenticated <c>SslStream</c>, settable for callers that drive the machine over something else.</summary>
        public byte[]? SeccLeafCertificate { get; set; }

        /// <summary>
        /// Whether a resumed session was confirmed to be with the same station: <c>true</c> on a match,
        /// <c>null</c> when it could not be checked (no binding on either side), and never <c>false</c> —
        /// a mismatch ends the session rather than reporting one.
        /// </summary>
        public bool? ResumedStationVerified { get; private set; }

        /// <summary>
        /// Set when this EVCC asked to resume and the station answered with a <em>new</em> session instead.
        /// Everything the paused session carried, authorization included, has been dropped and the opening
        /// sequence run from scratch — which is what the standard requires and not an error to raise.
        /// </summary>
        public bool ResumeRefused { get; private set; }

        /// <summary>The SECC's SessionSetup verdict: <c>OK_NewSessionEstablished</c>, or on a successful
        /// resume <c>OK_OldSessionJoined</c>.</summary>
        public ResponseCode SessionSetupCode { get; private set; }

        /// <summary>
        /// The SessionID this car puts in every request <b>after</b> SessionSetup, instead of the one the
        /// station issued. Null (the default) is what a conformant car does and what every recorded
        /// session contains. The `-20` twin of <c>Evcc2.SendSessionId</c>.
        /// </summary>
        /// <remarks>
        /// `[V2G20-460]` — any request except <c>SessionSetupReq</c> whose SessionID is not the stored one
        /// shall be answered <c>FAILED_UnknownSession</c> — is unreachable from a session unless a car can
        /// send a wrong one, which is why nothing here had ever exercised it. Eight zero bytes are the
        /// interesting value: that is what ISO reserves for *"I have no session"*, and what a station's
        /// decoder is likeliest to special-case.
        /// </remarks>
        public byte[]? SendSessionId { get; set; }

        /// <summary>
        /// The <c>SupportedServiceIDs</c> filter this car puts in <c>ServiceDiscoveryReq</c>. Null (the
        /// default) omits the element, which asks the station to list <b>everything</b> — the ordinary
        /// behaviour, and what every recorded session contains.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The element is optional and naming ids in it is exactly what it is for, so both settings are
        /// conformant; this is a filter, not a deviation. Added 2026-08-15 for a station that cannot be
        /// asked the ordinary question: eVDriveFlow's <c>process_service_discovery_request.py</c>
        /// dereferences the optional element unconditionally and dies on <c>None</c>, so <b>every</b>
        /// session this project ever drove against their SECC ended at the fifth message — see
        /// <c>docs/reports/evdriveflow-service-discovery-filter.md</c>, which is filed about exactly that.
        /// </para>
        /// <para>
        /// It is worth having beyond that one peer: a filter is the only way to ask a station what it
        /// offers <i>within</i> a set, and nothing here could send one.
        /// </para>
        /// </remarks>
        public IReadOnlyList<ushort>? SupportedServiceIds { get; set; }

        /// <summary>
        /// <c>V2G_EVCC_Msg_Timeout</c> for a charge-loop request — how long this car waits for the
        /// station's answer once the contactor is closed. Tables 216, 217 and 218 all put it at
        /// <b>0,5 s</b> for <c>{AC,DC,WPT}_ChargeLoopReq</c>, against Table 215's 2 s for ordinary
        /// messages (<c>[V2G20-1499]</c>, <c>[V2G20-1501]</c>, <c>[V2G20-5069]</c>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// The car-side twin of <see cref="Secc20Base.ChargeLoopSequenceTimeout"/>, added the same day and
        /// for the same reason: this class applied the single <c>perMessageTimeout</c> to every exchange,
        /// so the one message pair the standard tightens was as slack as session setup.
        /// </para>
        /// <para>
        /// Init-only so a test can put the flat behaviour back and watch the tight one fail. It carries
        /// the same residual risk the station side already took on: a loopback under load has been
        /// measured at 2 140–3 564 ms for its heaviest single exchange
        /// (<c>LoopbackTimeouts</c>), which is why the *baseline* there is 10 s rather than 2 s. Charge-loop
        /// exchanges are late and cheap, and the SECC has enforced its own 0,5 s across the whole suite
        /// since 2026-08-11 without flaking — but if this one ever does, that is where the answer is.
        /// </para>
        /// </remarks>
        public TimeSpan ChargeLoopMsgTimeout { get; init; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// <c>V2G_EVCC_Msg_Timeout</c> for the two messages Table 215 gives <b>5 s</b> rather than 2 —
        /// <c>CertificateInstallationReq</c> and <c>ServiceDetailReq</c>, both of which make the station do
        /// real work (a contract to mint, a catalogue to assemble) before it can answer.
        /// </summary>
        /// <remarks>Cutting these off at the ordinary 2 s would abort sessions the standard expects to
        /// complete, so the deviation runs the other way from <see cref="ChargeLoopMsgTimeout"/>: it is
        /// there to stop us being wrong about a slow peer, not to catch one.</remarks>
        public TimeSpan SlowMsgTimeout { get; init; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Stop sending after the first charge-loop iteration and wait, with the connection <b>open</b>,
        /// for the station to end the session on its own — up to this budget. Null (the default) charges
        /// normally. <see cref="SilenceEndedAfter"/> carries the answer.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A car that hangs up is an EOF and tells you nothing about a timer; a car that goes quiet while
        /// holding the socket is what <c>V2G_SECC_Sequence_Timeout</c> is defined against. Nothing in this
        /// suite could do the second until 2026-08-11, so no run had ever measured any station's sequence
        /// timeout — the same shape as the SessionID override added the day before, and as
        /// <see cref="RequestMeterInfo"/> before that.
        /// </para>
        /// <para>
        /// The charge loop is the interesting place to do it: `[V2G20-1500]` and `[V2G20-1502]` give the
        /// SECC <b>0,5 s</b> there (Tables 216 and 217) against the 60 s of Table 215 everywhere else, and
        /// it is the phase in which the contactor is closed.
        /// </para>
        /// <para>
        /// This doc block sat on <see cref="SendSessionId"/> until 2026-08-11 — two <c>&lt;summary&gt;</c>
        /// tags in a row, so the compiler took the second and this one documented nothing. Moved when the
        /// station-side mirror (<see cref="Secc20Base.GoSilentInChargeLoop"/>) went in beside it.
        /// </para>
        /// </remarks>
        public TimeSpan? GoSilentInChargeLoop { get; set; }

        /// <summary>How long the station kept the session after this car stopped sending, or null when it
        /// was still open at the end of <see cref="GoSilentInChargeLoop"/>. Measured from the moment our
        /// last charge-loop response was read.</summary>
        public TimeSpan? SilenceEndedAfter { get; private set; }

        /// <summary>What to carry into the next connection after ending with <c>ChargingSession.Pause</c>.</summary>
        public ResumableSession PausedSession

            => new (SessionId, SessionBinding, SelectedEnergyServiceId);

        /// <summary>Resume a paused predecessor — all three values at once; see <see cref="ResumeBinding"/>.</summary>
        public void ResumeFrom(ResumableSession? paused)
        {
            ResumeSessionId       = paused?.SessionId;
            ResumeBinding         = paused?.Binding;
            ResumeEnergyServiceId = paused?.EnergyServiceId ?? 0;
        }

        /// <summary>The session id in effect — keep it for a resume after a paused session.</summary>
        public byte[] SessionId => SessionCtx.SessionId;

        /// <summary>
        /// How long a phase may keep answering <c>EVSEProcessing = Ongoing</c> before the session ends.
        /// </summary>
        /// <remarks>60 s, ISO 15118's EVCC ongoing timeout. See <see cref="OngoingGuard"/> for the live
        /// run that made this necessary.</remarks>
        public TimeSpan OngoingTimeout { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Drive the session in <b>Dynamic</b> control mode (ControlMode = 2) instead of Scheduled.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The mirror of <c>Secc20Base.PreferDynamicControlMode</c>, and it arrived much later: until
        /// 2026-08-03 our station could answer a Dynamic EV but our car could not be one. Every recorded
        /// Dynamic run had Josev's EVCC on the other side
        /// (<c>ISO15118ConformanceTests/docs/interop-runs/2026-07-22-iso20-dynamic-sdp/</c>), so the mode was validated in exactly one
        /// direction and the roadmap's "Scheduled and Dynamic" quietly meant "Scheduled both ways, Dynamic
        /// inbound".
        /// </para>
        /// <para>
        /// It touches four places, because the mode is a property of the whole session and not of one
        /// message: the parameter set selected out of <c>ServiceDetailRes</c> (ControlMode = 2),
        /// <c>ScheduleExchangeReq</c>'s control-mode arm, the <c>EVPowerProfile</c> in
        /// <c>PowerDelivery(Start)</c>, and the charge loop's request arm. Answering in kind is
        /// [V2G20-1600]; asking in kind is the same rule read from the other end.
        /// </para>
        /// <para>
        /// The substantive difference is who plans: in Scheduled mode the EV picks a schedule tuple the SECC
        /// offered and commits to it, in Dynamic mode it states energy needs and a departure time and lets
        /// the station steer. Hence the mandatory energy triple below and the empty
        /// <c>Dynamic_EVPPTControlMode</c> — there is no tuple to point at.
        /// </para>
        /// </remarks>
        public Boolean PreferDynamicControlMode { get; set; }

        /// <summary>
        /// Ask for the <b>bidirectional</b> entry of whatever catalogue this vehicle wants — AC_BPT (5)
        /// ahead of AC (1), DC_BPT (6) ahead of DC (2), MCS_BPT (9) ahead of MCS (8).
        /// </summary>
        /// <remarks>
        /// <para>
        /// A car that can give energy back has a preference between the two entries a bidirectional station
        /// advertises, and nothing here could state it: <see cref="PreferredEnergyServiceIds"/> lists the
        /// unidirectional service first, so against a station offering both, the BPT entry was never
        /// selected. It sits beside <see cref="PreferDynamicControlMode"/> rather than in a subclass because
        /// it is the same kind of thing — a choice the vehicle makes among what the station offers — and
        /// because it has to hold for all three catalogues at once.
        /// </para>
        /// <para>
        /// <b>It replaces a probe that could only reach one of them.</b> The conformance harness carried an
        /// <c>McsBptFirstEvcc</c> deriving from <see cref="Evcc20Mcs"/> with the list written out reversed.
        /// That got to MCS_BPT and no further: the AC and DC rankings live on this class, and
        /// <see cref="Evcc20Ac"/> is sealed, so the same trick did not generalise. One flag does, without a
        /// subclass per catalogue and without unsealing anything.
        /// </para>
        /// <para>
        /// <b>Selecting a BPT service is itself what makes the session bidirectional</b>, not a second
        /// switch: <see cref="BidirectionalService"/> derives the direction from the selected id, and
        /// <c>Evcc20Ac</c> / <c>Evcc20Dc</c> then build the <c>BPT_*</c> request types. A station enforces
        /// that coupling, and EVerest's taught it to us the hard way — a plain
        /// <c>DC_CPDReqEnergyTransferModeType</c> sent under service 9 was refused with
        /// <c>FAILED_WrongChargeParameter</c> (<c>ISO15118ConformanceTests/docs/interop-runs/2026-08-05-everest-mcs-bpt/</c>).
        /// </para>
        /// <para>
        /// A station advertising no bidirectional service is unaffected. The reorder is stable and the
        /// selection still walks our ranking asking what the station offers, so a catalogue without a BPT
        /// entry lands on the same service it did before.
        /// </para>
        /// </remarks>
        public Boolean PreferBidirectionalService { get; set; }

        /// <summary>When the car leaves, as a -20 <c>DepartureTime</c> (seconds from the session's time
        /// anchor). Dynamic mode only: it is the deadline the station schedules against.</summary>
        public UInt32 DepartureTime { get; set; } = 3600;

        /// <summary>
        /// Set <c>MeterInfoRequested</c> in every charge-loop request, which is `[V2G20-1081]` — the one
        /// mechanism the standard gives an EV for asking to be told the meter reading. `[V2G20-1082]` is
        /// the station's half: having been asked, it <em>shall</em> answer with the <c>MeterInfo</c>
        /// element.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Opt-in, and the default keeps the field <c>false</c> exactly as every recorded session and
        /// every vector in <c>ISO15118ConformanceTests.Simulation/Vectors/</c> has it, so nothing needed
        /// regenerating. The same shape as <see cref="Battery"/> and <c>TransportSecurity.Unknown</c>:
        /// new behaviour that has to be asked for.
        /// </para>
        /// <para>
        /// It was hardcoded <c>false</c> in both charge loops until 2026-08-10, which meant this EVCC
        /// could not exercise `[V2G20-1081]` at all — and therefore could not tell whether any station
        /// honours `[V2G20-1082]`. It could not: the first station asked did not answer
        /// (<c>ISO15118ConformanceTests/docs/reports/everest-d20-meter-info.md</c>).
        /// </para>
        /// </remarks>
        public Boolean RequestMeterInfo { get; set; }

        /// <summary>How many charge-loop responses carried a <c>MeterInfo</c> element. Counted whether or
        /// not <see cref="RequestMeterInfo"/> is set, because `[V2G20-1833]` asks a metering station for
        /// an initial reading without being asked.</summary>
        public Int32 MeterInfoResponses { get; private set; }

        /// <summary>Record that a charge-loop response did or did not carry <c>MeterInfo</c>. Called by
        /// the AC and DC loops, which see different generated types for the same element.</summary>
        protected void NoteMeterInfo(Boolean present)
        {
            if (present)
                MeterInfoResponses++;
        }

        /// <summary>
        /// A battery that fills up, and the goal that ends the charge loop. Null — the default — keeps the
        /// fixed three iterations every recorded interop run was taken with.
        /// </summary>
        /// <remarks>
        /// Opt-in on purpose. A goal-driven loop is the honest simulation and it is also hundreds of
        /// exchanges rather than three, which would change the length of every session in
        /// <c>docs/interop-runs/</c> and every counterparty's view of us. Setting this is saying that a
        /// charging session, not a message sequence, is what the run is about.
        /// </remarks>
        public EvBattery? Battery { get; set; }

        /// <summary>Why the charge loop ended; null while it has not run.</summary>
        public ChargeStop? BatteryStop { get; private set; }

        /// <summary>
        /// An amount as a -20 rational's (value, exponent) pair, scaling down by powers of ten only when
        /// needed to fit the 16-bit value: 9 000 stays 9 000×10⁰, 60 000 becomes 6 000×10¹, 3 750 000
        /// becomes 3 750×10³, and beyond 32 767×10³ it saturates. The rational carries no unit, so this
        /// serves watts and watt-hours alike.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The AC, DC and common message sets each declare their own <c>RationalNumberType</c> as a
        /// distinct CLR type, so only the arithmetic can be shared and each side wraps it. Shared rather
        /// than copied because the saturation below was a silent <c>short</c> overflow until review caught
        /// it, and three copies is three chances to lose it again.
        /// </para>
        /// <para>
        /// <b>It does not normalise.</b> This said "keeping three significant figures — 9 kW becomes 9×10³"
        /// from the day the DC version was written, and that is not what the loop does: anything a
        /// <c>short</c> holds is sent at exponent 0, so 9 kW goes out as 9 000×10⁰. Both encodings are
        /// schema-valid and decode to the same number, so nothing on the wire was ever wrong — but in a
        /// project where the encoding is the thing under test, a comment naming the wrong one is what
        /// misleads the next person reading a byte diff.
        /// </para>
        /// </remarks>
        protected static (short Value, sbyte Exponent) ScaledRational(double amount)
        {
            sbyte exponent = 0;
            var   value    = Math.Round(amount);
            while (Math.Abs(value) > short.MaxValue && exponent < 3)
            {
                value /= 10;
                exponent++;
            }

            if (Math.Abs(value) > short.MaxValue)
                value = Math.Sign(value) * short.MaxValue;

            return ((short) Math.Round(value), exponent);
        }

        /// <summary>
        /// The three energy figures a -20 request carries, in watt-hours — how much the car wants, how
        /// much it can still take, how much it needs — or null when there is no pack to derive them from,
        /// in which case every call site keeps the literal it always sent.
        /// </summary>
        /// <remarks>
        /// Mandatory in the Dynamic arms and optional in the Scheduled ones, which is the schema saying
        /// the obvious: a station asked to choose the operating point cannot do it without knowing the
        /// target. Until this existed they were 30 / 60 / 10 kWh whatever the car was actually carrying,
        /// so a Dynamic station scheduled against three constants.
        /// </remarks>
        protected (double Target, double Maximum, double Minimum)? EnergyRequestWh
            => Battery is { } b ? (b.EnergyNeededWh, b.EnergyAcceptableWh, b.MinimumNeededWh) : null;

        /// <summary>The state of charge the car is heading for, as -20's signed-byte percentage, or null
        /// when no target was named.</summary>
        protected sbyte? DeclaredTargetSoCPercent
            => Battery?.TargetSoC is { } t ? (sbyte) Math.Clamp(Math.Round(t), 0, 100) : null;

        /// <summary>And what the driver needs to have by departure. Null when none was asked for.</summary>
        protected sbyte? DeclaredMinimumSoCPercent
            => Battery?.MinimumSoC is { } m ? (sbyte) Math.Clamp(Math.Round(m), 0, 100) : null;

        /// <summary>The tariff signer's public key (fachlich the eMSP's). When set and the
        /// ScheduleExchangeRes carries a signed AbsolutePriceSchedule, the EV verifies it.</summary>
        public System.Security.Cryptography.ECDsa? TariffVerifyKey { get; set; }

        /// <summary>The price-schedule verdict; null while no signed AbsolutePriceSchedule was seen.</summary>
        public Iso20TariffResult? Tariff { get; private set; }

        /// <summary>Runs charge-parameter discovery exactly once (no polling — -20's DC/AC CPD response carries no EVSEProcessing field).</summary>
        protected abstract Task RunChargeParameterDiscoveryAsync(CancellationToken ct);
        /// <summary>DC: CableCheck+PreCharge. AC: no-op.</summary>
        protected abstract Task RunPreChargeSequenceAsync(CancellationToken ct);
        /// <summary>One charge-loop request/response (caller loops this a fixed number of times).</summary>
        protected abstract Task RunChargeLoopIterationAsync(CancellationToken ct);
        /// <summary>DC: WeldingDetection. AC: no-op.</summary>
        protected abstract Task RunPostChargeSequenceAsync(CancellationToken ct);

        /// <summary>The energy-transfer mode this EVCC drives — used to pick the matching service from the
        /// SECC's advertised catalog during service discovery.</summary>
        protected abstract PowerMode EnergyMode { get; }

        /// <summary>
        /// Picks up a resumed session: confirm the station, restore what the pause carried, and send nothing.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The next message a resumed `-20` session may send is the charge-parameter one. Authorization is
        /// not repeated — the paused session's remains valid for the whole service session — and neither is
        /// service negotiation, because the standard names exactly one permitted next message and none of
        /// ServiceDiscovery/Detail/Selection is it.
        /// </para>
        /// <para>
        /// <b>This is the fix for a real defect.</b> Until 2026-08-08 this EVCC replayed its entire opening
        /// sequence regardless of the response code it had just been given, so against a station that
        /// implements the rule it sent <c>AuthorizationSetupReq</c> into a session already past that point
        /// and was answered <c>FAILED_SequenceError</c> — meaning our car could not resume against EVerest,
        /// or against anything else conformant. `-2` requires the opposite, replaying the values rather than
        /// skipping them, and this code was written from `-2`.
        /// </para>
        /// </remarks>
        private void JoinOldSession()
        {

            // The car's own obligation, mirroring what the station does to it: a resume has to be with the
            // same SECC. A mismatch is not a warning -- the paused session is purged and terminated, because
            // if this is a different station then something is wrong that continuing cannot improve.
            var presented = SessionBinding20.Compute(SessionCtx.SessionId, SeccLeafCertificate);

            if (ResumeBinding is { Length: > 0 } && presented is { Length: > 0 })
            {
                if (!SessionBinding20.Matches(ResumeBinding, presented))
                {
                    ResumeSessionId       = null;
                    ResumeEnergyServiceId = 0;
                    throw new SessionAborted(
                        "Resumed session is with a different SECC than the one that paused it — " +
                        "session purged and terminated.");
                }
                ResumedStationVerified = true;
            }
            // else: nothing to check against on one side or the other. See ResumeBinding for why the car
            // proceeds where the station would refuse.

            SessionBinding          = ResumeBinding;
            SelectedEnergyServiceId = ResumeEnergyServiceId;

        }

        /// <summary>
        /// Opens a fresh session: authorization setup, authorization, then the service negotiation that
        /// settles which energy-transfer service and parameter set this session runs.
        /// </summary>
        /// <remarks>
        /// Reached either because this is a new session, or because a resume was refused — in which case
        /// everything the paused session carried, authorization included, is dropped first and the sequence
        /// below runs from scratch, which is exactly what the standard prescribes.
        /// </remarks>
        private async Task OpenNewSessionAsync(CancellationToken ct)
        {

            if (ResumeSessionId is not null)
            {
                ResumeRefused          = true;
                ResumeSessionId        = null;
                ResumeBinding          = null;
                ResumeEnergyServiceId  = 0;
                ResumedStationVerified = null;
            }

            var authSetup = await Exchange<AuthorizationSetupRes>(MessageSet.Iso20CommonMessages,
                dest => new AuthorizationSetupReq(SessionCtx.ToCommonHeader()).TryEncode(dest, out int n) ? n : throw EncodeFailed(), ct);

            if (CertInstallRequest is { } oem && authSetup.CertificateInstallationService)
                await RunCertificateInstallationAsync(oem, ct);

            var encodeAuthReq = BuildAuthorizationReqEncoder(authSetup);
            var authGuard = new OngoingGuard(clock, OngoingTimeout, "Authorization");
            while ((await Exchange<AuthorizationRes>(MessageSet.Iso20CommonMessages, encodeAuthReq, ct))
                   .EVSEProcessing != Processing.Finished)
            {
                authGuard.Tick();
                await pollDelay.Wait(PollInterval, ct);
            }

            // Service negotiation is dynamic: select the energy-transfer service and parameter set the SECC
            // actually advertises, rather than assuming fixed ids. A live Josev interop run caught the old
            // hardcoded ServiceID=1/ParameterSetID=1 (Josev's DC catalog offers neither) — our loopback SECC
            // happened to advertise exactly those, which masked it.
            var discovery = await Exchange<ServiceDiscoveryRes>(MessageSet.Iso20CommonMessages,
                dest => new ServiceDiscoveryReq(SessionCtx.ToCommonHeader(),
                                                SupportedServiceIds is { Count: > 0 } ids
                                                    ? new ServiceIDListType(ids)
                                                    : null).TryEncode(dest, out int n) ? n : throw EncodeFailed(), ct);
            ushort serviceId = SelectEnergyTransferService(discovery);
            SelectedEnergyServiceId = serviceId;

            var detail = await Exchange<ServiceDetailRes>(MessageSet.Iso20CommonMessages,
                dest => new ServiceDetailReq(SessionCtx.ToCommonHeader(), serviceId).TryEncode(dest, out int n) ? n : throw EncodeFailed(), ct,
                SlowMsgTimeout);
            ushort parameterSetId = SelectParameterSet(detail);

            await Exchange<ServiceSelectionRes>(MessageSet.Iso20CommonMessages,
                dest => new ServiceSelectionReq(SessionCtx.ToCommonHeader(),
                    new SelectedServiceType(serviceId, parameterSetId), null).TryEncode(dest, out int n) ? n : throw EncodeFailed(), ct);

            SessionBinding = SessionBinding20.Compute(SessionCtx.SessionId, SeccLeafCertificate);

        }

        public async Task RunAsync(CancellationToken ct = default)
        {
            // The station's certificate comes from the handshake that already happened. `-20` permits only
            // full-handshake TLS, so in a conformant session this is present; over plain TCP it stays null
            // and a resume simply cannot be confirmed either way.
            SeccLeafCertificate ??= SessionBinding20.PeerLeafOf(stream);

            if (ResumeSessionId is not null)
                SessionCtx.SessionId = ResumeSessionId;   // rejoin: the SessionSetupReq header carries the paused id

            var setupRes = await Exchange<SessionSetupRes>(MessageSet.Iso20CommonMessages,
                dest => new SessionSetupReq(SessionCtx.ToCommonHeader(), "EVCC01").TryEncode(dest, out int n) ? n : throw EncodeFailed(), ct);
            SessionSetupCode = setupRes.ResponseCode;

            // Adopt the SECC-assigned SessionID: every subsequent request header must carry it, not the
            // all-zero id the EVCC opens SessionSetup with (ISO 15118-20 §7.9.2.4). A live Josev interop run
            // caught this — Josev's SECC strictly rejects a mismatched session id (our loopback SECC did not).
            SessionCtx.SessionId = setupRes.Header.SessionID;

            // …and only now, so SessionSetupReq itself keeps the id [V2G20-460] excludes from the rule.
            SessionCtx.SendSessionIdOverride = SendSessionId;

            if (SessionSetupCode == ResponseCode.OK_OldSessionJoined)
                JoinOldSession();
            else
                await OpenNewSessionAsync(ct);

            await RunChargeParameterDiscoveryAsync(ct);

            // MaximumSupportingPoints is schema-bounded to [12, 1024] (the encoder biases by 12); a smaller
            // value underflows on the wire. A live Josev run rejected the earlier 1 (our lenient SECC didn't).
            ScheduleExchangeRes scheduleRes;
            do
            {
                scheduleRes = await Exchange<ScheduleExchangeRes>(MessageSet.Iso20CommonMessages,
                    dest => new ScheduleExchangeReq(SessionCtx.ToCommonHeader(), MaximumSupportingPoints: 12,
                        Dynamic_SEReqControlMode:   PreferDynamicControlMode ? DynamicScheduleRequest() : null,
                        Scheduled_SEReqControlMode: PreferDynamicControlMode ? null : new Scheduled_SEReqControlModeType(null, null, null, null, null))
                        .TryEncode(dest, out int n) ? n : throw EncodeFailed(), ct);
                if (scheduleRes.EVSEProcessing != Processing.Finished)
                    await pollDelay.Wait(PollInterval, ct);
            }
            while (scheduleRes.EVSEProcessing != Processing.Finished);

            VerifyPriceSchedule(scheduleRes);

            await RunPreChargeSequenceAsync(ct);

            // PowerDelivery(Start) must carry an EVPowerProfile referencing a schedule tuple the SECC offered
            // (ISO 15118-20 §7.9.2.4): pick the first tuple from the ScheduleExchangeRes and echo a single
            // power-schedule entry. A live Josev run rejected the earlier absent profile (our SECC didn't).
            var evPowerProfile = BuildEvPowerProfile(scheduleRes);
            await Exchange<PowerDeliveryRes>(MessageSet.Iso20CommonMessages,
                dest => new PowerDeliveryReq(SessionCtx.ToCommonHeader(), Processing.Finished, ChargeProgress.Start, evPowerProfile, null)
                    .TryEncode(dest, out int n) ? n : throw EncodeFailed(), ct);

            // Without a battery this is a message sequence and three iterations are enough to exercise it.
            // With one it is a charging session, and it ends when the car is done rather than when a
            // counter runs out. The battery is fed from the meter's own increment, so it fills with what
            // the station actually delivered and not with what the car asked for.
            if (GoSilentInChargeLoop is { } budget)
            {
                // One iteration, so the station has answered inside the charge loop and armed the timer
                // this measures; then nothing at all, with the socket held open.
                await RunChargeLoopIterationAsync(ct);
                SilenceEndedAfter = await WaitForPeerToEndSessionAsync(budget, ct);
                return;
            }

            if (Battery is null)
                for (int cycle = 0; cycle < 3; cycle++)
                {
                    await RunChargeLoopIterationAsync(ct);
                    await pollDelay.Wait(PollInterval, ct);
                }
            else
            {
                ChargeStop stop;
                do
                {
                    var before = Meter.Energy;
                    await RunChargeLoopIterationAsync(ct);
                    Battery.Add(Meter.Energy - before);
                    await pollDelay.Wait(PollInterval, ct);
                }
                while ((stop = Battery.Stop) == ChargeStop.Running);
                BatteryStop = stop;
            }

            await Exchange<PowerDeliveryRes>(MessageSet.Iso20CommonMessages,
                dest => new PowerDeliveryReq(SessionCtx.ToCommonHeader(), Processing.Finished, ChargeProgress.Stop, null, null)
                    .TryEncode(dest, out int n) ? n : throw EncodeFailed(), ct);

            await RunPostChargeSequenceAsync(ct);

            await Exchange<SessionStopRes>(MessageSet.Iso20CommonMessages,
                dest => new SessionStopReq(SessionCtx.ToCommonHeader(), StopMode, null, null)
                    .TryEncode(dest, out int n) ? n : throw EncodeFailed(), ct);
        }

        /// <summary>
        /// Runs the contract-provisioning exchange (ISO 15118-20 CertificateInstallation): sends the signed
        /// OEM provisioning chain (Id "id1", Josev-interop signature form over the chain's EXI fragment —
        /// the same shape a live Josev EVCC produces), then verifies the response's CPS signature over the
        /// <c>SignedInstallationData</c> fragment and ECDH-unwraps the issued contract private key.
        /// </summary>
        private async Task RunCertificateInstallationAsync(CertInstallEvccOptions oem, CancellationToken ct)
        {
            var chain = new SignedCertificateChainType("id1", oem.OemCertificate,
                oem.OemSubCertificates.Count > 0 ? new SubCertificatesType(oem.OemSubCertificates.ToArray()) : null);

            var fragment = new byte[8192];
            if (!CommonMessagesCodec.EncodeFragment_OEMProvisioningCertificateChain(chain, fragment, out int fragmentLength))
                throw EncodeFailed();
            var signature = XmlDsigInteropSign.Sign("id1", fragment.AsSpan(0, fragmentLength), oem.OemSignKey);

            var res = await Exchange<CertificateInstallationRes>(MessageSet.Iso20CommonMessages,
                dest => new CertificateInstallationReq(SessionCtx.ToCommonHeader() with { Signature = signature },
                    chain,
                    new ListOfRootCertificateIDsType(new[] { new X509IssuerSerialType("CN=V2GRootCA (dev)", 1) }),
                    MaximumContractCertificateChains: 1,
                    PrioritizedEMAIDs: null).TryEncode(dest, out int n) ? n : throw EncodeFailed(), ct,
                SlowMsgTimeout);

            // Verify the CPS signature over the SignedInstallationData fragment (our production form:
            // combined grammar, P-521/SHA-512), then unwrap the contract key.
            var dataBuf = new byte[8192];
            if (CommonMessagesCodec.EncodeFragment_SignedInstallationData(res.SignedInstallationData, dataBuf, out int dataLen)
                && res.Header.Signature is { } resSig
                && resSig.SignedInfo.Reference.Count > 0
                && V2GSignature.VerifyReference(resSig.SignedInfo.Reference[0], dataBuf.AsSpan(0, dataLen)))
            {
                using var cpsLeaf = X509CertificateLoader.LoadCertificate(res.CPSCertificateChain.Certificate);
                using var cpsPub = cpsLeaf.GetECDsaPublicKey();
                InstalledContractSignatureOk = cpsPub is not null
                    && V2GSignature.Verify(resSig.SignedInfo, resSig.SignatureValue.Value, cpsPub);
            }

            if (res.SignedInstallationData.SECP521_EncryptedPrivateKey is { } wrapped)
            {
                InstalledContractKey = ContractProvisioning.RecoverContractKey(
                    oem.OemKeyAgreement, res.SignedInstallationData.DHPublicKey, wrapped);
                InstalledContractCertificate = res.SignedInstallationData.ContractCertificateChain.Certificate;
            }
        }

        /// <summary>
        /// Picks the authorization mode for this session and returns the AuthorizationReq encoder the poll
        /// loop reuses. Plug &amp; Charge — when <see cref="Pnc"/> is set AND the SECC both offers PnC and sent
        /// a GenChallenge — builds and signs the request <b>once</b> (the challenge does not change across
        /// polls): challenge echo + contract chain in <c>PnC_AReqAuthorizationMode</c> (Id "id1"), and the
        /// header signature over its EXI fragment in Josev's interop form (<see cref="XmlDsigInteropSign"/>).
        /// Everything else falls back to EIM.
        /// </summary>
        private Func<byte[], int> BuildAuthorizationReqEncoder(AuthorizationSetupRes authSetup)
        {
            if (Pnc is { } pnc
                && authSetup.AuthorizationServices.Contains(Authorization.PnC)
                && authSetup.PnC_ASResAuthorizationMode is { } pncSetup)
            {
                var pncMode = new PnC_AReqAuthorizationModeType("id1", pncSetup.GenChallenge,
                    new ContractCertificateChainType(pnc.ContractCertificate,
                        new SubCertificatesType(pnc.SubCertificates.ToArray())));

                var fragment = new byte[8192];
                if (!CommonMessagesCodec.EncodeFragment_PnC_AReqAuthorizationMode(pncMode, fragment, out int fragmentLength))
                    throw EncodeFailed();
                var signature = XmlDsigInteropSign.Sign("id1", fragment.AsSpan(0, fragmentLength), pnc.ContractKey);

                AuthorizationMode = "pnc-signed";
                return dest => new AuthorizationReq(SessionCtx.ToCommonHeader() with { Signature = signature },
                    Authorization.PnC, null, pncMode).TryEncode(dest, out int n) ? n : throw EncodeFailed();
            }

            // EIM is what is left, and it too has to be on offer: a station that advertises PnC only is
            // saying it cannot authorize this car, and hearing that at AuthorizationSetup is better than
            // hearing FAILED at AuthorizationReq.
            if (!authSetup.AuthorizationServices.Contains(Authorization.EIM))
                throw new SessionAborted(
                    "AuthorizationSetup: the station offers no EIM authorization "
                  + $"(offered: {String.Join(", ", authSetup.AuthorizationServices)})"
                  + (Pnc is null ? " and this EVCC has no contract certificate." : "."));

            return dest => new AuthorizationReq(SessionCtx.ToCommonHeader(), Authorization.EIM,
                new EIM_AReqAuthorizationModeType(), null).TryEncode(dest, out int n) ? n : throw EncodeFailed();
        }

        /// <summary>
        /// Sends one already-framed request and awaits its reply, enforcing <paramref name="expectedSet"/>
        /// and <see cref="perMessageTimeout"/>. Used directly by the CommonMessages phases above; DC/AC
        /// subclasses call <see cref="ExchangeRaw"/> instead since they need a different result type.
        /// </summary>
        private async Task<TRes> Exchange<TRes>(MessageSet expectedSet, Func<byte[], int> encode, CancellationToken ct,
                                                TimeSpan? budget = null)
        {
            var (set, message) = await ExchangeRaw(expectedSet, encode, ct, budget).ConfigureAwait(false);
            if (message is not TRes reply)
                throw new SessionAborted($"expected a {typeof(TRes).Name} on {expectedSet}, got {message.GetType().Name} on {set}.");
            return reply;
        }

        /// <summary>Same as <see cref="Exchange{TRes}"/> but returns the undiscriminated <see cref="MessageSet"/>/object pair — for DC/AC-specific exchanges.</summary>
        /// <summary>A deadline for one 'Ongoing' phase, for the subclasses' own poll loops.</summary>
        protected OngoingGuard Ongoing(String phase) => new(clock, OngoingTimeout, phase);

        /// <summary>
        /// Say nothing, keep the connection open, and time how long the station leaves the session
        /// standing. Returns null if it was still open when <paramref name="budget"/> ran out.
        /// </summary>
        /// <remarks>
        /// Anything the peer sends while we are silent counts as the session still being alive and is
        /// discarded — the question is when the socket ends, not what arrives on it. A read of zero bytes
        /// is the peer's close; a reset is the same answer by a blunter route, so both stop the clock.
        /// </remarks>
        private async Task<TimeSpan?> WaitForPeerToEndSessionAsync(TimeSpan budget, CancellationToken ct)
        {
            var start = clock.GetUtcNow();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(budget);

            var scratch = new byte[256];
            try
            {
                while (true)
                {
                    int n = await stream.ReadAsync(scratch, cts.Token).ConfigureAwait(false);
                    if (n == 0)
                        return clock.GetUtcNow() - start;   // EOF: the station ended it
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                return null;                                 // still open when the budget ran out
            }
            catch (IOException)
            {
                return clock.GetUtcNow() - start;            // reset rather than a clean close
            }
        }


        /// <param name="budget">The <c>V2G_EVCC_Msg_Timeout</c> this exchange is governed by, or null for
        /// the ordinary one. See <see cref="ChargeLoopMsgTimeout"/> for which messages deviate and why.</param>
        protected async Task<(MessageSet Set, object Message)> ExchangeRaw(MessageSet expectedSet, Func<byte[], int> encode,
                                                                           CancellationToken ct, TimeSpan? budget = null)
        {
            int reqLen = encode(_buf);
            var start = clock.GetUtcNow();
            var wait  = budget ?? perMessageTimeout;
            await V2GTPStream.WriteFrameAsync(stream, expectedSet, _buf.AsMemory(0, reqLen), ct).ConfigureAwait(false);

            // Bound the read itself rather than measuring afterwards. Until 2026-08-11 this awaited
            // ReadFrameAsync with no budget and *then* compared the elapsed time, so the timeout caught an
            // answer that arrived late and never a station that simply stopped answering — that one held
            // the car until the session-level token fired, minutes in a live run. Same defect and same fix
            // as Secc20Base.RunAsync's read budget, one side of the wire over.
            MessageSet set;
            object message;
            using (var readCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                readCts.CancelAfter(wait);
                try
                {
                    (set, message) = await V2GTPStream.ReadFrameAsync(stream, readCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (readCts.IsCancellationRequested && !ct.IsCancellationRequested)
                {
                    throw new SessionAborted($"no response within {wait.TotalMilliseconds:0} ms.");
                }
            }

            var elapsed = clock.GetUtcNow() - start;

            // Kept as well: a clock the read budget cannot see (a ManualTimeProvider in the timing tests)
            // still has to be able to fail an over-long exchange.
            if (elapsed > wait)
                throw new SessionAborted($"no response within {wait.TotalMilliseconds:0} ms (took {elapsed.TotalMilliseconds:0} ms).");

            RefuseOnFailure(message);

            Exchanges++;
            BytesOnWire += V2GTPCodec.HeaderSize + reqLen;
            return (set, message);
        }


        /// <summary>
        /// Ends the session when the station answers with a code from the <c>FAILED</c> family.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Found live, not by reasoning.</b> Until 2026-08-01 nothing in the -20 EVCC looked at a
        /// response code at all: <c>Expect&lt;T&gt;</c> checks the message set and the type, and the
        /// cable-check loop watched only <c>EVSEProcessing</c>. eVDriveFlow answered
        /// <c>DC_CableCheckRes</c> with <c>FAILED</c> and our car went on to PreCharge, PowerDelivery and
        /// into the charge loop — a station could answer FAILED to every message of a session and we
        /// would drive it to completion
        /// (<c>ISO15118ConformanceTests/docs/interop-runs/2026-08-01-edf-iso20-dc-notls/</c>).
        /// </para>
        /// <para>
        /// The loopback suite could not have found this: our own SECC never answers FAILED, so no
        /// recorded session and no trace replay contains one. It needed a station that says it.
        /// </para>
        /// <para>
        /// <b>The three families.</b> <c>OK*</c> continues, <c>WARNING*</c> continues — a warning is
        /// explicitly the code for "something is off and the session goes on" — and <c>FAILED*</c>
        /// terminates. The comparison is <c>&gt;= FAILED</c> because the enumeration is ordered by family
        /// in the schema (OK 0–4, WARNING 5–20, FAILED 21 and up), which
        /// <c>Evcc20FailureHandlingTests.TheResponseCodeFamiliesAreContiguousAndOrdered</c> pins so a
        /// regenerated enum cannot quietly move the boundary.
        /// </para>
        /// <para>
        /// <b>Why it aborts rather than sending SessionStop.</b> A FAILED response is the station saying
        /// it is done; the specification has the EVCC terminate, and sending a further message invites a
        /// second error on a session that already has one. The CLI and the fixtures surface the
        /// <see cref="SessionAborted"/> with the code in it, which is what a live run needs to read.
        /// </para>
        /// <para>
        /// Each message set has its own generated <c>ResponseCode</c> and its own <c>V2GResponseType</c>
        /// base, so all three are matched here. Anything without a code — the SupportedAppProtocol
        /// handshake, a request — falls through untouched.
        /// </para>
        /// </remarks>
        private static void RefuseOnFailure(object message)
        {

            var failure = message switch
            {
                V2GResponseType     r when r.ResponseCode >= ResponseCode.FAILED
                    => r.ResponseCode.ToString(),
                Ac20.V2GResponseType r when r.ResponseCode >= Ac20.ResponseCode.FAILED
                    => r.ResponseCode.ToString(),
                Dc20.V2GResponseType r when r.ResponseCode >= Dc20.ResponseCode.FAILED
                    => r.ResponseCode.ToString(),
                _   => null,
            };

            if (failure is not null)
                throw new SessionAborted(
                    $"the station answered {message.GetType().Name} with {failure}; the session ends here.");

        }

        // ISO 15118-20 energy-transfer service ids (Table 204): AC=1, DC=2, AC_BPT=5, DC_BPT=6,
        // MCS=8, MCS_BPT=9. MCS is the DC message set under different ids, so it is *drivable* by a DC
        // EVCC even when it is not what that EVCC would ask for first — which is the difference the two
        // lists below carry.
        private static readonly ushort[] DcServiceIds     = { EnergyTransferService.DC, EnergyTransferService.DC_BPT };
        private static readonly ushort[] AcServiceIds     = { EnergyTransferService.AC, EnergyTransferService.AC_BPT };
        private static readonly ushort[] DcDrivableIds    = { EnergyTransferService.DC,  EnergyTransferService.DC_BPT,
                                                              EnergyTransferService.MCS, EnergyTransferService.MCS_BPT };
        private static readonly ushort[] AcDrivableIds    = { EnergyTransferService.AC, EnergyTransferService.AC_BPT };

        /// <summary>Energy-transfer service ids this EVCC will accept from the SECC's catalogue, best first.
        /// Virtual so an MCS vehicle can ask for the megawatt services instead — see
        /// <see cref="Evcc20Mcs"/>. <see cref="PreferBidirectionalService"/> reorders whatever this returns,
        /// so a subclass states <i>which</i> catalogue it wants and never has to write it out twice.</summary>
        protected virtual IReadOnlyList<ushort> PreferredEnergyServiceIds
            => EnergyMode == PowerMode.Dc ? DcServiceIds : AcServiceIds;

        /// <summary>Every service id whose messages this EVCC can actually speak — the ones on its own
        /// message set. Wider than <see cref="PreferredEnergyServiceIds"/> on purpose: a megawatt truck at an
        /// ordinary DC charger should take the DC service rather than refuse, and a DC car at an AC-only
        /// station has nothing to take.</summary>
        protected virtual IReadOnlyList<ushort> DrivableEnergyServiceIds
            => EnergyMode == PowerMode.Dc ? DcDrivableIds : AcDrivableIds;

        /// <summary>Picks the energy-transfer service to select from the SECC's advertised list: the best one
        /// this EVCC asks for, else any other it can actually drive, else a refusal.</summary>
        private ushort SelectEnergyTransferService(ServiceDiscoveryRes res)
        {
            var offered = res.EnergyTransferServiceList.Service;
            if (offered.Count == 0)
                throw new SessionAborted("ServiceDiscovery: the SECC advertised no energy-transfer service.");

            var preferred = Ranked(PreferredEnergyServiceIds);
            var drivable  = Ranked(DrivableEnergyServiceIds);

            // PreferBidirectionalService, applied here rather than in each list, so it holds for the
            // overridden ones too — Evcc20Mcs's { 8, 9 } becomes { 9, 8 } by the same rule that turns DC's
            // { 2, 6 } into { 6, 2 }. OrderBy is a *stable* sort, which is the whole trick: entries keep
            // their relative order within each half, so this promotes the bidirectional services without
            // inventing an order among them, and is a no-op on a list that has none.
            IReadOnlyList<ushort> Ranked(IReadOnlyList<ushort> ids)
                => PreferBidirectionalService
                       ? [.. ids.OrderByDescending(EnergyTransferService.IsBidirectional)]
                       : ids;

            // First choice, then anything else on our own message set. The old fallback was `offered[0]`,
            // which for a DC car at an AC-only station selects the AC service and then sends the next
            // request on the DC set — refused two exchanges later, for a reason that no longer names the
            // cause. Falling back *within* the message set keeps the case this is really for (a megawatt
            // truck at an ordinary DC charger) and drops the one it never was.
            //
            // Both lookups walk *our* list and ask whether the station offers that id — not the other way
            // round. It used to read `offered.FirstOrDefault(s => preferred.Contains(s.ServiceID))`, which
            // walks the station's catalogue and takes the first entry we happen to accept: our order was
            // never read, `PreferredEnergyServiceIds` was a set rather than the ranking its summary claims,
            // and the station decided. A live run against EVerest found it — a catalogue of [8, 9] handed an
            // EVCC listing { 9, 8 } the service 8 (2026-08-05-everest-mcs-bpt). This project holds EVerest's
            // IsoMux to exactly this rule for the SAP Priority *we* send, which is reason enough not to be
            // the same way round the other way.
            var match = FirstOffered(preferred) ?? FirstOffered(drivable);

            ServiceType? FirstOffered(IReadOnlyList<ushort> ranked)
            {
                foreach (var id in ranked)
                    if (offered.FirstOrDefault(s => s.ServiceID == id) is { } found)
                        return found;
                return null;
            }

            if (match is null)
                throw new SessionAborted(
                    $"ServiceDiscovery: the station offers no {(EnergyMode == PowerMode.Dc ? "DC" : "AC")} "
                  + $"energy-transfer service (wanted {String.Join("/", preferred)}, "
                  + $"offered {String.Join(", ", offered.Select(s => s.ServiceID))}).");

            return match.ServiceID;
        }

        /// <summary>Picks the parameter set to select from the SECC's ServiceDetail: the one whose
        /// <c>ControlMode</c> matches the mode this EVCC is about to drive (1 = Scheduled, 2 = Dynamic), else
        /// the first offered set. The order the SECC lists them in is its own preference and not binding —
        /// what binds is that the selected set and the ScheduleExchange agree.</summary>
        private ushort SelectParameterSet(ServiceDetailRes res)
        {
            var sets = res.ServiceParameterList.ParameterSet;
            if (sets.Count == 0)
                throw new SessionAborted("ServiceDetail: the SECC advertised no parameter set.");

            int wanted = PreferDynamicControlMode ? 2 : 1;
            var match  = sets.FirstOrDefault(p => p.Parameter.Any(x => x.Name == "ControlMode" && x.IntValue == wanted));

            if (match is null && PreferDynamicControlMode)
                throw new SessionAborted("ServiceDetail: Dynamic control mode was requested, but the station "
                                       + "offers no parameter set with ControlMode = 2.");

            return (match ?? sets[0]).ParameterSetID;
        }

        /// <summary>The Dynamic-mode ScheduleExchange request: a departure time and what the battery needs,
        /// instead of a schedule to choose from. The three energy fields are <b>mandatory</b> in this arm
        /// (they are optional in the Scheduled one), which is the schema saying the same thing: a station can
        /// only steer if it knows the target.</summary>
        /// <remarks>
        /// This is the one -20 request where the state-of-charge goals are the car's to state — the
        /// charge-loop request has neither <c>TargetSOC</c> nor <c>MinimumSOC</c> — so <c>--target-soc</c>
        /// and <c>--min-soc</c> land here. Without a pack the four figures stay the constants every
        /// recorded run carries.
        /// </remarks>
        private Dynamic_SEReqControlModeType DynamicScheduleRequest()
            => new(DepartureTime:            DepartureTime,
                   MinimumSOC:               Battery is null ? (sbyte) 30 : DeclaredMinimumSoCPercent,
                   TargetSOC:                Battery is null ? (sbyte) 80 : DeclaredTargetSoCPercent,
                   EVTargetEnergyRequest:    EnergyRequestWh is { } e1 ? WattHours(e1.Target)  : new RationalNumberType(3, 30),   // 30 kWh
                   EVMaximumEnergyRequest:   EnergyRequestWh is { } e2 ? WattHours(e2.Maximum) : new RationalNumberType(3, 60),   // 60 kWh
                   EVMinimumEnergyRequest:   EnergyRequestWh is { } e3 ? WattHours(e3.Minimum) : new RationalNumberType(3, 10),   // 10 kWh
                   EVMaximumV2XEnergyRequest: null,
                   EVMinimumV2XEnergyRequest: null);

        /// <summary>Watt-hours as a CommonMessages rational; the arithmetic is <see cref="ScaledRational"/>.</summary>
        private static RationalNumberType WattHours(double wattHours)
        {
            var (value, exponent) = ScaledRational(wattHours);
            return new RationalNumberType(exponent, value);
        }

        /// <summary>Verifies a signed <c>AbsolutePriceSchedule</c> in the Scheduled-mode offer, if any:
        /// reference digest over the schedule's re-encoded EXI fragment, then (with
        /// <see cref="TariffVerifyKey"/>) the ECDSA-P521/SHA-512 signature over the SignedInfo — the -20
        /// analogue of the -2 SalesTariff check. No signed schedule → <see cref="Tariff"/> stays null
        /// (Josev's SECC, for one, never signs its price schedules).</summary>
        private void VerifyPriceSchedule(ScheduleExchangeRes res)
        {
            // Moved into Iso20PriceScheduleCheck 2026-08-17, as the -2 tariff check was: the verdict never
            // reaches the wire, so no recorded session can hold it, and inline it could not be reached
            // without a socket. `null` back means the offer carried no signed schedule at all — not a
            // failure, and Tariff stays null to say so rather than reporting three falses.
            if (Iso20PriceScheduleCheck.Evaluate(res, res.Header.Signature, TariffVerifyKey) is { } verdict)
                Tariff = verdict;
        }

        /// <summary>Builds the EVPowerProfile that <c>PowerDelivery(Start)</c> must carry. Scheduled mode
        /// selects the first schedule tuple the SECC returned in <c>ScheduleExchangeRes</c> and echoes one
        /// power-schedule entry (falling back to tuple id 1 if the SECC returned no Scheduled control mode);
        /// Dynamic mode has no tuple to point at, so its control-mode element is empty — the profile is then
        /// only the EV's own power curve.</summary>
        private EVPowerProfileType BuildEvPowerProfile(ScheduleExchangeRes scheduleRes)
        {
            uint tupleId = scheduleRes.Scheduled_SEResControlMode?.ScheduleTuple.FirstOrDefault()?.ScheduleTupleID ?? 1;

            return new EVPowerProfileType(
                TimeAnchor: 0,
                Dynamic_EVPPTControlMode: PreferDynamicControlMode ? new Dynamic_EVPPTControlModeType() : null,
                // PowerToleranceAcceptance is schema-optional but Josev's model requires it (its SECC rejects
                // an absent one); a live run needed it set. PowerToleranceConfirmed = the EV accepts the tolerance.
                Scheduled_EVPPTControlMode: PreferDynamicControlMode
                                                ? null
                                                : new Scheduled_EVPPTControlModeType(tupleId, PowerToleranceAcceptance.PowerToleranceConfirmed),
                EVPowerProfileEntries: new EVPowerProfileEntryListType(new[]
                {
                    // one 1-hour entry at 10 kW (Power = 10 × 10^3 W)
                    new PowerScheduleEntryType(Duration: 3600, Power: new RationalNumberType(3, 10), Power_L2: null, Power_L3: null),
                }));
        }

        private static InvalidOperationException EncodeFailed() => new("EXI encode failed (buffer too small?).");
    }
}
