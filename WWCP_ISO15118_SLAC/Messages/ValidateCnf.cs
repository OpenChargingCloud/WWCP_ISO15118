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


    // =========================================================================
    // CM_VALIDATE.CNF       EV → EVSE   (unicast)
    // =========================================================================
    public sealed record ValidateCnf(
        SignalType SignalType,
        byte       ToggleNum,
        ValidateResult Result) : ISlacMessage
    {
        public ManagementMessageType MmType => ManagementMessageType.CM_VALIDATE_CNF;

        public Byte[] Encode()
        {
            var buf = new Byte[1 + 1 + 1 + 1 + 1];
            buf[0] = SLACConstants.ApplicationType_PevEvseMatching;
            buf[1] = SLACConstants.SecurityType_None;
            buf[2] = (byte) SignalType;
            buf[3] = ToggleNum;
            buf[4] = (byte) Result;
            return buf;
        }

        public static ValidateCnf Decode(ReadOnlySpan<Byte> body)
        {
            if (body.Length < 5) throw new InvalidDataException("CM_VALIDATE.CNF truncated.");
            return new ValidateCnf(
                SignalType: (SignalType) body[2],
                ToggleNum : body[3],
                Result    : (ValidateResult) body[4]
            );
        }
    }


}
