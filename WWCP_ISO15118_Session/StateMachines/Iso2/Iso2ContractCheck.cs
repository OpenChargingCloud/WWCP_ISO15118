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
using System.Security.Cryptography.X509Certificates;

using cloud.charging.open.protocols.ISO15118.Session;
using cloud.charging.open.protocols.ISO15118_2;
using cloud.charging.open.protocols.ISO15118_2.Generated;

namespace cloud.charging.open.protocols.ISO15118.StateMachines.Iso2
{

    /// <summary>The five fields a <c>CertificateInstallationRes</c> and a <c>CertificateUpdateRes</c> both
    /// carry, in the same order, and which everything downstream of the response actually reads.</summary>
    /// <param name="ProvisioningChain">The station's SA provisioning chain — whose leaf key signed the
    /// response, so this is both a payload and the verify key for the signature over it.</param>
    /// <param name="ContractChain">The issued contract certificate and its MO sub-CAs.</param>
    /// <param name="EncryptedKey">IV(16) ‖ AES-128-CBC ciphertext(32) of the contract's private scalar.</param>
    /// <param name="DhPublicKey">The station's ephemeral P-256 point, uncompressed (65 B).</param>
    /// <param name="Emaid">The identity the contract was issued under.</param>
    public sealed record Iso2ProvisioningPayload(CertificateChainType                     ProvisioningChain,
                                                 CertificateChainType                     ContractChain,
                                                 ContractSignatureEncryptedPrivateKeyType EncryptedKey,
                                                 DiffieHellmanPublickeyType               DhPublicKey,
                                                 EMAIDType                                Emaid,
                                                 ResponseCode                             ResponseCode);

    /// <summary>An EVCC's verdict over a contract-provisioning response (§7.9.2.4.2).</summary>
    /// <param name="SignaturePresent">The response header carried a Signature at all. A station that sends
    /// none has issued a contract nobody vouched for.</param>
    /// <param name="References">How many References the SignedInfo carried. §7.9.2.4.2 asks for exactly
    /// four, and reporting the count separately is what distinguishes "one digest is wrong" from "the
    /// station only signed some of what it sent" — two different failures with one boolean between
    /// them.</param>
    /// <param name="DigestOk">All four references were present, each matched by URI to its element's Id,
    /// and each digest equals the SHA-256 of that element's own EXI fragment. Answerable without any key.
    /// <b>False when <see cref="References"/> is not four</b>, deliberately: three sound digests are not a
    /// signed response, they are a signed part of one.</param>
    /// <param name="SignatureOk">The ECDSA signature over the SignedInfo verified against the leaf of the
    /// provisioning chain the response itself carried.</param>
    /// <param name="SignatureGrammar">Which grammar matched: <c>iso2-msgdef</c>, <c>xmldsig-standalone</c>,
    /// or <c>none</c> when neither did or none was attempted.</param>
    public sealed record Iso2ContractVerdict(bool SignaturePresent, int References, bool DigestOk,
                                             bool SignatureOk, string SignatureGrammar);

    /// <summary>
    /// §7.9.2.4.2 — what an EVCC must make of the answer to its <c>CertificateInstallationReq</c> or
    /// <c>CertificateUpdateReq</c>, on its own so that something other than a live session can ask.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lifted out of <c>Evcc2.VerifyProvisioningSignature</c> for the reason <see cref="Iso2TariffCheck"/>
    /// was lifted out of the schedule evaluation: the verdict never reaches the wire. The car checks the
    /// response and tells the station nothing about the result, so a recorded session proves a port can
    /// <i>parse</i> a provisioning response and cannot prove it <i>judges</i> one. <c>Contract.provisioning.vectors.json</c>
    /// carries the cases that decide it, negatives included; the ports mirror this file rather than
    /// <see cref="Evcc2"/>.
    /// </para>
    /// <para>
    /// <b>Four references, and all four have to hold.</b> Every other signed -2 message has one. Here the
    /// contract chain, the encrypted private key, the DH public point and the eMAID are each digested
    /// separately, and a car that checked only the chain would accept an encrypted key nobody signed for —
    /// which is to say it would install a private key of the attacker's choosing under a certificate the
    /// operator really did issue.
    /// </para>
    /// <para>
    /// <b>This is only half of what makes a contract usable.</b> The other half is that the unwrapped key
    /// belongs to the certificate it arrived with, and it lives in <see cref="ContractProvisioning.Matches"/>
    /// rather than here because it needs the car's own private key and this class deliberately needs
    /// nothing but the response. -2 wraps with AES-CBC, which authenticates nothing, so that check is the
    /// only thing standing between a wrong unwrap and a car that carries on believing it holds a contract.
    /// </para>
    /// </remarks>
    public static class Iso2ContractCheck
    {

        /// <summary>What §7.9.2.4.2 asks the station to sign: the contract chain, the encrypted key, the DH
        /// point, and the eMAID.</summary>
        public const int RequiredReferences = 4;

        /// <summary>The common fields of either response, or a throw naming what arrived instead. The two
        /// messages differ in almost nothing but the update's trailing RetryCounter, which no verifier
        /// reads.</summary>
        public static Iso2ProvisioningPayload Unpack(BodyBaseType body)
            => body switch
            {
                CertificateInstallationResType r => new(r.SAProvisioningCertificateChain, r.ContractSignatureCertChain,
                                                        r.ContractSignatureEncryptedPrivateKey, r.DHpublickey,
                                                        r.EMAID, r.ResponseCode),
                CertificateUpdateResType r       => new(r.SAProvisioningCertificateChain, r.ContractSignatureCertChain,
                                                        r.ContractSignatureEncryptedPrivateKey, r.DHpublickey,
                                                        r.EMAID, r.ResponseCode),
                _ => throw new SessionAborted($"contract provisioning: unexpected response {body.GetType().Name}."),
            };

        /// <summary>
        /// Evaluates a provisioning response against the header signature it arrived with. No verify key is
        /// passed in — unlike every other check in this namespace — because the station sends its own: the
        /// signature is made by the leaf of <c>SAProvisioningCertificateChain</c>, which travels in the
        /// message. What makes that chain trustworthy is a separate question, and one the trust store
        /// answers.
        /// </summary>
        public static Iso2ContractVerdict Evaluate(BodyBaseType response, SignatureType? headerSignature)
        {

            if (headerSignature is not { } sig)
                return new Iso2ContractVerdict(false, 0, false, false, "none");

            var payload    = Unpack(response);
            var references = sig.SignedInfo.Reference.Count;

            // (1) the four digests, each over its own element's EXI fragment. A reference count other than
            //     four fails here rather than being read as "the ones present are fine".
            var buf = new byte[4096];
            var digestOk =
                references == RequiredReferences &&
                Matches(payload.ContractChain.Id, Iso2Codec.EncodeFragment_ContractSignatureCertChain(payload.ContractChain, buf, out int n1), buf, n1) &&
                Matches(payload.EncryptedKey.Id, Iso2Codec.EncodeFragment_ContractSignatureEncryptedPrivateKey(payload.EncryptedKey, buf, out int n2), buf, n2) &&
                Matches(payload.DhPublicKey.Id,  Iso2Codec.EncodeFragment_DHpublickey(payload.DhPublicKey, buf, out int n3), buf, n3) &&
                Matches(payload.Emaid.Id,        Iso2Codec.EncodeFragment_eMAID(payload.Emaid, buf, out int n4), buf, n4);

            // (2) the ECDSA signature over the SignedInfo, against the chain the station sent. Both
            //     grammars, and which one matched is reported for the reason Iso2TariffCheck reports it:
            //     "it verified" and "it verified the way the standard says" are different facts.
            try
            {
                using var provisioningLeaf = X509CertificateLoader.LoadCertificate(payload.ProvisioningChain.Certificate);
                using var verifyKey = provisioningLeaf.GetECDsaPublicKey();

                if (verifyKey is null)
                    return new Iso2ContractVerdict(true, references, digestOk, false, "none");

                if (V2GSignature.Verify(sig.SignedInfo, sig.SignatureValue.Value, verifyKey))
                    return new Iso2ContractVerdict(true, references, digestOk, true, "iso2-msgdef");

                if (XmlDsigInterop2.VerifyStandaloneXmldsig(sig.SignedInfo, sig.SignatureValue.Value, verifyKey))
                    return new Iso2ContractVerdict(true, references, digestOk, true, "xmldsig-standalone");
            }
            catch (CryptographicException)
            {
                // An unparseable provisioning certificate is a signature that cannot be established, not
                // one that failed — and either way the car must not install what it cannot check.
            }

            return new Iso2ContractVerdict(true, references, digestOk, false, "none");


            bool Matches(string? id, bool encoded, byte[] buffer, int length)
            {
                if (!encoded || id is null)
                    return false;
                var reference = sig.SignedInfo.Reference.FirstOrDefault(r => r.URI == "#" + id);
                return reference is not null && V2GSignature.VerifyReference(reference, buffer.AsSpan(0, length));
            }

        }

    }

}
