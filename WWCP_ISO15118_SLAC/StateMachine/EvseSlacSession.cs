/*
 * Copyright (c) 2014-2025 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

using System.Threading.Channels;
using cloud.charging.open.protocols.ISO15118.SLAC.Messages;
using cloud.charging.open.protocols.ISO15118.SLAC.Transport;
using cloud.charging.open.protocols.ISO15118.SLAC.Validation;
using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

namespace cloud.charging.open.protocols.ISO15118.SLAC.StateMachine;

/// <summary>State of the EVSE-side SLAC matching session.</summary>
public enum EvseSlacState
{
    /// <summary>The PEV's CM_SLAC_PARM.REQ has been observed; session is starting.</summary>
    SlacParmReceived,
    StartAttenCharReceived,
    SoundsReceived,
    AttenCharSent,
    AttenCharRspReceived,
    /// <summary>EVSE has sent its first CM_VALIDATE.REQ and is waiting for the EV to toggle.</summary>
    ValidationInProgress,
    /// <summary>EV's toggle count matched the EVSE's observation; validation passed.</summary>
    ValidationSucceeded,
    /// <summary>Validation failed (toggle counts disagreed or rounds exhausted).</summary>
    ValidationFailed,
    SlacMatchReceived,
    Matched,
    /// <summary>The local PLC chip has been programmed with the negotiated NMK and the AVLN is up.</summary>
    AvlnReady,
    Failed
}

/// <summary>
/// EVSE-side per-PEV SLAC matching state machine. Driven by an
/// <see cref="EvseSlacListener"/> which demultiplexes incoming frames by
/// RunID and feeds each session through <see cref="EnqueueFrame"/>.
///
/// The session knows the PEV's MAC and RunID up-front (extracted from the
/// initial CM_SLAC_PARM.REQ by the listener), so it skips the wait-for-PARM
/// step and starts the protocol at CM_SLAC_PARM.CNF.
/// </summary>
public sealed class EvseSlacSession : IAsyncDisposable
{
    private readonly ISlacTransport       _transport;
    private readonly EvseSlacOptions      _options;
    private readonly Channel<DecodedFrame> _inbox =
        Channel.CreateUnbounded<DecodedFrame>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    /// <summary>RunID of this matching attempt (taken from the initial CM_SLAC_PARM.REQ).</summary>
    public RunId RunId { get; }

    /// <summary>MAC address of the PEV this session is matching with.</summary>
    public MACAddress PevMac { get; }

    public EvseSlacState State { get; private set; } = EvseSlacState.SlacParmReceived;

    public event EventHandler<EvseSlacState>? StateChanged;
    public event EventHandler<string>?         Log;

    public EvseSlacSession(
        ISlacTransport  transport,
        EvseSlacOptions options,
        RunId           runId,
        MACAddress      pevMac)
    {
        _transport = transport;
        _options   = options;
        RunId      = runId;
        PevMac     = pevMac;
    }

    /// <summary>
    /// Called by <see cref="EvseSlacListener"/> to deliver a frame that matches this
    /// session's RunID. Non-blocking.
    /// </summary>
    internal void EnqueueFrame(DecodedFrame frame)
        => _inbox.Writer.TryWrite(frame);

    /// <summary>
    /// Run the matching protocol from CM_SLAC_PARM.CNF through to CM_SLAC_MATCH.CNF.
    /// </summary>
    public async Task<SlacMatchCnf> RunAsync(CancellationToken cancellationToken = default)
    {
        var evseMac = _transport.LocalMac;

        LogInfo($"PEV initiated SLAC: PEV MAC = {PevMac}, RunID = {RunId}");

        // ---- Step 2: CM_SLAC_PARM.CNF (unicast back to PEV) ----
        var parmCnf = new SLACParmCnf(
            MSoundTarget : MACAddress.Broadcast,
            NumSounds    : _options.NumSounds,
            TimeOut      : _options.TimeOut100Ms,
            RespType     : 0x01,
            ForwardingSta: evseMac,
            RunId        : RunId);

        await _transport.SendAsync(PevMac, parmCnf, cancellationToken);
        LogTrace($"→ CM_SLAC_PARM.CNF (unicast → {PevMac})");

        // ---- Step 3: wait for CM_START_ATTEN_CHAR.IND (any of the three is enough) ----
        await WaitFor<StartAttenCharInd>(
            f => f.Message is StartAttenCharInd,
            _options.StartAttenTimeout,
            cancellationToken);

        TransitionTo(EvseSlacState.StartAttenCharReceived);

        // ---- Step 4: collect CM_MNBC_SOUND.IND × NumSounds ----
        var sounds = new List<MnbcSoundInd>(_options.NumSounds);
        var soundDeadline = DateTime.UtcNow + _options.SoundCollectionTimeout;

        while (sounds.Count < _options.NumSounds)
        {
            var remaining = soundDeadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero) break;

            var frame = await ReadOneAsync(remaining, cancellationToken);
            if (frame is null) break;

            if (frame.Message is MnbcSoundInd snd)
            {
                sounds.Add(snd);
                LogTrace($"← CM_MNBC_SOUND.IND #{sounds.Count}/{_options.NumSounds} (cnt={snd.Cnt})");
            }
        }

        TransitionTo(EvseSlacState.SoundsReceived);
        LogInfo($"Collected {sounds.Count} of {_options.NumSounds} M-Sounds.");

        // ---- Step 5: compute attenuation profile (simulated) ----
        var profile = ComputeAttenuationProfile(sounds.Count);
        LogInfo($"Computed attenuation profile, avg = {Average(profile):F1} dB");

        // ---- Step 6: CM_ATTEN_CHAR.IND (broadcast) ----
        var attenInd = new AttenCharInd(
            SourceAddress: PevMac,
            RunId        : RunId,
            SourceId     : _options.EvseId,
            ResponseId   : new Byte[17],
            NumSounds    : (byte) sounds.Count,
            AttenProfile : profile);

        await _transport.SendAsync(MACAddress.Broadcast, attenInd, cancellationToken);
        TransitionTo(EvseSlacState.AttenCharSent);
        LogTrace("→ CM_ATTEN_CHAR.IND (broadcast)");

        // ---- Step 7: wait for CM_ATTEN_CHAR.RSP ----
        var attenRsp = await WaitFor<AttenCharRsp>(
            f => f.Message is AttenCharRsp && f.Source == PevMac,
            _options.AttenCharRspTimeout,
            cancellationToken);

        TransitionTo(EvseSlacState.AttenCharRspReceived);
        LogInfo($"PEV confirmed attenuation profile (Result = 0x{attenRsp.Result:X2}).");

        // ---- Step 7b: optional CM_VALIDATE round-trip(s) ----
        // Only entered if the EVSE is configured for "Network-Mode with
        // Validation" (anti-theft). Multiple REQ/CNF rounds may exchange
        // until the EVSE's observed toggle count matches the EV's reported
        // count, at which point the EVSE moves on to CM_SLAC_MATCH.REQ.
        if (_options.RequireValidation)
        {
            await RunValidationAsync(cancellationToken);
        }

        // ---- Step 8: wait for CM_SLAC_MATCH.REQ ----
        var matchReq = await WaitFor<SlacMatchReq>(
            f => f.Message is SlacMatchReq && f.Source == PevMac,
            _options.SlacMatchTimeout,
            cancellationToken);

        TransitionTo(EvseSlacState.SlacMatchReceived);

        // ---- Step 9: CM_SLAC_MATCH.CNF (unicast, hands over NID + NMK) ----
        var matchCnf = new SlacMatchCnf(
            PevId  : matchReq.PevId,
            PevMac : PevMac,
            EvseId : _options.EvseId,
            EvseMac: evseMac,
            RunId  : RunId,
            Nid    : _options.Nid,
            Nmk    : _options.Nmk);

        await _transport.SendAsync(PevMac, matchCnf, cancellationToken);
        TransitionTo(EvseSlacState.Matched);
        LogInfo("SLAC matching complete. Network credentials sent to PEV.");

        // ---- Step 10: AVLN setup -------------------------------------------
        // Program the negotiated NMK into the local PLC chip so the EVSE
        // leaves its initial AVLN and joins the EV-EVSE AVLN. Once this
        // completes, IPv6 traffic across the PLC link is possible and the
        // next protocol layer (SDP / ISO 15118) can take over.
        if (_options.ChipController is not null)
        {
            LogInfo("Programming NMK into local PLC chip ...");
            await _options.ChipController.SetKeyAsync(_options.Nid, _options.Nmk, cancellationToken);

            LogInfo("Waiting for AVLN to come up ...");
            await _options.ChipController.WaitForAvlnReadyAsync(_options.AvlnReadyTimeout, cancellationToken);

            TransitionTo(EvseSlacState.AvlnReady);
            LogInfo("AVLN is up. Ready for IPv6 / SDP / ISO 15118.");
        }

        return matchCnf;
    }

    /// <summary>
    /// Run one or more CM_VALIDATE.REQ/CNF round-trips with the PEV.
    /// Per HPGP / ISO 15118-3, the EVSE asks the EV to physically toggle
    /// the CP pilot signal a number of times; both sides count toggles
    /// independently and exchange counts until they agree (Result=Success)
    /// or fail (Result=Failure / round budget exhausted).
    /// </summary>
    private async Task RunValidationAsync(CancellationToken token)
    {
        if (_options.ToggleObserver is null)
            throw new InvalidOperationException(
                "RequireValidation = true but EvseSlacOptions.ToggleObserver is null. " +
                "Provide an IToggleObserver (e.g. SimulatedToggleChannel.Observer in the demo).");

        var observer = _options.ToggleObserver;
        observer.Reset();

        TransitionTo(EvseSlacState.ValidationInProgress);
        LogInfo("Starting CM_VALIDATE round-trip with PEV.");

        // Round 0 — tell the PEV that we are ready to start counting.
        await SendValidateReq(SignalType.PevS2Toggles, toggleNum: 0, ValidateResult.Ready, token);

        for (var round = 1; round <= _options.MaxValidationRounds; round++)
        {
            // Wait for the EV's CM_VALIDATE.CNF reporting its current count.
            ValidateCnf cnf;
            try
            {
                cnf = await WaitFor<ValidateCnf>(
                    f => f.Message is ValidateCnf && f.Source == PevMac,
                    _options.ValidateRoundTimeout,
                    token);
            }
            catch (TimeoutException)
            {
                LogInfo($"Round {round}: PEV did not respond, validation failed.");
                await ReportValidationFailure(token);
                return;
            }

            var observed = observer.GetCount();
            LogInfo($"Round {round}: PEV reports {cnf.ToggleNum} toggle(s), EVSE observed {observed}.");

            if (cnf.Result == ValidateResult.Failure)
            {
                LogInfo($"Round {round}: PEV reported failure.");
                await ReportValidationFailure(token);
                return;
            }

            if (observed >= _options.RequiredToggles && cnf.ToggleNum >= _options.RequiredToggles)
            {
                if (observed == cnf.ToggleNum)
                {
                    LogInfo($"Round {round}: counts match at {observed} → validation succeeded.");
                    await SendValidateReq(SignalType.PevS2Toggles, observed, ValidateResult.Success, token);
                    TransitionTo(EvseSlacState.ValidationSucceeded);
                    return;
                }

                LogInfo($"Round {round}: counts differ ({observed} vs {cnf.ToggleNum}) → failure.");
                await ReportValidationFailure(token);
                return;
            }

            // Either side hasn't reached the target yet — keep going.
            await SendValidateReq(SignalType.PevS2Toggles, observed, ValidateResult.Ready, token);
        }

        LogInfo($"Exhausted {_options.MaxValidationRounds} validation round(s) without convergence.");
        await ReportValidationFailure(token);
    }

    private async Task ReportValidationFailure(CancellationToken token)
    {
        await SendValidateReq(SignalType.PevS2Toggles, 0, ValidateResult.Failure, token);
        TransitionTo(EvseSlacState.ValidationFailed);

        if (_options.AbortOnValidationFailure)
        {
            TransitionTo(EvseSlacState.Failed);
            throw new InvalidOperationException("CM_VALIDATE failed; aborting matching session.");
        }

        // If not aborting, fall through and let CM_SLAC_MATCH still proceed.
        // This is non-spec but useful for some test scenarios.
        LogInfo("AbortOnValidationFailure = false → continuing despite failed validation.");
    }

    private async Task SendValidateReq(SignalType type, byte toggleNum, ValidateResult result, CancellationToken token)
    {
        var req = new ValidateReq(type, toggleNum, result);
        await _transport.SendAsync(PevMac, req, token);
        LogTrace($"→ CM_VALIDATE.REQ ToggleNum={toggleNum} Result={result}");
    }

    private async Task<DecodedFrame?> ReadOneAsync(TimeSpan timeout, CancellationToken token)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(timeout);

        try
        {
            return await _inbox.Reader.ReadAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return null;
        }
    }

    private async Task<T> WaitFor<T>(Func<DecodedFrame, bool> predicate, TimeSpan timeout, CancellationToken token)
        where T : ISlacMessage
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(timeout);

        try
        {
            await foreach (var frame in _inbox.Reader.ReadAllAsync(cts.Token))
            {
                LogTrace($"← {frame.Message.MmType} from {frame.Source}");
                if (predicate(frame))
                    return (T) frame.Message;
            }
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            TransitionTo(EvseSlacState.Failed);
            throw new TimeoutException($"Timed out waiting for {typeof(T).Name}.");
        }

        throw new InvalidOperationException("Inbox closed before expected message arrived.");
    }

    /// <summary>
    /// Demo attenuation profile. A real EVSE computes this by averaging the
    /// per-carrier attenuation reported by the HPGP PHY for each MNBC sound.
    /// Here we synthesise plausible values so the demo shows a realistic
    /// shape, with <see cref="EvseSlacOptions.AttenuationBias"/> letting the
    /// caller simulate "this EVSE is closer / further away" for multi-EVSE
    /// experiments. Values are dB attenuation per carrier group.
    /// </summary>
    private byte[] ComputeAttenuationProfile(int observedSounds)
    {
        var profile = new Byte[SLACConstants.NumAttenGroups];
        var rng     = new Random();
        var bias    = _options.AttenuationBias;

        for (var i = 0; i < profile.Length; i++)
        {
            var baseline = 30 + (int) (i * 0.6) + bias;
            var jitter   = rng.Next(-3, 4);
            profile[i]   = (byte) Math.Clamp(baseline + jitter, 0, 100);
        }

        if (observedSounds == 0)
            Array.Fill(profile, (byte) 0xFF);

        return profile;
    }

    private static double Average(byte[] profile)
    {
        if (profile.Length == 0) return 0;
        long sum = 0;
        foreach (var b in profile) sum += b;
        return (double) sum / profile.Length;
    }

    private void TransitionTo(EvseSlacState next)
    {
        State = next;
        StateChanged?.Invoke(this, next);
    }

    private void LogInfo(string  msg) => Log?.Invoke(this, msg);
    private void LogTrace(string msg) => Log?.Invoke(this, msg);

    public ValueTask DisposeAsync()
    {
        _inbox.Writer.TryComplete();
        return ValueTask.CompletedTask;
    }
}

/// <summary>Configuration values for an EVSE-side SLAC session.</summary>
public sealed record EvseSlacOptions
{
    /// <summary>17-byte EVSE identifier (e.g. ASCII like "DE*ABC*E0001*1\0\0\0").</summary>
    public required byte[] EvseId { get; init; }

    /// <summary>7-byte HomePlug AV Network Identifier handed to the PEV.</summary>
    public required byte[] Nid { get; init; }

    /// <summary>16-byte HomePlug AV Network Membership Key handed to the PEV.</summary>
    public required byte[] Nmk { get; init; }

    /// <summary>NUM_SOUNDS the EVSE expects from the PEV.</summary>
    public byte NumSounds { get; init; } = SLACConstants.DefaultNumSounds;

    /// <summary>TIME_OUT in 100 ms units.</summary>
    public byte TimeOut100Ms { get; init; } = SLACConstants.DefaultTimeOut100ms;

    public TimeSpan StartAttenTimeout      { get; init; } = TimeSpan.FromSeconds(2);
    public TimeSpan SoundCollectionTimeout { get; init; } = TimeSpan.FromSeconds(2);
    public TimeSpan AttenCharRspTimeout    { get; init; } = TimeSpan.FromSeconds(2);
    public TimeSpan SlacMatchTimeout       { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Demo-only: shifts the synthetic attenuation profile up or down by this
    /// many dB, to simulate "this EVSE is further away" in multi-EVSE setups.
    /// In a production implementation the profile comes from the HPGP PHY and
    /// this field is ignored.
    /// </summary>
    public int AttenuationBias { get; init; } = 0;

    // ----- CM_VALIDATE (anti-theft) configuration -------------------------

    /// <summary>
    /// Run CM_VALIDATE.REQ/CNF round-trip(s) before CM_SLAC_MATCH. Per
    /// HPGP this is "Network-Mode with Validation" — the EVSE asks the EV
    /// to physically toggle its CP pilot signal a defined number of times,
    /// and matching only proceeds if both sides agree on the count.
    /// Default: false (most production EVSEs run "Network-Mode without
    /// Validation").
    /// </summary>
    public bool RequireValidation { get; init; } = false;

    /// <summary>
    /// Required by <see cref="RequireValidation"/>. Source of CP-toggle
    /// counts the EVSE's hardware has observed on the pilot wire. Demo
    /// implementations: <see cref="Validation.SimulatedToggleChannel.Observer"/>
    /// or <see cref="Validation.AutoToggleObserver"/>.
    /// </summary>
    public IToggleObserver? ToggleObserver { get; init; }

    /// <summary>
    /// Number of CP toggles the EVSE expects to see before declaring success.
    /// Per HPGP / ISO 15118-3 this is a small number (typically 2 or 3).
    /// </summary>
    public byte RequiredToggles { get; init; } = 3;

    /// <summary>Maximum number of CM_VALIDATE.REQ/CNF rounds before giving up.</summary>
    public int MaxValidationRounds { get; init; } = 6;

    /// <summary>How long to wait for each CM_VALIDATE.CNF before timing out.</summary>
    public TimeSpan ValidateRoundTimeout { get; init; } = TimeSpan.FromMilliseconds(800);

    /// <summary>
    /// If true, a failed validation aborts the matching session and no NMK is
    /// handed out. If false, the EVSE proceeds to CM_SLAC_MATCH anyway —
    /// non-spec but useful for "what if validation breaks?" test scenarios.
    /// </summary>
    public bool AbortOnValidationFailure { get; init; } = true;

    // ----- AVLN setup (after match) ----------------------------------------

    /// <summary>
    /// Optional. If non-null, the session calls
    /// <see cref="Avln.IPlcChipController.SetKeyAsync"/> on this controller
    /// after CM_SLAC_MATCH.CNF has been sent, programming the negotiated NMK
    /// into the local PLC chip so the EVSE joins the EV's AVLN. Followed by
    /// <see cref="Avln.IPlcChipController.WaitForAvlnReadyAsync"/>. Once that
    /// returns, IPv6 traffic across the PLC link is possible — the next
    /// protocol layer (SDP / ISO 15118) can take over.
    ///
    /// Leave null to skip AVLN setup (e.g. if the host configures the chip
    /// out-of-band, or in pure-simulation mode where there is no chip).
    /// </summary>
    public Avln.IPlcChipController? ChipController { get; init; }

    /// <summary>How long to wait for the AVLN to come up after SetKey.</summary>
    public TimeSpan AvlnReadyTimeout { get; init; } = TimeSpan.FromSeconds(5);
}
