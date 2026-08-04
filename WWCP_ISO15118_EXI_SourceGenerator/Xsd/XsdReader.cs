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
using System.Linq;
using System.Xml.Linq;

namespace cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Xsd
{
    /// <summary>
    /// Parses a tightly-scoped XSD subset into <see cref="XsdSchema"/>.
    /// <para>
    /// Supported: top-level <c>xs:element</c> with inline <c>xs:complexType</c>;
    /// named <c>xs:complexType</c> with <c>xs:sequence</c>; named <c>xs:simpleType</c>
    /// with <c>xs:restriction</c> over <c>xs:string</c> or any unsigned built-in,
    /// carrying <c>xs:minInclusive</c>, <c>xs:maxInclusive</c>, <c>xs:maxLength</c>,
    /// or <c>xs:enumeration</c>.
    /// </para>
    /// <para>
    /// Unsupported constructs surface as <see cref="XsdReaderException"/> with the
    /// path of the offending element, which the generator turns into a build-time
    /// diagnostic. We deliberately fail loud rather than silently skipping; an XSD
    /// feature we don't model is a real gap, not a soft warning.
    /// </para>
    /// </summary>
    internal static class XsdReader
    {
        private static readonly XNamespace Xs = "http://www.w3.org/2001/XMLSchema";

        /// <summary>
        /// Namespaces whose full grammar the generator does not model yet. XMLDSig carries
        /// xs:any / mixed / recursive types that ISO 15118-2 only needs for signed messages
        /// (Phase 3). For Phase 2 the header's optional <c>ds:Signature</c> is always absent,
        /// so the whole namespace is treated as opaque: its types are not built, and the single
        /// reference to it becomes an encode-absent/round-trip-only child.
        /// </summary>
        private const string XmlDsigNamespace = "http://www.w3.org/2000/09/xmldsig#";

        /// <summary>Parse a single XSD document into its own schema.</summary>
        public static XsdSchema Parse(string xml)
        {
            var schema = new XsdSchema();
            AppendDocument(schema, xml, isFirst: true);
            return schema;
        }

        /// <summary>
        /// Parse a set of XSD documents (linked by <c>xs:import</c>) into ONE schema model.
        /// Named types and global elements from every file are merged; <c>xs:import</c> /
        /// <c>xs:include</c> are dependency declarations and need no action because all types
        /// are resolved across the collected set.
        /// </summary>
        public static XsdSchema ParseSet(IEnumerable<string> documents)
        {
            var schema = new XsdSchema();
            bool first = true;
            foreach (var xml in documents)
            {
                AppendDocument(schema, xml, isFirst: first);
                first = false;
            }
            return schema;
        }

        private static void AppendDocument(XsdSchema schema, string xml, bool isFirst)
        {
            var doc = XDocument.Parse(xml);
            var root = doc.Root ?? throw new XsdReaderException("XSD has no root element.");
            if (root.Name != Xs + "schema")
                throw new XsdReaderException($"Root must be xs:schema (got {root.Name}).");

            // The first document's targetNamespace names the set (used only for diagnostics; the
            // emitter picks its own C# namespace).
            if (isFirst)
                schema.TargetNamespace = (string?)root.Attribute("targetNamespace") ?? "";

            var targetNs = (string?)root.Attribute("targetNamespace") ?? "";

            // Record every element declaration (global + local) of this document for the fragment grammar.
            CollectElementDeclarations(schema, root, targetNs);

            if (targetNs == XmlDsigNamespace)
            {
                AppendOpaqueDsigDocument(schema, root);
                return;
            }

            foreach (var st in root.Elements(Xs + "simpleType"))
                ParseNamedSimpleType(st, schema);

            foreach (var ct in root.Elements(Xs + "complexType"))
                schema.ComplexTypes[Required(ct, "name")] = ParseComplexType(ct);

            foreach (var el in root.Elements(Xs + "element"))
                schema.GlobalElements.Add(ParseElement(el) with { Namespace = targetNs });
        }

        /// <summary>
        /// Processes the opaque XMLDSig schema. Every global element name is recorded as opaque (so a
        /// reference to one — the header's <c>ds:Signature</c> — becomes an encode-absent child), and
        /// its elements are NOT added as document roots. From its types only <em>self-contained data
        /// types</em> are exposed — a plain sequence of built-in-typed fields with no reference into the
        /// signature subtree (this is exactly <c>X509IssuerSerialType</c>, which ISO 15118-2's
        /// PaymentDetails genuinely uses). The signature plumbing (xs:any / mixed / recursive, reachable
        /// only through the opaque Signature element) is left unmodelled by design, not silently skipped.
        /// </summary>
        private static void AppendOpaqueDsigDocument(XsdSchema schema, XElement root)
        {
            var targetNs = (string?)root.Attribute("targetNamespace") ?? "";
            foreach (var el in root.Elements(Xs + "element"))
            {
                var n = (string?)el.Attribute("name");
                if (n is null) continue;

                if (DsigSignedInfoElements.Contains(n))
                {
                    // Whitelisted SignedInfo-subtree element: model it as a real global with its type,
                    // so a reference to it resolves to a concrete child (not an opaque absent).
                    schema.GlobalElements.Add(ParseElement(el) with { Namespace = targetNs });
                    continue;
                }

                schema.OpaqueElementNames.Add(n);
                // Opaque elements are not document roots (skipped by the grammar builder) but they DO
                // occupy a production in cbexigen's document grammar, so they must be counted there.
                schema.GlobalElements.Add(new XsdElement(n, "", 1, 1, null) { Namespace = targetNs });
            }

            foreach (var st in root.Elements(Xs + "simpleType"))
                if (DsigSignedInfoTypes.Contains(Required(st, "name")))
                    ParseNamedSimpleType(st, schema);

            foreach (var ct in root.Elements(Xs + "complexType"))
                if (DsigSignedInfoTypes.Contains(Required(ct, "name")) || IsSelfContainedDataType(ct))
                    schema.ComplexTypes[Required(ct, "name")] = ParseComplexType(ct);
        }

        /// <summary>The XMLDSig <c>SignedInfo</c> subtree — the only signature elements ISO 15118-2
        /// actually puts on the wire (the digest is over a SignedInfo fragment). These are modelled
        /// concretely; everything else in the dsig namespace stays opaque.</summary>
        private static readonly HashSet<string> DsigSignedInfoElements = new()
        {
            "Signature", "SignatureValue",
            "SignedInfo", "CanonicalizationMethod", "SignatureMethod", "Reference",
            "Transforms", "Transform", "DigestMethod", "DigestValue",
        };

        private static readonly HashSet<string> DsigSignedInfoTypes = new()
        {
            "SignatureType", "SignatureValueType",
            "SignedInfoType", "CanonicalizationMethodType", "SignatureMethodType", "ReferenceType",
            "TransformsType", "TransformType", "DigestMethodType", "DigestValueType", "HMACOutputLengthType",
        };

        /// <summary>
        /// True for a complex type that is a plain <c>xs:sequence</c> of built-in-typed elements — no
        /// attributes, no inheritance/choice/simpleContent, no element references, no inline types, no
        /// wildcards. Such a type can be modelled standalone; anything else in the opaque namespace
        /// pulls in the signature subtree and stays opaque.
        /// </summary>
        private static bool IsSelfContainedDataType(XElement ct)
        {
            if (ct.Elements(Xs + "attribute").Any()) return false;
            if (ct.Element(Xs + "complexContent") is not null ||
                ct.Element(Xs + "simpleContent")  is not null ||
                ct.Element(Xs + "choice")         is not null) return false;

            var seq = ct.Element(Xs + "sequence");
            if (seq is null) return false;

            // Only xs:element children, each with a built-in xs:* type and no ref/inline.
            foreach (var child in seq.Elements())
            {
                if (child.Name != Xs + "element") return false;               // e.g. xs:any
                if ((string?)child.Attribute("ref") is not null) return false;
                var resolved = ResolveTypeName(child, (string?)child.Attribute("type") ?? "");
                if (!resolved.StartsWith("xs:", StringComparison.Ordinal)) return false;
            }
            return true;
        }

        /// <summary>
        /// Resolves a type QName against its element's in-scope namespace bindings and returns a
        /// normalised reference: <c>"xs:localName"</c> for the XML Schema namespace (built-ins, whether
        /// written <c>xs:int</c>, <c>xsd:int</c>, or unprefixed under a default XSD namespace as in
        /// XMLDSig), otherwise the bare local name (resolved by local name across the collected set).
        /// </summary>
        private static string ResolveTypeName(XElement context, string qname)
        {
            if (string.IsNullOrEmpty(qname)) return qname;
            int i = qname.IndexOf(':');
            XNamespace ns;
            string local;
            if (i < 0)
            {
                local = qname;
                ns = context.GetDefaultNamespace();
            }
            else
            {
                local = qname.Substring(i + 1);
                ns = context.GetNamespaceOfPrefix(qname.Substring(0, i)) ?? XNamespace.None;
            }
            return ns == Xs ? "xs:" + local : local;
        }

        /// <summary>Records every named element declaration (global or local) in the document under its
        /// target namespace (elementFormDefault is "qualified" throughout the -2 set), for the fragment
        /// grammar. Element <em>references</em> carry no declaration and are skipped.</summary>
        private static void CollectElementDeclarations(XsdSchema schema, XElement root, string targetNs)
        {
            foreach (var el in root.Descendants(Xs + "element"))
            {
                var name = (string?)el.Attribute("name");
                if (name is null) continue;
                schema.AllElementDeclarations.Add((name, targetNs));
                var type = (string?)el.Attribute("type");
                if (type is not null)
                    schema.ElementTypeRefs[(name, targetNs)] = ResolveTypeName(el, type);
            }
        }

        private static void ParseNamedSimpleType(XElement st, XsdSchema schema)
        {
            var t = ParseSimpleType(st, Required(st, "name"));
            schema.SimpleTypes[t.Name] = t;
        }

        private static XsdSimpleType ParseSimpleType(XElement st, string name)
        {
            var restriction = st.Element(Xs + "restriction")
                ?? throw new XsdReaderException(
                    $"simpleType '{name}': only restriction-based types are supported in this prototype.");

            var t = new XsdSimpleType { Name = name, Base = ResolveTypeName(restriction, Required(restriction, "base")) };

            foreach (var f in restriction.Elements())
            {
                if (f.Name == Xs + "minInclusive") t.MinInclusive = long.Parse(Required(f, "value"));
                else if (f.Name == Xs + "maxInclusive") t.MaxInclusive = long.Parse(Required(f, "value"));
                else if (f.Name == Xs + "maxLength")    t.MaxLength    = int .Parse(Required(f, "value"));
                else if (f.Name == Xs + "enumeration")
                {
                    t.Enumeration ??= new List<string>();
                    t.Enumeration.Add(Required(f, "value"));
                }
                // Length/pattern/whitespace facets constrain the value space but do not change the
                // EXI wire encoding (length-prefixed strings/binary are encoded the same way), so
                // they are recognised and ignored — not silently skipped.
                else if (f.Name == Xs + "length"     || f.Name == Xs + "minLength" ||
                         f.Name == Xs + "pattern"    || f.Name == Xs + "whiteSpace" ||
                         f.Name == Xs + "totalDigits"|| f.Name == Xs + "fractionDigits")
                {
                    // no wire effect
                }
                else
                    throw new XsdReaderException(
                        $"simpleType '{name}': unsupported facet {f.Name.LocalName}.");
            }

            // EXI canonical ordering for enums is lexicographic over the string form,
            // but we keep declaration order here; the emitter computes the lex-index
            // mapping at code-gen time.

            return t;
        }

        private static XsdComplexType ParseComplexType(XElement ct)
        {
            var name = (string?)ct.Attribute("name") ?? "";
            bool isAbstract = string.Equals((string?)ct.Attribute("abstract"), "true", StringComparison.Ordinal);

            // xs:complexContent / xs:extension base="..."
            var complexContent = ct.Element(Xs + "complexContent");
            if (complexContent is not null)
            {
                var ext = complexContent.Element(Xs + "extension")
                    ?? throw new XsdReaderException(
                        $"complexType '{name}': only xs:extension is supported inside xs:complexContent.");
                var baseRef = ResolveTypeName(ext, Required(ext, "base"));
                var seq = ext.Element(Xs + "sequence");
                var els = seq is null ? new List<XsdElement>() : ParseParticles(seq);
                return new XsdComplexType(name, els, baseRef, isAbstract, ParseAttributes(ext));
            }

            // xs:simpleContent / xs:extension base="..." — a simple value plus attributes
            // (e.g. ContractSignatureEncryptedPrivateKeyType: a base64 value with a required Id).
            var simpleContent = ct.Element(Xs + "simpleContent");
            if (simpleContent is not null)
            {
                var ext = simpleContent.Element(Xs + "extension")
                    ?? throw new XsdReaderException(
                        $"complexType '{name}': only xs:extension is supported inside xs:simpleContent.");
                return new XsdComplexType(name, new List<XsdElement>(), null, isAbstract,
                    ParseAttributes(ext), null, SimpleContentBase: ResolveTypeName(ext, Required(ext, "base")));
            }

            var attributes = ParseAttributes(ct);

            // Direct xs:choice content.
            var directChoice = ct.Element(Xs + "choice");
            if (directChoice is not null)
            {
                var choiceEls = ParseParticles(directChoice);

                int choiceMin = int.Parse((string?)directChoice.Attribute("minOccurs") ?? "1");
                var choiceMaxAttr = (string?)directChoice.Attribute("maxOccurs") ?? "1";
                bool choiceRepeats = string.Equals(choiceMaxAttr, "unbounded", StringComparison.OrdinalIgnoreCase)
                                     || (int.TryParse(choiceMaxAttr, out var cMax) && cMax > 1);

                // A required single-occurrence choice (minOccurs=1, maxOccurs=1, the defaults) is a genuine
                // mutually-exclusive pick — model it as choice content (e.g. ParameterType).
                //
                // But an OPTIONAL and/or REPEATABLE direct choice — the xmldsig TransformType shape,
                // <choice minOccurs="0" maxOccurs="unbounded">{any, XPath}, mixed content — is, in cbexigen's
                // reduced grammar, "(one of the members)? then END-Element": an optional content group
                // terminated by EE, structurally identical to the mixed SignatureMethod/DigestMethod content
                // (an optional element member alongside the wildcard ANY). Model it that way — a plain
                // sequence whose element members are each made optional — so the emitter's EE-terminated
                // optional-run machinery (already byte-exact against cbexigen for SignatureMethodType) emits
                // the right content dispatch (member, EE, wildcard) rather than a mandatory single pick with
                // no EE alternative, which cannot decode an empty Transform.
                //
                // The empty-content case — a bare <Transform Algorithm="…canonical-exi…"/>, the only form that
                // occurs in ISO 15118 signatures and the only one a reference vector covers — is byte-exact vs
                // cbexigen either way. The sequence-vs-choice distinction (whether two members may both appear,
                // in order) only surfaces for present, repeated content, which no ISO 15118 message carries.
                if (choiceMin == 0 || choiceRepeats)
                {
                    var optionalMembers = choiceEls
                        .Select(e => e.MinOccurs == 0 ? e : e with { MinOccurs = 0 })
                        .ToList();
                    return new XsdComplexType(name, optionalMembers, null, isAbstract, attributes);
                }

                return new XsdComplexType(name, new List<XsdElement>(), null, isAbstract, attributes, choiceEls);
            }

            // Direct xs:sequence, or an attribute-only / empty complexType (e.g. abstract BodyBaseType).
            var directSeq = ct.Element(Xs + "sequence");
            if (directSeq is null)
            {
                if (!ct.Elements().Any(e => e.Name.Namespace == Xs &&
                                            e.Name.LocalName != "annotation" &&
                                            e.Name.LocalName != "attribute"))
                    return new XsdComplexType(name, new List<XsdElement>(), null, isAbstract, attributes);
                throw new XsdReaderException(
                    $"complexType '{name}': only xs:sequence or xs:complexContent/xs:extension is supported.");
            }

            var elements = ParseParticles(directSeq);
            return new XsdComplexType(name, elements, null, isAbstract, attributes);
        }

        /// <summary>Parses the <c>xs:element</c> children of a sequence/choice. An <c>xs:any</c> wildcard
        /// is modelled as a single trailing optional <c>ANY</c> element of type <c>base64Binary</c> —
        /// cbexigen's simplification of wildcard content (always absent for the ISO 15118 signature
        /// subtree, which has no foreign content). An <c>xs:choice</c> (ISO 15118-20, e.g.
        /// <c>AuthorizationSetupResType</c>'s EIM/PnC choice) is parsed as an inline-choice marker at its
        /// true document position — see <see cref="ParseInlineChoice"/>; it need not be the last particle
        /// (e.g. <c>EVPowerProfileType</c> has one in the middle, followed by a required list).</summary>
        private static List<XsdElement> ParseParticles(XElement container)
        {
            var els = container.Elements(Xs + "element").Select(ParseElement).ToList();
            if (container.Elements(Xs + "any").Any())
                els.Add(new XsdElement("ANY", "xs:base64Binary", 0, 1, null) { IsWildcard = true });

            var choices = container.Elements(Xs + "choice").ToList();
            if (choices.Count > 1)
                throw new XsdReaderException(
                    "a sequence with more than one xs:choice particle is not supported yet.");
            if (choices.Count == 1)
            {
                var choice = choices[0];
                int position = choice.ElementsBeforeSelf(Xs + "element").Count();
                els.Insert(position, ParseInlineChoice(choice));
            }
            return els;
        }

        /// <summary>
        /// An <c>xs:choice</c> nested inside an <c>xs:sequence</c> (ISO 15118-20's grammar shape,
        /// distinct from -2's root-level "whole content is a choice" and from a substitution-group
        /// reference). cbexigen models each branch as its own independent optional field in the
        /// enclosing record (verified against <c>iso20_AuthorizationSetupResType</c>'s
        /// <c>EIM_ASResAuthorizationMode_isUsed</c> / <c>PnC_ASResAuthorizationMode_isUsed</c> bits) —
        /// not a single polymorphic field as for a substitution group. Only direct
        /// <c>&lt;xs:element name="..." type="..."/&gt;</c> branch members are supported (no <c>ref</c>,
        /// no further nested particles); the branches keep document order (cbexigen assigns event codes
        /// in document order, not alphabetically — verified against <c>SignedInstallationDataType</c>).
        /// </summary>
        private static XsdElement ParseInlineChoice(XElement choice)
        {
            int minOccurs = int.Parse((string?)choice.Attribute("minOccurs") ?? "1");
            var members = new List<XsdElement>();
            foreach (var e in choice.Elements())
            {
                if (e.Name != Xs + "element")
                    throw new XsdReaderException(
                        $"xs:choice inside a sequence: only xs:element members are supported yet (found <{e.Name.LocalName}>).");
                if (e.Attribute("ref") is not null)
                    throw new XsdReaderException(
                        "xs:choice inside a sequence: xs:element ref is not supported yet (only name+type members).");
                var name = Required(e, "name");
                var typeRef = ResolveTypeName(e, (string?)e.Attribute("type")
                    ?? throw new XsdReaderException($"xs:choice member '{name}': must have a type attribute."));
                members.Add(new XsdElement(name, typeRef, 1, 1, null));
            }
            if (members.Count == 0)
                throw new XsdReaderException("xs:choice inside a sequence has no members.");
            return new XsdElement("$InlineChoice", "", minOccurs, 1, null) { InlineChoiceMembers = members };
        }

        private static IReadOnlyList<XsdAttribute>? ParseAttributes(XElement container)
        {
            var attrs = container.Elements(Xs + "attribute").Select(a => new XsdAttribute(
                Required(a, "name"),
                ResolveTypeName(a, (string?)a.Attribute("type") ?? "xs:string"),
                string.Equals((string?)a.Attribute("use"), "required", StringComparison.Ordinal))).ToList();
            return attrs.Count == 0 ? null : attrs;
        }

        private static XsdElement ParseElement(XElement el)
        {
            int minOccurs = int.Parse((string?)el.Attribute("minOccurs") ?? "1");
            var maxAttr = (string?)el.Attribute("maxOccurs") ?? "1";
            int maxOccurs = string.Equals(maxAttr, "unbounded", StringComparison.OrdinalIgnoreCase)
                ? int.MaxValue
                : int.Parse(maxAttr);

            // <xs:element ref="Head"/> — a reference to a (usually abstract, substitution-group)
            // global element. Carries no name/type of its own.
            var refAttr = (string?)el.Attribute("ref");
            if (refAttr is not null)
            {
                var local = StripPrefix(refAttr);
                return new XsdElement(local, "", minOccurs, maxOccurs, null, Ref: local);
            }

            var name = Required(el, "name");
            var typeRef = ResolveTypeName(el, (string?)el.Attribute("type") ?? "");
            var subst = (string?)el.Attribute("substitutionGroup");
            bool isAbstract = string.Equals((string?)el.Attribute("abstract"), "true", StringComparison.Ordinal);

            XsdComplexType? inline = null;
            XsdSimpleType?  inlineSimple = null;
            if (string.IsNullOrEmpty(typeRef))
            {
                var ctElem = el.Element(Xs + "complexType");
                var stElem = el.Element(Xs + "simpleType");
                if (ctElem is not null)
                    inline = ParseComplexType(ctElem);
                else if (stElem is not null)
                    inlineSimple = ParseSimpleType(stElem, name + "_inline");
                else
                    throw new XsdReaderException(
                        $"element '{name}': must have a type attribute or an inline simpleType/complexType.");
            }

            return new XsdElement(name, typeRef, minOccurs, maxOccurs, inline,
                Ref: null,
                SubstitutionGroup: subst is null ? null : StripPrefix(subst),
                IsAbstract: isAbstract,
                InlineSimpleType: inlineSimple);
        }

        private static string StripPrefix(string s)
        {
            int i = s.IndexOf(':');
            return i < 0 ? s : s.Substring(i + 1);
        }

        private static string Required(XElement el, string attr) =>
            (string?)el.Attribute(attr)
            ?? throw new XsdReaderException($"<{el.Name.LocalName}> missing required attribute '{attr}'.");
    }
}
