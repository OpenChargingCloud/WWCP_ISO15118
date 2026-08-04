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
using System.Globalization;
using System.Text.Json.Nodes;

namespace cloud.charging.open.protocols.ISO15118.EXI
{

    /// <summary>
    /// The hand-written half of the generated JSON-LD (de)serializer: reading a value out of a
    /// <see cref="JsonObject"/> and saying precisely what was wrong when it is not there.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every method here exists because the generated alternative would be worse. Emitting the same
    /// six lines of null-checking and range-checking beside each of ~1,800 fields would make the
    /// generated sources several times larger, and each copy would have to be got right by the
    /// emitter rather than once here. This is the same relationship <c>ExiPrimitives</c> has to the
    /// wire codec, and it is not the hand-written codec code the project rules forbid: nothing here
    /// knows a schema, a field or a message.
    /// </para>
    /// <para>
    /// <b>Errors name the field and the type that wanted it.</b> A parse failure deep inside a
    /// nested message is otherwise a null-reference exception with a stack trace full of generated
    /// method names, and the one thing it does not say is which property was missing.
    /// </para>
    /// </remarks>
    public static class JsonPrimitives
    {

        #region Structure

        /// <summary>The node as an object, or a failure that says what it was instead.</summary>
        public static JsonObject Object(JsonNode? node, string what)
            => node as JsonObject
               ?? throw new JsonLdException(node is null
                                                ? $"{what} is missing."
                                                : $"{what} is {Describe(node)}, not a JSON object.");

        /// <summary>
        /// The <c>@type</c> discriminator.
        /// </summary>
        /// <remarks>
        /// Every generated object carries one, which is what makes the format self-describing and a
        /// polymorphic field parseable at all: a substitution-group member is chosen on the wire by
        /// an event code that JSON has no equivalent of, so the concrete type has to be written down.
        /// </remarks>
        public static string TypeTag(JsonObject json, string what)
            => json["@type"]?.GetValue<string>()
               ?? throw new JsonLdException($"{what} has no \"@type\".");

        /// <summary>A required property, as a node.</summary>
        public static JsonNode Required(JsonObject json, string property, string owner)
            => json[property]
               ?? throw new JsonLdException($"{owner} is missing the required property '{property}'.");

        /// <summary>An optional property, as a node — <c>null</c> when absent *or* explicitly null.</summary>
        /// <remarks>
        /// Absent and <c>null</c> are folded together deliberately. The serializer omits absent
        /// values rather than writing nulls, so a null can only come from something else's
        /// serializer, and refusing it would make this stricter than it can justify being.
        /// </remarks>
        public static JsonNode? Optional(JsonObject json, string property)
        {
            var node = json[property];
            return node is null || node.GetValueKind() == System.Text.Json.JsonValueKind.Null ? null : node;
        }

        /// <summary>A required array property.</summary>
        public static JsonArray Array(JsonObject json, string property, string owner)
            => Required(json, property, owner) as JsonArray
               ?? throw new JsonLdException(
                      $"{owner}.{property} is {Describe(json[property]!)}, not a JSON array.");

        /// <summary>
        /// The result of parsing a polymorphic property, checked against the type the field declares.
        /// </summary>
        /// <remarks>
        /// A bare cast would raise <c>InvalidCastException</c> naming two generated type names and
        /// nothing else. This says which property carried the wrong <c>@type</c>, which is the only
        /// part a caller can act on.
        /// </remarks>
        public static T Cast<T>(object parsed, string property, string owner) where T : class
            => parsed as T
               ?? throw new JsonLdException(
                      $"{owner}.{property} has @type '{parsed.GetType().Name}', which is not a "
                    + $"{typeof(T).Name}.");

        #endregion

        #region Values

        public static bool Bool(JsonNode node, string property, string owner)
            => Read(node, property, owner, n => n.GetValue<bool>());

        public static sbyte  Int8  (JsonNode node, string property, string owner) => Number<sbyte> (node, property, owner);
        public static short  Int16 (JsonNode node, string property, string owner) => Number<short> (node, property, owner);
        public static int    Int32 (JsonNode node, string property, string owner) => Number<int>   (node, property, owner);
        public static byte   UInt8 (JsonNode node, string property, string owner) => Number<byte>  (node, property, owner);
        public static ushort UInt16(JsonNode node, string property, string owner) => Number<ushort>(node, property, owner);
        public static uint   UInt32(JsonNode node, string property, string owner) => Number<uint>  (node, property, owner);

        /// <summary>
        /// A number, read through its text rather than through <c>GetValue&lt;T&gt;</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>GetValue&lt;T&gt;</c> is not the same operation on the two kinds of node this can be
        /// handed. A node parsed from text is backed by a <c>JsonElement</c> and converts to any
        /// numeric type that fits; a node built in memory is backed by the exact CLR type it was
        /// created with, and <c>JsonValue.Create(0)</c> is an <c>int</c> that refuses to be read as a
        /// <c>byte</c>. Same document, same value, different answer.
        /// </para>
        /// <para>
        /// That is a trap rather than a nuisance: everything that arrives over the bridge is parsed
        /// from text, so the failure never shows up there — only for a caller that assembles a
        /// message by hand, which is what a test does. Found by exactly that.
        /// </para>
        /// </remarks>
        private static T Number<T>(JsonNode node, string property, string owner) where T : IParsable<T>
        {

            if (node.GetValueKind() != System.Text.Json.JsonValueKind.Number)
                throw new JsonLdException(
                    $"{owner}.{property} is {Describe(node)}, which is not a number.");

            if (T.TryParse(node.ToJsonString(), CultureInfo.InvariantCulture, out var value))
                return value;

            throw new JsonLdException(
                $"{owner}.{property} is {node.ToJsonString()}, which a {typeof(T).Name} cannot hold.");

        }

        /// <summary>
        /// A 64-bit integer, written and read as a <b>JSON string</b>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Not a number, and this is the one place the format deliberately departs from the obvious
        /// encoding. JSON has no integers — only doubles — and the JSON-LD side of this bridge is
        /// consumed by JavaScript, where every number above 2^53 is silently rounded. ISO 15118 has
        /// several fields that reach past it: <c>X509SerialNumber</c> is an <c>xs:long</c> and real
        /// certificate serials use the full range, and <c>TimeAnchor</c>/<c>TimeStamp</c> are
        /// <c>xs:unsignedLong</c>.
        /// </para>
        /// <para>
        /// The corruption would be silent, would not reproduce in C#, and would not be caught by a
        /// round-trip test written in C# either — <c>System.Text.Json</c> handles <c>ulong</c>
        /// exactly. It would appear only on a phone, as a certificate that fails to verify.
        /// </para>
        /// </remarks>
        public static long Int64(JsonNode node, string property, string owner)
            => long.TryParse(StringValue(node, property, owner), NumberStyles.Integer,
                             CultureInfo.InvariantCulture, out var value)
                   ? value
                   : throw new JsonLdException($"{owner}.{property} is not a 64-bit integer.");

        /// <summary>An <c>xs:unsignedLong</c>. A string for the reason <see cref="Int64"/> gives.</summary>
        public static ulong UInt64(JsonNode node, string property, string owner)
            => ulong.TryParse(StringValue(node, property, owner), NumberStyles.Integer,
                              CultureInfo.InvariantCulture, out var value)
                   ? value
                   : throw new JsonLdException($"{owner}.{property} is not an unsigned 64-bit integer.");

        public static string StringValue(JsonNode node, string property, string owner)
            => Read(node, property, owner, n => n.GetValue<string>());

        /// <summary>
        /// An octet string, as lower-case hex.
        /// </summary>
        /// <remarks>
        /// Hex rather than base64 although the XSD has both <c>xs:hexBinary</c> and
        /// <c>xs:base64Binary</c>: the grammar layer collapses them to one kind, so the JSON cannot
        /// tell them apart, and picking one keeps the mapping single-valued. Hex is also what every
        /// vector file and every error message in this repository already shows.
        /// </remarks>
        public static byte[] Binary(JsonNode node, string property, string owner)
        {
            var text = StringValue(node, property, owner);
            try
            {
                return Convert.FromHexString(text);
            }
            catch (FormatException)
            {
                throw new JsonLdException($"{owner}.{property} is not hex.");
            }
        }

        public static byte[] Empty { get; } = System.Array.Empty<byte>();

        /// <summary>Hex for the serializer's side, lower-case.</summary>
        public static string ToHex(byte[] value) => Convert.ToHexString(value).ToLowerInvariant();

        /// <summary>A 64-bit integer as it goes out — see <see cref="Int64"/> for why it is a string.</summary>
        public static string FromInt64(long value)   => value.ToString(CultureInfo.InvariantCulture);
        public static string FromUInt64(ulong value) => value.ToString(CultureInfo.InvariantCulture);

        public static TEnum Enumeration<TEnum>(JsonNode node, string property, string owner)
            where TEnum : struct, Enum
            => Enum.TryParse<TEnum>(StringValue(node, property, owner), out var value)
                   ? value
                   : throw new JsonLdException(
                         $"{owner}.{property} is not a {typeof(TEnum).Name}: '{node}'. "
                       + $"Known: {string.Join(", ", Enum.GetNames<TEnum>())}.");

        #endregion

        private static T Read<T>(JsonNode node, string property, string owner, Func<JsonNode, T> read)
        {
            try
            {
                return read(node);
            }
            catch (Exception ex) when (ex is FormatException or InvalidOperationException or OverflowException)
            {
                throw new JsonLdException(
                    $"{owner}.{property} is {Describe(node)}, which is not a {typeof(T).Name}.");
            }
        }

        private static string Describe(JsonNode node) => node.GetValueKind() switch
        {
            System.Text.Json.JsonValueKind.Object => "an object",
            System.Text.Json.JsonValueKind.Array  => "an array",
            System.Text.Json.JsonValueKind.String => $"the string \"{node.GetValue<string>()}\"",
            System.Text.Json.JsonValueKind.Null   => "null",
            _                                     => $"'{node.ToJsonString()}'",
        };

    }


    /// <summary>
    /// A JSON-LD document that could not be read as the message it claims to be.
    /// </summary>
    /// <remarks>
    /// Its own type rather than <c>JsonException</c>, because these are two different failures with
    /// two different audiences: <c>JsonException</c> means the text is not JSON, which is a transport
    /// problem, while this means the JSON is not a valid message, which is a schema problem.
    /// </remarks>
    public sealed class JsonLdException(string message) : Exception(message);

}
