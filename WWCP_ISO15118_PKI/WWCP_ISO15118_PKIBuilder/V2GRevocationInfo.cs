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

namespace cloud.charging.open.protocols.ISO15118.PKI
{

    public sealed record V2GRevocationInfo(String?  CrlDistributionPointUri,
                                           String?  OcspUri,
                                           String?  CaIssuersUri)
    {

        public static V2GRevocationInfo  None    { get; }

            = new (
                  null,
                  null,
                  null
              );


        public static V2GRevocationInfo ForRoot(String? baseUri)

            => ForIssuer(
                   baseUri,
                   issuer: null
               );


        public static V2GRevocationInfo ForIssuer(String?     baseUri,
                                                  V2GIssued?  issuer)
        {

            if (String.IsNullOrWhiteSpace(baseUri))
                return None;

            var root        = baseUri.TrimEnd('/');
            var issuerSlug  = issuer?.Slug ?? "v2g_root_ca";

            return new (
                       CrlDistributionPointUri:  $"{root}/crls/{issuerSlug}.crl",
                       OcspUri:                  $"{root}/ocsp",
                       CaIssuersUri:             issuer is null ? null : $"{root}/ca/{issuerSlug}.cert.der"
                   );

        }

    }

}
