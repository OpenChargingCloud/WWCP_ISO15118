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

using System.Text;

namespace cloud.charging.open.protocols.ISO15118.SLAC.Util;

public static class Hex
{

    /// <summary>
    /// Multi-line hex dump with offset + ASCII gutter, like xxd.
    /// </summary>
    public static string Dump(ReadOnlySpan<Byte> data, int bytesPerLine = 16)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < data.Length; i += bytesPerLine)
        {
            sb.Append($"  {i:X4}  ");

            var len = Math.Min(bytesPerLine, data.Length - i);
            for (var j = 0; j < bytesPerLine; j++)
            {
                if (j < len) sb.Append($"{data[i + j]:X2} ");
                else         sb.Append("   ");
                if (j == 7) sb.Append(' ');
            }

            sb.Append(' ');
            for (var j = 0; j < len; j++)
            {
                var b = data[i + j];
                sb.Append(b >= 0x20 && b < 0x7F ? (char) b : '.');
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static string Compact(ReadOnlySpan<Byte> data)
    {
        var sb = new StringBuilder(data.Length * 3);
        for (var i = 0; i < data.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.Append($"{data[i]:X2}");
        }
        return sb.ToString();
    }

}
