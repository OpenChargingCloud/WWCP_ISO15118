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
/// Certificate policy OIDs used for one generated hierarchy.
/// </summary>
public sealed record V2GPolicySet(
    string? RootPolicy,
    string? PnCPolicy,
    string? EIMPolicy,
    string? OEMProvPolicy)
{

    public static V2GPolicySet None { get; } = new(null, null, null, null);

    public static V2GPolicySet FromArc(string arc) => new(
        RootPolicy:    $"{arc}.0",
        PnCPolicy:     $"{arc}.1",
        EIMPolicy:     $"{arc}.2",
        OEMProvPolicy: $"{arc}.3");

    public static V2GPolicySet Lab { get; } = FromArc(V2GOids.LabPolicyArc);

    public string[] Policies(params string?[] oids) =>
        oids.Where(oid => !string.IsNullOrWhiteSpace(oid))
            .Select(oid => oid!)
            .ToArray();

}
