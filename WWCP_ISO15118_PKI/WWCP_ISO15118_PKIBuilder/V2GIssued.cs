/*
 * Copyright (c) 2014-2025 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

#region Usings

using Org.BouncyCastle.X509;
using Org.BouncyCastle.Crypto;

#endregion

namespace cloud.charging.open.protocols.ISO15118.PKI
{

    /// <summary>
    /// One issued certificate plus its private key. Used both for CAs and leaves.
    /// </summary>
    public sealed record V2GIssued(V2GCertProfile           Profile,
                                   V2GAlgorithm             Algorithm,
                                   AsymmetricCipherKeyPair  KeyPair,
                                   X509Certificate          Certificate)
    {

        /// <summary>
        /// The slug is a short, lowercase, alphanumeric-and-underscore string
        /// derived from the profile's common name, used for filenames and test labels.
        /// </summary>
        public String  Slug

            => SlugOf(Profile.CommonName);


        /// <summary>
        /// Derives a slug from the given text by lowercasing it, replacing non-alphanumeric
        /// characters with underscores, and collapsing multiple underscores into one
        /// and trimming leading/trailing underscores.
        /// </summary>
        /// <param name="Text">The text to derive the slug from, typically a common name.</param>
        public static String  SlugOf(String Text)

            => new String(
                   [.. Text.ToLowerInvariant().Select(c => Char.IsLetterOrDigit(c) ? c : '_')]
               ).Trim('_')
                .Replace("__", "_");

    }

}
