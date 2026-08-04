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

using System;
using System.Collections.Generic;
using System.Text;

namespace cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Emit
{
    /// <summary>
    /// How the JSON-LD form names things. Language-neutral on purpose: the C#, Kotlin and Swift
    /// JSON-LD emitters must produce byte-identical documents for the same message, so the rule that
    /// decides a property name lives above all three rather than three times inside them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The rule is mechanical, and that is the requirement.</b> A hand-maintained name table
    /// beside a generated codec is the drift §4.4 set out to remove; a rule derived from the XSD name
    /// cannot fall behind a schema update because there is nothing to update.
    /// </para>
    /// <para>
    /// <b>Where it departs from WWCP's conventions, and why.</b> §4.4 keeps WWCP's hand-written
    /// JSON-LD as style guidance, and one of its conventions is dropped deliberately: WWCP has no
    /// <c>Req</c>/<c>Res</c> suffixes on its type names, and here they stay. In the generated form
    /// <c>@type</c> is the only discriminator a reader has — stripping the suffix would map
    /// <c>AuthorizationReq</c> and <c>AuthorizationRes</c> onto one name, and a request that parses
    /// as a response is exactly the failure the tag exists to prevent. WWCP could afford the
    /// convention because its request and response types are reached through different code paths.
    /// </para>
    /// </remarks>
    internal static class JsonNaming
    {

        /// <summary>
        /// The JSON property name for a generated field: its PascalCase name, lower-camelised.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The whole leading run of capitals is lowered, except that its last letter is kept when a
        /// lower-case letter follows — the usual reading of an acronym boundary. So <c>Header</c>
        /// becomes <c>header</c>, <c>EVCCID</c> becomes <c>evccid</c>, and <c>EVSEStatus</c> becomes
        /// <c>evseStatus</c> rather than <c>eVSEStatus</c>, which is what a naïve "lower the first
        /// character" rule produces and which is unreadable in a format meant to be looked at.
        /// </para>
        /// <para>
        /// Digits and underscores end a run without being part of it: <c>X509SerialNumber</c> becomes
        /// <c>x509SerialNumber</c>, and <c>DC_ChargeParameter</c> becomes <c>dc_ChargeParameter</c>.
        /// The underscore is left alone rather than smoothed away, because it is in the XSD name and
        /// removing it would make <c>AC_Something</c> and <c>ACSomething</c> the same property.
        /// </para>
        /// </remarks>
        public static string Property(string fieldName)
        {
            if (fieldName.Length == 0)
                return fieldName;

            var run = 0;
            while (run < fieldName.Length && char.IsUpper(fieldName[run]))
                run++;

            if (run == 0)
                return fieldName;

            // A capital that begins the next word keeps its case: "EVSE" + "Status".
            if (run > 1 && run < fieldName.Length && char.IsLower(fieldName[run]))
                run--;

            var sb = new StringBuilder(fieldName.Length);
            for (var i = 0; i < run; i++)
                sb.Append(char.ToLowerInvariant(fieldName[i]));
            sb.Append(fieldName, run, fieldName.Length - run);

            return sb.ToString();
        }


        /// <summary>
        /// The <c>@context</c> for a schema set: the XSD target namespace, unchanged.
        /// </summary>
        /// <remarks>
        /// A URN rather than an invented <c>https://</c> URL. The namespace is the identifier ISO
        /// already assigns to this vocabulary, it is what the XML form carries, and it is stable
        /// across whatever this project's hosting looks like — an invented URL would be a second
        /// name for the same thing, and someone would eventually have to serve it.
        /// </remarks>
        public static string Context(string targetNamespace) => targetNamespace;


        /// <summary>
        /// Checks that a record's properties are distinct, and says which two collided if not.
        /// </summary>
        /// <remarks>
        /// <see cref="Property"/> is not injective in principle — <c>ID</c> and <c>Id</c> both lower
        /// to <c>id</c> — and a collision would be silent in the worst possible way: the serializer
        /// would write one property twice and the round-trip would return whichever won, with no
        /// error anywhere. Nothing in the current schemas collides; this is here so that a future
        /// amendment that introduces one fails the build instead of the phone. Fail-loud, as the
        /// project rules ask of every unhandled construct.
        /// </remarks>
        public static void RequireDistinct(string recordName, IEnumerable<string> fieldNames)
        {
            var seen = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var field in fieldNames)
            {
                var property = Property(field);

                if (seen.TryGetValue(property, out var other))
                    throw new NotSupportedException(
                        $"{recordName}.{field} and {recordName}.{other} both map to the JSON property "
                      + $"'{property}'. The JSON-LD naming rule must stay one-to-one — a collision "
                      + $"would silently drop one of the two fields on the bridge.");

                seen[property] = field;
            }
        }

    }
}
