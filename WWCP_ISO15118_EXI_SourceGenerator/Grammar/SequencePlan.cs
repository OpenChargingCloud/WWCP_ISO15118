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

using System.Collections.Generic;

namespace cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Grammar
{
    internal sealed record SequencePlan(
        string                   RecordName,        // e.g. "AppProtocolEntry"
        IReadOnlyList<ChildPlan> Children,
        int                      ListMin = 0,
        int                      ListMax = 0,
        bool                     IsAbstract = false, // emit as `abstract record`
        string?                  BaseRecordName = null, // extension/substitution base record
        IReadOnlyList<AttrPlan>? Attributes = null,    // AT events (sorted by name), before content
        bool                     IsChoice = false,      // Children are mutually-exclusive xs:choice alternatives
        ValueEncoding?           SimpleContent = null,  // xs:simpleContent: the single content value's encoding
        TypeRef?                 SimpleContentType = null);
}
