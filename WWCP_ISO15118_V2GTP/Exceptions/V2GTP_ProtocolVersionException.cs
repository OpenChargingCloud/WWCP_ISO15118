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

namespace cloud.charging.open.protocols.ISO15118.V2GTP
{

    /// <summary>
    /// ProtocolVersion / InverseProtocolVersion mismatch.
    /// </summary>
    public sealed class V2GTP_ProtocolVersionException(Byte  Version,
                                                       Byte  InverseVersion)

        : V2GTP_Exception(
              $"V2GTP protocol version mismatch: version=0x{Version:X2}, inverse=0x{InverseVersion:X2} " +
              $"(expected 0x{V2GTP_ProtocolVersion.Current:X2}/0x{V2GTP_ProtocolVersion.Inverse:X2})."
          );

}
