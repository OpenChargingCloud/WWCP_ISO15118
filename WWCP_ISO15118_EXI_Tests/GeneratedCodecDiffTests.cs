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

using NUnit.Framework;
using cloud.charging.open.protocols.ISO15118.EXI.Tests.Infrastructure;

// Bring the generated namespace in unqualified for the extension method TryEncode().
// The hand-written namespace is brought in via an alias to avoid type collisions.
using cloud.charging.open.protocols.ISO15118.AppProtocol.Generated;
using HW = cloud.charging.open.protocols.ISO15118.AppProtocol;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{
    /// <summary>
    /// Differential conformance: for every vector, the generated codec must produce
    /// the same bytes as the hand-written codec. This is the reference relationship
    /// between the two; once we trust this and ingest cbV2G output for the seed
    /// vectors, the hand-written codec can be deleted.
    /// </summary>
    [TestFixture]
    public class GeneratedCodecDiffTests
    {
        [TestCaseSource(typeof(AppProtocolVectorSource), nameof(AppProtocolVectorSource.All))]
        public void Generated_Bytes_Match_HandWritten(string file, Vector v)
        {
            byte[] bufHW = new byte[1024], bufGen = new byte[1024];
            int nHW, nGen;
            bool okHW, okGen;

            switch (v.MessageType)
            {
                case "SupportedAppProtocolReq":
                {
                    var hw  = VectorInputBinder.BindRequest(v.Input);
                    var gen = ToGenerated(hw);
                    okHW  = HW.SupportedAppProtocolCodec.TryEncodeRequest(hw, bufHW, out nHW);
                    okGen = gen.TryEncode(bufGen, out nGen);
                    break;
                }
                case "SupportedAppProtocolRes":
                {
                    var hw  = VectorInputBinder.BindResponse(v.Input);
                    var gen = ToGenerated(hw);
                    okHW  = HW.SupportedAppProtocolCodec.TryEncodeResponse(hw, bufHW, out nHW);
                    okGen = gen.TryEncode(bufGen, out nGen);
                    break;
                }
                default: throw new NotSupportedException(v.MessageType);
            }

            Assert.That(okHW,  Is.True, "hand-written encoder failed");
            Assert.That(okGen, Is.True, "generated encoder failed");

            var hw_bytes  = bufHW.AsSpan(0, nHW).ToArray();
            var gen_bytes = bufGen.AsSpan(0, nGen).ToArray();

            if (!hw_bytes.AsSpan().SequenceEqual(gen_bytes))
                Assert.Fail($"{file}/{v.Name}: generated codec diverges from hand-written.\n" +
                            HexUtil.Diff(hw_bytes, gen_bytes));
        }

        [TestCaseSource(typeof(AppProtocolVectorSource), nameof(AppProtocolVectorSource.All))]
        public void Generated_Roundtrip(string file, Vector v)
        {
            byte[] buf = new byte[1024];
            int written;

            switch (v.MessageType)
            {
                case "SupportedAppProtocolReq":
                {
                    var input = ToGenerated(VectorInputBinder.BindRequest(v.Input));
                    Assert.That(input.TryEncode(buf, out written), Is.True);
                    var got = (SupportedAppProtocolReq)
                        SupportedAppProtocolCodec.DecodeAny(buf.AsSpan(0, written), out _);
                    Assert.That(got.AppProtocol, Is.EqualTo(input.AppProtocol));
                    break;
                }
                case "SupportedAppProtocolRes":
                {
                    var input = ToGenerated(VectorInputBinder.BindResponse(v.Input));
                    Assert.That(input.TryEncode(buf, out written), Is.True);
                    Assert.That(
                        SupportedAppProtocolCodec.DecodeAny(buf.AsSpan(0, written), out _),
                        Is.EqualTo(input));
                    break;
                }
                default: throw new NotSupportedException(v.MessageType);
            }
        }

        // -----------------------------------------------------------------------
        //  Hand-written → generated mapping. Both type sets are structurally
        //  identical; the conversion exists only because they live in different
        //  namespaces (and the generator follows XSD names, not the hand-written
        //  stylistic choices like "AppProtocols" → "AppProtocol").
        // -----------------------------------------------------------------------

        private static SupportedAppProtocolReq ToGenerated(HW.SupportedAppProtocolReq h)
        {
            var entries = new List<AppProtocolType>(h.AppProtocols.Count);
            foreach (var e in h.AppProtocols)
            {
                entries.Add(new AppProtocolType(
                    ProtocolNamespace : e.ProtocolNamespace,
                    VersionNumberMajor: e.VersionNumberMajor,
                    VersionNumberMinor: e.VersionNumberMinor,
                    SchemaID          : e.SchemaID,
                    Priority          : e.Priority));
            }
            return new SupportedAppProtocolReq(entries);
        }

        private static SupportedAppProtocolRes ToGenerated(HW.SupportedAppProtocolRes h) =>
            new(
                ResponseCode: h.Code switch
                {
                    HW.ResponseCode.OK_SuccessfulNegotiation                       => ResponseCode.OK_SuccessfulNegotiation,
                    HW.ResponseCode.OK_SuccessfulNegotiationWithMinorDeviation     => ResponseCode.OK_SuccessfulNegotiationWithMinorDeviation,
                    HW.ResponseCode.Failed_NoNegotiation                           => ResponseCode.Failed_NoNegotiation,
                    _ => throw new ArgumentOutOfRangeException(),
                },
                SchemaID: h.SchemaID);
    }
}
