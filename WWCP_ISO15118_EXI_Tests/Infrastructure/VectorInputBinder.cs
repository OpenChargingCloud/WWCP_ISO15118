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
using cloud.charging.open.protocols.ISO15118.AppProtocol;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests.Infrastructure
{
    /// <summary>
    /// Maps a <see cref="Vector"/>'s <c>input</c> JSON onto strongly-typed message objects.
    /// Kept separate from the data source so it's easy to extend per new message type.
    /// </summary>
    public static class VectorInputBinder
    {
        public static SupportedAppProtocolReq BindRequest(JsonElement input)
        {
            var entries = new List<AppProtocolEntry>();
            foreach (var e in input.GetProperty("appProtocols").EnumerateArray())
            {
                entries.Add(new AppProtocolEntry(
                    ProtocolNamespace : e.GetProperty("protocolNamespace").GetString()!,
                    VersionNumberMajor: e.GetProperty("versionNumberMajor").GetUInt32(),
                    VersionNumberMinor: e.GetProperty("versionNumberMinor").GetUInt32(),
                    SchemaID          : e.GetProperty("schemaId").GetByte(),
                    Priority          : e.GetProperty("priority").GetByte()));
            }
            return new SupportedAppProtocolReq(entries);
        }

        public static SupportedAppProtocolRes BindResponse(JsonElement input)
        {
            var code = Enum.Parse<ResponseCode>(input.GetProperty("code").GetString()!);

            byte? schemaId = null;
            if (input.TryGetProperty("schemaId", out var sid) &&
                sid.ValueKind != JsonValueKind.Null)
            {
                schemaId = sid.GetByte();
            }
            return new SupportedAppProtocolRes(code, schemaId);
        }
    }
}
