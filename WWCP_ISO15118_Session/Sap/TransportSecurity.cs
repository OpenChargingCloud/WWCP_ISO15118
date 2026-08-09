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

using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;

namespace cloud.charging.open.protocols.ISO15118.Sap
{

    /// <summary>
    /// What the connection underneath a SupportedAppProtocol handshake turned out to be — the one fact
    /// <c>[V2G20-1237]</c> and <c>[V2G20-2356]</c> turn on.
    /// </summary>
    public enum TransportSecurity
    {
        /// <summary>Nobody said, and nothing here will guess. Both rules below are then not applied.</summary>
        Unknown,

        /// <summary>Plain TCP.</summary>
        None,

        /// <summary>TLS, but 1.2 or lower — the ISO 15118-2 profile.</summary>
        Tls12OrLower,

        /// <summary>TLS 1.3, which is the only thing ISO 15118-20 may ride on.</summary>
        Tls13,
    }


    /// <summary>
    /// The transport half of protocol negotiation: whether this connection may carry ISO 15118-20 at all,
    /// and how to find out what it is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this exists.</b> ISO 15118-20 says the same thing in three places, and we were on the wrong
    /// side of it. <c>[V2G20-1237]</c>: the EVCC shall not offer <c>-20</c> in the
    /// <c>SupportedAppProtocolReq</c> when the established connection is TLS 1.2 or lower, or plain TCP.
    /// <c>[V2G20-2356]</c> is the same sentence addressed to the SECC — it shall not <i>select</i> <c>-20</c>
    /// there. <c>[V2G20-1805]</c> states both at once from the SDP direction, and all three point at Table 5,
    /// where <c>-20</c> appears in the TLS 1.3 row and nowhere else. Serving TLS 1.2 is explicitly permitted
    /// (<c>[V2G20-2359]</c>); carrying <c>-20</c> on it is not.
    /// </para>
    /// <para>
    /// <b>Found by doing it.</b> On 2026-08-06 our EVCC offered both protocols over a connection that
    /// negotiated TLS 1.2, and EVerest's <c>IsoMux</c> selected the <c>-20</c> entry — a complete DC session
    /// on a profile the standard does not allow, from a car that should not have offered it. Their half is
    /// filed (<c>ISO15118ConformanceTests/docs/reports/everest-isomux-iso20-over-tls12.md</c>); this is ours.
    /// The ClientHello was right — <c>[V2G20-2365]</c> and <c>[V2G20-2062]</c> both ask a backward-compatible
    /// EVCC to offer 1.3 <i>and</i> 1.2, and <c>[V2G20-2064]</c> to continue on whichever the station picked
    /// — and exactly the next step was wrong.
    /// </para>
    /// <para>
    /// <b><see cref="TransportSecurity.Unknown"/> is not an oversight.</b> Most of this project's interop
    /// matrix runs <c>-20</c> over plain TCP on purpose, and a rule that could not be stood down would delete
    /// it. So the handshake applies these requirements only when a caller states what the transport is, and
    /// the runnable peers state it and say out loud when they are proceeding anyway. Silence, not
    /// conformance, was the actual defect.
    /// </para>
    /// </remarks>
    public static class Iso20Transport
    {

        /// <summary>
        /// <c>[V2G20-1237]</c> / <c>[V2G20-2356]</c>: whether ISO 15118-20 may be offered by an EVCC, and
        /// selected by an SECC, on a connection of this kind.
        /// </summary>
        /// <remarks><see cref="TransportSecurity.Unknown"/> answers <c>true</c> — nothing was claimed about
        /// the connection, so there is nothing to hold the offer against.</remarks>
        public static Boolean MayCarryIso20(TransportSecurity transport)

            => transport is TransportSecurity.Tls13
                         or TransportSecurity.Unknown;


        /// <summary>
        /// What <paramref name="stream"/> says about itself, for callers that have the stream and not the
        /// options that built it.
        /// </summary>
        /// <remarks>
        /// Recognises the two shapes <c>TcpV2GClient</c>/<c>TcpV2GListener</c> produce on the .NET stack: an
        /// authenticated <see cref="SslStream"/>, and a bare <see cref="NetworkStream"/>. Everything else —
        /// including the BouncyCastle backend's stream and every in-process test double — comes back
        /// <see cref="TransportSecurity.Unknown"/>, because guessing from a type is how a wrong answer would
        /// get made here. The BouncyCastle path is TLS 1.3 by construction on both sides
        /// (<c>BcV2GTls.Tls13Only</c>, the only value <c>GetProtocolVersions()</c> returns), so a caller on
        /// that path should pass <see cref="TransportSecurity.Tls13"/> rather than ask this.
        /// </remarks>
        public static TransportSecurity Of(Stream stream)

            => stream switch {
                   SslStream { IsAuthenticated: true } ssl => FromSslProtocol(ssl.SslProtocol),
                   NetworkStream                           => TransportSecurity.None,
                   _                                       => TransportSecurity.Unknown,
               };


        /// <summary>
        /// The version half of <see cref="Of"/>, separately because it is the part with a rule in it: only
        /// TLS 1.3 may carry <c>-20</c>, and every named version below it is one that may not.
        /// </summary>
        /// <remarks><see cref="SslProtocols.None"/> is what an unauthenticated stream reports, and it means
        /// "not yet", not "no encryption" — hence <see cref="TransportSecurity.Unknown"/> rather than
        /// <see cref="TransportSecurity.None"/>, which would be the wrong answer in the dangerous
        /// direction.</remarks>
        public static TransportSecurity FromSslProtocol(SslProtocols protocol)

            => protocol switch {
#pragma warning disable SYSLIB0039 // named to be classified, not to be offered
                   SslProtocols.Tls or SslProtocols.Tls11 or SslProtocols.Tls12
#pragma warning restore SYSLIB0039
                                          => TransportSecurity.Tls12OrLower,
                   SslProtocols.Tls13     => TransportSecurity.Tls13,
                   _                      => TransportSecurity.Unknown,
               };


        /// <summary>How to name it in a message a person has to act on.</summary>
        public static String Describe(TransportSecurity transport)

            => transport switch {
                   TransportSecurity.None         => "a plain TCP connection",
                   TransportSecurity.Tls12OrLower => "a TLS 1.2 (or lower) connection",
                   TransportSecurity.Tls13        => "a TLS 1.3 connection",
                   _                              => "a connection of unstated kind",
               };

    }

}
