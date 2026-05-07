/*
 * Copyright (c) 2014-2025 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

#region Usings

using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;

#endregion

// cloud.charging.open.protocols.ISO15118.PKI — ISO 15118 PKI builder for testing and pentesting
//
// References:
//   - ISO 15118-2:2014/AMD 1:2022, Annex H (V2G certificate profiles)
//   - ISO 15118-20:2022, Annex C (PKI for -20)
//   - VDE-AR-E 2802-100-1 (German national addendum)
//   - RFC 5280 (X.509 PKIX), RFC 8410 (Ed25519/X25519 in PKIX)

namespace cloud.charging.open.protocols.ISO15118.PKI;

public static class V2GAlgorithms
{

    public static bool IsSignatureOnly(V2GAlgorithm algorithm) =>
        algorithm is V2GAlgorithm.Ed25519 or V2GAlgorithm.Ed448 or V2GAlgorithm.MLDsa44 or V2GAlgorithm.MLDsa65 or V2GAlgorithm.MLDsa87;

    public static bool IsPqc(V2GAlgorithm algorithm) =>
        algorithm is V2GAlgorithm.MLDsa44 or V2GAlgorithm.MLDsa65 or V2GAlgorithm.MLDsa87;

    public static bool IsStrict15118_20(V2GAlgorithm algorithm) =>
        algorithm is V2GAlgorithm.EcdsaP521 or V2GAlgorithm.Ed448;

    public static string Tag(V2GAlgorithm algorithm) => algorithm switch
    {
        V2GAlgorithm.EcdsaP256 => "ecdsa_p256",
        V2GAlgorithm.EcdsaP384 => "ecdsa_p384",
        V2GAlgorithm.EcdsaP521 => "ecdsa_p521",
        V2GAlgorithm.Ed25519   => "ed25519",
        V2GAlgorithm.Ed448     => "ed448",
        V2GAlgorithm.MLDsa44   => "ml_dsa_44",
        V2GAlgorithm.MLDsa65   => "ml_dsa_65",
        V2GAlgorithm.MLDsa87   => "ml_dsa_87",
        _ => "unknown"
    };

}
