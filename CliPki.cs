/*
 * Copyright (c) 2021-2025 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP ISO/IEC 15118 <https://github.com/OpenChargingCloud/WWCP_ISO15118>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
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

using Vanaheimr.V2G.Simulation.Transport.BouncyCastle;

namespace Vanaheimr.V2G.Simulation.Cli
{
    /// <summary>
    /// Dev-only glue for the <c>--tls-backend bc</c> path: the SECC generates a strict-20 V2G PKI
    /// (ECDSA P-521) and writes the EVCC's material into a shared <c>--pki-dir</c>; the EVCC loads it back.
    /// Peer validation pins the exact expected leaf certificate. Not for production — a real deployment
    /// provisions Vehicle/SECC certificates out of band from the CharIN V2G PKI (see docs/pki-model.md).
    /// </summary>
    public static class CliPki
    {
        private const int SigScheme = SignatureScheme.ecdsa_secp521r1_sha512;

        /// <summary>SECC side: build the hierarchy, write the EVCC's files, return the SECC's mutual-TLS options.</summary>
        public static BcTlsOptions GenerateSeccOptions(string pkiDir)
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
                ValidatePeerLeaf         = Pin(h.VehicleLeaf.Certificate.GetEncoded()),
            };
        }

        /// <summary>EVCC side: load the Vehicle chain + key and the SECC leaf to pin, from the shared dir.</summary>
        public static BcTlsOptions LoadEvccOptions(string pkiDir)
        {
            byte[] Read(string name) => File.ReadAllBytes(Path.Combine(pkiDir, name));

            var vehicleKey = PrivateKeyFactory.CreateKey(Read("vehicle.key"));

            return new BcTlsOptions
            {
                OwnCredentials   = new BcTlsCredentials(
                                       [Read("vehicle.0.der"), Read("vehicle.1.der"), Read("vehicle.2.der")],
                                       vehicleKey, SigScheme),
                ValidatePeerLeaf = Pin(Read("secc.leaf.der")),
            };
        }

        private static Func<byte[], bool> Pin(byte[] expected) => actual => expected.AsSpan().SequenceEqual(actual);
    }
}
