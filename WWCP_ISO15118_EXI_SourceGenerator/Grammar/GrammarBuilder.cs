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
using cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Xsd;

namespace cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Grammar
{
    internal static class StringExt
    {
        public static string TrimSuffix(this string s, string suffix) =>
            s.EndsWith(suffix, StringComparison.Ordinal) && s.Length > suffix.Length
                ? s.Substring(0, s.Length - suffix.Length) : s;
    }

    /// <summary>
    /// Lowers an <see cref="XsdSchema"/> to a <see cref="SchemaPlan"/> that the
    /// emitter can consume mechanically.
    /// </summary>
    internal static class GrammarBuilder
    {
        private const string XmlDsigNamespace = "http://www.w3.org/2000/09/xmldsig#";

        public static SchemaPlan Build(XsdSchema schema) => Build(schema, System.Array.Empty<string>());

        public static SchemaPlan Build(XsdSchema schema, IReadOnlyList<string> fragmentElements)
            => Build(schema, fragmentElements, DocumentElementOrder.CbV2GCompatible);

        /// <summary>
        /// Numbers the schema's global elements for the document grammar. Public to the tests, because
        /// this is the one place where a wire-format fork lives and it deserves to be checked directly
        /// rather than inferred from generated output.
        /// </summary>
        /// <remarks>
        /// Both modes start from plain lexicographic order over (name, namespace). They diverge only
        /// when several global elements share one named type: <see cref="DocumentElementOrder.ExiSorted"/>
        /// leaves them where the sort put them, while <see cref="DocumentElementOrder.CbV2GCompatible"/>
        /// pulls the whole group up behind the alphabetically-first element of that type, which is what
        /// cbexigen emits. Types used by a single element are untouched either way — that is why every
        /// ISO 15118 schema set except ACDP is numbered identically by both.
        /// </remarks>
        internal static List<(string Name, string Namespace, string? TypeKey)> OrderDocumentElements(
            XsdSchema schema, DocumentElementOrder order)
        {
            var byName = schema.GlobalElements
                .Select(g => (g.Name, g.Namespace, TypeKey: string.IsNullOrEmpty(g.TypeRef) ? null : StripPrefix(g.TypeRef)))
                .OrderBy(x => x.Name, StringComparer.Ordinal)
                .ThenBy(x => x.Namespace, StringComparer.Ordinal)
                .ToList();

            if (order == DocumentElementOrder.ExiSorted)
                return byName;

            var sharedTypeGroups = byName
                .Where(x => x.TypeKey is not null)
                .GroupBy(x => x.TypeKey)
                .Where(g => g.Count() > 1)
                .ToDictionary(g => g.Key!, g => g.ToList());

            var placed   = new HashSet<string>();
            var grouped  = new List<(string Name, string Namespace, string? TypeKey)>();
            foreach (var x in byName)
            {
                if (!placed.Add(x.Name + "\0" + x.Namespace)) continue;
                grouped.Add(x);
                if (x.TypeKey is not null && sharedTypeGroups.TryGetValue(x.TypeKey, out var group))
                    foreach (var member in group)
                        if (placed.Add(member.Name + "\0" + member.Namespace))
                            grouped.Add(member);
            }
            return grouped;
        }

        public static SchemaPlan Build(XsdSchema schema, IReadOnlyList<string> fragmentElements,
                                       DocumentElementOrder documentElementOrder)
            => Build(schema, fragmentElements, documentElementOrder, ParticleGrammar.CbV2GCompatible);

        public static SchemaPlan Build(XsdSchema schema, IReadOnlyList<string> fragmentElements,
                                       DocumentElementOrder documentElementOrder,
                                       ParticleGrammar particleGrammar)
        {
            var enums = new List<EnumPlan>();
            var opaqueTypes = new List<string>();

            // Build per-named-complexType plans.
            var complex = new Dictionary<string, SequencePlan>();
            foreach (var kv in schema.ComplexTypes)
                complex[kv.Key] = BuildSequence(kv.Key, kv.Value, schema, enums, opaqueTypes);

            // The document grammar enumerates EVERY global element of the collected set (abstract
            // substitution heads, their members, opaque XMLDSig elements, …), sorted by element name
            // then namespace — cbexigen assigns each a production even though only true roots are
            // decodable. The selector width and each root's index come from this full list (verified
            // against cbV2G: V2G_Message is index 76 of 80, a 7-bit selector).
            //
            // Where the two orders differ, and why there is a choice at all, is in DocumentElementOrder.
            var docOrder = OrderDocumentElements(schema, documentElementOrder);
            int docBits = BitsForChoices(docOrder.Count + 1);

            // Build global-element plans for the true document roots — a concrete, non-substituting,
            // non-opaque global element (V2G_Message; supportedAppProtocolReq/Res). Abstract heads and
            // substitution members are reached through the substitution choice, not as roots.
            var globals = new List<GlobalElementPlan>();
            foreach (var ge in schema.GlobalElements)
            {
                if (ge.IsAbstract || ge.SubstitutionGroup is not null || ge.Ref is not null)
                    continue;
                if (schema.OpaqueElementNames.Contains(ge.Name))
                    continue;
                // Whitelisted XMLDSig SignedInfo-subtree elements are modelled (their type codecs exist)
                // but are never V2G document roots — they are reached only through a fragment or a
                // containing type. They still occupy a document-grammar production (counted above).
                if (ge.Namespace == XmlDsigNamespace)
                    continue;
                // ISO 15118-20's substitution-group heads (e.g. CommonTypes' CLReqControlMode) carry the
                // `abstract` flag on their TYPE, not on the element declaration itself (unlike -2's
                // BodyElement, where the head element is directly abstract="true") — so they slip past
                // the ge.IsAbstract check above. Skip them the same way: not a document root, reached
                // only through a ref + substitutionGroup from a containing type.
                if (ge.InlineType is null && complex.TryGetValue(StripPrefix(ge.TypeRef), out var headType) &&
                    headType.IsAbstract)
                    continue;

                var typeName = PascalCase(ge.Name);

                SequencePlan body;
                if (ge.InlineType is not null)
                {
                    body = BuildSequence(typeName, ge.InlineType, schema, enums, opaqueTypes);
                    complex[typeName] = body;
                }
                else if (complex.TryGetValue(StripPrefix(ge.TypeRef), out var named))
                {
                    body = named with { RecordName = typeName };
                    complex[typeName] = body;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Global element '{ge.Name}' references unknown complex type '{ge.TypeRef}'.");
                }

                int docIndex = docOrder.FindIndex(x => x.Name == ge.Name && x.Namespace == ge.Namespace);
                globals.Add(new GlobalElementPlan(ge.Name, typeName, body, docIndex));
            }

            // Fragment grammar: every element declaration of the set (global + local, all namespaces),
            // sorted by name then namespace, gets an event code. Signable elements named by the caller
            // get a fragment codec (their content encoder already exists).
            var fragOrder = schema.AllElementDeclarations
                .OrderBy(x => x.Name, StringComparer.Ordinal)
                .ThenBy(x => x.Namespace, StringComparer.Ordinal)
                .ToList();
            int fragBits = BitsForChoices(fragOrder.Count + 1);
            // FragmentContent productions: one SE per element (0..n-1), a generic slot (n), then ED (n+1)
            // — cbexigen's non-strict fragment grammar; the End-Fragment event code is n+1.
            int fragEnd = fragOrder.Count + 1;

            var fragments = new List<FragmentPlan>();
            foreach (var name in fragmentElements)
            {
                var decls = fragOrder.Where(x => x.Name == name).ToList();
                if (decls.Count != 1)
                    throw new InvalidOperationException(decls.Count == 0
                        ? $"fragment element '{name}' is not an element declaration of the set."
                        : $"fragment element '{name}' is declared in {decls.Count} namespaces; disambiguation not supported.");
                var key = decls[0];
                int code = fragOrder.IndexOf(key);
                if (!schema.ElementTypeRefs.TryGetValue(key, out var typeRef))
                    throw new InvalidOperationException($"fragment element '{name}' has no named type (inline types are not supported).");
                var local = StripPrefix(typeRef);
                if (!complex.ContainsKey(local))
                    throw new InvalidOperationException($"fragment element '{name}': type '{typeRef}' is not modelled.");
                fragments.Add(new FragmentPlan(name, PascalCase(local), code));
            }

            return new SchemaPlan(schema.TargetNamespace, globals, complex, enums,
                opaqueTypes.Distinct().ToList(), docBits, fragBits, fragEnd, fragments,
                particleGrammar);
        }

        private static SequencePlan BuildSequence(
            string ctName, XsdComplexType ct, XsdSchema schema, List<EnumPlan> enums,
            List<string> opaqueTypes)
        {
            var baseRecord = ct.BaseTypeRef is null ? null : PascalCase(StripPrefix(ct.BaseTypeRef));

            // Attributes (AT events) precede the content, in lexicographic name order.
            IReadOnlyList<AttrPlan>? attrPlans = null;
            if (ct.Attributes is { Count: > 0 })
            {
                var list = new List<AttrPlan>();
                foreach (var a in ct.Attributes.OrderBy(a => a.Name, StringComparer.Ordinal))
                {
                    var (csType, val, _) = ResolveTypeRef(a.TypeRef, schema, enums, a.Name);
                    list.Add(new AttrPlan(PascalCase(a.Name), csType, val, a.Required));
                }
                attrPlans = list;
            }

            // xs:simpleContent — a single content value plus attributes.
            if (ct.SimpleContentBase is not null)
            {
                var (scType, scVal, _) = ResolveTypeRef(ct.SimpleContentBase, schema, enums, ctName);
                return new SequencePlan(PascalCase(ctName), System.Array.Empty<ChildPlan>(),
                    IsAbstract: ct.IsAbstract, BaseRecordName: baseRecord, Attributes: attrPlans,
                    SimpleContent: scVal, SimpleContentType: scType);
            }

            // xs:choice content — the alternatives become mutually-exclusive nullable fields.
            if (ct.Choice is not null)
            {
                var alts = new List<ChildPlan>();
                foreach (var el in ct.Choice)
                {
                    var (csType, val, isVal) = ResolveElementType(el, schema, enums);
                    alts.Add(new ChildPlan(
                        FieldName       : PascalCase(el.Name),
                        Type            : csType,
                        IsValueType     : isVal,
                        Shape           : ChildShape.OptionalSingle, // renders the field as nullable
                        Value           : val));
                }
                return new SequencePlan(PascalCase(ctName), alts,
                    IsAbstract: ct.IsAbstract, BaseRecordName: baseRecord, Attributes: attrPlans, IsChoice: true);
            }

            // Flatten inherited particles: for xs:complexContent/xs:extension the base type's
            // particles come first, then this type's own — this is the EXI content order.
            var particles = FlattenParticles(ct, schema);

            // Detect "single repeating element" pattern (e.g. AppProtocolType list inside Req).
            if (particles.Count == 1 && particles[0].MaxOccurs > 1 && particles[0].Ref is null)
            {
                if (attrPlans is not null)
                    throw new NotSupportedException(
                        $"complexType '{ctName}': attributes on a repeating-content type are not supported yet.");
                var only = particles[0];
                var (csType, val, isVal) = ResolveElementType(only, schema, enums);

                // For bounded repeating, the "child" represents the repeating element type;
                // the emitter handles the loop using ListMin/ListMax.
                var child = new ChildPlan(
                    FieldName       : PascalCase(only.Name),
                    Type            : csType,
                    IsValueType     : isVal,
                    Shape           : ChildShape.BoundedRepeating,
                    Value           : val);

                return new SequencePlan(
                    RecordName      : PascalCase(ctName),
                    Children        : new[] { child },
                    ListMin         : only.MinOccurs,
                    ListMax         : only.MaxOccurs,
                    IsAbstract      : ct.IsAbstract,
                    BaseRecordName  : baseRecord);
            }

            // Otherwise, treat each child individually.
            var children = new List<ChildPlan>();
            foreach (var el in particles)
            {
                // An inline xs:choice nested in the sequence (ISO 15118-20): each branch resolves to its
                // own independent (always-nullable) field, exactly like any other child element — cbexigen
                // flattens the branches into the enclosing run's shared state (see ValueEncoding.InlineChoice).
                if (el.InlineChoiceMembers is not null)
                {
                    var members = new List<InlineChoiceMember>();
                    foreach (var mbr in el.InlineChoiceMembers)
                    {
                        var (mbrCsType, mbrVal, mbrIsValueType) = ResolveElementType(mbr, schema, enums);
                        members.Add(new InlineChoiceMember(mbr.Name, PascalCase(mbr.Name), mbrCsType, mbrVal, mbrIsValueType));
                    }
                    children.Add(new ChildPlan(
                        FieldName       : el.Name,   // "$InlineChoice" — never dereferenced as msg.<FieldName>
                        Type            : TypeRef.None,
                        IsValueType     : false,
                        Shape           : el.MinOccurs == 0 ? ChildShape.OptionalSingle : ChildShape.RequiredSingle,
                        Value           : new ValueEncoding.InlineChoice(BitsForChoices(members.Count + 1), members)));
                    continue;
                }

                // A repeating reference into an opaque namespace (SignatureType's Object): always absent
                // in ISO 15118-2, so it is modelled as an opaque optional single. While absent this is
                // byte-identical — its first-occurrence SE is one production of the enclosing state; the
                // repeat only matters once present, which never happens. A present instance fails loud.
                if (el.MaxOccurs > 1 && el.Ref is not null && schema.OpaqueElementNames.Contains(el.Ref))
                {
                    var opaqueType = PascalCase(el.Ref);
                    opaqueTypes.Add(opaqueType);
                    children.Add(new ChildPlan(
                        FieldName       : PascalCase(el.Ref),
                        Type            : new TypeRef.Named(opaqueType),
                        IsValueType     : false,
                        Shape           : ChildShape.OptionalSingle,
                        Value           : new ValueEncoding.OpaqueElement(opaqueType)));
                    continue;
                }

                // A repeating element (maxOccurs > 1) among other children: supported at the end of the
                // sequence (cbexigen encodes it as a list after the preceding children) or — ISO
                // 15118-20's AuthorizationSetupResType.AuthorizationServices shape — followed by more
                // particles, whose SE/dispatch shares the list's own "continue vs move on" event codes
                // only for the IMMEDIATE next particle (see EmitEncodeRequiredRepeatingWithTail); any
                // particles after that are handled by the normal sequence walk, independently — verified
                // against cbV2G, where CertificateInstallationService folds into the list's own states
                // but the choice that follows it does not.
                if (el.MaxOccurs > 1)
                {
                    var (repType, repVal, repIsVal) = ResolveElementType(el, schema, enums);
                    children.Add(new ChildPlan(
                        FieldName : PascalCase(el.Name),
                        Type      : repType,
                        IsValueType: repIsVal,
                        Shape     : ChildShape.BoundedRepeating,
                        Value     : repVal,
                        ListMin   : el.MinOccurs,
                        ListMax   : el.MaxOccurs));
                    continue;
                }

                // <xs:element ref="Head"/> pointing at a substitution-group head → a polymorphic
                // choice among the head's members.
                if (el.Ref is not null)
                {
                    var subst = TryBuildSubstitution(schema, el.Ref);
                    if (subst is { } s)
                    {
                        // A substitution reference expands to one grammar production per member (and the
                        // abstract head); an optional reference (minOccurs=0) joins the surrounding
                        // optional run and gains an EE alternative, a required one terminates it. The
                        // emitter flattens the members into the run's grammar state (cbexigen model,
                        // verified against PowerDeliveryReqType and ChargeParameterDiscoveryResType).
                        children.Add(new ChildPlan(
                            FieldName       : PascalCase(el.Ref),
                            Type            : new TypeRef.Named(s.BaseType),
                            IsValueType     : false,
                            Shape           : el.MinOccurs == 0 ? ChildShape.OptionalSingle : ChildShape.RequiredSingle,
                            Value           : s.Choice));
                        continue;
                    }

                    // A reference into an opaque namespace (ds:Signature in the message header):
                    // model it as an opaque, encode-absent child. It is optional in the schema and
                    // always absent for the Phase 2 messages.
                    if (schema.OpaqueElementNames.Contains(el.Ref))
                    {
                        var opaqueType = PascalCase(el.Ref);
                        opaqueTypes.Add(opaqueType);
                        children.Add(new ChildPlan(
                            FieldName       : PascalCase(el.Ref),
                            Type            : new TypeRef.Named(opaqueType),
                            IsValueType     : false, // reference type; nullability comes from Shape
                            Shape           : el.MinOccurs == 0 ? ChildShape.OptionalSingle : ChildShape.RequiredSingle,
                            Value           : new ValueEncoding.OpaqueElement(opaqueType)));
                        continue;
                    }

                    // A plain reference to a modelled (whitelisted) global element — resolve to that
                    // element's type, exactly like a named child. Used by the XMLDSig SignedInfo subtree
                    // (SignedInfoType → CanonicalizationMethod/SignatureMethod, ReferenceType → …).
                    var refTarget = schema.GlobalElements.FirstOrDefault(g => g.Ref is null && g.Name == el.Ref);
                    if (refTarget is not null && !string.IsNullOrEmpty(refTarget.TypeRef))
                    {
                        var (refType, refVal, refIsValueType) = ResolveElementType(el, schema, enums);
                        var refShape = el.MinOccurs == 0 ? ChildShape.OptionalSingle : ChildShape.RequiredSingle;
                        children.Add(new ChildPlan(
                            FieldName       : PascalCase(el.Ref),
                            Type            : refType,
                            IsValueType     : refIsValueType,
                            Shape           : refShape,
                            Value           : refVal));
                        continue;
                    }

                    throw new NotSupportedException(
                        $"complexType '{ctName}': element ref '{el.Ref}' is not a substitution-group head " +
                        "and not an opaque-namespace element (plain element references are not supported yet).");
                }

                var (csType, val, isValueType) = ResolveElementType(el, schema, enums);
                var shape = el.MinOccurs == 0 ? ChildShape.OptionalSingle : ChildShape.RequiredSingle;

                children.Add(new ChildPlan(
                    FieldName       : PascalCase(el.Name),
                    Type            : csType,
                    IsValueType     : isValueType,
                    Shape           : shape,
                    Value           : val,
                    IsWildcardAny   : el.IsWildcard));
            }

            // A lone repeating child (e.g. the xmldsig TransformsType: a single <element ref="Transform"
            // maxOccurs="unbounded"/>) reaches here through the general per-particle walk, which records the
            // bound on the CHILD. But the emitter's single-repeating-sequence path reads the bound from the
            // PLAN (where the Ref-is-null fast path above puts it), so it must be promoted — otherwise the
            // plan-level ListMax stays 0 and the generated encoder's `count is < 1 or > 0` guard rejects
            // every non-empty list, and the decoder's `count >= 0` guard rejects every element.
            if (children.Count == 1 && children[0].Shape == ChildShape.BoundedRepeating)
                return new SequencePlan(PascalCase(ctName), children,
                    ListMin: children[0].ListMin, ListMax: children[0].ListMax,
                    IsAbstract: ct.IsAbstract, BaseRecordName: baseRecord, Attributes: attrPlans);

            return new SequencePlan(PascalCase(ctName), children,
                IsAbstract: ct.IsAbstract, BaseRecordName: baseRecord, Attributes: attrPlans);
        }

        /// <summary>
        /// If <paramref name="headName"/> names a substitution-group head (an abstract element and/or
        /// one that others substitute), build the sorted production list. cbexigen includes the head
        /// element itself as a production and sorts by element name. ISO 15118-20 chains substitution
        /// groups — a member can itself be the head of further members (e.g. DC's
        /// <c>CLReqControlMode &lt;- Scheduled_DC_CLReqControlMode &lt;- BPT_Scheduled_DC_CLReqControlMode</c>,
        /// three levels deep) — so membership is collected transitively, not just one level; verified
        /// against cbV2G's <c>iso20_dc_DC_ChargeLoopReqType</c> (5 flattened productions, alphabetical,
        /// 3-bit width). ISO 15118-20 also has substitution heads whose <c>abstract</c> flag lives on the
        /// TYPE, not the element declaration (<c>CLReqControlMode</c> itself) — a production's "abstract,
        /// no runtime case" status is therefore decided per production by its OWN type, not by whether it
        /// is literally the named head.
        /// </summary>
        private static (string BaseType, ValueEncoding.SubstitutionChoice Choice)? TryBuildSubstitution(
            XsdSchema schema, string headName)
        {
            var head = schema.GlobalElements.FirstOrDefault(g => g.Ref is null && g.Name == headName);
            if (head is null) return null;

            var productions = new List<XsdElement> { head };
            var frontier = new Queue<string>();
            frontier.Enqueue(headName);
            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                foreach (var member in schema.GlobalElements.Where(g => g.Ref is null && g.SubstitutionGroup == current))
                {
                    productions.Add(member);
                    frontier.Enqueue(member.Name);
                }
            }

            if (productions.Count <= 1 && !head.IsAbstract)
                return null; // a plain global element, not a substitution point

            productions.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

            var members = productions
                .Select(e => new SubstMember(
                    ElementName    : e.Name,
                    TypeName       : PascalCase(StripPrefix(e.TypeRef)),
                    IsAbstractHead : IsTypeAbstract(schema, e.TypeRef)))
                .ToList();

            // Standalone width: n member productions + the non-strict phantom -> ceil(log2(n+1)).
            // (When the reference sits inside an optional run the emitter recomputes the width from
            // the whole state's production count and this value is unused.)
            var choice = new ValueEncoding.SubstitutionChoice(BitsForChoices(members.Count + 1), members);
            return (PascalCase(StripPrefix(head.TypeRef)), choice);
        }

        private static bool IsTypeAbstract(XsdSchema schema, string typeRef) =>
            schema.ComplexTypes.TryGetValue(StripPrefix(typeRef), out var ct) && ct.IsAbstract;

        private static (TypeRef Type, ValueEncoding Value, bool IsValueType) ResolveTypeRef(
            string typeRef, XsdSchema schema, List<EnumPlan> enums, string ownerName)
        {
            // Built-in xs:* / xsd:* first.
            if (typeRef.StartsWith("xs:",  StringComparison.Ordinal) ||
                typeRef.StartsWith("xsd:", StringComparison.Ordinal))
                return ResolveBuiltin(NormaliseBuiltin(typeRef));

            // Cross-namespace references carry a prefix (e.g. "v2gci_t:FooType"); the collected
            // set resolves them by local name.
            typeRef = StripPrefix(typeRef);

            // Named simpleType: walk through restriction.
            if (schema.SimpleTypes.TryGetValue(typeRef, out var st))
                return ResolveSimpleType(st, enums);

            // Named complexType → field is the corresponding C# record.
            if (schema.ComplexTypes.ContainsKey(typeRef))
            {
                var typeName = PascalCase(typeRef);
                return (new TypeRef.Named(typeName), new ValueEncoding.ComplexRef(typeName), false);
            }

            throw new InvalidOperationException($"Cannot resolve type reference '{typeRef}' for element '{ownerName}'.");
        }

        /// <summary>Resolve a (named or inline) simpleType's restriction to a value encoding.</summary>
        private static (TypeRef Type, ValueEncoding Value, bool IsValueType) ResolveSimpleType(
            XsdSimpleType st, List<EnumPlan> enums)
        {
            // String enumeration → C# enum.
            if (st.Enumeration is { Count: > 0 } members)
            {
                var enumName = PascalCase(st.Name).TrimSuffix("Type").TrimSuffix("_inline");
                if (!enums.Any(e => e.Name == enumName))
                    enums.Add(new EnumPlan(enumName, members));
                int enumWidth = BitsForChoices(members.Count);
                return (new TypeRef.Named(enumName), new ValueEncoding.EnumIndex(enumName, enumWidth, members), true);
            }

            // Bounded integer range → n-bit unsigned with bias, but ONLY when the range has ≤ 4096
            // values (EXI §7.1.10). A wider bounded range (e.g. RelativeTimeInterval's start, 0..16777214)
            // falls back to the base built-in's integer encoding — cbexigen encodes it as an EXI Unsigned
            // Integer, not a 24-bit n-bit field.
            if (st.MinInclusive is long min && st.MaxInclusive is long max && max >= min && max - min + 1 <= 4096)
            {
                long range = max - min + 1;
                int width = BitsForChoices(checked((int)range));
                var (csType, _, isVal) = ResolveBuiltin(st.Base);
                return (csType, new ValueEncoding.NBitUnsigned(width, min), isVal);
            }

            // Otherwise: inherit the base built-in's encoding (string, unsigned/signed integer, …).
            return ResolveBuiltin(NormaliseBuiltin(st.Base));
        }

        /// <summary>Resolve an element's type — its inline simpleType if present, else its type ref.</summary>
        private static (TypeRef Type, ValueEncoding Value, bool IsValueType) ResolveElementType(
            XsdElement el, XsdSchema schema, List<EnumPlan> enums)
        {
            if (el.InlineSimpleType is not null)
                return ResolveSimpleType(el.InlineSimpleType, enums);

            // A plain element reference (e.g. a repeating ref="SalesTariffEntry") resolves to the
            // referenced global element's type.
            if (el.Ref is not null)
            {
                var target = schema.GlobalElements.FirstOrDefault(g => g.Ref is null && g.Name == el.Ref)
                    ?? throw new InvalidOperationException($"element ref '{el.Ref}' not found.");
                return ResolveTypeRef(target.TypeRef, schema, enums, target.Name);
            }

            return ResolveTypeRef(el.TypeRef, schema, enums, el.Name);
        }

        /// <summary>Shorthand for a built-in type reference, to keep the table below readable.</summary>
        private static TypeRef P(PrimitiveKind kind) => new TypeRef.Primitive(kind);

        private static (TypeRef Type, ValueEncoding Value, bool IsValueType) ResolveBuiltin(string xsType)
        {
            return xsType switch
            {
                "xs:string"        => (P(PrimitiveKind.String), new ValueEncoding.StringValue(), false),
                "xs:anyURI"        => (P(PrimitiveKind.String), new ValueEncoding.StringValue(), false),
                // String-ish built-ins used by attributes (xs:ID / NCName / token …).
                "xs:ID"            => (P(PrimitiveKind.String), new ValueEncoding.StringValue(), false),
                "xs:IDREF"         => (P(PrimitiveKind.String), new ValueEncoding.StringValue(), false),
                "xs:NCName"        => (P(PrimitiveKind.String), new ValueEncoding.StringValue(), false),
                "xs:Name"          => (P(PrimitiveKind.String), new ValueEncoding.StringValue(), false),
                "xs:token"         => (P(PrimitiveKind.String), new ValueEncoding.StringValue(), false),
                "xs:normalizedString" => (P(PrimitiveKind.String), new ValueEncoding.StringValue(), false),
                // cbexigen encodes unsignedByte as a fixed 8-bit n-bit unsigned (its value
                // space is [0..255]), not as a multi-byte EXI Unsigned Integer.
                "xs:unsignedByte"  => (P(PrimitiveKind.UInt8),   new ValueEncoding.NBitUnsigned(8, 0), true),
                "xs:unsignedShort" => (P(PrimitiveKind.UInt16), new ValueEncoding.UnsignedInt(),  true),
                "xs:unsignedInt"   => (P(PrimitiveKind.UInt32),   new ValueEncoding.UnsignedInt(),  true),
                "xs:unsignedLong"  => (P(PrimitiveKind.UInt64),  new ValueEncoding.UnsignedInt(),  true),
                // xs:byte is bounded [-128..127] → 8-bit n-bit unsigned with bias (cbexigen model).
                "xs:byte"          => (P(PrimitiveKind.Int8),  new ValueEncoding.NBitUnsigned(8, -128), true),
                // Wider signed built-ins → EXI Integer (sign bit + Unsigned Integer magnitude).
                "xs:short"         => (P(PrimitiveKind.Int16),  new ValueEncoding.SignedInt(), true),
                "xs:int"           => (P(PrimitiveKind.Int32),    new ValueEncoding.SignedInt(), true),
                "xs:long"          => (P(PrimitiveKind.Int64),   new ValueEncoding.SignedInt(), true),
                "xs:integer"       => (P(PrimitiveKind.Int64),   new ValueEncoding.SignedInt(), true),
                "xs:boolean"       => (P(PrimitiveKind.Bool),   new ValueEncoding.NBitUnsigned(1, 0), true),
                // hexBinary and base64Binary are identical on the wire (length + raw octets).
                "xs:hexBinary"     => (P(PrimitiveKind.Binary), new ValueEncoding.Binary(), false),
                "xs:base64Binary"  => (P(PrimitiveKind.Binary), new ValueEncoding.Binary(), false),
                _ => throw new NotSupportedException($"Unsupported XSD built-in '{xsType}'."),
            };
        }

        /// <summary>Map both <c>xs:</c> and <c>xsd:</c> prefixed names to the canonical <c>xs:</c> form.</summary>
        private static string NormaliseBuiltin(string typeRef) =>
            typeRef.StartsWith("xsd:", StringComparison.Ordinal)
                ? "xs:" + typeRef.Substring("xsd:".Length)
                : typeRef;

        /// <summary>⌈log₂(n)⌉, with the EXI convention that n=1 needs 0 bits.</summary>
        private static int BitsForChoices(int n)
        {
            if (n <= 1) return 0;
            int bits = 0;
            int v = n - 1;
            while (v > 0) { bits++; v >>= 1; }
            return bits;
        }

        private static string PascalCase(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

        /// <summary>Drop an XML namespace prefix (<c>"ns:Local"</c> → <c>"Local"</c>).</summary>
        private static string StripPrefix(string s)
        {
            int i = s.IndexOf(':');
            return i < 0 ? s : s.Substring(i + 1);
        }

        /// <summary>
        /// The full ordered particle list of a complex type: for an extension, the base type's
        /// (recursively flattened) particles followed by this type's own. Non-derived types just
        /// return their own sequence.
        /// </summary>
        private static IReadOnlyList<XsdElement> FlattenParticles(XsdComplexType ct, XsdSchema schema)
        {
            if (ct.BaseTypeRef is null)
                return ct.Sequence;

            var baseLocal = StripPrefix(ct.BaseTypeRef);
            if (!schema.ComplexTypes.TryGetValue(baseLocal, out var baseCt))
                throw new InvalidOperationException(
                    $"complexType '{ct.Name}': unknown xs:extension base '{ct.BaseTypeRef}'.");

            var result = new List<XsdElement>(FlattenParticles(baseCt, schema));
            result.AddRange(ct.Sequence);
            return result;
        }
    }
}
