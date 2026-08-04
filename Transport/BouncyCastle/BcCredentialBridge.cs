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

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Tls;

namespace Vanaheimr.V2G.Simulation.Transport.BouncyCastle
{
    /// <summary>
    /// Converts .NET certificate material (<see cref="X509Certificate2"/> + optional intermediate chain)
    /// into <see cref="BcTlsCredentials"/> for the BouncyCastle backend. Needed by the macOS TLS 1.3
    /// fallback (<see cref="TlsPlatform"/>), which has to hand a <see cref="TlsOptions"/>-configured
    /// session to <see cref="BcTlsTransport"/> — the -20-faithful path feeds BouncyCastle objects
    /// straight from the PKI builder and does not need this bridge.
    /// <para>
    /// <b>Requires an exportable private key.</b> Test certificates (generated in-process, or imported
    /// from an in-memory PKCS#12) are exportable; a key held by the macOS Keychain or an HSM is not, and
    /// there is no way around that — the BouncyCastle stack signs in managed code and therefore needs the
    /// raw key. Such a certificate fails loudly here rather than silently degrading.
    /// </para>
    /// </summary>
    public static class BcCredentialBridge
    {
        /// <summary>
        /// Builds credentials from a leaf certificate and the intermediates to send alongside it.
        /// The chain is emitted leaf-first, matching <see cref="BcTlsCredentials.CertificateChain"/>.
        /// </summary>
        public static BcTlsCredentials FromX509(X509Certificate2 leaf, X509Certificate2Collection? intermediates = null)
        {
            ArgumentNullException.ThrowIfNull(leaf);

            var chain = new List<byte[]> { leaf.RawData };
            if (intermediates is not null)
                foreach (var intermediate in intermediates)
                    chain.Add(intermediate.RawData);

            var (privateKey, signatureScheme) = ExtractPrivateKey(leaf);
            return new BcTlsCredentials(chain.ToArray(), privateKey, signatureScheme);
        }

        // The TLS 1.3 signature scheme follows the leaf's curve: the certificate and the scheme it signs
        // with are one unit (docs/pki-model.md — "the certificate-chain curve must match the negotiated
        // TLS profile"), so deriving it here rather than letting a caller pick keeps them from drifting.
        private static (AsymmetricKeyParameter, int) ExtractPrivateKey(X509Certificate2 leaf)
        {
            using var ecdsa = leaf.GetECDsaPrivateKey()
                ?? throw new NotSupportedException(
                       $"Certificate '{leaf.Subject}' carries no ECDSA private key. The BouncyCastle backend " +
                       "signs in managed code and needs the raw key; RSA and Ed448 are not bridged (Ed448 has no " +
                       "X509Certificate2 accessor — feed the PKI builder's BouncyCastle objects directly instead).");

            var signatureScheme = ecdsa.KeySize switch
            {
                256 => SignatureScheme.ecdsa_secp256r1_sha256,
                384 => SignatureScheme.ecdsa_secp384r1_sha384,
                521 => SignatureScheme.ecdsa_secp521r1_sha512,
                var bits => throw new NotSupportedException(
                                $"Certificate '{leaf.Subject}' uses an unsupported ECDSA key size ({bits} bit); " +
                                "expected 256, 384 or 521.")
            };

            byte[] pkcs8;
            try
            {
                pkcs8 = ecdsa.ExportPkcs8PrivateKey();
            }
            catch (CryptographicException ex)
            {
                throw new NotSupportedException(
                    $"The private key of '{leaf.Subject}' is not exportable, so it cannot be handed to the " +
                    "BouncyCastle TLS backend. Use a certificate whose key material is available in process " +
                    "(a test certificate, or an in-memory PKCS#12 import).", ex);
            }

            return (PrivateKeyFactory.CreateKey(pkcs8), signatureScheme);
        }
    }
}
