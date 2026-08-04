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
    /// The JSON-LD (de)serializer, emitted from the same <see cref="SchemaPlan"/> as the wire codec
    /// (docs/CONCEPT.md §4.4).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Same plan, same pass.</b> This runs as part of <see cref="CSharpCodecEmitter"/> rather than
    /// as an emitter of its own, and that is the point of §4.4: a JSON mapper maintained beside a
    /// generated codec drifts from it, and the way to make drift impossible is to leave no seam where
    /// it could happen. There is no way to regenerate one and not the other.
    /// </para>
    /// <para>
    /// <b>What the JSON has to carry.</b> The requirement is that <c>EXI → JSON → EXI</c> reproduces
    /// the original bytes, which is stronger than "every field is present". Two structures on the
    /// wire have no natural JSON counterpart and are written down explicitly:
    /// </para>
    /// <list type="bullet">
    ///   <item>A <b>substitution-group</b> member is chosen by an event code, so the concrete type
    ///         has to travel with the value — hence <c>@type</c> on every object.</item>
    ///   <item>An <b>inline choice</b> is already N sibling nullable fields in the record, so the
    ///         branch falls out of which property is present. Nothing extra is needed.</item>
    /// </list>
    /// <para>
    /// <b>What the round-trip cannot check.</b> The oracle runs in C#, and C# does not have the
    /// bridge's limits — the 64-bit-integer-as-string decision in <c>JsonPrimitives.Int64</c> exists
    /// for a JavaScript consumer and would round-trip perfectly here either way. Decisions of that
    /// shape need their own tests; a green round-trip says nothing about them.
    /// </para>
    /// </remarks>
    internal sealed class CSharpJsonEmitter
    {

        private readonly SchemaPlan _plan;
        private readonly string _namespace;
        private readonly string _class;
        private readonly StringBuilder _sb = new();

        /// <summary>Every concrete record, by name — what gets a serializer and a parser.</summary>
        private readonly List<SequencePlan> _records = new();

        /// <summary>Record name → its plan, including abstract ones (needed to walk base chains).</summary>
        private readonly Dictionary<string, SequencePlan> _byName = new(StringComparer.Ordinal);

        /// <summary>Types with no content of their own — XMLDSig placeholders, always empty.</summary>
        private readonly HashSet<string> _opaque = new(StringComparer.Ordinal);

        private int _local;   // unique suffixes for optional-property temporaries


        private CSharpJsonEmitter(SchemaPlan plan, string ns, string codecClass)
        {
            _plan      = plan;
            _namespace = ns;
            _class     = codecClass + "Json";

            foreach (var name in plan.OpaqueTypes)
                _opaque.Add(name);

            // Global-element records (SessionSetupReq) are separate types from the complex types they
            // wrap (SessionSetupReqType) and are not in ComplexTypes, so both sources are walked.
            var seen = new HashSet<string>(StringComparer.Ordinal);

            void Add(SequencePlan sp)
            {
                if (!seen.Add(sp.RecordName)) return;
                _byName[sp.RecordName] = sp;
                if (!sp.IsAbstract) _records.Add(sp);
            }

            foreach (var sp in plan.ComplexTypes.Values) Add(sp);
            foreach (var g in plan.GlobalElements)       Add(g.Body);
        }


        public static IReadOnlyList<GeneratedFile> Emit(SchemaPlan plan, string ns, string codecClass) =>
            new CSharpJsonEmitter(plan, ns, codecClass).Run();


        /// <summary>
        /// One file per type, plus one for the two dispatchers — the same split the codec uses.
        /// </summary>
        /// <remarks>
        /// The split is not cosmetic. <c>CSharpEmitterSplitTests</c> holds every emitted file under
        /// 100,000 characters, and it caught this: as one file, the -2 JSON-LD serializer came to
        /// 128,499. The invariant is Kotlin's compiler heap in origin, but it earns its keep
        /// everywhere — a per-type file is what makes a generated diff readable, and a diff nobody
        /// can read is a diff nobody checks.
        /// </remarks>
        private IReadOnlyList<GeneratedFile> Run()
        {
            // Global-element types are the documents: only they carry @context, because repeating the
            // vocabulary on every nested object would multiply the size of a message for no reading
            // anyone does.
            var documents = new HashSet<string>(_plan.GlobalElements.Select(g => g.Body.RecordName),
                                                StringComparer.Ordinal);

            var files = new List<GeneratedFile>();

            foreach (var sp in _records)
                files.Add(Part(sp.RecordName, () => {
                    EmitSerializer(sp, documents.Contains(sp.RecordName));
                    EmitParser(sp);
                }));

            foreach (var name in _plan.OpaqueTypes)
                files.Add(Part(name, () => EmitOpaque(name)));

            files.Add(Part(_class, () => {
                EmitContext();
                EmitSerializerDispatch();
                EmitParserDispatch();
            }));

            return files;
        }


        /// <summary>One part of the partial JSON class, in its own file.</summary>
        private GeneratedFile Part(string name, Action emit)
        {
            _sb.Clear();
            EmitHeader();
            emit();
            _sb.AppendLine("    }");
            _sb.AppendLine("}");

            // `<Type>.Json.g.cs` beside the codec's `<Type>.g.cs`. Both end in the extension the
            // driver sweeps for, and both are in its keep-set, so neither deletes the other.
            return new GeneratedFile(name + ".Json.g.cs", _sb.ToString());
        }


        private void EmitHeader()
        {
            _sb.AppendLine("// <auto-generated/>");
            _sb.AppendLine("// Generated by WWCP_ISO15118_EXI_SourceGenerator. Do not edit by hand.");
            _sb.AppendLine("#nullable enable");
            _sb.AppendLine("using System;");
            _sb.AppendLine("using System.Collections.Generic;");
            _sb.AppendLine("using System.Linq;");
            _sb.AppendLine("using System.Text.Json.Nodes;");
            _sb.AppendLine("using cloud.charging.open.protocols.ISO15118.EXI;");
            _sb.AppendLine();
            _sb.Append("namespace ").AppendLine(_namespace);
            _sb.AppendLine("{");
            _sb.AppendLine("    /// <summary>");
            _sb.AppendLine("    /// The JSON-LD form of this message set, generated from the same schema plan as the");
            _sb.AppendLine("    /// wire codec. <c>EXI -&gt; JSON -&gt; EXI</c> reproduces the original bytes.");
            _sb.AppendLine("    /// </summary>");
            _sb.Append("    public static partial class ").AppendLine(_class);
            _sb.AppendLine("    {");
            _sb.AppendLine();
        }


        private void EmitContext()
        {
            _sb.AppendLine("        /// <summary>The vocabulary these messages are written in: the XSD target namespace.</summary>");
            _sb.Append("        public const string Context = \"")
               .Append(JsonNaming.Context(_plan.TargetNamespace)).AppendLine("\";");
            _sb.AppendLine();
        }


        /// <summary>
        /// <c>ToJSON(object)</c> — one switch over every concrete record.
        /// </summary>
        /// <remarks>
        /// Dispatch is on the <b>runtime</b> type, never the declared one, because a polymorphic
        /// field holds a substitution member and the declared type is its base. Branches are ordered
        /// most-derived-first for the reason the codec's substitution dispatch is: a type pattern
        /// matches subtypes too, so a base-first order would serialise every derived value as its
        /// base and silently drop the fields the derived type added.
        /// </remarks>
        private void EmitSerializerDispatch()
        {
            _sb.AppendLine("        /// <summary>The JSON-LD form of any generated message or type.</summary>");
            _sb.AppendLine("        public static JsonObject ToJSON(object value)");
            _sb.AppendLine();
            _sb.AppendLine("            => value switch");
            _sb.AppendLine("            {");

            foreach (var sp in _records.OrderByDescending(Depth).ThenBy(sp => sp.RecordName, StringComparer.Ordinal))
                _sb.Append("                ").Append(sp.RecordName).Append(" v => ToJSON_")
                   .Append(sp.RecordName).AppendLine("(v),");

            foreach (var name in _plan.OpaqueTypes)
                _sb.Append("                ").Append(name).Append(" v => ToJSON_")
                   .Append(name).AppendLine("(v),");

            _sb.AppendLine();
            _sb.AppendLine("                _ => throw new JsonLdException(");
            _sb.AppendLine("                         $\"{value.GetType().Name} is not a type of this message set.\"),");
            _sb.AppendLine();
            _sb.AppendLine("            };");
            _sb.AppendLine();
        }


        /// <summary>How many records deep the base chain runs — the sort key for the dispatch.</summary>
        private int Depth(SequencePlan sp)
        {
            var depth = 0;
            var current = sp;
            while (current.BaseRecordName is not null && _byName.TryGetValue(current.BaseRecordName, out var next))
            {
                depth++;
                current = next;
            }
            return depth;
        }


        private void EmitParserDispatch()
        {
            _sb.AppendLine("        /// <summary>Reads any generated message or type back, by its <c>@type</c>.</summary>");
            _sb.AppendLine("        public static object ParseJSON(JsonNode? node, string what = \"the document\")");
            _sb.AppendLine("        {");
            _sb.AppendLine();
            _sb.AppendLine("            var json = JsonPrimitives.Object(node, what);");
            _sb.AppendLine();
            _sb.AppendLine("            return JsonPrimitives.TypeTag(json, what) switch");
            _sb.AppendLine("            {");

            foreach (var sp in _records)
                _sb.Append("                \"").Append(sp.RecordName).Append("\" => Parse_")
                   .Append(sp.RecordName).AppendLine("(json),");

            foreach (var name in _plan.OpaqueTypes)
                _sb.Append("                \"").Append(name).Append("\" => Parse_")
                   .Append(name).AppendLine("(json),");

            _sb.AppendLine();
            _sb.AppendLine("                var other => throw new JsonLdException(");
            _sb.AppendLine("                                 $\"{what} has @type '{other}', which is not a type of this message set.\"),");
            _sb.AppendLine();
            _sb.AppendLine("            };");
            _sb.AppendLine();
            _sb.AppendLine("        }");
            _sb.AppendLine();
        }


        // -----------------------------------------------------------------------
        //  Per-record
        // -----------------------------------------------------------------------

        private void EmitSerializer(SequencePlan sp, bool isDocument)
        {
            JsonNaming.RequireDistinct(sp.RecordName, FieldNames(sp));

            _sb.Append("        private static JsonObject ToJSON_").Append(sp.RecordName)
               .Append("(").Append(sp.RecordName).AppendLine(" value)");
            _sb.AppendLine("        {");
            _sb.AppendLine();
            _sb.AppendLine("            var json = new JsonObject();");
            _sb.AppendLine();

            if (isDocument)
                _sb.AppendLine("            json[\"@context\"] = Context;");

            _sb.Append("            json[\"@type\"] = \"").Append(sp.RecordName).AppendLine("\";");
            _sb.AppendLine();

            if (sp.Attributes is not null)
                foreach (var a in sp.Attributes)
                    EmitWrite(a.FieldName, a.Type, a.Value,
                              optional: !a.Required, isValueType: IsValueType(a.Type, a.Value));

            if (sp.SimpleContent is not null)
                EmitWrite("Value", sp.SimpleContentType!, sp.SimpleContent, optional: false,
                          isValueType: IsValueType(sp.SimpleContentType!, sp.SimpleContent));

            foreach (var c in sp.Children)
            {
                if (c.Value is ValueEncoding.InlineChoice ic)
                {
                    foreach (var m in ic.Members)
                        EmitWrite(m.FieldName, m.Type, m.Value, optional: true, isValueType: m.IsValueType);
                    continue;
                }

                if (c.Shape == ChildShape.BoundedRepeating)
                {
                    var array = "_" + JsonNaming.Property(c.FieldName);
                    _sb.Append("            var ").Append(array).AppendLine(" = new JsonArray();");
                    _sb.Append("            foreach (var item in value.").Append(c.FieldName).AppendLine(")");
                    // The cast picks JsonArray.Add(JsonNode?) over the generic Add<T>(T). Without
                    // it the generic wins for a JsonObject argument, and it is annotated
                    // RequiresUnreferencedCode/RequiresDynamicCode — every list in every message set
                    // would raise IL2026 and IL3050, and this codec has to survive AOT on a phone.
                    _sb.Append("                ").Append(array).Append(".Add((JsonNode?) ")
                       .Append(Serialize("item", c.Type, c.Value)).AppendLine(");");
                    _sb.Append("            json[\"").Append(JsonNaming.Property(c.FieldName)).Append("\"] = ")
                       .Append(array).AppendLine(";");
                    _sb.AppendLine();
                    continue;
                }

                EmitWrite(c.FieldName, c.Type, c.Value,
                          optional: c.Shape == ChildShape.OptionalSingle, isValueType: c.IsValueType);
            }

            _sb.AppendLine("            return json;");
            _sb.AppendLine();
            _sb.AppendLine("        }");
            _sb.AppendLine();
        }


        private void EmitWrite(string field, TypeRef type, ValueEncoding value, bool optional, bool isValueType)
        {
            var property = JsonNaming.Property(field);

            if (!optional)
            {
                _sb.Append("            json[\"").Append(property).Append("\"] = ")
                   .Append(Serialize("value." + field, type, value)).AppendLine(";");
                return;
            }

            // Absent properties are omitted, not written as null: a JSON-LD document lists what is
            // there. `.Value` only where the field is a nullable value type — a reference type
            // carries its own null, exactly as CSharpSyntax.IsCsNullable decides for the codec.
            var access = "value." + field + (isValueType ? ".Value" : "");

            _sb.Append("            if (value.").Append(field).AppendLine(" is not null)");
            _sb.Append("                json[\"").Append(property).Append("\"] = ")
               .Append(Serialize(access, type, value)).AppendLine(";");
            _sb.AppendLine();
        }


        private void EmitParser(SequencePlan sp)
        {
            _sb.Append("        private static ").Append(sp.RecordName).Append(" Parse_")
               .Append(sp.RecordName).AppendLine("(JsonObject json)");
            _sb.AppendLine("        {");
            _sb.AppendLine();

            var arguments = new List<string>();

            if (sp.Attributes is not null)
                foreach (var a in sp.Attributes)
                    arguments.Add(Read(sp.RecordName, a.FieldName, a.Type, a.Value,
                                       optional: !a.Required, isValueType: IsValueType(a.Type, a.Value)));

            if (sp.SimpleContent is not null)
                arguments.Add(Read(sp.RecordName, "Value", sp.SimpleContentType!, sp.SimpleContent,
                                   optional: false, isValueType: IsValueType(sp.SimpleContentType!, sp.SimpleContent)));

            foreach (var c in sp.Children)
            {
                if (c.Value is ValueEncoding.InlineChoice ic)
                {
                    foreach (var m in ic.Members)
                        arguments.Add(Read(sp.RecordName, m.FieldName, m.Type, m.Value,
                                           optional: true, isValueType: m.IsValueType));
                    continue;
                }

                if (c.Shape == ChildShape.BoundedRepeating)
                {
                    arguments.Add(ReadList(sp.RecordName, c));
                    continue;
                }

                arguments.Add(Read(sp.RecordName, c.FieldName, c.Type, c.Value,
                                   optional: c.Shape == ChildShape.OptionalSingle, isValueType: c.IsValueType));
            }

            if (arguments.Count == 0)
            {
                _sb.Append("            return new ").Append(sp.RecordName).AppendLine("();");
                _sb.AppendLine();
                _sb.AppendLine("        }");
                _sb.AppendLine();
                return;
            }

            _sb.Append("            return new ").Append(sp.RecordName).AppendLine("(");
            for (var i = 0; i < arguments.Count; i++)
            {
                _sb.Append("                       ").Append(arguments[i]);
                _sb.AppendLine(i + 1 < arguments.Count ? "," : "");
            }
            _sb.AppendLine("                   );");
            _sb.AppendLine();
            _sb.AppendLine("        }");
            _sb.AppendLine();
        }


        /// <summary>An empty placeholder for an element in an opaque namespace (XMLDSig).</summary>
        /// <remarks>
        /// It has no content on the wire either — the codec only ever encodes it as absent — so an
        /// empty object carrying nothing but its tag is the whole of it, and round-trips exactly.
        /// </remarks>
        private void EmitOpaque(string name)
        {
            _sb.Append("        private static JsonObject ToJSON_").Append(name)
               .Append("(").Append(name).AppendLine(" value)");
            _sb.AppendLine();
            _sb.Append("            => new JsonObject { [\"@type\"] = \"").Append(name).AppendLine("\" };");
            _sb.AppendLine();
            _sb.Append("        private static ").Append(name).Append(" Parse_").Append(name)
               .AppendLine("(JsonObject json)");
            _sb.AppendLine();
            _sb.Append("            => new ").Append(name).AppendLine("();");
            _sb.AppendLine();
        }


        // -----------------------------------------------------------------------
        //  Values
        // -----------------------------------------------------------------------

        /// <summary>What kind of thing a (type, encoding) pair is, for both directions.</summary>
        private enum Kind { Record, Enumeration, Primitive }

        private Kind KindOf(TypeRef type, ValueEncoding value) => value switch
        {
            ValueEncoding.EnumIndex          => Kind.Enumeration,
            ValueEncoding.ComplexRef         => Kind.Record,
            ValueEncoding.OpaqueElement      => Kind.Record,
            ValueEncoding.SubstitutionChoice => Kind.Record,
            _ => type is TypeRef.Named ? Kind.Record : Kind.Primitive,
        };

        /// <summary>Whether the referent needs C#'s <c>.Value</c> when optional.</summary>
        private bool IsValueType(TypeRef type, ValueEncoding value) =>
            KindOf(type, value) != Kind.Record && type is not TypeRef.Primitive { Kind: PrimitiveKind.String }
                                               && type is not TypeRef.Primitive { Kind: PrimitiveKind.Binary };


        private string Serialize(string access, TypeRef type, ValueEncoding value) =>
            KindOf(type, value) switch
            {
                Kind.Record      => $"ToJSON({access})",
                Kind.Enumeration => $"JsonValue.Create({access}.ToString())",
                _                => SerializePrimitive(access, ((TypeRef.Primitive) type).Kind),
            };

        private static string SerializePrimitive(string access, PrimitiveKind kind) => kind switch
        {
            PrimitiveKind.Int64  => $"JsonValue.Create(JsonPrimitives.FromInt64({access}))",
            PrimitiveKind.UInt64 => $"JsonValue.Create(JsonPrimitives.FromUInt64({access}))",
            PrimitiveKind.Binary => $"JsonValue.Create(JsonPrimitives.ToHex({access}))",
            _                    => $"JsonValue.Create({access})",
        };


        private string Read(string owner, string field, TypeRef type, ValueEncoding value,
                            bool optional, bool isValueType)
        {
            var property = JsonNaming.Property(field);

            if (!optional)
                return ReadFrom($"JsonPrimitives.Required(json, \"{property}\", \"{owner}\")",
                                owner, property, type, value);

            // `is { } _n` reads "present and not null" — the optional half of JsonPrimitives.Optional,
            // spelled here so the null branch can carry the right type for C#'s ternary.
            var local = "_n" + (++_local);
            var nullLiteral = isValueType
                                  ? $"({CSharpSyntax.Syntax(type)}?) null"
                                  : "null";

            return $"JsonPrimitives.Optional(json, \"{property}\") is {{ }} {local} "
                 + $"? {ReadFrom(local, owner, property, type, value)} "
                 + $": {nullLiteral}";
        }


        private string ReadFrom(string node, string owner, string property, TypeRef type, ValueEncoding value) =>
            KindOf(type, value) switch
            {
                Kind.Record      => $"JsonPrimitives.Cast<{CSharpSyntax.Syntax(type)}>("
                                  + $"ParseJSON({node}, \"{owner}.{property}\"), \"{property}\", \"{owner}\")",
                Kind.Enumeration => $"JsonPrimitives.Enumeration<{CSharpSyntax.Syntax(type)}>("
                                  + $"{node}, \"{property}\", \"{owner}\")",
                _                => ReadPrimitive(node, owner, property, ((TypeRef.Primitive) type).Kind),
            };

        private static string ReadPrimitive(string node, string owner, string property, PrimitiveKind kind)
        {
            var method = kind switch
            {
                PrimitiveKind.Bool   => "Bool",
                PrimitiveKind.Int8   => "Int8",
                PrimitiveKind.Int16  => "Int16",
                PrimitiveKind.Int32  => "Int32",
                PrimitiveKind.Int64  => "Int64",
                PrimitiveKind.UInt8  => "UInt8",
                PrimitiveKind.UInt16 => "UInt16",
                PrimitiveKind.UInt32 => "UInt32",
                PrimitiveKind.UInt64 => "UInt64",
                PrimitiveKind.String => "StringValue",
                PrimitiveKind.Binary => "Binary",
                _ => throw new NotSupportedException($"Unmapped primitive kind '{kind}' in the JSON emitter."),
            };

            return $"JsonPrimitives.{method}({node}, \"{property}\", \"{owner}\")";
        }


        /// <summary>
        /// A repeating child, as a JSON array.
        /// </summary>
        /// <remarks>
        /// A list with <c>minOccurs=0</c> tolerates the property being absent and yields an empty
        /// list; one that requires items demands it. The serializer always writes the array, so this
        /// leniency only ever applies to documents from somewhere else.
        /// </remarks>
        private string ReadList(string owner, ChildPlan c)
        {
            var property = JsonNaming.Property(c.FieldName);
            var element  = ReadFrom("item!", owner, property, c.Type, c.Value);
            var type     = CSharpSyntax.Syntax(c.Type);

            var required = $"JsonPrimitives.Array(json, \"{property}\", \"{owner}\")"
                         + $".Select(item => {element}).ToList()";

            if (c.ListMin > 0)
                return required;

            return $"JsonPrimitives.Optional(json, \"{property}\") is JsonArray _a{++_local} "
                 + $"? _a{_local}.Select(item => {element}).ToList() "
                 + $": (IReadOnlyList<{type}>) new List<{type}>()";
        }


        private static IEnumerable<string> FieldNames(SequencePlan sp)
        {
            if (sp.Attributes is not null)
                foreach (var a in sp.Attributes)
                    yield return a.FieldName;

            if (sp.SimpleContent is not null)
                yield return "Value";

            foreach (var c in sp.Children)
            {
                if (c.Value is ValueEncoding.InlineChoice ic)
                {
                    foreach (var m in ic.Members)
                        yield return m.FieldName;
                    continue;
                }
                yield return c.FieldName;
            }
        }

    }
}
