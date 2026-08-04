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

namespace Vanaheimr.V2G.Simulation.Metering;

/// <summary>
/// A simulated meter that signs its own readings, so <c>SigMeterReading</c> / <c>MeterSignature</c>
/// carry something a vehicle can actually check.
/// </summary>
/// <remarks>
/// <para>
/// The point is not the energy model, which is a straight line. It is that these fields get
/// populated at all: they exist so the <b>meter</b> — not the SECC, not the backend — can sign what
/// it measured, which is the Eichrecht trust model transported over stock ISO 15118, and they are
/// almost never used. A phone that displays and verifies one would be novel on its own
/// (<c>docs/CONCEPT.md</c> §4.3).
/// </para>
/// <para>
/// So the key here is deliberately <b>not</b> the SECC's TLS key or its contract key. A meter that
/// signs with the station's identity proves the station said it, which is the claim the field
/// exists to improve on. Keeping it separate is what makes the three-way comparison in §4.3 mean
/// anything: EV model against <em>meter-signed</em> reading against OCPP MeterValues.
/// </para>
/// <para>
/// <b>P-256 and raw <c>r‖s</c>, for both protocols.</b> Not a choice — 64 bytes is the field's
/// <c>maxLength</c>, and that is exactly r and s at 32 bytes each. -20's stronger suite does not
/// apply here: it governs the XMLDSig signature over message fragments, while this field is a fixed
/// 64-byte slot in a data type. A DER signature does not fit, which for once means the usual trap
/// fails loudly instead of quietly.
/// </para>
/// </remarks>
public sealed class SigningMeter
{
    private readonly ECDsa key;
    private readonly TimeProvider clock;

    /// <summary>Watt-hours accumulated so far.</summary>
    private ulong energyWh;

    public SigningMeter(string meterId, ECDsa key, TimeProvider clock)
    {
        if (key.KeySize != 256)
            throw new ArgumentException(
                $"the meter key is P-{key.KeySize}; SigMeterReading holds 64 bytes, which is P-256 r‖s",
                nameof(key));

        MeterId    = meterId;
        this.key   = key;
        this.clock = clock;
    }

    public string MeterId { get; }

    /// <summary>
    /// The meter's public key, for the app to verify with. This is what the pairing code's
    /// <c>meter</c> field carries, so a vehicle can check readings from first contact.
    /// </summary>
    public ECDsa PublicKey => ECDsa.Create(key.ExportParameters(includePrivateParameters: false));

    /// <summary>Advances the meter. A simulator needs the reading to move for the demo to mean anything.</summary>
    public void Add(ulong wattHours) => energyWh += wattHours;

    /// <summary>The current reading and the moment it was taken.</summary>
    public (ulong Wh, long Timestamp) Read() => (energyWh, clock.GetUtcNow().ToUnixTimeSeconds());

    /// <summary>
    /// Signs a reading for one session, returning the 64 raw bytes the field takes.
    /// </summary>
    public byte[] Sign(int protocol, ReadOnlySpan<byte> sessionId, ulong reading, long? timestamp)
    {
        var payload = MeterSigningPayload.Build(protocol, sessionId, MeterId, reading, timestamp);

        // IeeeP1363 is the raw r‖s form. The default here is DER, which would be 70-ish bytes and
        // would not fit the field at all — see the class remarks.
        return key.SignData(payload, HashAlgorithmName.SHA256,
                            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    }

    /// <summary>Verifies a signed reading — the vehicle's half, and what the app will do.</summary>
    public static bool Verify(ECDsa publicKey, int protocol, ReadOnlySpan<byte> sessionId,
                              string meterId, ulong reading, long? timestamp, byte[] signature)
    {
        if (signature.Length != 64)
            return false;

        var payload = MeterSigningPayload.Build(protocol, sessionId, meterId, reading, timestamp);
        return publicKey.VerifyData(payload, signature, HashAlgorithmName.SHA256,
                                    DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    }
}
