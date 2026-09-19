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

#region Usings

using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

#endregion

namespace cloud.charging.open.protocols.ISO15118.T1S.Tests
{

    /// <summary>
    /// A UDP port a test bus can have to itself.
    /// </summary>
    /// <remarks>
    /// Drawn at random, and tried before it is handed out: Windows keeps
    /// ranges of ports for Hyper-V and WSL that nothing else may bind, and
    /// they move between reboots, so a port that was merely random failed a
    /// test one run in fifty with "access denied" and nothing to do with the
    /// bus.
    /// </remarks>
    internal static class TestPorts
    {

        public static UInt16 Free()
        {

            for (var attempt = 0; attempt < 25; attempt++)
            {

                var port = (UInt16) RandomNumberGenerator.GetInt32(20000, 60000);

                try
                {

                    using var probe = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                    probe.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    probe.Bind(new IPEndPoint(IPAddress.Any, port));

                    return port;

                }
                catch (SocketException)
                {
                    // Reserved, or in use: draw again.
                }

            }

            throw new InvalidOperationException("No UDP port between 20000 and 60000 could be bound in 25 attempts.");

        }

    }

}
