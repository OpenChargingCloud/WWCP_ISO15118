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

using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Tls;

using cloud.charging.open.protocols.ISO15118.PKI;
using cloud.charging.open.protocols.ISO15118.SharedCC;
using cloud.charging.open.protocols.ISO15118.Transport.BouncyCastle;

namespace cloud.charging.open.protocols.ISO15118.SECC
{
    /// <summary>
    /// Dev-only glue for <c>--tls-backend bc</c>: build a strict-20 V2G hierarchy (ECDSA P-521), keep the
    /// SECC's own credentials, and write the car's material into the shared <c>--pki-dir</c> for the EVCC
    /// program to read back. Peer validation pins the exact expected leaf.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The station is the side that builds the hierarchy, which is why generating lives here and loading
    /// lives in the EVCC program. That asymmetry is real, not an artefact of the split: somebody has to
    /// mint the material, and in a dev loopback it may as well be the side that is already long-running.
    /// </para>
    /// <para>
    /// Not for production. A real deployment provisions Vehicle and SECC certificates out of band from
    /// the CharIN V2G PKI — see <c>EVSimulatorApp/docs/pki-model.md</c>.
    /// </para>
    /// </remarks>
    public static class SeccPki
    {
        private const int SigScheme = SignatureScheme.ecdsa_secp521r1_sha512;

        /// <summary>
        /// The station brings its <b>own</b> chain from a PKCS#12, for a car whose PKI this process did not
        /// mint — the mirror of <c>EvccPki.WithVehicleCertificate</c>, and the only way this backend meets a
        /// foreign peer.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Nothing is pinned here, and that is deliberate.</b> <see cref="Generate"/> can pin the car's
        /// leaf because it minted it; a station serving a counterparty's certificate has no such expectation
        /// to check against, so the peer is judged by <c>--trust-roots</c> or not at all. The caller says
        /// which of the two happened rather than leaving it to be guessed.
        /// </para>
        /// <para>
        /// One consequence worth knowing before pointing this at a counterparty: the backend's
        /// <c>CertificateRequest</c> carries ISO 15118-20's strict signature pair
        /// (<c>ecdsa_secp521r1_sha512</c>, <c>ed448</c>) unless told otherwise, so a car whose client
        /// certificate is P-256 — Josev's, EVerest's, tux-evse's — cannot answer it. That is the profile
        /// being kept, not a defect: use the .NET backend for those, and this one for a peer whose material
        /// is what -20 describes.
        /// </para>
        /// </remarks>
        public static BcTlsOptions WithServerCertificate(string path, string? password, bool requireClientCertificate)
            => new()
            {
                OwnCredentials           = Credentials.LoadForBouncyCastle(path, password, "--server-cert"),
                RequireClientCertificate = requireClientCertificate,
            };

        /// <summary>Build the hierarchy, write the EVCC's files, return the SECC's mutual-TLS options.</summary>
        public static BcTlsOptions Generate(string pkiDir)
        {
            Directory.CreateDirectory(pkiDir);

            var h = V2GHierarchy.Build(
                        V2GAlgorithm.EcdsaP521,
                        new SecureRandom(),
                        V2GProfileOptions: new V2GProfileOptions(V2GProfileFlavor.Strict15118_20, V2GAlgorithm.EcdsaP521, V2GPolicySet.None));

            // What the EVCC needs: its Vehicle chain + key, plus the SECC leaf to pin.
            File.WriteAllBytes(Path.Combine(pkiDir, "vehicle.0.der"), h.VehicleLeaf.Certificate.GetEncoded());
            File.WriteAllBytes(Path.Combine(pkiDir, "vehicle.1.der"), h.VehicleSubCa2.Certificate.GetEncoded());
            File.WriteAllBytes(Path.Combine(pkiDir, "vehicle.2.der"), h.VehicleSubCa1.Certificate.GetEncoded());
            File.WriteAllBytes(Path.Combine(pkiDir, "vehicle.key"),   PrivateKeyInfoFactory.CreatePrivateKeyInfo(h.VehicleLeaf.KeyPair.Private).GetEncoded());
            File.WriteAllBytes(Path.Combine(pkiDir, "secc.leaf.der"), h.SeccLeaf.Certificate.GetEncoded());

            return new BcTlsOptions
            {
                OwnCredentials           = new BcTlsCredentials(
                                               [h.SeccLeaf.Certificate.GetEncoded(), h.CpoSubCa2.Certificate.GetEncoded(), h.CpoSubCa1.Certificate.GetEncoded()],
                                               h.SeccLeaf.KeyPair.Private, SigScheme),
                RequireClientCertificate = true,
                ValidatePeerLeaf         = expected => h.VehicleLeaf.Certificate.GetEncoded().AsSpan().SequenceEqual(expected),
            };
        }
    }
}
