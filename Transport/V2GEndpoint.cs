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
using System.Net.Sockets;

namespace Vanaheimr.V2G.Simulation.Transport
{

    /// <summary>
    /// A peer address as a human writes it — <c>host:port</c>, <c>[ipv6]:port</c>, or
    /// <c>[ipv6%zone]:port</c> — parsed once, checked, and resolved to something a socket cannot
    /// misunderstand.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this exists rather than <see cref="IPEndPoint.TryParse(string, out IPEndPoint)"/>.</b> An
    /// ISO 15118 EVCC reaches its station over an IPv6 <i>link-local</i> address, and a link-local
    /// address without its zone is not an address: <c>fe80::1</c> alone does not say which wire to put
    /// the packet on. The zone is written as <c>%eth0</c> or <c>%evcc-veth</c> — the form every
    /// counterparty's documentation uses, ours included.
    /// </para>
    /// <para>
    /// The framework's parsers accept that form and <b>silently discard the zone when the name is not an
    /// interface on this machine</b>. Measured, .NET 10 on macOS:
    /// </para>
    /// <code>
    /// IPAddress.TryParse("fe80::1%en0")        -> true, fe80::1%14   (resolved)
    /// IPAddress.TryParse("fe80::1%evcc-veth")  -> true, fe80::1      (zone gone, no error)
    /// IPEndPoint.TryParse("[fe80::1%evcc-veth]:9000") -> true, [fe80::1]:9000
    /// Dns.GetHostAddressesAsync("fe80::1%evcc-veth")  -> fe80::1, ScopeId 0
    /// </code>
    /// <para>
    /// So a mistyped interface, or a correct one that does not exist yet because the veth pair has not
    /// been created, or a run from inside a container that does not have it, all produce the same thing:
    /// a scope-0 link-local connect that fails with a generic socket error pointing at the <i>peer</i>.
    /// The one fact that would have explained it — that the zone was dropped — is the fact the parser
    /// threw away. This is the recurring lesson in <c>docs/CONCEPT.md</c> §5 in yet another place: a
    /// check made tolerant of a legitimate variation stops seeing what that variation was hiding.
    /// </para>
    /// <para>
    /// Hence: parse the zone ourselves, resolve it ourselves, and refuse loudly — naming the interfaces
    /// that do exist — rather than connecting to an address that has quietly lost the half that mattered.
    /// </para>
    /// </remarks>
    /// <param name="Host">The host as written, without brackets and without the zone: a DNS name, or an
    /// IP literal.</param>
    /// <param name="Port">The TCP port.</param>
    /// <param name="Zone">The zone exactly as written (<c>eth0</c>, <c>14</c>), or <c>null</c>.</param>
    /// <param name="Address">The parsed address, carrying a resolved <see cref="IPAddress.ScopeId"/> when
    /// a zone was given — or <c>null</c> when <paramref name="Host"/> is a name to be resolved later.</param>
    public sealed record V2GEndpoint(string Host, int Port, string? Zone, IPAddress? Address)
    {

        /// <summary>
        /// What to hand to <see cref="TcpV2GClient"/>: for a literal, the address with its zone as a
        /// <b>number</b>, which no parser can lose; for a name, the name.
        /// </summary>
        public string ConnectHost => Address?.ToString() ?? Host;

        public IPEndPoint? IPEndPoint => Address is null ? null : new IPEndPoint(Address, Port);

        public override string ToString()
            => Address is { AddressFamily: AddressFamily.InterNetworkV6 } || Host.Contains(':')
                   ? $"[{ConnectHost}]:{Port}"
                   : $"{ConnectHost}:{Port}";


        /// <param name="origin">What to name in an error message — the flag or environment variable the
        /// value came from, e.g. <c>--connect</c> or <c>V2G_INTEROP_SECC</c>.</param>
        /// <exception cref="ArgumentException">The value is not an endpoint, or names a zone this machine
        /// does not have.</exception>
        public static V2GEndpoint Parse(String value, String origin)
        {

            if (String.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{origin} expects host:port, got an empty value.");

            var text = value.Trim();
            String host;
            String portText;

            if (text.StartsWith('['))
            {
                var close = text.IndexOf(']');
                if (close < 2 || close + 2 > text.Length - 1 || text[close + 1] != ':')
                    throw new ArgumentException($"{origin} expects [ipv6]:port, got '{value}'.");
                host     = text[1..close];
                portText = text[(close + 2)..];
            }
            else
            {
                var colon = text.LastIndexOf(':');
                if (colon <= 0 || colon == text.Length - 1)
                    throw new ArgumentException($"{origin} expects host:port, got '{value}'.");

                host     = text[..colon];
                portText = text[(colon + 1)..];

                // An unbracketed IPv6 literal cannot be split at a colon at all — "fe80::1:9000" is a
                // perfectly good address, so guessing which colon separates the port would sometimes
                // guess wrong and connect somewhere else entirely.
                if (host.Contains(':'))
                    throw new ArgumentException(
                        $"{origin}: an IPv6 literal must be bracketed so its colons cannot be read as a " +
                        $"port separator — write [{host}]:{portText}, got '{value}'.");
            }

            if (!Int32.TryParse(portText, out var port) || port is < 1 or > 65535)
                throw new ArgumentException($"{origin}: '{portText}' is not a TCP port (1-65535), from '{value}'.");

            // The zone is split off before parsing, deliberately: handing "%evcc-veth" to the framework
            // is what loses it. See the class remarks.
            String? zone = null;
            var percent = host.IndexOf('%');
            if (percent >= 0)
            {
                zone = host[(percent + 1)..];
                host = host[..percent];
                if (zone.Length == 0)
                    throw new ArgumentException($"{origin}: '{value}' ends in '%' with no zone after it.");
            }

            if (host.Length == 0)
                throw new ArgumentException($"{origin} expects host:port, got '{value}'.");

            if (!IPAddress.TryParse(host, out var address))
            {
                if (zone is not null)
                    throw new ArgumentException(
                        $"{origin}: '{host}' is a name, not an address, so '%{zone}' means nothing — a zone " +
                        $"identifies the interface a link-local address lives on. From '{value}'.");
                return new V2GEndpoint(host, port, null, null);
            }

            if (zone is null)
            {
                // Not an error for a routable address; fatal for a link-local one, which is exactly the
                // address a station hands out. Refused here rather than at connect time, because at
                // connect time it is indistinguishable from "the station is not listening".
                if (address.IsIPv6LinkLocal)
                    throw new ArgumentException(
                        $"{origin}: '{host}' is a link-local address and carries no zone, so it does not say " +
                        $"which interface to use. Write [{host}%<interface>]:{port} — this machine has " +
                        $"{DescribeInterfaces()}. From '{value}'.");

                return new V2GEndpoint(host, port, null, address);
            }

            if (address.AddressFamily != AddressFamily.InterNetworkV6)
                throw new ArgumentException(
                    $"{origin}: '{host}' is an IPv4 address and has no zone; '%{zone}' cannot apply to it. " +
                    $"From '{value}'.");

            address.ScopeId = ResolveZone(zone, origin, value);

            return new V2GEndpoint(host, port, zone, address);

        }


        /// <summary>A zone is a scope id: a number, or the name of an interface that has one.</summary>
        private static long ResolveZone(String zone, String origin, String value)
        {

            // A number is already what the socket layer wants. Not checked against the live interface
            // list on purpose: an index is also how a zone is written on a machine whose interface
            // naming this process cannot see (a container, a netns), and refusing it would be refusing
            // the one form that always works.
            if (Int64.TryParse(zone, out var numeric) && numeric >= 0)
                return numeric;

            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (!String.Equals(nic.Name, zone, StringComparison.Ordinal) &&
                    !String.Equals(nic.Id,   zone, StringComparison.Ordinal))
                    continue;

                try
                {
                    return nic.GetIPProperties().GetIPv6Properties().Index;
                }
                catch (NetworkInformationException)
                {
                    throw new ArgumentException(
                        $"{origin}: interface '{zone}' exists but has no IPv6 configured, so it cannot carry " +
                        $"a link-local session. From '{value}'.");
                }
            }

            throw new ArgumentException(
                $"{origin}: this machine has no interface called '{zone}' — it has {DescribeInterfaces()}. " +
                $"A zone that names nothing is silently dropped by the platform's own parsers, which turns " +
                $"a typo (or an interface that has not been created yet) into a connection failure that " +
                $"looks like the peer's fault. From '{value}'.");

        }


        private static String DescribeInterfaces()
        {

            var names = new List<String>();

            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                try
                {
                    names.Add($"{nic.Name} ({nic.GetIPProperties().GetIPv6Properties().Index})");
                }
                catch (NetworkInformationException)
                { }   // no IPv6 on this one: it could not carry the session anyway
            }

            return names.Count == 0
                       ? "no IPv6-capable interfaces at all"
                       : String.Join(", ", names);

        }

    }

}
