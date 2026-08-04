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
    /// The TypeScript JSON-LD (de)serializer — the fourth back end of the same pass, held to the
    /// same documents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two things are simpler here than in the other three, and both come from the language.</b>
    /// A JavaScript object preserves the insertion order of its string keys by specification and
    /// <c>JSON.stringify</c> walks them in that order, so there is no hand-written ordered tree and
    /// no writer — the plain object is the document. And a value goes in as itself: a string is a
    /// string, a number is a number, with no wrapper type to construct.
    /// </para>
    /// <para>
    /// <b>One thing is harder.</b> TypeScript's types are gone at runtime, so a polymorphic property
    /// cannot be checked with a cast; the constructor is passed to
    /// <c>JsonPrimitives.cast</c> and the check is an <c>instanceof</c>. Without it a wrong
    /// <c>@type</c> would surface far away, as a missing property on a value of the wrong class.
    /// </para>
    /// <para>
    /// The agreement is checked, not assumed: <c>JsonLd.documents.json</c> holds every vector's JSON
    /// form as C# produces it, and the tests compare character for character. The round trip alone
    /// could not — rename a property in both directions and it stays green.
    /// </para>
    /// </remarks>
    internal sealed class TypeScriptJsonEmitter
    {

        private readonly SchemaPlan _plan;
        private readonly string _module;
        private readonly string _object;
        private readonly StringBuilder _sb = new();

        private readonly List<SequencePlan> _records = new();

        /// <summary>Every type name this schema set declares — the candidate list for imports.</summary>
        private readonly List<string> _declared = new();

        /// <summary>Whether a body names an identifier as a whole word, never as a substring.</summary>
        private static bool Mentions(string body, string identifier)
        {
            for (var i = body.IndexOf(identifier, StringComparison.Ordinal); i >= 0;
                     i = body.IndexOf(identifier, i + 1, StringComparison.Ordinal))
            {
                var before = i == 0 || !(char.IsLetterOrDigit(body[i - 1]) || body[i - 1] == '_');
                var end    = i + identifier.Length;
                var after  = end >= body.Length || !(char.IsLetterOrDigit(body[end]) || body[end] == '_');
                if (before && after) return true;
            }
            return false;
        }
        private readonly Dictionary<string, SequencePlan> _byName = new(StringComparer.Ordinal);


        private TypeScriptJsonEmitter(SchemaPlan plan, string module, string codecObject)
        {
            _plan    = plan;
            _module  = module;
            _object  = codecObject + "Json";

            var seen = new HashSet<string>(StringComparer.Ordinal);

            void Add(SequencePlan sp)
            {
                if (!seen.Add(sp.RecordName)) return;
                _byName[sp.RecordName] = sp;
                if (!sp.IsAbstract) _records.Add(sp);
            }

            foreach (var sp in plan.ComplexTypes.Values) Add(sp);
            foreach (var g in plan.GlobalElements)       Add(g.Body);

            foreach (var e in plan.Enums)                if (!_declared.Contains(e.Name)) _declared.Add(e.Name);
            foreach (var t in plan.OpaqueTypes)          if (!_declared.Contains(t)) _declared.Add(t);
            foreach (var g in plan.GlobalElements)       if (!_declared.Contains(g.TypeName)) _declared.Add(g.TypeName);
            foreach (var sp in plan.ComplexTypes.Values) if (!_declared.Contains(sp.RecordName)) _declared.Add(sp.RecordName);
        }


        public static IReadOnlyList<GeneratedFile> Emit(SchemaPlan plan, string module, string codecObject) =>
            new TypeScriptJsonEmitter(plan, module, codecObject).Run();


        private IReadOnlyList<GeneratedFile> Run()
        {

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

            files.Add(Part(_object, EmitDispatchers));

            return files;
        }


        /// <summary>One generated file. Kotlin has no partial objects, so the per-type halves are
        /// top-level functions next to the type — exactly what the codec emitter does.</summary>
        private GeneratedFile Part(string name, Action emit)
        {
            _sb.Clear();
            emit();
            var body = _sb.ToString();

            var head = new StringBuilder();
            head.AppendLine("// <auto-generated/>");
            head.AppendLine("// Generated by Vanaheimr.V2G.Exi.SourceGenerator (TypeScript back end). Do not edit by hand.");
            head.Append("// Schema target namespace: ").AppendLine(_module);
            head.AppendLine();

            head.AppendLine("import type { JsonObject, JsonValue } from \"../runtime/index.ts\";");

            var runtime = new[] { "JsonLdError", "JsonPrimitives" }.Where(t => Mentions(body, t)).ToList();
            if (runtime.Count > 0)
                head.Append("import { ").Append(string.Join(", ", runtime))
                    .AppendLine(" } from \"../runtime/index.ts\";");

            // ES modules see nothing they were not handed. A JSON part names the type it serialises,
            // every type it casts a polymorphic property to, and the dispatcher object.
            foreach (var other in _declared.OrderBy(t => t, StringComparer.Ordinal))
            {
                // The type and its name table come from the codec's module; the two JSON functions
                // from the JSON module beside it. Two files, so two import lines — and the
                // dispatcher needs both: the class to test with `instanceof`, the functions to call.
                var fromType = new[] { other, other + "Names" }.Where(w => Mentions(body, w)).ToList();
                if (fromType.Count > 0)
                    head.Append("import { ").Append(string.Join(", ", fromType)).Append(" } from \"./")
                        .Append(other).AppendLine(".ts\";");

                // Never from this file's own JSON module — that is this file, and importing a name
                // it declares is a redeclaration. The type import above has no such problem: the
                // class lives in the codec's module, which is always a different file.
                var fromJson = other == name
                                   ? new List<string>()
                                   : new[] { "toJSON" + other, "parseJSON" + other }
                                     .Where(w => Mentions(body, w)).ToList();
                if (fromJson.Count > 0)
                    head.Append("import { ").Append(string.Join(", ", fromJson)).Append(" } from \"./")
                        .Append(other).AppendLine(".Json.ts\";");
            }

            if (name != _object && Mentions(body, _object))
                head.Append("import { ").Append(_object).Append(" } from \"./").Append(_object)
                    .AppendLine(".Json.ts\";");

            head.AppendLine();

            return new GeneratedFile(name + ".Json.ts", head.Append(body).ToString());
        }


        private void EmitDispatchers()
        {
            _sb.Append("export const ").Append(_object).AppendLine(" = {");
            _sb.AppendLine();
            _sb.AppendLine("    /** The vocabulary these messages are written in: the XSD target namespace. */");
            _sb.Append("    context: \"").Append(JsonNaming.Context(_plan.TargetNamespace)).AppendLine("\",");
            _sb.AppendLine();

            // Dispatch is on the RUNTIME type, and branches are ordered most-derived-first for the
            // reason every other type dispatch in this back end is: `instanceof` is true for a base
            // class too, so a base-first order would serialise every derived value as its base and
            // silently drop the fields the derived type added.
            _sb.AppendLine("    /** The JSON-LD form of any generated message or type. */");
            _sb.AppendLine("    toJSON(value: unknown): JsonObject {");

            foreach (var sp in _records.OrderByDescending(Depth).ThenBy(sp => sp.RecordName, StringComparer.Ordinal))
                _sb.Append("        if (value instanceof ").Append(Ident(sp.RecordName)).Append(") return toJSON")
                   .Append(Ident(sp.RecordName)).AppendLine("(value);");

            foreach (var name in _plan.OpaqueTypes)
                _sb.Append("        if (value instanceof ").Append(Ident(name)).Append(") return toJSON")
                   .Append(Ident(name)).AppendLine("(value);");

            _sb.AppendLine();
            _sb.AppendLine("        throw new JsonLdError(");
            _sb.AppendLine("            `${(value as object)?.constructor?.name} is not a type of this message set.`);");
            _sb.AppendLine("    },");
            _sb.AppendLine();

            _sb.AppendLine("    /** Reads any generated message or type back, by its `@type`. */");
            _sb.AppendLine("    parseJSON(node: JsonValue | undefined, what = \"the document\"): unknown {");
            _sb.AppendLine();
            _sb.AppendLine("        const json = JsonPrimitives.object(node, what);");
            _sb.AppendLine();
            _sb.AppendLine("        switch (JsonPrimitives.typeTag(json, what)) {");

            foreach (var sp in _records)
                _sb.Append("            case \"").Append(KStr(sp.RecordName)).Append("\": return parseJSON")
                   .Append(Ident(sp.RecordName)).AppendLine("(json);");

            foreach (var name in _plan.OpaqueTypes)
                _sb.Append("            case \"").Append(KStr(name)).Append("\": return parseJSON")
                   .Append(Ident(name)).AppendLine("(json);");

            _sb.AppendLine();
            _sb.AppendLine("            default: throw new JsonLdError(");
            _sb.AppendLine("                `${what} has @type '${JsonPrimitives.typeTag(json, what)}', "
                         + "which is not a type of this message set.`);");
            _sb.AppendLine("        }");
            _sb.AppendLine("    },");
            _sb.AppendLine("};");
        }


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


        // -----------------------------------------------------------------------
        //  Per-type
        // -----------------------------------------------------------------------

        private void EmitSerializer(SequencePlan sp, bool isDocument)
        {
            JsonNaming.RequireDistinct(sp.RecordName, FieldNames(sp));

            _sb.Append("export function toJSON").Append(Ident(sp.RecordName))
               .Append("(value: ").Append(Ident(sp.RecordName)).AppendLine("): JsonObject {");
            _sb.AppendLine();
            _sb.AppendLine("    const json: JsonObject = {};");
            _sb.AppendLine();

            if (isDocument)
                _sb.Append("    json[\"@context\"] = ").Append(_object).AppendLine(".context;");

            _sb.Append("    json[\"@type\"] = \"").Append(KStr(sp.RecordName)).AppendLine("\";");
            _sb.AppendLine();

            if (sp.Attributes is not null)
                foreach (var a in sp.Attributes)
                    EmitWrite(a.FieldName, a.Type, a.Value, optional: !a.Required);

            if (sp.SimpleContent is not null)
                EmitWrite(SimpleContentField, sp.SimpleContentType!, sp.SimpleContent, optional: false);

            foreach (var c in sp.Children)
            {
                if (c.Value is ValueEncoding.InlineChoice ic)
                {
                    foreach (var m in ic.Members)
                        EmitWrite(m.FieldName, m.Type, m.Value, optional: true);
                    continue;
                }

                if (c.Shape == ChildShape.BoundedRepeating)
                {
                    _sb.Append("    json[\"").Append(JsonNaming.Property(c.FieldName)).Append("\"] = value.")
                       .Append(Prop(c.FieldName)).Append(".map(item => ")
                       .Append(Serialize("item", c.Type, c.Value)).AppendLine(");");
                    _sb.AppendLine();
                    continue;
                }

                EmitWrite(c.FieldName, c.Type, c.Value, optional: c.Shape == ChildShape.OptionalSingle);
            }

            _sb.AppendLine("    return json;");
            _sb.AppendLine("}");
            _sb.AppendLine();
        }


        private void EmitWrite(string field, TypeRef type, ValueEncoding value, bool optional)
        {
            var property = JsonNaming.Property(field);

            if (!optional)
            {
                _sb.Append("    json[\"").Append(property).Append("\"] = ")
                   .Append(Serialize("value." + Prop(field), type, value)).AppendLine(";");
                return;
            }

            // `?.let` rather than an `if` with a smart cast: an overridden `open val` has no smart
            // Absent properties are omitted, never written as null — a JSON-LD document lists what
            // is there.
            _sb.Append("    if (value.").Append(Prop(field)).AppendLine(" !== null) {");
            _sb.Append("        json[\"").Append(property).Append("\"] = ")
               .Append(Serialize("value." + Prop(field), type, value)).AppendLine(";");
            _sb.AppendLine("    }");
            _sb.AppendLine();
        }


        private void EmitParser(SequencePlan sp)
        {
            _sb.Append("export function parseJSON").Append(Ident(sp.RecordName))
               .Append("(json: JsonObject): ").Append(Ident(sp.RecordName)).AppendLine(" {");

            var arguments = new List<string>();

            if (sp.Attributes is not null)
                foreach (var a in sp.Attributes)
                    arguments.Add(Read(sp.RecordName, a.FieldName, a.Type, a.Value, optional: !a.Required));

            if (sp.SimpleContent is not null)
                arguments.Add(Read(sp.RecordName, SimpleContentField, sp.SimpleContentType!,
                                   sp.SimpleContent, optional: false));

            foreach (var c in sp.Children)
            {
                if (c.Value is ValueEncoding.InlineChoice ic)
                {
                    foreach (var m in ic.Members)
                        arguments.Add(Read(sp.RecordName, m.FieldName, m.Type, m.Value, optional: true));
                    continue;
                }

                if (c.Shape == ChildShape.BoundedRepeating)
                {
                    arguments.Add(ReadList(sp.RecordName, c));
                    continue;
                }

                arguments.Add(Read(sp.RecordName, c.FieldName, c.Type, c.Value,
                                   optional: c.Shape == ChildShape.OptionalSingle));
            }

            if (arguments.Count == 0)
            {
                _sb.Append("    return new ").Append(Ident(sp.RecordName)).AppendLine("();");
                _sb.AppendLine("}");
                _sb.AppendLine();
                return;
            }

            _sb.Append("    return new ").Append(Ident(sp.RecordName)).AppendLine("(");
            for (var i = 0; i < arguments.Count; i++)
            {
                _sb.Append("        ").Append(arguments[i]);
                _sb.AppendLine(i + 1 < arguments.Count ? "," : "");
            }
            _sb.AppendLine("    );");
            _sb.AppendLine("}");
            _sb.AppendLine();
        }


        /// <summary>An element in an opaque namespace (XMLDSig): no content on the wire, none here.</summary>
        private void EmitOpaque(string name)
        {
            _sb.Append("export function toJSON").Append(Ident(name)).Append("(value: ").Append(Ident(name))
               .AppendLine("): JsonObject {");
            _sb.AppendLine();
            _sb.AppendLine("    const json: JsonObject = {};");
            _sb.Append("    json[\"@type\"] = \"").Append(KStr(name)).AppendLine("\";");
            _sb.AppendLine("    return json;");
            _sb.AppendLine("}");
            _sb.AppendLine();
            _sb.Append("export function parseJSON").Append(Ident(name)).Append("(json: JsonObject): ")
               .Append(Ident(name)).AppendLine(" {");
            _sb.Append("    return new ").Append(Ident(name)).AppendLine("();");
            _sb.AppendLine("}");
            _sb.AppendLine();
        }


        // -----------------------------------------------------------------------
        //  Values
        // -----------------------------------------------------------------------

        private enum Kind { Record, Enumeration, Primitive }

        private static Kind KindOf(TypeRef type, ValueEncoding value) => value switch
        {
            ValueEncoding.EnumIndex          => Kind.Enumeration,
            ValueEncoding.ComplexRef         => Kind.Record,
            ValueEncoding.OpaqueElement      => Kind.Record,
            ValueEncoding.SubstitutionChoice => Kind.Record,
            _ => type is TypeRef.Named ? Kind.Record : Kind.Primitive,
        };


        private string Serialize(string access, TypeRef type, ValueEncoding value) =>
            KindOf(type, value) switch
            {
                Kind.Record      => $"{_object}.toJSON({access})",
                // The wire value IS the index, so the name table is what turns it back into a name.
                Kind.Enumeration => $"{((ValueEncoding.EnumIndex) value).EnumName}Names[{access}]",
                _                => SerializePrimitive(access, ((TypeRef.Primitive) type).Kind),
            };

        /// <summary>
        /// A primitive on its way out. Most go in as themselves — a string is a string and a number
        /// is a number, with no wrapper to construct, which is the one place this back end does less
        /// work than the other three.
        /// </summary>
        private static string SerializePrimitive(string access, PrimitiveKind kind) => kind switch
        {
            PrimitiveKind.Int64  => $"{access}.toString()",
            PrimitiveKind.UInt64 => $"{access}.toString()",
            PrimitiveKind.Binary => $"JsonPrimitives.toHex({access})",
            _                    => access,
        };


        private string Read(string owner, string field, TypeRef type, ValueEncoding value, bool optional)
        {
            var property = JsonNaming.Property(field);

            // The optional form reads the node once into `n`, because the arrow body needs it twice
            // — once to test and once to convert — and reading a property twice is how a serializer
            // that mutates between reads would slip through.
            return optional
                       ? $"((n) => n === null ? null : {ReadFrom("n", owner, property, type, value)})"
                       + $"(JsonPrimitives.optional(json, \"{property}\"))"
                       : ReadFrom($"JsonPrimitives.required(json, \"{property}\", \"{KStr(owner)}\")",
                                  owner, property, type, value);
        }


        private string ReadFrom(string node, string owner, string property, TypeRef type, ValueEncoding value) =>
            KindOf(type, value) switch
            {
                // The constructor is passed because TypeScript's types are gone at runtime: an
                // `as T` would assert without checking, and a wrong `@type` would then surface far
                // away as a missing property on a value of the wrong class.
                Kind.Record      => $"JsonPrimitives.cast({_object}.parseJSON({node}, "
                                  + $"\"{KStr(owner)}.{property}\"), {Ident(Type(type))}, "
                                  + $"\"{property}\", \"{KStr(owner)}\")",
                Kind.Enumeration => $"JsonPrimitives.enumeration({node}, \"{property}\", "
                                  + $"\"{KStr(owner)}\", \"{KStr(Type(type))}\", {Ident(Type(type))}Names) "
                                  + $"as {Ident(Type(type))}",
                _                => ReadPrimitive(node, owner, property, ((TypeRef.Primitive) type).Kind),
            };

        private static string ReadPrimitive(string node, string owner, string property, PrimitiveKind kind)
        {
            var method = kind switch
            {
                // One reader per JavaScript type rather than per XSD width: `number` holds every
                // width below 64 bits exactly, and a reader per width would be four names for one
                // check.
                PrimitiveKind.Bool   => "bool",
                PrimitiveKind.Int8   => "int",
                PrimitiveKind.Int16  => "int",
                PrimitiveKind.Int32  => "int",
                PrimitiveKind.Int64  => "big",
                PrimitiveKind.UInt8  => "int",
                PrimitiveKind.UInt16 => "int",
                PrimitiveKind.UInt32 => "int",
                PrimitiveKind.UInt64 => "big",
                PrimitiveKind.String => "stringValue",
                PrimitiveKind.Binary => "binary",
                _ => throw new NotSupportedException($"TypeScript JSON back end: primitive '{kind}'."),
            };

            return $"JsonPrimitives.{method}({node}, \"{property}\", \"{KStr(owner)}\")";
        }


        /// <summary>A repeating child. A list that may be empty tolerates the property being absent;
        /// one that requires items demands it.</summary>
        private string ReadList(string owner, ChildPlan c)
        {
            var property = JsonNaming.Property(c.FieldName);
            var element  = ReadFrom("it", owner, property, c.Type, c.Value);

            return c.ListMin > 0
                       ? $"JsonPrimitives.array(json, \"{property}\", \"{KStr(owner)}\")"
                       + $".map(it => {element})"
                       : $"((a) => Array.isArray(a) ? a.map(it => {element}) : [])"
                       + $"(JsonPrimitives.optional(json, \"{property}\"))";
        }


        private static IEnumerable<string> FieldNames(SequencePlan sp)
        {
            if (sp.Attributes is not null)
                foreach (var a in sp.Attributes)
                    yield return a.FieldName;

            if (sp.SimpleContent is not null)
                yield return SimpleContentField;

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


        // -----------------------------------------------------------------------
        //  TypeScript spelling
        // -----------------------------------------------------------------------
        //
        //  Deliberate copies of TypeScriptCodecEmitter's private helpers rather than a shared
        //  layer: the two emitters have to agree, and a disagreement is not silent — it produces
        //  TypeScript that does not load, which is the cheapest possible way to find out.

        private const string SimpleContentField = "Value";

        private static readonly HashSet<string> TypeScriptKeywords = new(StringComparer.Ordinal)
        {
            "break", "case", "catch", "class", "const", "continue", "debugger", "default", "delete",
            "do", "else", "enum", "export", "extends", "false", "finally", "for", "function", "if",
            "import", "in", "instanceof", "new", "null", "return", "super", "switch", "this",
            "throw", "true", "try", "typeof", "var", "void", "while", "with",
        };

        private static string KStr(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("$", "\\$");

        private static string Camel(string pascal) =>
            pascal.Length == 0 ? pascal : char.ToLowerInvariant(pascal[0]) + pascal.Substring(1);

        private static string Prop(string pascal) => Ident(Camel(pascal));

        private static string Ident(string name)
        {
            if (string.IsNullOrEmpty(name)) return "_";

            var sb = new StringBuilder(name.Length);
            foreach (var c in name)
                sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');

            var identifier = sb.ToString();
            return TypeScriptKeywords.Contains(identifier) ? identifier + "_" : identifier;
        }

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
                _ => throw new NotSupportedException($"TypeScript JSON back end: primitive {p.Kind}."),
            },
            TypeRef.Named n => n.Name,
            _ => throw new NotSupportedException("TypeScript JSON back end: untyped child."),
        };

    }
}
