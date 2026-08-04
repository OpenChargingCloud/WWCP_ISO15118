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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Grammar;

namespace cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Emit
{
    /// <summary>
    /// Kotlin back end — the second target through <see cref="ICodecEmitter"/>, and the proof
    /// that the front end is genuinely language-neutral.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Scope: deliberately partial.</b> This covers the constructs the AppProtocol schema
    /// exercises — enums, records, required/optional/repeating children, and the
    /// string / unsigned-integer / n-bit / enum-index value encodings. Everything else
    /// (substitution groups, inline choices, attributes, simple content, opaque elements,
    /// fragments) throws <see cref="NotSupportedException"/> rather than emitting something
    /// plausible, following the generator's fail-loud rule: an unhandled construct must break
    /// the build, never produce silently wrong bytes.
    /// </para>
    /// <para>
    /// Bit-level behaviour mirrors <see cref="CodecEmitter"/> exactly — same event codes, same
    /// widths, same order — because both are driven by the same <see cref="SchemaPlan"/>. The
    /// vectors under <c>cloud.charging.open.protocols.ISO15118.EXI.Tests/Vectors/</c> are the shared oracle.
    /// </para>
    /// </remarks>
    internal sealed class KotlinCodecEmitter : ICodecEmitter
    {
        public static readonly KotlinCodecEmitter Instance = new();

        public string Language      => "kotlin";
        public string FileExtension => ".kt";

        /// <remarks>
        /// The JSON-LD pass runs here rather than as an emitter of its own, for the reason
        /// docs/CONCEPT.md §4.4 gives: wire codec and JSON-LD codec come from the same type graph in
        /// the same pass, so there is no seam at which one could be regenerated and the other not.
        /// </remarks>
        public IReadOnlyList<GeneratedFile> Emit(SchemaPlan plan, string targetNamespace, string codecClassName) =>
        [
            .. new Writer(plan, targetNamespace, codecClassName).Run(),
            .. KotlinJsonEmitter.Emit(plan, targetNamespace, codecClassName),
        ];

        private sealed class Writer(SchemaPlan plan, string package, string codecObject)
        {
            /// <summary>
            /// The buffer every <c>Emit*</c> method appends to. Unlike the C# back end's, this one
            /// is swapped as emission moves from type to type — see <see cref="Run"/>.
            /// </summary>
            private StringBuilder _sb = new();

            private readonly HashSet<string> _emitted = new(StringComparer.Ordinal);
            private readonly HashSet<string> _codecs  = new(StringComparer.Ordinal);
            private HashSet<string> _baseNames = new(StringComparer.Ordinal);
            private int _run;

            /// <summary>Per-type buffers: the declaration, and its encoder/decoder pair.</summary>
            private readonly Dictionary<string, StringBuilder> _decl = new(StringComparer.Ordinal);
            private readonly Dictionary<string, StringBuilder> _code = new(StringComparer.Ordinal);

            /// <summary>Type names in order of first emission — the order files come out in.</summary>
            private readonly List<string> _order = new();

            /// <summary>Guards against two declarations claiming the same file.</summary>
            private readonly HashSet<string> _fileNames = new(StringComparer.Ordinal);

            /// <summary>
            /// One file per type, plus one for the codec object.
            /// </summary>
            /// <remarks>
            /// <para>
            /// The alternative — everything in one file, as the C# back end does — does not survive
            /// contact with the Kotlin compiler: the AC_DER sets are around a megabyte, and the
            /// compiler exhausts a default heap on them. It also put every method of a message set
            /// into a single class file, which on Android counts against the 64k method limit of a
            /// DEX file all at once, and made the smallest schema change recompile everything.
            /// </para>
            /// <para>
            /// A type's encoder and decoder move out of the codec object and become top-level
            /// <c>internal</c> functions next to the type they encode. The object stays as the
            /// public face — <c>encode</c>, <c>decodeAny</c> and the fragment codecs — because
            /// that is what callers use and Kotlin cannot spread an <c>object</c> across files.
            /// </para>
            /// </remarks>
            public IReadOnlyList<GeneratedFile> Run()
            {
                Reject(plan);

                var files = new List<GeneratedFile>();

                foreach (var e in plan.Enums)
                    files.Add(Standalone(e.Name, () => EmitEnum(e)));

                foreach (var t in plan.OpaqueTypes)
                    files.Add(Standalone(t, () => EmitOpaque(t)));

                EmitRecords();
                EmitTypeCodecs();

                foreach (var name in _order)
                {
                    var body = new StringBuilder(_decl[name].ToString());
                    if (_code.TryGetValue(name, out var codec))
                        body.Append(Dedent(codec.ToString()));
                    files.Add(File(name, body.ToString()));
                }

                files.Add(Standalone(codecObject, EmitFacade));

                return files;
            }

            /// <summary>A file holding one thing, emitted by <paramref name="emit"/> into a fresh buffer.</summary>
            private GeneratedFile Standalone(string name, Action emit)
            {
                _sb = new StringBuilder();
                emit();
                return File(name, _sb.ToString());
            }

            /// <summary>
            /// Wraps a body in the file header. Imports are filtered to what the body actually
            /// mentions — most type files never touch a <c>BitReader</c>, and Kotlin reports an
            /// unused import as a warning.
            /// </summary>
            private GeneratedFile File(string name, string body)
            {
                var sb = new StringBuilder();
                sb.AppendLine("// <auto-generated/>");
                sb.AppendLine("// Generated by Vanaheimr.V2G.Exi.SourceGenerator (Kotlin back end). Do not edit by hand.");
                sb.AppendLine();
                sb.Append("package ").AppendLine(package);
                sb.AppendLine();

                var imported = false;
                foreach (var t in new[] { "BitReader", "BitWriter", "ExiPrimitives" })
                    if (body.Contains(t))
                    {
                        sb.Append("import cloud.charging.v2g.exi.").AppendLine(t);
                        imported = true;
                    }
                if (imported)
                    sb.AppendLine();

                // A declaration ends with a blank separator line that only made sense when the
                // next declaration followed it.
                sb.AppendLine(body.TrimEnd('\r', '\n'));

                if (!_fileNames.Add(name))
                    throw new NotSupportedException(
                        $"Kotlin back end: two declarations are both called '{name}', so they would " +
                        "share a file. One file per type only works while type names are unique.");

                return new GeneratedFile(name + ".kt", sb.ToString());
            }

            /// <summary>
            /// Removes one level of indentation from text written for the inside of the codec
            /// object. Safe as a plain text transformation because nothing this back end emits
            /// spans lines — there are no raw strings, and every string literal is written by
            /// <see cref="KStr"/> on a single line.
            /// </summary>
            private static string Dedent(string body)
            {
                var sb = new StringBuilder(body.Length);
                foreach (var line in body.Split('\n'))
                    sb.Append(line.StartsWith("    ", StringComparison.Ordinal) ? line.Substring(4) : line)
                      .Append('\n');
                // Split on the trailing newline produced one empty last element; drop its newline.
                if (sb.Length > 0) sb.Length--;
                return sb.ToString();
            }

            /// <summary>Directs subsequent emission at one type's declaration or codec buffer.</summary>
            private void Target(Dictionary<string, StringBuilder> which, string name)
            {
                if (!_decl.ContainsKey(name))
                {
                    _order.Add(name);
                    _decl[name] = new StringBuilder();
                }
                if (!which.TryGetValue(name, out var sb))
                    which[name] = sb = new StringBuilder();
                _sb = sb;
            }

            /// <summary>Fail loud on anything this back end does not model yet.</summary>
            private static void Reject(SchemaPlan p)
            {
                foreach (var sp in p.ComplexTypes.Values)
                {
                    ValidateAttributes(p, sp);

                    foreach (var c in sp.Children)
                    {
                        // Only an optional wildcard is modelled — it then rides in an optional run,
                        // where TrailingAny() checks the position cbexigen's code layout requires.
                        if (c.IsWildcardAny && c.Shape != ChildShape.OptionalSingle)
                            throw new NotSupportedException(
                                $"Kotlin back end: the xs:any wildcard '{sp.RecordName}.{c.FieldName}' is " +
                                $"{c.Shape}; only an optional wildcard is implemented.");
                        _ = c.Value switch
                        {
                            ValueEncoding.StringValue or ValueEncoding.UnsignedInt or ValueEncoding.SignedInt
                                or ValueEncoding.Binary or ValueEncoding.NBitUnsigned or ValueEncoding.EnumIndex
                                or ValueEncoding.ComplexRef or ValueEncoding.OpaqueElement
                                or ValueEncoding.SubstitutionChoice or ValueEncoding.InlineChoice => true,
                            _ => throw new NotSupportedException(
                                     $"Kotlin back end: value encoding {c.Value.GetType().Name} " +
                                     $"('{sp.RecordName}.{c.FieldName}') is not implemented yet."),
                        };
                    }
                }
            }

            /// <summary>
            /// Attribute shapes this back end models: either a single required attribute, or any
            /// number of optional ones, all string-typed. A base type's own attributes are the one
            /// real obstacle: only its *children* are flattened into the derived type, so there
            /// would be nothing to pass for them at the base constructor call.
            /// </summary>
            private static void ValidateAttributes(SchemaPlan plan, SequencePlan sp)
            {
                if (sp.Attributes is null or { Count: 0 })
                    return;

                if (sp.BaseRecordName is not null
                    && plan.ComplexTypes.TryGetValue(sp.BaseRecordName, out var basePlan)
                    && basePlan.Attributes is { Count: > 0 })
                    throw new NotSupportedException(
                        $"Kotlin back end: '{sp.RecordName}' and its base type '{sp.BaseRecordName}' " +
                        "both carry attributes; the base's are not flattened, so they cannot be passed on.");

                // Optional attributes ride along as leading optionals of the *content run*, which an
                // xs:choice does not have — they would be dropped silently. (A required attribute is
                // written before the content and is unaffected.)
                if (sp.IsChoice && RequiredAttr(sp) is null)
                    throw new NotSupportedException(
                        $"Kotlin back end: optional attributes on the xs:choice type '{sp.RecordName}' " +
                        "are not implemented yet.");

                foreach (var a in sp.Attributes)
                {
                    if (a.Value is not ValueEncoding.StringValue)
                        throw new NotSupportedException(
                            $"Kotlin back end: only string-typed attributes are supported " +
                            $"('{sp.RecordName}.{a.FieldName}' is {a.Value.GetType().Name}).");
                    if (a.Required && sp.Attributes.Count != 1)
                        throw new NotSupportedException(
                            $"Kotlin back end: '{sp.RecordName}' combines a required attribute with others; " +
                            "only a lone required attribute, or an all-optional set, is modelled.");
                }
            }

            /// <summary>
            /// The record field carrying an xs:simpleContent value. Matches the C# emitter's name, so
            /// both back ends expose the same shape.
            /// </summary>
            private const string SimpleContentField = "Value";

            /// <summary>A synthetic child standing in for the simpleContent value.</summary>
            private static ChildPlan SimpleContentChild(SequencePlan sp) =>
                new(SimpleContentField, sp.SimpleContentType!, IsValueType: false,
                    ChildShape.RequiredSingle, sp.SimpleContent!);

            /// <summary>The lone required attribute of a type, or null.</summary>
            private static AttrPlan? RequiredAttr(SequencePlan sp) =>
                sp.Attributes is { Count: 1 } && sp.Attributes[0].Required ? sp.Attributes[0] : null;

            /// <summary>
            /// Whether a type carries optional attributes. ValidateAttributes has already ruled out
            /// mixtures, so "has attributes and none is the lone required one" means all are optional.
            /// </summary>
            private static bool HasOptionalAttributes(SequencePlan sp) =>
                sp.Attributes is { Count: > 0 } && RequiredAttr(sp) is null;

            /// <summary>
            /// Optional attributes are the leading optionals of the content run: the AT event is the
            /// first production of the content's initial grammar state, and when the attribute is
            /// absent the same code doubles as the first SE (cbexigen model, as in
            /// <see cref="CodecEmitter"/>). Prepending them lets the general run machine handle them.
            /// </summary>
            private static IReadOnlyList<ChildPlan> WithOptionalAttributes(SequencePlan sp)
            {
                if (sp.Attributes is null or { Count: 0 } || RequiredAttr(sp) is not null)
                    return sp.Children;

                var list = new List<ChildPlan>(sp.Attributes.Count + sp.Children.Count);
                foreach (var a in sp.Attributes)
                    list.Add(new ChildPlan(a.FieldName, a.Type, IsValueType: false,
                                           ChildShape.OptionalSingle, new ValueEncoding.AttributeValue()));
                list.AddRange(sp.Children);
                return list;
            }

            /// <summary>
            /// Whether a child's value is framed by a value-start bit and a child EE. An AT value is
            /// not: the run's event code *was* the AT event. A complex child frames itself. An opaque
            /// (un-modelled XMLDSig) child is never really read — it throws — so framing it would
            /// consume a bit before failing and leave the rest unreachable.
            /// </summary>
            private static bool WrapsValue(ChildPlan c) =>
                c.Value is not ValueEncoding.ComplexRef
                       and not ValueEncoding.AttributeValue
                       and not ValueEncoding.OpaqueElement;

            // ---------------------------------------------------------------- naming

            /// <summary>
            /// Kotlin's hard keywords. Lower-casing a PascalCase schema name can land on one — the
            /// C# emitter never hits this because it keeps the name PascalCase. XMLDSig's
            /// <c>Object</c> element is the real-world case.
            /// </summary>
            private static readonly HashSet<string> KotlinKeywords = new(StringComparer.Ordinal)
            {
                "as", "break", "class", "continue", "do", "else", "false", "for", "fun", "if", "in",
                "interface", "is", "null", "object", "package", "return", "super", "this", "throw",
                "true", "try", "typealias", "typeof", "val", "var", "when", "while",
            };

            /// <summary>
            /// Escapes a schema-derived name for use inside a generated Kotlin string literal. `$`
            /// matters here: the grammar's synthetic inline-choice child is literally named
            /// `$InlineChoice`, and unescaped it would be read as a string template.
            /// </summary>
            private static string KStr(string s) =>
                s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("$", "\\$");

            /// <summary>Kotlin properties are camelCase; the plan's field names are PascalCase.</summary>
            private static string Camel(string pascal) =>
                pascal.Length == 0 ? pascal : char.ToLowerInvariant(pascal[0]) + pascal.Substring(1);

            /// <summary>
            /// A property name as written in declarations and accessors — back-quoted when the
            /// camelCase form collides with a keyword.
            /// </summary>
            private static string Prop(string pascal) => Ident(Camel(pascal));

            /// <summary>
            /// A schema name as a Kotlin identifier, case unchanged. Characters illegal in an
            /// identifier become <c>_</c>, mirroring what <c>CodecEmitter</c> does for C# so both
            /// back ends expose the same names — WPT's power classes are spelled <c>MF-WPT1</c> in
            /// the schema. A name that then collides with a keyword is back-quoted, which C# does
            /// not need: the -20 DER schemas have a unit literally called <c>var</c>, a hard keyword
            /// in Kotlin but only a contextual one in C#.
            /// </summary>
            private static string Ident(string name)
            {
                if (string.IsNullOrEmpty(name)) return "_";

                var chars = name.ToCharArray();
                for (var i = 0; i < chars.Length; i++)
                    if (!(char.IsLetterOrDigit(chars[i]) || chars[i] == '_'))
                        chars[i] = '_';

                var result = new string(chars);
                if (char.IsDigit(result[0])) result = "_" + result;

                return KotlinKeywords.Contains(result) ? "`" + result + "`" : result;
            }

            /// <summary>
            /// The decoder's local for a field. Built from the unescaped name: the leading underscore
            /// already makes it a legal identifier, and back-quotes would not survive concatenation.
            /// </summary>
            private static string Local(string pascal) => "_" + Camel(pascal);

            private static string Type(TypeRef t) => t switch
            {
                TypeRef.Primitive p => p.Kind switch
                {
                    PrimitiveKind.Bool   => "Boolean",
                    PrimitiveKind.Int8   => "Byte",
                    PrimitiveKind.Int16  => "Short",
                    PrimitiveKind.Int32  => "Int",
                    PrimitiveKind.Int64  => "Long",
                    PrimitiveKind.UInt8  => "UByte",
                    PrimitiveKind.UInt16 => "UShort",
                    PrimitiveKind.UInt32 => "UInt",
                    PrimitiveKind.UInt64 => "ULong",
                    PrimitiveKind.String => "String",
                    PrimitiveKind.Binary => "ByteArray",
                    _ => throw new NotSupportedException($"Kotlin back end: primitive {p.Kind}."),
                },
                TypeRef.Named n => n.Name,
                _ => throw new NotSupportedException("Kotlin back end: untyped child."),
            };

            /// <summary>Declared type of a child, including Kotlin's nullability / list wrapping.</summary>
            private static string DeclType(ChildPlan c) => c.Shape switch
            {
                ChildShape.BoundedRepeating => $"List<{Type(c.Type)}>",
                ChildShape.OptionalSingle   => Type(c.Type) + "?",
                _                           => Type(c.Type),
            };

            // ---------------------------------------------------------------- types

            private void EmitEnum(EnumPlan e)
            {
                _sb.Append("enum class ").Append(e.Name).AppendLine(" {");
                for (var i = 0; i < e.Members.Count; i++)
                    _sb.Append("    ").Append(Ident(e.Members[i]))
                       .AppendLine(i == e.Members.Count - 1 ? "" : ",");
                _sb.AppendLine("}");
            }

            private void EmitOpaque(string t)
            {
                _sb.Append("/** Opaque placeholder for the un-modelled XMLDSig element `").Append(t)
                   .AppendLine("`.");
                _sb.AppendLine(" *  Only ever encoded/decoded as absent; a present instance fails loud. */");
                _sb.Append("class ").AppendLine(t);
            }

            /// <summary>
            /// Kotlin resolves top-level declarations regardless of order, so — unlike the C#
            /// emitter — no dependency or base-before-derived sorting is needed here.
            /// </summary>
            private void EmitRecords()
            {
                // Not .ToHashSet(): this file is also compiled into the netstandard2.0 analyzer
                // project, whose LINQ has no such overload.
                _baseNames = new HashSet<string>(plan.ComplexTypes.Values
                                                     .Select(s => s.BaseRecordName)
                                                     .Where(n => n is not null)!,
                                                 StringComparer.Ordinal);

                foreach (var ge in plan.GlobalElements)
                    EmitRecord(ge.Body, ge.TypeName);
                foreach (var sp in plan.ComplexTypes.Values)
                    EmitRecord(sp, sp.RecordName);
            }

            private void EmitRecord(SequencePlan sp, string name)
            {
                if (!_emitted.Add(name)) return;
                Target(_decl, name);

                // A type others extend must be `open`; an abstract one `abstract`. Only leaves that
                // take part in no hierarchy can be `data` classes — and only if they carry at least
                // one parameter, which Kotlin requires of a data class (an empty complexType such as
                // EIM_AReqAuthorizationModeType is a plain class).
                var extended = _baseNames.Contains(name);
                var hasParams = sp.Children.Count > 0
                                || sp.Attributes is { Count: > 0 }
                                || sp.SimpleContent is not null;
                var keyword  = sp.IsAbstract ? "abstract class"
                             : extended      ? "open class"
                             : sp.BaseRecordName is not null || !hasParams ? "class"
                             : "data class";

                // Inheritance: the first N flattened children belong to the base, are re-declared
                // here as `override`, and are handed straight to the base constructor.
                var baseChildren = sp.BaseRecordName is not null
                                   && plan.ComplexTypes.TryGetValue(sp.BaseRecordName, out var basePlan)
                                       ? basePlan.Children.Count
                                       : 0;

                _sb.Append(keyword).Append(' ').Append(name);

                if (sp.Children.Count == 0 && sp.Attributes is null or { Count: 0 } && sp.SimpleContent is null)
                {
                    _sb.Append(sp.BaseRecordName is not null ? " : " + sp.BaseRecordName + "()" : "").AppendLine();
                    _sb.AppendLine();
                    return;
                }

                // Attributes come first, matching the AT-before-content order of the grammar (and the
                // C# emitter's parameter order). They never belong to a base type — Reject() bars
                // attributes on a derived type — so the base-child indices below are unaffected.
                var parms = new List<string>();
                if (sp.Attributes is not null)
                    foreach (var a in sp.Attributes)
                        parms.Add(((sp.IsAbstract || extended) ? "open val " : "val ")
                                  + Prop(a.FieldName) + ": " + Type(a.Type) + (a.Required ? "" : "?"));

                // xs:simpleContent contributes a single value parameter and has no children.
                if (sp.SimpleContent is not null)
                    parms.Add(((sp.IsAbstract || extended) ? "open val " : "val ")
                              + Prop(SimpleContentField) + ": " + Type(sp.SimpleContentType!));

                // Which parameters come from the base is a question of identity, not position: they
                // are handed to the base constructor by name, so attributes and simpleContent may sit
                // in front of them without disturbing anything.
                var baseArgs = new List<string>();

                for (var i = 0; i < sp.Children.Count; i++)
                {
                    var fromBase = i < baseChildren;
                    foreach (var (nm, ty) in ChildParams(sp.Children[i]))
                    {
                        var modifier = fromBase ? "override val "
                                     : (sp.IsAbstract || extended) ? "open val "
                                     : "val ";
                        parms.Add(modifier + nm + ": " + ty);
                        if (fromBase) baseArgs.Add(nm);
                    }
                }

                _sb.AppendLine("(");
                for (var i = 0; i < parms.Count; i++)
                    _sb.Append("    ").Append(parms[i]).AppendLine(i == parms.Count - 1 ? "" : ",");
                _sb.Append(")");

                if (sp.BaseRecordName is not null)
                    _sb.Append(" : ").Append(sp.BaseRecordName).Append('(')
                       .Append(string.Join(", ", baseArgs)).Append(')');
                _sb.AppendLine();
                _sb.AppendLine();
            }

            // ---------------------------------------------------------------- codec

            /// <summary>
            /// The codec object: the public entry points, and nothing else. Every per-type
            /// encoder/decoder it used to hold now lives beside its own type.
            /// </summary>
            /// <summary>How many types deep a base chain runs — the sort key for a type dispatch.</summary>
            private int BaseDepth(SequencePlan sp)
            {
                var depth   = 0;
                var current = sp;
                while (current.BaseRecordName is not null
                       && plan.ComplexTypes.TryGetValue(current.BaseRecordName, out var next))
                {
                    depth++;
                    current = next;
                }
                return depth;
            }

            private void EmitFacade()
            {
                _sb.Append("object ").Append(codecObject).AppendLine(" {");
                _sb.AppendLine();
                _sb.AppendLine("    const val EXI_HEADER: Byte = 0x80.toByte()");
                _sb.AppendLine();
                _sb.AppendLine("    /** Generous upper bound; encode() returns a right-sized copy. */");
                _sb.AppendLine("    private const val MAX_MESSAGE_BYTES = 65536");
                _sb.AppendLine();

                foreach (var ge in plan.GlobalElements)
                {
                    _sb.Append("    fun encode(msg: ").Append(ge.TypeName).AppendLine("): ByteArray {");
                    _sb.AppendLine("        val buf = ByteArray(MAX_MESSAGE_BYTES)");
                    _sb.AppendLine("        buf[0] = EXI_HEADER");
                    _sb.AppendLine("        val w = BitWriter(buf, 1)");
                    _sb.Append("        w.writeBits(").Append(ge.DocumentIndex).Append("u, ")
                       .Append(plan.DocumentSelectorBits).AppendLine(")   // document element selector");
                    _sb.Append("        encode").Append(ge.TypeName).AppendLine("(w, msg)");
                    _sb.AppendLine("        w.alignToByte()");
                    _sb.AppendLine("        return buf.copyOf(1 + w.bytesWritten)");
                    _sb.AppendLine("    }");
                    _sb.AppendLine();
                }

                // The mirror of decodeAny. Without it, `encode` is a set of static overloads and a
                // caller holding a message it did not construct itself has to write the type switch
                // by hand — which is what Secc20Ac and Secc20Dc do on the C# side, the usual sign
                // that the generator was missing a method. Branches are ordered most-derived-first:
                // Kotlin's `is` matches subtypes, so a base-first order would encode a derived
                // message with its base's document index.
                _sb.AppendLine("    /** Encodes any document element of this message set, dispatching on its runtime type. */");
                _sb.AppendLine("    fun encodeAny(msg: Any): ByteArray =");
                _sb.AppendLine("        when (msg) {");
                foreach (var ge in plan.GlobalElements.OrderByDescending(g => BaseDepth(g.Body))
                                                      .ThenBy(g => g.DocumentIndex))
                    _sb.Append("            is ").Append(ge.TypeName).AppendLine(" -> encode(msg)");
                _sb.AppendLine();
                _sb.AppendLine("            else -> throw IllegalArgumentException(");
                _sb.AppendLine("                \"${msg::class.simpleName} is not a document element of this message set.\")");
                _sb.AppendLine("        }");
                _sb.AppendLine();

                _sb.AppendLine("    fun decodeAny(src: ByteArray): Any {");
                _sb.AppendLine("        require(src.isNotEmpty() && src[0] == EXI_HEADER) { \"Invalid EXI header.\" }");
                _sb.AppendLine("        val r = BitReader(src, 1)");
                _sb.Append("        return when (val sel = r.readBits(").Append(plan.DocumentSelectorBits).AppendLine(")) {");
                foreach (var ge in plan.GlobalElements)
                    _sb.Append("            ").Append(ge.DocumentIndex).Append("u -> decode")
                       .Append(ge.TypeName).AppendLine("(r)");
                _sb.AppendLine("            else -> throw IllegalArgumentException(\"Unknown document index $sel.\")");
                _sb.AppendLine("        }");
                _sb.AppendLine("    }");
                _sb.AppendLine();

                EmitFragmentCodecs();

                _sb.AppendLine("}");
            }

            private void EmitTypeCodecs()
            {
                foreach (var sp in plan.ComplexTypes.Values)
                    EmitSequenceCodec(sp, sp.RecordName);
                foreach (var ge in plan.GlobalElements)
                    EmitSequenceCodec(ge.Body, ge.TypeName);
            }

            /// <summary>
            /// One EXI fragment encoder/decoder per signable element: the EXI header, the element's
            /// fragment-grammar event code (a selector over every element declaration of the set),
            /// its content, then End Fragment. No document or body wrapper — this is what XMLDSig
            /// digests. Mirrors <c>CodecEmitter</c>, which is diffed against cbV2G's exiFragment.
            /// </summary>
            private void EmitFragmentCodecs()
            {
                var bits = plan.FragmentSelectorBits;

                foreach (var f in plan.Fragments)
                {
                    _sb.Append("    fun encodeFragment_").Append(f.ElementName).Append("(content: ")
                       .Append(f.TypeName).AppendLine("): ByteArray {");
                    _sb.AppendLine("        val buf = ByteArray(MAX_MESSAGE_BYTES)");
                    _sb.AppendLine("        buf[0] = EXI_HEADER");
                    _sb.AppendLine("        val w = BitWriter(buf, 1)");
                    _sb.Append("        w.writeBits(").Append(f.EventCode).Append("u, ").Append(bits)
                       .Append(")   // fragment SE(").Append(f.ElementName).AppendLine(")");
                    _sb.Append("        encode").Append(f.TypeName).AppendLine("(w, content)");
                    _sb.Append("        w.writeBits(").Append(plan.FragmentEndCode).Append("u, ").Append(bits)
                       .AppendLine(")   // End Fragment (ED)");
                    _sb.AppendLine("        w.alignToByte()");
                    _sb.AppendLine("        return buf.copyOf(1 + w.bytesWritten)");
                    _sb.AppendLine("    }");
                    _sb.AppendLine();

                    _sb.Append("    fun decodeFragment_").Append(f.ElementName).Append("(src: ByteArray): ")
                       .Append(f.TypeName).AppendLine(" {");
                    _sb.AppendLine("        require(src.isNotEmpty() && src[0] == EXI_HEADER) { \"Invalid EXI header.\" }");
                    _sb.AppendLine("        val r = BitReader(src, 1)");
                    _sb.Append("        require(r.readBits(").Append(bits).Append(") == ").Append(f.EventCode)
                       .Append("u) { \"Not a ").Append(KStr(f.ElementName)).AppendLine(" fragment.\" }");
                    _sb.Append("        val result = decode").Append(f.TypeName).AppendLine("(r)");
                    _sb.Append("        require(r.readBits(").Append(bits).Append(") == ").Append(plan.FragmentEndCode)
                       .AppendLine("u) { \"missing End Fragment.\" }");
                    _sb.AppendLine("        return result");
                    _sb.AppendLine("    }");
                    _sb.AppendLine();
                }
            }

            /// <summary>
            /// A global element's body is also registered in <c>ComplexTypes</c>, so the two
            /// emission loops overlap; emit each codec pair once.
            /// </summary>
            private void EmitSequenceCodec(SequencePlan sp, string name)
            {
                // An abstract type is never encoded or decoded through its own name — substitution
                // dispatch always names a concrete member — and a decoder for one would have to
                // instantiate it. The C# emitter skips them for the same reason.
                if (sp.IsAbstract) return;
                if (!_codecs.Add(name)) return;
                Target(_code, name);
                EmitEncode(sp, name);
                EmitDecode(sp, name);
            }

            private void EmitEncode(SequencePlan sp, string name)
            {
                _sb.Append("    internal fun encode").Append(name).Append("(w: BitWriter, msg: ")
                   .Append(name).AppendLine(") {");

                // A required attribute is unconditional: a 1-bit AT event, then a bare value.
                if (RequiredAttr(sp) is { } req)
                {
                    _sb.AppendLine("        w.writeBits(0u, 1)   // AT(required attribute)");
                    _sb.Append("        ExiPrimitives.writeStringValue(w, msg.").Append(Prop(req.FieldName)).AppendLine(")");
                }

                if (sp.SimpleContent is not null)
                {
                    if (HasOptionalAttributes(sp))
                        EmitEncodeSimpleContentOptionalAttrs(sp);
                    else
                    {
                        _sb.AppendLine("        w.writeBits(0u, 1)   // CONTENT event");
                        EmitEncodeBareValue(SimpleContentChild(sp), "msg." + Prop(SimpleContentField), "        ");
                    }
                    _sb.AppendLine("        w.writeBits(0u, 1)   // element EE");
                    _sb.AppendLine("    }");
                    _sb.AppendLine();
                    return;
                }

                if (sp.IsChoice)
                {
                    EmitEncodeChoice(sp);
                    _sb.AppendLine("        w.writeBits(0u, 1)   // element EE");
                    _sb.AppendLine("    }");
                    _sb.AppendLine();
                    return;
                }

                var kids = WithOptionalAttributes(sp);
                for (var i = 0; i < kids.Count;)
                {
                    var c = kids[i];
                    if (c.Shape == ChildShape.BoundedRepeating && (kids.Count == 1 || c.ListMin > 0))
                    {
                        if (kids.Count == 1)
                        {
                            EmitEncodeList(c, sp);
                            i++;   // the list's own terminator doubles as the element EE
                        }
                        else if (c.ListMin > 0 && i == kids.Count - 1)
                        {
                            EmitEncodeRepeatingChild(c, "        ");
                            i++;   // ditto
                        }
                        else if (c.ListMin > 0 && i + 1 < kids.Count)
                        {
                            // The list's "another item vs move on" code doubles as the next particle's
                            // event code, so the two are emitted together.
                            var tailC = kids[i + 1];
                            EmitEncodeRepeatingWithTail(c, tailC, "        ");
                            i += 2;
                            // An optional tail ends the sequence and closes the element itself.
                            if (i == kids.Count && tailC.Shape == ChildShape.RequiredSingle)
                                _sb.AppendLine("        w.writeBits(0u, 1)   // element EE");
                        }
                        else
                            throw new NotSupportedException(
                                $"Kotlin back end: the repeating child '{name}.{c.FieldName}' has a shape " +
                                "this back end does not model yet.");
                        continue;
                    }
                    if (StartsRun(c))
                    {
                        // A run of optionals ends either at the element EE or at the first required
                        // child, which then carries the run's highest event code.
                        var e    = RunEnd(kids, i);
                        var term = e < kids.Count ? kids[e] : null;
                        RejectRunTerminator(term, name);

                        var midE = MidRunListIndex(kids, i, e);
                        if (midE >= 0)
                        {
                            EmitEncodeMidRunList(kids, i, midE, e, kids.Count);
                            i = kids.Count;   // this grammar closes the element itself
                            continue;
                        }

                        EmitEncodeOptionalRun(kids, i, e, term);

                        i = term is null ? kids.Count : e + 1;
                        // A repeating terminator closes the element with its own list-end EE.
                        if (term is not null && term.Shape != ChildShape.BoundedRepeating && i == kids.Count)
                            _sb.AppendLine("        w.writeBits(0u, 1)   // element EE");
                        continue;
                    }

                    // A substitution reference is dispatched by its own event code; that code IS the
                    // selector, so no SE precedes it.
                    if (c.Value is ValueEncoding.SubstitutionChoice sub)
                    {
                        EmitEncodeSubstitution(c, sub);
                        i++;
                        if (i == kids.Count)
                            _sb.AppendLine("        w.writeBits(0u, 1)   // element EE");
                        continue;
                    }

                    // Likewise an inline choice with no optional siblings to flatten into.
                    if (c.Value is ValueEncoding.InlineChoice inl)
                    {
                        EmitEncodeInlineChoiceStandalone(inl);
                        i++;
                        if (i == kids.Count)
                            _sb.AppendLine("        w.writeBits(0u, 1)   // element EE");
                        continue;
                    }

                    _sb.AppendLine("        w.writeBits(0u, 1)   // SE");
                    EmitEncodeValue(c, "msg." + Prop(c.FieldName), "        ");
                    i++;
                    if (i == kids.Count)
                        _sb.AppendLine("        w.writeBits(0u, 1)   // element EE");
                }

                if (kids.Count == 0)
                    _sb.AppendLine("        w.writeBits(0u, 1)   // element EE");

                _sb.AppendLine("    }");
                _sb.AppendLine();
            }

            /// <summary>
            /// xs:simpleContent with optional attributes: a bare value (CONTENT) preceded by an
            /// optional run of AT productions. State k offers <c>{attr_k … attr_{n-1}, CONTENT}</c>
            /// over ceil(log2(count+1)) bits — the same grammar <c>CodecEmitter</c> emits, verified
            /// there against SignatureValueType. The element EE is a separate 1-bit production the
            /// caller writes.
            /// </summary>
            private void EmitEncodeSimpleContentOptionalAttrs(SequencePlan sp)
            {
                var oa    = sp.Attributes!;
                var n     = oa.Count;
                var id    = _run++;
                var value = SimpleContentChild(sp);
                const string ind = "                    ";

                _sb.Append("        var st").Append(id).AppendLine(" = 0");
                _sb.Append("        var done").Append(id).AppendLine(" = false");
                _sb.Append("        while (!done").Append(id).AppendLine(") {");
                _sb.Append("            when (st").Append(id).AppendLine(") {");

                for (var k = 0; k <= n; k++)
                {
                    // remaining optional attributes + CONTENT, plus the non-strict phantom
                    var width = BitsFor((n - k + 1) + 1);
                    _sb.Append("                ").Append(k).AppendLine(" -> {");

                    var code  = 0;
                    var first = true;
                    for (var i = k; i < n; i++, code++, first = false)
                    {
                        var prop = "msg." + Prop(oa[i].FieldName);
                        _sb.Append(ind).Append(first ? "if (" : "} else if (").Append(prop).AppendLine(" != null) {");
                        _sb.Append(ind).Append("    w.writeBits(").Append(code).Append("u, ").Append(width)
                           .Append(")   // AT(").Append(oa[i].FieldName).AppendLine(")");
                        _sb.Append(ind).Append("    ExiPrimitives.writeStringValue(w, ").Append(prop).AppendLine("!!)");
                        _sb.Append(ind).Append("    st").Append(id).Append(" = ").Append(i + 1).AppendLine();
                    }

                    var tail = first ? ind : ind + "    ";
                    if (!first)
                        _sb.Append(ind).AppendLine("} else {");

                    _sb.Append(tail).Append("w.writeBits(").Append(code).Append("u, ").Append(width)
                       .AppendLine(")   // CONTENT");
                    EmitEncodeBareValue(value, "msg." + Prop(SimpleContentField), tail);
                    _sb.Append(tail).Append("done").Append(id).AppendLine(" = true");

                    if (!first)
                        _sb.Append(ind).AppendLine("}");

                    _sb.AppendLine("                }");
                }

                _sb.AppendLine("            }");
                _sb.AppendLine("        }");
            }

            /// <summary>
            /// An inline xs:choice with no adjacent optionals to flatten into: N sibling nullable
            /// fields, exactly one set. An n-bit code selects it and the content follows directly —
            /// the code IS the selector, so there is no SE wrapper.
            /// </summary>
            private void EmitEncodeInlineChoiceStandalone(ValueEncoding.InlineChoice ic)
            {
                for (var i = 0; i < ic.Members.Count; i++)
                {
                    var m = ic.Members[i];
                    var f = "msg." + Prop(m.FieldName);
                    _sb.Append("        ").Append(i == 0 ? "if (" : "} else if (").Append(f).AppendLine(" != null) {");
                    _sb.Append("            w.writeBits(").Append(i).Append("u, ").Append(ic.BitWidth)
                       .Append(")   // ").AppendLine(m.ElementName);
                    EmitEncodeValue(AsChildPlan(m), f + "!!", "            ");
                }
                _sb.AppendLine("        } else {");
                _sb.AppendLine("            throw IllegalArgumentException(\"no choice alternative set\")");
                _sb.AppendLine("        }");
            }

            /// <summary>
            /// xs:choice content: exactly one alternative is present, and its index — not an SE — is
            /// the event code, over a width covering the alternatives plus the non-strict phantom.
            /// The caller writes the element EE afterwards.
            /// </summary>
            private void EmitEncodeChoice(SequencePlan sp)
            {
                if (sp.Children.Count == 0)
                    throw new NotSupportedException($"Kotlin back end: '{sp.RecordName}' is an empty xs:choice.");

                var width = BitsFor(sp.Children.Count + 1);
                for (var i = 0; i < sp.Children.Count; i++)
                {
                    var c    = sp.Children[i];
                    var prop = "msg." + Prop(c.FieldName);
                    _sb.Append("        ").Append(i == 0 ? "if (" : "} else if (").Append(prop).AppendLine(" != null) {");
                    _sb.Append("            w.writeBits(").Append(i).Append("u, ").Append(width)
                       .Append(")   // ").AppendLine(c.FieldName);
                    EmitEncodeValue(c, prop + "!!", "            ");
                }
                _sb.AppendLine("        } else {");
                _sb.Append("            throw IllegalArgumentException(\"no choice alternative set for ")
                   .Append(KStr(sp.RecordName)).AppendLine("\")");
                _sb.AppendLine("        }");
            }

            /// <summary>
            /// A repeating child: first item takes a 1-bit SE, every following item and the
            /// terminator a 2-bit event code (item = 0, EE = 1). Mirrors <c>CodecEmitter</c>.
            /// </summary>
            private void EmitEncodeList(ChildPlan c, SequencePlan sp)
            {
                var (min, max) = ListBounds(c, sp);
                _sb.Append("        val list = msg.").AppendLine(Prop(c.FieldName));
                EmitEncodeRepeating(c, "list", min, max, "        ");
            }

            /// <summary>
            /// A required list closing a sequence that has other children too. Unlike the lone-child
            /// case the bounds sit on the child, and the local is named after the field so it cannot
            /// collide with a sibling's.
            /// </summary>
            private void EmitEncodeRepeatingChild(ChildPlan c, string indent)
            {
                var list = ListLocal(c);
                _sb.Append(indent).Append("val ").Append(list).Append(" = msg.").AppendLine(Prop(c.FieldName));
                EmitEncodeRepeating(c, list, Math.Max(1, c.ListMin), c.ListMax, indent);
            }

            private static string ListLocal(ChildPlan c) => Camel(c.FieldName) + "List";

            /// <summary>
            /// A required list followed by exactly one more particle. cbexigen unrolls the two
            /// positions rather than looping: after the first item the code says either "another item"
            /// or "start the tail", and once the list is full only the tail remains — so the tail is
            /// reachable at two different codes and widths. Verified in <c>CodecEmitter</c> against
            /// cbV2G for AuthorizationSetupResType, the only place -20 CommonMessages needs it.
            /// </summary>
            private void EmitEncodeRepeatingWithTail(ChildPlan list, ChildPlan tail, string indent)
            {
                if (tail.Value is ValueEncoding.SubstitutionChoice or ValueEncoding.InlineChoice)
                    throw new NotSupportedException(
                        $"Kotlin back end: repeating '{list.FieldName}' with a choice/substitution tail " +
                        $"('{tail.FieldName}') is not implemented.");

                if (list.ListMax != 2)
                {
                    EmitEncodeRepeatingSelfLoop(list, tail, indent);
                    return;
                }

                var prop     = "msg." + Prop(list.FieldName);
                var tailProd = ProductionCount(tail);
                var widthMid = BitsFor((1 + tailProd) + 1);   // another item, the tail, + the phantom
                var widthMax = BitsFor(tailProd + 1);         // list full: only the tail remains

                _sb.Append(indent).Append("require(").Append(prop).AppendLine(".size in 1..2) { \"list size out of schema range\" }");
                _sb.Append(indent).Append("w.writeBits(0u, 1)   // SE(").Append(list.FieldName).AppendLine(")");
                EmitEncodeValue(list, prop + "[0]", indent);

                _sb.Append(indent).Append("if (").Append(prop).AppendLine(".size > 1) {");
                _sb.Append(indent).Append("    w.writeBits(0u, ").Append(widthMid).Append(")   // ")
                   .Append(list.FieldName).AppendLine(" (loop)");
                EmitEncodeValue(list, prop + "[1]", indent + "    ");
                EmitEncodeTailDispatch(tail, 0, widthMax, indent + "    ");
                _sb.Append(indent).AppendLine("} else {");
                EmitEncodeTailDispatch(tail, 1, widthMid, indent + "    ");
                _sb.Append(indent).AppendLine("}");
            }

            /// <summary>
            /// The self-loop variant of <see cref="EmitEncodeRepeatingWithTail"/>, for a list too long
            /// to unroll. <b>This shape has no working reference encoder</b>: cbexigen's own output for
            /// it (WPT_LF_TransmitterDataType) is documented in <c>CodecEmitter</c> as unable to encode
            /// even the schema's required minimum. Both back ends therefore emit a plain
            /// schema-informed non-strict reading — first item unconditional, then a loop offering
            /// [another item, the tail, (element EE)] — which is a design decision, not a diff against
            /// a reference. Treat bytes from this construct as unvalidated.
            /// </summary>
            private void EmitEncodeRepeatingSelfLoop(ChildPlan list, ChildPlan tail, string indent)
            {
                var required = tail.Shape == ChildShape.RequiredSingle;
                var prop     = "msg." + Prop(list.FieldName);
                var tailProp = "msg." + Prop(tail.FieldName);
                var width    = BitsFor(1 + ProductionCount(tail) + (required ? 0 : 1) + 1);

                _sb.Append(indent).Append("require(").Append(prop).Append(".size in ")
                   .Append(Math.Max(1, list.ListMin)).Append("..").Append(list.ListMax)
                   .AppendLine(") { \"list size out of schema range\" }");
                _sb.Append(indent).Append("w.writeBits(0u, 1)   // SE(").Append(list.FieldName).AppendLine(") first");
                EmitEncodeValue(list, prop + "[0]", indent);

                _sb.Append(indent).Append("for (ci in 1 until ").Append(prop).AppendLine(".size) {");
                _sb.Append(indent).Append("    w.writeBits(0u, ").Append(width).Append(")   // ")
                   .Append(list.FieldName).AppendLine(" (loop)");
                EmitEncodeValue(list, prop + "[ci]", indent + "    ");
                _sb.Append(indent).AppendLine("}");

                if (required)
                {
                    EmitEncodeTailDispatch(tail, 1, width, indent);
                    return;
                }

                // An optional tail ends the sequence, so this construct closes the element itself.
                _sb.Append(indent).Append("if (").Append(tailProp).AppendLine(" != null) {");
                _sb.Append(indent).Append("    w.writeBits(1u, ").Append(width).Append(")   // ")
                   .AppendLine(tail.FieldName);
                EmitEncodeValue(tail, tailProp + "!!", indent + "    ");
                _sb.Append(indent).AppendLine("    w.writeBits(0u, 1)   // element EE");
                _sb.Append(indent).AppendLine("} else {");
                _sb.Append(indent).Append("    w.writeBits(2u, ").Append(width).AppendLine(")   // element EE");
                _sb.Append(indent).AppendLine("}");
            }

            private void EmitEncodeTailDispatch(ChildPlan tail, int code, int width, string indent)
            {
                _sb.Append(indent).Append("w.writeBits(").Append(code).Append("u, ").Append(width)
                   .Append(")   // ").AppendLine(tail.FieldName);
                EmitEncodeValue(tail, "msg." + Prop(tail.FieldName), indent);
            }

            /// <summary>
            /// A repeating element inside an optional run. The first item takes the run state's event
            /// code; every further item and the closing EE use the 2-bit loop code {item = 0, EE = 1}.
            /// </summary>
            private void EmitEncodeRepeatingItems(ChildPlan p, int firstCode, int width, string indent, string after)
            {
                var list = "msg." + Prop(p.FieldName);
                _sb.Append(indent).Append("w.writeBits(").Append(firstCode).Append("u, ").Append(width)
                   .Append(")   // ").AppendLine(p.FieldName);
                EmitEncodeValue(p, list + "[0]", indent);
                _sb.Append(indent).Append("for (ci in 1 until ").Append(list).AppendLine(".size) {");
                _sb.Append(indent).Append("    w.writeBits(0u, 2)   // ").AppendLine(p.FieldName);
                EmitEncodeValue(p, list + "[ci]", indent + "    ");
                _sb.Append(indent).AppendLine("}");
                _sb.Append(indent).AppendLine("w.writeBits(1u, 2)   // element EE (list end)");
                _sb.Append(indent).AppendLine(after);
            }

            /// <summary>
            /// Declares a run particle's local in record order: a list gets an empty collection (an
            /// empty one also encodes "absent"), everything else a nullable.
            /// </summary>
            private void DeclareRunLocal(ChildPlan c, List<string> ctor)
            {
                if (c.Value is ValueEncoding.InlineChoice ic)
                {
                    DeclareInlineChoiceLocals(ic, ctor);
                    return;
                }
                if (c.Shape == ChildShape.BoundedRepeating)
                {
                    _sb.Append("        val ").Append(ListLocal(c)).Append(" = ArrayList<")
                       .Append(Type(c.Type)).AppendLine(">()");
                    ctor.Add(ListLocal(c));
                    return;
                }
                _sb.Append("        var ").Append(Local(c.FieldName)).Append(": ").Append(DeclType(c))
                   .AppendLine(" = null");
                ctor.Add(Local(c.FieldName));
            }

            /// <summary>Decode mirror of <see cref="EmitEncodeRepeatingWithTail"/>.</summary>
            private void EmitDecodeRepeatingWithTail(ChildPlan list, ChildPlan tail, List<string> ctor, string indent)
            {
                var lst      = ListLocal(list);
                var tailProd = ProductionCount(tail);
                var required = tail.Shape == ChildShape.RequiredSingle;
                var widthMid = BitsFor((1 + tailProd) + 1);
                var widthMax = BitsFor(tailProd + 1);

                _sb.Append(indent).Append("val ").Append(lst).Append(" = ArrayList<")
                   .Append(Type(list.Type)).AppendLine(">()");
                _sb.Append(indent).Append("r.readBits(1)   // SE(").Append(list.FieldName).AppendLine(") first");
                EmitDecodeItem(list, lst, lst + "First", indent);
                ctor.Add(lst);

                _sb.Append(indent).Append("var ").Append(Local(tail.FieldName)).Append(": ")
                   .Append(Type(tail.Type)).AppendLine("? = null");
                ctor.Add(Local(tail.FieldName) + (required ? "!!" : ""));

                if (list.ListMax != 2)
                {
                    EmitDecodeRepeatingSelfLoop(list, tail, lst, required, indent);
                    return;
                }

                _sb.Append(indent).Append("when (r.readBits(").Append(widthMid).AppendLine(")) {");
                _sb.Append(indent).Append("    0u -> {   // ").Append(list.FieldName).AppendLine(" (loop)");
                EmitDecodeItem(list, lst, lst + "Next", indent + "        ");
                _sb.Append(indent).Append("        when (r.readBits(").Append(widthMax).AppendLine(")) {");
                EmitDecodeTailCase(tail, 0, indent + "            ");
                _sb.Append(indent).AppendLine("            else -> throw IllegalArgumentException(\"invalid event code\")");
                _sb.Append(indent).AppendLine("        }");
                _sb.Append(indent).AppendLine("    }");
                EmitDecodeTailCase(tail, 1, indent + "    ");
                _sb.Append(indent).AppendLine("    else -> throw IllegalArgumentException(\"invalid event code\")");
                _sb.Append(indent).AppendLine("}");
            }

            /// <summary>
            /// Decode mirror of <see cref="EmitEncodeRepeatingSelfLoop"/>. Note the present-tail branch
            /// consumes the element EE the encoder writes after the tail's content; without it the two
            /// sides would disagree by exactly one bit.
            /// </summary>
            private void EmitDecodeRepeatingSelfLoop(ChildPlan list, ChildPlan tail, string lst,
                                                     bool required, string indent)
            {
                var id    = _run++;
                var width = BitsFor(1 + ProductionCount(tail) + (required ? 0 : 1) + 1);

                _sb.Append(indent).Append("var done").Append(id).AppendLine(" = false");
                _sb.Append(indent).Append("while (!done").Append(id).AppendLine(") {");
                _sb.Append(indent).Append("    val rc = r.readBits(").Append(width).AppendLine(")");
                _sb.Append(indent).AppendLine("    if (rc == 0u) {");
                _sb.Append(indent).Append("        require(").Append(lst).Append(".size < ").Append(list.ListMax)
                   .AppendLine(") { \"invalid repeating-element event code\" }");
                EmitDecodeItem(list, lst, lst + "Next", indent + "        ");
                _sb.Append(indent).AppendLine("    } else if (rc == 1u) {");
                if (WrapsValue(tail))
                    _sb.Append(indent).AppendLine("        r.readBits(1)   // value-start");
                _sb.Append(indent).Append("        ").Append(Local(tail.FieldName)).Append(" = ")
                   .AppendLine(DecodeValueExpr(tail));
                if (WrapsValue(tail))
                    _sb.Append(indent).AppendLine("        r.readBits(1)   // child EE");
                if (!required)
                    _sb.Append(indent).AppendLine("        r.readBits(1)   // element EE");
                _sb.Append(indent).Append("        done").Append(id).AppendLine(" = true");
                if (!required)
                {
                    _sb.Append(indent).AppendLine("    } else if (rc == 2u) {");
                    _sb.Append(indent).Append("        done").Append(id).AppendLine(" = true   // element EE");
                }
                _sb.Append(indent).AppendLine("    } else {");
                _sb.Append(indent).AppendLine("        throw IllegalArgumentException(\"invalid event code\")");
                _sb.Append(indent).AppendLine("    }");
                _sb.Append(indent).AppendLine("}");
            }

            private void EmitDecodeTailCase(ChildPlan tail, int code, string indent)
            {
                _sb.Append(indent).Append(code).Append("u -> {   // ").AppendLine(tail.FieldName);
                if (WrapsValue(tail))
                    _sb.Append(indent).AppendLine("    r.readBits(1)   // value-start");
                _sb.Append(indent).Append("    ").Append(Local(tail.FieldName)).Append(" = ")
                   .AppendLine(DecodeValueExpr(tail));
                if (WrapsValue(tail))
                    _sb.Append(indent).AppendLine("    r.readBits(1)   // child EE");
                _sb.Append(indent).AppendLine("}");
            }

            /// <summary>Decode mirror of <see cref="EmitEncodeMidRunList"/>.</summary>
            private void EmitDecodeMidRunList(IReadOnlyList<ChildPlan> kids, int start, int listIdx,
                                              int end, int childCount, List<string> ctor)
            {
                var suffix      = MidRunSuffix(kids, start, listIdx, end, childCount);
                var list        = kids[listIdx];
                var suffixTotal = suffix.Sum(ProductionCount);
                var lst         = ListLocal(list);
                var id          = _run++;
                const string ca = "                        ";

                var w0 = BitsFor(1 + 1 + 1);
                var w1 = BitsFor(1 + suffixTotal + 1 + 1);
                var w2 = BitsFor(suffixTotal + 1 + 1);

                _sb.Append("        val ").Append(lst).Append(" = ArrayList<").Append(Type(list.Type)).AppendLine(">()");
                ctor.Add(lst);
                foreach (var s in suffix)
                {
                    _sb.Append("        var ").Append(Local(s.FieldName)).Append(": ").Append(DeclType(s))
                       .AppendLine(" = null");
                    ctor.Add(Local(s.FieldName));
                }

                _sb.Append("        var st").Append(id).AppendLine(" = 0");
                _sb.Append("        var done").Append(id).AppendLine(" = false");
                _sb.Append("        while (!done").Append(id).AppendLine(") {");
                _sb.Append("            when (st").Append(id).AppendLine(") {");

                _sb.AppendLine("                0 -> {");
                _sb.Append("                    when (r.readBits(").Append(w0).AppendLine(")) {");
                _sb.Append(ca).Append("0u -> {   // ").AppendLine(list.FieldName);
                EmitDecodeItem(list, lst, lst + "First", ca + "    ");
                _sb.Append(ca).Append("    st").Append(id).AppendLine(" = 1");
                _sb.Append(ca).AppendLine("}");
                _sb.Append(ca).Append("1u -> done").Append(id).AppendLine(" = true   // element EE");
                _sb.Append(ca).AppendLine("else -> throw IllegalArgumentException(\"invalid optional-run event code\")");
                _sb.AppendLine("                    }");
                _sb.AppendLine("                }");

                _sb.AppendLine("                1 -> {");
                _sb.Append("                    when (r.readBits(").Append(w1).AppendLine(")) {");
                _sb.Append(ca).Append("0u -> {   // ").AppendLine(list.FieldName);
                EmitDecodeItem(list, lst, lst + "Next", ca + "    ");
                _sb.Append(ca).Append("    st").Append(id).AppendLine(" = 2");
                _sb.Append(ca).AppendLine("}");
                EmitDecodeMidRunTail(suffix, 1, id, ca);
                _sb.AppendLine("                    }");
                _sb.AppendLine("                }");

                _sb.AppendLine("                2 -> {");
                _sb.Append("                    when (r.readBits(").Append(w2).AppendLine(")) {");
                EmitDecodeMidRunTail(suffix, 0, id, ca);
                _sb.AppendLine("                    }");
                _sb.AppendLine("                }");

                if (suffix.Count > 0)
                {
                    // A suffix particle was decoded; only the element EE is left.
                    _sb.AppendLine("                3 -> {");
                    _sb.AppendLine("                    r.readBits(1)   // element EE");
                    _sb.Append("                    done").Append(id).AppendLine(" = true");
                    _sb.AppendLine("                }");
                }

                _sb.AppendLine("            }");
                _sb.AppendLine("        }");
            }

            private void EmitDecodeMidRunTail(List<ChildPlan> suffix, int code, int id, string ca)
            {
                foreach (var s in suffix)
                {
                    _sb.Append(ca).Append(code).Append("u -> {   // ").AppendLine(s.FieldName);
                    if (WrapsValue(s))
                        _sb.Append(ca).AppendLine("    r.readBits(1)   // value-start");
                    _sb.Append(ca).Append("    ").Append(Local(s.FieldName)).Append(" = ")
                       .AppendLine(DecodeValueExpr(s));
                    if (WrapsValue(s))
                        _sb.Append(ca).AppendLine("    r.readBits(1)   // child EE");
                    _sb.Append(ca).Append("    st").Append(id).AppendLine(" = 3");
                    _sb.Append(ca).AppendLine("}");
                    code++;
                }
                _sb.Append(ca).Append(code).Append("u -> done").Append(id).AppendLine(" = true   // element EE");
                _sb.Append(ca).AppendLine("else -> throw IllegalArgumentException(\"invalid optional-run event code\")");
            }

            /// <summary>One nullable local per inline-choice branch, in record-parameter order.</summary>
            private void DeclareInlineChoiceLocals(ValueEncoding.InlineChoice ic, List<string> ctor)
            {
                foreach (var m in ic.Members)
                {
                    _sb.Append("        var ").Append(Local(m.FieldName)).Append(": ").Append(Type(m.Type))
                       .AppendLine("? = null");
                    ctor.Add(Local(m.FieldName));
                }
            }

            /// <summary>Decode mirror of <see cref="EmitEncodeInlineChoiceStandalone"/>.</summary>
            private void EmitDecodeInlineChoiceStandalone(ValueEncoding.InlineChoice ic, List<string> ctor)
            {
                DeclareInlineChoiceLocals(ic, ctor);
                _sb.Append("        when (r.readBits(").Append(ic.BitWidth).AppendLine(")) {");
                for (var i = 0; i < ic.Members.Count; i++)
                {
                    var m = ic.Members[i];
                    _sb.Append("            ").Append(i).Append("u -> {   // ").AppendLine(m.ElementName);
                    EmitDecodeInlineMember(m, "                ");
                    _sb.AppendLine("            }");
                }
                _sb.AppendLine("            else -> throw IllegalArgumentException(\"unknown choice event code\")");
                _sb.AppendLine("        }");
            }

            /// <summary>Reads one inline-choice branch into its local, framing the value if it needs it.</summary>
            private void EmitDecodeInlineMember(InlineChoiceMember m, string indent)
            {
                var child = AsChildPlan(m);
                if (WrapsValue(child))
                    _sb.Append(indent).AppendLine("r.readBits(1)   // value-start");
                _sb.Append(indent).Append(Local(m.FieldName)).Append(" = ").AppendLine(DecodeValueExpr(child));
                if (WrapsValue(child))
                    _sb.Append(indent).AppendLine("r.readBits(1)   // child EE");
            }

            /// <summary>Decode mirror of <see cref="EmitEncodeRepeatingItems"/>.</summary>
            private void EmitDecodeRepeatingItems(ChildPlan p, string list, string indent, string after)
            {
                EmitDecodeItem(p, list, list + "First", indent);   // the event code was its SE
                _sb.Append(indent).AppendLine("while (true) {");
                _sb.Append(indent).AppendLine("    val lc = r.readBits(2)");
                _sb.Append(indent).AppendLine("    if (lc == 1u) break   // element EE (list end)");
                _sb.Append(indent).Append("    require(lc == 0u && ").Append(list).Append(".size < ")
                   .Append(p.ListMax).AppendLine(") { \"invalid repeating-element event code\" }");
                EmitDecodeItem(p, list, list + "Next", indent + "    ");
                _sb.Append(indent).AppendLine("}");
                _sb.Append(indent).AppendLine(after);
            }

            /// <summary>
            /// The item loop of a bounded-repeating child: the first item takes a 1-bit SE, every
            /// following item a 2-bit event code. Mirrors <c>CodecEmitter</c>.
            /// </summary>
            private void EmitEncodeRepeating(ChildPlan c, string list, int min, int max, string indent)
            {
                _sb.Append(indent).Append("require(").Append(list).Append(".size in ").Append(min)
                   .Append("..").Append(max).AppendLine(") { \"list size out of schema range\" }");
                _sb.Append(indent).Append("for (i in ").Append(list).AppendLine(".indices) {");
                _sb.Append(indent).AppendLine("    w.writeBits(0u, if (i == 0) 1 else 2)   // SE(item)");
                EmitEncodeValue(c, list + "[i]", indent + "    ");
                _sb.Append(indent).AppendLine("}");
                EmitEncodeListTerminator(list, max, indent);
            }

            /// <summary>
            /// At maxOccurs=2 a full list has no "another item" production left, so the grammar closes
            /// it with the 1-bit element EE rather than the 2-bit terminator.
            /// </summary>
            private void EmitEncodeListTerminator(string list, int max, string indent)
            {
                if (max == 2)
                {
                    _sb.Append(indent).Append("if (").Append(list)
                       .AppendLine(".size >= 2) w.writeBits(0u, 1)   // element EE (list at max)");
                    _sb.Append(indent).AppendLine("else w.writeBits(1u, 2)   // element EE");
                }
                else
                    _sb.Append(indent).AppendLine("w.writeBits(1u, 2)   // list terminator / element EE");
            }

            /// <summary>
            /// The exclusive end of the optional run starting at <paramref name="i"/>: consecutive
            /// optionals, and — as <c>CodecEmitter</c> does — at most one optional bounded-repeating
            /// particle among them, since an empty list is itself the "absent" encoding.
            /// </summary>
            /// <summary>
            /// Whether a particle opens an optional run. An optional bounded-repeating list does too —
            /// an empty list is itself the "absent" encoding — so it must not be claimed by the
            /// standalone-repeating path first.
            /// </summary>
            private static bool StartsRun(ChildPlan c) =>
                c.Shape == ChildShape.OptionalSingle
                || (c.Shape == ChildShape.BoundedRepeating && c.ListMin == 0);

            /// <summary>The index of an optional list sitting before the end of its run, or -1.</summary>
            private static int MidRunListIndex(IReadOnlyList<ChildPlan> kids, int start, int end)
            {
                for (var p = start; p < end - 1; p++)
                    if (kids[p].Shape == ChildShape.BoundedRepeating)
                        return p;
                return -1;
            }

            /// <summary>
            /// The particles following a mid-run list, checked against what this shape's grammar can
            /// express. Mirrors the constraints <c>CodecEmitter</c> enforces.
            /// </summary>
            private static List<ChildPlan> MidRunSuffix(IReadOnlyList<ChildPlan> kids, int start, int listIdx,
                                                        int end, int childCount)
            {
                if (listIdx != start)
                    throw new NotSupportedException(
                        $"Kotlin back end: particles before the mid-run list '{kids[listIdx].FieldName}' " +
                        "are not supported.");
                if (kids[listIdx].ListMin != 0)
                    throw new NotSupportedException(
                        $"Kotlin back end: the mid-run list '{kids[listIdx].FieldName}' must be optional.");
                if (end != childCount)
                    throw new NotSupportedException(
                        $"Kotlin back end: the mid-run list '{kids[listIdx].FieldName}' must be followed only " +
                        "by particles ending the sequence.");

                var suffix = new List<ChildPlan>();
                for (var p = listIdx + 1; p < end; p++)
                {
                    if (kids[p].Value is ValueEncoding.SubstitutionChoice or ValueEncoding.InlineChoice)
                        throw new NotSupportedException(
                            $"Kotlin back end: suffix particle '{kids[p].FieldName}' after the mid-run list " +
                            $"'{kids[listIdx].FieldName}' must be a plain optional element.");
                    suffix.Add(kids[p]);
                }

                // Selecting a suffix particle writes the element EE and ends the run, so exactly one
                // of them can ever be encoded. With two, the second would be dropped silently.
                if (suffix.Count > 1)
                    throw new NotSupportedException(
                        $"Kotlin back end: the mid-run list '{kids[listIdx].FieldName}' is followed by " +
                        $"{suffix.Count} particles; only one is representable, since choosing one ends the run.");

                return suffix;
            }

            /// <summary>
            /// A run whose optional list is *not* its last particle. cbexigen's grammar for this is
            /// narrower than a normal run and surprising twice over, and <c>CodecEmitter</c> documents
            /// why it is matched rather than corrected: the particles after the list are unreachable
            /// until at least one item has been written, and the list is hard-capped at two items
            /// regardless of its schema maxOccurs. Being byte-exact with the reference encoder is the
            /// point, so this reproduces it.
            /// </summary>
            private void EmitEncodeMidRunList(IReadOnlyList<ChildPlan> kids, int start, int listIdx,
                                              int end, int childCount)
            {
                var suffix      = MidRunSuffix(kids, start, listIdx, end, childCount);
                var list        = kids[listIdx];
                var suffixTotal = suffix.Sum(ProductionCount);
                var prop        = "msg." + Prop(list.FieldName);
                var id          = _run++;
                const string br = "                    ";

                var w0 = BitsFor(1 + 1 + 1);
                var w1 = BitsFor(1 + suffixTotal + 1 + 1);
                var w2 = BitsFor(suffixTotal + 1 + 1);

                // Choosing a suffix particle still needs the element's own closing EE — cbexigen puts a
                // dedicated one-bit-EE state after it, unlike a normal run where the caller appends it.
                var afterSuffix = $"w.writeBits(0u, 1)   // element EE\n{br}    done{id} = true";

                _sb.Append("        require(").Append(prop).Append(".size <= 2) { \"")
                   .Append(KStr(list.FieldName))
                   .AppendLine(": cbV2G's grammar for this position caps this list at 2 items.\" }");
                _sb.Append("        var st").Append(id).AppendLine(" = 0");
                _sb.Append("        var done").Append(id).AppendLine(" = false");
                _sb.Append("        while (!done").Append(id).AppendLine(") {");
                _sb.Append("            when (st").Append(id).AppendLine(") {");

                // State 0: nothing written yet — start the first item, or close the element.
                _sb.AppendLine("                0 -> {");
                _sb.Append(br).Append("if (").Append(prop).AppendLine(".isNotEmpty()) {");
                _sb.Append(br).Append("    w.writeBits(0u, ").Append(w0).Append(")   // ").AppendLine(list.FieldName);
                EmitEncodeValue(list, prop + "[0]", br + "    ");
                _sb.Append(br).Append("    st").Append(id).AppendLine(" = 1");
                _sb.Append(br).AppendLine("} else {");
                // The suffix has no event code in this state, so a caller that set one would
                // otherwise have it dropped without a word. Refuse instead.
                foreach (var s in suffix)
                {
                    _sb.Append(br).Append("    require(msg.").Append(Prop(s.FieldName)).Append(" == null) { \"")
                       .Append(KStr(s.FieldName)).Append(" cannot be encoded while ")
                       .Append(KStr(list.FieldName))
                       .AppendLine(" is empty: cbV2G's grammar for this position only reaches it after at " +
                                   "least one list item.\" }");
                }
                _sb.Append(br).Append("    w.writeBits(1u, ").Append(w0).AppendLine(")   // element EE");
                _sb.Append(br).Append("    done").Append(id).AppendLine(" = true");
                _sb.Append(br).AppendLine("}");
                _sb.AppendLine("                }");

                // State 1: one item written — a second item, a suffix particle, or the element EE.
                _sb.AppendLine("                1 -> {");
                _sb.Append(br).Append("if (").Append(prop).AppendLine(".size > 1) {");
                _sb.Append(br).Append("    w.writeBits(0u, ").Append(w1).Append(")   // ").AppendLine(list.FieldName);
                EmitEncodeValue(list, prop + "[1]", br + "    ");
                _sb.Append(br).Append("    st").Append(id).AppendLine(" = 2");
                EmitEncodeMidRunTail(suffix, 1, w1, id, br, afterSuffix, first: false);
                _sb.AppendLine("                }");

                // State 2: the list is capped — only a suffix particle or the element EE remain.
                _sb.AppendLine("                2 -> {");
                EmitEncodeMidRunTail(suffix, 0, w2, id, br, afterSuffix, first: true);
                _sb.AppendLine("                }");

                _sb.AppendLine("            }");
                _sb.AppendLine("        }");
            }

            /// <summary>The suffix branches plus the closing element EE of one mid-run-list state.</summary>
            private void EmitEncodeMidRunTail(List<ChildPlan> suffix, int code, int width, int id,
                                              string br, string afterSuffix, bool first)
            {
                foreach (var s in suffix)
                    code = EmitEncodeRunParticle(s, code, width, ref first, br, afterSuffix);

                var tail = first ? br : br + "    ";
                if (!first)
                    _sb.Append(br).AppendLine("} else {");
                _sb.Append(tail).Append("w.writeBits(").Append(code).Append("u, ").Append(width)
                   .AppendLine(")   // element EE");
                _sb.Append(tail).Append("done").Append(id).AppendLine(" = true");
                if (!first)
                    _sb.Append(br).AppendLine("}");
            }

            private static int RunEnd(IReadOnlyList<ChildPlan> kids, int i)
            {
                var j = i;
                while (j < kids.Count && kids[j].Shape == ChildShape.OptionalSingle) j++;
                if (j < kids.Count && kids[j].Shape == ChildShape.BoundedRepeating && kids[j].ListMin == 0)
                {
                    j++;
                    while (j < kids.Count && kids[j].Shape == ChildShape.OptionalSingle) j++;
                }
                return j;
            }

            /// <summary>
            /// Only a plain required child may close an optional run so far; anything else would
            /// need extra productions in the run's tail code.
            /// </summary>
            private static void RejectRunTerminator(ChildPlan? term, string owner)
            {
                if (term is null)
                    return;
                if (term.Shape == ChildShape.BoundedRepeating && term.ListMin == 0)
                    throw new NotSupportedException(
                        $"Kotlin back end: '{owner}.{term.FieldName}' terminates an optional run but is an " +
                        "optional list; only a required one is modelled here.");
                if (term.Shape is not (ChildShape.RequiredSingle or ChildShape.BoundedRepeating))
                    throw new NotSupportedException(
                        $"Kotlin back end: '{owner}.{term.FieldName}' terminates an optional run but is {term.Shape}.");
            }

            /// <summary>
            /// A run of optional children spanning [start, end). It is closed either by the element
            /// EE (<paramref name="term"/> is null) or by the required child at <paramref name="end"/>,
            /// which then occupies the run's highest event code. At each state the code width covers
            /// the still-possible productions plus the non-strict phantom, exactly as
            /// <c>CodecEmitter</c> computes it.
            /// </summary>
            private void EmitEncodeOptionalRun(IReadOnlyList<ChildPlan> kids, int start, int end, ChildPlan? term)
            {
                var m           = end - start;
                var id          = _run++;
                var trailingAny = TrailingAny(kids, start, end, term);
                const string ind = "                    ";

                _sb.Append("        var st").Append(id).AppendLine(" = 0");
                _sb.Append("        var done").Append(id).AppendLine(" = false");
                _sb.Append("        while (!done").Append(id).AppendLine(") {");
                _sb.Append("            when (st").Append(id).AppendLine(") {");

                // State k: the cursor sits at particle start+k; any particle from there on may be
                // the next one present, so each state offers all of them.
                for (var k = 0; k <= m; k++)
                {
                    var totalProd = term is null ? 1                        // the element EE
                                                 : ProductionCount(term);   // or the required child
                    for (var i = k; i < m; i++) totalProd += ProductionCount(kids[start + i]);
                    var width = BitsFor(totalProd + 1);                  // + the non-strict phantom

                    _sb.Append("                ").Append(k).AppendLine(" -> {");

                    var code   = 0;
                    var first  = true;
                    var optEnd = trailingAny ? m - 1 : m;   // the ANY is handled with the tail

                    EmitEncodeRunSubLocals(kids, start, k, optEnd, term, ind);

                    for (var i = k; i < optEnd; i++)
                    {
                        // A list consumes the rest of the element: its own list-end EE closes it.
                        var after = kids[start + i].Shape == ChildShape.BoundedRepeating
                                        ? $"done{id} = true"
                                        : $"st{id} = {i + 1}";
                        code = EmitEncodeRunParticle(kids[start + i], code, width, ref first, ind, after);
                    }

                    var eeCode = code;
                    if (trailingAny && k < m)
                    {
                        // cbexigen ordering: [normal optionals], the generic-wildcard slot (reserved,
                        // never emitted), the element EE, then the typed ANY element. Selecting ANY
                        // advances to the EE-only state.
                        EmitEncodeRunParticle(kids[end - 1], code + 2, width, ref first, ind, $"st{id} = {m}");
                        eeCode = code + 1;
                    }

                    // A required repeating terminator: its first item takes the run's tail code, and
                    // its own list-end EE closes the element.
                    if (term?.Shape == ChildShape.BoundedRepeating)
                    {
                        var repTail = first ? ind : ind + "    ";
                        if (!first)
                            _sb.Append(ind).AppendLine("} else {");
                        _sb.Append(repTail).Append("require(msg.").Append(Prop(term.FieldName))
                           .Append(".size in 1..").Append(term.ListMax)
                           .AppendLine(") { \"list size out of schema range\" }");
                        EmitEncodeRepeatingItems(term, eeCode, width, repTail, $"done{id} = true");
                        if (!first)
                            _sb.Append(ind).AppendLine("}");
                        _sb.AppendLine("                }");
                        continue;
                    }

                    // A substitution or inline-choice terminator has one production per member, so it
                    // extends the chain rather than closing it; the chain then ends in a throw,
                    // because a required child must be set.
                    if (term?.Value is ValueEncoding.SubstitutionChoice or ValueEncoding.InlineChoice)
                    {
                        EmitEncodeRunParticle(term, eeCode, width, ref first, ind, $"done{id} = true");
                        _sb.Append(ind).AppendLine("} else {");
                        _sb.Append(ind).Append("    throw IllegalArgumentException(\"no value set for ")
                           .Append(KStr(term.FieldName)).AppendLine("\")");
                        _sb.Append(ind).AppendLine("}");
                        _sb.AppendLine("                }");
                        continue;
                    }

                    // The highest remaining code closes the run: the element EE, or the required
                    // child — whose content then follows inline.
                    var tail = first ? ind : ind + "    ";
                    if (!first)
                        _sb.Append(ind).AppendLine("} else {");

                    _sb.Append(tail).Append("w.writeBits(").Append(eeCode).Append("u, ").Append(width);
                    if (term is null)
                        _sb.AppendLine(")   // element EE");
                    else
                    {
                        _sb.Append(")   // SE(").Append(term.FieldName).AppendLine(")");
                        EmitEncodeValue(term, "msg." + Prop(term.FieldName), tail);
                    }
                    _sb.Append(tail).Append("done").Append(id).AppendLine(" = true");

                    if (!first)
                        _sb.Append(ind).AppendLine("}");

                    _sb.AppendLine("                }");
                }

                _sb.AppendLine("            }");
                _sb.AppendLine("        }");
            }

            /// <summary>
            /// One particle of an optional run: an optional element (one production) or an optional
            /// substitution reference (one production per member, the abstract head reserving a code
            /// slot without a branch). Emits an <c>if</c> / <c>} else if</c> link of the state's
            /// chain and returns the next free event code.
            /// </summary>
            private int EmitEncodeRunParticle(ChildPlan p, int code, int width, ref bool first,
                                              string indent, string after)
            {
                var prop = "msg." + Prop(p.FieldName);

                if (p.Shape == ChildShape.BoundedRepeating)
                {
                    // An empty list means this optional element is absent.
                    _sb.Append(indent).Append(first ? "if (" : "} else if (").Append(prop).AppendLine(".isNotEmpty()) {");
                    _sb.Append(indent).Append("    require(").Append(prop).Append(".size <= ").Append(p.ListMax)
                       .AppendLine(") { \"list size out of schema range\" }");
                    EmitEncodeRepeatingItems(p, code, width, indent + "    ", after);
                    first = false;
                    return code + 1;
                }

                if (p.Value is ValueEncoding.InlineChoice ic)
                {
                    // Each branch is its own nullable field, so each takes its own event code and
                    // extends the chain — there is nothing to smart-cast.
                    foreach (var m in ic.Members)
                    {
                        var f = "msg." + Prop(m.FieldName);
                        _sb.Append(indent).Append(first ? "if (" : "} else if (").Append(f).AppendLine(" != null) {");
                        _sb.Append(indent).Append("    w.writeBits(").Append(code).Append("u, ").Append(width)
                           .Append(")   // ").AppendLine(m.ElementName);
                        EmitEncodeValue(AsChildPlan(m), f + "!!", indent + "    ");
                        _sb.Append(indent).Append("    ").AppendLine(after);
                        first = false;
                        code++;
                    }
                    return code;
                }

                if (p.Value is ValueEncoding.SubstitutionChoice sc)
                {
                    // Hoisted into a local because `is` cannot smart-cast an `open`/`override` property,
                    // which generated base types declare.
                    // The local is declared by EmitEncodeRunSubLocals before the chain opens; every
                    // branch must use it, because a `val` cannot be introduced mid-chain and `is`
                    // cannot smart-cast the `open`/`override` property a generated base type declares.
                    var local    = SubLocal(p);
                    var baseCode = code;
                    var ordered  = sc.Members
                                     .Select((mm, i) => (Member: mm, Wire: baseCode + i))
                                     .Where(x => !x.Member.IsAbstractHead)
                                     .OrderByDescending(x => InheritanceDepth(x.Member.TypeName));

                    foreach (var (mbr, wire) in ordered)
                    {
                        _sb.Append(indent).Append(first ? "if (" : "} else if (")
                           .Append(local).Append(" is ").Append(mbr.TypeName).AppendLine(") {");
                        EmitSubstitutionMemberGuard(p, mbr.TypeName, local, indent + "    ");
                        _sb.Append(indent).Append("    w.writeBits(").Append(wire).Append("u, ").Append(width)
                           .Append(")   // ").AppendLine(mbr.ElementName);
                        _sb.Append(indent).Append("    encode").Append(mbr.TypeName).Append("(w, ")
                           .Append(local).AppendLine(")");
                        _sb.Append(indent).Append("    ").AppendLine(after);
                        first = false;
                    }
                    return code + sc.Members.Count;
                }

                _sb.Append(indent).Append(first ? "if (" : "} else if (").Append(prop).AppendLine(" != null) {");
                _sb.Append(indent).Append("    w.writeBits(").Append(code).Append("u, ").Append(width)
                   .Append(")   // ").AppendLine(p.FieldName);
                EmitEncodeValue(p, prop + "!!", indent + "    ");
                _sb.Append(indent).Append("    ").AppendLine(after);
                first = false;
                return code + 1;
            }

            /// <summary>
            /// A child's flattened constructor parameters. An inline xs:choice contributes one
            /// nullable parameter per branch — cbexigen models it as N sibling fields with only one
            /// set, not as a single polymorphic field the way a substitution group is modelled.
            /// </summary>
            private static List<(string Name, string Type)> ChildParams(ChildPlan c) =>
                c.Value is ValueEncoding.InlineChoice ic
                    ? ic.Members.Select(m => (Prop(m.FieldName), Type(m.Type) + "?")).ToList()
                    : [(Prop(c.FieldName), DeclType(c))];

            /// <summary>An inline-choice branch as a throwaway child, so it can go through the
            /// ordinary value emitters — which only ever read <c>Value</c> and <c>Type</c>.</summary>
            private static ChildPlan AsChildPlan(InlineChoiceMember m) =>
                new(m.FieldName, m.Type, m.IsValueType, ChildShape.RequiredSingle, m.Value);

            /// <summary>The read-once local a substitution particle dispatches on.</summary>
            private static string SubLocal(ChildPlan p) => "sub_" + Camel(p.FieldName);

            /// <summary>
            /// Declares the dispatch local of every substitution particle a state's if/else-if chain
            /// will touch. They have to exist before the chain opens — Kotlin has no way to introduce
            /// a `val` between two `else if` links.
            /// </summary>
            private void EmitEncodeRunSubLocals(IReadOnlyList<ChildPlan> kids, int start, int from,
                                                int optEnd, ChildPlan? term, string indent)
            {
                for (var i = from; i < optEnd; i++)
                    DeclareSubLocal(kids[start + i], indent);
                DeclareSubLocal(term, indent);
            }

            private void DeclareSubLocal(ChildPlan? p, string indent)
            {
                if (p?.Value is not ValueEncoding.SubstitutionChoice)
                    return;
                _sb.Append(indent).Append("val ").Append(SubLocal(p)).Append(" = msg.")
                   .AppendLine(Prop(p.FieldName));
            }

            /// <summary>
            /// How many grammar productions a run particle occupies: a substitution reference
            /// contributes one per member (the abstract head included), an xs:any wildcard two (the
            /// generic wildcard event and the typed element), everything else one.
            /// </summary>
            private static int ProductionCount(ChildPlan c) =>
                c.Value is ValueEncoding.SubstitutionChoice sc ? sc.Members.Count
                : c.Value is ValueEncoding.InlineChoice ic ? ic.Members.Count
                : c.IsWildcardAny ? 2
                : 1;

            /// <summary>
            /// Whether the run ends in an xs:any wildcard, and a guard that it appears nowhere else.
            /// cbexigen only splits a wildcard into its two productions as the last particle of an
            /// EE-terminated run (verified there against SignatureMethodType / DigestMethodType);
            /// anywhere else the code layout would differ and this back end does not model it.
            /// </summary>
            private static bool TrailingAny(IReadOnlyList<ChildPlan> kids, int start, int end, ChildPlan? term)
            {
                for (var p = start; p < end; p++)
                    if (kids[p].IsWildcardAny && (p != end - 1 || term is not null))
                        throw new NotSupportedException(
                            $"Kotlin back end: xs:any wildcard '{kids[p].FieldName}' must be the last child " +
                            "of an EE-terminated sequence.");

                return term is null && end > start && kids[end - 1].IsWildcardAny;
            }

            /// <summary>
            /// Substitution dispatch. Branches are emitted most-derived-first because Kotlin's
            /// `is` checks, like C#'s type patterns, also match subtypes — members can extend each
            /// other, not just the common head. The wire code stays each member's own (alphabetical,
            /// cbV2G-verified) position, independent of emission order.
            /// </summary>
            private void EmitEncodeSubstitution(ChildPlan c, ValueEncoding.SubstitutionChoice sc)
            {
                var prop = "msg." + Prop(c.FieldName);
                var ordered = sc.Members
                                .Select((m, i) => (Member: m, Code: i))
                                .Where(x => !x.Member.IsAbstractHead)
                                .OrderByDescending(x => InheritanceDepth(x.Member.TypeName))
                                .ToList();

                // When the head of the group is itself concrete it is a member, and being the least
                // derived it sorts last — so the final branch tests the property against its own
                // declared type. That `is` can never be false, which makes the `else -> throw`
                // behind it unreachable: the two are one branch written twice. Emitting the last
                // one as `else` is the same code with the dead arm dropped, and stops Kotlin
                // reporting a check it can see through ("Check for instance is always 'true'").
                //
                // With an abstract head the head is not a member, the last branch is some derived
                // type, and the throw is genuinely reachable — then nothing is collapsed.
                var headIsLast = c.Shape == ChildShape.RequiredSingle
                                 && ordered.Count > 1
                                 && ordered[ordered.Count - 1].Member.TypeName == Type(c.Type);

                _sb.Append("        when (val v = ").Append(prop).AppendLine(") {");
                for (var i = 0; i < ordered.Count; i++)
                {
                    var (m, code) = ordered[i];

                    if (headIsLast && i == ordered.Count - 1)
                        _sb.AppendLine("            else -> {");
                    else
                        _sb.Append("            is ").Append(m.TypeName).AppendLine(" -> {");

                    EmitSubstitutionMemberGuard(c, m.TypeName, "v", "                ");
                    _sb.Append("                w.writeBits(").Append(code).Append("u, ").Append(sc.BitWidth)
                       .Append(")   // ").AppendLine(m.ElementName);
                    _sb.Append("                encode").Append(m.TypeName).AppendLine("(w, v)");
                    _sb.AppendLine("            }");
                }
                if (!headIsLast)
                    _sb.Append("            else -> throw IllegalArgumentException(\"unsupported substitution member for ")
                       .Append(KStr(c.FieldName)).AppendLine("\")");
                _sb.AppendLine("        }");
            }

            /// <summary>
            /// Requires the value to be *exactly* the member type its branch selected, not merely
            /// assignable to it.
            /// </summary>
            /// <remarks>
            /// <para>
            /// An `is` test matches subtypes, which is what makes the most-derived-first ordering
            /// necessary in the first place. Every type the schema set derives from a member is
            /// itself a member, so within the generated types the branches partition the space
            /// exactly — but nothing stops application code subclassing a generated type, and the
            /// generated types are `open` precisely because they get extended. Such a value would
            /// take its nearest ancestor's branch and be written with that member's event code and
            /// that member's encoder, quietly encoding something the caller did not ask for.
            /// </para>
            /// <para>
            /// The equivalent shape in an optional run is worse still: it can match no branch at
            /// all, and the field is then dropped from the message without a word.
            /// </para>
            /// </remarks>
            private void EmitSubstitutionMemberGuard(ChildPlan c, string typeName, string value, string indent)
            {
                // A leaf class is final, so `is` on it already means "exactly this type" and the
                // check would be dead code. Only the classes something extends — and abstract ones —
                // are emitted `open`, and only those can be derived from by a consumer.
                var extensible = _baseNames.Contains(typeName)
                                 || (plan.ComplexTypes.TryGetValue(typeName, out var sp) && sp.IsAbstract);
                if (!extensible)
                    return;

                _sb.Append(indent).Append("require(").Append(value).Append("::class == ").Append(typeName)
                   .Append("::class) { \"").Append(KStr(c.FieldName)).Append(": ${").Append(value)
                   .Append("::class.simpleName} is not a substitution member\" }").AppendLine();
            }

            /// <summary>How many base links separate a type from its root; 0 for a type with no base.</summary>
            private int InheritanceDepth(string typeName)
            {
                var depth = 0;
                var current = typeName;
                while (plan.ComplexTypes.TryGetValue(current, out var sp) && sp.BaseRecordName is not null)
                {
                    depth++;
                    current = sp.BaseRecordName;
                }
                return depth;
            }

            private void EmitEncodeValue(ChildPlan c, string accessor, string indent, bool inList = false)
            {
                if (c.Value is ValueEncoding.OpaqueElement oe)
                {
                    // Only reached with a present instance; absence is handled by the optional run.
                    _sb.Append(indent).Append("throw UnsupportedOperationException(\"Encoding a present ")
                       .Append(KStr(oe.TypeName)).AppendLine(" (XMLDSig) is not implemented in the Kotlin back end.\")");
                    return;
                }
                if (c.Value is ValueEncoding.ComplexRef cr)
                {
                    _sb.Append(indent).Append("encode").Append(cr.TypeName).Append("(w, ").Append(accessor).AppendLine(")");
                    return;
                }
                if (c.Value is ValueEncoding.AttributeValue)
                {
                    // Bare string — the run's event code was the AT event itself.
                    _sb.Append(indent).Append("ExiPrimitives.writeStringValue(w, ").Append(accessor).AppendLine(")");
                    return;
                }

                _sb.Append(indent).AppendLine("w.writeBits(0u, 1)   // value-start");
                EmitEncodeBareValue(c, accessor, indent);
                _sb.Append(indent).AppendLine("w.writeBits(0u, 1)   // child EE");
            }

            /// <summary>
            /// A value with no framing around it. Used on its own for an AT value and for
            /// xs:simpleContent, where the preceding event code already selected the production.
            /// </summary>
            private void EmitEncodeBareValue(ChildPlan c, string accessor, string indent)
            {
                switch (c.Value)
                {
                    case ValueEncoding.StringValue:
                        _sb.Append(indent).Append("ExiPrimitives.writeStringValue(w, ").Append(accessor).AppendLine(")");
                        break;
                    case ValueEncoding.UnsignedInt:
                        _sb.Append(indent).Append("ExiPrimitives.writeUnsignedInteger(w, ").Append(accessor).AppendLine(".toULong())");
                        break;
                    case ValueEncoding.SignedInt:
                        _sb.Append(indent).Append("ExiPrimitives.writeSignedInteger(w, ").Append(accessor).AppendLine(".toLong())");
                        break;
                    case ValueEncoding.Binary:
                        _sb.Append(indent).Append("ExiPrimitives.writeBinary(w, ").Append(accessor).AppendLine(")");
                        break;
                    case ValueEncoding.EnumIndex ei:
                        _sb.Append(indent).Append("w.writeBits(").Append(accessor).Append(".ordinal.toUInt(), ")
                           .Append(ei.BitWidth).AppendLine(")");
                        break;
                    case ValueEncoding.NBitUnsigned nb when IsBool(c):
                        // xs:boolean is a 1-bit n-bit unsigned, and Kotlin's Boolean has no numeric
                        // conversion — same special case as in CodecEmitter.
                        _sb.Append(indent).Append("w.writeBits(if (").Append(accessor).Append(") 1u else 0u, ")
                           .Append(nb.BitWidth).AppendLine(")");
                        break;
                    case ValueEncoding.NBitUnsigned nb:
                        var expr = nb.Bias == 0
                                       ? accessor + ".toLong()"
                                       : "(" + accessor + ".toLong() - " + nb.Bias + "L)";
                        _sb.Append(indent).Append("w.writeBits(").Append(expr).Append(".toUInt(), ")
                           .Append(nb.BitWidth).AppendLine(")");
                        break;
                    default:
                        throw new NotSupportedException($"Kotlin back end: value encoding {c.Value.GetType().Name}.");
                }
            }

            private void EmitDecode(SequencePlan sp, string name)
            {
                _sb.Append("    internal fun decode").Append(name).Append("(r: BitReader): ")
                   .Append(name).AppendLine(" {");

                var ctor = new List<string>();

                if (RequiredAttr(sp) is { } req)
                {
                    _sb.AppendLine("        r.readBits(1)   // AT(required attribute)");
                    _sb.Append("        val ").Append(Local(req.FieldName))
                       .Append(" = ExiPrimitives.readStringValue(r, \"").Append(KStr(req.FieldName))
                       .AppendLine("\")");
                    ctor.Add(Local(req.FieldName));
                }

                if (sp.SimpleContent is not null)
                {
                    if (HasOptionalAttributes(sp))
                        EmitDecodeSimpleContentOptionalAttrs(sp, ctor);
                    else
                    {
                        _sb.AppendLine("        r.readBits(1)   // CONTENT event");
                        _sb.Append("        val ").Append(Local(SimpleContentField)).Append(" = ")
                           .AppendLine(DecodeValueExpr(SimpleContentChild(sp)));
                        ctor.Add(Local(SimpleContentField));
                    }
                    _sb.AppendLine("        r.readBits(1)   // element EE");
                    _sb.Append("        return ").Append(name).Append("(").Append(string.Join(", ", ctor)).AppendLine(")");
                    _sb.AppendLine("    }");
                    _sb.AppendLine();
                    return;
                }

                if (sp.IsChoice)
                {
                    EmitDecodeChoice(sp, ctor);
                    _sb.Append("        return ").Append(name).Append("(").Append(string.Join(", ", ctor)).AppendLine(")");
                    _sb.AppendLine("    }");
                    _sb.AppendLine();
                    return;
                }

                var kids = WithOptionalAttributes(sp);

                for (var i = 0; i < kids.Count;)
                {
                    var c = kids[i];
                    if (c.Shape == ChildShape.BoundedRepeating && (kids.Count == 1 || c.ListMin > 0))
                    {
                        if (kids.Count == 1)
                        {
                            EmitDecodeRepeating(c, "list", ListBounds(c, sp).Max, "        ");
                            ctor.Add("list");
                            i++;
                        }
                        else if (c.ListMin > 0 && i == kids.Count - 1)
                        {
                            EmitDecodeRepeating(c, ListLocal(c), c.ListMax, "        ");
                            ctor.Add(ListLocal(c));
                            i++;
                        }
                        else
                        {
                            var tailD = kids[i + 1];
                            EmitDecodeRepeatingWithTail(c, tailD, ctor, "        ");
                            i += 2;
                            if (i == kids.Count && tailD.Shape == ChildShape.RequiredSingle)
                                _sb.AppendLine("        r.readBits(1)   // element EE");
                        }
                        continue;
                    }
                    if (StartsRun(c))
                    {
                        var e    = RunEnd(kids, i);
                        var term = e < kids.Count ? kids[e] : null;
                        RejectRunTerminator(term, name);

                        var midD = MidRunListIndex(kids, i, e);
                        if (midD >= 0)
                        {
                            EmitDecodeMidRunList(kids, i, midD, e, kids.Count, ctor);
                            i = kids.Count;
                            continue;
                        }

                        EmitDecodeOptionalRun(kids, i, e, term, ctor);

                        i = term is null ? kids.Count : e + 1;
                        if (term is not null && term.Shape != ChildShape.BoundedRepeating && i == kids.Count)
                            _sb.AppendLine("        r.readBits(1)   // element EE");
                        continue;
                    }

                    // An inline choice declares one local per branch and returns them all.
                    if (c.Value is ValueEncoding.InlineChoice inl)
                    {
                        EmitDecodeInlineChoiceStandalone(inl, ctor);
                        i++;
                        if (i == kids.Count)
                            _sb.AppendLine("        r.readBits(1)   // element EE");
                        continue;
                    }

                    var v = Local(c.FieldName);
                    if (c.Value is ValueEncoding.SubstitutionChoice sub)
                    {
                        _sb.Append("        val ").Append(v).Append(": ").Append(Type(c.Type))
                           .Append(" = when (r.readBits(").Append(sub.BitWidth).AppendLine(")) {");
                        for (var k = 0; k < sub.Members.Count; k++)
                        {
                            var m = sub.Members[k];
                            if (m.IsAbstractHead)
                                _sb.Append("            ").Append(k)
                                   .AppendLine("u -> throw IllegalArgumentException(\"abstract substitution head cannot be decoded\")");
                            else
                                _sb.Append("            ").Append(k).Append("u -> decode")
                                   .Append(m.TypeName).AppendLine("(r)");
                        }
                        _sb.AppendLine("            else -> throw IllegalArgumentException(\"unknown substitution index\")");
                        _sb.AppendLine("        }");
                        ctor.Add(v);
                        i++;
                        if (i == kids.Count)
                            _sb.AppendLine("        r.readBits(1)   // element EE");
                        continue;
                    }

                    _sb.AppendLine("        r.readBits(1)   // SE");
                    if (!WrapsValue(c))
                    {
                        _sb.Append("        val ").Append(v).Append(" = ").AppendLine(DecodeValueExpr(c));
                    }
                    else
                    {
                        _sb.AppendLine("        r.readBits(1)   // value-start");
                        _sb.Append("        val ").Append(v).Append(" = ").AppendLine(DecodeValueExpr(c));
                        _sb.AppendLine("        r.readBits(1)   // child EE");
                    }
                    ctor.Add(v);
                    i++;
                    if (i == kids.Count)
                        _sb.AppendLine("        r.readBits(1)   // element EE");
                }

                if (kids.Count == 0)
                    _sb.AppendLine("        r.readBits(1)   // element EE");

                _sb.Append("        return ").Append(name).Append("(").Append(string.Join(", ", ctor)).AppendLine(")");
                _sb.AppendLine("    }");
                _sb.AppendLine();
            }

            /// <summary>Decode mirror of <see cref="EmitEncodeRepeating"/>.</summary>
            private void EmitDecodeRepeating(ChildPlan c, string list, int max, string indent)
            {
                _sb.Append(indent).Append("val ").Append(list).Append(" = ArrayList<")
                   .Append(Type(c.Type)).AppendLine(">()");
                _sb.Append(indent).AppendLine("r.readBits(1)   // SE(item) first");
                EmitDecodeItem(c, list, list + "First", indent);

                _sb.Append(indent).AppendLine("while (true) {");
                if (max == 2)
                    _sb.Append(indent).Append("    if (").Append(list)
                       .AppendLine(".size >= 2) { r.readBits(1); break }   // element EE (list at max)");
                _sb.Append(indent).AppendLine("    val ec = r.readBits(2)");
                _sb.Append(indent).AppendLine("    if (ec == 1u) break   // element EE");
                _sb.Append(indent).Append("    require(ec == 0u && ").Append(list).Append(".size < ")
                   .Append(max).AppendLine(") { \"invalid repeating-element event code\" }");
                EmitDecodeItem(c, list, list + "Next", indent + "    ");
                _sb.Append(indent).AppendLine("}");
            }

            /// <summary>
            /// One list item. A value needing framing has to be read into a local so the value-start
            /// and child EE can bracket it; a self-framing complex item is appended directly.
            /// </summary>
            private void EmitDecodeItem(ChildPlan c, string list, string local, string indent)
            {
                if (!WrapsValue(c))
                {
                    _sb.Append(indent).Append(list).Append(".add(").Append(DecodeValueExpr(c)).AppendLine(")");
                    return;
                }
                _sb.Append(indent).AppendLine("r.readBits(1)   // value-start");
                _sb.Append(indent).Append("val ").Append(local).Append(" = ").AppendLine(DecodeValueExpr(c));
                _sb.Append(indent).AppendLine("r.readBits(1)   // child EE");
                _sb.Append(indent).Append(list).Append(".add(").Append(local).AppendLine(")");
            }

            /// <summary>Decode mirror of <see cref="EmitEncodeSimpleContentOptionalAttrs"/>.</summary>
            private void EmitDecodeSimpleContentOptionalAttrs(SequencePlan sp, List<string> ctor)
            {
                var oa    = sp.Attributes!;
                var n     = oa.Count;
                var id    = _run++;
                var value = SimpleContentChild(sp);
                const string ind = "                        ";

                foreach (var a in oa)
                {
                    _sb.Append("        var ").Append(Local(a.FieldName)).Append(": ").Append(Type(a.Type))
                       .AppendLine("? = null");
                    ctor.Add(Local(a.FieldName));
                }
                _sb.Append("        var ").Append(Local(SimpleContentField)).Append(": ")
                   .Append(Type(sp.SimpleContentType!)).AppendLine("? = null");
                ctor.Add(Local(SimpleContentField) + "!!");

                _sb.Append("        var st").Append(id).AppendLine(" = 0");
                _sb.Append("        var done").Append(id).AppendLine(" = false");
                _sb.Append("        while (!done").Append(id).AppendLine(") {");
                _sb.Append("            when (st").Append(id).AppendLine(") {");

                for (var k = 0; k <= n; k++)
                {
                    var width = BitsFor((n - k + 1) + 1);
                    _sb.Append("                ").Append(k).AppendLine(" -> {");
                    _sb.Append("                    when (r.readBits(").Append(width).AppendLine(")) {");

                    for (var i = k; i < n; i++)
                    {
                        _sb.Append(ind).Append(i - k).Append("u -> {   // AT(").Append(oa[i].FieldName).AppendLine(")");
                        _sb.Append(ind).Append("    ").Append(Local(oa[i].FieldName))
                           .Append(" = ExiPrimitives.readStringValue(r, \"").Append(KStr(oa[i].FieldName))
                           .AppendLine("\")");
                        _sb.Append(ind).Append("    st").Append(id).Append(" = ").Append(i + 1).AppendLine();
                        _sb.Append(ind).AppendLine("}");
                    }

                    _sb.Append(ind).Append(n - k).AppendLine("u -> {   // CONTENT");
                    _sb.Append(ind).Append("    ").Append(Local(SimpleContentField)).Append(" = ")
                       .AppendLine(DecodeValueExpr(value));
                    _sb.Append(ind).Append("    done").Append(id).AppendLine(" = true");
                    _sb.Append(ind).AppendLine("}");
                    _sb.Append(ind).AppendLine("else -> throw IllegalArgumentException(\"invalid simpleContent event code\")");
                    _sb.AppendLine("                    }");
                    _sb.AppendLine("                }");
                }

                _sb.AppendLine("            }");
                _sb.AppendLine("        }");
            }

            /// <summary>Decode mirror of <see cref="EmitEncodeChoice"/>.</summary>
            private void EmitDecodeChoice(SequencePlan sp, List<string> ctor)
            {
                var width = BitsFor(sp.Children.Count + 1);

                foreach (var c in sp.Children)
                {
                    _sb.Append("        var ").Append(Local(c.FieldName)).Append(": ").Append(DeclType(c))
                       .AppendLine(" = null");
                    ctor.Add(Local(c.FieldName));
                }

                _sb.Append("        when (r.readBits(").Append(width).AppendLine(")) {");
                for (var i = 0; i < sp.Children.Count; i++)
                {
                    var c = sp.Children[i];
                    _sb.Append("            ").Append(i).Append("u -> {   // ").AppendLine(c.FieldName);
                    if (WrapsValue(c))
                        _sb.AppendLine("                r.readBits(1)   // value-start");
                    _sb.Append("                ").Append(Local(c.FieldName)).Append(" = ")
                       .AppendLine(DecodeValueExpr(c));
                    if (WrapsValue(c))
                        _sb.AppendLine("                r.readBits(1)   // child EE");
                    _sb.AppendLine("            }");
                }
                _sb.AppendLine("            else -> throw IllegalArgumentException(\"unknown choice event code\")");
                _sb.AppendLine("        }");
                _sb.AppendLine("        r.readBits(1)   // element EE");
            }

            private void EmitDecodeOptionalRun(IReadOnlyList<ChildPlan> kids, int start, int end,
                                               ChildPlan? term, List<string> ctor)
            {
                var id          = _run++;
                var trailingAny = TrailingAny(kids, start, end, term);

                for (var s = start; s < end; s++)
                    DeclareRunLocal(kids[s], ctor);

                // The terminator is required, but it is only assigned inside the state machine, so a
                // non-list one has to be declared nullable and unwrapped at the constructor call.
                if (term is not null)
                {
                    if (term.Shape == ChildShape.BoundedRepeating
                        || term.Value is ValueEncoding.InlineChoice)
                        DeclareRunLocal(term, ctor);
                    else
                    {
                        _sb.Append("        var ").Append(Local(term.FieldName)).Append(": ").Append(Type(term.Type))
                           .AppendLine("? = null");
                        ctor.Add(Local(term.FieldName) + "!!");
                    }
                }

                _sb.Append("        var st").Append(id).AppendLine(" = 0");
                _sb.Append("        var done").Append(id).AppendLine(" = false");
                _sb.Append("        while (!done").Append(id).AppendLine(") {");
                _sb.Append("            when (st").Append(id).AppendLine(") {");

                var m = end - start;
                for (var k = 0; k <= m; k++)
                {
                    var totalProd = term is null ? 1                        // the element EE
                                                 : ProductionCount(term);   // or the required child
                    for (var i = k; i < m; i++) totalProd += ProductionCount(kids[start + i]);
                    var width = BitsFor(totalProd + 1);                  // + the non-strict phantom

                    _sb.Append("                ").Append(k).AppendLine(" -> {");
                    _sb.Append("                    when (r.readBits(").Append(width).AppendLine(")) {");

                    const string ind = "                        ";
                    var code   = 0;
                    var optEnd = trailingAny ? m - 1 : m;   // the ANY is handled with the tail

                    for (var i = k; i < optEnd; i++)
                    {
                        var c     = kids[start + i];
                        var field = Local(c.FieldName);

                        if (c.Shape == ChildShape.BoundedRepeating)
                        {
                            // A list consumes the rest of the element; its list-end EE closes it.
                            _sb.Append(ind).Append(code).Append("u -> {   // ").AppendLine(c.FieldName);
                            EmitDecodeRepeatingItems(c, ListLocal(c), ind + "    ", $"done{id} = true");
                            _sb.Append(ind).AppendLine("}");
                            code++;
                            continue;
                        }

                        if (c.Value is ValueEncoding.InlineChoice cic)
                        {
                            for (var j = 0; j < cic.Members.Count; j++)
                            {
                                var mbr = cic.Members[j];
                                _sb.Append(ind).Append(code + j).Append("u -> {   // ").AppendLine(mbr.ElementName);
                                EmitDecodeInlineMember(mbr, ind + "    ");
                                _sb.Append(ind).Append("    st").Append(id).Append(" = ").Append(i + 1).AppendLine();
                                _sb.Append(ind).AppendLine("}");
                            }
                            code += cic.Members.Count;
                            continue;
                        }

                        if (c.Value is ValueEncoding.SubstitutionChoice sc)
                        {
                            for (var j = 0; j < sc.Members.Count; j++)
                            {
                                var mbr = sc.Members[j];
                                if (mbr.IsAbstractHead)
                                {
                                    _sb.Append(ind).Append(code + j)
                                       .AppendLine("u -> throw IllegalArgumentException(\"abstract substitution head cannot be decoded\")");
                                    continue;
                                }
                                _sb.Append(ind).Append(code + j).AppendLine("u -> {");
                                _sb.Append(ind).Append("    ").Append(field).Append(" = decode")
                                   .Append(mbr.TypeName).AppendLine("(r)");
                                _sb.Append(ind).Append("    st").Append(id).Append(" = ").Append(i + 1).AppendLine();
                                _sb.Append(ind).AppendLine("}");
                            }
                            code += sc.Members.Count;
                            continue;
                        }

                        _sb.Append(ind).Append(code).AppendLine("u -> {");
                        if (WrapsValue(c))
                            _sb.Append(ind).AppendLine("    r.readBits(1)   // value-start");
                        _sb.Append(ind).Append("    ").Append(field).Append(" = ").AppendLine(DecodeValueExpr(c));
                        if (WrapsValue(c))
                            _sb.Append(ind).AppendLine("    r.readBits(1)   // child EE");
                        _sb.Append(ind).Append("    st").Append(id).Append(" = ").Append(i + 1).AppendLine();
                        _sb.Append(ind).AppendLine("}");
                        code++;
                    }

                    if (trailingAny && k < m)
                    {
                        // `code` is the generic-wildcard slot: no branch, so it falls through to the
                        // error case — a generic wildcard event is not modelled. The element EE takes
                        // code+1, the typed ANY element code+2.
                        var any = kids[end - 1];
                        _sb.Append(ind).Append(code + 1).Append("u -> done").Append(id).AppendLine(" = true   // element EE");
                        _sb.Append(ind).Append(code + 2).Append("u -> {   // ").AppendLine(any.FieldName);
                        if (WrapsValue(any))
                            _sb.Append(ind).AppendLine("    r.readBits(1)   // value-start");
                        _sb.Append(ind).Append("    ").Append(Local(any.FieldName)).Append(" = ")
                           .AppendLine(DecodeValueExpr(any));
                        if (WrapsValue(any))
                            _sb.Append(ind).AppendLine("    r.readBits(1)   // child EE");
                        _sb.Append(ind).Append("    st").Append(id).Append(" = ").Append(m).AppendLine();
                        _sb.Append(ind).AppendLine("}");
                    }
                    else if (term is null)
                        _sb.Append(ind).Append(code).Append("u -> done").Append(id).AppendLine(" = true   // element EE");
                    else if (term.Shape == ChildShape.BoundedRepeating)
                    {
                        _sb.Append(ind).Append(code).Append("u -> {   // ").AppendLine(term.FieldName);
                        EmitDecodeRepeatingItems(term, ListLocal(term), ind + "    ", $"done{id} = true");
                        _sb.Append(ind).AppendLine("}");
                    }
                    else if (term.Value is ValueEncoding.InlineChoice tic)
                    {
                        for (var j = 0; j < tic.Members.Count; j++)
                        {
                            var mbr = tic.Members[j];
                            _sb.Append(ind).Append(code + j).Append("u -> {   // ").AppendLine(mbr.ElementName);
                            EmitDecodeInlineMember(mbr, ind + "    ");
                            _sb.Append(ind).Append("    done").Append(id).AppendLine(" = true");
                            _sb.Append(ind).AppendLine("}");
                        }
                    }
                    else if (term.Value is ValueEncoding.SubstitutionChoice tsc)
                    {
                        // One case per member, at the run's highest codes.
                        var field = Local(term.FieldName);
                        for (var j = 0; j < tsc.Members.Count; j++)
                        {
                            var mbr = tsc.Members[j];
                            if (mbr.IsAbstractHead)
                            {
                                _sb.Append(ind).Append(code + j)
                                   .AppendLine("u -> throw IllegalArgumentException(\"abstract substitution head cannot be decoded\")");
                                continue;
                            }
                            _sb.Append(ind).Append(code + j).Append("u -> {   // ").AppendLine(mbr.ElementName);
                            _sb.Append(ind).Append("    ").Append(field).Append(" = decode")
                               .Append(mbr.TypeName).AppendLine("(r)");
                            _sb.Append(ind).Append("    done").Append(id).AppendLine(" = true");
                            _sb.Append(ind).AppendLine("}");
                        }
                    }
                    else
                    {
                        var field = Local(term.FieldName);
                        _sb.Append(ind).Append(code).Append("u -> {   // SE(").Append(term.FieldName).AppendLine(")");
                        if (WrapsValue(term))
                            _sb.Append(ind).AppendLine("    r.readBits(1)   // value-start");
                        _sb.Append(ind).Append("    ").Append(field).Append(" = ").AppendLine(DecodeValueExpr(term));
                        if (WrapsValue(term))
                            _sb.Append(ind).AppendLine("    r.readBits(1)   // child EE");
                        _sb.Append(ind).Append("    done").Append(id).AppendLine(" = true");
                        _sb.Append(ind).AppendLine("}");
                    }
                    _sb.Append(ind).AppendLine("else -> throw IllegalArgumentException(\"invalid optional-run event code\")");
                    _sb.AppendLine("                    }");
                    _sb.AppendLine("                }");
                }

                _sb.AppendLine("            }");
                _sb.AppendLine("        }");
            }

            private string DecodeValueExpr(ChildPlan c) => c.Value switch
            {
                ValueEncoding.OpaqueElement oe =>
                    $"throw UnsupportedOperationException(\"Decoding a present {KStr(oe.TypeName)} " +
                    "(XMLDSig) is not implemented in the Kotlin back end.\")",
                ValueEncoding.ComplexRef cr  => $"decode{cr.TypeName}(r)",
                // An AT value is a bare string, like StringValue but without the value framing.
                // The slot is the element's or attribute's own QName local part: EXI keeps one
                // local value partition per slot (§7.3.3), and the decoder must name the right one.
                ValueEncoding.AttributeValue => $"ExiPrimitives.readStringValue(r, \"{KStr(c.FieldName)}\")",
                ValueEncoding.StringValue    => $"ExiPrimitives.readStringValue(r, \"{KStr(c.FieldName)}\")",
                ValueEncoding.UnsignedInt    => $"ExiPrimitives.readUnsignedInteger(r).{ToNarrow(c.Type)}",
                ValueEncoding.SignedInt      => $"ExiPrimitives.readSignedInteger(r).{ToNarrow(c.Type)}",
                ValueEncoding.Binary         => "ExiPrimitives.readBinary(r)",
                ValueEncoding.EnumIndex ei   => $"{ei.EnumName}.entries[r.readBits({ei.BitWidth}).toInt()]",
                ValueEncoding.NBitUnsigned nb => nb.Bias == 0
                                                    ? $"r.readBits({nb.BitWidth}).{ToNarrow(c.Type)}"
                                                    : $"(r.readBits({nb.BitWidth}).toLong() + {nb.Bias}L).{ToNarrow(c.Type)}",
                _ => throw new NotSupportedException($"Kotlin back end: value encoding {c.Value.GetType().Name}."),
            };

            /// <summary>Kotlin has no implicit numeric conversions — every read needs an explicit narrowing.</summary>
            private static bool IsBool(ChildPlan c) =>
                c.Type is TypeRef.Primitive { Kind: PrimitiveKind.Bool };

            private static string ToNarrow(TypeRef t) => t switch
            {
                TypeRef.Primitive p => p.Kind switch
                {
                    PrimitiveKind.Int8   => "toByte()",
                    PrimitiveKind.Int16  => "toShort()",
                    PrimitiveKind.Int32  => "toInt()",
                    PrimitiveKind.Int64  => "toLong()",
                    PrimitiveKind.UInt8  => "toUByte()",
                    PrimitiveKind.UInt16 => "toUShort()",
                    PrimitiveKind.UInt32 => "toUInt()",
                    PrimitiveKind.UInt64 => "toULong()",
                    PrimitiveKind.Bool   => "toInt() != 0",
                    _ => throw new NotSupportedException($"Kotlin back end: narrowing for {p.Kind}."),
                },
                _ => throw new NotSupportedException("Kotlin back end: narrowing for a named type."),
            };

            /// <summary>
            /// Occurrence bounds of a repeating child. For the "single repeating element" shape the
            /// builder records them on the <see cref="SequencePlan"/> and leaves the
            /// <see cref="ChildPlan"/>'s at zero; a repeating child among siblings carries its own.
            /// </summary>
            private static (int Min, int Max) ListBounds(ChildPlan c, SequencePlan sp) =>
                c.ListMax != 0 ? (Math.Max(c.ListMin, 1), c.ListMax)
                               : (Math.Max(sp.ListMin, 1), sp.ListMax);

            /// <summary>⌈log₂(n)⌉, with the EXI convention that n = 1 needs 0 bits.</summary>
            private static int BitsFor(int n)
            {
                var bits = 0;
                while ((1 << bits) < n) bits++;
                return bits;
            }
        }
    }
}
