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

namespace cloud.charging.open.protocols.ISO15118.Metering;

/// <summary>
/// One reading of a peer's clock, taken from a timestamp it put on the wire.
/// </summary>
/// <param name="Sent">This side's clock when the request left.</param>
/// <param name="Received">This side's clock when the answer arrived.</param>
/// <param name="Peer">The whole second the peer stamped.</param>
/// <remarks>
/// <para>
/// <b>The peer stamps whole seconds</b> — ISO 15118-20's header <c>TimeStamp</c>, <c>MeterTimestamp</c>
/// and -2's <c>TMeter</c> are all unix seconds — so the instant it read its clock lies somewhere in
/// <c>[Peer, Peer + 1 s)</c>, and somewhere between <see cref="Sent"/> and <see cref="Received"/> on
/// this side. <see cref="Offset"/> compares the two midpoints and <see cref="Uncertainty"/> is the sum
/// of the two half-widths. Reporting an offset without it would call a station half a second fast for
/// no better reason than where its second happened to tick.
/// </para>
/// </remarks>
public readonly record struct ClockSample(DateTimeOffset Sent, DateTimeOffset Received, DateTimeOffset Peer)
{

    /// <summary>How long the exchange took on this side's clock.</summary>
    public TimeSpan RoundTrip => Received - Sent;

    /// <summary>Peer clock minus this side's clock: positive when the peer is ahead.</summary>
    public TimeSpan Offset => Peer + TimeSpan.FromSeconds(0.5) - (Sent + RoundTrip / 2);

    /// <summary>How far off <see cref="Offset"/> can be without either clock being wrong.</summary>
    public TimeSpan Uncertainty => TimeSpan.FromSeconds(0.5) + RoundTrip / 2;

}


/// <summary>
/// A peer's clock compared with this side's, over one session.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a session keeps this at all.</b> Every later check on a meter reading leans on time: which
/// tariff period it falls in, whether a signed record belongs to this session, how two power curves
/// line up sample by sample. The comparison costs one subtraction per answer and is available from the
/// first response, before anything has been authorised — which is when a driver can still walk away.
/// </para>
/// <para>
/// <b>First and last, not an average.</b> The first sample is what was true before charging began; the
/// difference to the last is the drift over the session. Averaging would hide exactly that.
/// </para>
/// <para>
/// <b>It measures and does not judge.</b> <see cref="Agrees"/> takes the tolerance from the caller,
/// because what is tolerable is a question of tariff and law, not of this class.
/// </para>
/// </remarks>
public sealed class ClockComparison
{

    // Year 9999 in unix seconds: beyond it DateTimeOffset cannot represent the value at all.
    private const ulong MaxUnixSeconds = 253_402_300_799;

    /// <summary>How many timestamps were compared.</summary>
    public int Samples { get; private set; }

    /// <summary>How many timestamps could not be read as a time at all.</summary>
    public int Unreadable { get; private set; }

    /// <summary>The first comparison of the session.</summary>
    public ClockSample? First { get; private set; }

    /// <summary>The most recent comparison.</summary>
    public ClockSample? Last { get; private set; }

    /// <summary>How far the offset moved between the first and the last sample.</summary>
    public TimeSpan? Drift => First is { } first && Last is { } last && Samples > 1
                                  ? last.Offset - first.Offset
                                  : null;

    /// <summary>
    /// Whether the offset moved by more than the two samples can resolve between them.
    /// </summary>
    /// <remarks>
    /// A drift is the difference of two offsets and inherits the uncertainty of both, so a clock that
    /// merely ticked over to its next whole second between two samples "moves" by up to a second
    /// without having moved at all. Found in the first live run, where a meter in perfect step was
    /// reported as half a second fast.
    /// </remarks>
    public bool Drifted => Drift is { } drift && First is { } first && Last is { } last &&
                           drift.Duration() > first.Uncertainty + last.Uncertainty;

    /// <summary>
    /// Records one timestamp of the peer. Returns the sample, or null when the value is no time.
    /// </summary>
    public ClockSample? Note(DateTimeOffset sent, DateTimeOffset received, ulong peerUnixSeconds)
    {

        if (peerUnixSeconds > MaxUnixSeconds)
        {
            Unreadable++;
            return null;
        }

        var sample = new ClockSample(sent, received, DateTimeOffset.FromUnixTimeSeconds((long) peerUnixSeconds));

        First ??= sample;
        Last    = sample;
        Samples++;

        return sample;

    }

    /// <summary>
    /// Whether the latest offset is within <paramref name="tolerance"/>, allowing for what the
    /// comparison itself cannot resolve. Null when nothing was compared.
    /// </summary>
    public bool? Agrees(TimeSpan tolerance) => Last is { } last
                                                   ? last.Offset.Duration() <= tolerance + last.Uncertainty
                                                   : null;

}


/// <summary>
/// One meter reading a station reported, paired with what the vehicle had counted by then.
/// </summary>
/// <param name="Sample">Which charge-loop iteration carried it, counted from 1.</param>
/// <param name="MeterId">The meter's identifier, as the station sent it.</param>
/// <param name="StationWh">The station's reading in watt-hours.</param>
/// <param name="VehicleWh">The vehicle's own count at the same iteration (<see cref="EvMeter"/>).</param>
/// <param name="Signed">Whether the reading carried the meter's signature. Present, not verified.</param>
/// <param name="MeterTime">The meter's own timestamp, when it sent one.</param>
/// <param name="DischargedWh">The meter's discharge register, <c>BPT_DischargedEnergyReadingWh</c>, when the
/// reading carries one: energy from the vehicle to the station in a bidirectional session. Null for a
/// charge-only reading, which is every -2 one.</param>
/// <remarks>
/// <para>
/// Paired <b>at the reading</b>: the vehicle's figure is taken after it counted the same iteration the
/// station's response answered. Comparing a station's early reading with the vehicle's final total
/// would show a difference with nothing wrong in it.
/// </para>
/// <para>
/// <b>Compared net in a bidirectional session.</b> The vehicle keeps one signed total, which goes down
/// when it gives energy back, and the station keeps two rising registers. So the figure the vehicle's
/// count is held to is charged minus discharged, <see cref="StationNetWh"/>. Until 2026-11-28 it was the
/// charged register alone, and a discharge session showed a difference of everything it gave back.
/// </para>
/// </remarks>
public sealed record StationReading(int Sample, string MeterId, ulong StationWh, double VehicleWh,
                                    bool Signed, DateTimeOffset? MeterTime, ulong? DischargedWh = null)
{

    /// <summary>The station's net figure in watt-hours: charged minus discharged, or the charged register
    /// alone when the reading has no discharge register. Negative when more went back than came in.</summary>
    public double StationNetWh => (double) StationWh - (DischargedWh ?? 0);

    /// <summary>Station minus vehicle, in watt-hours, on the net figure.</summary>
    public double DifferenceWh => StationNetWh - VehicleWh;

    /// <summary>The difference relative to the vehicle's count, or null while that is zero.</summary>
    public double? RelativeDifference => VehicleWh != 0 ? DifferenceWh / VehicleWh : null;

}
