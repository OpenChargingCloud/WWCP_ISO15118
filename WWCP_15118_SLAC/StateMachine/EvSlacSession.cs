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

using System.Security.Cryptography;
using System.Threading.Channels;
using cloud.charging.open.protocols.ISO15118.SLAC.Messages;
using cloud.charging.open.protocols.ISO15118.SLAC.Selection;
using cloud.charging.open.protocols.ISO15118.SLAC.Transport;
using cloud.charging.open.protocols.ISO15118.SLAC.Validation;
using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

namespace cloud.charging.open.protocols.ISO15118.SLAC.StateMachine;

/// <summary>State of the EV-side SLAC matching session.</summary>
public enum EvSlacState
{
    Idle,
    SlacParmRequested,
    /// <summary>The CM_SLAC_PARM.CNF collection window has closed. <see cref="EvSlacSession.Candidates"/> is populated.</summary>
    ParmCnfsCollected,
    StartAttenCharSent,
    SoundingSent,
    /// <summary>The CM_ATTEN_CHAR.IND collection window has closed. Candidates have profiles attached.</summary>
    AttenCharsCollected,
    /// <summary>The selector has chosen a winner.</summary>
    EvseSelected,
    AttenCharResponded,
    /// <summary>EVSE has requested CM_VALIDATE; the EV is exchanging toggle counts.</summary>
    Validating,
    /// <summary>CM_VALIDATE succeeded; EV is free to proceed to CM_SLAC_MATCH.REQ.</summary>
    ValidationDone,
    MatchRequested,
    Matched,
    /// <summary>The local PLC chip has been programmed with the NMK and the AVLN is up.</summary>
    AvlnReady,
    Failed
}

/// <summary>
/// EV-side SLAC matching state machine. Drives the protocol from
/// CM_SLAC_PARM.REQ through to CM_SLAC_MATCH.CNF, supporting multiple EVSEs
/// in range — collects responses from all of them, hands the candidates to
/// an <see cref="IEVSESelector"/> (default: lowest-average-attenuation), and
/// finishes the matching only with the winner.
/// </summary>
public sealed class EvSlacSession : IAsyncDisposable
{
    private readonly ISlacTransport        _transport;
    private readonly EvSlacOptions         _options;
    private readonly IEVSESelector         _selector;
    private readonly Channel<DecodedFrame> _inbox =
        Channel.CreateUnbounded<DecodedFrame>(
            new UnboundedChannelOptions {
                SingleReader = true,
                SingleWriter = false
            }
        );

    /// <summary>
    /// Snapshot of all EVSEs that responded to CM_SLAC_PARM.REQ. The map is
    /// updated incrementally during the run; this property always returns a
    /// safe copy that can be enumerated without colliding with the worker.
    /// </summary>
    public IReadOnlyDictionary<MACAddress, EVSECandidate> Candidates
        => _candidates.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

    private readonly Dictionary<MACAddress, EVSECandidate> _candidates = new();

    public EvSlacState State { get; private set; } = EvSlacState.Idle;

    public event EventHandler<EvSlacState>?          StateChanged;
    public event EventHandler<string>?               Log;
    /// <summary>Fired whenever a new EVSE candidate is added (CM_SLAC_PARM.CNF).</summary>
    public event EventHandler<EVSECandidate>?        CandidateAdded;
    /// <summary>Fired whenever a candidate's attenuation profile arrives (CM_ATTEN_CHAR.IND).</summary>
    public event EventHandler<EVSECandidate>?        CandidateProfileReceived;

    public EvSlacSession(
        ISlacTransport transport,
        EvSlacOptions  options,
        IEVSESelector? selector = null)
    {
        _transport = transport;
        _options   = options;
        _selector  = selector ?? new LowestAverageAttenuationSelector();
        _transport.FrameReceived += OnFrameReceived;
    }

    private void OnFrameReceived(object? sender, DecodedFrame frame)
        => _inbox.Writer.TryWrite(frame);

    /// <summary>Run the full SLAC matching sequence. Throws on protocol failure or timeout.</summary>
    public async Task<EVSLACMatchingResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var runId  = RunId.NewRandom();
        var pevId  = _options.PevId;
        var pevMac = _transport.LocalMac;

        LogInfo($"Starting SLAC session, RunID = {runId}");

        // ---- Step 1: CM_SLAC_PARM.REQ (broadcast) --------------------------
        TransitionTo(EvSlacState.Idle);
        var parmReq = new SLACParamReq(runId);
        await SendBroadcast(parmReq, cancellationToken);
        TransitionTo(EvSlacState.SlacParmRequested);

        // ---- Step 2: collect CM_SLAC_PARM.CNF in a time window -------------
        // Multiple EVSEs may answer; keep listening until the window closes.
        var parmDeadline = DateTime.UtcNow + _options.ParmCnfCollectionWindow;
        while (DateTime.UtcNow < parmDeadline)
        {
            var remaining = parmDeadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero) break;

            var frame = await TryReadAsync(remaining, cancellationToken);
            if (frame is null) break;

            if (frame.Message is SLACParmCnf cnf && cnf.RunId == runId)
            {
                if (!_candidates.ContainsKey(frame.Source))
                {
                    var cand = new EVSECandidate(frame.Source, cnf, AttenChar: null);
                    _candidates[frame.Source] = cand;
                    LogInfo($"  candidate +{frame.Source}  (NumSounds={cnf.NumSounds})");
                    CandidateAdded?.Invoke(this, cand);
                }
                // else: duplicate ParmCnf retransmit — first-write-wins.
            }
        }

        TransitionTo(EvSlacState.ParmCnfsCollected);

        if (_candidates.Count == 0)
        {
            TransitionTo(EvSlacState.Failed);
            throw new TimeoutException(
                $"No EVSE responded to CM_SLAC_PARM.REQ within {_options.ParmCnfCollectionWindow}.");
        }

        LogInfo($"Found {_candidates.Count} candidate EVSE(s).");

        // ---- Step 3: derive sounding parameters from candidates ------------
        // If candidates disagree on NumSounds / TimeOut, pick the maximum so
        // every EVSE gets at least what it expected. Real-world: all HPGP
        // EVSEs use 10.
        byte numSounds = 0, timeOut = 0;
        foreach (var c in _candidates.Values)
        {
            if (c.ParmCnf.NumSounds > numSounds) numSounds = c.ParmCnf.NumSounds;
            if (c.ParmCnf.TimeOut   > timeOut)   timeOut   = c.ParmCnf.TimeOut;
        }

        // ---- Step 4: CM_START_ATTEN_CHAR.IND × 3 (broadcast) ---------------
        var startInd = new StartAttenCharInd(
            NumSounds    : numSounds,
            TimeOut      : timeOut,
            RespType     : 0x01,
            ForwardingSta: pevMac,
            RunId        : runId);

        for (var i = 0; i < 3; i++)
        {
            await SendBroadcast(startInd, cancellationToken);
            await Task.Delay(_options.StartAttenInterFrameDelay, cancellationToken);
        }
        TransitionTo(EvSlacState.StartAttenCharSent);

        // ---- Step 5: CM_MNBC_SOUND.IND × NumSounds (broadcast) -------------
        for (var i = 0; i < numSounds; i++)
        {
            var random16 = new Byte[16];
            RandomNumberGenerator.Fill(random16);

            var snd = new MnbcSoundInd(
                SenderId : new Byte[17],
                Cnt      : (byte) (numSounds - 1 - i),
                RunId    : runId,
                Random16 : random16);

            await SendBroadcast(snd, cancellationToken);
            await Task.Delay(_options.SoundInterFrameDelay, cancellationToken);
        }
        TransitionTo(EvSlacState.SoundingSent);

        // ---- Step 6: collect CM_ATTEN_CHAR.IND from each candidate ---------
        var attenDeadline = DateTime.UtcNow + _options.AttenCharCollectionWindow;
        while (DateTime.UtcNow < attenDeadline)
        {
            var remaining = attenDeadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero) break;

            var frame = await TryReadAsync(remaining, cancellationToken);
            if (frame is null) break;

            if (frame.Message is AttenCharInd ind && ind.RunId == runId)
            {
                if (_candidates.TryGetValue(frame.Source, out var existing) &&
                    existing.AttenChar is null)
                {
                    var updated = existing with { AttenChar = ind };
                    _candidates[frame.Source] = updated;
                    LogInfo($"  profile from {frame.Source}: avg={updated.AverageAttenuation:F1} dB");
                    CandidateProfileReceived?.Invoke(this, updated);
                }
                // else: AttenChar from an EVSE that did not answer ParmCnf, or
                // a duplicate retransmit — silently dropped.
            }

            // Optional early exit: every candidate has reported in.
            if (AllCandidatesHaveProfiles())
                break;
        }

        TransitionTo(EvSlacState.AttenCharsCollected);

        // ---- Step 7: select winner -----------------------------------------
        var rankable = _candidates.Values.Where(c => c.HasAttenuationProfile).ToList();
        if (rankable.Count == 0)
        {
            TransitionTo(EvSlacState.Failed);
            throw new TimeoutException(
                $"No EVSE returned an attenuation profile within {_options.AttenCharCollectionWindow}.");
        }

        var winner = _selector.Select(rankable)
            ?? throw new InvalidOperationException("Selector returned no candidate.");

        TransitionTo(EvSlacState.EvseSelected);
        LogInfo($"Selected EVSE {winner.EVSEMACAddress}  (avg attenuation: {winner.AverageAttenuation:F1} dB)");

        // ---- Step 8: CM_ATTEN_CHAR.RSP (unicast to winner) -----------------
        var attenRsp = new AttenCharRsp(
            SourceAddress: pevMac,
            RunId        : runId,
            SourceId     : pevId,
            ResponseId   : winner.AttenChar!.SourceId,
            Result       : 0x00); // success

        await _transport.SendAsync(winner.EVSEMACAddress, attenRsp, cancellationToken);
        TransitionTo(EvSlacState.AttenCharResponded);

        // The non-selected EVSEs receive nothing further from us — they will
        // time out their own match-response timer and clean themselves up,
        // exactly as ISO 15118-3 prescribes.

        // ---- Step 8b: optional CM_VALIDATE round-trip(s) -------------------
        // The EVSE may now ask us to physically prove our presence by toggling
        // the CP pilot signal. If we see a CM_VALIDATE.REQ within a short
        // window, we reply until the EVSE reports Result=Success or Failure;
        // if no REQ arrives we assume the EVSE runs in "Network-Mode without
        // Validation" and proceed straight to CM_SLAC_MATCH.REQ.
        await TryRunValidationAsync(winner.EVSEMACAddress, runId, cancellationToken);

        // ---- Step 9: CM_SLAC_MATCH.REQ (unicast to winner) -----------------
        var matchReq = new SlacMatchReq(
            PevId  : pevId,
            PevMac : pevMac,
            EvseId : winner.EvseId ?? new Byte[17],
            EvseMac: winner.EVSEMACAddress,
            RunId  : runId);

        await _transport.SendAsync(winner.EVSEMACAddress, matchReq, cancellationToken);
        TransitionTo(EvSlacState.MatchRequested);

        // ---- Step 10: wait for CM_SLAC_MATCH.CNF ---------------------------
        var matchCnf = await WaitFor<SlacMatchCnf>(
            f => f.Message is SlacMatchCnf cnf && cnf.RunId == runId && f.Source == winner.EVSEMACAddress,
            _options.SlacMatchTimeout,
            cancellationToken);

        TransitionTo(EvSlacState.Matched);

        LogInfo($"SLAC matching complete with {winner.EVSEMACAddress}.  " +
                $"NID={Convert.ToHexString(matchCnf.Nid)}  " +
                $"NMK={Convert.ToHexString(matchCnf.Nmk)}");

        // ---- Step 11: AVLN setup -------------------------------------------
        // Program the received NMK into the local PLC chip so the EV joins
        // the EVSE's AVLN. After WaitForAvlnReadyAsync returns, IPv6 traffic
        // across the PLC link is possible — the next protocol layer (SDP /
        // ISO 15118) can take over.
        if (_options.ChipController is not null)
        {
            LogInfo("Programming NMK into local PLC chip ...");
            await _options.ChipController.SetKeyAsync(matchCnf.Nid, matchCnf.Nmk, cancellationToken);

            LogInfo("Waiting for AVLN to come up ...");
            await _options.ChipController.WaitForAvlnReadyAsync(_options.AvlnReadyTimeout, cancellationToken);

            TransitionTo(EvSlacState.AvlnReady);
            LogInfo("AVLN is up. Ready for IPv6 / SDP / ISO 15118.");
        }

        return new EVSLACMatchingResult(
            Winner       : winner,
            MatchCnf     : matchCnf,
            AllCandidates: _candidates.Values.ToList(),
            RunId        : runId);
    }

    private bool AllCandidatesHaveProfiles()
    {
        foreach (var c in _candidates.Values)
            if (!c.HasAttenuationProfile) return false;
        return _candidates.Count > 0;
    }

    private async Task SendBroadcast(ISlacMessage message, CancellationToken token)
    {
        LogTrace($"→ {message.MmType} (broadcast)");
        await _transport.SendAsync(MACAddress.Broadcast, message, token);
    }

    /// <summary>Read one frame, or return null on timeout. Used for collection windows.</summary>
    /// <summary>
    /// Wait briefly for a CM_VALIDATE.REQ from the chosen EVSE. If none
    /// arrives within <see cref="EvSlacOptions.ValidateInitialWait"/>, the
    /// EVSE is running without validation and we proceed silently.
    /// </summary>
    private async Task TryRunValidationAsync(MACAddress evseMac, RunId runId, CancellationToken token)
    {
        // Peek for a CM_VALIDATE.REQ. If one shows up, run the round-trip;
        // otherwise the EVSE is in "Network-Mode without Validation" and
        // we simply move on.
        ValidateReq? firstReq = null;
        var deadline = DateTime.UtcNow + _options.ValidateInitialWait;

        while (DateTime.UtcNow < deadline)
        {
            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero) break;

            var frame = await TryReadAsync(remaining, token);
            if (frame is null) break;

            if (frame.Message is ValidateReq req && frame.Source == evseMac)
            {
                firstReq = req;
                break;
            }
            // Other frames (e.g. duplicate ATTEN_CHAR.IND retransmits) are dropped.
        }

        if (firstReq is null)
        {
            LogTrace("No CM_VALIDATE.REQ — EVSE runs without validation, proceeding.");
            return;
        }

        // ---- Validation loop ---------------------------------------------
        if (_options.ToggleSource is null)
            throw new InvalidOperationException(
                "EVSE requested CM_VALIDATE but EvSlacOptions.ToggleSource is null. " +
                "Provide an IToggleSource (e.g. SimulatedToggleChannel.Source in the demo).");

        var source = _options.ToggleSource;
        source.Reset();

        TransitionTo(EvSlacState.Validating);
        LogInfo($"EVSE requested CM_VALIDATE (Result={firstReq.Result}). Reporting toggle counts.");

        // Reply to the initial REQ.
        await SendValidateCnf(evseMac, source.GetCount(), MapResult(firstReq.Result), token);

        for (var round = 1; round <= _options.MaxValidationRounds; round++)
        {
            ValidateReq req;
            try
            {
                req = await WaitFor<ValidateReq>(
                    f => f.Message is ValidateReq && f.Source == evseMac,
                    _options.ValidateRoundTimeout,
                    token);
            }
            catch (TimeoutException)
            {
                LogInfo($"Round {round}: no further CM_VALIDATE.REQ — assuming EVSE finished.");
                return;
            }

            LogTrace($"Round {round}: EVSE reports {req.ToggleNum} toggle(s), Result={req.Result}.");

            if (req.Result == ValidateResult.Success)
            {
                LogInfo("EVSE confirmed validation success.");
                TransitionTo(EvSlacState.ValidationDone);
                return;
            }

            if (req.Result == ValidateResult.Failure)
            {
                TransitionTo(EvSlacState.Failed);
                throw new InvalidOperationException("EVSE reported CM_VALIDATE failure.");
            }

            await SendValidateCnf(evseMac, source.GetCount(), ValidateResult.Ready, token);
        }

        // Spec doesn't precisely define what happens if rounds exhaust on the
        // EV side; a real implementation should give up rather than spam.
        TransitionTo(EvSlacState.Failed);
        throw new InvalidOperationException("Exhausted CM_VALIDATE rounds without EVSE confirmation.");
    }

    private async Task SendValidateCnf(MACAddress evseMac, byte toggleNum, ValidateResult result, CancellationToken token)
    {
        var cnf = new ValidateCnf(SignalType.PevS2Toggles, toggleNum, result);
        await _transport.SendAsync(evseMac, cnf, token);
        LogTrace($"→ CM_VALIDATE.CNF ToggleNum={toggleNum} Result={result}");
    }

    /// <summary>The EV mirrors the EVSE's intent: Ready→Ready, Success→Success, Failure→Failure.</summary>
    private static ValidateResult MapResult(ValidateResult evseResult)
        => evseResult switch
        {
            ValidateResult.Success  => ValidateResult.Success,
            ValidateResult.Failure  => ValidateResult.Failure,
            ValidateResult.NotReady => ValidateResult.NotReady,
            _                       => ValidateResult.Ready
        };

    private async Task<DecodedFrame?> TryReadAsync(TimeSpan timeout, CancellationToken token)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(timeout);

        try
        {
            var frame = await _inbox.Reader.ReadAsync(cts.Token).ConfigureAwait(false);
            LogTrace($"← {frame.Message.MmType} from {frame.Source}");
            return frame;
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
            TransitionTo(EvSlacState.Failed);
            throw new TimeoutException($"Timed out waiting for {typeof(T).Name} after {timeout}.");
        }

        throw new InvalidOperationException("Inbox closed before expected message arrived.");
    }

    private void TransitionTo(EvSlacState next)
    {
        State = next;
        StateChanged?.Invoke(this, next);
    }

    private void LogInfo(string  msg) => Log?.Invoke(this, msg);
    private void LogTrace(string msg) => Log?.Invoke(this, msg);

    public ValueTask DisposeAsync()
    {
        _transport.FrameReceived -= OnFrameReceived;
        _inbox.Writer.TryComplete();
        return ValueTask.CompletedTask;
    }
}

/// <summary>Configuration values for the EV-side SLAC session.</summary>
public sealed record EvSlacOptions
{
    /// <summary>17-byte PEV identifier sent in matching messages.</summary>
    public required byte[] PevId { get; init; }

    /// <summary>
    /// How long to listen for CM_SLAC_PARM.CNF responses. Multiple EVSEs may
    /// answer; the EV waits the full window so it can compare all of them.
    /// ISO 15118-3 calls this TT_EV_match_MNBC; default is the spec's 600 ms.
    /// </summary>
    public TimeSpan ParmCnfCollectionWindow { get; init; } = TimeSpan.FromMilliseconds(600);

    /// <summary>
    /// How long to listen for CM_ATTEN_CHAR.IND broadcasts after the EV has
    /// finished sending sounds. Each candidate EVSE will broadcast one IND.
    /// </summary>
    public TimeSpan AttenCharCollectionWindow { get; init; } = TimeSpan.FromMilliseconds(1200);

    /// <summary>How long to wait for CM_SLAC_MATCH.CNF from the chosen EVSE.</summary>
    public TimeSpan SlacMatchTimeout { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>Pause between the three CM_START_ATTEN_CHAR.IND broadcasts.</summary>
    public TimeSpan StartAttenInterFrameDelay { get; init; } = TimeSpan.FromMilliseconds(40);

    /// <summary>Pause between consecutive CM_MNBC_SOUND.IND broadcasts.</summary>
    public TimeSpan SoundInterFrameDelay { get; init; } = TimeSpan.FromMilliseconds(40);

    // ----- CM_VALIDATE (anti-theft) configuration -------------------------

    /// <summary>
    /// CP-toggle source the EV uses to count physical toggles when an EVSE
    /// requests CM_VALIDATE. Required when the chosen EVSE is configured for
    /// "Network-Mode with Validation"; null is fine when no EVSE in range
    /// requests validation.
    /// </summary>
    public IToggleSource? ToggleSource { get; init; }

    /// <summary>
    /// How long the EV waits for an initial CM_VALIDATE.REQ from the chosen
    /// EVSE after sending CM_ATTEN_CHAR.RSP. If no REQ arrives in this window,
    /// the EV assumes "Network-Mode without Validation" and proceeds straight
    /// to CM_SLAC_MATCH.REQ.
    /// </summary>
    public TimeSpan ValidateInitialWait { get; init; } = TimeSpan.FromMilliseconds(300);

    /// <summary>How long to wait for each subsequent CM_VALIDATE.REQ.</summary>
    public TimeSpan ValidateRoundTimeout { get; init; } = TimeSpan.FromMilliseconds(800);

    /// <summary>Maximum CM_VALIDATE rounds before the EV gives up.</summary>
    public int MaxValidationRounds { get; init; } = 6;

    // ----- AVLN setup (after match) ----------------------------------------

    /// <summary>
    /// Optional. If non-null, the session calls
    /// <see cref="Avln.IPlcChipController.SetKeyAsync"/> on this controller
    /// after CM_SLAC_MATCH.CNF is received, programming the EVSE's NMK into
    /// the local PLC chip so the EV joins the EVSE's AVLN. Followed by
    /// <see cref="Avln.IPlcChipController.WaitForAvlnReadyAsync"/>. Once that
    /// returns, IPv6 traffic across the PLC link is possible — the next
    /// protocol layer (SDP / ISO 15118) can take over.
    /// </summary>
    public Avln.IPlcChipController? ChipController { get; init; }

    /// <summary>How long to wait for the AVLN to come up after SetKey.</summary>
    public TimeSpan AvlnReadyTimeout { get; init; } = TimeSpan.FromSeconds(5);
}
