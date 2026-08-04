/*
 * Copyright (c) 2021-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of EVSimulatorApp
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

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vanaheimr.V2G.Simulation.Ocpp;

/// <summary>
/// What a charge point tells its backend about the energy it delivered — recorded, so a phone has a
/// third account of one charging session to hold against the other two.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is not an OCPP implementation and must not be mistaken for one.</b> It is the handful of
/// fields the comparison in <c>docs/CONCEPT.md</c> §4.3 needs, shaped like OCPP 2.0.1's
/// <c>MeterValueType</c> / <c>SampledValueType</c> / <c>SignedMeterValueType</c> so that a real
/// implementation can produce it without translation. Nothing here is checked against an OCPP schema
/// — there is no reference to check against in this repository, unlike the ISO 15118 XSDs — so the
/// shape is a convention with a familiar name, exactly as <see cref="Metering.MeterSigningPayload"/>
/// is. The real emitter is a CSMS-facing stack, and this exists so the app has something to read
/// before one is wired up.
/// </para>
/// <para>
/// <b>What is genuinely checkable without a CSMS.</b> The third leg's *point* is independence, and a
/// record produced by the station is not independent of the station — it is the same party twice, and
/// anything reading it has to say so. But one real question survives: <b>did the station tell the
/// backend what it told the car?</b> The signed value here is the very same <c>SigMeterReading</c>
/// the vehicle saw on the wire, byte for byte, so a station quietly reporting one figure for billing
/// and showing another to the driver is visible from the two records alone — with no key, no CSMS,
/// and no trust in either party. That is the fraud this chapter is about, and it is invisible from
/// either side on its own.
/// </para>
/// <para>
/// <b>The binding is not decoration.</b> <see cref="V2GSessionId"/> ties the transaction to one ISO
/// 15118 session. Without it a reader is comparing this session's energy against some transaction
/// or other, and any agreement it finds is luck. Whatever consumes this must refuse to compare when
/// the binding does not hold rather than compare anyway and report a difference.
/// </para>
/// </remarks>
public sealed record OcppTransactionRecord(
    string                        TransactionId,
    string                        EvseId,
    string                        V2GSessionId,
    IReadOnlyList<OcppMeterValue> MeterValues)
{

    /// <summary>Where these values came from, and therefore what comparing them is worth.</summary>
    /// <remarks>
    /// <c>station-recording</c>: captured from the charge point itself, so it is the same party as
    /// the <c>MeterInfo</c> on the wire — the energy figures cannot disagree in a way that means
    /// anything, and only the *signature* comparison says something. <c>csms</c>: fetched from the
    /// backend, which is the arrangement §4.3 actually asks for and the only one where three
    /// independent measurements exist. A consumer that ignored this field would overstate every
    /// verdict it produced.
    /// </remarks>
    public string Source { get; init; } = "station-recording";

    public const int SchemaVersion = 1;

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented          = true,
        PropertyNamingPolicy   = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string ToJson() =>
        JsonSerializer.Serialize(new
        {
            schemaVersion = SchemaVersion,
            source        = Source,
            transactionId = TransactionId,
            evseId        = EvseId,
            v2gSessionId  = V2GSessionId,
            meterValues   = MeterValues,
        }, Json);

}


/// <summary>One reading, at one instant. OCPP groups several sampled values under one timestamp;
/// this records the one that matters here and leaves the shape able to carry more.</summary>
/// <param name="Timestamp">ISO 8601 UTC, as OCPP transports it — not the unix seconds ISO 15118
/// uses, because this is the backend's record and mixing the two conventions is how a comparison
/// ends up off by an epoch.</param>
public sealed record OcppMeterValue(string Timestamp, IReadOnlyList<OcppSampledValue> SampledValue);


/// <param name="Value">The register reading. A string, because OCPP's is a decimal and JSON numbers
/// are doubles — the same reason the ISO 15118 emitter writes <c>unsignedLong</c> as text.</param>
/// <param name="Context">OCPP's <c>ReadingContext</c>: <c>Sample.Periodic</c> during the loop,
/// <c>Transaction.End</c> for the last one. The final value is the billed one, so which reading is
/// final is part of the record rather than a matter of position.</param>
/// <param name="Measurand">Fixed at <c>Energy.Active.Import.Register</c>: cumulative imported energy,
/// which is what both other legs count. A power or a per-interval measurand would be a different
/// quantity wearing the same unit.</param>
public sealed record OcppSampledValue(
    string                 Value,
    string                 Context,
    string                 Measurand,
    OcppUnitOfMeasure      UnitOfMeasure,
    string                 Location,
    OcppSignedMeterValue?  SignedMeterValue = null);


public sealed record OcppUnitOfMeasure(string Unit, int Multiplier = 0);


/// <summary>
/// OCPP's carrier for a meter's own signature — the field that makes Eichrecht transportable.
/// </summary>
/// <param name="SignedMeterData">The signature itself, hex. <b>The same 64 bytes the vehicle saw in
/// <c>SigMeterReading</c></b>, which is the whole reason this record is worth keeping.</param>
/// <param name="SigningMethod">The algorithm, named the way OCPP names one.</param>
/// <param name="EncodingMethod">What <paramref name="SignedMeterData"/> covers. A real Eichrecht
/// meter would say <c>OCMF</c> or <c>EDL</c>; this says <c>V2G-METER-1</c>, because the layout is
/// this project's own and claiming OCMF would be a lie a verifier could not detect until it failed.</param>
/// <param name="PublicKey">The meter's public key, hex <c>X‖Y</c>. Present so the record is
/// self-describing, and worth exactly as much as a key handed over by the party it authenticates —
/// which is to say, not enough on its own.</param>
public sealed record OcppSignedMeterValue(
    string SignedMeterData,
    string SigningMethod,
    string EncodingMethod,
    string PublicKey);


/// <summary>
/// Collects a station's meter values as a session runs.
/// </summary>
/// <remarks>
/// Injected into the SECC rather than built by it, so a station that has no backend records nothing
/// and behaves exactly as before. The seam is the point: swap this for something that speaks to a
/// real CSMS and everything downstream — the corpus, the app's comparison — is unchanged apart from
/// <see cref="OcppTransactionRecord.Source"/> saying so.
/// </remarks>
public sealed class OcppTransactionRecorder(string transactionId, string evseId, string v2gSessionId)
{

    private readonly List<OcppMeterValue> values = [];

    /// <summary>Books one reading. <paramref name="signature"/> must be the very bytes that went out
    /// on the wire, or the one comparison this record enables is quietly worthless.</summary>
    public void Sample(ulong wattHours, long unixSeconds,
                       string? signature = null, string? meterPublicKey = null)
    {
        values.Add(new OcppMeterValue(
            DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime
                          .ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture),
            [new OcppSampledValue(
                wattHours.ToString(CultureInfo.InvariantCulture),
                Context:       "Sample.Periodic",
                Measurand:     "Energy.Active.Import.Register",
                UnitOfMeasure: new OcppUnitOfMeasure("Wh"),
                Location:      "Outlet",
                SignedMeterValue: signature is null || meterPublicKey is null
                                      ? null
                                      : new OcppSignedMeterValue(signature,
                                                                 SigningMethod:  "ECDSA-secp256r1-SHA256",
                                                                 EncodingMethod: "V2G-METER-1",
                                                                 PublicKey:      meterPublicKey))]));
    }

    /// <summary>The record, with the last reading marked as the transaction's final one.</summary>
    /// <remarks>
    /// Marked at the end rather than when it is taken, because a station does not know which sample
    /// is the last until the session stops — and "the billed figure is the final reading" has to be
    /// a statement in the record rather than an inference from array position.
    /// </remarks>
    public OcppTransactionRecord Build()
    {
        var meterValues = values.ToList();

        if (meterValues.Count > 0)
        {
            var last = meterValues[^1];
            meterValues[^1] = last with
            {
                SampledValue = last.SampledValue
                                   .Select(v => v with { Context = "Transaction.End" })
                                   .ToList(),
            };
        }

        return new OcppTransactionRecord(transactionId, evseId, v2gSessionId, meterValues);
    }

}
