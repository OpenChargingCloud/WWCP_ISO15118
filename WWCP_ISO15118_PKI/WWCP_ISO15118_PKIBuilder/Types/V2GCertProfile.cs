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
/// Profile for a single certificate: distinguished name pieces, key usage, EKU,
/// basic constraints, validity, and policy OIDs.
/// </summary>
public sealed record V2GCertProfile(
    V2GRole Role,
    string CommonName,
    string[] DomainComponents,
    string? Organization,
    string? Country,
    bool IsCa,
    int? PathLenConstraint,
    int KeyUsageBits,
    KeyPurposeID[] ExtendedKeyUsages,
    TimeSpan Validity,
    string[] PolicyOids,
    string[] SubjectAltDnsNames)
{
    public static V2GCertProfile ForRole(
        V2GRole role,
        V2GProfileOptions options,
        string? cnSuffix = null)
    {
        var suffix = cnSuffix is null ? "" : $" {cnSuffix}";
        var leafKeyUsage = V2GAlgorithms.IsSignatureOnly(options.Algorithm)
            ? KeyUsage.DigitalSignature
            : KeyUsage.DigitalSignature | KeyUsage.KeyAgreement;

        return role switch
        {
            V2GRole.V2GRootCA => new(role,
                CommonName: "V2G Root CA" + suffix,
                DomainComponents: ["V2G", "Root"],
                Organization: "V2G PKI",
                Country: "DE",
                IsCa: true,
                PathLenConstraint: null,
                KeyUsageBits: KeyUsage.KeyCertSign | KeyUsage.CrlSign,
                ExtendedKeyUsages: [],
                Validity: TimeSpan.FromDays(365 * 40),
                PolicyOids: options.Policies.Policies(options.Policies.RootPolicy),
                SubjectAltDnsNames: []),

            V2GRole.CPOSubCA1 => SubCa(role, "CPO Sub-CA 1" + suffix, ["V2G", "CPO"], pathLen: 1, options),
            V2GRole.CPOSubCA2 => SubCa(role, "CPO Sub-CA 2" + suffix, ["V2G", "CPO"], pathLen: 0, options),

            V2GRole.MOSubCA1  => SubCa(role, "MO Sub-CA 1" + suffix,  ["V2G", "MO"],  pathLen: 1, options),
            V2GRole.MOSubCA2  => SubCa(role, "MO Sub-CA 2" + suffix,  ["V2G", "MO"],  pathLen: 0, options),

            V2GRole.OEMSubCA1 => SubCa(role, "OEM Sub-CA 1" + suffix, ["V2G", "OEM"], pathLen: 1, options),
            V2GRole.OEMSubCA2 => SubCa(role, "OEM Sub-CA 2" + suffix, ["V2G", "OEM"], pathLen: 0, options),

            V2GRole.CPSSubCA  => SubCa(role, "CPS Sub-CA" + suffix,   ["V2G", "CPS"], pathLen: 0, options),

            V2GRole.VehicleSubCA1 => SubCa(role, "Vehicle Sub-CA 1" + suffix, ["V2G", "Vehicle"], pathLen: 1, options),
            V2GRole.VehicleSubCA2 => SubCa(role, "Vehicle Sub-CA 2" + suffix, ["V2G", "Vehicle"], pathLen: 0, options),

            V2GRole.SECCLeaf => new(role,
                CommonName: "SECC" + suffix,
                DomainComponents: ["V2G", "CPO"],
                Organization: "V2G PKI",
                Country: "DE",
                IsCa: false,
                PathLenConstraint: null,
                KeyUsageBits: leafKeyUsage,
                ExtendedKeyUsages: options.IsLab
                    ? [KeyPurposeID.id_kp_serverAuth, KeyPurposeID.id_kp_clientAuth]
                    : [KeyPurposeID.id_kp_serverAuth],
                Validity: TimeSpan.FromDays(365 * 2),
                PolicyOids: options.Policies.Policies(options.Policies.EIMPolicy, options.Policies.PnCPolicy),
                SubjectAltDnsNames: ["secc.v2g.local", "evse.v2g.local"]),

            // The Vehicle cert is the EV's TLS *client* certificate for -20 mutual TLS —
            // the clientAuth counterpart of the SECC leaf's serverAuth. It is a -20 construct
            // (CharIN 2nd-gen "Vehicle" branch), so like the other EV-side leaves it omits the
            // clientAuth EKU under the ISO 15118-2 strict profile. Validity mirrors the SECC
            // TLS leaf (2y) rather than the CP's nominal 5y, matching this builder's own
            // short-lived-leaf convention.
            V2GRole.VehicleLeaf => new(role,
                CommonName: "Vehicle" + suffix,
                DomainComponents: ["V2G", "Vehicle"],
                Organization: "V2G PKI",
                Country: "DE",
                IsCa: false,
                PathLenConstraint: null,
                KeyUsageBits: leafKeyUsage,
                ExtendedKeyUsages: options.Flavor == V2GProfileFlavor.Strict15118_2
                    ? []
                    : [KeyPurposeID.id_kp_clientAuth],
                Validity: TimeSpan.FromDays(365 * 2),
                PolicyOids: options.Policies.Policies(options.Policies.EIMPolicy, options.Policies.PnCPolicy),
                SubjectAltDnsNames: []),

            V2GRole.ContractCertLeaf => new(role,
                CommonName: "Contract" + suffix,
                DomainComponents: ["V2G", "MO"],
                Organization: "V2G PKI",
                Country: "DE",
                IsCa: false,
                PathLenConstraint: null,
                KeyUsageBits: leafKeyUsage,
                ExtendedKeyUsages: options.Flavor == V2GProfileFlavor.Strict15118_2
                    ? []
                    : [KeyPurposeID.id_kp_clientAuth],
                Validity: TimeSpan.FromDays(365 * 2),
                PolicyOids: options.Policies.Policies(options.Policies.PnCPolicy),
                SubjectAltDnsNames: []),

            V2GRole.OEMProvCertLeaf => new(role,
                CommonName: "OEMProv" + suffix,
                DomainComponents: ["V2G", "OEM"],
                Organization: "V2G PKI",
                Country: "DE",
                IsCa: false,
                PathLenConstraint: null,
                KeyUsageBits: leafKeyUsage,
                ExtendedKeyUsages: options.Flavor == V2GProfileFlavor.Strict15118_2
                    ? []
                    : [KeyPurposeID.id_kp_clientAuth],
                Validity: TimeSpan.FromDays(365 * 30),
                PolicyOids: options.Policies.Policies(options.Policies.OEMProvPolicy),
                SubjectAltDnsNames: []),

            V2GRole.CPSSigningLeaf => new(role,
                CommonName: "CPS Signer" + suffix,
                DomainComponents: ["V2G", "CPS"],
                Organization: "V2G PKI",
                Country: "DE",
                IsCa: false,
                PathLenConstraint: null,
                KeyUsageBits: KeyUsage.DigitalSignature,
                ExtendedKeyUsages: [],
                Validity: TimeSpan.FromDays(365 * 2),
                PolicyOids: options.Policies.Policies(options.Policies.PnCPolicy),
                SubjectAltDnsNames: []),

            V2GRole.MOSigningLeaf => new(role,
                CommonName: "MO Signer" + suffix,
                DomainComponents: ["V2G", "MO"],
                Organization: "V2G PKI",
                Country: "DE",
                IsCa: false,
                PathLenConstraint: null,
                KeyUsageBits: KeyUsage.DigitalSignature,
                ExtendedKeyUsages: [KeyPurposeID.id_kp_OCSPSigning],
                Validity: TimeSpan.FromDays(365 * 2),
                PolicyOids: options.Policies.Policies(options.Policies.PnCPolicy),
                SubjectAltDnsNames: []),

            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };
    }

    public static V2GCertProfile ForRole(V2GRole role, string? cnSuffix = null)
    {
        return ForRole(role, V2GProfileOptions.LabFor(V2GAlgorithm.EcdsaP256), cnSuffix);
    }

    private static V2GCertProfile SubCa(
        V2GRole role,
        string cn,
        string[] dcs,
        int pathLen,
        V2GProfileOptions options) => new(
        role,
        CommonName: cn,
        DomainComponents: dcs,
        Organization: "V2G PKI",
        Country: "DE",
        IsCa: true,
        PathLenConstraint: pathLen,
        KeyUsageBits: KeyUsage.KeyCertSign | KeyUsage.CrlSign,
        ExtendedKeyUsages: [],
        Validity: TimeSpan.FromDays(365 * 12),
        PolicyOids: options.Policies.Policies(options.Policies.RootPolicy),
        SubjectAltDnsNames: []);
}
