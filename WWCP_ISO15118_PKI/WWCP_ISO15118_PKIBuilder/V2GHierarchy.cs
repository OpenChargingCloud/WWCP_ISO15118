/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

using Org.BouncyCastle.Security;

#endregion

namespace cloud.charging.open.protocols.ISO15118.PKI
{

    /// <summary>
    /// Full V2G PKI hierarchy:
    ///
    ///   V2G Root CA
    ///   ├── CPO     Sub-CA 1 ─── CPO     Sub-CA 2 ─── SECC Leaf         (TLS server)
    ///   ├──  MO     Sub-CA 1 ───  MO     Sub-CA 2 ─── Contract Cert Leaf (PnC, app layer)
    ///   ├── OEM     Sub-CA 1 ─── OEM     Sub-CA 2 ─── OEM Prov Cert Leaf (provisioning)
    ///   ├── Vehicle Sub-CA 1 ─── Vehicle Sub-CA 2 ─── Vehicle Leaf      (TLS client, -20 mTLS)
    ///   └── CPS     Sub-CA   ────────────────────────  CPS Signing Leaf
    ///
    /// Two intermediates on CPO/MO/OEM/Vehicle mirror the real-world deployment topology
    /// (operator vs sub-operator). CPS uses a single intermediate. The Vehicle branch is the
    /// CharIN 2nd-gen V2G PKI's dedicated TLS-client identity for ISO 15118-20 mutual TLS,
    /// kept separate from the Contract cert (application-layer PnC only).
    ///
    /// With <see cref="V2GRootLayout.SeparateRoots"/> the MO branch hangs below an MO Root CA
    /// and the OEM and Vehicle branches below an OEM Root CA, each self-signed, and only the
    /// CPO and CPS branches stay below the V2G Root CA:
    ///
    ///   V2G Root CA ── CPO Sub-CA 1 ── CPO Sub-CA 2 ── SECC Leaf
    ///               └─ CPS Sub-CA ─────────────────── CPS Signing Leaf
    ///    MO Root CA ──  MO Sub-CA 1 ──  MO Sub-CA 2 ── Contract Cert Leaf
    ///   OEM Root CA ── OEM Sub-CA 1 ── OEM Sub-CA 2 ── OEM Prov Cert Leaf
    ///               └─ Vehicle Sub-CA 1 ── Vehicle Sub-CA 2 ── Vehicle Leaf
    ///
    /// Those are the three anchors ISO 15118-20 Annex C names, and what a vehicle that keeps
    /// its trust anchors apart by kind - a v2gRoot, an moRoot and an oemRoot - is filled from.
    /// <see cref="MoRoot"/> and <see cref="OemRoot"/> are those roots, or <see cref="Root"/>
    /// itself where the layout has only the one, so a consumer can always ask "what anchors
    /// the contract chain?" and get an answer.
    /// </summary>
    public class V2GHierarchy
    {

        #region Properties

        public required V2GAlgorithm       Algorithm         { get; init; }
        public required V2GProfileOptions  Options           { get; init; }
        public required V2GIssued          Root              { get; init; }

        /// <summary>The anchor of the MO branch: a root of its own, or <see cref="Root"/> where there is only the one.</summary>
        public required V2GIssued          MoRoot            { get; init; }

        /// <summary>The anchor of the OEM and Vehicle branches: a root of its own, or <see cref="Root"/> where there is only the one.</summary>
        public required V2GIssued          OemRoot           { get; init; }
        public required V2GIssued          CpoSubCa1         { get; init; }
        public required V2GIssued          CpoSubCa2         { get; init; }
        public required V2GIssued          SeccLeaf          { get; init; }

        public required V2GIssued          MoSubCa1          { get; init; }
        public required V2GIssued          MoSubCa2          { get; init; }
        public required V2GIssued          ContractLeaf      { get; init; }

        public required V2GIssued          OemSubCa1         { get; init; }
        public required V2GIssued          OemSubCa2         { get; init; }
        public required V2GIssued          OemProvLeaf       { get; init; }

        public required V2GIssued          VehicleSubCa1     { get; init; }
        public required V2GIssued          VehicleSubCa2     { get; init; }
        public required V2GIssued          VehicleLeaf       { get; init; }

        public required V2GIssued          CpsSubCa          { get; init; }
        public required V2GIssued          CpsSigningLeaf    { get; init; }

        /// <summary>Whether <see cref="MoRoot"/> and <see cref="OemRoot"/> are roots of their own rather than <see cref="Root"/>.</summary>
        public Boolean HasSeparateRoots
            => Options.HasSeparateRoots;

        /// <summary>Every trust anchor of this hierarchy: one, or three.</summary>
        public IEnumerable<V2GIssued> Roots()
        {

            yield return Root;

            if (HasSeparateRoots)
            {
                yield return MoRoot;
                yield return OemRoot;
            }

        }

        public IEnumerable<V2GIssued> AllCerts()
        {

            foreach (var root in Roots())
                yield return root;
            yield return CpoSubCa1;
            yield return CpoSubCa2;
            yield return SeccLeaf;
            yield return MoSubCa1;
            yield return MoSubCa2;
            yield return ContractLeaf;
            yield return OemSubCa1;
            yield return OemSubCa2;
            yield return OemProvLeaf;
            yield return VehicleSubCa1;
            yield return VehicleSubCa2;
            yield return VehicleLeaf;
            yield return CpsSubCa;
            yield return CpsSigningLeaf;

        }

        #endregion


        #region Build(V2GAlgorithm, SecureRandom, CommonNameSuffix = null, V2GProfileOptions = null, RevocationBaseURL = null)

        /// <summary>
        /// Builds a full V2G PKI hierarchy with the given signature algorithm, random source,
        /// and optional profile options and revocation URL.
        /// The CommonNameSuffix is appended to the CN of each certificate to help distinguish
        /// different hierarchies in tests and outputs.
        /// </summary>
        /// <param name="V2GAlgorithm"></param>
        /// <param name="SecureRandom"></param>
        /// <param name="CommonNameSuffix"></param>
        /// <param name="V2GProfileOptions"></param>
        /// <param name="RevocationBaseURL"></param>
        /// <returns></returns>
        public static V2GHierarchy Build(V2GAlgorithm        V2GAlgorithm,
                                         SecureRandom        SecureRandom,
                                         String?             CommonNameSuffix    = null,
                                         V2GProfileOptions?  V2GProfileOptions   = null,
                                         String?             RevocationBaseURL   = null)
        {

            V2GProfileOptions ??= V2GProfileOptions.LabFor(V2GAlgorithm);

            var root            = V2GCertificateBuilder.Issue(
                                      V2GCertProfile.ForRole(
                                          V2GRole.V2GRootCA,
                                          V2GProfileOptions,
                                          CommonNameSuffix
                                      ),
                                      V2GAlgorithm,
                                      SecureRandom,
                                      issuer:      null,
                                      revocation:  V2GRevocationInfo.ForRoot(RevocationBaseURL)
                                  );

            // The MO and OEM branches' anchors: roots of their own where the
            // layout asks for them, and the V2G root itself where it does not,
            // so that everything below is issued the same way either way.
            var moRoot          = V2GProfileOptions.HasSeparateRoots
                                      ? V2GCertificateBuilder.Issue(
                                            V2GCertProfile.ForRole(
                                                V2GRole.MORootCA,
                                                V2GProfileOptions,
                                                CommonNameSuffix
                                            ),
                                            V2GAlgorithm,
                                            SecureRandom,
                                            issuer:      null,
                                            revocation:  V2GRevocationInfo.ForRoot(RevocationBaseURL)
                                        )
                                      : root;

            var oemRoot         = V2GProfileOptions.HasSeparateRoots
                                      ? V2GCertificateBuilder.Issue(
                                            V2GCertProfile.ForRole(
                                                V2GRole.OEMRootCA,
                                                V2GProfileOptions,
                                                CommonNameSuffix
                                            ),
                                            V2GAlgorithm,
                                            SecureRandom,
                                            issuer:      null,
                                            revocation:  V2GRevocationInfo.ForRoot(RevocationBaseURL)
                                        )
                                      : root;

            var cpo1            = V2GCertificateBuilder.Issue(
                                      V2GCertProfile.ForRole(
                                          V2GRole.CPOSubCA1,
                                          V2GProfileOptions,
                                          CommonNameSuffix
                                      ),
                                      V2GAlgorithm,
                                      SecureRandom,
                                      root,
                                      revocation:  V2GRevocationInfo.ForIssuer(
                                                       RevocationBaseURL,
                                                       root
                                                   )
                                  );

            var cpo2            = V2GCertificateBuilder.Issue(
                                      V2GCertProfile.ForRole(
                                          V2GRole.CPOSubCA2,
                                          V2GProfileOptions,
                                          CommonNameSuffix
                                      ),
                                      V2GAlgorithm,
                                      SecureRandom,
                                      cpo1,
                                      revocation:  V2GRevocationInfo.ForIssuer(
                                                       RevocationBaseURL,
                                                       cpo1
                                                   )
                                  );

            var secc            = V2GCertificateBuilder.Issue(
                                      V2GCertProfile.ForRole(
                                          V2GRole.SECCLeaf,
                                          V2GProfileOptions,
                                          CommonNameSuffix
                                      ),
                                      V2GAlgorithm,
                                      SecureRandom,
                                      cpo2,
                                      revocation:  V2GRevocationInfo.ForIssuer(
                                                       RevocationBaseURL,
                                                       cpo2
                                                   )
                                  );

            var mo1             = V2GCertificateBuilder.Issue(
                                      V2GCertProfile.ForRole(
                                          V2GRole.MOSubCA1,
                                          V2GProfileOptions,
                                          CommonNameSuffix
                                      ),
                                      V2GAlgorithm,
                                      SecureRandom,
                                      moRoot,
                                      revocation:  V2GRevocationInfo.ForIssuer(
                                                       RevocationBaseURL,
                                                       moRoot
                                                   )
                                  );

            var mo2             = V2GCertificateBuilder.Issue(
                                      V2GCertProfile.ForRole(
                                          V2GRole.MOSubCA2,
                                          V2GProfileOptions,
                                          CommonNameSuffix
                                      ),
                                      V2GAlgorithm,
                                      SecureRandom,
                                      mo1,
                                      revocation:  V2GRevocationInfo.ForIssuer(
                                                       RevocationBaseURL,
                                                       mo1
                                                   )
                                  );

            var contract        = V2GCertificateBuilder.Issue(
                                      V2GCertProfile.ForRole(
                                          V2GRole.ContractCertLeaf,
                                          V2GProfileOptions,
                                          CommonNameSuffix
                                      ),
                                      V2GAlgorithm,
                                      SecureRandom,
                                      mo2,
                                      revocation:  V2GRevocationInfo.ForIssuer(
                                                       RevocationBaseURL,
                                                       mo2
                                                   )
                                  );



            var oem1            = V2GCertificateBuilder.Issue(
                                      V2GCertProfile.ForRole(
                                          V2GRole.OEMSubCA1,
                                          V2GProfileOptions,
                                          CommonNameSuffix
                                      ),
                                      V2GAlgorithm,
                                      SecureRandom,
                                      oemRoot,
                                      revocation:  V2GRevocationInfo.ForIssuer(
                                                       RevocationBaseURL,
                                                       oemRoot
                                                   )
                                  );

            var oem2            = V2GCertificateBuilder.Issue(
                                      V2GCertProfile.ForRole(
                                          V2GRole.OEMSubCA2,
                                          V2GProfileOptions,
                                          CommonNameSuffix
                                      ),
                                      V2GAlgorithm,
                                      SecureRandom,
                                      oem1,
                                      revocation:  V2GRevocationInfo.ForIssuer(
                                                       RevocationBaseURL,
                                                       oem1
                                                   )
                                  );

            var oemProv         = V2GCertificateBuilder.Issue(
                                      V2GCertProfile.ForRole(
                                          V2GRole.OEMProvCertLeaf,
                                          V2GProfileOptions,
                                          CommonNameSuffix
                                      ),
                                      V2GAlgorithm,
                                      SecureRandom,
                                      oem2,
                                      revocation:  V2GRevocationInfo.ForIssuer(
                                                       RevocationBaseURL,
                                                       oem2
                                                   )
                                 );



            var veh1            = V2GCertificateBuilder.Issue(
                                      V2GCertProfile.ForRole(
                                          V2GRole.VehicleSubCA1,
                                          V2GProfileOptions,
                                          CommonNameSuffix
                                      ),
                                      V2GAlgorithm,
                                      SecureRandom,
                                      oemRoot,
                                      revocation:  V2GRevocationInfo.ForIssuer(
                                                       RevocationBaseURL,
                                                       oemRoot
                                                   )
                                  );

            var veh2            = V2GCertificateBuilder.Issue(
                                      V2GCertProfile.ForRole(
                                          V2GRole.VehicleSubCA2,
                                          V2GProfileOptions,
                                          CommonNameSuffix
                                      ),
                                      V2GAlgorithm,
                                      SecureRandom,
                                      veh1,
                                      revocation:  V2GRevocationInfo.ForIssuer(
                                                       RevocationBaseURL,
                                                       veh1
                                                   )
                                  );

            var vehicle         = V2GCertificateBuilder.Issue(
                                      V2GCertProfile.ForRole(
                                          V2GRole.VehicleLeaf,
                                          V2GProfileOptions,
                                          CommonNameSuffix
                                      ),
                                      V2GAlgorithm,
                                      SecureRandom,
                                      veh2,
                                      revocation:  V2GRevocationInfo.ForIssuer(
                                                       RevocationBaseURL,
                                                       veh2
                                                   )
                                  );



            var cps             = V2GCertificateBuilder.Issue(
                                      V2GCertProfile.ForRole(
                                          V2GRole.CPSSubCA,
                                          V2GProfileOptions,
                                          CommonNameSuffix
                                      ),
                                      V2GAlgorithm,
                                      SecureRandom,
                                      root,
                                      revocation:  V2GRevocationInfo.ForIssuer(
                                                       RevocationBaseURL,
                                                       root
                                                   )
                                  );

            var cpsSigning      = V2GCertificateBuilder.Issue(
                                      V2GCertProfile.ForRole(
                                          V2GRole.CPSSigningLeaf,
                                          V2GProfileOptions,
                                          CommonNameSuffix
                                      ),
                                      V2GAlgorithm,
                                      SecureRandom,
                                      cps,
                                      revocation:  V2GRevocationInfo.ForIssuer(
                                                       RevocationBaseURL,
                                                       cps
                                                   )
                                  );


            return new V2GHierarchy {
                       Algorithm       = V2GAlgorithm,
                       Options         = V2GProfileOptions,
                       Root            = root,
                       MoRoot          = moRoot,
                       OemRoot         = oemRoot,
                       CpoSubCa1       = cpo1,
                       CpoSubCa2       = cpo2,
                       SeccLeaf        = secc,
                       MoSubCa1        = mo1,
                       MoSubCa2        = mo2,
                       ContractLeaf    = contract,
                       OemSubCa1       = oem1,
                       OemSubCa2       = oem2,
                       OemProvLeaf     = oemProv,
                       VehicleSubCa1   = veh1,
                       VehicleSubCa2   = veh2,
                       VehicleLeaf     = vehicle,
                       CpsSubCa        = cps,
                       CpsSigningLeaf  = cpsSigning
                   };

        }

        #endregion


    }

}
