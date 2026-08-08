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

using System.Text.Json;

using NUnit.Framework;

using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

using cloud.charging.open.protocols.ISO15118_20.CommonMessages;
using cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{
    /// <summary>
    /// The published Ed448 test vectors from RFC 8032 §7.4, against the signer ISO 15118-20's
    /// second signature suite runs on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These fill a hole rather than adding depth. Until now the Ed448 path was covered only by
    /// sign-then-verify, tampered-signature and wrong-key tests — all three self-referential. An
    /// implementation that signed the wrong octets, or signed them with a non-empty context, passes
    /// every one of them and is rejected by every conforming peer. Nothing else in the repository
    /// would have caught it either: unlike ECDSA-P521 there is no live-interop evidence, because
    /// the -20 secp521r1/Ed448 profile is one Josev cannot exercise
    /// (<c>ISO15118ConformanceTests/docs/interop-runs/2026-07-21-iso20-dc-tls-forward/notes.md</c>).
    /// </para>
    /// <para>
    /// These vectors are a stronger oracle than the rest of the corpus. The cbV2G vectors are one
    /// reference implementation's output; these are the standard's own numbers, so agreeing with
    /// them is agreeing with the specification rather than with a peer.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class Ed448RfcVectorTests
    {
        public sealed record Vector(string Label, string SecretKey, string PublicKey,
                                    string Message, string Context, string Signature)
        {
            public override string ToString() => Label;
        }

        private static byte[] Hex(string s) => Convert.FromHexString(s);

        public static IEnumerable<Vector> Vectors()
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory,
                                    "Vectors", "Ed448.rfc8032.vectors.json");
            using var doc = JsonDocument.Parse(File.ReadAllText(path));

            foreach (var v in doc.RootElement.GetProperty("vectors").EnumerateArray())
                yield return new Vector(
                    v.GetProperty("label").GetString()!,
                    v.GetProperty("secretKey").GetString()!,
                    v.GetProperty("publicKey").GetString()!,
                    v.GetProperty("message").GetString()!,
                    v.GetProperty("context").GetString()!,
                    v.GetProperty("signature").GetString()!);
        }

        /// <summary>The corpus is complete and was not silently truncated to the easy cases.</summary>
        [Test]
        public void TheCorpusIsTheWholeOfSection74()
        {
            var all = Vectors().ToList();

            Assert.Multiple(() =>
            {
                Assert.That(all, Has.Count.EqualTo(9), "RFC 8032 §7.4 has nine Ed448 vectors");
                Assert.That(all.Count(v => v.Context.Length > 0), Is.EqualTo(1),
                            "exactly one §7.4 vector carries a context");
                Assert.That(all.Max(v => v.Message.Length / 2), Is.EqualTo(1023),
                            "the 1023-octet vector is missing — the one that crosses SHAKE256 block "
                          + "boundaries and would catch a broken streaming update");
            });
        }

        /// <summary>A private key really does determine the public key the RFC pairs it with.</summary>
        [TestCaseSource(nameof(Vectors))]
        public void ThePublicKeyDerivesFromTheSecretKey(Vector v)
        {
            var derived = new Ed448PrivateKeyParameters(Hex(v.SecretKey)).GeneratePublicKey();

            Assert.That(Convert.ToHexString(derived.GetEncoded()).ToLowerInvariant(),
                        Is.EqualTo(v.PublicKey));
        }

        /// <summary>
        /// Signing reproduces the RFC's own signature, byte for byte.
        /// </summary>
        /// <remarks>
        /// Ed448 is deterministic (RFC 8032 has no per-signature randomness), so unlike ECDSA this
        /// can be an equality check rather than a verify-what-we-produced check — which is what
        /// makes it a real oracle instead of another round trip.
        /// </remarks>
        [TestCaseSource(nameof(Vectors))]
        public void SigningReproducesTheRfcSignature(Vector v)
        {
            var signer = new Ed448Signer(Hex(v.Context));
            signer.Init(forSigning: true, new Ed448PrivateKeyParameters(Hex(v.SecretKey)));
            var message = Hex(v.Message);
            signer.BlockUpdate(message, 0, message.Length);

            Assert.That(Convert.ToHexString(signer.GenerateSignature()).ToLowerInvariant(),
                        Is.EqualTo(v.Signature));
        }

        [TestCaseSource(nameof(Vectors))]
        public void VerifyingAcceptsTheRfcSignature(Vector v)
        {
            var verifier = new Ed448Signer(Hex(v.Context));
            verifier.Init(forSigning: false, new Ed448PublicKeyParameters(Hex(v.PublicKey)));
            var message = Hex(v.Message);
            verifier.BlockUpdate(message, 0, message.Length);

            Assert.That(verifier.VerifySignature(Hex(v.Signature)), Is.True);
        }

        /// <summary>
        /// The context is load-bearing, and the RFC proves it for us.
        /// </summary>
        /// <remarks>
        /// §7.4's "1 octet" and "1 octet (with context)" vectors share a key and a message and
        /// differ only in the context — <c>""</c> against <c>"foo"</c> — and their signatures have
        /// nothing in common. So "empty context" is a *choice* our implementation makes, not a
        /// property of Ed448, and an API that hides the parameter is making that choice silently.
        /// That matters for the Swift back end: the libraries under consideration expose no context
        /// argument at all (EVSimulatorApp/docs/CONCEPT.md §8 #10).
        /// </remarks>
        [Test]
        public void TheContextChangesTheSignatureEntirely()
        {
            var all      = Vectors().ToList();
            var withOut  = all.Single(v => v.Label == "1 octet");
            var withFoo  = all.Single(v => v.Label == "1 octet (with context)");

            Assert.Multiple(() =>
            {
                Assert.That(withFoo.SecretKey, Is.EqualTo(withOut.SecretKey), "same key");
                Assert.That(withFoo.Message,   Is.EqualTo(withOut.Message),   "same message");
                Assert.That(withOut.Context,   Is.Empty);
                Assert.That(Hex(withFoo.Context), Is.EqualTo("foo"u8.ToArray()));
                Assert.That(withFoo.Signature, Is.Not.EqualTo(withOut.Signature));
            });

            // And an empty-context signer must not accept the "foo" signature, or the parameter
            // would be decorative.
            var verifier = new Ed448Signer([]);
            verifier.Init(forSigning: false, new Ed448PublicKeyParameters(Hex(withFoo.PublicKey)));
            var message = Hex(withFoo.Message);
            verifier.BlockUpdate(message, 0, message.Length);

            Assert.That(verifier.VerifySignature(Hex(withFoo.Signature)), Is.False,
                        "a signature made under a context verified without one");
        }

        // ── Connecting the validated primitive to our own signing path ──────────────────────────

        private static SignedInfoType SignedInfo()
        {
            var content = new MeteringConfirmationReqType(
                new MessageHeaderType(SessionID: new byte[8], TimeStamp: 1_700_000_000UL, Signature: null),
                new SignedMeteringDataType(
                    Id: "ID1", SessionID: new byte[8],
                    MeterInfo: new MeterInfoType(
                        MeterID: "M1", ChargedEnergyReadingWh: 5000,
                        BPT_DischargedEnergyReadingWh: null, CapacitiveEnergyReadingVARh: null,
                        BPT_InductiveEnergyReadingVARh: null, MeterSignature: null,
                        MeterStatus: null, MeterTimestamp: null),
                    Receipt: null,
                    Dynamic_SMDTControlMode: null,
                    Scheduled_SMDTControlMode: new Scheduled_SMDTControlModeType(SelectedScheduleTupleID: 1)));

            var buf = new byte[512];
            Assert.That(CommonMessagesCodec.EncodeFragment_MeteringConfirmationReq(content, buf, out int n), Is.True);

            return V2GSignature.BuildSignedInfo("ID1", V2GSignature.Digest(buf.AsSpan(0, n)),
                                                V2GSignature.EddsaEd448);
        }

        /// <summary>
        /// <see cref="V2GSignature.SignEd448"/> is the vector-checked primitive and nothing else.
        /// </summary>
        /// <remarks>
        /// The vectors above validate BouncyCastle; this validates the two lines of ours that stand
        /// between it and the wire. It reproduces what our wrapper does using the raw signer — pure
        /// Ed448, empty context, over the SignedInfo fragment octets with no external pre-hash — and
        /// requires the bytes to be identical. If the wrapper ever grows a pre-hash, a context, or a
        /// different notion of which octets are signed, this fails while every self-referential test
        /// in <see cref="Iso15118_20SignatureTests"/> keeps passing.
        /// </remarks>
        [Test]
        public void OurSigningPathIsPureEd448WithAnEmptyContextOverTheFragment()
        {
            var key        = new Ed448PrivateKeyParameters(Hex(Vectors().First().SecretKey));
            var signedInfo = SignedInfo();

            var ours = V2GSignature.SignEd448(signedInfo, key);

            var expected = new Ed448Signer([]);
            expected.Init(forSigning: true, key);
            var fragment = V2GSignature.SignedInfoFragment(signedInfo);
            expected.BlockUpdate(fragment, 0, fragment.Length);

            Assert.That(ours, Is.EqualTo(expected.GenerateSignature()));
            Assert.That(ours, Has.Length.EqualTo(114));
        }

        /// <summary>
        /// The SignedInfo must declare the algorithm it was actually signed with.
        /// </summary>
        /// <remarks>
        /// The URI travels inside the message, so a peer states its suite rather than leaving us to
        /// infer it. RFC 9231 §2.3.12 lists <c>#eddsa-ed448ph</c> as a separate identifier from
        /// <c>#eddsa-ed448</c>, and we implement only the latter — signing the prehashed variant's
        /// octets under the pure variant's URI, or the reverse, is the quiet failure this pins.
        /// </remarks>
        [Test]
        public void TheEd448SignatureMethodUriIsThePureVariant()
        {
            Assert.Multiple(() =>
            {
                Assert.That(V2GSignature.EddsaEd448,
                            Is.EqualTo("http://www.w3.org/2021/04/xmldsig-more#eddsa-ed448"));
                Assert.That(V2GSignature.EddsaEd448, Does.Not.Contain("ed448ph"),
                            "the prehashed variant is a different algorithm and is not implemented");
                Assert.That(SignedInfo().SignatureMethod.Algorithm, Is.EqualTo(V2GSignature.EddsaEd448));
            });
        }
    }
}
