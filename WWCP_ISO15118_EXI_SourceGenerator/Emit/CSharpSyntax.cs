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
using cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Grammar;

namespace cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Emit
{
    /// <summary>
    /// The C#-specific half of code generation that the grammar layer deliberately does not know:
    /// how a language-neutral <see cref="TypeRef"/> is spelled, and when a field needs C#'s
    /// nullable annotation. A Kotlin or Swift emitter supplies its own equivalent of this file —
    /// this is the seam that keeps <c>Xsd/</c> and <c>Grammar/</c> target-language agnostic.
    /// </summary>
    internal static class CSharpSyntax
    {
        /// <summary>The C# spelling of an XSD built-in.</summary>
        public static string Syntax(PrimitiveKind kind) => kind switch
        {
            PrimitiveKind.Bool   => "bool",
            PrimitiveKind.Int8   => "sbyte",
            PrimitiveKind.Int16  => "short",
            PrimitiveKind.Int32  => "int",
            PrimitiveKind.Int64  => "long",
            PrimitiveKind.UInt8  => "byte",
            PrimitiveKind.UInt16 => "ushort",
            PrimitiveKind.UInt32 => "uint",
            PrimitiveKind.UInt64 => "ulong",
            PrimitiveKind.String => "string",
            PrimitiveKind.Binary => "byte[]",
            _ => throw new NotSupportedException($"Unmapped primitive kind '{kind}'."),
        };

        /// <summary>The C# spelling of any type reference.</summary>
        public static string Syntax(TypeRef type) => type switch
        {
            TypeRef.Primitive p => Syntax(p.Kind),
            TypeRef.Named n     => n.Name,
            TypeRef.NoType      => "",   // synthetic child; never dereferenced as a field type
            _ => throw new NotSupportedException($"Unmapped type reference '{type}'."),
        };

        public static string CsType(this ChildPlan          c) => Syntax(c.Type);
        public static string CsType(this AttrPlan           a) => Syntax(a.Type);
        public static string CsType(this InlineChoiceMember m) => Syntax(m.Type);

        /// <summary>
        /// Whether the field needs C#'s <c>?</c> and <c>.Value</c> access: only optional
        /// <em>value</em> types do — a reference type carries its own null. Reference types and
        /// repeating children are addressed directly.
        /// </summary>
        public static bool IsCsNullable(this ChildPlan c) =>
            c.Shape == ChildShape.OptionalSingle && c.IsValueType;

        /// <summary>
        /// An inline-choice branch is always optional (only one branch is ever set), so its
        /// nullability follows from the referent alone.
        /// </summary>
        public static bool IsCsNullable(this InlineChoiceMember m) => m.IsValueType;

        /// <summary>True for <c>xs:boolean</c>, which the encoder special-cases.</summary>
        public static bool IsBool(this ChildPlan c) =>
            c.Type is TypeRef.Primitive { Kind: PrimitiveKind.Bool };
    }
}
