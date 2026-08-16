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

using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace cloud.charging.open.protocols.ISO15118.Transport.BouncyCastle
{
    /// <summary>
    /// Shared building blocks for the BouncyCastle V2G TLS client/server: the ISO 15118-20 signature
    /// schemes, credentialed-signer construction, and peer-certificate validation.
    /// </summary>
    internal static class BcV2GTls
    {
        /// <summary>ISO 15118-20's two TLS signature schemes: ECDSA-secp521r1-SHA512 and Ed448.</summary>
        internal static readonly IList<SignatureAndHashAlgorithm> AcceptedSignatureAlgorithms =
            new List<SignatureAndHashAlgorithm>
            {
                SignatureScheme.GetSignatureAndHashAlgorithm(SignatureScheme.ecdsa_secp521r1_sha512),
                SignatureScheme.GetSignatureAndHashAlgorithm(SignatureScheme.ed448),
            };

        /// <summary>
        /// The -20 TLS 1.3 cipher suites — derived from <see cref="TlsProfiles.Iso20CipherSuites"/> rather than
        /// listed again, so the .NET and BouncyCastle backends cannot drift apart. Both enumerations carry the
        /// IANA code points, so the cast is exact (asserted by <c>TlsProfilesTests</c>).
        /// </summary>
        internal static readonly int[] CipherSuites =
            TlsProfiles.Iso20CipherSuites.Select(suite => (int) suite).ToArray();

        internal static readonly ProtocolVersion[] Tls13Only = { ProtocolVersion.TLSv13 };

        /// <summary>ISO 15118-2's transport: TLS 1.2, and only 1.2.</summary>
        internal static readonly ProtocolVersion[] Tls12Only = { ProtocolVersion.TLSv12 };

        /// <summary>The `-2` cipher suites, derived from <see cref="TlsProfiles.Iso2CipherSuites"/> for the
        /// same reason the `-20` pair is: two lists of the same thing drift.</summary>
        internal static readonly int[] Iso2CipherSuites =
            TlsProfiles.Iso2CipherSuites.Select(suite => (int) suite).ToArray();

        /// <summary>RFC 6066 <c>identifier_type = cert_sha1_hash</c>.</summary>
        private const byte CertSha1Hash = 3;

        /// <summary>
        /// The <c>trusted_ca_keys</c> extension body (RFC 6066 §6) for the V2G root certificates this EV
        /// holds — one <c>TrustedAuthority</c> per root, each a <c>cert_sha1_hash</c> of the root's DER.
        /// </summary>
        /// <remarks>
        /// <para>
        /// `[V2G2-651]` obliges **every** `-2` EV to send this and says only *"a list of V2G root
        /// certificates it possesses … as defined in IETF RFC 6066"*, leaving the identifier type open.
        /// RFC 6066 offers four; <c>cert_sha1_hash</c> is the one that names a certificate rather than a
        /// key or a subject, which is what the requirement asks for, and it is the form EVerest's own
        /// server-side implementation documents in its worked example
        /// (<c>lib/everest/tls/extensions/trusted_ca_keys.cpp</c>). Their parser accepts all four, so the
        /// choice costs no interop; it is recorded here because a future disagreement about it will be
        /// about this line.
        /// </para>
        /// <para>
        /// SHA-1 is the algorithm the extension is defined with. It is an identifier, not a signature —
        /// nothing is authenticated by it — but it is worth saying out loud, because a SHA-1 in 2026 is
        /// otherwise a finding.
        /// </para>
        /// </remarks>
        internal static byte[] BuildTrustedCaKeys(IReadOnlyList<byte[]> rootsDer)
        {

            if (rootsDer.Count == 0)
                throw new ArgumentException("trusted_ca_keys with no authorities is not a list of roots — omit the extension instead.",
                                            nameof(rootsDer));

            var body = new MemoryStream();

            foreach (var der in rootsDer)
            {
                body.WriteByte(CertSha1Hash);
                body.Write(System.Security.Cryptography.SHA1.HashData(der));
            }

            var list = body.ToArray();

            // TrustedAuthorities: a 2-byte length prefix over the whole list.
            var extension = new byte[2 + list.Length];
            extension[0] = (byte) (list.Length >> 8);
            extension[1] = (byte)  list.Length;
            list.CopyTo(extension, 2);

            return extension;

        }

        /// <summary>The <c>cert_sha1_hash</c> entries of a received <c>trusted_ca_keys</c> body, in order.
        /// Entries of the other three identifier types are skipped and counted by
        /// <paramref name="otherTypes"/> — a station that only understands one form should still be able
        /// to say how much it ignored.</summary>
        internal static IReadOnlyList<byte[]> ParseTrustedCaKeys(byte[] extension, out int otherTypes)
        {

            var hashes = new List<byte[]>();
            otherTypes = 0;

            if (extension.Length < 2)
                return hashes;

            var declared = (extension[0] << 8) | extension[1];
            var end      = Math.Min(2 + declared, extension.Length);

            for (var i = 2; i < end; )
            {
                var type = extension[i++];

                switch (type)
                {

                    case CertSha1Hash:
                    case 1:                                    // key_sha1_hash — same 20-byte shape
                        if (i + 20 > end)
                            return hashes;
                        if (type == CertSha1Hash)
                            hashes.Add(extension[i..(i + 20)]);
                        else
                            otherTypes++;
                        i += 20;
                        break;

                    case 2:                                    // x509_name: 2-byte length + DER name
                        if (i + 2 > end)
                            return hashes;
                        var nameLength = (extension[i] << 8) | extension[i + 1];
                        i += 2 + nameLength;
                        otherTypes++;
                        break;

                    case 0:                                    // pre_agreed: empty
                        otherTypes++;
                        break;

                    default:                                   // unknown type: the rest is unparseable
                        return hashes;

                }
            }

            return hashes;

        }

        /// <summary>
        /// The signature algorithms to put in the <c>CertificateRequest</c>: the -20 pair by default, or the
        /// caller's explicit list (<see cref="BcTlsOptions.AcceptedClientSignatureSchemes"/>) — a documented
        /// deviation used by the macOS TLS 1.3 fallback for its P-256 certificates.
        /// </summary>
        internal static IList<SignatureAndHashAlgorithm> AcceptedClientSignatureAlgorithms(BcTlsOptions options)
            => options.AcceptedClientSignatureSchemes is { Length: > 0 } schemes
                   ? schemes.Select(SignatureScheme.GetSignatureAndHashAlgorithm).ToList()
                   : AcceptedSignatureAlgorithms;

        internal static TlsCredentials BuildSigner(BcTlsCrypto crypto, BcTlsCredentials creds, TlsContext context,
                                                  bool iso2Profile = false)
        {
            // The two versions carry the certificate list in different structures, and using the wrong one
            // is an internal_error rather than a readable alert: TLS 1.3 wants CertificateEntry with an
            // (empty) certificate_request_context, TLS 1.2 the legacy Certificate(TlsCertificate[]).
            // The -2 branch arrived with the TLS 1.2 profile on 2026-08-16 and this comment's first half
            // had been standing since the -20 one — half a rule, which is how it read as complete.
            var certificate = iso2Profile

                                  ? new Certificate(creds.CertificateChain
                                                         .Select(der => (TlsCertificate) new BcTlsCertificate(crypto, der))
                                                         .ToArray())

                                  : new Certificate(TlsUtilities.EmptyBytes,
                                                    creds.CertificateChain
                                                         .Select(der => new CertificateEntry(new BcTlsCertificate(crypto, der),
                                                                                              (IDictionary<int, byte[]>?) null))
                                                         .ToArray());

            return new BcDefaultTlsCredentialedSigner(
                       new TlsCryptoParameters(context),
                       crypto,
                       creds.PrivateKey,
                       certificate,
                       SignatureScheme.GetSignatureAndHashAlgorithm(creds.SignatureScheme));
        }

        internal static void ValidatePeer(Certificate? peer, Func<byte[], bool>? validateLeaf,
                                          short missingAlert, Func<byte[][], bool>? validateChain = null)
        {
            if (peer is null || peer.IsEmpty)
                throw new TlsFatalAlert(missingAlert);

            if (validateLeaf is not null && !validateLeaf(peer.GetCertificateAt(0).GetEncoded()))
                throw new TlsFatalAlert(AlertDescription.bad_certificate);

            // The whole chain as the peer sent it, leaf first. The handshake already carries it; until now
            // only the leaf was handed on, which is why a chain check was not expressible on this backend.
            if (validateChain is not null)
            {
                var chain = new byte[peer.Length][];
                for (int i = 0; i < peer.Length; i++)
                    chain[i] = peer.GetCertificateAt(i).GetEncoded();

                if (!validateChain(chain))
                    throw new TlsFatalAlert(AlertDescription.bad_certificate);
            }
        }
    }
}
