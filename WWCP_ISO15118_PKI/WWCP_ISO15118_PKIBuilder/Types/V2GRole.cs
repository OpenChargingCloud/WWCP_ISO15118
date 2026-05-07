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
/// V2G certificate roles per ISO 15118-2 Annex H and ISO 15118-20 Annex C.
/// </summary>
public enum V2GRole
{
    /// <summary>V2G Root CA — anchor of trust. Self-signed, ~30-40y validity.</summary>
    V2GRootCA,

    /// <summary>CPO Sub-CA — issues SECC leaf certs for charging stations.</summary>
    CPOSubCA1,
    CPOSubCA2,

    /// <summary>MO Sub-CA — issues Contract Certs for PnC.</summary>
    MOSubCA1,
    MOSubCA2,

    /// <summary>OEM Sub-CA — issues OEM Provisioning Certs (factory-installed in EV).</summary>
    OEMSubCA1,
    OEMSubCA2,

    /// <summary>CPS Sub-CA — issues CPS signing certs for CertificateInstallationRes.</summary>
    CPSSubCA,

    /// <summary>SECC Leaf — TLS server cert presented by the charging station.</summary>
    SECCLeaf,

    /// <summary>Contract Cert — EV's PnC identity. xmldsig in -2, mTLS in -20.</summary>
    ContractCertLeaf,

    /// <summary>OEM Provisioning Cert — factory-installed in vehicle. mTLS client in -20 EIM.</summary>
    OEMProvCertLeaf,

    /// <summary>CPS Signing Cert — signs CertificateInstallationRes payloads (xmldsig).</summary>
    CPSSigningLeaf,

    /// <summary>MO Signing Cert — signs OCSP responses or back-end messages.</summary>
    MOSigningLeaf,
}
