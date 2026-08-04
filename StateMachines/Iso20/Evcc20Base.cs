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
using Vanaheimr.V2G.Simulation.Framing;
using Vanaheimr.V2G.Simulation.Session;
using Vanaheimr.V2G.Simulation.Timing;
using cloud.charging.open.protocols.ISO15118.EXI.Dispatch;

// Each -20 message set carries its own generated ResponseCode and V2GResponseType; RefuseOnFailure
// has to see all three.
using Ac20 = cloud.charging.open.protocols.ISO15118_20.AC.Generated;
using Dc20 = cloud.charging.open.protocols.ISO15118_20.DC.Generated;

namespace Vanaheimr.V2G.Simulation.StateMachines.Iso20
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

        /// <summary>Contract credentials enabling Plug &amp; Charge; <c>null</c> (default) authorizes via EIM.</summary>
        public PncEvccOptions? Pnc { get; set; }

        /// <summary>
        /// The vehicle's own energy counter — what this EV thinks it took, kept independently of what
        /// the station reports (<c>docs/CONCEPT.md</c> §4.2/§4.3).
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

        /// <summary>The SECC's SessionSetup verdict: <c>OK_NewSessionEstablished</c>, or on a successful
        /// resume <c>OK_OldSessionJoined</c>.</summary>
        public ResponseCode SessionSetupCode { get; private set; }

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
        /// (<c>docs/interop-runs/2026-07-22-iso20-dynamic-sdp/</c>), so the mode was validated in exactly one
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

        /// <summary>When the car leaves, as a -20 <c>DepartureTime</c> (seconds from the session's time
        /// anchor). Dynamic mode only: it is the deadline the station schedules against.</summary>
        public UInt32 DepartureTime { get; set; } = 3600;

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

        public async Task RunAsync(CancellationToken ct = default)
        {
            if (ResumeSessionId is not null)
                SessionCtx.SessionId = ResumeSessionId;   // rejoin: the SessionSetupReq header carries the paused id

            var setupRes = await Exchange<SessionSetupRes>(MessageSet.Iso20CommonMessages,
                dest => new SessionSetupReq(SessionCtx.ToCommonHeader(), "EVCC01").TryEncode(dest, out int n) ? n : throw EncodeFailed(), ct);
            SessionSetupCode = setupRes.ResponseCode;

            // Adopt the SECC-assigned SessionID: every subsequent request header must carry it, not the
            // all-zero id the EVCC opens SessionSetup with (ISO 15118-20 §7.9.2.4). A live Josev interop run
            // caught this — Josev's SECC strictly rejects a mismatched session id (our loopback SECC did not).
            SessionCtx.SessionId = setupRes.Header.SessionID;

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
                dest => new ServiceDiscoveryReq(SessionCtx.ToCommonHeader(), null).TryEncode(dest, out int n) ? n : throw EncodeFailed(), ct);
            ushort serviceId = SelectEnergyTransferService(discovery);
            SelectedEnergyServiceId = serviceId;

            var detail = await Exchange<ServiceDetailRes>(MessageSet.Iso20CommonMessages,
                dest => new ServiceDetailReq(SessionCtx.ToCommonHeader(), serviceId).TryEncode(dest, out int n) ? n : throw EncodeFailed(), ct);
            ushort parameterSetId = SelectParameterSet(detail);

            await Exchange<ServiceSelectionRes>(MessageSet.Iso20CommonMessages,
                dest => new ServiceSelectionReq(SessionCtx.ToCommonHeader(),
                    new SelectedServiceType(serviceId, parameterSetId), null).TryEncode(dest, out int n) ? n : throw EncodeFailed(), ct);

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

            for (int cycle = 0; cycle < 3; cycle++)
            {
                await RunChargeLoopIterationAsync(ct);
                await pollDelay.Wait(PollInterval, ct);
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
                    PrioritizedEMAIDs: null).TryEncode(dest, out int n) ? n : throw EncodeFailed(), ct);

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
        private async Task<TRes> Exchange<TRes>(MessageSet expectedSet, Func<byte[], int> encode, CancellationToken ct)
        {
            var (set, message) = await ExchangeRaw(expectedSet, encode, ct).ConfigureAwait(false);
            if (message is not TRes reply)
                throw new SessionAborted($"expected a {typeof(TRes).Name} on {expectedSet}, got {message.GetType().Name} on {set}.");
            return reply;
        }

        /// <summary>Same as <see cref="Exchange{TRes}"/> but returns the undiscriminated <see cref="MessageSet"/>/object pair — for DC/AC-specific exchanges.</summary>
        /// <summary>A deadline for one 'Ongoing' phase, for the subclasses' own poll loops.</summary>
        protected OngoingGuard Ongoing(String phase) => new(clock, OngoingTimeout, phase);

        protected async Task<(MessageSet Set, object Message)> ExchangeRaw(MessageSet expectedSet, Func<byte[], int> encode, CancellationToken ct)
        {
            int reqLen = encode(_buf);
            var start = clock.GetUtcNow();
            await V2GTPStream.WriteFrameAsync(stream, expectedSet, _buf.AsMemory(0, reqLen), ct).ConfigureAwait(false);
            var (set, message) = await V2GTPStream.ReadFrameAsync(stream, ct).ConfigureAwait(false);
            var elapsed = clock.GetUtcNow() - start;

            if (elapsed > perMessageTimeout)
                throw new SessionAborted($"no response within {perMessageTimeout.TotalMilliseconds:0} ms (took {elapsed.TotalMilliseconds:0} ms).");

            RefuseOnFailure(message);

            Exchanges++;
            BytesOnWire += V2GTP.HeaderSize + reqLen;
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
        /// (<c>docs/interop-runs/2026-08-01-edf-iso20-dc-notls/</c>).
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
        private static readonly ushort[] DcServiceIds     = { 2, 6 };
        private static readonly ushort[] AcServiceIds     = { 1, 5 };
        private static readonly ushort[] DcDrivableIds    = { 2, 6, 8, 9 };
        private static readonly ushort[] AcDrivableIds    = { 1, 5 };

        /// <summary>Energy-transfer service ids this EVCC will accept from the SECC's catalogue, best first.
        /// Virtual so an MCS vehicle can ask for the megawatt services instead — see
        /// <see cref="Evcc20Mcs"/>.</summary>
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

            var preferred = PreferredEnergyServiceIds;
            var drivable  = DrivableEnergyServiceIds;

            // First choice, then anything else on our own message set. The old fallback was `offered[0]`,
            // which for a DC car at an AC-only station selects the AC service and then sends the next
            // request on the DC set — refused two exchanges later, for a reason that no longer names the
            // cause. Falling back *within* the message set keeps the case this is really for (a megawatt
            // truck at an ordinary DC charger) and drops the one it never was.
            var match = offered.FirstOrDefault(s => preferred.Contains(s.ServiceID))
                     ?? offered.FirstOrDefault(s => drivable.Contains(s.ServiceID));

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
        private Dynamic_SEReqControlModeType DynamicScheduleRequest()
            => new(DepartureTime:            DepartureTime,
                   MinimumSOC:               30,
                   TargetSOC:                80,
                   EVTargetEnergyRequest:    new RationalNumberType(3, 30),   // 30 kWh
                   EVMaximumEnergyRequest:   new RationalNumberType(3, 60),   // 60 kWh
                   EVMinimumEnergyRequest:   new RationalNumberType(3, 10),   // 10 kWh
                   EVMaximumV2XEnergyRequest: null,
                   EVMinimumV2XEnergyRequest: null);

        /// <summary>Verifies a signed <c>AbsolutePriceSchedule</c> in the Scheduled-mode offer, if any:
        /// reference digest over the schedule's re-encoded EXI fragment, then (with
        /// <see cref="TariffVerifyKey"/>) the ECDSA-P521/SHA-512 signature over the SignedInfo — the -20
        /// analogue of the -2 SalesTariff check. No signed schedule → <see cref="Tariff"/> stays null
        /// (Josev's SECC, for one, never signs its price schedules).</summary>
        private void VerifyPriceSchedule(ScheduleExchangeRes res)
        {
            // Scheduled mode hangs the price schedule off each schedule tuple; Dynamic mode has no tuples and
            // carries one directly on the control mode. Same verification either way.
            var priceSchedule = res.Dynamic_SEResControlMode?.AbsolutePriceSchedule is { Id: not null } dynamicPrice
                ? dynamicPrice
                : res.Scheduled_SEResControlMode?.ScheduleTuple
                    .Select(t => t.ChargingSchedule.AbsolutePriceSchedule)
                    .FirstOrDefault(p => p?.Id is not null);
            if (priceSchedule is null)
                return;

            bool signaturePresent = res.Header.Signature is not null;
            bool digestOk = false, signatureOk = false;
            if (signaturePresent)
            {
                var sig = res.Header.Signature!;
                var buf = new byte[4096];
                var reference = sig.SignedInfo.Reference.FirstOrDefault(r => r.URI == "#" + priceSchedule.Id);
                digestOk = reference is not null
                    && CommonMessagesCodec.EncodeFragment_AbsolutePriceSchedule(priceSchedule, buf, out int n)
                    && V2GSignature.VerifyReference(reference, buf.AsSpan(0, n));
                signatureOk = TariffVerifyKey is not null
                    && V2GSignature.Verify(sig.SignedInfo, sig.SignatureValue.Value, TariffVerifyKey);
            }
            Tariff = new Iso20TariffResult(signaturePresent, digestOk, signatureOk);
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
