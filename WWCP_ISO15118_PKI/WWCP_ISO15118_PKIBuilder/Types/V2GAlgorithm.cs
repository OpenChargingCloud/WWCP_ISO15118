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

/// <summary>
/// Cryptographic profile for the entire hierarchy.
/// </summary>
public enum V2GAlgorithm
{

    /// <summary>
    /// NIST P-256 (secp256r1) with ECDSA-SHA256.
    /// Mandatory baseline for ISO 15118-2.
    /// </summary>
    EcdsaP256,

    /// <summary>
    /// NIST P-384 (secp384r1) with ECDSA-SHA384.
    /// Useful for lab/interop tests, not the ISO 15118-20 headline profile.
    /// </summary>
    EcdsaP384,

    /// <summary>
    /// NIST P-521 (secp521r1) with ECDSA-SHA512.
    /// ISO 15118-20 classical ECC profile.
    /// </summary>
    EcdsaP521,

    /// <summary>
    /// Ed25519 signatures (RFC 8410). Lab/interop profile.
    /// </summary>
    Ed25519,

    /// <summary>
    /// Ed448 signatures (RFC 8410). ISO 15118-20 Edwards-curve signature profile.
    /// X448 ECDHE happens at the TLS layer, not in the certificate.
    /// </summary>
    Ed448,

    /// <summary>
    /// ML-DSA-44 post-quantum signature profile, formerly Dilithium2.
    /// Experimental for ISO 15118 PKI/TLS interop.
    /// </summary>
    MLDsa44,

    /// <summary>
    /// ML-DSA-65 post-quantum signature profile, formerly Dilithium3.
    /// Experimental for ISO 15118 PKI/TLS interop.
    /// </summary>
    MLDsa65,

    /// <summary>
    /// ML-DSA-87 post-quantum signature profile, formerly Dilithium5.
    /// Experimental for ISO 15118 PKI/TLS interop.
    /// </summary>
    MLDsa87

}
