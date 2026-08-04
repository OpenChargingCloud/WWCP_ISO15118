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

using System.Text.Json;
using NUnit.Framework;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests.Infrastructure
{
    /// <summary>
    /// Provides parameterised test data for the AppProtocol codec.
    /// Loads every <c>Vectors\*.vectors.json</c> file copied next to the test assembly
    /// and yields one <see cref="TestCaseData"/> per vector. The vector's <c>name</c>
    /// is used as the test case name so failures show up readably.
    /// </summary>
    public static class AppProtocolVectorSource
    {
        public static IEnumerable<TestCaseData> All()
        {
            var dir = Path.Combine(TestContext.CurrentContext.TestDirectory, "Vectors");
            if (!Directory.Exists(dir)) yield break;

            var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Only the AppProtocol vector files — other *.vectors.json (e.g. Primitives) have
            // their own shape and their own test source.
            foreach (var file in Directory.EnumerateFiles(dir, "AppProtocol*.vectors.json"))
            {
                var doc = JsonSerializer.Deserialize<VectorFile>(File.ReadAllText(file), jsonOpts)
                          ?? throw new InvalidDataException($"Empty vector file: {file}");

                foreach (var v in doc.Vectors)
                {
                    yield return new TestCaseData(Path.GetFileName(file), v)
                        .SetName($"{{m}}({v.Name})");
                }
            }
        }
    }
}
