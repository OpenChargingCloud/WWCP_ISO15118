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

using Microsoft.CodeAnalysis;

namespace cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator
{
    internal static class Diagnostics
    {
        public static readonly DiagnosticDescriptor XsdParseError = new(
            id:                 "EXIGEN001",
            title:              "XSD parse error",
            messageFormat:      "Failed to parse XSD '{0}': {1}",
            category:           "cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator",
            defaultSeverity:    DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnsupportedConstruct = new(
            id:                 "EXIGEN002",
            title:              "Unsupported XSD construct",
            messageFormat:      "XSD '{0}' uses an unsupported construct: {1}",
            category:           "cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator",
            defaultSeverity:    DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InternalError = new(
            id:                 "EXIGEN003",
            title:              "Internal generator error",
            messageFormat:      "Generator failed for '{0}': {1}",
            category:           "cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator",
            defaultSeverity:    DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}
