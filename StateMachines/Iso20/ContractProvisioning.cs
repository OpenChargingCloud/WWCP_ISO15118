/*
 * Copyright (c) 2021-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of EVSimulatorApp
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

using cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated;

namespace Vanaheimr.V2G.Simulation.StateMachines.Iso20
{
    /// <summary>
    /// The ISO 15118-20 contract-provisioning key transport (<c>SignedInstallationData</c>): the SECC
    /// generates an <b>ephemeral secp521r1</b> key pair, runs ECDH against the EV's static OEM-provisioning
    /// public key, derives an AES-256 key, and AES-GCM-encrypts the freshly issued contract certificate's
    /// raw private scalar. The EV runs the same ECDH with its OEM private key + the transmitted
    /// <c>DHPublicKey</c> and unwraps the key. Wire shapes come from the schema:
    /// <c>DHPublicKey</c> = 133 B (uncompressed P-521 point <c>0x04‖X66‖Y66</c>),
    /// <c>SECP521_EncryptedPrivateKey</c> = 94 B (<c>IV12‖ciphertext66‖tag16</c>).
    /// <para>
    /// <b>Honesty note:</b> the KDF here (a single-round ConcatKDF: <c>SHA-512(0x00000001‖Z)[0..32]</c>,
    /// empty OtherInfo) is schema-valid and round-trips, but no external reference stack implements -20
    /// provisioning to diff against (Josev raises <c>NotImplementedError</c> on both sides), so unlike the
    /// message codecs these *crypto payload* octets are self-consistent only — flagged in
    /// <c>docs/phase5-report.md</c>. The wire *messages* around them stay byte-exact per the usual oracles.
    /// </para>
    /// </summary>
    public static class ContractProvisioning
    {
        private const int ScalarBytes = 66;   // secp521r1
        private const int IvBytes     = 12;
        private const int TagBytes    = 16;

        /// <summary>Encrypts <paramref name="contractKey"/>'s raw private scalar for the holder of
        /// <paramref name="oemPublicKey"/> (a P-521 OEM-provisioning key): ephemeral ECDH → AES-256-GCM.
        /// Returns the wire fields of <c>SignedInstallationData</c>.</summary>
        public static (byte[] DhPublicKey, byte[] EncryptedPrivateKey) EncryptContractKey(
            ECDiffieHellmanPublicKey oemPublicKey, ECDsa contractKey)
        {
            using var ephemeral = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP521);
            var aesKey = DeriveKey(ephemeral.DeriveRawSecretAgreement(oemPublicKey));

            var scalar = FixedWidthScalar(contractKey);
            var iv = RandomNumberGenerator.GetBytes(IvBytes);
            var output = new byte[IvBytes + ScalarBytes + TagBytes];
            iv.CopyTo(output, 0);
            using (var gcm = new AesGcm(aesKey, TagBytes))
                gcm.Encrypt(iv, scalar, output.AsSpan(IvBytes, ScalarBytes), output.AsSpan(IvBytes + ScalarBytes, TagBytes));

            return (UncompressedPoint(ephemeral), output);
        }

        /// <summary>The EV side: repeats the ECDH with its OEM-provisioning private key and the SECC's
        /// <paramref name="dhPublicKey"/>, unwraps the scalar, and reconstructs the contract private key.
        /// Throws <see cref="CryptographicException"/> if the GCM tag does not authenticate.</summary>
        public static ECDsa RecoverContractKey(ECDiffieHellman oemKey, byte[] dhPublicKey, byte[] encryptedPrivateKey)
        {
            if (dhPublicKey.Length != 1 + 2 * ScalarBytes || dhPublicKey[0] != 0x04)
                throw new CryptographicException($"DHPublicKey: expected a {1 + 2 * ScalarBytes}-byte uncompressed P-521 point.");
            if (encryptedPrivateKey.Length != IvBytes + ScalarBytes + TagBytes)
                throw new CryptographicException($"SECP521_EncryptedPrivateKey: expected {IvBytes + ScalarBytes + TagBytes} bytes.");

            using var ephemeralPub = ECDiffieHellman.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP521,
                Q = new ECPoint
                {
                    X = dhPublicKey[1..(1 + ScalarBytes)],
                    Y = dhPublicKey[(1 + ScalarBytes)..],
                },
            });
            var aesKey = DeriveKey(oemKey.DeriveRawSecretAgreement(ephemeralPub.PublicKey));

            var scalar = new byte[ScalarBytes];
            using (var gcm = new AesGcm(aesKey, TagBytes))
                gcm.Decrypt(encryptedPrivateKey.AsSpan(0, IvBytes),
                            encryptedPrivateKey.AsSpan(IvBytes, ScalarBytes),
                            encryptedPrivateKey.AsSpan(IvBytes + ScalarBytes, TagBytes),
                            scalar);

            // .NET cannot import an EC private key without its public point — derive Q = d·G via BouncyCastle.
            var domain = SecNamedCurves.GetByName("secp521r1");
            var q = domain.G.Multiply(new BigInteger(1, scalar)).Normalize();
            var ecdsa = ECDsa.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP521,
                D = scalar,
                Q = new ECPoint
                {
                    X = q.AffineXCoord.GetEncoded(),
                    Y = q.AffineYCoord.GetEncoded(),
                },
            });
            return ecdsa;
        }

        /// <summary>Single-round ConcatKDF (NIST SP 800-56A §5.8.1 with SHA-512, empty OtherInfo):
        /// <c>SHA-512(counter=1 ‖ Z)</c>, truncated to the 32-byte AES-256 key.</summary>
        private static byte[] DeriveKey(byte[] z)
        {
            var input = new byte[4 + z.Length];
            input[3] = 1;
            z.CopyTo(input, 4);
            return SHA512.HashData(input)[..32];
        }

        private static byte[] FixedWidthScalar(ECDsa key)
        {
            var d = key.ExportParameters(includePrivateParameters: true).D
                ?? throw new CryptographicException("contract key has no private scalar.");
            if (d.Length == ScalarBytes) return d;
            var padded = new byte[ScalarBytes];                 // left-pad to the fixed 66-byte width
            d.CopyTo(padded, ScalarBytes - d.Length);
            return padded;
        }

        private static byte[] UncompressedPoint(ECDiffieHellman key)
        {
            var p = key.ExportParameters(includePrivateParameters: false).Q;
            var point = new byte[1 + 2 * ScalarBytes];
            point[0] = 0x04;
            Pad(p.X!).CopyTo(point, 1);
            Pad(p.Y!).CopyTo(point, 1 + ScalarBytes);
            return point;

            static byte[] Pad(byte[] c)
            {
                if (c.Length == ScalarBytes) return c;
                var padded = new byte[ScalarBytes];
                c.CopyTo(padded, ScalarBytes - c.Length);
                return padded;
            }
        }
    }
}
