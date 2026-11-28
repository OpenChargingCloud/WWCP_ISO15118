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

using System.Buffers.Binary;
using System.Text;

namespace cloud.charging.open.protocols.ISO15118.Metering;

/// <summary>
/// The octets a simulated meter signs into <c>MeterInfo.SigMeterReading</c> (-2) or
/// <c>MeterInfo.MeterSignature</c> (-20).
/// </summary>
/// <remarks>
/// <para>
/// <b>This layout is ours, not the standard's, and that has to be said plainly.</b> ISO 15118
/// defines the <em>field</em> — <c>xs:base64Binary</c>, <c>maxLength 64</c>, which is exactly one
/// raw ECDSA P-256 <c>r‖s</c> pair — and says nothing about what the signature covers. The field
/// exists so the <em>meter</em>, not the SECC and not the CPO backend, can sign its own reading and
/// hand it to the vehicle; it is almost never populated in the field, so there is no de-facto
/// convention to follow either.
/// </para>
/// <para>
/// So this is a convention invented for the simulator, and a real Eichrecht meter would use OCMF or
/// similar. It is documented rather than merely implemented because the app has to reproduce it
/// byte for byte to verify anything, and because "the signature did not check out" is a miserable
/// thing to debug when the two sides disagree about what was signed rather than about the crypto.
/// </para>
/// <para>
/// What the full transparency record needs — meter public key, serial, OCMF envelope, tariff — does
/// not fit in 64 bytes and never will. That needs a side channel, and -20's VAS is the right one
/// (<c>EVSimulatorApp/docs/CONCEPT.md</c> §4.3). This covers the part that fits.
/// </para>
///
/// ## Layout
///
/// <code>
/// "V2G-METER-1\0"      12 bytes, domain separator
/// protocol              1 byte   2 or 20
/// sessionId             8 bytes  the V2G session, verbatim
/// meterIdLength         1 byte
/// meterId               n bytes  UTF-8
/// reading               8 bytes  big-endian, Wh
/// timestamp             8 bytes  big-endian, unix seconds; 0 when absent
/// </code>
///
/// <para>
/// Three properties are deliberate. The <b>domain separator</b> stops a signature over the same
/// numbers in some other context being replayed in as a meter reading. The <b>session id</b> binds
/// the reading to one session, so a signed value captured from another cannot be presented here —
/// without it the signature proves the reading is genuine but not that it is <em>yours</em>. And
/// every variable-length field is <b>length-prefixed</b>, so no two different readings can produce
/// the same octets: with plain concatenation, meter <c>"A1"</c> reading 23 and meter <c>"A"</c>
/// reading 123 could collide.
/// </para>
///
/// ## Layout 2, for a bidirectional meter (since 2026-11-28)
///
/// <code>
/// "V2G-METER-2\0"      12 bytes, domain separator
/// protocol              1 byte   20 (-2 has no discharge register)
/// sessionId             8 bytes
/// meterIdLength         1 byte
/// meterId               n bytes  UTF-8
/// reading               8 bytes  big-endian, Wh charged (ChargedEnergyReadingWh)
/// discharged            8 bytes  big-endian, Wh discharged (BPT_DischargedEnergyReadingWh)
/// timestamp             8 bytes  big-endian, unix seconds; 0 when absent
/// </code>
///
/// <para>
/// Used exactly when the reading carries <c>BPT_DischargedEnergyReadingWh</c>, which a station with a
/// meter sends in every reading of a bidirectional session. Layout 1 stays what it was for everything
/// else, so no signature made before stops verifying. The separator differs, so a layout-1 signature
/// can never be read as a layout-2 one or the other way round: a verifier picks the layout by whether
/// the discharge register is present, and a reading with the register stripped no longer verifies.
/// Signing both registers is the point. A discharged figure beside a signature over the charged one
/// alone would be an unsigned number that looks signed, and the discharge direction is the one in
/// which the driver is the seller.
/// </para>
/// </remarks>
public static class MeterSigningPayload
{
    private static ReadOnlySpan<byte> DomainSeparator => "V2G-METER-1\0"u8;

    private static ReadOnlySpan<byte> DomainSeparatorBidirectional => "V2G-METER-2\0"u8;

    /// <summary>The maximum id length the layout can express.</summary>
    public const int MaxMeterIdBytes = 255;

    /// <summary>Builds the octets to sign or verify.</summary>
    /// <param name="protocol">2 or 20 — the two encodings differ, so a reading cannot cross over.</param>
    /// <param name="sessionId">The V2G session id, exactly as it appears in the header.</param>
    /// <param name="reading">Energy in Wh: <c>MeterReading</c> (-2) or <c>ChargedEnergyReadingWh</c> (-20).</param>
    /// <param name="timestamp">Unix seconds, or null — <c>TMeter</c> (-2) or <c>MeterTimestamp</c> (-20).</param>
    public static byte[] Build(int protocol, ReadOnlySpan<byte> sessionId, string meterId,
                               ulong reading, long? timestamp)
        => Build(protocol, sessionId, meterId, reading, discharged: null, timestamp);

    /// <summary>Builds the octets to sign or verify, in layout 2 when <paramref name="discharged"/> is
    /// present and in layout 1 otherwise.</summary>
    /// <param name="discharged">Energy discharged in Wh, <c>BPT_DischargedEnergyReadingWh</c>: present
    /// exactly when the reading carries it. Only -20 has the register.</param>
    public static byte[] Build(int protocol, ReadOnlySpan<byte> sessionId, string meterId,
                               ulong reading, ulong? discharged, long? timestamp)
    {
        if (protocol is not (2 or 20))
            throw new ArgumentOutOfRangeException(nameof(protocol), protocol, "protocol must be 2 or 20");

        if (discharged is not null && protocol != 20)
            throw new ArgumentException("only -20 has a discharged-energy register", nameof(discharged));

        var id = Encoding.UTF8.GetBytes(meterId);
        if (id.Length > MaxMeterIdBytes)
            throw new ArgumentException($"meter id is {id.Length} bytes; the layout allows {MaxMeterIdBytes}",
                                        nameof(meterId));

        var separator = discharged is null ? DomainSeparator : DomainSeparatorBidirectional;
        var payload   = new byte[separator.Length + 1 + sessionId.Length + 1 + id.Length + 8
                                 + (discharged is null ? 0 : 8) + 8];
        var at = 0;

        separator.CopyTo(payload.AsSpan(at));
        at += separator.Length;

        payload[at++] = (byte) protocol;

        sessionId.CopyTo(payload.AsSpan(at));
        at += sessionId.Length;

        payload[at++] = (byte) id.Length;
        id.CopyTo(payload.AsSpan(at));
        at += id.Length;

        BinaryPrimitives.WriteUInt64BigEndian(payload.AsSpan(at), reading);
        at += 8;

        if (discharged is { } exported)
        {
            BinaryPrimitives.WriteUInt64BigEndian(payload.AsSpan(at), exported);
            at += 8;
        }

        // Absent is encoded as 0 rather than omitted, so the payload length never depends on which
        // optional fields happen to be present — one fewer way for two readings to share octets.
        BinaryPrimitives.WriteInt64BigEndian(payload.AsSpan(at), timestamp ?? 0);

        return payload;
    }
}
