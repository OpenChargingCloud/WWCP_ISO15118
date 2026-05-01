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
    /// SDP "TransportProtocol" field, octet 18 of an SDP_Response payload
    /// (and octet 2 of an SDP_Request).
    ///
    /// ISO 15118-2 §8.4.3.3.6:
    ///   0x00  TCP
    ///   0x10  reserved (UDP – not used in 15118-2/-20 application transport)
    /// </summary>
    public enum SDP_TransportProtocol : Byte
    {
        TCP       = 0x00,
        Reserved  = 0x10,
    }

}
