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

namespace cloud.charging.open.protocols.ISO15118.StateMachines.Iso2
{

    /// <summary>Which of the two ISO 15118-2 provisioning exchanges an EVCC runs.</summary>
    public enum Iso2CertificateAction
    {

        /// <summary>The car has no contract and asks for one, signing with its OEM provisioning key.</summary>
        Install,

        /// <summary>The car has a contract that is running out and asks for a fresh one, signing with the
        /// expiring contract's key — which is also the key the answer is encrypted for.</summary>
        Update,

    }

    /// <summary>
    /// The credentials that make an <see cref="Evcc2"/> ask for a contract: when set, and the station
    /// advertises the certificate service, the EVCC selects that service and runs a
    /// <c>CertificateInstallationReq</c> or <c>CertificateUpdateReq</c> before PaymentDetails, then verifies
    /// the four-reference response signature and ECDH-unwraps the issued contract private key.
    /// </summary>
    /// <remarks>
    /// One record for both exchanges, because in -2 they differ in almost nothing but which credential
    /// plays the part: an installation presents the OEM provisioning certificate, an update presents the
    /// contract about to expire. Both sign their own request and both have the answer wrapped for the same
    /// key they signed with — which is exactly why an update needs no other proof of who is asking.
    /// </remarks>
    /// <param name="Certificate">The certificate to present (DER): the OEM provisioning one for an
    /// installation, the expiring contract for an update. Must carry a <b>P-256</b> key — that is the only
    /// curve the -2 key transport can wrap for.</param>
    /// <param name="SignKey">That certificate's private key, for signing the request.</param>
    /// <param name="KeyAgreement">The same private key as an ECDH handle, for unwrapping the answer.</param>
    /// <param name="Action">Which exchange to run.</param>
    /// <param name="Emaid">The contract identity, required for <see cref="Iso2CertificateAction.Update"/>
    /// (the message carries it) and unused for an installation, where the operator picks it.</param>
    /// <param name="SubCertificates">The MO sub-CAs of the expiring contract, for an update; -2's
    /// installation message has nowhere to put a chain and carries the OEM certificate alone.</param>
    public sealed record Iso2CertInstallOptions(
        Byte[]                    Certificate,
        ECDsa                     SignKey,
        ECDiffieHellman           KeyAgreement,
        Iso2CertificateAction     Action           = Iso2CertificateAction.Install,
        String?                   Emaid            = null,
        IReadOnlyList<Byte[]>?    SubCertificates  = null);

}
