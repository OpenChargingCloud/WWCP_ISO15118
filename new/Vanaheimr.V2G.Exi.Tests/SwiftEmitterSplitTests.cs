/*
 * Copyright (c) 2021-2025 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP ISO/IEC 15118 <https://github.com/OpenChargingCloud/WWCP_ISO15118>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using NUnit.Framework;

using cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Emit;
using cloud.charging.open.protocols.ISO15118.EXI.Tests.Infrastructure;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{
    /// <summary>
    /// Gate 3 for the Swift back end: the things a byte-level diff cannot see. Bit-exactness says
    /// nothing about whether the emitted Swift is well-formed — that only shows up in
    /// <c>swift test</c>, which does not run in this suite, so the shape is checked here instead.
    /// </summary>
    [TestFixture]
    public class SwiftEmitterSplitTests
    {
        private static IReadOnlyList<GeneratedFile> AppProtocol() =>
            EmitterHarness.EmitSwift("app", "SupportedAppProtocolCodec",
                EmitterHarness.RealSchemaSet("Vanaheimr.V2G.Exi.Prototype")
                              .Where(f => f.Name.Contains("AppProtocol"))
                              .Select(f => (f.Name, f.Xsd))
                              .ToArray());

        [Test]
        public void EmitsOneFilePerTypePlusTheCodec()
        {
            var files = AppProtocol();

            Assert.That(files.Select(f => f.FileName), Is.EquivalentTo(new[]
            {
                "ResponseCode.swift",
                "AppProtocolType.swift",
                "SupportedAppProtocolReq.swift",
                "SupportedAppProtocolRes.swift",
                "SupportedAppProtocolCodec.swift",

                // The JSON-LD pass, split the same way: one part per type, one for the dispatchers.
                // An enum has no JSON part of its own — it is a string wherever it appears.
                "AppProtocolType.Json.swift",
                "SupportedAppProtocolReq.Json.swift",
                "SupportedAppProtocolRes.Json.swift",
                "SupportedAppProtocolCodecJson.Json.swift",
            }));
        }

        /// <summary>
        /// A global element's body also appears in <c>SchemaPlan.ComplexTypes</c>, so emitting both
        /// without a guard produces every struct, encoder and decoder twice. Swift rejects the
        /// redeclaration, but only after the emitter has silently written it — this caught exactly
        /// that during the port.
        /// </summary>
        [Test]
        public void DeclaresNothingTwice()
        {
            var declarations = AppProtocol()
                .SelectMany(f => EmitterHarness.Lines(f)
                    .Where(l => l.StartsWith("public struct ") || l.StartsWith("public enum ") ||
                                l.StartsWith("internal func "))
                    .Select(l => l.Trim()))
                .ToList();

            Assert.That(declarations, Is.Unique);
            Assert.That(declarations, Is.Not.Empty);
        }

        /// <summary>Every codec call must resolve to a function some file in the set declares.</summary>
        [Test]
        public void EveryCodecCallResolves()
        {
            var files = AppProtocol();

            var declared = files
                .SelectMany(f => EmitterHarness.Lines(f))
                .Select(l => System.Text.RegularExpressions.Regex.Match(
                            l, @"^internal func (?<name>(?:encode|decode)[A-Za-z0-9_]*)\("))
                .Where(m => m.Success)
                .Select(m => m.Groups["name"].Value)
                .ToHashSet(StringComparer.Ordinal);

            var called = files
                .SelectMany(f => EmitterHarness.Lines(f))
                .SelectMany(l => EmitterHarness.CodecCall.Matches(l).Select(m => m.Groups["name"].Value))
                // The facade's two entry points are `public static func` members of the codec enum
                // rather than top-level `internal func`s, so they are not in `declared` by
                // construction. encodeAny joined decodeAny when the JSON-LD pass needed a way to
                // re-encode a message it had not constructed.
                .Where(n => n is not ("decodeAny" or "encodeAny"))
                .ToHashSet(StringComparer.Ordinal);

            Assert.That(called, Is.Not.Empty);
            Assert.That(called.Except(declared), Is.Empty,
                        "calls with no declaration: " + string.Join(", ", called.Except(declared)));
        }

        /// <summary>
        /// The runtime import appears exactly where the runtime is used. Swift does not warn about
        /// an unused import, so a blanket one would go unnoticed — and its absence where it *is*
        /// needed is a compile error the .NET suite would never see.
        /// </summary>
        [Test]
        public void ImportsTheRuntimeExactlyWhereItIsUsed()
        {
            foreach (var file in AppProtocol())
            {
                var usesRuntime = file.Source.Contains("BitReader") || file.Source.Contains("BitWriter") ||
                                  file.Source.Contains("ExiPrimitives") || file.Source.Contains("ExiError") ||
                                  file.Source.Contains("exiEnum") ||
                                  // The JSON-LD parts use the runtime too: the ordered JSON tree and
                                  // JsonPrimitives live beside ExiPrimitives, for the same reason.
                                  file.Source.Contains("JsonObject") || file.Source.Contains("JsonValue") ||
                                  file.Source.Contains("JsonPrimitives") || file.Source.Contains("JsonLdError");
                var imports = file.Source.Contains("import ExiRuntime");

                Assert.That(imports, Is.EqualTo(usesRuntime),
                            $"{file.FileName}: import/use mismatch (uses={usesRuntime}, imports={imports})");
            }
        }

        /// <summary>
        /// Constructs the back end does not model must fail loudly. The -2 and -20 sets are full of
        /// them, and a back end that quietly emitted something plausible for a substitution group
        /// would produce a codec that compiles, runs, and is wrong on the wire.
        /// </summary>
        [Test]
        public void RefusesConstructsItDoesNotModel()
        {
            // -2, and -20's CommonMessages, DC, AC and ACDP all pass Reject() now. WPT is the one
            // that does not, and unlike the others its refusal is not a gap waiting to be closed:
            // WPT_LF_TransmitterDataType is the self-loop list shape for which cbexigen's own
            // generated encoder cannot represent even the schema's required minimum, so there is no
            // reference to check an implementation against. Guessing here would produce bytes no
            // oracle has ever seen — see CodecEmitter's note and kotlin/README's "Unvalidated
            // construct".
            var set = EmitterHarness.RealSchemaSet("Vanaheimr.V2G.Exi.Iso15118_20.WPT")
                                    .Select(f => (f.Name, f.Xsd))
                                    .ToArray();

            var ex = Assert.Throws<NotSupportedException>(
                         () => EmitterHarness.EmitSwift("wpt", "WPTCodec", set));

            // Which construct stops it moves as the back end grows, and so does the wording — the
            // refusals are not one formula any more, because their reasons differ. What has to hold
            // is that a refusal is attributable and names what it refused, so it can be acted on.
            Assert.That(ex!.Message, Does.Contain("Swift back end"));
            Assert.That(ex.Message, Does.Match(@"'[^']+'"), "the refusal must name what it refused");
        }
    }
}
