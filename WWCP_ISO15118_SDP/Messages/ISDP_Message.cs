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

using cloud.charging.open.protocols.ISO15118.V2GTP;

#endregion

namespace cloud.charging.open.protocols.ISO15118.SDP.Messages
{

    public static class ISDPMessageExtensions
    {

        /// <summary>
        /// Encode the SDP message body together with its V2GTP header into a
        /// single complete frame ready to be sent on UDP.
        /// </summary>
        public static Byte[] EncodeFrame(this ISDP_Message Message)

            => V2GTP_Frame.Wrap(
                   Message.PayloadType,
                   Message.EncodePayload()
               ).ToArray();

    }

    /// <summary>
    /// An SDP application-layer message (request or response). Encodes to the
    /// V2GTP *payload* portion only – the V2GTP header is added by the caller
    /// (typically <see cref="V2GTP_Frame.Wrap"/>, or via the
    /// <see cref="ISDPMessageExtensions.EncodeFrame"/> extension).
    /// </summary>
    public interface ISDP_Message
    {

        /// <summary>
        /// Which ISO 15118 revision this message instance belongs to.
        /// </summary>
        SDP_Version        Version        { get; }

        /// <summary>
        /// The V2GTP payload type that should prefix this message on the wire.
        /// </summary>
        V2GTP_PayloadType  PayloadType    { get; }

        /// <summary>
        /// Encode the message body (without V2GTP header) to a new byte array.
        /// </summary>
        Byte[] EncodePayload();

    }

}
