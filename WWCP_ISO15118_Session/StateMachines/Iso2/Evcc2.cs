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
using cloud.charging.open.protocols.ISO15118.Session;
using cloud.charging.open.protocols.ISO15118.StateMachines.Iso20;
using cloud.charging.open.protocols.ISO15118.Timing;
using System.Collections.Concurrent;
using System.Reflection;

using cloud.charging.open.protocols.ISO15118.EXI.Dispatch;


namespace cloud.charging.open.protocols.ISO15118.StateMachines.Iso2
{
    /// <summary>The EV's smart-charging verdict over the SASchedule offer: how many tuples were offered,
    /// whether the SalesTariffs carried a header signature and it verified (digest per tariff + ECDSA,
    /// dual-grammar like the SECC's checks), which tuple the EV chose (lowest average EPriceLevel), and how
    /// many ChargingProfile entries it derived from that tuple's PMaxSchedule.</summary>
    public sealed record Iso2TariffResult(int TuplesOffered, bool SignaturePresent, bool DigestOk,
                                          bool SignatureOk, string SignatureGrammar,
                                          byte ChosenTupleId, int ProfileEntries);

    /// <summary>
    /// The vehicle (EVCC) side of an ISO 15118-2 session — it drives the session over an already-connected
    /// (and, for -20, already-SAP-negotiated) <see cref="Stream"/>. Each step is one request/response
    /// exchange framed as V2GTP/EXI via <see cref="V2GTPStream"/>; the poll loops (Authorization,
    /// ChargeParameterDiscovery) back off through <see cref="IAsyncDelay"/> instead of a hardcoded
    /// <c>Task.Delay</c>, and every exchange is checked against <paramref name="perMessageTimeout"/> using
    /// <paramref name="clock"/> — mirroring the ISO 15118-2 EV-side performance timeout, simplified.
    /// Payment: EIM (<c>ExternalPayment</c>) by default; with <see cref="Pnc"/> set and the SECC offering
    /// <c>Contract</c>, the session runs -2 Plug &amp; Charge — PaymentDetails (contract chain in,
    /// GenChallenge out), a <b>signed</b> AuthorizationReq, and a <b>signed</b> MeteringReceiptReq whenever
    /// a charging-status response demands one (all in Josev's signature form, <see cref="XmlDsigInterop2"/>).
    /// </summary>
    public sealed class Evcc2(
        Stream stream, PowerMode mode, TimeProvider clock, IAsyncDelay pollDelay, TimeSpan perMessageTimeout)
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(50);

        /// <summary>
        /// What the car waits between charge-loop iterations. Null (default) uses the same 50 ms as every
        /// other poll in this class, which is what every recorded run was taken at.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Separate from <c>PollInterval</c> on purpose.</b> That one also paces the authorization poll,
        /// <c>ChargeParameterDiscovery</c> and <c>CableCheck</c>, and those intervals are *measured against*
        /// in this project — a counterparty's pacing is judged by how fast our side asks. Raising the shared
        /// constant to make a session last longer would silently move the yardstick of every one of those
        /// findings.
        /// </para>
        /// <para>
        /// <b>What it is for.</b> One charge-loop iteration stands for
        /// <see cref="Metering.ChargeLoopSample.Period"/> — one minute of simulated charging — so a
        /// physically sensible charge is tens of iterations and, at 50 ms apart, is over in a second of wall
        /// clock. A session somebody wants to *watch* against a live station needs the two clocks pulled
        /// apart: this sets the real one, <see cref="Battery"/> sets how many iterations there are.
        /// </para>
        /// <para>
        /// It cannot make a session non-conformant by being too slow in any interesting range:
        /// ISO 15118-2 gives the SECC <c>V2G_SECC_SequenceTimeout</c> = 60 s, so seconds between requests
        /// are ordinary. A value above that is the caller measuring a station's timeout, which is a
        /// different run and has its own knob.
        /// </para>
        /// </remarks>
        public TimeSpan? ChargeLoopInterval { get; set; }

        // 8 KiB: a PaymentDetailsReq carries the full 3-cert contract chain (~2 KiB).
        private readonly byte[] _buf = new byte[8192];
        private byte[] _sid = new byte[8];   // 0 until SessionSetupRes assigns one

        public int Exchanges { get; private set; }
        public long BytesOnWire { get; private set; }

        /// <summary>Contract credentials (same shape as the -20 EVCC's); <c>null</c> (default) pays via EIM.</summary>
        public PncEvccOptions? Pnc { get; set; }

        /// <summary>Provisioning credentials; when set (and the station advertises the certificate service),
        /// the EVCC asks for a contract before authorizing. <c>null</c> (default) skips it.</summary>
        public Iso2CertInstallOptions? CertInstallRequest { get; set; }

        /// <summary>
        /// The parameter-set ID this car names when it selects the certificate service, overriding the
        /// conformant pairing — <b>Installation is set 1, Update is set 2</b>. Null (default) keeps the
        /// pairing, which is what a real car does and what every recorded run used.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>A deliberately non-conformant probe, and it exists because a station's own state table said
        /// so.</b> EVerest's <c>EvseV2G</c> advertises the certificate service with parameter-set 1 alone —
        /// <c>Update</c> is an explicit <c>TODO</c> in <c>ISO15118_chargerImpl.cpp</c> — so a car pairing
        /// <i>Update → set 2</i> is refused at <c>PaymentServiceSelection</c> with
        /// <c>FAILED_ServiceSelectionInvalid</c> and never reaches their handler. Their sequence table
        /// nonetheless admits <c>CertificateUpdateReq</c> in the state after a Contract selection — it is
        /// named <c>WAIT_FOR_PAYMENTDETAILS_CERTINST_CERTUPD</c> — and the state is chosen by the payment
        /// option, not by the parameter set. Selecting the set they offer and sending the other message
        /// therefore reaches code their own dispatch says should handle it.
        /// </para>
        /// <para>
        /// <b>It does not manufacture the defect it measures.</b> What the handler then does — answering
        /// from the union slot the previous response left behind — is theirs and would happen identically
        /// to a fully conformant car the moment they advertised set 2. Any run using this has to say so:
        /// the car is off-profile here on purpose, and a report that hides that is refutable in one
        /// sentence.
        /// </para>
        /// </remarks>
        public short? CertificateParameterSetId { get; set; }

        /// <summary>The contract certificate (DER) installed via the provisioning exchange, once received —
        /// with <see cref="InstalledContractKey"/> proving the ECDH unwrap round-tripped.</summary>
        public byte[]? InstalledContractCertificate { get; private set; }

        /// <summary>The unwrapped contract private key (P-256); the caller owns disposal.</summary>
        public ECDsa? InstalledContractKey { get; private set; }

        /// <summary>Whether the response's four-reference signature verified against the provisioning
        /// certificate the station sent with it.</summary>
        public bool InstalledContractSignatureOk { get; private set; }

        /// <summary>The eMAID the operator issued the contract under.</summary>
        public string? InstalledEmaid { get; private set; }

        /// <summary>How this session authorized: <c>"eim"</c>, or <c>"pnc-signed"</c> after a Contract
        /// PaymentDetails + signed AuthorizationReq.</summary>
        public string AuthorizationMode { get; private set; } = "eim";

        /// <summary>How many signed MeteringReceiptReq this session sent (Contract only).</summary>
        public int MeteringReceiptsSent { get; private set; }

        /// <summary>
        /// The vehicle's own energy counter — what this EV thinks it took, kept independently of what
        /// the station reports (<c>EVSimulatorApp/docs/CONCEPT.md</c> §4.2/§4.3).
        /// </summary>
        /// <remarks>
        /// Settable so a caller can shorten or lengthen what one charge-loop iteration stands for; see
        /// <see cref="EvMeter"/> for why an iteration has to declare a duration at all.
        /// </remarks>
        public EvMeter Meter { get; init; } = new();

        /// <summary>How to end the session: <c>Terminate</c> (default) or <c>Pause</c> — after a pause the
        /// caller reconnects and resumes via <see cref="ResumeSessionId"/> ([V2G2-740]).</summary>
        public ChargingSession StopMode { get; set; } = ChargingSession.Terminate;

        /// <summary>A paused predecessor's session id: the opening SessionSetupReq carries it (instead of
        /// the all-zero id) so the SECC rejoins the old session.</summary>
        public byte[]? ResumeSessionId { get; set; }

        /// <summary>
        /// The SessionID this car puts in every request <b>after</b> SessionSetup, instead of the one the
        /// station issued. Null (the default) means "the one we were given", which is what a conformant car
        /// does and what every recorded session contains.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This exists so a station's `[V2G2-460]` handling is reachable from a session at all. Until
        /// 2026-08-11 no test or interop run of this suite could send a wrong SessionID, so **no run had ever
        /// exercised any station's duty to refuse one** — ours included, which turned out not to implement it.
        /// The same shape as <c>Evcc20Base.RequestMeterInfo</c> and the running-limit clamp before it: a
        /// question the car could not ask, hiding an answer nobody checked.
        /// </para>
        /// <para>
        /// Eight zero bytes are the interesting value rather than a random one — that is what ISO reserves
        /// for *"I have no session"*, what a station's decoder is likeliest to special-case, and what
        /// EVerest's `EvseV2G` was measured serving as if it were the session owner
        /// (<c>docs/reports/everest-evsev2g-session-id-zero.md</c>).
        /// </para>
        /// </remarks>
        public byte[]? SendSessionId { get; set; }

        /// <summary>
        /// What a paused predecessor already charged, so this session asks for the remainder:
        /// <c>[V2G2-743]</c> requires <c>EAmount</c> on a resume to be reduced by the energy already
        /// charged.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Read only when there is no <see cref="Battery"/>, and that is the whole contract.</b> A pack
        /// carried across the pause already holds the better answer — its state of charge moved, so
        /// <c>EnergyNeededWh</c> is the remainder by construction — and subtracting this on top would count
        /// the same energy twice. So the battery wins where there is one, and a caller that sets both still
        /// gets the right number rather than a footgun.
        /// </para>
        /// <para>
        /// A real car cannot be in the second case: its pack does not forget when the cable comes out. The
        /// simulator can, because a battery is optional here, and until 2026-08-10 the CLI built a
        /// <i>fresh</i> one per connection — so a resumed session asked for the full original amount again
        /// with a pack that had already been charged. That was the actual violation; this property is only
        /// what covers the batteryless case behind it.
        /// </para>
        /// </remarks>
        public Double AlreadyChargedWh { get; set; }

        /// <summary>
        /// How long a phase may keep answering <c>EVSEProcessing = Ongoing</c> before the session ends.
        /// </summary>
        /// <remarks>60 s, ISO 15118's EVCC ongoing timeout. See <see cref="OngoingGuard"/> for the live
        /// run that made this necessary — without it a station that answers promptly and never finishes
        /// is polled for ever.</remarks>
        public TimeSpan OngoingTimeout { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>The SECC's SessionSetup verdict: <c>OK_NewSessionEstablished</c> or, on a successful
        /// resume, <c>OK_OldSessionJoined</c>.</summary>
        public ResponseCode SessionSetupCode { get; private set; }

        /// <summary>The session id in effect (SECC-assigned, or the rejoined one) — keep it for a resume.</summary>
        public byte[] SessionId => _sid;

        /// <summary>When set, the EV initiates one renegotiation on its own after the first charging-status
        /// cycle (<c>PowerDeliveryReq(Renegotiate)</c> → new ChargeParameterDiscovery → the DC isolation
        /// sequence → PowerDelivery(Start)). Independent of that, the EV always reacts to a SECC-side
        /// <c>EVSENotification.ReNegotiation</c>.</summary>
        public bool Renegotiate { get; set; }

        /// <summary>DC only: renegotiate the way this car did until 2026-08-15 — straight from the new
        /// <c>ChargeParameterDiscovery</c> to <c>PowerDelivery(Start)</c>, skipping <c>CableCheck</c> and
        /// <c>PreCharge</c>. <b>Off</b> by default, because it is the non-conformant sequence.</summary>
        /// <remarks>
        /// It exists to point a car that skips the isolation sequence at a station and see what the station
        /// does — which is how the fix on both sides is shown to be load-bearing, and how the answer
        /// EVerest's <c>EvseV2G</c> gave us on 2026-08-11 is reproduced against our own station. A run that
        /// sets this is measuring a refusal, not charging: expect <c>FAILED_SequenceError</c> from any
        /// station that implements the DC state table.
        /// </remarks>
        public bool RenegotiationSkipsIsolationSequence { get; set; }

        /// <summary>DC only: declare <c>EVReady = false</c> in the <c>DC_EVStatus</c> of the isolation
        /// sequence — <c>CableCheckReq</c> and <c>PreChargeReq</c> — instead of the <c>true</c> this car
        /// sends everywhere. <b>Off</b> by default, so every recorded session keeps its bytes.</summary>
        /// <remarks>
        /// <para>
        /// An instrument, not a conformance claim. `EVReady` is a status flag — Table E.1 maps
        /// <c>false</c> to SAE J2847/2's *vehicle not ready* and <c>true</c> to *vehicle charging or
        /// energy transfer* — and no `[V2G2-…]` this project can read ties it to the isolation phase. So
        /// which value belongs in a <c>CableCheckReq</c> is, as far as the document goes, open.
        /// </para>
        /// <para>
        /// What is not open is the question this exists to ask. On 2026-08-15 a renegotiated
        /// <c>CableCheckReq</c> was accepted by EVerest's station and then failed inside their
        /// <c>EvseManager</c>, which waits for the DC link to fall below 60 V; our car was announcing that
        /// it was ready to charge at that moment. Whether their supply ramps down once the car says
        /// otherwise is a measurement, and this is the one knob it needs
        /// (<c>docs/interop-runs/2026-08-15-everest-iso2-renegotiation-rerun/</c>).
        /// </para>
        /// </remarks>
        public bool IsolationDeclaresNotReady { get; set; }

        /// <summary>True while <see cref="RunDcIsolationSequence"/> is running, so <c>EvStatus()</c> can
        /// answer differently there without every caller having to know about the phase.</summary>
        private bool _inIsolationSequence;

        /// <summary>How many renegotiation cycles this session ran (own + SECC-requested).</summary>
        public int Renegotiations { get; private set; }

        /// <summary>
        /// A battery that fills up, and the goal that ends the charge loop. Null — the default — keeps the
        /// fixed three iterations every recorded interop run was taken with. See the -20 side for why this
        /// is opt-in rather than the default.
        /// </summary>
        public Simulation.EvBattery? Battery { get; set; }

        /// <summary>Why the charge loop ended; null while it has not finished.</summary>
        public Simulation.ChargeStop? BatteryStop { get; private set; }

        /// <summary>The tariff signer's public key (fachlich the Mobility Operator's). When set AND the
        /// SECC's SASchedule offer carries signed SalesTariffs, the EV verifies them (§7.9.2.5); without a
        /// key the tariffs are still read for the price-aware tuple choice, just not verified.</summary>
        public ECDsa? TariffVerifyKey { get; set; }

        /// <summary>The smart-charging verdict over the (last) SASchedule offer; null until
        /// ChargeParameterDiscovery finished.</summary>
        public Iso2TariffResult? Tariff { get; private set; }

        private MessageHeaderType? _lastHeader;             // the header of the response just received
        private byte _chosenTupleId = 1;                    // the SAScheduleTuple the EV selected
        private EnergyTransferMode _energyTransferMode;     // chosen from what ServiceDiscoveryRes offered
        private ChargingProfileType? _chargingProfile;      // derived from the chosen tuple's PMaxSchedule

        public async Task RunAsync(CancellationToken ct = default)
        {
            // ── SETUP ──────────────────────────────────────────────────────────
            // Check the credential before opening the session, not four exchanges in: a station that
            // has already assigned a session id and been asked for its payment options should not
            // then be abandoned over something knowable before the first byte.
            if (Pnc is not null)
                ContractEmaid();

            if (ResumeSessionId is not null)
                _sid = ResumeSessionId;   // rejoin: the SessionSetupReq header carries the paused session's id
            var setup = await Send<SessionSetupResType>(new SessionSetupReqType(EVCCID: new byte[] { 0xAB, 0xCD, 0xEF, 0x01, 0x02, 0x03 }), ct);
            SessionSetupCode = setup.ResponseCode;
            var discovery = await Send<ServiceDiscoveryResType>(new ServiceDiscoveryReqType(ServiceScope: null, ServiceCategory: null), ct);
            _energyTransferMode = SelectEnergyTransferMode(discovery);

            // The service id is the station's, not a constant. Ours has always been 1 and so has every
            // counterparty's so far, which is exactly why this was a literal until the sweep of 2026-08-03.
            ushort chargeServiceId = discovery.ChargeService?.ServiceID
                ?? throw new SessionAborted("ServiceDiscovery: the station advertised no ChargeService.");

            bool contract = Pnc is not null && discovery.PaymentOptionList.PaymentOption.Contains(PaymentOption.Contract);

            // Contract provisioning is a value-added service in -2: it has to be found in the station's
            // ServiceList and then *selected* by id, where -20 needs only a flag in AuthorizationSetupRes.
            var certificateService = CertInstallRequest is null
                                         ? null
                                         : discovery.ServiceList?.Service
                                                    .FirstOrDefault(s => s.ServiceCategory == ServiceCategory.ContractCertificate);

            var selected = new List<SelectedServiceType> { new(chargeServiceId, ParameterSetID: null) };
            if (certificateService is not null)
                selected.Add(new SelectedServiceType(certificateService.ServiceID,
                                                     CertificateParameterSetId
                                                         ?? (short) (CertInstallRequest!.Action == Iso2CertificateAction.Update ? 2 : 1)));

            await Send<PaymentServiceSelectionResType>(new PaymentServiceSelectionReqType(
                contract ? PaymentOption.Contract : PaymentOption.ExternalPayment,
                new SelectedServiceListType(selected.ToArray())), ct);

            if (certificateService is not null)
                await RunCertificateProvisioningAsync(CertInstallRequest!, ct);

            // ── AUTH (loop until authorised) ───────────────────────────────────
            // Contract: PaymentDetails first (contract chain → GenChallenge), then a signed AuthorizationReq
            // (Id "id1", challenge echo; body-element fragment digested, Josev-form signature). Signed once —
            // the challenge does not change across polls. EIM: the plain unsigned request.
            AuthorizationReqType authReq;
            SignatureType? authSignature = null;
            if (contract)
            {
                var details = await Send<PaymentDetailsResType>(new PaymentDetailsReqType(
                    ContractEmaid(), new CertificateChainType(Id: null, Pnc!.ContractCertificate,
                        new SubCertificatesType(Pnc.SubCertificates.ToArray()))), ct);

                authReq = new AuthorizationReqType("id1", details.GenChallenge);
                var fragment = new byte[1024];
                if (!Iso2Codec.EncodeFragment_AuthorizationReq(authReq, fragment, out int n))
                    throw new InvalidOperationException("AuthorizationReq fragment encode failed.");
                authSignature = XmlDsigInterop2.Sign("id1", fragment.AsSpan(0, n), Pnc.ContractKey);
                AuthorizationMode = "pnc-signed";
            }
            else
                authReq = new AuthorizationReqType(Id: null, GenChallenge: null);

            var authGuard = new OngoingGuard(clock, OngoingTimeout, "Authorization");
            while ((await Send<AuthorizationResType>(authReq, ct, authSignature))
                       .EVSEProcessing != EVSEProcessing.Finished)
            {
                authGuard.Tick();
                await pollDelay.Wait(PollInterval, ct);
            }

            // ── CHARGE PARAMETERS (+ DC cable check / pre-charge) ──────────────
            await RunChargeParameterDiscovery(ct);

            if (mode == PowerMode.Dc)
                await RunDcIsolationSequence(ct);

            // ── CHARGE ─────────────────────────────────────────────────────────
            await Send<PowerDeliveryResType>(PowerDelivery(ChargeProgress.Start), ct);

            bool renegotiated = false;
            // Three iterations stand in for a session when there is no battery; with one, the loop ends
            // when the car is done. Same rule as the -20 side, and the same reason it is opt-in: every
            // recorded run was taken at three.
            for (int cycle = 0; Battery is null ? cycle < 3 : BatteryStop is null; cycle++)
            {
                var energyBefore = Meter.Energy;
                // A Contract SECC may demand a receipt (ReceiptRequired) in its status response — answer with
                // a signed MeteringReceiptReq echoing its MeterInfo (as a real EV, e.g. Josev, does).
                EVSENotification notification;
                if (mode == PowerMode.Dc)
                {
                    var demand = CurrentDemand();
                    var cd = await Send<CurrentDemandResType>(demand, ct);
                    notification = cd.DC_EVSEStatus.EVSENotification;

                    // What this station can deliver *now*. -2 lets it restate its ceiling every
                    // iteration, and the next request is where reading it shows.
                    ReadEvseLimits(cd.EVSEMaximumCurrentLimit, cd.EVSEMaximumPowerLimit);

                    // The EV's own view, from the EV's own request. ISO 15118-2 gives a DC vehicle no
                    // field for a *measured* inlet power, so what it asked for is the closest thing it
                    // owns — and taking the station's EVSEPresent* instead would make this counter an
                    // echo of the very number it exists to be compared against.
                    Meter.Sample((double) demand.EVTargetVoltage.ToDecimal(),
                                 (double) demand.EVTargetCurrent.ToDecimal());

                    if (cd.ReceiptRequired == true && cd.MeterInfo is not null)
                        await SendMeteringReceipt(cd.MeterInfo, cd.SAScheduleTupleID, ct);
                }
                else
                {
                    var cs = await Send<ChargingStatusResType>(new ChargingStatusReqType(), ct);
                    notification = cs.AC_EVSEStatus.EVSENotification;

                    // AC carries no power in either direction, so the EV's own view is the profile it
                    // committed to in PowerDeliveryReq — which it derived itself from the tuple it
                    // chose, and which the station validated against its own PMax.
                    Meter.Sample(CommittedPowerW());

                    if (cs.ReceiptRequired == true && cs.MeterInfo is not null)
                        await SendMeteringReceipt(cs.MeterInfo, cs.SAScheduleTupleID, ct);
                }

                // Renegotiation ([V2G2-841]) — reactive (the SECC notified ReNegotiation) or proactive
                // (Renegotiate option, once): PowerDelivery(Renegotiate) → fresh ChargeParameterDiscovery →
                // PowerDelivery(Start), then the charging loop continues.
                if (!renegotiated && (notification == EVSENotification.ReNegotiation || Renegotiate))
                {
                    renegotiated = true;
                    Renegotiations++;
                    await Send<PowerDeliveryResType>(PowerDelivery(ChargeProgress.Renegotiate), ct);
                    await RunChargeParameterDiscovery(ct);

                    // DC returns through the isolation sequence, exactly as it did on the way in: the
                    // SECC state table admits CableCheckReq after ChargeParameterDiscoveryReq and nothing
                    // else ([V2G2-565], [V2G2-582]), with no renegotiation exception. Until 2026-08-15
                    // this line was missing and the car went straight to PowerDelivery(Start) — which a
                    // conformant station refuses, and EVerest's did. AC has no such phase and skips it.
                    if (mode == PowerMode.Dc && !RenegotiationSkipsIsolationSequence)
                        await RunDcIsolationSequence(ct);

                    await Send<PowerDeliveryResType>(PowerDelivery(ChargeProgress.Start), ct);
                }

                if (Battery is not null)
                {
                    Battery.Add(Meter.Energy - energyBefore);
                    if (Battery.Stop is var stop && stop != Simulation.ChargeStop.Running)
                        BatteryStop = stop;
                }

                await pollDelay.Wait(ChargeLoopInterval ?? PollInterval, ct);
            }

            await Send<PowerDeliveryResType>(PowerDelivery(ChargeProgress.Stop), ct);

            // ── STOP ───────────────────────────────────────────────────────────
            if (mode == PowerMode.Dc)
                await Send<WeldingDetectionResType>(new WeldingDetectionReqType(EvStatus()), ct);
            await Send<SessionStopResType>(new SessionStopReqType(StopMode), ct);
        }

        /// <summary>Polls ChargeParameterDiscovery until Finished, then evaluates the SASchedule offer:
        /// verify signed SalesTariffs, pick the cheapest tuple, derive the ChargingProfile. Runs again
        /// after a renegotiation (the offer may have changed).</summary>
        private async Task RunChargeParameterDiscovery(CancellationToken ct)
        {
            ChargeParameterDiscoveryResType cpd;
            var guard = new OngoingGuard(clock, OngoingTimeout, "ChargeParameterDiscovery");
            while ((cpd = await Send<ChargeParameterDiscoveryResType>(ChargeParameterDiscovery(), ct))
                       .EVSEProcessing != EVSEProcessing.Finished)
            {
                guard.Tick();
                await pollDelay.Wait(PollInterval, ct);
            }
            EvaluateSchedules(cpd);
        }

        /// <summary>DC only: the isolation sequence between <c>ChargeParameterDiscovery</c> and
        /// <c>PowerDelivery</c> — <c>CableCheckReq</c> polled to <c>Finished</c>, then one
        /// <c>PreChargeReq</c>.</summary>
        /// <remarks>
        /// <para>
        /// A method rather than two inline blocks since 2026-08-15, because it is needed **twice**: on the
        /// way in, and again after a renegotiation. ISO 15118-2's SECC state table for DC has exactly one
        /// successor to <c>Process ChargeParameterDiscoveryReq</c> — *Wait for CableCheckReq*,
        /// `[V2G2-565]` and `[V2G2-582]` — and no renegotiation exception, so the return path is the same
        /// path.
        /// </para>
        /// <para>
        /// <b>This was filed against somebody else first.</b> EVerest's <c>EvseV2G</c> answered our
        /// short renegotiation <c>FAILED_SequenceError</c> on 2026-08-11 and that was written up as their
        /// defect; working the filing's own document gate four days later refuted it. Their station was
        /// right, our car was not, and our own station accepted the short sequence too — see
        /// <c>Secc2.RenegotiationNeedsIsolationSequence</c>, added with this.
        /// </para>
        /// </remarks>
        private async Task RunDcIsolationSequence(CancellationToken ct)
        {

            _inIsolationSequence = true;
            try
            {

                var cableGuard = new OngoingGuard(clock, OngoingTimeout, "CableCheck");

                while ((await Send<CableCheckResType>(new CableCheckReqType(EvStatus()), ct))
                           .EVSEProcessing != EVSEProcessing.Finished)
                {
                    cableGuard.Tick();
                    await pollDelay.Wait(PollInterval, ct);
                }

                await Send<PreChargeResType>(new PreChargeReqType(EvStatus(),
                    EVTargetVoltage: Volt(400), EVTargetCurrent: Amp(2)), ct);

            }
            finally
            {
                _inIsolationSequence = false;
            }

        }

        /// <summary>The EV-side smart-charging step over a finished ChargeParameterDiscoveryRes:
        /// (1) if the offer's SalesTariffs are signed (§7.9.2.5), check each tariff's reference digest
        /// against its re-encoded EXI fragment and (with <see cref="TariffVerifyKey"/>) the ECDSA signature
        /// — dual-grammar, like every other signature in this interop; (2) choose the tuple with the lowest
        /// average EPriceLevel; (3) shape the ChargingProfile to the chosen tuple's PMaxSchedule (entry for
        /// entry, capped at <see cref="ProfileCapW"/> — this simulated EV draws PMax unless <c>--power</c>
        /// asked for less, which is the "weaker EV capping at its own limit" this used to only describe).</summary>
        private void EvaluateSchedules(ChargeParameterDiscoveryResType cpd)
        {

            // The envelope this station announced, before a single charge-loop message exists to revise
            // it. On AC there is nothing to read: AC_EVSEChargeParameter carries EVSEMaxCurrent per phase
            // and this ceiling is a DC one.
            if (cpd.EVSEChargeParameter is DC_EVSEChargeParameterType dcEvse)
                ReadEvseLimits(dcEvse.EVSEMaximumCurrentLimit, dcEvse.EVSEMaximumPowerLimit);

            if (cpd.SASchedules is not SAScheduleListType offer || offer.SAScheduleTuple.Count == 0)
            {
                Tariff = null;   // no offer (EVSEProcessing games aside, [V2G2-905] makes this a SECC bug)
                _chosenTupleId = 1;
                _chargingProfile = null;
                return;
            }

            // (1) tariff signature: one header signature, one reference per SalesTariff Id.
            // Moved into Iso2TariffCheck 2026-08-17. It lived here, and the only way to exercise it was a
            // whole session against a station configured to sign — which no recorded trace is, because the
            // verdict never reaches the wire. Out here, TariffSignatureCorpusTests can hold this side to
            // the same corpus the Swift and Kotlin ports are held to.
            var (signaturePresent, digestOk, signatureOk, grammar) =
                Iso2TariffCheck.Evaluate(offer, _lastHeader?.Signature, TariffVerifyKey);

            // (2) cheapest tuple: lowest average EPriceLevel; tariff-less tuples rank last, ties keep offer order.
            var chosen = offer.SAScheduleTuple.OrderBy(AveragePriceLevel).First();
            _chosenTupleId = chosen.SAScheduleTupleID;

            // (3) the ChargingProfile follows the chosen tuple's PMaxSchedule step for step — capped, when
            //     --power asked for less, at what this car will actually draw. That cap is the whole of
            //     what --power means on a -2 AC wire: the profile is the only power either side ever sees,
            //     and both counters read it back (CommittedPowerW here, _acCommittedPowerW at the station).
            //     It only ever lowers an entry, so a profile that was within PMax ([V2G2-761]) stays within it.
            //     DC caps too, though nothing there reads it back: a profile claiming PMax beside a
            //     CurrentDemand asking for a quarter of it would be the EV contradicting itself on the wire.
            var cap = ProfileCapW;
            _chargingProfile = new ChargingProfileType(chosen.PMaxSchedule.PMaxScheduleEntry
                .Select(p => new ProfileEntryType(
                    ChargingProfileEntryStart: p.TimeInterval is RelativeTimeIntervalType rti ? rti.Start : 0,
                    ChargingProfileEntryMaxPower: cap is { } watts && (double) p.PMax.ToDecimal() > watts
                                                      ? PhysicalValue.Of((decimal) watts, UnitSymbol.W)
                                                      : p.PMax,
                    ChargingProfileEntryMaxNumberOfPhasesInUse: null))
                .ToArray());

            Tariff = new Iso2TariffResult(offer.SAScheduleTuple.Count, signaturePresent, digestOk,
                                          signatureOk, grammar, _chosenTupleId,
                                          _chargingProfile.ProfileEntry.Count);
        }

        private static double AveragePriceLevel(SAScheduleTupleType tuple) =>
            tuple.SalesTariff is { SalesTariffEntry.Count: > 0 } tariff
                ? tariff.SalesTariffEntry.Average(e => (double?)e.EPriceLevel ?? byte.MaxValue)
                : double.MaxValue;

        /// <summary>
        /// Runs the -2 contract-provisioning exchange (§7.9.2.4): sends the signed request — the OEM
        /// provisioning certificate for an installation, the expiring contract for an update, signed over
        /// its own message fragment in the Josev interop form — then verifies the response's four-reference
        /// signature against the provisioning certificate the station sent and ECDH-unwraps the issued
        /// contract private key.
        /// </summary>
        private async Task RunCertificateProvisioningAsync(Iso2CertInstallOptions options, CancellationToken ct)
        {

            // -2 has the EV name the roots it trusts, so the operator can pick a chain the car can build.
            // Ours names the one dev root this stack uses; a real car lists what it was built with.
            var roots = new ListOfRootCertificateIDsType(new[] { new X509IssuerSerialType("CN=V2GRootCA (dev)", 1) });

            var fragment = new byte[4096];
            SignatureType signature;
            BodyBaseType request;

            if (options.Action == Iso2CertificateAction.Update)
            {
                var update = new CertificateUpdateReqType("id1",
                    new CertificateChainType(Id: null, options.Certificate,
                        options.SubCertificates is { Count: > 0 } subs ? new SubCertificatesType(subs.ToArray()) : null),
                    options.Emaid ?? throw new SessionAborted("CertificateUpdateReq: the eMAID of the expiring contract is required."),
                    roots);

                if (!Iso2Codec.EncodeFragment_CertificateUpdateReq(update, fragment, out int n))
                    throw new InvalidOperationException("CertificateUpdateReq fragment encode failed.");
                signature = XmlDsigInterop2.Sign("id1", fragment.AsSpan(0, n), options.SignKey);
                request = update;
            }
            else
            {
                var install = new CertificateInstallationReqType("id1", options.Certificate, roots);

                if (!Iso2Codec.EncodeFragment_CertificateInstallationReq(install, fragment, out int n))
                    throw new InvalidOperationException("CertificateInstallationReq fragment encode failed.");
                signature = XmlDsigInterop2.Sign("id1", fragment.AsSpan(0, n), options.SignKey);
                request = install;
            }

            // The two responses carry the same six fields in the same order, bar the update's trailing
            // RetryCounter, so everything after this point is common.
            var (code, provisioningChain, contractChain, encryptedKey, dhPublicKey, emaid) =
                options.Action == Iso2CertificateAction.Update
                    ? Unpack(await Send<CertificateUpdateResType>(request, ct, signature))
                    : Unpack(await Send<CertificateInstallationResType>(request, ct, signature));

            if (code >= ResponseCode.FAILED)
                throw new SessionAborted($"contract provisioning refused: {code}.");

            InstalledContractSignatureOk = VerifyProvisioningSignature(provisioningChain, contractChain,
                                                                       encryptedKey, dhPublicKey, emaid);

            InstalledContractCertificate = contractChain.Certificate;
            InstalledEmaid               = emaid.Value;
            InstalledContractKey         = ContractProvisioning.RecoverContractKey(
                                               options.KeyAgreement, dhPublicKey.Value, encryptedKey.Value);

            // CBC authenticates nothing, so an unwrap always "succeeds". The check that it succeeded with
            // the right key is that the key belongs to the certificate it arrived with — without this a
            // car would carry on and only find out at its next AuthorizationReq, one session later.
            using var issued = X509CertificateLoader.LoadCertificate(contractChain.Certificate);
            using var issuedPublicKey = issued.GetECDsaPublicKey();
            if (issuedPublicKey is null || !ContractProvisioning.Matches(InstalledContractKey, issuedPublicKey))
            {
                InstalledContractKey.Dispose();
                InstalledContractKey = null;
                throw new SessionAborted("contract provisioning: the unwrapped key does not belong to the issued certificate.");
            }

            static (ResponseCode, CertificateChainType, CertificateChainType,
                    ContractSignatureEncryptedPrivateKeyType, DiffieHellmanPublickeyType, EMAIDType) Unpack(BodyBaseType body)
                => body switch
                {
                    CertificateInstallationResType r => (r.ResponseCode, r.SAProvisioningCertificateChain,
                                                         r.ContractSignatureCertChain, r.ContractSignatureEncryptedPrivateKey,
                                                         r.DHpublickey, r.EMAID),
                    CertificateUpdateResType r       => (r.ResponseCode, r.SAProvisioningCertificateChain,
                                                         r.ContractSignatureCertChain, r.ContractSignatureEncryptedPrivateKey,
                                                         r.DHpublickey, r.EMAID),
                    _ => throw new SessionAborted($"contract provisioning: unexpected response {body.GetType().Name}."),
                };

        }

        /// <summary>
        /// Checks the response's header signature: four references — the contract chain, the encrypted key,
        /// the DH public key and the eMAID — each digested over its own EXI fragment, then the ECDSA
        /// signature against the provisioning certificate the station sent alongside.
        /// </summary>
        /// <remarks>
        /// Four references where every other signed -2 message has one, and all four have to hold: a car
        /// that checked only the chain would take an encrypted key nobody signed for.
        /// </remarks>
        private bool VerifyProvisioningSignature(CertificateChainType provisioningChain,
                                                 CertificateChainType contractChain,
                                                 ContractSignatureEncryptedPrivateKeyType encryptedKey,
                                                 DiffieHellmanPublickeyType dhPublicKey,
                                                 EMAIDType emaid)
        {

            if (_lastHeader?.Signature is not { } sig || sig.SignedInfo.Reference.Count != 4)
                return false;

            var buf = new byte[4096];
            if (!Matches(contractChain.Id, Iso2Codec.EncodeFragment_ContractSignatureCertChain(contractChain, buf, out int n1), buf, n1) ||
                !Matches(encryptedKey.Id, Iso2Codec.EncodeFragment_ContractSignatureEncryptedPrivateKey(encryptedKey, buf, out int n2), buf, n2) ||
                !Matches(dhPublicKey.Id, Iso2Codec.EncodeFragment_DHpublickey(dhPublicKey, buf, out int n3), buf, n3) ||
                !Matches(emaid.Id, Iso2Codec.EncodeFragment_eMAID(emaid, buf, out int n4), buf, n4))
                return false;

            try
            {
                using var provisioningLeaf = X509CertificateLoader.LoadCertificate(provisioningChain.Certificate);
                using var verifyKey = provisioningLeaf.GetECDsaPublicKey();
                if (verifyKey is null)
                    return false;

                return V2GSignature.Verify(sig.SignedInfo, sig.SignatureValue.Value, verifyKey)
                    || XmlDsigInterop2.VerifyStandaloneXmldsig(sig.SignedInfo, sig.SignatureValue.Value, verifyKey);
            }
            catch (CryptographicException)
            {
                return false;
            }

            bool Matches(string? id, bool encoded, byte[] buffer, int length)
            {
                if (!encoded || id is null)
                    return false;
                var reference = sig.SignedInfo.Reference.FirstOrDefault(r => r.URI == "#" + id);
                return reference is not null && V2GSignature.VerifyReference(reference, buffer.AsSpan(0, length));
            }

        }

        /// <summary>Signs and sends one MeteringReceiptReq for the SECC's MeterInfo, in the Josev form.</summary>
        private async Task SendMeteringReceipt(MeterInfoType meterInfo, byte? saScheduleTupleId, CancellationToken ct)
        {
            var receipt = new MeteringReceiptReqType("id2", _sid, saScheduleTupleId, meterInfo);
            var fragment = new byte[1024];
            if (!Iso2Codec.EncodeFragment_MeteringReceiptReq(receipt, fragment, out int n))
                throw new InvalidOperationException("MeteringReceiptReq fragment encode failed.");
            var signature = XmlDsigInterop2.Sign("id2", fragment.AsSpan(0, n), Pnc!.ContractKey);

            await Send<MeteringReceiptResType>(receipt, ct, signature);
            MeteringReceiptsSent++;
        }

        /// <summary>
        /// The eMAID for PaymentDetails — the contract certificate's CN (e.g. <c>UKSWI123456791A</c>),
        /// checked against the one rule the schema states.
        /// </summary>
        /// <remarks>
        /// <para>
        /// ISO 15118-2 constrains <c>eMAIDType</c> to <b>14 or 15 characters</b>
        /// (<c>V2G_CI_MsgDataTypes.xsd</c>: country code, provider id, instance, optional check
        /// digit). A CN outside that range cannot be sent as an eMAID at all.
        /// </para>
        /// <para>
        /// This check is here because it was missing: a corpus certificate with a 19-character CN
        /// travelled in a recorded PnC session and nothing objected, in any of the three back ends.
        /// The generated codec does not enforce string-length facets — reasonably, since an EXI
        /// encoder assumes schema-valid input — which means nothing else will catch this either.
        /// </para>
        /// <para>
        /// It is a <b>-2</b> rule, not a certificate-profile rule: ISO 15118-20 never sends the eMAID
        /// from the certificate, so the same credential can be perfectly usable there. Hence the
        /// check lives on this path and not on <see cref="PncEvccOptions"/>.
        /// </para>
        /// </remarks>
        private string ContractEmaid()
        {
            using var contract = X509CertificateLoader.LoadCertificate(Pnc!.ContractCertificate);
            var commonName = contract.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

            if (commonName.Length is < 14 or > 15)
                throw new SessionAborted(
                    $"the contract certificate's Common Name \"{commonName}\" is {commonName.Length} " +
                     "characters; ISO 15118-2 allows an eMAID of 14 or 15, so this credential cannot " +
                     "authorize a -2 Plug & Charge session.");

            return commonName;
        }

        private async Task<T> Send<T>(BodyBaseType requestBody, CancellationToken ct, SignatureType? signature = null) where T : BodyBaseType
        {
            // Opt-in, and the default (null) is the id the station gave us — so every recorded session and
            // every vector keeps the bytes it was recorded with, the shape of Battery and
            // TransportSecurity.Unknown. SessionSetupReq is deliberately exempt: that is the one message
            // [V2G2-460] excludes, where the id means "new" or "resume" rather than "mine", and overriding
            // it there would open a different session instead of testing this one.
            var sid = SendSessionId is not null && requestBody is not SessionSetupReqType
                          ? SendSessionId
                          : _sid;

            var header = new MessageHeaderType(sid, Notification: null, Signature: signature);
            var request = new V2G_Message(header, new BodyType(requestBody));
            if (!request.TryEncode(_buf, out int reqLen))
                throw new InvalidOperationException("EXI encode failed (buffer too small?).");

            var start = clock.GetUtcNow();
            await V2GTPStream.WriteFrameAsync(stream, MessageSet.Iso15118_2, _buf.AsMemory(0, reqLen), ct).ConfigureAwait(false);
            var (set, message) = await V2GTPStream.ReadFrameAsync(stream, ct).ConfigureAwait(false);
            var elapsed = clock.GetUtcNow() - start;

            if (elapsed > perMessageTimeout)
                throw new SessionAborted(
                    $"{typeof(T).Name.Replace("ResType", "")}: no response within {perMessageTimeout.TotalMilliseconds:0} ms " +
                    $"(took {elapsed.TotalMilliseconds:0} ms).");
            if (set != MessageSet.Iso15118_2 || message is not V2G_Message reply)
                throw new SessionAborted($"expected an ISO 15118-2 reply, got {set}.");

            RefuseOnFailure(reply.Body.BodyElement!);

            Exchanges++;
            BytesOnWire += V2GTPCodec.HeaderSize + reqLen; // request side; response side is the peer's own accounting

            _sid = reply.Header.SessionID;             // adopt the SECC-assigned session id
            _lastHeader = reply.Header;                // tariff verification reads the response signature
            return (T)reply.Body.BodyElement!;
        }


        /// <summary>
        /// Ends the session when the station answers with a code from the <c>FAILED</c> family.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The -2 half of the gap a live peer found in the -20 EVCC on 2026-08-01: nothing here read a
        /// response code either, beyond recording <see cref="SessionSetupCode"/> for the caller's
        /// information. A station could answer <c>FAILED</c> to every message and this car would charge
        /// through it (<c>ISO15118ConformanceTests/docs/interop-runs/2026-08-01-edf-iso20-dc-notls/</c>, finding 3).
        /// </para>
        /// <para>
        /// <b>Why reflection rather than a switch.</b> ISO 15118-2 has no common response base — every
        /// <c>*ResType</c> declares its own <c>ResponseCode</c> field, so there is nothing to pattern-match
        /// on. A hand-written switch over the response types would work and would be *fail-open*: the one
        /// forgotten in the list, or the one added later, is silently unchecked, which is precisely the
        /// failure this method exists to end. Reading the property by name covers every response type
        /// there is and every one there will be, and
        /// <c>Evcc2FailureHandlingTests.EveryResponseTypeIsCheckable</c> enumerates the generated assembly
        /// to prove that "every" is not an assumption.
        /// </para>
        /// <para>
        /// <b>Only two families here.</b> Unlike -20, -2 has no <c>WARNING</c> codes: the enumeration is
        /// four <c>OK*</c> values and then <c>FAILED</c> onwards. Same range test, same reason, and the
        /// same test pins the ordering.
        /// </para>
        /// </remarks>
        private static void RefuseOnFailure(BodyBaseType body)
        {

            var code = ResponseCodeOf(body);

            if (code >= ResponseCode.FAILED)
                throw new SessionAborted(
                    $"the station answered {body.GetType().Name} with {code}; the session ends here.");

        }


        /// <summary>The response code of a -2 body element, or <c>null</c> for a request (which is what an
        /// EVCC never receives) or for the handful of bodies that carry none.</summary>
        internal static ResponseCode? ResponseCodeOf(BodyBaseType body)
            => ResponseCodeReaders.GetOrAdd(body.GetType(),
                   static type => type.GetProperty(nameof(ResponseCode)) is { } property &&
                                  property.PropertyType == typeof(ResponseCode)
                                      ? property
                                      : null)
                                  ?.GetValue(body) as ResponseCode?;

        private static readonly ConcurrentDictionary<Type, PropertyInfo?> ResponseCodeReaders = new();

        // ── request builders ──────────────────────────────────────────────────
        /// <summary>Start carries the smart-charging outcome: the chosen tuple id and the PMax-shaped
        /// ChargingProfile; Renegotiate/Stop reference the tuple without a profile.</summary>
        private PowerDeliveryReqType PowerDelivery(ChargeProgress progress) =>
            new(progress, SAScheduleTupleID: _chosenTupleId,
                ChargingProfile: progress == ChargeProgress.Start ? _chargingProfile : null,
                EVPowerDeliveryParameter: null);

        /// <summary>
        /// The energy transfer mode to request, chosen from the ones the station advertised in
        /// <c>ServiceDiscoveryRes</c>'s ChargeService rather than assumed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This used to be hard-coded — <c>DC_extended</c> for DC, <c>AC_three_phase_core</c> for AC — and it
        /// worked against every station this project had met, because every one of them offered exactly what
        /// we happened to name. EVerest's AC SIL configuration does not: it advertises a single-phase mode,
        /// answers our three-phase request with <c>FAILED_WrongEnergyTransferMode</c>, and is right to
        /// (<c>ISO15118ConformanceTests/docs/interop-runs/2026-08-03-everest-iso2-ac/</c>).
        /// </para>
        /// <para>
        /// The list is there to be read; picking without looking is the same mistake as the unread response
        /// code, one message earlier. Preference within our own power mode is best-first — three-phase over
        /// single-phase, extended over core — and a station that offers nothing in our mode is refused by
        /// name rather than asked for something it just said it cannot do.
        /// </para>
        /// </remarks>
        private EnergyTransferMode SelectEnergyTransferMode(ServiceDiscoveryResType discovery)
        {

            var offered = discovery.ChargeService?.SupportedEnergyTransferMode?.EnergyTransferMode
                              ?? Array.Empty<EnergyTransferMode>();

            var preferred = mode == PowerMode.Dc
                                ? new[] { EnergyTransferMode.DC_extended, EnergyTransferMode.DC_core,
                                          EnergyTransferMode.DC_combo_core, EnergyTransferMode.DC_unique }
                                : new[] { EnergyTransferMode.AC_three_phase_core,
                                          EnergyTransferMode.AC_single_phase_core };

            foreach (var candidate in preferred)
                if (offered.Contains(candidate))
                    return candidate;

            // Nothing in our power mode. Say what was offered: it is the one line that turns "the station
            // refused" into "the station is a DC charger and we are an AC car".
            throw new SessionAborted(
                $"ServiceDiscovery: the station offers no {(mode == PowerMode.Dc ? "DC" : "AC")} energy "
              + $"transfer mode (offered: {(offered.Count == 0 ? "none" : String.Join(", ", offered))}).");

        }

        // ── what --power means on a -2 wire ───────────────────────────────────
        // -2 has no field anywhere that says "watts I want", so the ask has to be expressed as the current
        // and the profile the protocol does carry. Each mode below states where, and every one of them
        // falls back to the literal it had before batteries existed when no battery names a power — the
        // recorded sessions in ISO15118ConformanceTests are taken without one and must not move.

        /// <summary>The line-to-line voltage this EV declares for AC, and the per-phase voltage derived
        /// from it — one constant rather than a 400 here and a 230 there, because a single phase of a
        /// 400 V system <em>is</em> 400/√3 and saying so is better than restating it.</summary>
        private const  short  AcLineVolts  = 400;
        private static readonly double AcPhaseVolts = AcLineVolts / Math.Sqrt(3);   // ≈ 230.9 V

        /// <summary>The current band this EV's AC contactor can switch, per phase. Hardware: declared at
        /// discovery, and not lowered because the driver asked for less today.</summary>
        private const short AcMaxAmps = 32;
        private const short AcMinAmps =  6;

        /// <summary>The voltage the DC loop operates at — reported as <c>EVTargetVoltage</c>, and the one
        /// the current requests below are derived at so a power and a current describe one operating
        /// point.</summary>
        private const short DcLoopVolts = 400;
        private const short DcMaxAmps   = 200;

        /// <summary>How many phases the selected energy transfer mode draws through.</summary>
        private int AcPhases => _energyTransferMode == EnergyTransferMode.AC_single_phase_core ? 1 : 3;

        /// <summary>
        /// The power this EV will actually draw on AC, in watts — <c>--power</c> held inside the current
        /// band it just declared. Null when no battery named a power.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The clamp is the hardware, not caution: a contactor that cannot modulate below 6 A per phase
        /// charges at 6 A whatever the driver typed, and one that tops out at 32 A takes 32. So
        /// <c>--power 1</c> on three-phase AC charges at 4.2 kW — and says 4.2 kW in both the current and
        /// the profile, rather than putting a profile on the wire that the current beside it contradicts.
        /// </para>
        /// <para>
        /// <b>No taper here, and it cannot be one.</b> The profile is agreed once, at
        /// <c>PowerDeliveryReq(Start)</c>, and -2's AC charge loop (<c>ChargingStatusReq</c>) is an empty
        /// message: there is no per-iteration field for the vehicle to lower as the pack fills. Both meters
        /// read the committed profile, so a taper applied on this side alone would only make them disagree.
        /// -20 AC has <c>EVPresentActivePower</c> every iteration and does taper; that difference between
        /// the two protocols is real and this is where it shows.
        /// </para>
        /// </remarks>
        private double? AcRequestedPowerW
            => Battery is { RequestedPowerW: > 0 } b
                   ? Math.Round(Math.Clamp(b.RequestedPowerW,
                                           AcPhases * AcPhaseVolts * AcMinAmps,
                                           AcPhases * AcPhaseVolts * AcMaxAmps))
                   : null;

        /// <summary>The per-phase current the AC request asks for, derived from
        /// <see cref="AcRequestedPowerW"/> at the voltage it declared. The full 32 A when nothing asked.</summary>
        private short AcRequestedAmps
            => AcRequestedPowerW is { } watts
                   ? (short) Math.Clamp(Math.Round(watts / (AcPhases * AcPhaseVolts)), AcMinAmps, AcMaxAmps)
                   : AcMaxAmps;

        /// <summary>What <c>--power</c> asks for on DC, scaled by the battery's taper: above the knee the
        /// car asks for progressively less, which is what makes the last fifth take as long as it does.
        /// Null when no battery named a power.</summary>
        private double? DcRequestedPowerW
            => Battery is { RequestedPowerW: > 0 } b ? Math.Round(b.RequestedPowerW * b.PowerFactor) : null;

        /// <summary>The DC setpoint this vehicle <em>wants</em>, at <see cref="DcLoopVolts"/> and capped by
        /// the envelope it declared it can take. 120 A when nothing asked — the figure every recorded -2 DC
        /// run carries. What actually goes on the wire is <see cref="DcTargetAmps"/>, which holds this
        /// inside what the station said it can deliver.</summary>
        private short DcRequestedAmps
            => DcRequestedPowerW is { } watts
                   ? (short) Math.Clamp(Math.Round(watts / DcLoopVolts), 1, DcMaxAmps)
                   : (short) 120;

        /// <summary>
        /// What the station last said it can deliver: seeded from the <c>DC_EVSEChargeParameter</c> of the
        /// ChargeParameterDiscoveryRes and then replaced by whatever each CurrentDemandRes carries. Null
        /// until a station has said anything, and null again for any field a station omits — both of which
        /// mean "no ceiling known from that quarter", not zero.
        /// </summary>
        private Double? _evseMaxAmps;
        private Double? _evseMaxPowerW;

        /// <summary>
        /// The DC setpoint actually put on the wire: <see cref="DcRequestedAmps"/> held inside the station's
        /// current <em>and</em> power ceiling, at the voltage this loop runs at.
        /// </summary>
        /// <remarks>
        /// <para>
        /// -2 lets the SECC restate <c>EVSEMaximumCurrentLimit</c>, <c>EVSEMaximumPowerLimit</c> and
        /// <c>EVSEMaximumVoltageLimit</c> in <b>every</b> CurrentDemandRes, and a station that derates
        /// mid-session — thermal, grid, a second outlet starting — does exactly that. Until 2026-08-10 this
        /// car read none of it: it computed a setpoint once and re-sent it, and a live EVerest station
        /// dropped its limit from 200 A to 55.2 A while our EVCC went on asking for 120 A, three times out
        /// of three. Their `EvseManager` clamped and warned each time — 47 such warnings across the
        /// recorded runs before anyone read one.
        /// </para>
        /// <para>
        /// <b>Floor, not round</b>: a car that rounds up asks for more than it was allowed. And the limit
        /// applies from the response that carried it onwards — the first request of a session is sent
        /// before any CurrentDemandRes exists, which is why the discovery values are read too rather than
        /// waiting for the loop to teach it.
        /// </para>
        /// </remarks>
        private short DcTargetAmps
        {
            get
            {

                var amps = (Double) DcRequestedAmps;

                if (_evseMaxAmps   is { } maxAmps)
                    amps = Math.Min(amps, maxAmps);

                if (_evseMaxPowerW is { } maxWatts)
                    amps = Math.Min(amps, maxWatts / DcLoopVolts);

                return (short) Math.Max(0, Math.Floor(amps));

            }
        }

        /// <summary>Read the station's limits off whatever message carried them. Called for the
        /// ChargeParameterDiscoveryRes (through <see cref="EvaluateSchedules"/>) and for every
        /// CurrentDemandRes; each field is replaced only when the message actually carries one, so a
        /// station that states its ceiling once and then omits it keeps that ceiling.</summary>
        private void ReadEvseLimits(PhysicalValueType? maxCurrent, PhysicalValueType? maxPower)
        {

            if (maxCurrent is not null)
                _evseMaxAmps   = (Double) maxCurrent.ToDecimal();

            if (maxPower   is not null)
                _evseMaxPowerW = (Double) maxPower.ToDecimal();

        }

        /// <summary>
        /// The ceiling to shape the ChargingProfile to: the untapered ask, held inside what this vehicle
        /// can take in the mode it is charging in. Null when nothing asked, which leaves the profile
        /// following PMax exactly as it always did.
        /// </summary>
        /// <remarks>Untapered because the profile is agreed once and then stands for the whole session,
        /// while the taper is a function of a state of charge that keeps moving. A commitment cannot track
        /// it; the DC setpoint, re-sent every iteration, can and does.</remarks>
        private double? ProfileCapW
            => mode == PowerMode.Ac
                   ? AcRequestedPowerW
                   : Battery is { RequestedPowerW: > 0 } b
                         ? Math.Round(Math.Min(b.RequestedPowerW, (double) DcLoopVolts * DcMaxAmps))
                         : null;

        /// <summary>
        /// The discovery request. The envelope stated here is what the vehicle can take <em>at all</em> —
        /// on DC it stays hardware, the same split <see cref="Iso20.Evcc20Dc"/> makes. AC is the exception,
        /// and not by choice: <c>EVMaxCurrent</c> is the only current an AC EV ever sends, so it has to
        /// carry both the capability and the ask, and a station shaping its PMax schedule has nothing else
        /// to read.
        /// </summary>
        private ChargeParameterDiscoveryReqType ChargeParameterDiscovery() =>
            mode == PowerMode.Dc
                ? new ChargeParameterDiscoveryReqType(MaxEntriesSAScheduleTuple: null, _energyTransferMode,
                    new DC_EVChargeParameterType(DepartureTime: null, EvStatus(),
                        EVMaximumCurrentLimit: Amp(DcMaxAmps), EVMaximumPowerLimit: null, EVMaximumVoltageLimit: Volt(500),
                        // Both optional and both absent until there was a pack to describe. -2 DC is the
                        // one place a car states its capacity outright, which is what a station needs to
                        // turn "40 kWh wanted" into a schedule rather than a number.
                        EVEnergyCapacity: Battery is { } dc ? WattHours(dc.CapacityWh)     : null,
                        EVEnergyRequest:  Battery is { } dr ? WattHours(dr.EnergyNeededWh) : null,
                        FullSOC: 100, BulkSOC: 80))
                : new ChargeParameterDiscoveryReqType(MaxEntriesSAScheduleTuple: null, _energyTransferMode,
                    new AC_EVChargeParameterType(DepartureTime: null,
                        // EAmount is -2 AC's only energy field, and it is the request: how much this
                        // session wants, not what the pack holds. 22 kWh when nothing asked — less what a
                        // paused predecessor already charged, which is [V2G2-743] and is why the fallback
                        // is no longer a constant. With a pack there is nothing to subtract: charging moved
                        // its state of charge, so EnergyNeededWh is already the remainder.
                        EAmount: Battery is { } ac ? WattHours(ac.EnergyNeededWh)
                                                   : WattHours(Math.Max(0, 22_000 - AlreadyChargedWh)),
                        EVMaxVoltage: Volt(AcLineVolts),
                        EVMaxCurrent: Amp(AcRequestedAmps), EVMinCurrent: Amp(AcMinAmps)));

        /// <summary>Watt-hours as a -2 physical value, rounded to the whole watt-hour the wire and the
        /// meter both count in.</summary>
        private static PhysicalValueType WattHours(double wattHours)
            => PhysicalValue.Of((decimal) Math.Round(wattHours), UnitSymbol.Wh);

        /// <summary>
        /// One DC charge-loop request. <c>--power</c> lands here: <c>EVTargetCurrent</c> is the setpoint
        /// this car asks for at its own voltage, and the optional <c>EVMaximumPowerLimit</c> — absent until
        /// something had a figure to put in it — states the same ask as a power outright, which is the
        /// nearest -2 comes to a watts field.
        /// </summary>
        private CurrentDemandReqType CurrentDemand()
        {

            var amps = DcTargetAmps;

            return new(EvStatus(), EVTargetCurrent: Amp(amps),
                EVMaximumVoltageLimit: null, EVMaximumCurrentLimit: null,
                // The power is the same operating point stated in watts, so the two fields cannot
                // contradict each other once the station's ceiling has moved the current.
                EVMaximumPowerLimit: DcRequestedPowerW is not null
                                         ? PhysicalValue.Of(amps * (decimal) DcLoopVolts, UnitSymbol.W)
                                         : null,
                BulkChargingComplete: null, ChargingComplete: false,
                RemainingTimeToFullSoC: null, RemainingTimeToBulkSoC: null,
                EVTargetVoltage: Volt(DcLoopVolts));

        }

        /// <summary>
        /// The power this EV committed to in its ChargingProfile — its own view of an AC session,
        /// since -2 puts no power on the wire in either direction.
        /// </summary>
        /// <remarks>
        /// The first entry, because the profile's later entries start at offsets a three-iteration
        /// charge loop never reaches. Zero when there is no profile: an AC session that never agreed
        /// one has no committed power to count, and inventing one would put a number on screen that
        /// nothing in the session supports.
        /// </remarks>
        private double CommittedPowerW() =>
            _chargingProfile is { ProfileEntry.Count: > 0 } profile
                ? (double) profile.ProfileEntry[0].ChargingProfileEntryMaxPower.ToDecimal()
                : 0;

        /// <summary>
        /// The DC status this car repeats in every request of the DC sequence — and, with a pack, the one
        /// field in -2 that <em>moves</em> during a session: <c>EVRESSSOC</c> is the present state of
        /// charge, so a station watching it sees the battery fill.
        /// </summary>
        /// <remarks>
        /// A flat 50 % until there were packs, which is what a car without one still sends. Worth naming
        /// because it is the only per-iteration reading -2 asks the vehicle for: -2 gives a DC car no field
        /// for a measured power, so this percentage is the whole of what the station learns about the
        /// vehicle's own state while charging.
        /// </remarks>
        private DC_EVStatusType EvStatus()
            => new(EVReady: !(_inIsolationSequence && IsolationDeclaresNotReady), DC_EVErrorCode.NO_ERROR,
                   EVRESSSOC: Battery is { } b ? (sbyte) Math.Clamp(Math.Round(b.SoC), 0, 100) : (sbyte) 50);
        private static PhysicalValueType Volt(short v) => new(0, UnitSymbol.V, v);
        private static PhysicalValueType Amp(short a)  => new(0, UnitSymbol.A, a);
    }
}
