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
    /// Which ISO 15118 revision an SDP message follows.
    ///
    /// At the wire-byte level the basic SDP_Request (2 bytes) and SDP_Response
    /// (20 bytes) are byte-for-byte identical between -2 and -20 – the V2GTP
    /// payload type is even the same (0x9000 / 0x9001). The distinction is only
    /// in the *interpretation*:
    ///
    ///   * In -2, Security=0x10 (no-TLS) is permitted.
    ///   * In -20, TLS is mandatory; Security must be 0x00.
    ///   * -20 additionally defines Wireless SDP (payload types 0x9002/0x9003)
    ///     for ACDP/WPT scenarios, with a different wire format.
    ///
    /// We carry the version as metadata on the message object so server/client
    /// can apply the right validation.
    /// </summary>
    public enum SDP_Version
    {

        /// <summary>
        /// ISO 15118-2 (Edition 1, 2014; Edition 2, 2024 amendment).
        /// </summary>
        ISO_15118_2,

        /// <summary>
        /// ISO 15118-20 (Edition 1, 2022).
        /// </summary>
        ISO_15118_20,

    }

}
