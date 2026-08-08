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

using cloud.charging.open.protocols.ISO15118.Transport.BouncyCastle;

namespace cloud.charging.open.protocols.ISO15118.SharedCC
{
    /// <summary>
    /// Getting certificate material out of a PKCS#12 file, for both roles.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every `--*-cert` flag on either program lands here. The shapes differ — TLS wants a leaf and its
    /// intermediates, a tariff wants one key, a contract or OEM chain wants leaf-plus-sub-certificates in
    /// file order — but the awkward part is the same each time: a `.p12` is an unordered bag, so "the
    /// leaf" means "the one certificate carrying a private key" and everything else is the chain above it.
    /// Getting that wrong is quiet, which is why it is written once.
    /// </para>
    /// <para>
    /// Every failure names the flag that caused it. These are dev tools driven from a command line and a
    /// shell script, and "no certificate in the file carries a private key" is only useful when it also
    /// says which of four possible files was meant.
    /// </para>
    /// </remarks>
    public static class Credentials
    {

        /// <summary>
        /// Loads a PKCS#12 for TLS, splitting the private-key leaf from its intermediate CA chain so
        /// <c>SslStream</c> can send both. Returns (null, null) when <paramref name="path"/> is null, so a
        /// caller can pass an unset option straight through.
        /// </summary>
        public static (X509Certificate2? Leaf, X509Certificate2Collection? Chain) LoadForTls(string? path, string? password, string flag)
        {
            if (path is null)
                return (null, null);

            var all = Load(path, password, flag, X509KeyStorageFlags.Exportable);
            var leaf = all.FirstOrDefault(c => c.HasPrivateKey) ?? all[0];
            var chain = new X509Certificate2Collection(all.Where(c => !ReferenceEquals(c, leaf)).ToArray());
            return (leaf, chain);
        }

        /// <summary>
        /// The same material as <see cref="LoadForTls"/>, converted for the BouncyCastle backend — the
        /// leaf's key becomes a BouncyCastle handle and its curve picks the TLS 1.3 signature scheme.
        /// </summary>
        /// <remarks>
        /// This is what lets a caller bring its own Vehicle or SECC chain to the -20-faithful stack
        /// instead of the one the dev PKI minted. <see cref="BcCredentialBridge"/> does the conversion;
        /// the only thing added here is finding the leaf in the bag and naming the flag on failure.
        /// </remarks>
        public static BcTlsCredentials LoadForBouncyCastle(string path, string? password, string flag)
        {
            var (leaf, chain) = LoadForTls(path, password, flag);
            if (leaf is null || !leaf.HasPrivateKey)
                throw new ArgumentException($"{flag}: no certificate in '{path}' carries a private key, so it cannot be presented in a handshake.");

            return BcCredentialBridge.FromX509(leaf, chain);
        }

        /// <summary>
        /// A chain as the ISO 15118 messages carry one: the leaf's DER, its sub-CAs' DER in file order,
        /// and the leaf's ECDSA private key. This is the shape both a contract chain and an OEM
        /// provisioning chain need.
        /// </summary>
        /// <param name="exportable">
        /// Whether the private key must be exportable. A chain that only <em>signs</em> does not need it,
        /// and not asking is the safer default. An OEM provisioning chain does: the same key has to become
        /// an <c>ECDiffieHellman</c> handle to unwrap the issued contract key, and the only way across is
        /// through <c>ExportECPrivateKey</c> — which on a key loaded without this throws "the requested
        /// operation is not supported", from inside the session rather than at load time.
        /// </param>
        public static (byte[] Leaf, byte[][] SubCertificates, ECDsa Key, string Subject) LoadChain(
            string path, string? password, string flag, bool exportable = false)
        {
            var storage = exportable
                              ? X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable
                              : X509KeyStorageFlags.EphemeralKeySet;
            var all = Load(path, password, flag, storage);
            var leaf = all.FirstOrDefault(c => c.HasPrivateKey)
                ?? throw new ArgumentException($"{flag}: no certificate in '{path}' carries a private key.");
            var key = leaf.GetECDsaPrivateKey()
                ?? throw new ArgumentException($"{flag}: the leaf's private key in '{path}' is not ECDSA.");

            var subCertificates = all.Where(c => !ReferenceEquals(c, leaf)).Select(c => c.RawData).ToArray();
            return (leaf.RawData, subCertificates, key, leaf.Subject);
        }

        /// <summary>
        /// One ECDSA key from a PKCS#12: the private half for a side that signs, the public half for a
        /// side that verifies. Used for the tariff key, where the same file serves both roles.
        /// </summary>
        public static (ECDsa Key, string Subject) LoadEcdsaKey(string path, string? password, bool wantPrivate, string flag)
        {
            var all = Load(path, password, flag, X509KeyStorageFlags.EphemeralKeySet);
            var leaf = all.FirstOrDefault(c => c.HasPrivateKey) ?? all.FirstOrDefault()
                ?? throw new ArgumentException($"{flag}: '{path}' contains no certificate.");

            var key = wantPrivate
                          ? leaf.GetECDsaPrivateKey() ?? throw new ArgumentException($"{flag}: signing needs an ECDSA private key, and '{path}' has none.")
                          : leaf.GetECDsaPublicKey()  ?? throw new ArgumentException($"{flag}: the leaf's key in '{path}' is not ECDSA.");

            return (key, leaf.Subject);
        }

        private static X509Certificate2Collection Load(string path, string? password, string flag, X509KeyStorageFlags flags)
        {
            if (!File.Exists(path))
                throw new ArgumentException($"{flag}: no such file '{path}'.");

            try
            {
                var all = X509CertificateLoader.LoadPkcs12CollectionFromFile(path, password, flags);
                return all.Count > 0
                           ? all
                           : throw new ArgumentException($"{flag}: '{path}' contains no certificate.");
            }
            catch (CryptographicException ex)
            {
                // Overwhelmingly a wrong or missing password, and the platform message does not say so.
                throw new ArgumentException($"{flag}: could not read '{path}' — wrong password? ({ex.Message})", ex);
            }
        }

    }
}
