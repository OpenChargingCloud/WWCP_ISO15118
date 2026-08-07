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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Grammar;

namespace cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Emit
{
    /// <summary>
    /// Produces C# source from a <see cref="SchemaPlan"/>.
    /// One emitted file per type — the record or enum together with its encode/decode pair, which
    /// is one part of a <c>partial</c> codec class — plus one file for that class's public entry
    /// points. The generated codec mirrors the structure of the hand-written
    /// <c>SupportedAppProtocolCodec</c> so the AppProtocol bytes are byte-equivalent.
    ///
    /// <para>
    /// <b>Wire model: non-strict schema-informed EXI grammar</b>, as produced by
    /// EVerest's cbexigen / libcbv2g (the de-facto ISO 15118 reference). Every
    /// structural transition carries an explicit event code:
    /// </para>
    /// <list type="bullet">
    ///   <item>Document element selector: <c>ceil(log2(globals+1))</c> bits (cbexigen
    ///         reserves one slot for the generic production).</item>
    ///   <item>Simple child element: SE (1 bit = 0), value-start (1 bit = 0), value,
    ///         child EE (1 bit = 0).</item>
    ///   <item>Complex child element: SE (1 bit = 0) then the nested content (which
    ///         emits its own element EE).</item>
    ///   <item>Required list: first item 1-bit SE, following items and the terminator
    ///         a 2-bit event code (item = 0, EE = 1).</item>
    ///   <item>Trailing optional element: 2-bit event code (present = 0, EE = 1).</item>
    ///   <item>Element EE of a sequence that does not end in a list/optional: 1 bit = 0.</item>
    /// </list>
    /// </summary>
    internal sealed class CodecEmitter
    {
        private readonly string _ns;         // generated C# namespace
        private readonly string _codecClass; // generated static codec class name

        /// <summary>
        /// The buffer every <c>Emit*</c> method appends to. Swapped as emission moves from type to
        /// type — see <see cref="Run"/>.
        /// </summary>
        private StringBuilder _sb = new();

        /// <summary>Per-type buffers: the declaration, and its encode/decode pair.</summary>
        private readonly Dictionary<string, StringBuilder> _decl = new(StringComparer.Ordinal);
        private readonly Dictionary<string, StringBuilder> _code = new(StringComparer.Ordinal);

        /// <summary>Type names in order of first emission — the order files come out in.</summary>
        private readonly List<string> _order = new();

        /// <summary>Guards against two declarations claiming the same file.</summary>
        private readonly HashSet<string> _fileNames = new(StringComparer.Ordinal);

        private readonly SchemaPlan _plan;
        private readonly HashSet<string> _emittedRecords = new(StringComparer.Ordinal);
        private readonly Dictionary<string, SequencePlan> _byRecordName = new(StringComparer.Ordinal);
        private readonly HashSet<string> _baseRecordNames = new(StringComparer.Ordinal); // types other types extend
        private int _runCounter; // unique suffixes for optional-run state locals
        private int _tmpCounter; // unique suffixes for decode temporaries

        private CodecEmitter(SchemaPlan plan, string ns, string codecClass)
        {
            _plan = plan;
            _ns = ns;
            _codecClass = codecClass;
            foreach (var sp in plan.ComplexTypes.Values)
            {
                _byRecordName[sp.RecordName] = sp;
                if (sp.BaseRecordName is not null)
                    _baseRecordNames.Add(sp.BaseRecordName);
            }
        }

        public static IReadOnlyList<GeneratedFile> Emit(SchemaPlan plan, string ns, string codecClass) =>
            new CodecEmitter(plan, ns, codecClass).Run();

        /// <summary>
        /// One file per type, plus one for the codec class.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A type's declaration and its encode/decode pair live together, which is the only layout
        /// in which a schema change touches a bounded set of files. The alternative — one file for
        /// a whole message set — made every diff a diff of the same 20,000-line file, and left the
        /// -20 DER sets at a size no reviewer reads.
        /// </para>
        /// <para>
        /// Unlike the Kotlin back end, nothing has to move or change visibility: the codec class
        /// becomes <c>partial</c>, its parts are spread over the type files, and a private member
        /// declared in one part is still reachable from another. The class is the same class; only
        /// the files differ.
        /// </para>
        /// </remarks>
        private IReadOnlyList<GeneratedFile> Run()
        {
            var files = new List<GeneratedFile>();

            foreach (var e in _plan.Enums)
                files.Add(Standalone(e.Name, () => EmitEnum(e)));

            foreach (var t in _plan.OpaqueTypes)
                files.Add(Standalone(t, () => EmitOpaqueType(t)));

            EmitRecords();
            EmitTypeCodecs();

            foreach (var name in _order)
            {
                var body = new StringBuilder(_decl[name].ToString());

                if (_code.TryGetValue(name, out var codec) && codec.Length > 0)
                {
                    body.Append("public static partial class ").Append(_codecClass).AppendLine();
                    body.AppendLine("{");
                    body.AppendLine(codec.ToString().TrimEnd('\r', '\n'));
                    body.AppendLine("}");
                }

                files.Add(File(name, body.ToString()));
            }

            files.Add(Standalone(_codecClass, EmitFacade));

            return files;
        }

        /// <summary>A file holding one thing, emitted by <paramref name="emit"/> into a fresh buffer.</summary>
        private GeneratedFile Standalone(string name, Action emit)
        {
            _sb = new StringBuilder();
            emit();
            return File(name, _sb.ToString());
        }

        /// <summary>Wraps a body in the file header and the block-scoped namespace.</summary>
        private GeneratedFile File(string name, string body)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("// Generated by WWCP_ISO15118_EXI_SourceGenerator. Do not edit by hand.");
            sb.AppendLine("#nullable enable");
            // Bounded grammars produce provably-unreachable states/patterns (e.g. the terminal EE state
            // of a repeating-terminated run); silence the resulting dead-code / always-match warnings.
            sb.AppendLine("#pragma warning disable CS0162, CS8794");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.IO;");
            sb.AppendLine("using cloud.charging.open.protocols.ISO15118.EXI;");
            sb.AppendLine();

            // Block-scoped namespace (repo style, see .editorconfig) — applied here as a single
            // indent pass over the already-emitted body rather than threading an extra indent
            // level through every Emit* method above.
            sb.Append("namespace ").Append(_ns).AppendLine();
            sb.AppendLine("{");
            using (var reader = new System.IO.StringReader(body.TrimEnd('\r', '\n')))
            {
                string? line;
                while ((line = reader.ReadLine()) is not null)
                    sb.AppendLine(line.Length == 0 ? "" : "    " + line);
            }
            sb.AppendLine("}");

            if (!_fileNames.Add(name))
                throw new NotSupportedException(
                    $"C# back end: two declarations are both called '{name}', so they would share a " +
                    "file. One file per type only works while type names are unique.");

            return new GeneratedFile(name + ".g.cs", sb.ToString());
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

        // -----------------------------------------------------------------------
        //  Opaque placeholder records — for references into an un-modelled namespace
        //  (XMLDSig). They exist so the containing record is structurally typed and
        //  round-trips while the element is absent; a present instance fails loud.
        // -----------------------------------------------------------------------

        private void EmitOpaqueType(string t)
        {
            _sb.Append("/// <summary>Opaque placeholder for the un-modelled XMLDSig element <c>")
               .Append(t).AppendLine("</c> (full grammar deferred to Phase 3).</summary>");
            _sb.Append("public sealed record ").Append(t).AppendLine("();");
        }

        // -----------------------------------------------------------------------
        //  Enums — declaration order; the enum value IS the EXI n-bit index.
        // -----------------------------------------------------------------------

        private void EmitEnum(EnumPlan e)
        {
            _sb.Append("public enum ").Append(e.Name).AppendLine(" : byte");
            _sb.AppendLine("{");
            for (int i = 0; i < e.Members.Count; i++)
            {
                // Wire encoding is by ordinal position (ValueEncoding.EnumIndex), not the XML string
                // value, so it's safe to sanitize the C# identifier — needed for enumerations like
                // WPT_PowerClassType's "MF-WPT1" (a hyphen isn't a valid identifier character).
                _sb.Append("    ").Append(SanitizeIdentifier(e.Members[i])).Append(" = ").Append(i);
                _sb.AppendLine(i + 1 < e.Members.Count ? "," : "");
            }
            _sb.AppendLine("}");
        }

        /// <summary>Replaces any character invalid in a C# identifier with <c>_</c>, and prefixes with
        /// <c>_</c> if the result would otherwise start with a digit.</summary>
        private static string SanitizeIdentifier(string s)
        {
            if (string.IsNullOrEmpty(s)) return "_";
            var chars = s.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                if (!(char.IsLetterOrDigit(chars[i]) || chars[i] == '_'))
                    chars[i] = '_';
            var result = new string(chars);
            return char.IsDigit(result[0]) ? "_" + result : result;
        }

        // -----------------------------------------------------------------------
        //  Records — emit dependencies first, deduplicated
        // -----------------------------------------------------------------------

        private void EmitRecords()
        {
            foreach (var sp in _plan.ComplexTypes.Values
                                      .OrderBy(s => DependencyDepth(s)))
            {
                EmitRecord(sp);
            }
        }

        private int DependencyDepth(SequencePlan sp)
        {
            int max = 0;
            foreach (var c in sp.Children)
            {
                if (c.Value is ValueEncoding.ComplexRef cr &&
                    _plan.ComplexTypes.TryGetValue(cr.TypeName, out var inner))
                {
                    max = Math.Max(max, 1 + DependencyDepth(inner));
                }
            }
            return max;
        }

        private void EmitRecord(SequencePlan sp)
        {
            if (!_emittedRecords.Add(sp.RecordName)) return;
            Target(_decl, sp.RecordName);

            // A concrete type that other types extend (e.g. ServiceType, base of ChargeServiceType)
            // must not be sealed; only leaf concrete records are.
            string keyword = sp.IsAbstract ? "abstract "
                           : _baseRecordNames.Contains(sp.RecordName) ? ""
                           : "sealed ";

            // Inheritance: the first N flattened children are the base type's, so they are passed
            // straight through to the base record's constructor (C# positional-record pattern).
            string baseClause = "";
            if (sp.BaseRecordName is not null)
            {
                var baseArgs = _byRecordName.TryGetValue(sp.BaseRecordName, out var basePlan)
                    ? string.Join(", ", basePlan.Children.Select(bc => bc.FieldName))
                    : "";
                baseClause = $" : {sp.BaseRecordName}({baseArgs})";
            }

            // Parameters: attributes first (nullable unless use="required"), then the content particles.
            var parameters = new List<string>();
            if (sp.Attributes is not null)
                foreach (var a in sp.Attributes)
                    parameters.Add(a.Required ? $"{a.CsType()} {a.FieldName}" : $"{a.CsType()}? {a.FieldName}");
            if (sp.SimpleContent is not null)
                parameters.Add($"{CSharpSyntax.Syntax(sp.SimpleContentType!)} Value");
            foreach (var c in sp.Children)
            {
                if (c.Value is ValueEncoding.InlineChoice ic)
                {
                    // Each branch is its own independent (always-nullable) parameter — the wrapping
                    // ChildPlan itself is a bookkeeping marker only, never a real record field.
                    foreach (var m in ic.Members)
                        parameters.Add($"{m.CsType()}? {m.FieldName}");
                    continue;
                }
                string typeText = c.Shape switch
                {
                    ChildShape.BoundedRepeating => $"IReadOnlyList<{c.CsType()}>",
                    ChildShape.OptionalSingle   => c.CsType() + "?",
                    _                           => c.CsType(),
                };
                parameters.Add($"{typeText} {c.FieldName}");
            }

            if (parameters.Count == 0)
            {
                _sb.Append("public ").Append(keyword).Append("record ").Append(sp.RecordName)
                   .Append("()").Append(baseClause).AppendLine(";");
                _sb.AppendLine();
                return;
            }

            _sb.Append("public ").Append(keyword).Append("record ").Append(sp.RecordName).AppendLine("(");
            for (int i = 0; i < parameters.Count; i++)
            {
                _sb.Append("    ").Append(parameters[i]);
                _sb.AppendLine(i + 1 < parameters.Count ? "," : "");
            }
            _sb.Append(")").Append(baseClause).AppendLine(";");
            _sb.AppendLine();
        }

        // -----------------------------------------------------------------------
        //  Codec
        // -----------------------------------------------------------------------

        /// <summary>
        /// The codec class's own file: the public entry points, and nothing else. Every per-type
        /// encode/decode pair it used to hold sits beside its own record, in another part of this
        /// same partial class.
        /// </summary>
        private void EmitFacade()
        {
            _sb.Append("public static partial class ").AppendLine(_codecClass);
            _sb.AppendLine("{");
            _sb.AppendLine("    public const byte ExiHeader = 0x80;");
            _sb.AppendLine();

            var globals = _plan.GlobalElements
                               .OrderBy(g => g.DocumentIndex)
                               .ToList();
            int docBits = _plan.DocumentSelectorBits;

            // Public encode entry points (one extension method per decodable document root).
            foreach (var g in globals)
                EmitEncodeEntryPoint(g, docBits);

            // The two dispatchers, in both directions.
            EmitEncodeDispatcher(globals);
            EmitDecodeDispatcher(globals, docBits);

            // EXI fragment codecs for the signable elements (XMLDSig).
            EmitFragmentCodecs();

            _sb.AppendLine("}");
        }

        /// <summary>
        /// Per-complex-type encode/decode methods, deduplicated by record name, each into the buffer
        /// of the type it belongs to. Abstract types (substitution heads, extension bases) are never
        /// encoded/decoded directly — only their concrete members are — so emitting their codec
        /// methods would just be dead, uncompilable code (a <c>new AbstractType(...)</c>). Skip them.
        /// </summary>
        private void EmitTypeCodecs()
        {
            var seenComplex = new HashSet<string>(StringComparer.Ordinal);
            foreach (var sp in _plan.ComplexTypes.Values
                                      .OrderBy(s => s.RecordName, StringComparer.Ordinal))
            {
                if (sp.IsAbstract) continue;
                if (!seenComplex.Add(sp.RecordName)) continue;
                Target(_code, sp.RecordName);
                EmitEncodeMethod(sp);
                EmitDecodeMethod(sp);
            }
        }

        /// <summary>
        /// Emits an EXI fragment encoder/decoder per signable element. A fragment is the EXI header,
        /// then the element's fragment-grammar event code (§8.5.3, a selector over every element
        /// declaration of the set), then the element's content — no document/body wrapper. Used to
        /// digest a signable element for XMLDSig. Verified against cbV2G's encode_iso2_exiFragment.
        /// </summary>
        private void EmitFragmentCodecs()
        {
            foreach (var f in _plan.Fragments)
            {
                int bits = _plan.FragmentSelectorBits;

                _sb.Append("    public static bool EncodeFragment_").Append(f.ElementName)
                   .Append("(").Append(f.TypeName).AppendLine(" content, Span<byte> dest, out int bytesWritten)");
                _sb.AppendLine("    {");
                _sb.AppendLine("        bytesWritten = 0;");
                _sb.AppendLine("        if (dest.Length < 1) return false;");
                _sb.AppendLine("        dest[0] = ExiHeader;");
                _sb.AppendLine("        var w = new BitWriter(dest[1..]);");
                _sb.Append("        w.WriteBits(").Append(f.EventCode).Append(", ").Append(bits)
                   .Append(");   // fragment SE(").Append(f.ElementName).AppendLine(")");
                _sb.Append("        Encode_").Append(f.TypeName).AppendLine("(ref w, content);");
                _sb.Append("        w.WriteBits(").Append(_plan.FragmentEndCode).Append(", ").Append(bits)
                   .AppendLine(");   // End Fragment (ED)");
                _sb.AppendLine("        w.AlignToByte();");
                _sb.AppendLine("        bytesWritten = 1 + w.BytesWritten;");
                _sb.AppendLine("        return true;");
                _sb.AppendLine("    }");
                _sb.AppendLine();

                _sb.Append("    public static ").Append(f.TypeName).Append(" DecodeFragment_")
                   .Append(f.ElementName).AppendLine("(ReadOnlySpan<byte> src, out int bytesConsumed)");
                _sb.AppendLine("    {");
                _sb.AppendLine("        if (src.Length < 1 || src[0] != ExiHeader)");
                _sb.AppendLine("            throw new InvalidDataException(\"Invalid EXI header.\");");
                _sb.AppendLine("        var r = new BitReader(src[1..]);");
                _sb.Append("        if (r.ReadBits(").Append(bits).Append(") != ").Append(f.EventCode).AppendLine("u)");
                _sb.Append("            throw new InvalidDataException(\"Not a ").Append(f.ElementName).AppendLine(" fragment.\");");
                _sb.Append("        var result = Decode_").Append(f.TypeName).AppendLine("(ref r);");
                _sb.Append("        if (r.ReadBits(").Append(bits).Append(") != ").Append(_plan.FragmentEndCode)
                   .AppendLine("u) throw new InvalidDataException(\"missing End Fragment.\");");
                _sb.AppendLine("        bytesConsumed = 1 + r.BytesConsumed;");
                _sb.AppendLine("        return result;");
                _sb.AppendLine("    }");
                _sb.AppendLine();
            }
        }

        private void EmitEncodeEntryPoint(GlobalElementPlan g, int docBits)
        {
            _sb.Append("    public static bool TryEncode(this ")
               .Append(g.TypeName)
               .AppendLine(" msg, Span<byte> dest, out int bytesWritten)");
            _sb.AppendLine("    {");
            _sb.AppendLine("        bytesWritten = 0;");
            _sb.AppendLine("        if (dest.Length < 1) return false;");
            _sb.AppendLine("        dest[0] = ExiHeader;");
            _sb.AppendLine("        var w = new BitWriter(dest[1..]);");
            if (docBits > 0)
                _sb.Append("        w.WriteBits(").Append(g.DocumentIndex).Append(", ").Append(docBits)
                   .AppendLine(");   // document element selector");
            _sb.Append("        Encode_").Append(g.Body.RecordName).AppendLine("(ref w, msg);");
            _sb.AppendLine("        w.AlignToByte();");
            _sb.AppendLine("        bytesWritten = 1 + w.BytesWritten;");
            _sb.AppendLine("        return true;");
            _sb.AppendLine("    }");
            _sb.AppendLine();
        }

        /// <summary>
        /// <c>TryEncodeAny(object, …)</c> — the mirror of <see cref="EmitDecodeDispatcher"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Decoding always produced an <c>object</c> and encoding never accepted one, so anything
        /// holding a message it did not construct itself — a proxy, a recorder, a re-encoder — had to
        /// write the type switch out by hand. Two copies of exactly that live in
        /// <c>Secc20Ac</c>/<c>Secc20Dc</c>, which is the usual sign that the generator was missing a
        /// method rather than that the callers were unusual.
        /// </para>
        /// <para>
        /// Branches are ordered most-derived-first for the reason every other type dispatch here is:
        /// a type pattern matches subtypes too, so a base-first order would silently encode a derived
        /// message with its base's document index.
        /// </para>
        /// </remarks>
        private void EmitEncodeDispatcher(List<GlobalElementPlan> globals)
        {
            _sb.AppendLine("    /// <summary>Encodes any document element of this message set, dispatching on its runtime type.</summary>");
            _sb.AppendLine("    public static bool TryEncodeAny(object msg, Span<byte> dest, out int bytesWritten)");
            _sb.AppendLine("    {");
            _sb.AppendLine("        switch (msg)");
            _sb.AppendLine("        {");

            foreach (var g in globals.OrderByDescending(g => BaseDepth(g.Body))
                                     .ThenBy(g => g.DocumentIndex))
                _sb.Append("            case ").Append(g.Body.RecordName)
                   .AppendLine(" m: return m.TryEncode(dest, out bytesWritten);");

            _sb.AppendLine("            default: throw new InvalidDataException(");
            _sb.AppendLine("                         $\"{msg.GetType().Name} is not a document element of this message set.\");");
            _sb.AppendLine("        }");
            _sb.AppendLine("    }");
            _sb.AppendLine();
        }

        /// <summary>How many records deep a type's base chain runs.</summary>
        private int BaseDepth(SequencePlan sp)
        {
            var depth   = 0;
            var current = sp;
            while (current.BaseRecordName is not null && _byRecordName.TryGetValue(current.BaseRecordName, out var next))
            {
                depth++;
                current = next;
            }
            return depth;
        }

        private void EmitDecodeDispatcher(List<GlobalElementPlan> globals, int docBits)
        {
            _sb.AppendLine("    public static object DecodeAny(ReadOnlySpan<byte> src, out int bytesConsumed)");
            _sb.AppendLine("    {");
            _sb.AppendLine("        if (src.Length < 1 || src[0] != ExiHeader)");
            _sb.AppendLine("            throw new InvalidDataException(\"Invalid EXI header.\");");
            _sb.AppendLine("        var r = new BitReader(src[1..]);");

            if (docBits > 0)
            {
                _sb.Append("        uint sel = r.ReadBits(").Append(docBits).AppendLine(");");
                _sb.AppendLine("        object result = sel switch");
                _sb.AppendLine("        {");
                foreach (var g in globals)
                {
                    _sb.Append("            ").Append(g.DocumentIndex).Append("u => Decode_")
                       .Append(g.Body.RecordName).AppendLine("(ref r),");
                }
                _sb.AppendLine("            _ => throw new InvalidDataException($\"Unknown document index {sel}.\"),");
                _sb.AppendLine("        };");
            }
            else
            {
                _sb.Append("        object result = Decode_")
                   .Append(globals[0].Body.RecordName).AppendLine("(ref r);");
            }

            _sb.AppendLine("        bytesConsumed = 1 + r.BytesConsumed;");
            _sb.AppendLine("        return result;");
            _sb.AppendLine("    }");
            _sb.AppendLine();
        }

        // -----------------------------------------------------------------------
        //  Encode_<RecordName>
        // -----------------------------------------------------------------------

        private void EmitEncodeMethod(SequencePlan sp)
        {
            _sb.Append("    private static void Encode_").Append(sp.RecordName)
               .Append("(ref BitWriter w, ").Append(sp.RecordName).AppendLine(" msg)");
            _sb.AppendLine("    {");

            if (IsBoundedRepeating(sp, out var rep))
            {
                _sb.Append("        var list = msg.").Append(rep.FieldName).AppendLine(";");
                _sb.Append("        if (list.Count is < ").Append(Math.Max(1, sp.ListMin))
                   .Append(" or > ").Append(sp.ListMax)
                   .AppendLine(") throw new ArgumentOutOfRangeException(nameof(msg));");
                int forced = ForcedOccurrences(sp.ListMin);
                _sb.AppendLine("        for (int i = 0; i < list.Count; i++)");
                _sb.AppendLine("        {");
                _sb.Append("            w.WriteBits(0, ").Append(SeWidthExpr("i", forced)).Append(");")
                   .AppendLine(SeWidthComment(forced));
                EmitEncodeContent(rep, "list[i]", "            ");
                _sb.AppendLine("        }");
                EmitEncodeListTerminator("        ", "list", sp.ListMax);
            }
            else if (sp.SimpleContent is not null && sp.Attributes is not null && sp.Attributes.Any(a => !a.Required))
            {
                EmitEncodeSimpleContentOptionalAttrs(sp);
            }
            else if (sp.SimpleContent is not null)
            {
                EmitRequiredAttributePrefix(sp);   // no-op when there are no attributes
                var valueChild = new ChildPlan("Value", sp.SimpleContentType!, false,
                                               ChildShape.RequiredSingle, sp.SimpleContent!);
                _sb.AppendLine("        w.WriteBits(0, 1);   // CONTENT event");
                EmitWriteValue(valueChild, "msg.Value", "        ");
                _sb.AppendLine("        w.WriteBits(0, 1);   // element EE");
            }
            else if (sp.Attributes is { Count: 1 } && sp.Attributes[0].Required)
            {
                EmitRequiredAttributePrefix(sp);
                EmitEncodeContentBody(sp);
            }
            else if (sp.Attributes is not null)
            {
                // Optional attribute(s): the AT event is the first production of the content's initial
                // grammar state — i.e. the attribute is simply the leading optional of the content run
                // (cbexigen model, verified against AuthorizationReqType grammar 222/223 and
                // CertificateChainType). Prepend it and reuse the general optional-run machine.
                var children = WithOptionalAttributes(sp);
                bool terminated = EmitEncodeSequenceChildren(children, "        ");
                if (!terminated)
                    _sb.AppendLine("        w.WriteBits(0, 1);   // element EE");
            }
            else
            {
                EmitEncodeContentBody(sp);
            }

            _sb.AppendLine("    }");
            _sb.AppendLine();
        }

        /// <summary>
        /// Prepends a type's optional attributes to its content as leading optional "children" whose
        /// value is a bare AT string. This unifies the optional-attribute grammar with the general
        /// optional-run machine. Attributes are already sorted lexicographically by the grammar builder.
        /// </summary>
        private static IReadOnlyList<ChildPlan> WithOptionalAttributes(SequencePlan sp)
        {
            var list = new List<ChildPlan>();
            foreach (var a in sp.Attributes!)
            {
                if (a.Required)
                    throw new NotSupportedException(
                        $"{sp.RecordName}: mixing required and optional attributes is not supported yet.");
                if (a.Value is not ValueEncoding.StringValue)
                    throw new NotSupportedException(
                        $"{sp.RecordName}: only string-typed optional attributes are supported yet.");
                list.Add(new ChildPlan(a.FieldName, a.Type, IsValueType: false,
                                       ChildShape.OptionalSingle, new ValueEncoding.AttributeValue()));
            }
            if (sp.IsChoice)
                throw new NotSupportedException(
                    $"{sp.RecordName}: optional attributes with xs:choice content are not supported yet.");
            list.AddRange(sp.Children);
            return list;
        }

        /// <summary>A required attribute is always present: a 1-bit AT event, then its value.</summary>
        private void EmitRequiredAttributePrefix(SequencePlan sp)
        {
            if (sp.Attributes is not { Count: 1 } || !sp.Attributes[0].Required) return;
            var attr = sp.Attributes[0];
            _sb.AppendLine("        w.WriteBits(0, 1);   // AT(required attribute)");
            _sb.Append("        ExiPrimitives.WriteStringValue(ref w, msg.").Append(attr.FieldName).AppendLine("!);");
        }

        /// <summary>
        /// simpleContent with optional attribute(s): a bare value (CONTENT) preceded by an optional-run of
        /// AT productions. cbexigen's grammar (verified against SignatureValueType 96/97): state k holds
        /// <c>{attr_k … attr_{n-1}, CONTENT}</c> at <c>ceil(log2(count+1))</c> bits; choosing CONTENT
        /// writes its event code then the bare value (no value-start / child EE), and the element EE is a
        /// separate 1-bit production.
        /// </summary>
        private void EmitEncodeSimpleContentOptionalAttrs(SequencePlan sp)
        {
            var oa = sp.Attributes!;   // all optional (the required/none cases take the simpler path)
            foreach (var a in oa)
                if (a.Required || a.Value is not ValueEncoding.StringValue)
                    throw new NotSupportedException(
                        $"{sp.RecordName}: simpleContent supports only string-typed optional attributes.");
            var valueChild = new ChildPlan("Value", sp.SimpleContentType!, false,
                                           ChildShape.RequiredSingle, sp.SimpleContent!);
            int n = oa.Count;
            int id = _runCounter++;
            string st = "_ost" + id, done = "_odone" + id;

            _sb.Append("        int ").Append(st).AppendLine(" = 0;");
            _sb.Append("        bool ").Append(done).AppendLine(" = false;");
            _sb.Append("        while (!").Append(done).AppendLine(")");
            _sb.AppendLine("        {");
            _sb.Append("            switch (").Append(st).AppendLine(")");
            _sb.AppendLine("            {");
            for (int k = 0; k <= n; k++)
            {
                int width = BitsForChoices((n - k + 1) + 1);   // remaining optional attrs + CONTENT + phantom
                _sb.Append("                case ").Append(k).AppendLine(":");
                _sb.AppendLine("                {");
                int code = 0;
                bool first = true;
                for (int i = k; i < n; i++, code++, first = false)
                {
                    _sb.Append("                    ").Append(first ? "if" : "else if")
                       .Append(" (msg.").Append(oa[i].FieldName).AppendLine(" is not null)");
                    _sb.AppendLine("                    {");
                    _sb.Append("                        w.WriteBits(").Append(code).Append(", ").Append(width)
                       .Append(");   // AT(").Append(oa[i].FieldName).AppendLine(")");
                    _sb.Append("                        ExiPrimitives.WriteStringValue(ref w, msg.")
                       .Append(oa[i].FieldName).AppendLine("!);");
                    _sb.Append("                        ").Append(st).Append(" = ").Append(i + 1).AppendLine(";");
                    _sb.AppendLine("                    }");
                }
                if (!first) _sb.AppendLine("                    else");
                _sb.AppendLine("                    {");
                _sb.Append("                        w.WriteBits(").Append(code).Append(", ").Append(width)
                   .AppendLine(");   // CONTENT");
                EmitWriteValue(valueChild, "msg.Value", "                        ");
                _sb.Append("                        ").Append(done).AppendLine(" = true;");
                _sb.AppendLine("                    }");
                _sb.AppendLine("                    break;");
                _sb.AppendLine("                }");
            }
            _sb.AppendLine("            }");
            _sb.AppendLine("        }");
            _sb.AppendLine("        w.WriteBits(0, 1);   // element EE");
        }

        /// <summary>Decode counterpart of <see cref="EmitEncodeSimpleContentOptionalAttrs"/>.</summary>
        private void EmitDecodeSimpleContentOptionalAttrs(SequencePlan sp)
        {
            var oa = sp.Attributes!;
            var valueChild = new ChildPlan("Value", sp.SimpleContentType!, false,
                                           ChildShape.RequiredSingle, sp.SimpleContent!);
            int n = oa.Count;
            int id = _runCounter++;
            string st = "_ist" + id, done = "_idone" + id, code = "_ic" + id;

            foreach (var a in oa)
                _sb.Append("        ").Append(a.CsType()).Append("? _").Append(a.FieldName).AppendLine(" = default;");
            _sb.Append("        ").Append(CSharpSyntax.Syntax(sp.SimpleContentType!)).Append(" _Value = default!;");
            _sb.AppendLine();
            _sb.Append("        int ").Append(st).AppendLine(" = 0;");
            _sb.Append("        bool ").Append(done).AppendLine(" = false;");
            _sb.Append("        while (!").Append(done).AppendLine(")");
            _sb.AppendLine("        {");
            _sb.Append("            switch (").Append(st).AppendLine(")");
            _sb.AppendLine("            {");
            for (int k = 0; k <= n; k++)
            {
                int width = BitsForChoices((n - k + 1) + 1);
                _sb.Append("                case ").Append(k).AppendLine(":");
                _sb.AppendLine("                {");
                _sb.Append("                    uint ").Append(code).Append(" = r.ReadBits(").Append(width).AppendLine(");");
                _sb.Append("                    switch (").Append(code).AppendLine(")");
                _sb.AppendLine("                    {");
                for (int i = k; i < n; i++)
                {
                    _sb.Append("                        case ").Append(i - k).AppendLine("u:");
                    _sb.Append("                            _").Append(oa[i].FieldName)
                       .Append(" = ExiPrimitives.ReadStringValue(ref r, \"").Append(oa[i].FieldName).AppendLine("\");");
                    _sb.Append("                            ").Append(st).Append(" = ").Append(i + 1).AppendLine(";");
                    _sb.AppendLine("                            break;");
                }
                _sb.Append("                        case ").Append(n - k).AppendLine("u:");
                _sb.Append("                            _Value = ");
                AppendReadValueExpr(valueChild);
                _sb.AppendLine(";");
                _sb.Append("                            ").Append(done).AppendLine(" = true; break;");
                _sb.AppendLine("                        default: throw new InvalidDataException(\"invalid simpleContent event code\");");
                _sb.AppendLine("                    }");
                _sb.AppendLine("                    break;");
                _sb.AppendLine("                }");
            }
            _sb.AppendLine("            }");
            _sb.AppendLine("        }");
            _sb.AppendLine("        r.ReadBits(1);   // element EE");
            var locals = oa.Select(a => "_" + a.FieldName).Append("_Value");
            _sb.Append("        return new ").Append(sp.RecordName).Append('(')
               .Append(string.Join(", ", locals)).AppendLine(");");
        }

        /// <summary>Emit a complex type's content (an xs:choice or an xs:sequence) plus its element EE.</summary>
        private void EmitEncodeContentBody(SequencePlan sp)
        {
            if (sp.IsChoice)
            {
                EmitEncodeChoice(sp);
                _sb.AppendLine("        w.WriteBits(0, 1);   // element EE");
                return;
            }

            bool terminated = EmitEncodeSequenceChildren(sp.Children, "        ");
            if (!terminated)
                _sb.AppendLine("        w.WriteBits(0, 1);   // element EE");
        }

        /// <summary>
        /// Walks a sequence's particles left to right, emitting each segment: a required element
        /// (1-bit SE + content or a substitution machine), a bounded-repeating list (its own EE),
        /// or a run of consecutive optional elements (a flat grammar-state machine, see
        /// <see cref="EmitEncodeOptionalRun"/>). Returns whether the element's END event has already
        /// been written (a list terminator or an EE-terminated optional run doubles as it).
        /// </summary>
        private bool EmitEncodeSequenceChildren(IReadOnlyList<ChildPlan> children, string indent)
        {
            bool terminated = false;
            int i = 0;
            while (i < children.Count)
            {
                var c = children[i];
                switch (c.Shape)
                {
                    case ChildShape.BoundedRepeating when c.ListMin > 0 && i == children.Count - 1:
                        EmitEncodeRepeatingChild(c, indent);   // required list: 1-bit first / 2-bit loop
                        terminated = true;
                        i++;
                        break;

                    case ChildShape.BoundedRepeating when c.ListMin > 0:
                        // Followed by exactly one more particle (AuthorizationSetupResType's
                        // AuthorizationServices -> CertificateInstallationService shape, required tail; or
                        // WPT_LF_TransmitterDataType's TxSpecData -> TxPackageSpecData?, optional tail):
                        // the list's own "continue vs move on" code doubles as the tail's SE/dispatch. An
                        // optional tail that's the sequence's last particle writes its own closing EE (no
                        // fallback from the caller); a required tail never does (same as always).
                        EmitEncodeRequiredRepeatingWithTail(c, children[i + 1], indent);
                        terminated = children[i + 1].Shape != ChildShape.RequiredSingle && i + 2 == children.Count;
                        i += 2;
                        break;

                    case ChildShape.RequiredSingle:
                        if (c.Value is ValueEncoding.SubstitutionChoice sc)
                            EmitEncodeSubstitution(c, sc);
                        else if (c.Value is ValueEncoding.InlineChoice ic)
                            EmitEncodeInlineChoiceStandalone(ic);
                        else
                        {
                            _sb.Append(indent).AppendLine("w.WriteBits(0, 1);   // SE");
                            EmitEncodeContent(c, "msg." + c.FieldName, indent);
                        }
                        terminated = false;
                        i++;
                        break;

                    default: // OptionalSingle (or an optional bounded-repeating list) — the head of a run
                        int j = RunEnd(children, i);
                        bool endsElement = j == children.Count;
                        EmitEncodeOptionalRun(children, i, j, indent);
                        // A required repeating terminator's loop emits the element EE itself.
                        bool repTerm = !endsElement && children[j].Shape == ChildShape.BoundedRepeating;
                        terminated = endsElement || repTerm;
                        i = endsElement ? j : j + 1;
                        break;
                }
            }
            return terminated;
        }

        /// <summary>
        /// Emits the flat EXI grammar-state machine for a run of optional particles
        /// <c>children[s..e)</c> terminated by the element EE (when the run ends the sequence) or the
        /// following required particle <c>children[e]</c>. Each particle contributes one grammar
        /// production, except a substitution reference, which flattens into one production per member
        /// (and the abstract head); the event-code width at each state is
        /// <c>ceil(log2(totalProductions + 1))</c> — cbexigen's non-strict grammar, verified against
        /// MessageHeaderType, CurrentDemandResType, PowerDeliveryReqType and ChargeParameterDiscoveryResType.
        /// State <c>k</c> covers optional particles from cursor <c>s+k</c> plus the terminator; choosing
        /// a production advances the cursor past its particle. The terminator's content is emitted here;
        /// the caller continues after <c>children[e]</c>.
        /// </summary>
        private void EmitEncodeOptionalRun(IReadOnlyList<ChildPlan> children, int s, int e, string indent)
        {
            int listIdx = -1;
            for (int p = s; p < e; p++)
                if (children[p].Shape == ChildShape.BoundedRepeating) { listIdx = p; break; }
            if (listIdx >= 0 && listIdx != e - 1)
            {
                if (_plan.ParticleGrammar == ParticleGrammar.SchemaConformant)
                     EmitEncodeOptionalRunWithMidListSchema(children, s, listIdx, e, indent);
                else EmitEncodeOptionalRunWithMidList(children, s, listIdx, e, indent);
                return;
            }

            int m = e - s;                       // number of optional particles in the run
            bool endsElement = e == children.Count;
            ChildPlan? term = endsElement ? null : children[e];
            if (term is not null && term.Shape is not (ChildShape.RequiredSingle or ChildShape.BoundedRepeating))
                throw new NotSupportedException(
                    $"optional run before '{term.FieldName}': the terminator must be the element EE or a required particle.");
            if (term is not null && term.Shape == ChildShape.BoundedRepeating && e != children.Count - 1)
                throw new NotSupportedException(
                    $"repeating terminator '{term.FieldName}' must be the last child of the sequence (its loop ends the element).");
            // A bounded-repeating member is only supported as the last member of an EE-terminated run
            // (its loop consumes the element EE).
            for (int p = s; p < e; p++)
                if (children[p].Shape == ChildShape.BoundedRepeating && (p != e - 1 || !endsElement))
                    throw new NotSupportedException(
                        $"repeating element '{children[p].FieldName}' in an optional run must be its last member and end the sequence.");

            // An xs:any wildcard (the synthetic ANY) is only supported as the last member of an
            // EE-terminated run: cbexigen splits it into a generic wildcard event and the typed element,
            // with the element EE between them (verified against SignatureMethodType / DigestMethodType).
            for (int p = s; p < e; p++)
                if (children[p].IsWildcardAny && (p != e - 1 || !endsElement))
                    throw new NotSupportedException(
                        $"xs:any wildcard '{children[p].FieldName}' must be the last child of an EE-terminated sequence.");
            bool trailingAny = endsElement && m > 0 && children[e - 1].IsWildcardAny;

            int id = _runCounter++;
            string st = "_ost" + id, done = "_odone" + id;
            string inner = indent + "    ";
            string body = inner + "    ";
            string br   = body + "    ";

            _sb.Append(indent).Append("int ").Append(st).AppendLine(" = 0;");
            _sb.Append(indent).Append("bool ").Append(done).AppendLine(" = false;");
            _sb.Append(indent).Append("while (!").Append(done).AppendLine(")");
            _sb.Append(indent).AppendLine("{");
            _sb.Append(inner).Append("switch (").Append(st).AppendLine(")");
            _sb.Append(inner).AppendLine("{");

            for (int k = 0; k <= m; k++)   // state k: cursor at optional particle s+k (k==m: terminator only)
            {
                int totalProd = endsElement ? 1 : ProductionCount(term!);
                for (int i = k; i < m; i++) totalProd += ProductionCount(children[s + i]);
                int width = BitsForChoices(totalProd + 1);

                _sb.Append(body).Append("case ").Append(k).AppendLine(":");
                _sb.Append(body).AppendLine("{");
                int code = 0;
                bool first = true;

                // The trailing wildcard ANY is handled after the EE below, so stop the normal loop before it.
                int optEnd = trailingAny ? m - 1 : m;
                for (int i = k; i < optEnd; i++)
                {
                    // A repeating member enters its own loop and ends the element; others advance the cursor.
                    string after = children[s + i].Shape == ChildShape.BoundedRepeating
                        ? done + " = true;"
                        : st + " = " + (i + 1) + ";";
                    code = EmitEncodeRunParticle(children[s + i], code, width, ref first, br, after);
                }

                // Terminator productions occupy the highest event codes.
                if (trailingAny && k <= m - 1)
                {
                    // cbexigen ordering: [normal optionals] generic-wildcard (reserved, never emitted),
                    // element EE, then the typed ANY element. Selecting ANY advances to the EE-only state m.
                    int eeCode    = code + 1;   // code == generic-wildcard slot
                    int typedCode = code + 2;
                    EmitEncodeRunParticle(children[e - 1], typedCode, width, ref first, br, st + " = " + m + ";");
                    EmitEncodeRunTail(first, br, "w.WriteBits(" + eeCode + ", " + width + ");   // element EE", done);
                }
                else if (endsElement)
                    EmitEncodeRunTail(first, br, "w.WriteBits(" + code + ", " + width + ");   // element EE", done);
                else if (term!.Value is ValueEncoding.SubstitutionChoice or ValueEncoding.InlineChoice)
                {
                    code = EmitEncodeRunParticle(term, code, width, ref first, br, done + " = true;");
                    _sb.Append(br).Append("else throw new ArgumentException(\"no value set for ")
                       .Append(term.FieldName).AppendLine("\");");
                }
                else if (term.Shape == ChildShape.BoundedRepeating)
                    EmitEncodeRunTailRepeating(first, br, term, code, width, done);
                else
                    EmitEncodeRunTail(first, br, "w.WriteBits(" + code + ", " + width + ");   // SE(" + term.FieldName + ")",
                                      done, term);
                _sb.Append(body).AppendLine("    break;");
                _sb.Append(body).AppendLine("}");
            }

            _sb.Append(inner).AppendLine("}");
            _sb.Append(indent).AppendLine("}");
        }

        /// <summary>
        /// A run where a <c>minOccurs=0</c> bounded-repeating list sits mid-run (not the run's last
        /// particle), followed only by plain optional particles ending the sequence. cbexigen's actual
        /// generated grammar for this shape (verified byte-for-byte against
        /// <c>encode_iso20_wpt_WPT_FinePositioningReqType</c>, states 178-180 of
        /// <c>iso20_WPT_Encoder.c</c>) is surprising on two counts:
        /// <list type="number">
        ///   <item>the "zero items yet" state offers only [start first item] or [element EE] — the
        ///   particles <em>after</em> the list are unreachable unless at least one list item is written
        ///   first, unlike a normal optional run where every later particle is always reachable;</item>
        ///   <item>the list is hard-capped at <b>2</b> items here, regardless of its schema
        ///   <c>maxOccurs</c> (16 for <c>VendorSpecificDataContainer</c>) — cbexigen only unrolls two
        ///   positions before it must hand off to the following particles' states, so a third item can
        ///   never be represented in this position. This looks like a genuine cbexigen limitation for
        ///   this narrow construct (a trailing bounded list gets a real self-looping state instead, see
        ///   <see cref="EmitEncodeRepeatingItems"/>), not a deliberate design choice — but matching it is
        ///   the only way to stay byte-exact with the reference encoder, which is this repo's actual
        ///   interop target.</item>
        /// </list>
        /// Only a single suffix of plain optional particles is supported (no choice/substitution/wildcard,
        /// no particles before the list in the same run) — the only shape ISO 15118-20 WPT actually needs
        /// (<c>WPT_FinePositioningReqType</c>/<c>ResType</c>, <c>WPT_FinePositioningSetupReqType</c>/<c>ResType</c>).
        /// </summary>
        private void EmitEncodeOptionalRunWithMidList(IReadOnlyList<ChildPlan> children, int s, int listIdx, int e, string indent)
        {
            if (listIdx != s)
                throw new NotSupportedException(
                    $"repeating element '{children[listIdx].FieldName}' mid-run: particles before it in the same run are not supported.");
            var list = children[listIdx];
            if (list.ListMin != 0)
                throw new NotSupportedException(
                    $"repeating element '{list.FieldName}' mid-run must be optional (minOccurs=0).");
            if (e != children.Count)
                throw new NotSupportedException(
                    $"repeating element '{list.FieldName}' mid-run must be followed only by particles ending the sequence " +
                    "(a required/repeating terminator after it is not supported).");

            var suffix = new List<ChildPlan>();
            for (int p = listIdx + 1; p < e; p++)
            {
                if (children[p].Value is ValueEncoding.SubstitutionChoice or ValueEncoding.InlineChoice)
                    throw new NotSupportedException(
                        $"repeating element '{list.FieldName}' mid-run: suffix particle '{children[p].FieldName}' " +
                        "must be a plain optional element (choice/substitution suffixes are not supported).");
                suffix.Add(children[p]);
            }
            // Selecting a suffix particle writes the element EE and ends the run, so exactly one of
            // them can ever be encoded. With two, the second would be dropped silently — fail at
            // generation time rather than emit a codec that loses data.
            if (suffix.Count > 1)
                throw new NotSupportedException(
                    $"repeating element '{list.FieldName}' mid-run: {suffix.Count} following particles " +
                    "(only one is representable — choosing one ends the run).");

            int suffixTotal = 0;
            foreach (var sp in suffix) suffixTotal += ProductionCount(sp);

            string listExpr = "msg." + list.FieldName;
            int id = _runCounter++;
            string st = "_ost" + id, done = "_odone" + id;
            string inner = indent + "    ";
            string body = inner + "    ";
            string br = body + "    ";

            _sb.Append(indent).Append("if (").Append(listExpr).AppendLine(".Count > 2)");
            _sb.Append(indent).Append("    throw new ArgumentOutOfRangeException(nameof(msg), \"")
               .Append(list.FieldName).AppendLine(": cbV2G's grammar for this position caps this list at 2 items.\");");
            _sb.Append(indent).Append("int ").Append(st).AppendLine(" = 0;");
            _sb.Append(indent).Append("bool ").Append(done).AppendLine(" = false;");
            _sb.Append(indent).Append("while (!").Append(done).AppendLine(")");
            _sb.Append(indent).AppendLine("{");
            _sb.Append(inner).Append("switch (").Append(st).AppendLine(")");
            _sb.Append(inner).AppendLine("{");

            // State 0: zero items written — [start item 0] or [element EE]. The suffix is unreachable here.
            int w0 = BitsForChoices(1 + 1 + 1);
            _sb.Append(body).AppendLine("case 0:");
            _sb.Append(body).AppendLine("{");
            _sb.Append(br).Append("if (").Append(listExpr).AppendLine(".Count > 0)");
            _sb.Append(br).AppendLine("{");
            _sb.Append(br).Append("    w.WriteBits(0, ").Append(w0).Append(");   // ").AppendLine(list.FieldName);
            EmitEncodeContent(list, listExpr + "[0]", br + "    ");
            _sb.Append(br).Append("    ").Append(st).AppendLine(" = 1;");
            _sb.Append(br).AppendLine("}");
            _sb.Append(br).AppendLine("else");
            _sb.Append(br).AppendLine("{");
            // The suffix has no event code in this state, so a caller that set one would otherwise
            // have it dropped without a word. Refuse instead: silently losing a field the caller
            // asked for is worse than an exception naming exactly what cannot be represented.
            foreach (var sfx in suffix)
            {
                string presence = sfx.IsCsNullable() ? $"msg.{sfx.FieldName}.HasValue"
                                                     : $"msg.{sfx.FieldName} is not null";
                _sb.Append(br).Append("    if (").Append(presence).AppendLine(")");
                _sb.Append(br).Append("        throw new ArgumentException(\"").Append(sfx.FieldName)
                   .Append(" cannot be encoded while ").Append(list.FieldName)
                   .AppendLine(" is empty: cbV2G's grammar for this position only reaches it after at " +
                               "least one list item.\", nameof(msg));");
            }
            _sb.Append(br).Append("    w.WriteBits(1, ").Append(w0).AppendLine(");   // element EE");
            _sb.Append(br).Append("    ").Append(done).AppendLine(" = true;");
            _sb.Append(br).AppendLine("}");
            _sb.Append(body).AppendLine("    break;");
            _sb.Append(body).AppendLine("}");

            // State 1: one item written — [start item 1 (loop)], each suffix particle, or [element EE].
            int w1 = BitsForChoices(1 + suffixTotal + 1 + 1);
            _sb.Append(body).AppendLine("case 1:");
            _sb.Append(body).AppendLine("{");
            _sb.Append(br).Append("if (").Append(listExpr).AppendLine(".Count > 1)");
            _sb.Append(br).AppendLine("{");
            _sb.Append(br).Append("    w.WriteBits(0, ").Append(w1).Append(");   // ").AppendLine(list.FieldName);
            EmitEncodeContent(list, listExpr + "[1]", br + "    ");
            _sb.Append(br).Append("    ").Append(st).AppendLine(" = 2;");
            _sb.Append(br).AppendLine("}");
            {
                // Choosing a suffix particle still needs the outer element's own closing EE afterwards —
                // cbexigen inserts a dedicated one-bit-EE state after it (verified: WPT_FinePositioningReqType
                // grammar id 2), unlike this repo's normal optional-run terminator (where the *caller*
                // appends that EE, since it only fires when the whole run reports itself un-terminated).
                string afterSuffix = "w.WriteBits(0, 1);   // element EE\n" + br + done + " = true;";
                bool first = false;   // continues the if/else-if chain opened by the loop branch above
                int code = 1;
                foreach (var sp in suffix)
                    code = EmitEncodeRunParticle(sp, code, w1, ref first, br, afterSuffix);
                EmitEncodeRunTail(first, br, "w.WriteBits(" + code + ", " + w1 + ");   // element EE", done);
            }
            _sb.Append(body).AppendLine("    break;");
            _sb.Append(body).AppendLine("}");

            // State 2: two items written — the list is capped here; each suffix particle, or [element EE].
            int w2 = BitsForChoices(suffixTotal + 1 + 1);
            _sb.Append(body).AppendLine("case 2:");
            _sb.Append(body).AppendLine("{");
            {
                string afterSuffix = "w.WriteBits(0, 1);   // element EE\n" + br + done + " = true;";
                bool first = true;
                int code = 0;
                foreach (var sp in suffix)
                    code = EmitEncodeRunParticle(sp, code, w2, ref first, br, afterSuffix);
                EmitEncodeRunTail(first, br, "w.WriteBits(" + code + ", " + w2 + ");   // element EE", done);
            }
            _sb.Append(body).AppendLine("    break;");
            _sb.Append(body).AppendLine("}");

            _sb.Append(inner).AppendLine("}");
            _sb.Append(indent).AppendLine("}");
        }


        /// <summary>
        /// The same construct, given the grammar ISO's schema actually describes — see
        /// <see cref="ParticleGrammar.SchemaConformant"/>.
        ///
        /// <para>
        /// Simpler than the cbexigen shape it replaces, because there is nothing to unroll: the list
        /// loops to its own <c>maxOccurs</c>, and every state offers the same three choices — another
        /// item, the following optional particle, or the end element. cbexigen instead hides the suffix
        /// in the zero-item state and runs out of states after two items.
        /// </para>
        /// </summary>
        private void EmitEncodeOptionalRunWithMidListSchema(IReadOnlyList<ChildPlan> children, int s, int listIdx,
                                                            int e, string indent)
        {
            var (list, suffix, suffixTotal) = MidListShape(children, s, listIdx, e);

            string listExpr = "msg." + list.FieldName;
            int width = BitsForChoices(1 + suffixTotal + 1 + 1);
            string inner = indent + "    ";

            if (list.ListMax > 0)
            {
                _sb.Append(indent).Append("if (").Append(listExpr).Append(".Count > ").Append(list.ListMax).AppendLine(")");
                _sb.Append(indent).Append("    throw new ArgumentOutOfRangeException(nameof(msg), \"")
                   .Append(list.FieldName).Append(": at most ").Append(list.ListMax)
                   .AppendLine(" item(s) per the schema.\");");
            }

            _sb.Append(indent).Append("foreach (var _item in ").Append(listExpr).AppendLine(")");
            _sb.Append(indent).AppendLine("{");
            _sb.Append(inner).Append("w.WriteBits(0, ").Append(width).Append(");   // ").AppendLine(list.FieldName);
            EmitEncodeContent(list, "_item", inner);
            _sb.Append(indent).AppendLine("}");

            {
                string afterSuffix = "w.WriteBits(0, 1);   // element EE";
                bool first = true;
                int code = 1;
                foreach (var sp in suffix)
                    code = EmitEncodeRunParticle(sp, code, width, ref first, indent, afterSuffix);
                if (!first) _sb.Append(indent).AppendLine("else");
                _sb.Append(indent).Append(first ? "" : "    ")
                   .Append("w.WriteBits(").Append(code).Append(", ").Append(width).AppendLine(");   // element EE");
            }
        }


        /// <summary>Shared preconditions and shape of the mid-list run, for both particle grammars.</summary>
        private (ChildPlan List, List<ChildPlan> Suffix, int SuffixTotal) MidListShape(
            IReadOnlyList<ChildPlan> children, int s, int listIdx, int e)
        {
            if (listIdx != s)
                throw new NotSupportedException(
                    $"repeating element '{children[listIdx].FieldName}' mid-run: particles before it in the same run are not supported.");
            var list = children[listIdx];
            if (list.ListMin != 0)
                throw new NotSupportedException(
                    $"repeating element '{list.FieldName}' mid-run must be optional (minOccurs=0).");
            if (e != children.Count)
                throw new NotSupportedException(
                    $"repeating element '{list.FieldName}' mid-run must be followed only by particles ending the sequence " +
                    "(a required/repeating terminator after it is not supported).");

            var suffix = new List<ChildPlan>();
            for (int p = listIdx + 1; p < e; p++)
            {
                if (children[p].Value is ValueEncoding.SubstitutionChoice or ValueEncoding.InlineChoice)
                    throw new NotSupportedException(
                        $"repeating element '{list.FieldName}' mid-run: suffix particle '{children[p].FieldName}' " +
                        "must be a plain optional element (choice/substitution suffixes are not supported).");
                suffix.Add(children[p]);
            }
            if (suffix.Count > 1)
                throw new NotSupportedException(
                    $"repeating element '{list.FieldName}' mid-run: {suffix.Count} following particles " +
                    "(only one is representable — choosing one ends the run).");

            int suffixTotal = 0;
            foreach (var sp in suffix) suffixTotal += ProductionCount(sp);
            return (list, suffix, suffixTotal);
        }

        /// <summary>Emits the presence/type-dispatch branch(es) for one run particle: an optional
        /// element (one production), or a substitution reference (one production per concrete member,
        /// with the abstract head reserving a code slot but no branch). Returns the next event code.</summary>
        private int EmitEncodeRunParticle(ChildPlan p, int code, int width, ref bool first, string indent, string after)
        {
            if (p.Shape == ChildShape.BoundedRepeating)
            {
                // First item takes this state's event code; further items and the terminating EE use the
                // 2-bit loop code {item = 0, EE = 1} (cbexigen model). Optional member: guarded by a
                // non-empty check (an empty list means the element is absent).
                string list = "msg." + p.FieldName;
                _sb.Append(indent).Append(first ? "if" : "else if").Append(" (").Append(list).AppendLine(".Count > 0)");
                _sb.Append(indent).AppendLine("{");
                _sb.Append(indent).Append("    if (").Append(list).Append(".Count > ").Append(p.ListMax)
                   .AppendLine(") throw new ArgumentOutOfRangeException(nameof(msg));");
                EmitEncodeRepeatingItems(p, code, width, indent + "    ", after);
                _sb.Append(indent).AppendLine("}");
                first = false;
                return code + 1;
            }

            if (p.Value is ValueEncoding.SubstitutionChoice sc)
            {
                // Same most-derived-first reordering as EmitEncodeSubstitution (see its comment) — the
                // wire code is precomputed per member from its original position (code + offset) so
                // reordering the if/else-if emission doesn't disturb it.
                int baseCode = code;
                var ordered = sc.Members
                    .Select((m, k) => (Member: m, WireCode: baseCode + k))
                    .Where(x => !x.Member.IsAbstractHead)
                    .OrderByDescending(x => InheritanceDepth(x.Member.TypeName));
                foreach (var (mbr, wireCode) in ordered)
                {
                    string v = "v" + wireCode;   // unique per branch (pattern variables share the case scope)
                    _sb.Append(indent).Append(first ? "if" : "else if")
                       .Append(" (msg.").Append(p.FieldName).Append(" is ").Append(mbr.TypeName)
                       .Append(' ').Append(v).AppendLine(")");
                    _sb.Append(indent).AppendLine("{");
                    EmitSubstitutionMemberGuard(p, mbr.TypeName, v, indent + "    ");
                    _sb.Append(indent).Append("    w.WriteBits(").Append(wireCode).Append(", ").Append(width)
                       .Append(");   // ").AppendLine(mbr.ElementName);
                    _sb.Append(indent).Append("    Encode_").Append(mbr.TypeName).Append("(ref w, ").Append(v).AppendLine(");");
                    _sb.Append(indent).Append("    ").AppendLine(after);
                    _sb.Append(indent).AppendLine("}");
                    first = false;
                }
                return code + sc.Members.Count;
            }

            if (p.Value is ValueEncoding.InlineChoice ic)
            {
                // Each branch is its own independent field (cbexigen: N sibling _isUsed-flagged fields,
                // not one polymorphic field as for a substitution reference) — see EmitEncodeInlineChoiceStandalone
                // for the presence-check style this mirrors.
                foreach (var mbr in ic.Members)
                {
                    string maccessor = mbr.IsCsNullable() ? $"msg.{mbr.FieldName}!.Value" : $"msg.{mbr.FieldName}!";
                    _sb.Append(indent).Append(first ? "if" : "else if")
                       .Append(" (msg.").Append(mbr.FieldName).AppendLine(" is not null)");
                    _sb.Append(indent).AppendLine("{");
                    _sb.Append(indent).Append("    w.WriteBits(").Append(code).Append(", ").Append(width)
                       .Append(");   // ").AppendLine(mbr.ElementName);
                    EmitEncodeContent(AsChildPlan(mbr), maccessor, indent + "    ");
                    _sb.Append(indent).Append("    ").AppendLine(after);
                    _sb.Append(indent).AppendLine("}");
                    first = false; code++;
                }
                return code;
            }

            string presence = p.IsCsNullable() ? $"msg.{p.FieldName}.HasValue" : $"msg.{p.FieldName} is not null";
            string accessor = p.IsCsNullable() ? $"msg.{p.FieldName}!.Value" : $"msg.{p.FieldName}!";
            _sb.Append(indent).Append(first ? "if" : "else if").Append(" (").Append(presence).AppendLine(")");
            _sb.Append(indent).AppendLine("{");
            _sb.Append(indent).Append("    w.WriteBits(").Append(code).Append(", ").Append(width)
               .Append(");   // ").AppendLine(p.FieldName);
            EmitEncodeContent(p, accessor, indent + "    ");
            _sb.Append(indent).Append("    ").AppendLine(after);
            _sb.Append(indent).AppendLine("}");
            first = false;
            return code + 1;
        }

        /// <summary>A required or optional inline <c>xs:choice</c> with no adjacent optional siblings to
        /// flatten into (see <see cref="EmitEncodeRunParticle"/>/<see cref="EmitEncodeOptionalRun"/> for
        /// that case): N sibling nullable fields, exactly one set; an n-bit code selects it, content
        /// follows directly (no SE wrapper — the code IS the selector, cbexigen's flattened-choice model,
        /// distinct from substitution-group's single polymorphic field).</summary>
        private void EmitEncodeInlineChoiceStandalone(ValueEncoding.InlineChoice ic)
        {
            for (int i = 0; i < ic.Members.Count; i++)
            {
                var m = ic.Members[i];
                string accessor = m.IsCsNullable() ? $"msg.{m.FieldName}!.Value" : $"msg.{m.FieldName}!";
                _sb.Append("        ").Append(i == 0 ? "if" : "else if")
                   .Append(" (msg.").Append(m.FieldName).AppendLine(" is not null)");
                _sb.AppendLine("        {");
                _sb.Append("            w.WriteBits(").Append(i).Append(", ").Append(ic.BitWidth)
                   .Append(");   // ").AppendLine(m.ElementName);
                EmitEncodeContent(AsChildPlan(m), accessor, "            ");
                _sb.AppendLine("        }");
            }
            _sb.AppendLine("        else throw new ArgumentException(\"no choice alternative set\");");
        }

        /// <summary>Emits the terminator (element EE, or a required simple/complex element) as either a
        /// standalone block (when no optional branch preceded it) or the trailing <c>else</c>.</summary>
        private void EmitEncodeRunTail(bool first, string indent, string writeBits, string done, ChildPlan? content = null)
        {
            if (!first) _sb.Append(indent).AppendLine("else");
            _sb.Append(indent).AppendLine("{");
            _sb.Append(indent).Append("    ").Append(writeBits).AppendLine();
            if (content is not null)
                EmitEncodeContent(content, "msg." + content.FieldName, indent + "    ");
            _sb.Append(indent).Append("    ").Append(done).AppendLine(" = true;");
            _sb.Append(indent).AppendLine("}");
        }

        /// <summary>Emits the wire form of a bounded-repeating element: the first item at
        /// <paramref name="firstCode"/>/<paramref name="width"/> (its grammar-state event code), then
        /// each further item and the terminating EE at the 2-bit loop code {item = 0, EE = 1}.</summary>
        private void EmitEncodeRepeatingItems(ChildPlan p, int firstCode, int width, string indent, string after)
        {
            string list = "msg." + p.FieldName;
            _sb.Append(indent).Append("w.WriteBits(").Append(firstCode).Append(", ").Append(width)
               .Append(");   // ").AppendLine(p.FieldName);
            EmitEncodeContent(p, list + "[0]", indent);
            int forced = ForcedOccurrences(p.ListMin);
            _sb.Append(indent).Append("for (int ci = 1; ci < ").Append(list).AppendLine(".Count; ci++)");
            _sb.Append(indent).AppendLine("{");
            if (forced <= 1)
                _sb.Append(indent).Append("    w.WriteBits(0, 2);   // ").AppendLine(p.FieldName);
            else
                _sb.Append(indent).Append("    w.WriteBits(0, ci < ").Append(forced).Append(" ? 1 : 2);   // ")
                   .Append(p.FieldName).AppendLine(" (1-bit while forced by minOccurs)");
            EmitEncodeContent(p, list + "[ci]", indent + "    ");
            _sb.Append(indent).AppendLine("}");
            _sb.Append(indent).AppendLine("w.WriteBits(1, 2);   // element EE (list end)");
            _sb.Append(indent).Append(after).AppendLine();
        }

        /// <summary>Terminator variant of <see cref="EmitEncodeRepeatingItems"/>: a required
        /// (<c>minOccurs≥1</c>) repeating element that ends the run — emitted unconditionally as the
        /// trailing <c>else</c> (all optionals absent) or a standalone block.</summary>
        private void EmitEncodeRunTailRepeating(bool first, string indent, ChildPlan term, int code, int width, string done)
        {
            string list = "msg." + term.FieldName;
            if (!first) _sb.Append(indent).AppendLine("else");
            _sb.Append(indent).AppendLine("{");
            _sb.Append(indent).Append("    if (").Append(list).Append(".Count is < ").Append(ForcedOccurrences(term.ListMin))
               .Append(" or > ").Append(term.ListMax)
               .AppendLine(") throw new ArgumentOutOfRangeException(nameof(msg));");
            EmitEncodeRepeatingItems(term, code, width, indent + "    ", done + " = true;");
            _sb.Append(indent).AppendLine("}");
        }

        /// <summary>
        /// Emits the EE that terminates a repeating element. A list that has reached its
        /// <c>maxOccurs</c> is in a state whose only production is the end-element, so the EE there is
        /// a 1-bit code; at any shorter length the state still offers another item as well, and the EE
        /// is the 2-bit loop code.
        /// <para>
        /// This used to be written only for <c>maxOccurs=2</c>, where cbexigen's bounded unroll made it
        /// visible (verified against PaymentOptionListType vs SupportedEnergyTransferModeType). It is
        /// the general rule: EXIficient encoding a synthetic <c>maxOccurs="10"</c> schema ends a
        /// ten-item list with a single zero bit, not the two-bit loop EE (2026-08-07, alongside the
        /// <see cref="ForcedOccurrences"/> finding). Truly unbounded lists have no such state and keep
        /// the loop EE.
        /// </para>
        /// </summary>
        private void EmitEncodeListTerminator(string indent, string listExpr, int listMax)
        {
            if (listMax != int.MaxValue)
            {
                _sb.Append(indent).Append("if (").Append(listExpr).Append(".Count >= ").Append(listMax)
                   .AppendLine(") w.WriteBits(0, 1);   // element EE (list at max)");
                _sb.Append(indent).AppendLine("else w.WriteBits(1, 2);   // element EE");
            }
            else
            {
                _sb.Append(indent).AppendLine("w.WriteBits(1, 2);   // list terminator / element EE");
            }
        }

        /// <summary>Decode counterpart of <see cref="EmitEncodeListTerminator"/>: once the list holds
        /// its maximum, the terminator is the 1-bit END of the max-reached state, not the 2-bit loop
        /// EE. Emitted at the top of the decode loop; a no-op for unbounded lists.</summary>
        private void EmitDecodeListMaxCheck(string indent, string listExpr, int listMax)
        {
            if (listMax != int.MaxValue)
                _sb.Append(indent).Append("if (").Append(listExpr).Append(".Count >= ").Append(listMax)
                   .AppendLine(") { r.ReadBits(1); break; }   // element EE (list at max)");
        }

        private static int ProductionCount(ChildPlan c) =>
            c.Value is ValueEncoding.SubstitutionChoice sc ? sc.Members.Count
            : c.Value is ValueEncoding.InlineChoice ic ? ic.Members.Count
            : c.IsWildcardAny ? 2   // xs:any → generic wildcard event + typed element (cbexigen)
            : 1;

        /// <summary>Wraps an <see cref="InlineChoiceMember"/> as a throwaway <see cref="ChildPlan"/> so it
        /// can be fed through <see cref="EmitEncodeContent"/>/<see cref="EmitDecodeContent"/>, which only
        /// ever read <c>Value</c> from the plan they're given.</summary>
        private static ChildPlan AsChildPlan(InlineChoiceMember m) =>
            new(m.FieldName, m.Type, m.IsValueType, ChildShape.RequiredSingle, m.Value);

        /// <summary>
        /// The exclusive end of the optional run starting at <paramref name="i"/>: consecutive
        /// <see cref="ChildShape.OptionalSingle"/> particles, plus one optional (<c>minOccurs=0</c>)
        /// bounded-repeating list — its first item is a production of the run's grammar state and
        /// further items loop (cbexigen model, verified against SalesTariffEntryType) — followed by
        /// more consecutive <see cref="ChildShape.OptionalSingle"/> particles, if the list isn't last
        /// (WPT_FinePositioningReqType's <c>VendorSpecificDataContainer, WPT_LF_DataPackageList?</c>;
        /// see <see cref="EmitEncodeOptionalRunWithMidList"/> for the quirky grammar this produces).
        /// </summary>
        private static int RunEnd(IReadOnlyList<ChildPlan> children, int i)
        {
            int j = i;
            while (j < children.Count && children[j].Shape == ChildShape.OptionalSingle) j++;
            if (j < children.Count && children[j].Shape == ChildShape.BoundedRepeating && children[j].ListMin == 0)
            {
                j++;
                while (j < children.Count && children[j].Shape == ChildShape.OptionalSingle) j++;
            }
            return j;
        }

        /// <summary>
        /// xs:choice: exactly one alternative is set. An n-bit event code (declaration order)
        /// selects it, followed by its content (no surrounding SE — the code is the selector).
        /// </summary>
        private void EmitEncodeChoice(SequencePlan sp)
        {
            int width = BitsForChoices(sp.Children.Count + 1);
            for (int i = 0; i < sp.Children.Count; i++)
            {
                var c = sp.Children[i];
                string accessor = c.IsCsNullable() ? $"msg.{c.FieldName}!.Value" : $"msg.{c.FieldName}!";
                _sb.Append("        ").Append(i == 0 ? "if" : "else if")
                   .Append(" (msg.").Append(c.FieldName).AppendLine(" is not null)");
                _sb.AppendLine("        {");
                _sb.Append("            w.WriteBits(").Append(i).Append(", ").Append(width)
                   .Append(");   // ").AppendLine(c.FieldName);
                EmitEncodeContent(c, accessor, "            ");
                _sb.AppendLine("        }");
            }
            _sb.Append("        else throw new ArgumentException(\"no choice alternative set for ")
               .Append(sp.RecordName).AppendLine("\");");
        }

        /// <summary>
        /// Emits the content of a child element after its SE event has been written:
        /// for a complex child the nested encode call (which contains its own EE);
        /// for a simple child the value-start event, the value, and the child EE.
        /// </summary>
        private void EmitEncodeContent(ChildPlan c, string accessor, string indent)
        {
            if (c.Value is ValueEncoding.AttributeValue)
            {
                // AT value: a bare string, no value-start / child-EE (the run's event code was the AT event).
                _sb.Append(indent).Append("ExiPrimitives.WriteStringValue(ref w, ").Append(accessor).AppendLine(");");
            }
            else if (c.Value is ValueEncoding.OpaqueElement oe)
            {
                // Reached only if a present (signed) instance is supplied — not modelled in Phase 2.
                _sb.Append(indent).Append("throw new NotSupportedException(\"Encoding a present ")
                   .Append(oe.TypeName).AppendLine(" (XMLDSig) is deferred to Phase 3.\");");
            }
            else if (c.Value is ValueEncoding.ComplexRef cr)
            {
                _sb.Append(indent).Append("Encode_").Append(cr.TypeName)
                   .Append("(ref w, ").Append(accessor).AppendLine(");");
            }
            else
            {
                _sb.Append(indent).AppendLine("w.WriteBits(0, 1);   // value-start");
                EmitWriteValue(c, accessor, indent);
                _sb.Append(indent).AppendLine("w.WriteBits(0, 1);   // child EE");
            }
        }

        /// <summary>
        /// Substitution-group choice: an n-bit event code selects the concrete member type, then
        /// its content is encoded directly (no surrounding SE/EE — the event code IS the selector).
        /// Dispatch is by runtime type; the abstract head has a production slot but no case.
        /// </summary>
        private void EmitEncodeSubstitution(ChildPlan c, ValueEncoding.SubstitutionChoice sc)
        {
            _sb.Append("        switch (msg.").Append(c.FieldName).AppendLine(")");
            _sb.AppendLine("        {");
            // C# type-pattern matching shadowing: ISO 15118-20 substitution members can extend EACH
            // OTHER (not just the common abstract head, e.g. BPT_AC_CPDReqEnergyTransferModeType :
            // AC_CPDReqEnergyTransferModeType) — a base type's `case` also matches derived instances, so
            // it must come AFTER (not before) any of its own derived members' cases, or the derived
            // case becomes unreachable (CS8120). Emit most-derived-first; the wire event code still
            // comes from each member's original (alphabetical, cbV2G-verified) position in sc.Members.
            var ordered = sc.Members
                .Select((m, i) => (Member: m, Code: i))
                .Where(x => !x.Member.IsAbstractHead)
                .OrderByDescending(x => InheritanceDepth(x.Member.TypeName));
            foreach (var (m, code) in ordered)
            {
                _sb.Append("            case ").Append(m.TypeName).Append(" v:");
                _sb.AppendLine();
                EmitSubstitutionMemberGuard(c, m.TypeName, "v", "                ");
                _sb.Append("                w.WriteBits(").Append(code).Append(", ").Append(sc.BitWidth)
                   .Append(");   // ").Append(m.ElementName).AppendLine();
                _sb.Append("                Encode_").Append(m.TypeName).AppendLine("(ref w, v);");
                _sb.AppendLine("                break;");
            }
            _sb.Append("            default: throw new ArgumentException(\"Unsupported substitution member for ")
               .Append(c.FieldName).AppendLine("\");");
            _sb.AppendLine("        }");
        }

        /// <summary>
        /// Requires the value to be *exactly* the member type its branch selected, not merely
        /// assignable to it.
        /// </summary>
        /// <remarks>
        /// A type pattern matches derived instances too — that is why these branches are ordered
        /// most-derived-first. Every type the schema set derives from a member is itself a member,
        /// so the branches partition the generated types exactly; but the generated records are
        /// public and nothing stops application code deriving from one. Such a value would take its
        /// nearest ancestor's branch and be written with that member's event code and encoder,
        /// silently encoding something the caller never asked for — and in an optional run it can
        /// match no branch at all and vanish from the message. The Kotlin back end carries the same
        /// guard.
        /// </remarks>
        private void EmitSubstitutionMemberGuard(ChildPlan c, string typeName, string value, string indent)
        {
            // A leaf record is sealed, so its type pattern already means "exactly this type" and the
            // check would be dead code. Only the records something extends — and abstract ones — can
            // be derived from, here or by a consumer.
            var extensible = _baseRecordNames.Contains(typeName)
                             || (_byRecordName.TryGetValue(typeName, out var sp) && sp.IsAbstract);
            if (!extensible)
                return;

            _sb.Append(indent).Append("if (").Append(value).Append(".GetType() != typeof(")
               .Append(typeName).AppendLine("))");
            _sb.Append(indent).Append("    throw new ArgumentException($\"").Append(c.FieldName)
               .Append(": {").Append(value).AppendLine(".GetType().Name} is not a substitution member\");");
        }

        /// <summary>How many <c>BaseRecordName</c> links separate <paramref name="typeName"/> from its
        /// root (0 for a type with no base). Used to order substitution-choice <c>case</c>/<c>if</c>
        /// branches most-derived-first, since C# type patterns on a base type also match derived
        /// instances.</summary>
        private int InheritanceDepth(string typeName)
        {
            int depth = 0;
            var current = typeName;
            while (_byRecordName.TryGetValue(current, out var sp) && sp.BaseRecordName is not null)
            {
                depth++;
                current = sp.BaseRecordName;
            }
            return depth;
        }

        // -----------------------------------------------------------------------
        //  Attributes (AT events): a single optional attribute preceding the content.
        //  The initial grammar state chooses between AT(attr) and SE(first content); when the
        //  attribute is absent the same n-bit code doubles as that first SE (cbexigen model,
        //  verified against CertificateChainType). Attribute values carry no value-start bit.
        // -----------------------------------------------------------------------

        /// <summary>
        /// A repeating element occurring as the last child of a sequence: the first item is
        /// preceded by a 1-bit SE, each further item by a 2-bit loop code, and a 2-bit code
        /// terminates the list (which also serves as the element EE).
        /// </summary>
        private void EmitEncodeRepeatingChild(ChildPlan c, string indent)
        {
            string list = c.FieldName + "_list";
            _sb.Append(indent).Append("var ").Append(list).Append(" = msg.").Append(c.FieldName).AppendLine(";");
            _sb.Append(indent).Append("if (").Append(list).Append(".Count is < ").Append(Math.Max(1, c.ListMin))
               .Append(" or > ").Append(c.ListMax).AppendLine(") throw new ArgumentOutOfRangeException(nameof(msg));");
            int forced = ForcedOccurrences(c.ListMin);
            _sb.Append(indent).Append("for (int i = 0; i < ").Append(list).AppendLine(".Count; i++)");
            _sb.Append(indent).AppendLine("{");
            _sb.Append(indent).Append("    w.WriteBits(0, ").Append(SeWidthExpr("i", forced)).Append(");")
               .AppendLine(SeWidthComment(forced));
            EmitEncodeContent(c, list + "[i]", indent + "    ");
            _sb.Append(indent).AppendLine("}");
            EmitEncodeListTerminator(indent, list, c.ListMax);
        }

        /// <summary>
        /// A required (<c>minOccurs≥1</c>) bounded-repeating list followed by exactly one more particle,
        /// either required (ISO 15118-20's <c>AuthorizationSetupResType.AuthorizationServices</c> →
        /// <c>CertificateInstallationService</c>, <c>maxOccurs=2</c>, unrolled two-position grammar
        /// verified against cbV2G) or optional (<c>WPT_LF_TransmitterDataType.TxSpecData</c> →
        /// <c>TxPackageSpecData?</c>, <c>maxOccurs=255</c>, a true self-loop).
        /// <para>
        /// <b>The optional-tail, large-<c>maxOccurs</c> shape is an independent design, not reverse
        /// engineered</b>: cbV2G/cbexigen's own generated encoder for this exact construct
        /// (<c>encode_iso20_wpt_WPT_LF_TransmitterDataType</c>, states 81/82) cannot represent it —
        /// empirically confirmed to fail with <c>EXI_ERROR__UNKNOWN_EVENT_CODE</c> even at the schema's
        /// own required minimum of 2 <c>TxSpecData</c> items (state 82 loops forever with no exit
        /// production). With no working reference to diff against, this emits the straightforward
        /// schema-informed-non-strict-grammar reading instead: first item unconditional, then a true
        /// self-loop offering [loop, tail, element EE] every iteration.
        /// </para>
        /// </summary>
        private void EmitEncodeRequiredRepeatingWithTail(ChildPlan list, ChildPlan tail, string indent)
        {
            bool tailRequired = tail.Shape == ChildShape.RequiredSingle;
            if (!tailRequired && tail.Value is ValueEncoding.SubstitutionChoice or ValueEncoding.InlineChoice)
                throw new NotSupportedException(
                    $"required repeating '{list.FieldName}': an optional choice/substitution tail is not supported.");

            string listExpr = "msg." + list.FieldName;

            if (list.ListMax == 2)
            {
                // The original, cbV2G-verified unrolled-two-position grammar (required tail only —
                // the only combination this shape has ever needed).
                if (!tailRequired)
                    throw new NotSupportedException(
                        $"required repeating '{list.FieldName}' (maxOccurs=2): an optional tail is not supported " +
                        "for the unrolled two-position grammar (only observed with a required tail so far).");
                // A forced second occurrence would narrow the mid code to one bit, as it does in the
                // self-loop below. No ISO 15118 particle combines minOccurs≥2 with maxOccurs=2, so rather
                // than guess at how that interacts with the cbV2G-verified unroll, refuse to generate it.
                if (ForcedOccurrences(list.ListMin) > 1)
                    throw new NotSupportedException(
                        $"required repeating '{list.FieldName}' (minOccurs={list.ListMin}, maxOccurs=2): " +
                        "a forced second occurrence in the unrolled two-position grammar is unverified.");
                _sb.Append(indent).Append("if (").Append(listExpr).AppendLine(".Count is < 1 or > 2) throw new ArgumentOutOfRangeException(nameof(msg));");
                _sb.Append(indent).Append("w.WriteBits(0, 1);   // SE(").Append(list.FieldName).AppendLine(")");
                EmitEncodeContent(list, listExpr + "[0]", indent);

                int tailProd = ProductionCount(tail);
                int widthMid = BitsForChoices((1 + tailProd) + 1);   // continue + tail productions + phantom
                int widthMax = BitsForChoices(tailProd + 1);         // list at max: only the tail remains

                _sb.Append(indent).Append("if (").Append(listExpr).AppendLine(".Count > 1)");
                _sb.Append(indent).AppendLine("{");
                _sb.Append(indent).Append("    w.WriteBits(0, ").Append(widthMid).Append(");   // ")
                   .Append(list.FieldName).AppendLine(" (loop)");
                EmitEncodeContent(list, listExpr + "[1]", indent + "    ");
                EmitEncodeRequiredTailDispatch(tail, 0, widthMax, indent + "    ");
                _sb.Append(indent).AppendLine("}");
                _sb.Append(indent).AppendLine("else");
                _sb.Append(indent).AppendLine("{");
                EmitEncodeRequiredTailDispatch(tail, 1, widthMid, indent + "    ");
                _sb.Append(indent).AppendLine("}");
                return;
            }

            // True self-loop (own design, see class doc above): item 0 unconditional, then every further
            // item shares its event code with [tail-start] and, for an optional tail, [element EE].
            _sb.Append(indent).Append("if (").Append(listExpr).Append(".Count is < ").Append(Math.Max(1, list.ListMin))
               .Append(" or > ").Append(list.ListMax).AppendLine(") throw new ArgumentOutOfRangeException(nameof(msg));");
            _sb.Append(indent).Append("w.WriteBits(0, 1);   // SE(").Append(list.FieldName).AppendLine(") first");
            EmitEncodeContent(list, listExpr + "[0]", indent);

            int prod = ProductionCount(tail);
            int width = BitsForChoices(1 + prod + (tailRequired ? 0 : 1) + 1);   // loop + tail (+ EE) + reserved

            int forcedItems = ForcedOccurrences(list.ListMin);
            _sb.Append(indent).Append("for (int ci = 1; ci < ").Append(listExpr).AppendLine(".Count; ci++)");
            _sb.Append(indent).AppendLine("{");
            if (forcedItems <= 1)
                _sb.Append(indent).Append("    w.WriteBits(0, ").Append(width).Append(");   // ")
                   .Append(list.FieldName).AppendLine(" (loop)");
            else
                // Until minOccurs is met neither the tail nor the EE is reachable, so the state has the
                // single production SE(item) — one bit, not the loop's width. See ForcedOccurrences.
                _sb.Append(indent).Append("    w.WriteBits(0, ci < ").Append(forcedItems).Append(" ? 1 : ")
                   .Append(width).Append(");   // ").Append(list.FieldName)
                   .AppendLine(" (1-bit while forced by minOccurs, then loop)");
            EmitEncodeContent(list, listExpr + "[ci]", indent + "    ");
            _sb.Append(indent).AppendLine("}");

            if (tailRequired)
            {
                EmitEncodeRequiredTailDispatch(tail, 1, width, indent);
                return;
            }

            // Optional tail: this is the sequence's last particle, so unlike the required-tail case, this
            // construct must close the element itself — the caller only adds the fallback EE when it
            // wasn't already written here (see the call site's `terminated` computation).
            string presence = tail.IsCsNullable() ? $"msg.{tail.FieldName}.HasValue" : $"msg.{tail.FieldName} is not null";
            string accessor = tail.IsCsNullable() ? $"msg.{tail.FieldName}!.Value" : $"msg.{tail.FieldName}!";
            _sb.Append(indent).Append("if (").Append(presence).AppendLine(")");
            _sb.Append(indent).AppendLine("{");
            _sb.Append(indent).Append("    w.WriteBits(1, ").Append(width).Append(");   // ").AppendLine(tail.FieldName);
            EmitEncodeContent(tail, accessor, indent + "    ");
            _sb.Append(indent).AppendLine("    w.WriteBits(0, 1);   // element EE");
            _sb.Append(indent).AppendLine("}");
            _sb.Append(indent).AppendLine("else");
            _sb.Append(indent).Append("    w.WriteBits(2, ").Append(width).AppendLine(");   // element EE");
        }

        /// <summary>
        /// Emits the tail of <see cref="EmitEncodeRequiredRepeatingWithTail"/>: a choice/substitution
        /// tail has several branches, each with its own presence check (<see cref="EmitEncodeRunParticle"/>
        /// already handles that); a plain required tail has exactly one, and — being required, possibly
        /// a non-nullable value type (e.g. a required <c>bool</c>) — must be written unconditionally, not
        /// behind an <c>is not null</c> presence check (which does not compile for a non-nullable value
        /// type and would be redundant for a reference type anyway).
        /// </summary>
        private void EmitEncodeRequiredTailDispatch(ChildPlan tail, int code, int width, string indent)
        {
            if (tail.Value is ValueEncoding.SubstitutionChoice or ValueEncoding.InlineChoice)
            {
                bool first = true;
                EmitEncodeRunParticle(tail, code, width, ref first, indent, "");
                return;
            }
            _sb.Append(indent).Append("w.WriteBits(").Append(code).Append(", ").Append(width)
               .Append(");   // ").AppendLine(tail.FieldName);
            EmitEncodeContent(tail, "msg." + tail.FieldName, indent);
        }

        private void EmitDecodeRepeatingChild(ChildPlan c, string local, string indent)
        {
            _sb.Append(indent).Append("var ").Append(local).Append(" = new List<").Append(c.CsType()).AppendLine(">();");
            _sb.Append(indent).AppendLine("r.ReadBits(1);   // SE(item) first");
            EmitDecodeContent(c, local + "_first", indent, declare: true);
            _sb.Append(indent).Append(local).Append(".Add(").Append(local).AppendLine("_first);");
            for (int f = 1; f < ForcedOccurrences(c.ListMin); f++)
            {
                _sb.Append(indent).Append("r.ReadBits(1);   // SE(item): forced by minOccurs=")
                   .Append(c.ListMin).AppendLine();
                EmitDecodeContent(c, local + "_forced" + f, indent, declare: true);
                _sb.Append(indent).Append(local).Append(".Add(").Append(local).Append("_forced").Append(f).AppendLine(");");
            }
            _sb.Append(indent).AppendLine("while (true)");
            _sb.Append(indent).AppendLine("{");
            EmitDecodeListMaxCheck(indent + "    ", local, c.ListMax);
            _sb.Append(indent).AppendLine("    uint ec = r.ReadBits(2);");
            _sb.Append(indent).AppendLine("    if (ec == 1) break;   // element EE");
            _sb.Append(indent).Append("    if (ec != 0 || ").Append(local).Append(".Count >= ").Append(c.ListMax)
               .AppendLine(") throw new InvalidDataException(\"invalid repeating-element event code\");");
            EmitDecodeContent(c, local + "_next", indent + "    ", declare: true);
            _sb.Append(indent).Append("    ").Append(local).Append(".Add(").Append(local).AppendLine("_next);");
            _sb.Append(indent).AppendLine("}");
        }

        /// <summary>Decode side of <see cref="EmitEncodeRequiredRepeatingWithTail"/> — see there for the
        /// two supported shapes (cbV2G-verified <c>maxOccurs=2</c>/required-tail unroll, and the
        /// independently-designed true self-loop for a larger <c>maxOccurs</c>/optional tail).</summary>
        private void EmitDecodeRequiredRepeatingWithTail(ChildPlan list, ChildPlan tail, string indent, List<string> locals)
        {
            bool tailRequired = tail.Shape == ChildShape.RequiredSingle;
            if (!tailRequired && tail.Value is ValueEncoding.SubstitutionChoice or ValueEncoding.InlineChoice)
                throw new NotSupportedException(
                    $"required repeating '{list.FieldName}': an optional choice/substitution tail is not supported.");

            string local = "_" + list.FieldName;
            _sb.Append(indent).Append("var ").Append(local).Append(" = new List<").Append(list.CsType()).AppendLine(">();");
            locals.Add(local);
            _sb.Append(indent).AppendLine("r.ReadBits(1);   // SE(" + list.FieldName + ") first");
            EmitDecodeContent(list, local + "0", indent, declare: true);
            _sb.Append(indent).Append(local).Append(".Add(").Append(local).AppendLine("0);");

            if (tail.Value is ValueEncoding.InlineChoice ic)
                DeclareInlineChoiceLocals(ic, indent, locals);
            else if (tailRequired)
            {
                _sb.Append(indent).Append(tail.CsType()).Append(" _").Append(tail.FieldName).AppendLine(" = default!;");
                locals.Add("_" + tail.FieldName);
            }
            else
            {
                _sb.Append(indent).Append(tail.CsType()).Append("? _").Append(tail.FieldName).AppendLine(" = default;");
                locals.Add("_" + tail.FieldName);
            }

            if (list.ListMax == 2)
            {
                if (!tailRequired)
                    throw new NotSupportedException(
                        $"required repeating '{list.FieldName}' (maxOccurs=2): an optional tail is not supported " +
                        "for the unrolled two-position grammar (only observed with a required tail so far).");

                int tailProd = ProductionCount(tail);
                int widthMid = BitsForChoices((1 + tailProd) + 1);
                int widthMax = BitsForChoices(tailProd + 1);

                _sb.Append(indent).Append("switch (r.ReadBits(").Append(widthMid).AppendLine("))");
                _sb.Append(indent).AppendLine("{");
                _sb.Append(indent).AppendLine("    case 0u:");
                _sb.Append(indent).AppendLine("    {");
                EmitDecodeContent(list, local + "1", indent + "        ", declare: true);
                _sb.Append(indent).Append("        ").Append(local).Append(".Add(").Append(local).AppendLine("1);");
                _sb.Append(indent).Append("        switch (r.ReadBits(").Append(widthMax).AppendLine("))");
                _sb.Append(indent).AppendLine("        {");
                EmitDecodeRunParticle(tail, 0, indent + "            ", "");
                _sb.Append(indent).AppendLine("            default: throw new InvalidDataException(\"invalid event code\");");
                _sb.Append(indent).AppendLine("        }");
                _sb.Append(indent).AppendLine("        break;");
                _sb.Append(indent).AppendLine("    }");
                EmitDecodeRunParticle(tail, 1, indent + "    ", "");
                _sb.Append(indent).AppendLine("    default: throw new InvalidDataException(\"invalid event code\");");
                _sb.Append(indent).AppendLine("}");
                return;
            }

            // True self-loop mirroring the encode side: every iteration reads [loop, tail, (EE)] —
            // but only once minOccurs is met. The occurrences before that are forced, so their SE is
            // a one-bit code with nothing to choose from (see ForcedOccurrences).
            for (int f = 1; f < ForcedOccurrences(list.ListMin); f++)
            {
                _sb.Append(indent).Append("r.ReadBits(1);   // SE(").Append(list.FieldName)
                   .Append("): forced by minOccurs=").Append(list.ListMin).AppendLine();
                EmitDecodeContent(list, local + "f" + f, indent, declare: true);
                _sb.Append(indent).Append(local).Append(".Add(").Append(local).Append("f").Append(f).AppendLine(");");
            }

            int prod = ProductionCount(tail);
            int width = BitsForChoices(1 + prod + (tailRequired ? 0 : 1) + 1);
            int id = _runCounter++;
            string doneVar = "_rrtdone" + id;

            _sb.Append(indent).Append("bool ").Append(doneVar).AppendLine(" = false;");
            _sb.Append(indent).Append("while (!").Append(doneVar).AppendLine(")");
            _sb.Append(indent).AppendLine("{");
            _sb.Append(indent).Append("    uint rc = r.ReadBits(").Append(width).AppendLine(");");
            _sb.Append(indent).AppendLine("    if (rc == 0)");
            _sb.Append(indent).AppendLine("    {");
            _sb.Append(indent).Append("        if (").Append(local).Append(".Count >= ").Append(list.ListMax)
               .AppendLine(") throw new InvalidDataException(\"invalid repeating-element event code\");");
            EmitDecodeContent(list, local + "n", indent + "        ", declare: true);
            _sb.Append(indent).Append("        ").Append(local).Append(".Add(").Append(local).AppendLine("n);");
            _sb.Append(indent).AppendLine("    }");
            _sb.Append(indent).AppendLine("    else");
            _sb.Append(indent).AppendLine("    {");
            _sb.Append(indent).Append("        switch (rc)").AppendLine();
            _sb.Append(indent).AppendLine("        {");
            // A present optional tail is followed by the element's own EE — the encode side writes it
            // as a separate 1-bit event (see EmitEncodeRequiredRepeatingWithTail), and this construct
            // closes the element itself, so nothing further up consumes it. Without this read the
            // decoder ends up one bit short of the encoder.
            string tailAfter = tailRequired
                ? doneVar + " = true;"
                : "r.ReadBits(1);   // element EE\n" + indent + "                " + doneVar + " = true;";
            EmitDecodeRunParticle(tail, 1, indent + "            ", tailAfter);
            if (!tailRequired)
                _sb.Append(indent).Append("            case ").Append(1 + prod).Append("u: ").Append(doneVar).AppendLine(" = true; break;");
            _sb.Append(indent).AppendLine("            default: throw new InvalidDataException(\"invalid event code\");");
            _sb.Append(indent).AppendLine("        }");
            _sb.Append(indent).AppendLine("    }");
            _sb.Append(indent).AppendLine("}");
        }

        private void EmitWriteValue(ChildPlan c, string accessor, string indent)
        {
            switch (c.Value)
            {
                case ValueEncoding.UnsignedInt:
                    _sb.Append(indent).Append("ExiPrimitives.WriteUnsignedInteger(ref w, (ulong)")
                       .Append(accessor).AppendLine(");");
                    break;
                case ValueEncoding.SignedInt:
                    _sb.Append(indent).Append("ExiPrimitives.WriteSignedInteger(ref w, (long)")
                       .Append(accessor).AppendLine(");");
                    break;
                case ValueEncoding.Binary:
                    _sb.Append(indent).Append("ExiPrimitives.WriteBinary(ref w, ")
                       .Append(accessor).AppendLine(");");
                    break;
                case ValueEncoding.StringValue:
                    _sb.Append(indent).Append("ExiPrimitives.WriteStringValue(ref w, ")
                       .Append(accessor).AppendLine(");");
                    break;
                case ValueEncoding.NBitUnsigned nb when c.IsBool():
                    // xs:boolean is a 1-bit n-bit unsigned; bool has no numeric conversion in C#.
                    _sb.Append(indent).Append("w.WriteBits(").Append(accessor).Append(" ? 1u : 0u, ")
                       .Append(nb.BitWidth).AppendLine(");");
                    break;
                case ValueEncoding.NBitUnsigned nb when nb.Bias != 0:
                    _sb.Append(indent).Append("w.WriteBits((uint)((long)")
                       .Append(accessor).Append(" - ").Append(nb.Bias).Append("), ")
                       .Append(nb.BitWidth).AppendLine(");");
                    break;
                case ValueEncoding.NBitUnsigned nb:
                    _sb.Append(indent).Append("w.WriteBits((uint)").Append(accessor)
                       .Append(", ").Append(nb.BitWidth).AppendLine(");");
                    break;
                case ValueEncoding.EnumIndex ei:
                    // The C# enum value equals the XSD declaration index == the EXI n-bit index.
                    _sb.Append(indent).Append("w.WriteBits((uint)").Append(accessor)
                       .Append(", ").Append(ei.BitWidth).AppendLine(");");
                    break;
            }
        }

        // -----------------------------------------------------------------------
        //  Decode_<RecordName>
        // -----------------------------------------------------------------------

        private void EmitDecodeMethod(SequencePlan sp)
        {
            _sb.Append("    private static ").Append(sp.RecordName)
               .Append(" Decode_").Append(sp.RecordName).AppendLine("(ref BitReader r)");
            _sb.AppendLine("    {");

            if (IsBoundedRepeating(sp, out var rep))
            {
                _sb.Append("        var list = new List<").Append(rep.CsType()).AppendLine(">();");
                _sb.AppendLine("        r.ReadBits(1);   // SE(item) first");
                EmitDecodeContent(rep, "first", "        ", declare: true);
                _sb.AppendLine("        list.Add(first);");
                for (int f = 1; f < ForcedOccurrences(sp.ListMin); f++)
                {
                    _sb.Append("        r.ReadBits(1);   // SE(item): forced by minOccurs=")
                       .Append(sp.ListMin).AppendLine();
                    EmitDecodeContent(rep, "forced" + f, "        ", declare: true);
                    _sb.Append("        list.Add(forced").Append(f).AppendLine(");");
                }
                _sb.AppendLine("        while (true)");
                _sb.AppendLine("        {");
                EmitDecodeListMaxCheck("            ", "list", sp.ListMax);
                _sb.AppendLine("            uint ec = r.ReadBits(2);");
                _sb.AppendLine("            if (ec == 1) break;   // element EE");
                _sb.Append("            if (ec != 0 || list.Count >= ").Append(sp.ListMax)
                   .AppendLine(") throw new InvalidDataException(\"invalid repeating-element event code\");");
                EmitDecodeContent(rep, "next", "            ", declare: true);
                _sb.AppendLine("            list.Add(next);");
                _sb.AppendLine("        }");
                _sb.Append("        return new ").Append(sp.RecordName).AppendLine("(list);");
            }
            else if (sp.SimpleContent is not null && sp.Attributes is not null && sp.Attributes.Any(a => !a.Required))
            {
                EmitDecodeSimpleContentOptionalAttrs(sp);
            }
            else if (sp.SimpleContent is not null)
            {
                var locals = new List<string>();
                if (sp.Attributes is { Count: 1 } && sp.Attributes[0].Required)
                {
                    var attr = sp.Attributes[0];
                    _sb.AppendLine("        r.ReadBits(1);   // AT(required attribute)");
                    _sb.Append("        var _").Append(attr.FieldName)
                   .Append(" = ExiPrimitives.ReadStringValue(ref r, \"").Append(attr.FieldName).AppendLine("\");");
                    locals.Add("_" + attr.FieldName);
                }
                _sb.AppendLine("        r.ReadBits(1);   // CONTENT event");
                var valueChild = new ChildPlan("Value", sp.SimpleContentType!, false,
                                               ChildShape.RequiredSingle, sp.SimpleContent!);
                _sb.Append("        var _Value = ");
                AppendReadValueExpr(valueChild);
                _sb.AppendLine(";");
                locals.Add("_Value");
                _sb.AppendLine("        r.ReadBits(1);   // element EE");
                _sb.Append("        return new ").Append(sp.RecordName).Append('(')
                   .Append(string.Join(", ", locals)).AppendLine(");");
            }
            else if (sp.Attributes is { Count: 1 } && sp.Attributes[0].Required)
            {
                var attr = sp.Attributes[0];
                _sb.AppendLine("        r.ReadBits(1);   // AT(required attribute)");
                _sb.Append("        var _").Append(attr.FieldName)
                   .Append(" = ExiPrimitives.ReadStringValue(ref r, \"").Append(attr.FieldName).AppendLine("\");");
                EmitDecodeContentBody(sp, new List<string> { "_" + attr.FieldName });
            }
            else if (sp.Attributes is not null)
            {
                // Optional attribute(s) unified into the content run (see the encode counterpart).
                var children = WithOptionalAttributes(sp);
                var locals = new List<string>();
                bool terminated = EmitDecodeSequenceChildren(children, "        ", locals);
                if (!terminated)
                    _sb.AppendLine("        r.ReadBits(1);   // element EE");
                _sb.Append("        return new ").Append(sp.RecordName).Append('(')
                   .Append(string.Join(", ", locals)).AppendLine(");");
            }
            else
            {
                EmitDecodeContentBody(sp, new List<string>());
            }

            _sb.AppendLine("    }");
            _sb.AppendLine();
        }

        /// <summary>Decode a complex type's content (xs:choice or xs:sequence) and emit the record return.</summary>
        private void EmitDecodeContentBody(SequencePlan sp, List<string> prefixLocals)
        {
            var locals = new List<string>(prefixLocals);

            if (sp.IsChoice)
            {
                int width = BitsForChoices(sp.Children.Count + 1);
                foreach (var c in sp.Children)
                {
                    _sb.Append("        ").Append(c.CsType()).Append("? _").Append(c.FieldName).AppendLine(" = default;");
                    locals.Add("_" + c.FieldName);
                }
                _sb.Append("        switch (r.ReadBits(").Append(width).AppendLine("))");
                _sb.AppendLine("        {");
                for (int i = 0; i < sp.Children.Count; i++)
                {
                    var c = sp.Children[i];
                    _sb.Append("            case ").Append(i).AppendLine("u:");
                    EmitDecodeContent(c, "_" + c.FieldName, "                ", declare: false);
                    _sb.AppendLine("                break;");
                }
                _sb.AppendLine("            default: throw new InvalidDataException(\"unknown choice event code\");");
                _sb.AppendLine("        }");
                _sb.AppendLine("        r.ReadBits(1);   // element EE");
                _sb.Append("        return new ").Append(sp.RecordName).Append('(')
                   .Append(string.Join(", ", locals)).AppendLine(");");
                return;
            }

            bool terminated = EmitDecodeSequenceChildren(sp.Children, "        ", locals);
            if (!terminated)
                _sb.AppendLine("        r.ReadBits(1);   // element EE");

            _sb.Append("        return new ").Append(sp.RecordName).Append('(')
               .Append(string.Join(", ", locals)).AppendLine(");");
        }

        /// <summary>
        /// Decode mirror of <see cref="EmitEncodeSequenceChildren"/>: walks the sequence, declaring a
        /// local per particle (in record-constructor order) and reading each segment. Returns whether
        /// the element's END event was already consumed.
        /// </summary>
        private bool EmitDecodeSequenceChildren(IReadOnlyList<ChildPlan> children, string indent, List<string> locals)
        {
            bool terminated = false;
            int i = 0;
            while (i < children.Count)
            {
                var c = children[i];
                string local = "_" + c.FieldName;
                switch (c.Shape)
                {
                    case ChildShape.BoundedRepeating when c.ListMin > 0 && i == children.Count - 1:
                        EmitDecodeRepeatingChild(c, local, indent);   // required list
                        locals.Add(local);
                        terminated = true;
                        i++;
                        break;

                    case ChildShape.BoundedRepeating when c.ListMin > 0:
                        EmitDecodeRequiredRepeatingWithTail(c, children[i + 1], indent, locals);
                        terminated = children[i + 1].Shape != ChildShape.RequiredSingle && i + 2 == children.Count;
                        i += 2;
                        break;

                    case ChildShape.RequiredSingle:
                        if (c.Value is ValueEncoding.SubstitutionChoice sc)
                        {
                            EmitDecodeSubstitution(c, local, sc);
                            locals.Add(local);
                        }
                        else if (c.Value is ValueEncoding.InlineChoice ic)
                        {
                            EmitDecodeInlineChoiceStandalone(ic, locals);   // adds its own per-member locals
                        }
                        else
                        {
                            _sb.Append(indent).AppendLine("r.ReadBits(1);   // SE");
                            EmitDecodeContent(c, local, indent, declare: true);
                            locals.Add(local);
                        }
                        terminated = false;
                        i++;
                        break;

                    default: // OptionalSingle (or an optional bounded-repeating list) — head of a run
                        int j = RunEnd(children, i);
                        bool endsElement = j == children.Count;
                        EmitDecodeOptionalRun(children, i, j, indent, locals);
                        bool repTerm = !endsElement && children[j].Shape == ChildShape.BoundedRepeating;
                        terminated = endsElement || repTerm;
                        i = endsElement ? j : j + 1;
                        break;
                }
            }
            return terminated;
        }

        /// <summary>Decode side of <see cref="EmitEncodeOptionalRun"/>. Declares a local per particle
        /// (in record-constructor order), then reads the flat state machine: at each state it reads the
        /// width-bit event code and dispatches it to the production it selects (an optional element, a
        /// substitution member, the required terminator, or the element EE).</summary>
        private void EmitDecodeOptionalRun(IReadOnlyList<ChildPlan> children, int s, int e, string indent, List<string> locals)
        {
            int listIdx = -1;
            for (int p = s; p < e; p++)
                if (children[p].Shape == ChildShape.BoundedRepeating) { listIdx = p; break; }
            if (listIdx >= 0 && listIdx != e - 1)
            {
                if (_plan.ParticleGrammar == ParticleGrammar.SchemaConformant)
                     EmitDecodeOptionalRunWithMidListSchema(children, s, listIdx, e, indent, locals);
                else EmitDecodeOptionalRunWithMidList(children, s, listIdx, e, indent, locals);
                return;
            }

            int m = e - s;
            bool endsElement = e == children.Count;
            ChildPlan? term = endsElement ? null : children[e];
            if (term is not null && term.Shape is not (ChildShape.RequiredSingle or ChildShape.BoundedRepeating))
                throw new NotSupportedException(
                    $"optional run before '{term.FieldName}': the terminator must be the element EE or a required particle.");
            if (term is not null && term.Shape == ChildShape.BoundedRepeating && e != children.Count - 1)
                throw new NotSupportedException(
                    $"repeating terminator '{term.FieldName}' must be the last child of the sequence (its loop ends the element).");
            bool trailingAny = endsElement && m > 0 && children[e - 1].IsWildcardAny;

            // Declare the locals in record order: each optional (nullable), a repeating member as a
            // list, then the terminator.
            for (int p = s; p < e; p++)
            {
                var o = children[p];
                if (o.Value is ValueEncoding.InlineChoice ico)
                    DeclareInlineChoiceLocals(ico, indent, locals);
                else if (o.Shape == ChildShape.BoundedRepeating)
                {
                    _sb.Append(indent).Append("var _").Append(o.FieldName)
                       .Append(" = new List<").Append(o.CsType()).AppendLine(">();");
                    locals.Add("_" + o.FieldName);
                }
                else
                {
                    _sb.Append(indent).Append(o.CsType()).Append("? _").Append(o.FieldName).AppendLine(" = default;");
                    locals.Add("_" + o.FieldName);
                }
            }
            if (term is not null)
            {
                if (term.Value is ValueEncoding.InlineChoice ict)
                    DeclareInlineChoiceLocals(ict, indent, locals);
                else if (term.Shape == ChildShape.BoundedRepeating)
                {
                    _sb.Append(indent).Append("var _").Append(term.FieldName)
                       .Append(" = new List<").Append(term.CsType()).AppendLine(">();");
                    locals.Add("_" + term.FieldName);
                }
                else
                {
                    _sb.Append(indent).Append(term.CsType()).Append(" _").Append(term.FieldName).AppendLine(" = default!;");
                    locals.Add("_" + term.FieldName);
                }
            }

            int id = _runCounter++;
            string st = "_ist" + id, done = "_idone" + id, code = "_ic" + id;
            string inner = indent + "    ";
            string body = inner + "    ";
            string sw = body + "    ";
            string ca = sw + "    ";

            _sb.Append(indent).Append("int ").Append(st).AppendLine(" = 0;");
            _sb.Append(indent).Append("bool ").Append(done).AppendLine(" = false;");
            _sb.Append(indent).Append("while (!").Append(done).AppendLine(")");
            _sb.Append(indent).AppendLine("{");
            _sb.Append(inner).Append("switch (").Append(st).AppendLine(")");
            _sb.Append(inner).AppendLine("{");

            for (int k = 0; k <= m; k++)
            {
                int totalProd = endsElement ? 1 : ProductionCount(term!);
                for (int i = k; i < m; i++) totalProd += ProductionCount(children[s + i]);
                int width = BitsForChoices(totalProd + 1);

                _sb.Append(body).Append("case ").Append(k).AppendLine(":");
                _sb.Append(body).AppendLine("{");
                _sb.Append(sw).Append("uint ").Append(code).Append(" = r.ReadBits(").Append(width).AppendLine(");");
                _sb.Append(sw).Append("switch (").Append(code).AppendLine(")");
                _sb.Append(sw).AppendLine("{");

                int c = 0;
                int optEnd = trailingAny ? m - 1 : m;
                for (int i = k; i < optEnd; i++)
                {
                    string after = children[s + i].Shape == ChildShape.BoundedRepeating
                        ? done + " = true;"
                        : st + " = " + (i + 1) + ";";
                    c = EmitDecodeRunParticle(children[s + i], c, ca, after);
                }

                if (trailingAny && k <= m - 1)
                {
                    // c == generic-wildcard slot (no case; a generic wildcard event is unsupported and
                    // falls through to default). Element EE at c+1, then the typed ANY element at c+2.
                    _sb.Append(ca).Append("case ").Append(c + 1).AppendLine("u:");   // element EE
                    _sb.Append(ca).Append("    ").Append(done).AppendLine(" = true; break;");
                    EmitDecodeRunParticle(children[e - 1], c + 2, ca, st + " = " + m + ";");
                }
                else if (endsElement)
                {
                    _sb.Append(ca).Append("case ").Append(c).AppendLine("u:");   // element EE
                    _sb.Append(ca).Append("    ").Append(done).AppendLine(" = true; break;");
                }
                else
                {
                    c = EmitDecodeRunParticle(term!, c, ca, done + " = true;");
                }

                _sb.Append(ca).AppendLine("default: throw new InvalidDataException(\"invalid optional-run event code\");");
                _sb.Append(sw).AppendLine("}");
                _sb.Append(sw).AppendLine("break;");
                _sb.Append(body).AppendLine("}");
            }

            _sb.Append(inner).AppendLine("}");
            _sb.Append(indent).AppendLine("}");
        }

        /// <summary>Decode side of <see cref="EmitEncodeOptionalRunWithMidList"/> — see there for the
        /// grammar shape (states 0/1/2, the 2-item cap) this mirrors.</summary>
        private void EmitDecodeOptionalRunWithMidList(
            IReadOnlyList<ChildPlan> children, int s, int listIdx, int e, string indent, List<string> locals)
        {
            if (listIdx != s)
                throw new NotSupportedException(
                    $"repeating element '{children[listIdx].FieldName}' mid-run: particles before it in the same run are not supported.");
            var list = children[listIdx];
            if (list.ListMin != 0)
                throw new NotSupportedException(
                    $"repeating element '{list.FieldName}' mid-run must be optional (minOccurs=0).");
            if (e != children.Count)
                throw new NotSupportedException(
                    $"repeating element '{list.FieldName}' mid-run must be followed only by particles ending the sequence " +
                    "(a required/repeating terminator after it is not supported).");

            var suffix = new List<ChildPlan>();
            for (int p = listIdx + 1; p < e; p++)
            {
                if (children[p].Value is ValueEncoding.SubstitutionChoice or ValueEncoding.InlineChoice)
                    throw new NotSupportedException(
                        $"repeating element '{list.FieldName}' mid-run: suffix particle '{children[p].FieldName}' " +
                        "must be a plain optional element (choice/substitution suffixes are not supported).");
                suffix.Add(children[p]);
            }
            int suffixTotal = 0;
            foreach (var sp in suffix) suffixTotal += ProductionCount(sp);

            _sb.Append(indent).Append("var _").Append(list.FieldName)
               .Append(" = new List<").Append(list.CsType()).AppendLine(">();");
            locals.Add("_" + list.FieldName);
            foreach (var sp in suffix)
            {
                _sb.Append(indent).Append(sp.CsType()).Append("? _").Append(sp.FieldName).AppendLine(" = default;");
                locals.Add("_" + sp.FieldName);
            }

            string local = "_" + list.FieldName;
            int id = _runCounter++;
            string st = "_ist" + id, done = "_idone" + id;
            string inner = indent + "    ";
            string body = inner + "    ";
            string sw = body + "    ";
            string ca = sw + "    ";

            _sb.Append(indent).Append("int ").Append(st).AppendLine(" = 0;");
            _sb.Append(indent).Append("bool ").Append(done).AppendLine(" = false;");
            _sb.Append(indent).Append("while (!").Append(done).AppendLine(")");
            _sb.Append(indent).AppendLine("{");
            _sb.Append(inner).Append("switch (").Append(st).AppendLine(")");
            _sb.Append(inner).AppendLine("{");

            // State 0: zero items yet — [start item 0] or [element EE].
            int w0 = BitsForChoices(1 + 1 + 1);
            _sb.Append(body).AppendLine("case 0:");
            _sb.Append(body).AppendLine("{");
            _sb.Append(sw).Append("uint c0 = r.ReadBits(").Append(w0).AppendLine(");");
            _sb.Append(sw).AppendLine("switch (c0)");
            _sb.Append(sw).AppendLine("{");
            _sb.Append(ca).AppendLine("case 0u:");
            _sb.Append(ca).AppendLine("{");
            string it0 = "_it" + _tmpCounter++;
            EmitDecodeContent(list, it0, ca + "    ", declare: true);
            _sb.Append(ca).Append("    ").Append(local).Append(".Add(").Append(it0).AppendLine(");");
            _sb.Append(ca).Append("    ").Append(st).AppendLine(" = 1;");
            _sb.Append(ca).AppendLine("    break;");
            _sb.Append(ca).AppendLine("}");
            _sb.Append(ca).Append("case 1u: ").Append(done).AppendLine(" = true; break;");
            _sb.Append(ca).AppendLine("default: throw new InvalidDataException(\"invalid optional-run event code\");");
            _sb.Append(sw).AppendLine("}");
            _sb.Append(sw).AppendLine("break;");
            _sb.Append(body).AppendLine("}");

            // State 1: one item written — [start item 1 (loop)], each suffix particle, or [element EE].
            int w1 = BitsForChoices(1 + suffixTotal + 1 + 1);
            _sb.Append(body).AppendLine("case 1:");
            _sb.Append(body).AppendLine("{");
            _sb.Append(sw).Append("uint c1 = r.ReadBits(").Append(w1).AppendLine(");");
            _sb.Append(sw).AppendLine("switch (c1)");
            _sb.Append(sw).AppendLine("{");
            _sb.Append(ca).AppendLine("case 0u:");
            _sb.Append(ca).AppendLine("{");
            string it1 = "_it" + _tmpCounter++;
            EmitDecodeContent(list, it1, ca + "    ", declare: true);
            _sb.Append(ca).Append("    ").Append(local).Append(".Add(").Append(it1).AppendLine(");");
            _sb.Append(ca).Append("    ").Append(st).AppendLine(" = 2;");
            _sb.Append(ca).AppendLine("    break;");
            _sb.Append(ca).AppendLine("}");
            {
                int code = 1;
                foreach (var sp in suffix)
                    code = EmitDecodeRunParticle(sp, code, ca, st + " = 3;");
                _sb.Append(ca).Append("case ").Append(code).Append("u: ").Append(done).AppendLine(" = true; break;");
            }
            _sb.Append(ca).AppendLine("default: throw new InvalidDataException(\"invalid optional-run event code\");");
            _sb.Append(sw).AppendLine("}");
            _sb.Append(sw).AppendLine("break;");
            _sb.Append(body).AppendLine("}");

            // State 2: two items written (the list is capped here) — each suffix particle, or [element EE].
            int w2 = BitsForChoices(suffixTotal + 1 + 1);
            _sb.Append(body).AppendLine("case 2:");
            _sb.Append(body).AppendLine("{");
            _sb.Append(sw).Append("uint c2 = r.ReadBits(").Append(w2).AppendLine(");");
            _sb.Append(sw).AppendLine("switch (c2)");
            _sb.Append(sw).AppendLine("{");
            {
                int code = 0;
                foreach (var sp in suffix)
                    code = EmitDecodeRunParticle(sp, code, ca, st + " = 3;");
                _sb.Append(ca).Append("case ").Append(code).Append("u: ").Append(done).AppendLine(" = true; break;");
            }
            _sb.Append(ca).AppendLine("default: throw new InvalidDataException(\"invalid optional-run event code\");");
            _sb.Append(sw).AppendLine("}");
            _sb.Append(sw).AppendLine("break;");
            _sb.Append(body).AppendLine("}");

            // State 3: a suffix particle was just decoded — nothing left to read but the element EE
            // (only reachable when there IS a suffix; harmless unreachable case otherwise).
            if (suffix.Count > 0)
            {
                _sb.Append(body).AppendLine("case 3:");
                _sb.Append(body).AppendLine("{");
                _sb.Append(sw).AppendLine("r.ReadBits(1);   // element EE");
                _sb.Append(sw).Append(done).AppendLine(" = true;");
                _sb.Append(sw).AppendLine("break;");
                _sb.Append(body).AppendLine("}");
            }

            _sb.Append(inner).AppendLine("}");
            _sb.Append(indent).AppendLine("}");
        }

        /// <summary>
        /// Decode side of <see cref="EmitEncodeOptionalRunWithMidListSchema"/>. One state, because every
        /// state of this grammar offers the same three choices: another item, the following optional
        /// particle, or the end element.
        /// </summary>
        private void EmitDecodeOptionalRunWithMidListSchema(
            IReadOnlyList<ChildPlan> children, int s, int listIdx, int e, string indent, List<string> locals)
        {
            var (list, suffix, suffixTotal) = MidListShape(children, s, listIdx, e);

            _sb.Append(indent).Append("var _").Append(list.FieldName)
               .Append(" = new List<").Append(list.CsType()).AppendLine(">();");
            locals.Add("_" + list.FieldName);
            foreach (var sp in suffix)
            {
                _sb.Append(indent).Append(sp.CsType()).Append("? _").Append(sp.FieldName).AppendLine(" = default;");
                locals.Add("_" + sp.FieldName);
            }

            string local = "_" + list.FieldName;
            int width = BitsForChoices(1 + suffixTotal + 1 + 1);
            int id = _runCounter++;
            string done = "_idone" + id;
            string inner = indent + "    ";
            string sw = inner + "    ";
            string ca = sw + "    ";

            _sb.Append(indent).Append("bool ").Append(done).AppendLine(" = false;");
            _sb.Append(indent).Append("while (!").Append(done).AppendLine(")");
            _sb.Append(indent).AppendLine("{");
            _sb.Append(inner).Append("uint _c").Append(id).Append(" = r.ReadBits(").Append(width).AppendLine(");");
            _sb.Append(inner).Append("switch (_c").Append(id).AppendLine(")");
            _sb.Append(inner).AppendLine("{");

            _sb.Append(sw).AppendLine("case 0u:");
            _sb.Append(sw).AppendLine("{");
            string it = "_it" + _tmpCounter++;
            EmitDecodeContent(list, it, ca, declare: true);
            _sb.Append(ca).Append(local).Append(".Add(").Append(it).AppendLine(");");
            _sb.Append(ca).AppendLine("break;");
            _sb.Append(sw).AppendLine("}");

            int code = 1;
            foreach (var sp in suffix)
            {
                _sb.Append(sw).Append("case ").Append(code).AppendLine("u:");
                _sb.Append(sw).AppendLine("{");
                string tmp = "_sx" + _tmpCounter++;
                EmitDecodeContent(sp, tmp, ca, declare: true);
                _sb.Append(ca).Append("_").Append(sp.FieldName).Append(" = ").Append(tmp).AppendLine(";");
                _sb.Append(ca).AppendLine("r.ReadBits(1);   // element EE");
                _sb.Append(ca).Append(done).AppendLine(" = true;");
                _sb.Append(ca).AppendLine("break;");
                _sb.Append(sw).AppendLine("}");
                code += ProductionCount(sp);
            }

            _sb.Append(sw).Append("case ").Append(code).Append("u: ").Append(done).AppendLine(" = true; break;");
            _sb.Append(sw).AppendLine("default: throw new InvalidDataException(\"invalid optional-run event code\");");
            _sb.Append(inner).AppendLine("}");
            _sb.Append(indent).AppendLine("}");
        }


        /// <summary>Emits the decode <c>switch</c> case(s) for one run particle: an optional element
        /// (one case reading its content), or a substitution reference (one case per member, decoding
        /// into the base-typed local; the abstract head's slot throws). Returns the next event code.</summary>
        private int EmitDecodeRunParticle(ChildPlan p, int code, string indent, string after)
        {
            string local = "_" + p.FieldName;
            if (p.Shape == ChildShape.BoundedRepeating)
            {
                string it0 = "_it" + _tmpCounter++;
                string itn = "_it" + _tmpCounter++;
                _sb.Append(indent).Append("case ").Append(code).AppendLine("u:");
                _sb.Append(indent).AppendLine("{");
                EmitDecodeContent(p, it0, indent + "    ", declare: true);   // first item (event code was its SE)
                _sb.Append(indent).Append("    ").Append(local).Append(".Add(").Append(it0).AppendLine(");");
                _sb.Append(indent).AppendLine("    while (true)");
                _sb.Append(indent).AppendLine("    {");
                _sb.Append(indent).AppendLine("        uint lc = r.ReadBits(2);");
                _sb.Append(indent).AppendLine("        if (lc == 1) break;   // element EE (list end)");
                _sb.Append(indent).Append("        if (lc != 0 || ").Append(local).Append(".Count >= ").Append(p.ListMax)
                   .AppendLine(") throw new InvalidDataException(\"invalid repeating-element event code\");");
                EmitDecodeContent(p, itn, indent + "        ", declare: true);
                _sb.Append(indent).Append("        ").Append(local).Append(".Add(").Append(itn).AppendLine(");");
                _sb.Append(indent).AppendLine("    }");
                _sb.Append(indent).Append("    ").AppendLine(after);
                _sb.Append(indent).AppendLine("    break;");
                _sb.Append(indent).AppendLine("}");
                return code + 1;
            }
            if (p.Value is ValueEncoding.SubstitutionChoice sc)
            {
                foreach (var mbr in sc.Members)
                {
                    _sb.Append(indent).Append("case ").Append(code).AppendLine("u:");
                    if (mbr.IsAbstractHead)
                        _sb.Append(indent).AppendLine("    throw new InvalidDataException(\"abstract substitution head cannot be decoded\");");
                    else
                    {
                        _sb.Append(indent).Append("    ").Append(local).Append(" = Decode_")
                           .Append(mbr.TypeName).AppendLine("(ref r);");
                        _sb.Append(indent).Append("    ").AppendLine(after);
                        _sb.Append(indent).AppendLine("    break;");
                    }
                    code++;
                }
                return code;
            }
            if (p.Value is ValueEncoding.InlineChoice ic)
            {
                foreach (var mbr in ic.Members)
                {
                    _sb.Append(indent).Append("case ").Append(code).AppendLine("u:");
                    _sb.Append(indent).AppendLine("{");
                    EmitDecodeContent(AsChildPlan(mbr), "_" + mbr.FieldName, indent + "    ", declare: false);
                    _sb.Append(indent).Append("    ").AppendLine(after);
                    _sb.Append(indent).AppendLine("    break;");
                    _sb.Append(indent).AppendLine("}");
                    code++;
                }
                return code;
            }

            _sb.Append(indent).Append("case ").Append(code).AppendLine("u:");
            EmitDecodeContent(p, local, indent + "    ", declare: false);
            _sb.Append(indent).Append("    ").AppendLine(after);
            _sb.Append(indent).AppendLine("    break;");
            return code + 1;
        }

        /// <summary>
        /// Reads the content of a child element after its SE event has been consumed.
        /// Complex child: nested decode (consumes its own EE). Simple child: value-start,
        /// value, child EE. When <paramref name="declare"/> is false the value is assigned
        /// to an existing local (used for optional elements).
        /// </summary>
        private void EmitDecodeContent(ChildPlan c, string local, string indent, bool declare)
        {
            if (c.Value is ValueEncoding.AttributeValue)
            {
                // AT value: a bare string, no value-start / child-EE.
                _sb.Append(indent).Append(declare ? "var " : "").Append(local)
                   .Append(" = ExiPrimitives.ReadStringValue(ref r, \"").Append(c.FieldName).AppendLine("\");");
                return;
            }
            if (c.Value is ValueEncoding.OpaqueElement oe)
            {
                // Reached only if the wire carries a present (signed) instance — not modelled in Phase 2.
                if (declare)
                    _sb.Append(indent).Append(oe.TypeName).Append(' ').Append(local).AppendLine(" = default!;");
                _sb.Append(indent).Append("throw new NotSupportedException(\"Decoding a present ")
                   .Append(oe.TypeName).AppendLine(" (XMLDSig) is deferred to Phase 3.\");");
                return;
            }
            if (c.Value is ValueEncoding.ComplexRef cr)
            {
                _sb.Append(indent);
                _sb.Append(declare ? "var " : "").Append(local).Append(" = Decode_")
                   .Append(cr.TypeName).AppendLine("(ref r);");
                return;
            }

            _sb.Append(indent).AppendLine("r.ReadBits(1);   // value-start");
            _sb.Append(indent);
            _sb.Append(declare ? "var " : "").Append(local).Append(" = ");
            AppendReadValueExpr(c);
            _sb.AppendLine(";");
            _sb.Append(indent).AppendLine("r.ReadBits(1);   // child EE");
        }

        /// <summary>Declares one nullable local per branch of an inline choice (used whether the choice
        /// is a plain optional-run member or the run's required terminator — every branch is
        /// individually nullable regardless, since at most one is ever set).</summary>
        private void DeclareInlineChoiceLocals(ValueEncoding.InlineChoice ic, string indent, List<string> locals)
        {
            foreach (var m in ic.Members)
            {
                _sb.Append(indent).Append(m.CsType()).Append("? _").Append(m.FieldName).AppendLine(" = default;");
                locals.Add("_" + m.FieldName);
            }
        }

        /// <summary>Decode side of <see cref="EmitEncodeInlineChoiceStandalone"/>: declares one nullable
        /// local per branch, reads the n-bit selector, and assigns the selected branch's decoded value.</summary>
        private void EmitDecodeInlineChoiceStandalone(ValueEncoding.InlineChoice ic, List<string> locals)
        {
            DeclareInlineChoiceLocals(ic, "        ", locals);
            _sb.Append("        switch (r.ReadBits(").Append(ic.BitWidth).AppendLine("))");
            _sb.AppendLine("        {");
            for (int i = 0; i < ic.Members.Count; i++)
            {
                var m = ic.Members[i];
                _sb.Append("            case ").Append(i).AppendLine("u:");
                EmitDecodeContent(AsChildPlan(m), "_" + m.FieldName, "                ", declare: false);
                _sb.AppendLine("                break;");
            }
            _sb.AppendLine("            default: throw new InvalidDataException(\"unknown choice event code\");");
            _sb.AppendLine("        }");
        }

        /// <summary>Decode side of <see cref="EmitEncodeSubstitution"/>: read the event code and
        /// dispatch to the selected member's decoder.</summary>
        private void EmitDecodeSubstitution(ChildPlan c, string local, ValueEncoding.SubstitutionChoice sc)
        {
            _sb.Append("        ").Append(c.CsType()).Append(' ').Append(local).AppendLine(";");
            _sb.Append("        switch (r.ReadBits(").Append(sc.BitWidth).AppendLine("))");
            _sb.AppendLine("        {");
            for (int i = 0; i < sc.Members.Count; i++)
            {
                var m = sc.Members[i];
                if (m.IsAbstractHead)
                    _sb.Append("            case ").Append(i)
                       .AppendLine("u: throw new InvalidDataException(\"abstract substitution head cannot be decoded\");");
                else
                    _sb.Append("            case ").Append(i).Append("u: ").Append(local)
                       .Append(" = Decode_").Append(m.TypeName).AppendLine("(ref r); break;");
            }
            _sb.AppendLine("            default: throw new InvalidDataException(\"unknown substitution index\");");
            _sb.AppendLine("        }");
        }

        private void AppendReadValueExpr(ChildPlan c)
        {
            switch (c.Value)
            {
                case ValueEncoding.UnsignedInt:
                    _sb.Append('(').Append(c.CsType())
                       .Append(")ExiPrimitives.ReadUnsignedInteger(ref r)");
                    break;
                case ValueEncoding.SignedInt:
                    _sb.Append('(').Append(c.CsType())
                       .Append(")ExiPrimitives.ReadSignedInteger(ref r)");
                    break;
                case ValueEncoding.Binary:
                    _sb.Append("ExiPrimitives.ReadBinary(ref r)");
                    break;
                case ValueEncoding.StringValue:
                    // The slot is the element's own QName local part: EXI keeps one local value
                    // partition per slot (§7.3.3), and the decoder needs to name the right one.
                    _sb.Append("ExiPrimitives.ReadStringValue(ref r, \"").Append(c.FieldName).Append("\")");
                    break;
                case ValueEncoding.NBitUnsigned nb when c.IsBool():
                    _sb.Append("r.ReadBits(").Append(nb.BitWidth).Append(") != 0u");
                    break;
                case ValueEncoding.NBitUnsigned nb when nb.Bias != 0:
                    _sb.Append('(').Append(c.CsType()).Append(")((long)r.ReadBits(")
                       .Append(nb.BitWidth).Append(") + ").Append(nb.Bias).Append(')');
                    break;
                case ValueEncoding.NBitUnsigned nb:
                    _sb.Append('(').Append(c.CsType()).Append(")r.ReadBits(").Append(nb.BitWidth).Append(')');
                    break;
                case ValueEncoding.EnumIndex ei:
                    _sb.Append('(').Append(ei.EnumName).Append(")r.ReadBits(").Append(ei.BitWidth).Append(')');
                    break;
            }
        }

        // -----------------------------------------------------------------------
        //  Helpers
        // -----------------------------------------------------------------------

        private static bool IsBoundedRepeating(SequencePlan sp, out ChildPlan rep)
        {
            if (sp.Children.Count == 1 && sp.Children[0].Shape == ChildShape.BoundedRepeating)
            {
                rep = sp.Children[0];
                return true;
            }
            rep = default!;
            return false;
        }

        private static int BitsForChoices(int n)
        {
            if (n <= 1) return 0;
            int bits = 0;
            int v = n - 1;
            while (v > 0) { bits++; v >>= 1; }
            return bits;
        }

        /// <summary>
        /// How many occurrences of a repeating particle the grammar <b>forces</b> before it offers any
        /// alternative: <c>minOccurs</c>, but never fewer than one.
        /// <para>
        /// EXI unrolls a bounded particle into one grammar state per occurrence. The first
        /// <c>minOccurs</c> of those states have a single production — <c>SE(item)</c>, because nothing
        /// else is legal yet — so the event code there is one bit wide; only once the minimum is met does
        /// the state also offer the end-element (and whatever particle follows), which widens the code to
        /// two. Writing the wide code for the second occurrence of a <c>minOccurs="2"</c> particle emits
        /// one bit too many, and every bit after it is misaligned.
        /// </para>
        /// <para>
        /// ISO 15118 has exactly five such particles, and all five sit in message sets no reference
        /// encoder covers: <c>CurveDataPoint</c> (DER IEC and SAE) and <c>TxSpecData</c>,
        /// <c>RxSpecData</c>, <c>PulseSequenceOrder</c> (WPT). That is why the vector corpus never caught
        /// this — the affected vectors are our own output, checked only against ourselves. Found
        /// 2026-08-07 when EXIficient could not read <c>AC_ChargeParameterDiscoveryRes_DER</c> and gave up
        /// inside the second <c>CurveDataPoint</c>; confirmed independently by encoding a synthetic
        /// <c>minOccurs="2" maxOccurs="10"</c> schema with EXIficient, where the second occurrence costs
        /// one bit and the third costs two.
        /// </para>
        /// </summary>
        private static int ForcedOccurrences(int listMin) => Math.Max(1, listMin);

        /// <summary>The SE event-code width for occurrence <paramref name="index"/> of a list: one bit
        /// while the occurrence is still forced, two once the state also offers a way out. Rendered as
        /// the original <c>i == 0 ? 1 : 2</c> whenever only the first occurrence is forced, so that the
        /// generated codec for every <c>minOccurs≤1</c> particle — which is all but five in ISO 15118 —
        /// is unchanged.</summary>
        private static string SeWidthExpr(string index, int forced)
            => forced <= 1 ? index + " == 0 ? 1 : 2"
                           : index + " < " + forced + " ? 1 : 2";

        private static string SeWidthComment(int forced)
            => forced <= 1 ? "   // SE(item): 1-bit first, 2-bit loop"
                           : "   // SE(item): 1-bit while forced (minOccurs=" + forced + "), 2-bit loop";
    }
}
