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

using System.Net.NetworkInformation;

#endregion

namespace cloud.charging.open.protocols.ISO15118.NetworkInterfaces
{

    public sealed class SystemV2GNetworkInterfaceProvider : IV2GNetworkInterfaceProvider
    {
        public IReadOnlyList<V2GNetworkInterface> Discover()

            => [.. NetworkInterface.GetAllNetworkInterfaces().
                   Where (V2GNetworkInterface.LooksLikeV2GCandidate).
                   Select(Convert).
                   Where (networkInterface => networkInterface is not null).
                   Cast<V2GNetworkInterface>()];

        public V2GNetworkInterface? FindByName(String NetworkInterfaceName)
        {

            var networkInterface = NetworkInterface.GetAllNetworkInterfaces().
                                       FirstOrDefault(nic => String.Equals(
                                                                 nic.Name,
                                                                 NetworkInterfaceName,
                                                                 StringComparison.OrdinalIgnoreCase
                                                             ));

            return networkInterface is null
                       ? null
                       : Convert(networkInterface);

        }

        private static V2GNetworkInterface? Convert(NetworkInterface NetworkInterface)
        {

            var ipInterfaceProperties  = NetworkInterface.GetIPProperties();

            var index                  = ipInterfaceProperties.GetIPv6Properties()?.Index ??
                                         ipInterfaceProperties.GetIPv4Properties()?.Index;
            if (index is null)
                return null;

            var linkLocalIPAddress     = ipInterfaceProperties.UnicastAddresses.
                                             FirstOrDefault(unicastIPAddressInformation => unicastIPAddressInformation.Address.IsIPv6LinkLocal)?.Address;
            if (linkLocalIPAddress is null)
                return null;

            return new V2GNetworkInterface(
                       index.Value,
                       NetworkInterface.Name,
                       linkLocalIPAddress,
                       NetworkInterface.GetPhysicalAddress().GetAddressBytes()
                   );

        }

    }

}
