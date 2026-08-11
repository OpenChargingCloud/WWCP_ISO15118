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

using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Math;

namespace cloud.charging.open.protocols.ISO15118.StateMachines.Iso2
{

    /// <summary>
    /// The ISO 15118-<b>2</b> contract-provisioning key transport (§7.9.2.4): the secondary actor generates
    /// an ephemeral <b>secp256r1</b> key pair, runs ECDH against the receiver's public key, derives a
    /// 128-bit AES key, and AES-128-CBC-encrypts the freshly issued contract certificate's raw private
    /// scalar. The receiver repeats the ECDH with its own private key and the transmitted
    /// <c>DHpublickey</c> and unwraps. Wire shapes come from the schema:
    /// <c>DHpublickey</c> = 65 B (uncompressed P-256 point <c>0x04‖X32‖Y32</c>),
    /// <c>ContractSignatureEncryptedPrivateKey</c> = 48 B (<c>IV16‖ciphertext32</c>).
    /// <para>
    /// <b>Who the receiver is depends on the message.</b> For a <c>CertificateInstallationReq</c> it is the
    /// OEM provisioning certificate — the only key a car has before it has a contract. For a
    /// <c>CertificateUpdateReq</c> it is the <em>expiring contract certificate</em>, which is what makes an
    /// update self-authenticating: only the holder of the old contract key can unwrap the new one.
    /// </para>
    /// <para>
    /// <b>Differences from the -20 transport</b> (<see cref="Iso20.ContractProvisioning"/>), none of them
    /// cosmetic: a different curve (secp256r1 against secp521r1), a different cipher mode (CBC against
    /// GCM, so this one is <em>unauthenticated</em> — a wrong key yields 32 bytes of nonsense rather than a
    /// thrown tag check), a different KDF, and half the key length. The two share no code, and trying to
    /// make them would only hide that.
    /// </para>
    /// <para>
    /// <b>Honesty note:</b> as with the -20 transport, these *crypto payload* octets are self-consistent
    /// only — they round-trip against our own EVCC and nothing else. The KDF below is the ANSI X9.63 form
    /// the standard names (counter <em>after</em> Z, which is where it differs from the ConcatKDF -20
    /// uses), with empty SharedInfo. No capture in this repository contains a real
    /// CertificateInstallationRes to diff against, so a first interop run against a foreign stack should
    /// expect the SharedInfo to be the thing that needs settling. The wire *messages* around the payload
    /// stay byte-exact per the usual oracles.
    /// </para>
    /// </summary>
    public static class ContractProvisioning
    {

        private const Int32 ScalarBytes = 32;   // secp256r1
        private const Int32 IvBytes     = 16;
        private const Int32 KeyBytes    = 16;   // AES-128, per §7.9.2.4

        /// <summary>Encrypts <paramref name="contractKey"/>'s raw private scalar for the holder of
        /// <paramref name="receiverPublicKey"/> (a P-256 OEM-provisioning or contract key): ephemeral ECDH →
        /// AES-128-CBC. Returns the wire fields.</summary>
        public static (Byte[] DhPublicKey, Byte[] EncryptedPrivateKey) EncryptContractKey(
            ECDiffieHellmanPublicKey receiverPublicKey, ECDsa contractKey)
        {

            using var ephemeral = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            var aesKey = DeriveKey(ephemeral.DeriveRawSecretAgreement(receiverPublicKey));

            var scalar = FixedWidthScalar(contractKey);
            var iv     = RandomNumberGenerator.GetBytes(IvBytes);

            using var aes = Aes.Create();
            aes.Key     = aesKey;
            aes.Mode    = CipherMode.CBC;
            // The scalar is exactly two AES blocks, so there is nothing to pad and the field is the 48
            // bytes the schema expects. PKCS#7 would append a whole block and make it 64.
            aes.Padding = PaddingMode.None;

            var output = new Byte[IvBytes + ScalarBytes];
            iv.CopyTo(output, 0);
            aes.EncryptCbc(scalar, iv, output.AsSpan(IvBytes), PaddingMode.None);

            return (UncompressedPoint(ephemeral), output);

        }

        /// <summary>The receiving side: repeats the ECDH with its own private key and the transmitted
        /// <paramref name="dhPublicKey"/>, unwraps the scalar, and reconstructs the contract private key.
        /// </summary>
        /// <remarks>
        /// CBC authenticates nothing, so unlike the -20 counterpart this cannot fail on a wrong key — it
        /// returns a key built from 32 bytes of nonsense. The caller has to check the result against the
        /// certificate it was issued alongside; <see cref="Matches"/> is that check.
        /// </remarks>
        public static ECDsa RecoverContractKey(ECDiffieHellman receiverKey, Byte[] dhPublicKey, Byte[] encryptedPrivateKey)
        {

            if (dhPublicKey.Length != 1 + 2 * ScalarBytes || dhPublicKey[0] != 0x04)
                throw new CryptographicException($"DHpublickey: expected a {1 + 2 * ScalarBytes}-byte uncompressed P-256 point.");

            if (encryptedPrivateKey.Length != IvBytes + ScalarBytes)
                throw new CryptographicException($"ContractSignatureEncryptedPrivateKey: expected {IvBytes + ScalarBytes} bytes.");

            using var ephemeralPub = ECDiffieHellman.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint
                {
                    X = dhPublicKey[1..(1 + ScalarBytes)],
                    Y = dhPublicKey[(1 + ScalarBytes)..],
                },
            });

            var aesKey = DeriveKey(receiverKey.DeriveRawSecretAgreement(ephemeralPub.PublicKey));

            using var aes = Aes.Create();
            aes.Key     = aesKey;
            aes.Mode    = CipherMode.CBC;
            aes.Padding = PaddingMode.None;

            var scalar = new Byte[ScalarBytes];
            aes.DecryptCbc(encryptedPrivateKey.AsSpan(IvBytes), encryptedPrivateKey.AsSpan(0, IvBytes),
                           scalar, PaddingMode.None);

            // .NET cannot import an EC private key without its public point — derive Q = d·G via BouncyCastle.
            var domain = SecNamedCurves.GetByName("secp256r1");
            var q      = domain.G.Multiply(new BigInteger(1, scalar)).Normalize();

            return ECDsa.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                D     = scalar,
                Q     = new ECPoint
                {
                    X = q.AffineXCoord.GetEncoded(),
                    Y = q.AffineYCoord.GetEncoded(),
                },
            });

        }

        /// <summary>Whether an unwrapped key really belongs to the certificate it arrived with — the check
        /// CBC's lack of authentication makes the caller's job. A sign/verify round trip rather than a
        /// parameter comparison, because that is the property actually wanted: this key can produce
        /// signatures that certificate verifies.</summary>
        public static Boolean Matches(ECDsa privateKey, ECDsa certificatePublicKey)
        {
            try
            {
                var probe     = "contract-key-check"u8.ToArray();
                var signature = privateKey.SignData(probe, HashAlgorithmName.SHA256);
                return certificatePublicKey.VerifyData(probe, signature, HashAlgorithmName.SHA256);
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        /// <summary>ANSI X9.63 KDF with SHA-256 and empty SharedInfo: <c>SHA-256(Z ‖ counter=1)</c>,
        /// truncated to the 16-byte AES-128 key. The counter goes <b>after</b> Z here, which is the one
        /// place this differs from the ConcatKDF the -20 transport uses.</summary>
        private static Byte[] DeriveKey(Byte[] z)
        {
            var input = new Byte[z.Length + 4];
            z.CopyTo(input, 0);
            input[z.Length + 3] = 1;
            return SHA256.HashData(input)[..KeyBytes];
        }

        private static Byte[] FixedWidthScalar(ECDsa key)
        {

            var d = key.ExportParameters(includePrivateParameters: true).D
                        ?? throw new CryptographicException("contract key has no private scalar.");

            if (d.Length == ScalarBytes)
                return d;

            if (d.Length > ScalarBytes)
                throw new CryptographicException($"contract key is not a P-256 key ({d.Length}-byte scalar).");

            var padded = new Byte[ScalarBytes];         // left-pad to the fixed 32-byte width
            d.CopyTo(padded, ScalarBytes - d.Length);
            return padded;

        }

        private static Byte[] UncompressedPoint(ECDiffieHellman key)
        {

            var p     = key.ExportParameters(includePrivateParameters: false).Q;
            var point = new Byte[1 + 2 * ScalarBytes];
            point[0]  = 0x04;
            Pad(p.X!).CopyTo(point, 1);
            Pad(p.Y!).CopyTo(point, 1 + ScalarBytes);
            return point;

            static Byte[] Pad(Byte[] c)
            {
                if (c.Length == ScalarBytes) return c;
                var padded = new Byte[ScalarBytes];
                c.CopyTo(padded, ScalarBytes - c.Length);
                return padded;
            }

        }

    }

}
