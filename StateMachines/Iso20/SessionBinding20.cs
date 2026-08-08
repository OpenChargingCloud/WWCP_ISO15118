/*
 * Copyright (c) 2021-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of EVSimulatorApp
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
using System.Security.Cryptography;

namespace Vanaheimr.V2G.Simulation.StateMachines.Iso20
{

    /// <summary>
    /// Binds an ISO 15118-20 session to the peer that opened it, so a paused session can only be
    /// resumed by that same peer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this type exists.</b> `-20` obliges the SECC to check that a resumption request came from
    /// the same EVCC it set the session up with — a *shall*, with only the method left open. Omitting
    /// the check is not a hardening opportunity but a hole: a second EV that names another's SessionID
    /// inherits that EV's authorization, EIM or Plug &amp; Charge alike, and charges on someone else's
    /// contract. Ours omitted it until 2026-08-08, having been written by analogy to `-2`, which has no
    /// such requirement (<c>docs/normative-basis.md</c> in the conformance repository).
    /// </para>
    /// <para>
    /// <b>What it computes</b> is the standard's own worked example:
    /// <c>SHA-512(SessionID ‖ SHA-512(peer leaf certificate))</c>, the certificate taken from the
    /// verified TLS handshake. That example is *should*-level — the obligation is the check, not this
    /// particular way of performing it — so a deployment may substitute its own and this type is the
    /// seam where it would. The leaf only: never the chain.
    /// </para>
    /// <para>
    /// <b>Why a certificate is always there to hash.</b> `-20` permits nothing but full-handshake TLS,
    /// initial or resumed, and defines a full handshake as one where both ends authenticate by
    /// certificate. So in any conformant `-20` session both sides hold the other's leaf. Plain TCP is
    /// already outside the protocol; this harness speaks it only to reach stacks that offer nothing
    /// else, and there a resume simply cannot be verified — see the callers for what each side does
    /// about that.
    /// </para>
    /// </remarks>
    public static class SessionBinding20
    {

        /// <summary>
        /// The binding for <paramref name="sessionId"/> against <paramref name="peerLeafCertificate"/>
        /// (DER), or <c>null</c> when either is missing — an unverifiable session, not a matching one.
        /// </summary>
        public static byte[]? Compute(byte[]? sessionId, byte[]? peerLeafCertificate)
        {

            if (sessionId is not { Length: > 0 } || peerLeafCertificate is not { Length: > 0 })
                return null;

            // SHA-512 of the leaf first, then of the SessionID prepended to that digest -- the nesting is
            // the standard's, not an embellishment: the stored value is a hash *of a hash*.
            Span<byte> certificateHash = stackalloc byte[SHA512.HashSizeInBytes];
            SHA512.HashData(peerLeafCertificate, certificateHash);

            Span<byte> input = stackalloc byte[sessionId.Length + SHA512.HashSizeInBytes];
            sessionId.CopyTo(input);
            certificateHash.CopyTo(input[sessionId.Length..]);

            return SHA512.HashData(input);

        }

        /// <summary>
        /// Whether a resume presenting <paramref name="presented"/> may join a session stored with
        /// <paramref name="stored"/>. A missing value on either side never matches.
        /// </summary>
        /// <remarks>
        /// Constant-time, because this comparison decides who gets to charge on whose authorization, and
        /// an attacker who can time it can otherwise search the space a byte at a time.
        /// </remarks>
        public static bool Matches(byte[]? stored, byte[]? presented)

            => stored is { Length: > 0 } && presented is { Length: > 0 } &&
               CryptographicOperations.FixedTimeEquals(stored, presented);

        /// <summary>
        /// The other end's leaf certificate (DER) when <paramref name="stream"/> is an authenticated TLS
        /// stream, else <c>null</c>. Symmetric: the SECC gets the vehicle's, the EVCC gets the station's.
        /// </summary>
        public static byte[]? PeerLeafOf(Stream? stream)

            => stream is SslStream { IsAuthenticated: true, RemoteCertificate: { } peer }
                   ? peer.GetRawCertData()
                   : null;

    }

}
