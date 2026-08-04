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

using System.Text.Json;
using System.Text.Json.Serialization;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests.Infrastructure
{
    /// <summary>One test vector. <see cref="Input"/> is schema-dependent; tests parse it per <see cref="MessageType"/>.</summary>
    public sealed record Vector(
        [property: JsonPropertyName("name")]          string      Name,
        [property: JsonPropertyName("description")]   string      Description,
        [property: JsonPropertyName("messageType")]   string      MessageType,
        [property: JsonPropertyName("input")]         JsonElement Input,
        [property: JsonPropertyName("expectedBytes")] int         ExpectedBytes,
        [property: JsonPropertyName("expectedHex")]   string      ExpectedHex)
    {
        // Keeps NUnit's auto-generated test names short; the explicit SetName in
        // AppProtocolVectorSource takes precedence anyway, but TRX output and
        // diagnostics also use this.
        public override string ToString() => $"{MessageType}/{Name}";
    }
}
