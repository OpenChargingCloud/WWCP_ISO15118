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
    /// Per-child plan inside a sequence — combines the value encoding with the EXI
    /// event-code wrapping (mandatory / optional / repeating).
    /// </summary>
    internal sealed record ChildPlan(
        string         FieldName,        // PascalCase as in the message record
        TypeRef        Type,             // built-in kind, or a named record/enum
        bool           IsValueType,      // of the referent; emitters derive their own nullability
        ChildShape     Shape,
        ValueEncoding  Value,
        int            ListMin = 0,      // for BoundedRepeating children
        int            ListMax = 0,
        bool           IsWildcardAny = false);   // synthetic ANY from an xs:any wildcard (two productions)
}
