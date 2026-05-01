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

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

#endregion

namespace cloud.charging.open.protocols.ISO15118.SLAC.Messages
{

    /// <summary>
    /// CM_SLAC_PARM.CNF: EVSE → EV (unicast)
    /// </summary>
    /// <param name="MSoundTarget">The SLAC sounding target address (MAC address) to which the SLAC soundings should be sent.
    /// <param name="NumSounds">The number of SLAC soundings to be sent by the PEV.
    /// This is used by the PEV to determine how many SLAC soundings it should send before giving up and reporting a failure to the user.</param>
    /// <param name="TimeOut">The SLAC timeout value in seconds. This is used by the PEV to determine how long it should wait for a SLAC response before retransmitting the SLAC soundings.</param>
    /// <param name="RespType">The SLAC response type. This is used by the PEV to determine whether the SLAC soundings should be unicast or broadcast.</param>
    /// <param name="ForwardingSta">The forwarding station address (MAC address) to which the SLAC soundings should be forwarded.
    /// This is used by the EVSE to forward SLAC soundings to the PEV when the PEV is behind a switch or a bridge.
    /// The forwarding station address is set to 00:00:00:00:00:00 if the SLAC soundings should not be forwarded.</param>
    /// <param name="RunId">The SLAC run ID. This is used by the PEV to determine whether the SLAC soundings are part of the same SLAC run or not.</param>
    public sealed record SLACParmCnf(MACAddress  MSoundTarget,
                                     Byte        NumSounds,
                                     Byte        TimeOut,
                                     Byte        RespType,
                                     MACAddress  ForwardingSta,
                                     RunId       RunId)

        : ISlacMessage

    {

        public ManagementMessageType MmType
            => ManagementMessageType.CM_SLAC_PARM_CNF;

        public Byte[] Encode()
        {

            var bytes  = new Byte[6 + 1 + 1 + 1 + 6 + 1 + 1 + 8];
            var span   = bytes.AsSpan();

            MSoundTarget.CopyTo(span[..6]);
            span[ 6]   = NumSounds;
            span[ 7]   = TimeOut;
            span[ 8]   = RespType;
            ForwardingSta.CopyTo(span.Slice(9, 6));
            span[15]   = SLACConstants.ApplicationType_PevEvseMatching;
            span[16]   = SLACConstants.SecurityType_None;
            RunId.CopyTo(span.Slice(17, 8));

            return bytes;

        }

        public static SLACParmCnf Decode(ReadOnlySpan<Byte> Bytes)
        {

            if (Bytes.Length < 25)
                throw new InvalidDataException("CM_SLAC_PARM.CNF truncated.");

            return new SLACParmCnf(
                       MSoundTarget:   MACAddress.From(Bytes[..6]),
                       NumSounds:      Bytes[6],
                       TimeOut:        Bytes[7],
                       RespType:       Bytes[8],
                       ForwardingSta:  MACAddress.From(Bytes.Slice(9, 6)),
                       // body[15] = APPLICATION_TYPE, body[16] = SECURITY_TYPE
                       RunId:          new RunId(Bytes.Slice(17, 8))
                   );

        }

    }

}
