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
using System.Net.Sockets;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.DNS;

using cloud.charging.open.protocols.ISO15118.Transport;

namespace cloud.charging.open.protocols.ISO15118.Tests.Transport;

/// <summary>
/// How the EVCC side finds its SECC: an IP address is itself - a link-local one with its zone - and a host
/// name what the DNS client it was given says, never the resolver of the operating system.
/// </summary>
[TestFixture]
public class TcpV2GClientNameTests
{

    /// <summary>
    /// A DNS client that names the loopback for every host, and records what it was asked.
    /// </summary>
    private sealed class LoopbackDNS : IDNSClient
    {

        private static readonly DNSServerConfig origin = new (IPv4Address.Localhost, IPPort.Parse(53));

        public List<String> Asked { get; } = [];

        public Task<DNSInfo> Query(DomainName DomainName, IEnumerable<DNSResourceRecordTypes> ResourceRecordTypes, TimeSpan? Timeout = null,
                                   Boolean? RecursionDesired = true, Boolean? ForceUpdate = false, CancellationToken CancellationToken = default)
            => Query(DNSServiceName.Parse(DomainName.FullName), ResourceRecordTypes, Timeout, RecursionDesired, ForceUpdate, CancellationToken);

        public Task<DNSInfo> Query(DNSServiceName DNSServiceName, IEnumerable<DNSResourceRecordTypes> ResourceRecordTypes, TimeSpan? Timeout = null,
                                   Boolean? RecursionDesired = true, Boolean? ForceUpdate = false, CancellationToken CancellationToken = default)
        {

            lock (Asked)
                Asked.Add(DNSServiceName.FullName.TrimEnd('.'));

            IDNSResourceRecord[] answers = ResourceRecordTypes.Contains(DNSResourceRecordTypes.A)
                                               ? [ new A(DNSServiceName, DNSQueryClasses.IN, TimeSpan.FromMinutes(1), IPv4Address.Localhost) ]
                                               : [];

            return Task.FromResult(new DNSInfo(origin, 0, true, false, true, true, DNSResponseCodes.NoError,
                                               answers, [], [], true, false, TimeSpan.FromSeconds(5), TimeSpan.Zero));

        }

        public void Dispose() { }

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;

    }

    [Test]
    [CancelAfter(20000)]
    public async Task AHostNameIsResolvedWithTheDNSClientOnly(CancellationToken CancellationToken)
    {

        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();

        try
        {

            var port      = ((IPEndPoint) listener.LocalEndpoint).Port;
            var accepted  = listener.AcceptTcpClientAsync(CancellationToken).AsTask();
            var dnsClient = new LoopbackDNS();

            // .test is a name no resolver of an operating system answers, and one a name resolution library
            // hands to its DNS client as any other (RFC 6761 §6.2) - unlike .invalid, which it answers itself
            // with "no such name" and asks no DNS client for (§6.4).
            await using (var stream = await TcpV2GClient.ConnectAsync("secc.test", port, dnsClient: dnsClient, ct: CancellationToken))
            {
                using var secc = await accepted;
                Assert.That(secc.Connected, Is.True);
            }

            Assert.That(dnsClient.Asked.Distinct(), Is.EqualTo(new[] { "secc.test" }));

            // The same name without a DNS client: refused for that, not for being an "invalid" one.
            await Assert.ThrowsAsync<NameResolutionException>(async () => await TcpV2GClient.ConnectAsync("secc.test", port, ct: CancellationToken),
                                                        "A host name without a DNS client was resolved some other way.");

        }
        finally
        {
            listener.Stop();
        }

    }

    [Test]
    [CancelAfter(20000)]
    public async Task AnAddressNeedsNoDNSClient(CancellationToken CancellationToken)
    {

        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();

        try
        {

            var port     = ((IPEndPoint) listener.LocalEndpoint).Port;
            var accepted = listener.AcceptTcpClientAsync(CancellationToken).AsTask();

            await using var stream = await TcpV2GClient.ConnectAsync("127.0.0.1", port, ct: CancellationToken);
            using var secc = await accepted;

            Assert.That(secc.Connected, Is.True);

        }
        finally
        {
            listener.Stop();
        }

    }

    [Test]
    public void AnEndpointsZoneReachesTheSocket()
    {

        // V2GEndpoint.ConnectHost writes a link-local address's zone as a number; that number is the
        // interface the connection leaves by, and must not be lost on the way to the socket.
        Assert.That(NameResolution.TryLiteral("fe80::1%14", out var addresses), Is.True);
        Assert.That(addresses.Single().ScopeId,                                 Is.EqualTo(14));

    }

}
