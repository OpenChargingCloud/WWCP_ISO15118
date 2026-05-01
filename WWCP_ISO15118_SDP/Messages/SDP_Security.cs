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

namespace cloud.charging.open.protocols.ISO15118.SDP.Messages
{

    /// <summary>
    /// SDP "Security" field, octet 17 of an SDP_Response payload (and octet 1
    /// of an SDP_Request payload). Defines whether the V2G TCP transport will
    /// be secured with TLS.
    ///
    /// ISO 15118-2 §8.4.3.3.5:
    ///   0x00  Secure communication via TLS
    ///   0x10  No transport-layer security (plaintext TCP)
    ///
    /// ISO 15118-20 raises the bar – TLS is mandatory and the value 0x10 is
    /// no longer permitted in compliant deployments. The codec accepts both
    /// values regardless so pentest tooling can craft non-compliant frames.
    /// </summary>
    public enum SDP_Security : Byte
    {

        /// <summary>
        /// TLS-secured TCP transport (mandatory for 15118-20).
        /// </summary>
        TLS    = 0x00,

        /// <summary>
        /// Plaintext TCP transport (15118-2 only, deprecated).
        /// </summary>
        NoTLS  = 0x10

    }

}
