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

namespace cloud.charging.open.protocols.ISO15118.Transport.BouncyCastle
{

    /// <summary>
    /// BouncyCastle's authenticated application-data stream, carrying the peer's leaf certificate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The certificate is what this type exists for; everything else is delegation. BouncyCastle hands
    /// the peer's chain to <c>NotifyServerCertificate</c> / <c>NotifyClientCertificate</c> during the
    /// handshake and keeps nothing afterwards, and the stream it returns is an internal type with no
    /// accessor for it. So the one moment the certificate is in reach is inside the callback, and this
    /// is where it is put so that it outlives the handshake.
    /// </para>
    /// <para>
    /// On the stream specifically, and named like <see cref="System.Net.Security.SslStream"/>'s
    /// <c>RemoteCertificate</c>, because that is where the `-20` session binding looks — see
    /// <c>StateMachines.Iso20.SessionBinding20.PeerLeafOf</c>. A `-20` session over this backend is a
    /// full mutual-TLS handshake and has every right to be bound; until this existed it was treated as
    /// though it were plain TCP, so a session paused over BouncyCastle was unbound and every resume of
    /// it was refused. That made the conformant `-20` profile and a working pause/resume mutually
    /// exclusive on any platform where Schannel cannot do the profile.
    /// </para>
    /// <para>
    /// Null when the peer sent no certificate. On the server side that is the ordinary unilateral-TLS
    /// case - no <c>CertificateRequest</c> was sent, so nothing arrived - and it stays null rather than
    /// becoming empty, because "not authenticated" and "authenticated as nobody" must not compare equal
    /// further down.
    /// </para>
    /// </remarks>
    public sealed class BcTlsStream : Stream
    {

        #region Data

        private readonly Stream inner;

        #endregion

        #region Properties

        /// <summary>
        /// The other end's leaf certificate (DER) from the handshake, or null when it sent none.
        /// </summary>
        public Byte[]?  PeerLeafCertificate    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Wrap a BouncyCastle application-data stream together with the peer's leaf certificate.
        /// </summary>
        /// <param name="Inner">The stream BouncyCastle returned once the handshake completed.</param>
        /// <param name="PeerLeafCertificate">The peer's leaf certificate (DER), or null when it sent none.</param>
        public BcTlsStream(Stream   Inner,
                           Byte[]?  PeerLeafCertificate)
        {

            this.inner                = Inner;
            this.PeerLeafCertificate  = PeerLeafCertificate;

        }

        #endregion


        #region Stream

        public override Boolean  CanRead     => inner.CanRead;
        public override Boolean  CanSeek     => inner.CanSeek;
        public override Boolean  CanWrite    => inner.CanWrite;
        public override Boolean  CanTimeout  => inner.CanTimeout;

        public override Int64    Length      => inner.Length;

        public override Int64    Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override Int32    ReadTimeout
        {
            get => inner.ReadTimeout;
            set => inner.ReadTimeout = value;
        }

        public override Int32    WriteTimeout
        {
            get => inner.WriteTimeout;
            set => inner.WriteTimeout = value;
        }

        public override void  Flush()
            => inner.Flush();

        public override Task  FlushAsync(CancellationToken cancellationToken)
            => inner.FlushAsync(cancellationToken);

        public override Int32  Read(Byte[] buffer, Int32 offset, Int32 count)
            => inner.Read(buffer, offset, count);

        public override Int32  Read(Span<Byte> buffer)
            => inner.Read(buffer);

        public override Task<Int32>  ReadAsync(Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken)
            => inner.ReadAsync(buffer, offset, count, cancellationToken);

        public override ValueTask<Int32>  ReadAsync(Memory<Byte> buffer, CancellationToken cancellationToken = default)
            => inner.ReadAsync(buffer, cancellationToken);

        public override Int32  ReadByte()
            => inner.ReadByte();

        public override void  Write(Byte[] buffer, Int32 offset, Int32 count)
            => inner.Write(buffer, offset, count);

        public override void  Write(ReadOnlySpan<Byte> buffer)
            => inner.Write(buffer);

        public override Task  WriteAsync(Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken)
            => inner.WriteAsync(buffer, offset, count, cancellationToken);

        public override ValueTask  WriteAsync(ReadOnlyMemory<Byte> buffer, CancellationToken cancellationToken = default)
            => inner.WriteAsync(buffer, cancellationToken);

        public override void  WriteByte(Byte value)
            => inner.WriteByte(value);

        public override Int64  Seek(Int64 offset, SeekOrigin origin)
            => inner.Seek(offset, origin);

        public override void  SetLength(Int64 value)
            => inner.SetLength(value);

        // Closing the TLS stream is what sends close_notify, so it has to reach the inner stream
        // rather than being swallowed by a wrapper that only meant to carry a certificate.
        protected override void  Dispose(Boolean disposing)
        {

            if (disposing)
                inner.Dispose();

            base.Dispose(disposing);

        }

        public override ValueTask  DisposeAsync()
            => inner.DisposeAsync();

        #endregion

    }

}
