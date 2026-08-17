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
using System.Security.Cryptography;

using cloud.charging.open.protocols.ISO15118_20.CommonMessages;
using cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated;

namespace cloud.charging.open.protocols.ISO15118.StateMachines.Iso20
{

    /// <summary>
    /// The §-20 counterpart of <see cref="Iso2.Iso2TariffCheck"/>: a signed <c>AbsolutePriceSchedule</c>
    /// in a <c>ScheduleExchangeRes</c>, and what an EVCC should make of it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lifted out of <c>Evcc20Base.VerifyPriceSchedule</c> for the reason its -2 sibling was: inline, the
    /// only way to exercise it was a whole session against a station configured to sign, and the verdict
    /// never reaches the wire — the EV checks the schedule and tells the station nothing. So no recorded
    /// trace can pin it, and <c>PriceScheduleSignatureCorpusTests</c> does instead.
    /// </para>
    /// <para>
    /// <b>Two places carry the schedule, and both are the same check.</b> Scheduled mode hangs an
    /// <c>AbsolutePriceSchedule</c> off each schedule tuple's ChargingSchedule; Dynamic mode has no tuples
    /// and carries one directly on the control mode. A verifier that looks in only one of them reports an
    /// unsigned offer for half the sessions in the field, which is indistinguishable on screen from a
    /// station that does not sign.
    /// </para>
    /// <para>
    /// <b>One reference, and no dual grammar.</b> Unlike the -2 tariff check — and unlike this repository's
    /// own -20 <i>SECC</i>, which tries both grammars when verifying a PnC signature — this path attempts
    /// ISO's grammar alone. That is deliberate rather than forgotten: the counterparty that signs under
    /// Josev's standalone grammar does not sign price schedules at all (its SECC never emits one), so a
    /// second attempt here would be code no counterparty exercises. It is written down because the
    /// asymmetry is otherwise the kind of thing that reads as an oversight.
    /// </para>
    /// </remarks>
    public static class Iso20PriceScheduleCheck
    {

        /// <summary>The signed price schedule a response carries, from whichever control mode holds it, or
        /// <c>null</c> when the offer has none to check.</summary>
        public static AbsolutePriceScheduleType? ScheduleIn(ScheduleExchangeRes res) =>
            res.Dynamic_SEResControlMode?.AbsolutePriceSchedule is { Id: not null } dynamicPrice
                ? dynamicPrice
                : res.Scheduled_SEResControlMode?.ScheduleTuple
                      .Select(t => t.ChargingSchedule.AbsolutePriceSchedule)
                      .FirstOrDefault(p => p?.Id is not null);

        /// <summary>
        /// Evaluates a response's price schedule. Returns <c>null</c> when there is no signed schedule at
        /// all — which is not a failure and must not be reported as one: most stations never sign, and
        /// Josev's SECC emits no <c>AbsolutePriceSchedule</c> whatsoever.
        /// </summary>
        /// <param name="verifyKey">May be <c>null</c>: the digest half is still answered, and the signature
        /// half honestly reports "not established" rather than "failed".</param>
        public static Iso20TariffResult? Evaluate(ScheduleExchangeRes res,
                                                  SignatureType? headerSignature,
                                                  ECDsa? verifyKey)
        {

            if (ScheduleIn(res) is not { } priceSchedule)
                return null;

            var signaturePresent = headerSignature is not null;
            if (!signaturePresent)
                return new Iso20TariffResult(false, false, false);

            var sig = headerSignature!;
            var buf = new byte[4096];

            // One reference, matched by the schedule's own Id. A signature that references something else
            // covers something else, however well its ECDSA verifies.
            var reference = sig.SignedInfo.Reference.FirstOrDefault(r => r.URI == "#" + priceSchedule.Id);

            var digestOk = reference is not null
                        && CommonMessagesCodec.EncodeFragment_AbsolutePriceSchedule(priceSchedule, buf, out int n)
                        && V2GSignature.VerifyReference(reference, buf.AsSpan(0, n));

            var signatureOk = verifyKey is not null
                           && V2GSignature.Verify(sig.SignedInfo, sig.SignatureValue.Value, verifyKey);

            return new Iso20TariffResult(true, digestOk, signatureOk);

        }

    }

}
