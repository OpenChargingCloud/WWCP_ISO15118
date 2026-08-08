/*
 * Copyright (c) 2021-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

using System.Net.NetworkInformation;
using System.Security.Cryptography;

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

using cloud.charging.open.protocols.ISO15118.NetworkInterfaces;
using cloud.charging.open.protocols.ISO15118.StateMachines;

namespace cloud.charging.open.protocols.ISO15118.SharedCC
{
    /// <summary>Small things both programs need to say the same way.</summary>
    public static class V2GInterface
    {

        /// <summary>
        /// The V2G-capable interface with this name, or a refusal that lists what the machine does have —
        /// the wrong interface name is the most common way an <c>--sdp</c> run fails, and "not found" on
        /// its own leaves you guessing at spelling.
        /// </summary>
        public static V2GNetworkInterface Resolve(string name)
        {
            var provider = new SystemV2GNetworkInterfaceProvider();
            if (provider.FindByName(name) is { } found)
                return found;

            var available = provider.Discover().Select(i => i.Name).ToArray();
            throw new ArgumentException(
                $"no V2G-capable network interface named '{name}'. " +
                (available.Length > 0
                     ? $"This machine has: {string.Join(", ", available)}."
                     : "This machine has none — a V2G interface needs an IPv6 link-local address."));
        }

        /// <summary>A random locally-administered MAC for a simulated SLAC node.</summary>
        public static MACAddress RandomMac()
            => MACAddress.FromPhysicalAddress(new PhysicalAddress(RandomNumberGenerator.GetBytes(6)));

        /// <summary>How the two protocols are written in this project's logs and run notes.</summary>
        public static string Name(ProtocolVariant protocol)
            => protocol == ProtocolVariant.Iso15118_2 ? "-2" : "-20";

        /// <summary>How the two power modes are written.</summary>
        public static string Name(PowerMode mode)
            => mode == PowerMode.Ac ? "AC" : "DC";

    }
}
