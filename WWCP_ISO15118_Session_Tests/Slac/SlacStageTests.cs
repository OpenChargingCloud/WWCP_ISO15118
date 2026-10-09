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

using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

using cloud.charging.open.protocols.ISO15118.Slac;
using cloud.charging.open.protocols.ISO15118.SLAC.StateMachine;
using cloud.charging.open.protocols.ISO15118.SLAC.Transport;

namespace cloud.charging.open.protocols.ISO15118.Tests.Slac;

/// <summary>
/// A vehicle's and a station's SLAC stage pairing over the simulated medium on the loopback: each says who
/// the other side is, and the vehicle which station it chose and every one that answered. Both said the
/// network alone, and a vehicle could not tell which station it had paired with.
/// </summary>
[TestFixture]
public class SlacStageTests
{

    /// <summary>A random MAC address for a simulated SLAC node.</summary>
    private static MACAddress RandomMac()
        => MACAddress.FromPhysicalAddress(new PhysicalAddress(RandomNumberGenerator.GetBytes(6)));

    [Test]
    public async Task EachSideSaysWhoTheOtherIsAndTheVehicleWhichStationItChose()
    {

        var nid = RandomNumberGenerator.GetBytes(7);

        await using var evseTransport  = new UdpSlacTransport(RandomMac(), new IPEndPoint(IPAddress.Loopback, 0));
        await using var station        = new SlacEvseStage(evseTransport,
                                                           new EvseSlacOptions {
                                                               EvseId  = new byte[17],
                                                               Nid     = nid,
                                                               Nmk     = RandomNumberGenerator.GetBytes(16)
                                                           });

        await station.StartAsync();

        var matched = station.WaitForMatchAsync();

        await using var evTransport    = new UdpSlacTransport(RandomMac(),
                                                              new IPEndPoint(IPAddress.Loopback, 0),
                                                              bootstrapPeers: [ evseTransport.LocalEndpoint ]);

        var vehicle   = await new SlacEvStage(evTransport, new EvSlacOptions { PevId = new byte[17] }).
                                  PairAsync().WaitAsync(TimeSpan.FromSeconds(10));

        var stations  = await matched.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Multiple(() => {

            Assert.That(vehicle.Nid,                                  Is.EqualTo(nid));
            Assert.That(vehicle.Peer,                                 Is.EqualTo(evseTransport.LocalMac), "the vehicle's other side is the station");
            Assert.That(vehicle.Station,                              Is.Not.Null);
            Assert.That(vehicle.Station!.EVSEMACAddress,              Is.EqualTo(evseTransport.LocalMac), "the station chosen is the one paired with");
            Assert.That(vehicle.Station.AverageAttenuation,           Is.Not.Null,                        "with how loudly it heard the vehicle");
            Assert.That(vehicle.Candidates.Select(one => one.EVSEMACAddress),
                                                                      Is.EqualTo(new[] { evseTransport.LocalMac }), "every station that answered");

            Assert.That(stations.Nid,                                 Is.EqualTo(nid));
            Assert.That(stations.Peer,                                Is.EqualTo(evTransport.LocalMac),   "the station's other side is the vehicle");
            Assert.That(stations.Station,                             Is.Null,                            "a station chooses among nobody");
            Assert.That(stations.Candidates,                          Is.Empty);

        });

    }

}
