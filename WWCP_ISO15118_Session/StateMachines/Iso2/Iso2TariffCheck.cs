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

using cloud.charging.open.protocols.ISO15118_2;
using cloud.charging.open.protocols.ISO15118_2.Generated;

namespace cloud.charging.open.protocols.ISO15118.StateMachines.Iso2
{

    /// <summary>The signature half of an EVCC's verdict over a SASchedule offer (§7.9.2.5).</summary>
    /// <param name="SignaturePresent">The header carried a Signature <b>and</b> at least one tuple carried a
    /// SalesTariff with an Id to reference. Neither alone is a signed offer.</param>
    /// <param name="DigestOk">Every signed tariff has a matching Reference whose DigestValue equals the
    /// SHA-256 of that tariff's own EXI fragment. Answerable without any key.</param>
    /// <param name="SignatureOk">The ECDSA signature over the SignedInfo verified. <c>false</c> also means
    /// "not attempted" when no verify key was supplied — see <see cref="SignatureGrammar"/> to tell
    /// the two apart in the reporting.</param>
    /// <param name="SignatureGrammar">Which grammar the signature matched under: <c>iso2-msgdef</c>,
    /// <c>xmldsig-standalone</c>, or <c>none</c> when it matched neither or was never attempted.</param>
    public sealed record Iso2TariffSignatureVerdict(bool SignaturePresent, bool DigestOk,
                                                    bool SignatureOk, string SignatureGrammar);

    /// <summary>
    /// §7.9.2.5, on its own so that something other than a live session can ask the question.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This was inline in <see cref="Evcc2"/> until 2026-08-17, which meant the only way to exercise it was
    /// to run a whole session against a station configured to sign — and no recorded trace does, because the
    /// verdict never reaches the wire. Extracted so <c>TariffSignatureCorpusTests</c> can hold C# to the same
    /// corpus the Swift and Kotlin ports are held to. The ports mirror this file, not <see cref="Evcc2"/>.
    /// </para>
    /// <para>
    /// <b>The two halves answer different questions and are deliberately not collapsed.</b> The digest says
    /// "these are the tariffs that were signed"; the signature says "and the Mobility Operator signed them".
    /// A verifier that checks only the signature accepts an offer whose tariffs were edited after signing —
    /// the signature covers the SignedInfo, not the tariffs — and one that checks only the digests accepts
    /// anything a station cares to hash. The corpus carries a case for each.
    /// </para>
    /// </remarks>
    public static class Iso2TariffCheck
    {

        /// <summary>Evaluates a received offer. <paramref name="verifyKey"/> may be <c>null</c>: the digest
        /// half is still answered, and the signature half honestly reports "not established".</summary>
        public static Iso2TariffSignatureVerdict Evaluate(SAScheduleListType? offer,
                                                          SignatureType? headerSignature,
                                                          ECDsa? verifyKey)
        {

            var signedTariffs = offer?.SAScheduleTuple
                                    .Where(t => t.SalesTariff?.Id is not null)
                                    .Select(t => t.SalesTariff!)
                                    .ToList()
                                ?? [];

            var signaturePresent = headerSignature is not null && signedTariffs.Count > 0;
            if (!signaturePresent)
                return new Iso2TariffSignatureVerdict(false, false, false, "none");

            // (1) one reference per SalesTariff Id, each over that tariff's own EXI fragment.
            var digestOk = true;
            var buf = new byte[2048];
            foreach (var tariff in signedTariffs)
            {
                var reference = headerSignature!.SignedInfo.Reference
                                    .FirstOrDefault(r => r.URI == "#" + tariff.Id);
                digestOk &= reference is not null
                         && Iso2Codec.EncodeFragment_SalesTariff(tariff, buf, out int n)
                         && V2GSignature.VerifyReference(reference, buf.AsSpan(0, n));
            }

            // (2) the ECDSA signature over the SignedInfo — ISO's grammar first, then Josev's. Reporting
            //     which one matched is the point: "it verified" and "it verified the way the standard says"
            //     are different facts, and only one of them is a conformance statement.
            if (verifyKey is null)
                return new Iso2TariffSignatureVerdict(true, digestOk, false, "none");

            var sig = headerSignature!;
            if (V2GSignature.Verify(sig.SignedInfo, sig.SignatureValue.Value, verifyKey))
                return new Iso2TariffSignatureVerdict(true, digestOk, true, "iso2-msgdef");

            if (XmlDsigInterop2.VerifyStandaloneXmldsig(sig.SignedInfo, sig.SignatureValue.Value, verifyKey))
                return new Iso2TariffSignatureVerdict(true, digestOk, true, "xmldsig-standalone");

            return new Iso2TariffSignatureVerdict(true, digestOk, false, "none");

        }

    }

}
