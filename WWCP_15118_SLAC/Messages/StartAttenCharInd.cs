/*
 * Copyright (c) 2021-2025 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP ISO/IEC 15118 <https://github.com/OpenChargingCloud/WWCP_ISO15118>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#region Usings

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

#endregion

namespace cloud.charging.open.protocols.ISO15118.SLAC.Messages
{

    // =========================================================================
    // CM_START_ATTEN_CHAR.IND   EV → EVSE   (broadcast, sent 3x)
    // =========================================================================
    public sealed record StartAttenCharInd(
        byte       NumSounds,
        byte       TimeOut,
        byte       RespType,
        MACAddress ForwardingSta,
        RunId      RunId) : ISlacMessage
    {
        public ManagementMessageType MmType => ManagementMessageType.CM_START_ATTEN_CHAR_IND;

        public Byte[] Encode()
        {
            var buf = new Byte[1 + 1 + 1 + 1 + 1 + 6 + 8];
            var span = buf.AsSpan();

            span[0] = SLACConstants.ApplicationType_PevEvseMatching;
            span[1] = SLACConstants.SecurityType_None;
            span[2] = NumSounds;
            span[3] = TimeOut;
            span[4] = RespType;
            ForwardingSta.CopyTo(span.Slice(5, 6));
            RunId.CopyTo(span.Slice(11, 8));

            return buf;
        }

        public static StartAttenCharInd Decode(ReadOnlySpan<Byte> body)
        {
            if (body.Length < 19) throw new InvalidDataException("CM_START_ATTEN_CHAR.IND truncated.");

            return new StartAttenCharInd(
                NumSounds    : body[2],
                TimeOut      : body[3],
                RespType     : body[4],
                ForwardingSta: MACAddress.From(body.Slice(5, 6)),
                RunId        : new RunId(body.Slice(11, 8))
            );
        }
    }



}
