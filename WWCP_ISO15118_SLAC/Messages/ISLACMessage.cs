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

namespace cloud.charging.open.protocols.ISO15118.SLAC.Messages
{

    /// <summary>
    /// Common interface implemented by every SLAC message body. The body does
    /// NOT include the HomePlug AV MM header (MMV / MMTYPE / FMI) — that is
    /// added by <see cref="ManagementMessageEntry"/> when serialising to the wire.
    /// </summary>
    public interface ISlacMessage
    {

        /// <summary>
        /// The MMTYPE that identifies this message on the wire.
        /// </summary>
        ManagementMessageType MmType { get; }

        /// <summary>
        /// Serialise the message body (everything after the MM header).
        /// </summary>
        Byte[] Encode();

    }

}
