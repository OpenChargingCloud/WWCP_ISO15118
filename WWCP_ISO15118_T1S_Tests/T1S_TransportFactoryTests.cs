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
using System.Net.NetworkInformation;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

using cloud.charging.open.protocols.ISO15118.T1S.Transport;

#endregion

namespace cloud.charging.open.protocols.ISO15118.T1S.Tests
{

    /// <summary>
    /// The one decision a configuration turns into a medium: which kind, and
    /// what to say when it cannot be had.
    /// </summary>
    /// <remarks>
    /// The emulated medium is opened for real here, on ports of its own; the
    /// real adapter is asked for and refused, which on every machine without
    /// one - and on every machine without CAP_NET_RAW - is the answer worth
    /// testing. Where an answer depends on the operating system the test
    /// says which answer it expects on which.
    /// </remarks>
    [TestFixture]
    public class T1S_TransportFactoryTests
    {

        #region The words

        [TestCase("udp",        T1STransportKind.UDP)]
        [TestCase("UDP",        T1STransportKind.UDP)]
        [TestCase(" udp ",      T1STransportKind.UDP)]
        [TestCase("afpacket",   T1STransportKind.AfPacket)]
        [TestCase("AF_PACKET",  T1STransportKind.AfPacket)]
        [TestCase("af-packet",  T1STransportKind.AfPacket)]
        [TestCase("auto",       T1STransportKind.Auto)]
        [TestCase("none",       T1STransportKind.None)]
        public void EveryWordIsRead(String Text, T1STransportKind Expected)
        {
            Assert.That(T1STransportKinds.TryParse(Text, out var kind), Is.True);
            Assert.That(kind, Is.EqualTo(Expected));
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("multicast")]
        [TestCase("pcap")]
        public void AnythingElseIsNot(String? Text)
        {
            Assert.That(T1STransportKinds.TryParse(Text, out _), Is.False);
        }

        [Test]
        public void WhatIsWrittenIsWhatIsRead()
        {
            foreach (var kind in Enum.GetValues<T1STransportKind>())
            {
                Assert.That(T1STransportKinds.TryParse(kind.Write(), out var back), Is.True, kind.ToString());
                Assert.That(back, Is.EqualTo(kind));
            }
        }

        #endregion

        #region None, and Auto where there is nothing

        [Test]
        public void NoneIsNoBusAndNoError()
        {

            var medium = T1STransports.Open(new T1STransportOptions(T1STransportKind.None));

            Assert.That(medium.IsOpen,    Is.False);
            Assert.That(medium.IsFailed,  Is.False);
            Assert.That(medium.Kind,      Is.EqualTo(T1STransportKind.None));
            Assert.That(medium.Reason,    Does.StartWith("no bus"));

        }

        [Test]
        public void AutoNeverPicksTheEmulatedMedium()
        {

            // On Linux, Auto with an interface name tries the adapter; without
            // one it declines. Anywhere else it declines because there is no
            // AF_PACKET. It never answers with UDP, which is the rule.
            var medium = T1STransports.Open(new T1STransportOptions(T1STransportKind.Auto));

            Assert.That(medium.Kind,      Is.Not.EqualTo(T1STransportKind.UDP));
            Assert.That(medium.IsOpen,    Is.False);
            Assert.That(medium.IsFailed,  Is.False, "declining is not failing");
            Assert.That(medium.Reason,    OperatingSystem.IsLinux()
                                              ? Does.Contain("name the interface")
                                              : Does.Contain("Linux only"));

        }

        #endregion

        #region The real adapter, refused

        [Test]
        public void AfPacketIsAnErrorWhereThereIsNoAdapterToBeHad()
        {

            var medium = T1STransports.Open(new T1STransportOptions(T1STransportKind.AfPacket, "no-such-interface-0"));

            Assert.That(medium.IsOpen,    Is.False);
            Assert.That(medium.IsFailed,  Is.True);
            Assert.That(medium.Kind,      Is.EqualTo(T1STransportKind.AfPacket));

            // Windows and macOS: there is no AF_PACKET at all. Linux: there is
            // no such interface - or, as somebody without CAP_NET_RAW, no raw
            // socket at all; both are said in as many words.
            Assert.That(medium.Error,     OperatingSystem.IsLinux()
                                              ? Does.Contain("no-such-interface-0").Or.Contain("CAP_NET_RAW")
                                              : Does.Contain("Linux only"));

        }

        [Test]
        public void AfPacketNeedsAnInterfaceName()
        {

            var medium = T1STransports.Open(new T1STransportOptions(T1STransportKind.AfPacket));

            Assert.That(medium.IsFailed,  Is.True);
            Assert.That(medium.Error,     OperatingSystem.IsLinux()
                                              ? Does.Contain("name of the interface")
                                              : Does.Contain("Linux only"));

        }

        #endregion

        #region The emulated medium, opened

        [Test]
        public async Task UDPOpensTheEmulatedMediumOnTheGroupAsked()
        {

            var port    = TestPorts.Free();
            var group   = new IPEndPoint(IPAddress.Parse("239.151.18.77"), port);
            var medium  = T1STransports.Open(new T1STransportOptions(T1STransportKind.UDP, Group: group));

            Assert.That(medium.IsFailed,   Is.False, medium.Error);
            Assert.That(medium.IsOpen,     Is.True);
            Assert.That(medium.Kind,       Is.EqualTo(T1STransportKind.UDP));
            Assert.That(medium.Transport,  Is.InstanceOf<UdpMulticastT1STransport>());
            Assert.That(medium.Reason,     Does.Contain($"239.151.18.77:{port}"));

            var udp = (UdpMulticastT1STransport) medium.Transport!;

            Assert.That(udp.Group,                            Is.EqualTo(group));
            Assert.That(udp.Interface,                        Is.Null, "nothing named, so the operating system picks");
            Assert.That(udp.LocalMac.GetBytes()[0] & 0x02,    Is.EqualTo(0x02), "a made-up address is locally administered");

            await udp.DisposeAsync();

        }

        [Test]
        public async Task UDPKeepsTheAddressItWasGiven()
        {

            var mac     = MACAddress.Parse("02:71:50:AA:BB:CC");
            var medium  = T1STransports.Open(new T1STransportOptions(T1STransportKind.UDP,
                                                                     Group:     new IPEndPoint(IPAddress.Parse("239.151.18.78"), TestPorts.Free()),
                                                                     LocalMac:  mac));

            Assert.That(medium.IsOpen,               Is.True, medium.Error);
            Assert.That(medium.Transport!.LocalMac,  Is.EqualTo(mac));

            await medium.Transport.DisposeAsync();

        }

        [Test]
        public void UDPRefusesAnInterfaceThisMachineDoesNotHave()
        {

            var medium = T1STransports.Open(new T1STransportOptions(T1STransportKind.UDP, InterfaceName: "no-such-interface-0"));

            Assert.That(medium.IsOpen,    Is.False);
            Assert.That(medium.IsFailed,  Is.True);
            Assert.That(medium.Error,     Does.Contain("no-such-interface-0"));

        }

        [Test]
        public async Task UDPJoinsOnTheInterfaceNamed()
        {

            // Whichever interface of this machine is up and has an IPv4
            // address; a machine with none cannot run this and says so.
            var candidate = NetworkInterface.GetAllNetworkInterfaces()
                                            .Where(nic => nic.OperationalStatus == OperationalStatus.Up &&
                                                          nic.SupportsMulticast)
                                            .Select(nic => (nic.Name,
                                                            Address: nic.GetIPProperties()
                                                                        .UnicastAddresses
                                                                        .Select(u => u.Address)
                                                                        .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)))
                                            .FirstOrDefault(pair => pair.Address is not null);

            if (candidate.Address is null)
                Assert.Ignore("No interface with an IPv4 address on this machine.");

            var medium = T1STransports.Open(new T1STransportOptions(T1STransportKind.UDP,
                                                                    InterfaceName:  candidate.Name,
                                                                    Group:          new IPEndPoint(IPAddress.Parse("239.151.18.79"), TestPorts.Free())));

            Assert.That(medium.IsOpen,  Is.True, medium.Error);

            var udp = (UdpMulticastT1STransport) medium.Transport!;

            Assert.That(udp.Interface,  Is.EqualTo(candidate.Address));
            Assert.That(udp.Description, Does.Contain(candidate.Address.ToString()));

            await udp.DisposeAsync();

        }

        [Test]
        public void UDPRefusesAGroupThatIsNotOne()
        {

            var medium = T1STransports.Open(new T1STransportOptions(T1STransportKind.UDP,
                                                                    Group: new IPEndPoint(IPAddress.Loopback, TestPorts.Free())));

            Assert.That(medium.IsFailed,  Is.True);
            Assert.That(medium.Error,     Does.Contain("multicast"));

        }

        #endregion

    }

}
