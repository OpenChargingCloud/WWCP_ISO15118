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

namespace cloud.charging.open.protocols.ISO15118.StateMachines.Iso20
{
    /// <summary>
    /// Contract credentials that switch an <see cref="Evcc20Base"/> from EIM to <b>Plug &amp; Charge</b>
    /// authorization: when set (and the SECC offers PnC with a GenChallenge), the EVCC sends a <b>signed</b>
    /// <c>AuthorizationReq</c> — challenge echo + contract chain + an XMLDSig signature over the
    /// <c>PnC_AReqAuthorizationMode</c> fragment (see <see cref="XmlDsigInteropSign"/>).
    /// </summary>
    /// <param name="ContractCertificate">The contract leaf certificate (DER).</param>
    /// <param name="SubCertificates">The MO sub-CA certificates (DER), leaf-issuer first.</param>
    /// <param name="ContractKey">The contract leaf's private key. -20 asks for secp521r1 ([V2G20-2674]).</param>
    /// <param name="Suite">The algorithms a -20 signature declares: -20's own by default,
    /// <see cref="XmlDsigSuite.JosevSha256"/> for a Josev station. -2 ignores it; its form is fixed.</param>
    public sealed record PncEvccOptions(
        byte[] ContractCertificate,
        IReadOnlyList<byte[]> SubCertificates,
        ECDsa ContractKey,
        XmlDsigSuite Suite = XmlDsigSuite.Iso20);

    /// <summary>
    /// OEM-provisioning credentials that make an <see cref="Evcc20Base"/> request <b>contract provisioning</b>:
    /// when set (and the SECC announces CertificateInstallationService), the EVCC sends a signed
    /// <c>CertificateInstallationReq</c> before its AuthorizationReq — the OEM chain signed over its EXI
    /// fragment (see <see cref="XmlDsigInteropSign"/>) — and processes the response: it verifies the CPS signature over
    /// <c>SignedInstallationData</c> and ECDH-unwraps the issued contract private key
    /// (<see cref="ContractProvisioning.RecoverContractKey"/>). The OEM key must be <b>P-521</b> to take part
    /// in the -20 secp521r1 key agreement.
    /// </summary>
    /// <param name="OemCertificate">The OEM provisioning leaf certificate (DER, P-521).</param>
    /// <param name="OemSubCertificates">The OEM sub-CA certificates (DER), leaf-issuer first.</param>
    /// <param name="OemSignKey">The OEM leaf's private key, for signing the request.</param>
    /// <param name="OemKeyAgreement">The same private key as an ECDH handle, for unwrapping the contract key.</param>
    /// <param name="Suite">The algorithms the request's signature declares, as for <see cref="PncEvccOptions"/>.</param>
    public sealed record CertInstallEvccOptions(
        byte[] OemCertificate,
        IReadOnlyList<byte[]> OemSubCertificates,
        ECDsa OemSignKey,
        ECDiffieHellman OemKeyAgreement,
        XmlDsigSuite Suite = XmlDsigSuite.Iso20);
}
