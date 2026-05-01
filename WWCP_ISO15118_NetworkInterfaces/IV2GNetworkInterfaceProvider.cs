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

namespace cloud.charging.open.protocols.ISO15118.NetworkInterfaces
{

    /// <summary>
    /// Looks up network interfaces and returns the V2G-relevant subset.
    /// Pluggable so unit tests can swap a fake provider in.
    /// </summary>
    public interface IV2GNetworkInterfaceProvider
    {

        /// <summary>
        /// All interfaces that look like V2G candidates.
        /// </summary>
        IReadOnlyList<V2GNetworkInterface> Discover();

        /// <summary>
        /// Look up an interface by its OS name (e.g. "eth1", "plc0").
        /// </summary>
        V2GNetworkInterface? FindByName(String NetworkInterfaceName);

    }

}
