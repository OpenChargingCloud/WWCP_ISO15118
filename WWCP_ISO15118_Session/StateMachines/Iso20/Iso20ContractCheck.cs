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
using System.Security.Cryptography.X509Certificates;

using cloud.charging.open.protocols.ISO15118_20.CommonMessages;
using cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated;

namespace cloud.charging.open.protocols.ISO15118.StateMachines.Iso20
{

    /// <summary>An EVCC's verdict over a <c>CertificateInstallationRes</c>.</summary>
    /// <param name="SignaturePresent">The response header carried a Signature at all.</param>
    /// <param name="References">How many References the SignedInfo carried. -20 signs one element here, so
    /// anything but one is a station signing something other than what it sent.</param>
    /// <param name="DigestOk">The reference named by the <c>SignedInstallationData</c>'s own Id carries the
    /// SHA-512 of that element's EXI fragment. Answerable without any key.</param>
    /// <param name="SignatureOk">The ECDSA signature over the SignedInfo verified against the leaf of the
    /// CPS chain the response itself carried.</param>
    public sealed record Iso20ContractVerdict(bool SignaturePresent, int References,
                                              bool DigestOk, bool SignatureOk);

    /// <summary>
    /// The -20 counterpart of <see cref="Iso2.Iso2ContractCheck"/>: what an EVCC must make of the answer to
    /// its <c>CertificateInstallationReq</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lifted out of <c>Evcc20Base.RunCertificateInstallationAsync</c> for the reason its siblings were
    /// lifted: the verdict never reaches the wire. The car reads the response, decides, and tells the
    /// station nothing — so a recorded session proves a port can parse one and cannot prove it judges one.
    /// <c>Contract.provisioning.vectors.json</c> carries the cases that decide it.
    /// </para>
    /// <para>
    /// <b>One reference where -2 has four.</b> Not a weaker signature: -20 folds the contract chain, the
    /// curve, the DH point and the wrapped key into a single <c>SignedInstallationData</c> element and signs
    /// that, so one digest covers everything -2 needs four to cover. The eMAID is the one thing -2 signs and
    /// -20 does not — it has no eMAID field here at all; the identity travels inside the issued certificate.
    /// </para>
    /// <para>
    /// <b>Matched by Id, not by position.</b> The inline version read <c>Reference[0]</c>, which accepts a
    /// signature whose one reference names some other element entirely. Every sound response is unaffected —
    /// our SECC emits <c>#sid1</c> as its only reference — and a malformed one is now refused rather than
    /// verified against the wrong thing, which is the same rule <see cref="Iso20PriceScheduleCheck"/> already
    /// applies to a price schedule.
    /// </para>
    /// <para>
    /// <b>ISO's grammar alone.</b> As with <see cref="Iso20PriceScheduleCheck"/>, and for the same reason:
    /// the counterparty that signs under Josev's standalone grammar does not implement -20 provisioning at
    /// all (<c>NotImplementedError</c> on both sides), so a second attempt here would be code no counterparty
    /// exercises. The <i>request</i> direction is different — our SECC does try both there, because a foreign
    /// EVCC really can arrive signing Josev-style.
    /// </para>
    /// </remarks>
    public static class Iso20ContractCheck
    {

        /// <summary>Evaluates a certificate-installation response against the header signature it arrived
        /// with. No verify key is passed in: the station sends its own CPS chain, and the leaf of that chain
        /// is what signed. Whether the chain deserves trust is the trust store's question, not this one's.
        /// </summary>
        public static Iso20ContractVerdict Evaluate(CertificateInstallationRes res, SignatureType? headerSignature)
        {

            if (headerSignature is not { } sig)
                return new Iso20ContractVerdict(false, 0, false, false);

            var references = sig.SignedInfo.Reference.Count;
            var installData = res.SignedInstallationData;

            var buf = new byte[8192];
            var reference = installData.Id is { } id
                                ? sig.SignedInfo.Reference.FirstOrDefault(r => r.URI == "#" + id)
                                : null;

            var digestOk = reference is not null
                        && CommonMessagesCodec.EncodeFragment_SignedInstallationData(installData, buf, out int n)
                        && V2GSignature.VerifyReference(reference, buf.AsSpan(0, n));

            var signatureOk = false;
            try
            {
                using var cpsLeaf = X509CertificateLoader.LoadCertificate(res.CPSCertificateChain.Certificate);
                using var cpsPublicKey = cpsLeaf.GetECDsaPublicKey();
                signatureOk = cpsPublicKey is not null
                           && V2GSignature.Verify(sig.SignedInfo, sig.SignatureValue.Value, cpsPublicKey);
            }
            catch (CryptographicException)
            {
                // An unparseable CPS certificate leaves the signature unestablished rather than failed, and
                // the car must not install what it cannot check either way.
            }

            return new Iso20ContractVerdict(true, references, digestOk, signatureOk);

        }

    }

}
