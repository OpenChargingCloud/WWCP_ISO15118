/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

namespace cloud.charging.open.protocols.ISO15118.T1S.Transport
{

    /// <summary>
    /// Which medium a node attaches to, as a configuration names it.
    /// </summary>
    /// <remarks>
    /// The same four words the SLAC side of a charging station uses, with the
    /// same rule behind them: only the real medium is ever chosen by itself.
    /// The emulated one has to be asked for, because a vehicle that quietly
    /// joined a multicast group on a machine without a 10BASE-T1S adapter
    /// would look attached and be talking to nothing a coupler has.
    /// </remarks>
    public enum T1STransportKind
    {

        /// <summary>
        /// No bus at all: a CCS vehicle, or a station without an MCS coupler.
        /// </summary>
        None,

        /// <summary>
        /// The real adapter where there is one - AF_PACKET on Linux, on the
        /// named interface - and nothing anywhere else. Never the emulated
        /// medium.
        /// </summary>
        Auto,

        /// <summary>
        /// A real 10BASE-T1S adapter: EtherType 0x88B5 over AF_PACKET. Linux
        /// only, and needs CAP_NET_RAW.
        /// </summary>
        AfPacket,

        /// <summary>
        /// The emulated medium: one UDP multicast group every node joins. For a
        /// bench without an adapter, and asked for explicitly.
        /// </summary>
        UDP

    }


    /// <summary>
    /// How a transport kind is written in a configuration file, on a command
    /// line and on a page, and read back.
    /// </summary>
    public static class T1STransportKinds
    {

        /// <summary>
        /// The words a configuration may use, in the spelling they are written
        /// back in.
        /// </summary>
        public static readonly IReadOnlyList<String>  Words  = [ "none", "auto", "afpacket", "udp" ];

        #region TryParse(Text, out Kind)

        /// <summary>
        /// One of the four words, in any case; "af_packet" and "af-packet" are
        /// taken as well, because that is how the kernel spells it.
        /// </summary>
        public static Boolean TryParse(String? Text, out T1STransportKind Kind)
        {

            switch (Text?.Trim().ToLowerInvariant())
            {

                case "none":
                    Kind = T1STransportKind.None;
                    return true;

                case "auto":
                    Kind = T1STransportKind.Auto;
                    return true;

                case "afpacket":
                case "af_packet":
                case "af-packet":
                    Kind = T1STransportKind.AfPacket;
                    return true;

                case "udp":
                    Kind = T1STransportKind.UDP;
                    return true;

                default:
                    Kind = T1STransportKind.None;
                    return false;

            }

        }

        #endregion

        #region Write(Kind)

        /// <summary>
        /// The word for a kind.
        /// </summary>
        public static String Write(this T1STransportKind Kind)

            => Kind switch {
                   T1STransportKind.Auto      => "auto",
                   T1STransportKind.AfPacket  => "afpacket",
                   T1STransportKind.UDP       => "udp",
                   _                          => "none"
               };

        #endregion

    }

}
