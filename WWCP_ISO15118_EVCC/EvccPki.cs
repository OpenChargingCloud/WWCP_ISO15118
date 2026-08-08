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

using Org.BouncyCastle.Security;
using Org.BouncyCastle.Tls;

using cloud.charging.open.protocols.ISO15118.SharedCC;
using cloud.charging.open.protocols.ISO15118.Transport.BouncyCastle;

namespace cloud.charging.open.protocols.ISO15118.EVCC
{
    /// <summary>
    /// Dev-only glue for <c>--tls-backend bc</c>: load the Vehicle chain and key the station minted into
    /// the shared <c>--pki-dir</c>, and pin the station's leaf.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This side only reads. The hierarchy is built by <c>WWCP_ISO15118_SECC</c>, which writes the five
    /// files read below — run the station first, or the directory is empty.
    /// </para>
    /// <para>
    /// Not for production. A real vehicle's contract and Vehicle certificates are provisioned out of
    /// band from the CharIN V2G PKI — see <c>EVSimulatorApp/docs/pki-model.md</c>.
    /// </para>
    /// </remarks>
    public static class EvccPki
    {
        private const int SigScheme = SignatureScheme.ecdsa_secp521r1_sha512;

        /// <summary>
        /// The car brings its <b>own</b> Vehicle chain and key from a PKCS#12, for a station whose PKI this
        /// process did not mint.
        /// </summary>
        /// <remarks>
        /// The peer check is the part worth understanding. With <paramref name="pkiDir"/> given we still pin
        /// the station's leaf from <c>secc.leaf.der</c> there; without it there is nothing to pin against —
        /// the station is a stranger — so the peer is accepted and the run says so. That is the honest
        /// shape for a dev tool: a pin invented here would be a pin against nothing.
        /// </remarks>
        public static BcTlsOptions WithVehicleCertificate(string path, string? password, string? pkiDir)
        {
            var own = Credentials.LoadForBouncyCastle(path, password, "--vehicle-cert");

            byte[]? seccLeaf = null;
            if (pkiDir is not null)
            {
                var candidate = Path.Combine(pkiDir, "secc.leaf.der");
                if (File.Exists(candidate))
                    seccLeaf = File.ReadAllBytes(candidate);
            }

            Console.WriteLine(seccLeaf is not null
                                  ? "Presenting your Vehicle chain; pinning the station's leaf from --pki-dir."
                                  : "WARNING: presenting your Vehicle chain and accepting ANY station certificate — " +
                                    "nothing to pin against without --pki-dir. Dev tool only.");

            return new BcTlsOptions
            {
                OwnCredentials   = own,
                ValidatePeerLeaf = seccLeaf is null ? null : actual => seccLeaf.AsSpan().SequenceEqual(actual),
            };
        }

        /// <summary>Load the Vehicle chain + key and the SECC leaf to pin, from the shared dir.</summary>
        public static BcTlsOptions Load(string pkiDir)
        {
            byte[] Read(string name)
            {
                var path = Path.Combine(pkiDir, name);
                return File.Exists(path)
                           ? File.ReadAllBytes(path)
                           : throw new FileNotFoundException(
                                 $"--pki-dir '{pkiDir}' has no '{name}'. The station mints this material: " +
                                 "start WWCP_ISO15118_SECC with the same --pki-dir first.", path);
            }

            var vehicleKey = PrivateKeyFactory.CreateKey(Read("vehicle.key"));
            var seccLeaf   = Read("secc.leaf.der");

            return new BcTlsOptions
            {
                OwnCredentials   = new BcTlsCredentials(
                                       [Read("vehicle.0.der"), Read("vehicle.1.der"), Read("vehicle.2.der")],
                                       vehicleKey, SigScheme),
                ValidatePeerLeaf = actual => seccLeaf.AsSpan().SequenceEqual(actual),
            };
        }
    }
}
