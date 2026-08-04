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

namespace cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Grammar
{
    /// <summary>
    /// The type of a field, named independently of any target language: either an XSD
    /// built-in (<see cref="Primitive"/>) or a generated type referred to by name
    /// (<see cref="Named"/> — a record, an enum, or an opaque placeholder).
    /// </summary>
    /// <remarks>
    /// <see cref="Named"/> deliberately does not distinguish record from enum: whether the
    /// referent is a value type is carried alongside (<c>IsValueType</c> on the owning plan),
    /// because that is a property of the referent, not of the reference. Emitters that need
    /// the distinction — C# nullability, for instance — read that flag.
    /// </remarks>
    internal abstract record TypeRef
    {
        /// <summary>An XSD built-in datatype.</summary>
        public sealed record Primitive(PrimitiveKind Kind) : TypeRef;

        /// <summary>A generated type, referred to by its (PascalCase) name.</summary>
        public sealed record Named(string Name) : TypeRef;

        /// <summary>
        /// No type of its own. Used by synthetic children that exist only to carry a
        /// <see cref="ValueEncoding"/> — the inline-choice placeholder, whose branches are
        /// each their own field, so the placeholder itself is never dereferenced.
        /// </summary>
        public sealed record NoType : TypeRef;

        public static readonly TypeRef None = new NoType();
    }
}
