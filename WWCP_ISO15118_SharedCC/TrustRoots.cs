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

using System.Security.Cryptography.X509Certificates;

using cloud.charging.open.protocols.ISO15118.Security;

namespace cloud.charging.open.protocols.ISO15118.SharedCC
{
    /// <summary>
    /// Turning <c>--trust-roots</c> into a <see cref="V2GChainValidator"/>.
    /// </summary>
    /// <remarks>
    /// Both forms exist because both happen. A single root is the common case — one V2G root for one test
    /// hierarchy. A <b>directory</b> is what a run against several counterparties needs: EVerest, Josev and
    /// eVDriveFlow each generate their own root, and a station that should accept cars from all three has to
    /// hold all three anchors at once. That is also how a real CPO's trust store looks, so the directory form
    /// is not merely a convenience.
    /// </remarks>
    public static class TrustRoots
    {

        /// <summary>Certificate extensions a directory scan will pick up. PKCS#12 is deliberately absent:
        /// a trust anchor is a public certificate and has no business carrying a private key.</summary>
        private static readonly string[] Extensions = [".pem", ".crt", ".cer", ".der"];

        /// <summary>
        /// Loads trust roots from a single certificate file or from every certificate in a directory, and
        /// returns a validator over them.
        /// </summary>
        /// <remarks>
        /// A PEM file may hold several certificates and all of them are taken, which is how most tools
        /// export a root bundle. An empty directory throws rather than yielding a validator that rejects
        /// everything: "no roots" and "roots that trust nobody" fail identically at the handshake and mean
        /// entirely different things about the run.
        /// </remarks>
        public static V2GChainValidator Load(string path, string flag = "--trust-roots")
        {
            var roots = new X509Certificate2Collection();

            if (Directory.Exists(path))
            {
                var files = Directory.EnumerateFiles(path)
                                     .Where(f => Extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                                     .OrderBy(f => f, StringComparer.Ordinal)
                                     .ToArray();

                if (files.Length == 0)
                    throw new ArgumentException(
                        $"{flag}: '{path}' is a directory with no certificate in it " +
                        $"(looked for {string.Join(", ", Extensions)}).");

                foreach (var file in files)
                    AddFrom(file, roots, flag);
            }
            else if (File.Exists(path))
                AddFrom(path, roots, flag);
            else
                throw new ArgumentException($"{flag}: no such file or directory '{path}'.");

            if (roots.Count == 0)
                throw new ArgumentException($"{flag}: '{path}' contained no readable certificate.");

            return new V2GChainValidator(roots);
        }

        private static void AddFrom(string file, X509Certificate2Collection into, string flag)
        {
            try
            {
                // Handles PEM (including multi-certificate bundles) and single DER alike.
                var loaded = new X509Certificate2Collection();
                loaded.ImportFromPemFile(file);
                if (loaded.Count == 0)
                    loaded.Add(X509CertificateLoader.LoadCertificateFromFile(file));
                into.AddRange(loaded);
            }
            catch (Exception ex)
            {
                // Try DER before giving up: ImportFromPemFile throws on a binary file rather than returning
                // nothing, so the fallback cannot live in the `Count == 0` branch above.
                try { into.Add(X509CertificateLoader.LoadCertificateFromFile(file)); }
                catch { throw new ArgumentException($"{flag}: '{file}' is not a readable certificate ({ex.Message})."); }
            }
        }

    }
}
