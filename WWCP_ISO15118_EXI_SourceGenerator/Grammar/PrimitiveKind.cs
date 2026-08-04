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

namespace cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Grammar
{
    /// <summary>
    /// An XSD built-in datatype, named independently of any target language.
    /// Each emitter maps these to its own syntax (C# <c>uint</c>, Kotlin <c>UInt</c>,
    /// Swift <c>UInt32</c>, …) — the grammar layer never spells a language's type names.
    /// </summary>
    /// <remarks>
    /// The width names describe the XSD value space, not a storage promise: how a target
    /// language represents <see cref="UInt64"/> is the emitter's business. The EXI wire
    /// encoding is carried separately by <see cref="ValueEncoding"/> and does not follow
    /// from this enum — <c>xs:unsignedByte</c> and <c>xs:byte</c> are both 8-bit here but
    /// encode as n-bit fields with different biases.
    /// </remarks>
    internal enum PrimitiveKind
    {
        Bool,

        Int8,
        Int16,
        Int32,
        Int64,

        UInt8,
        UInt16,
        UInt32,
        UInt64,

        String,

        /// <summary>xs:hexBinary / xs:base64Binary — an octet sequence.</summary>
        Binary,
    }
}
