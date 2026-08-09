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

        // 8 KiB: a PaymentDetailsReq carries the full 3-cert contract chain (~2 KiB).
        private readonly byte[] _buf = new byte[8192];
        private byte[] _sid = new byte[8];   // 0 until SessionSetupRes assigns one

        public int Exchanges { get; private set; }
        public long BytesOnWire { get; private set; }

        /// <summary>Contract credentials (same shape as the -20 EVCC's); <c>null</c> (default) pays via EIM.</summary>
        public PncEvccOptions? Pnc { get; set; }

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
        /// cycle (<c>PowerDeliveryReq(Renegotiate)</c> → new ChargeParameterDiscovery → PowerDelivery(Start)).
        /// Independent of that, the EV always reacts to a SECC-side <c>EVSENotification.ReNegotiation</c>.</summary>
        public bool Renegotiate { get; set; }

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
            await Send<PaymentServiceSelectionResType>(new PaymentServiceSelectionReqType(
                contract ? PaymentOption.Contract : PaymentOption.ExternalPayment,
                new SelectedServiceListType(new[] { new SelectedServiceType(chargeServiceId, ParameterSetID: null) })), ct);

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
                    await Send<PowerDeliveryResType>(PowerDelivery(ChargeProgress.Start), ct);
                }

                if (Battery is not null)
                {
                    Battery.Add(Meter.Energy - energyBefore);
                    if (Battery.Stop is var stop && stop != Simulation.ChargeStop.Running)
                        BatteryStop = stop;
                }

                await pollDelay.Wait(PollInterval, ct);
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

        /// <summary>The EV-side smart-charging step over a finished ChargeParameterDiscoveryRes:
        /// (1) if the offer's SalesTariffs are signed (§7.9.2.5), check each tariff's reference digest
        /// against its re-encoded EXI fragment and (with <see cref="TariffVerifyKey"/>) the ECDSA signature
        /// — dual-grammar, like every other signature in this interop; (2) choose the tuple with the lowest
        /// average EPriceLevel; (3) shape the ChargingProfile to the chosen tuple's PMaxSchedule (entry for
        /// entry, capped at <see cref="ProfileCapW"/> — this simulated EV draws PMax unless <c>--power</c>
        /// asked for less, which is the "weaker EV capping at its own limit" this used to only describe).</summary>
        private void EvaluateSchedules(ChargeParameterDiscoveryResType cpd)
        {
            if (cpd.SASchedules is not SAScheduleListType offer || offer.SAScheduleTuple.Count == 0)
            {
                Tariff = null;   // no offer (EVSEProcessing games aside, [V2G2-905] makes this a SECC bug)
                _chosenTupleId = 1;
                _chargingProfile = null;
                return;
            }

            // (1) tariff signature: one header signature, one reference per SalesTariff Id.
            var signedTariffs = offer.SAScheduleTuple
                .Where(t => t.SalesTariff?.Id is not null)
                .Select(t => t.SalesTariff!)
                .ToList();
            bool signaturePresent = _lastHeader?.Signature is not null && signedTariffs.Count > 0;
            bool digestOk = signaturePresent;
            if (signaturePresent)
            {
                var sig = _lastHeader!.Signature!;
                var buf = new byte[2048];
                foreach (var tariff in signedTariffs)
                {
                    var reference = sig.SignedInfo.Reference.FirstOrDefault(r => r.URI == "#" + tariff.Id);
                    digestOk &= reference is not null
                        && Iso2Codec.EncodeFragment_SalesTariff(tariff, buf, out int n)
                        && V2GSignature.VerifyReference(reference, buf.AsSpan(0, n));
                }
            }
            var (signatureOk, grammar) = (false, "none");
            if (signaturePresent && TariffVerifyKey is not null)
            {
                var sig = _lastHeader!.Signature!;
                if (V2GSignature.Verify(sig.SignedInfo, sig.SignatureValue.Value, TariffVerifyKey))
                    (signatureOk, grammar) = (true, "iso2-msgdef");
                else if (XmlDsigInterop2.VerifyStandaloneXmldsig(sig.SignedInfo, sig.SignatureValue.Value, TariffVerifyKey))
                    (signatureOk, grammar) = (true, "xmldsig-standalone");
            }

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
            var header = new MessageHeaderType(_sid, Notification: null, Signature: signature);
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

        /// <summary>The DC setpoint, at <see cref="DcLoopVolts"/> and capped by the envelope this vehicle
        /// declared it can take. 120 A when nothing asked — the figure every recorded -2 DC run carries.</summary>
        private short DcRequestedAmps
            => DcRequestedPowerW is { } watts
                   ? (short) Math.Clamp(Math.Round(watts / DcLoopVolts), 1, DcMaxAmps)
                   : (short) 120;

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
                        // session wants, not what the pack holds. 22 kWh when nothing asked.
                        EAmount: Battery is { } ac ? WattHours(ac.EnergyNeededWh)
                                                   : PhysicalValue.Of(22_000, UnitSymbol.Wh),
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
        private CurrentDemandReqType CurrentDemand() =>
            new(EvStatus(), EVTargetCurrent: Amp(DcRequestedAmps),
                EVMaximumVoltageLimit: null, EVMaximumCurrentLimit: null,
                EVMaximumPowerLimit: DcRequestedPowerW is { } watts
                                         ? PhysicalValue.Of((decimal) watts, UnitSymbol.W)
                                         : null,
                BulkChargingComplete: null, ChargingComplete: false,
                RemainingTimeToFullSoC: null, RemainingTimeToBulkSoC: null,
                EVTargetVoltage: Volt(DcLoopVolts));

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
            => new(EVReady: true, DC_EVErrorCode.NO_ERROR,
                   EVRESSSOC: Battery is { } b ? (sbyte) Math.Clamp(Math.Round(b.SoC), 0, 100) : (sbyte) 50);
        private static PhysicalValueType Volt(short v) => new(0, UnitSymbol.V, v);
        private static PhysicalValueType Amp(short a)  => new(0, UnitSymbol.A, a);
    }
}
