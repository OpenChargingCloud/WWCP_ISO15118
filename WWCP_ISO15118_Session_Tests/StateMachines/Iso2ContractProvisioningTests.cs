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

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using cloud.charging.open.protocols.ISO15118.Session;
using cloud.charging.open.protocols.ISO15118.StateMachines;
using cloud.charging.open.protocols.ISO15118.StateMachines.Iso2;
using cloud.charging.open.protocols.ISO15118_2;
using cloud.charging.open.protocols.ISO15118_2.Generated;


using NUnit.Framework;

namespace cloud.charging.open.protocols.ISO15118.Session.Tests.StateMachines
{

    /// <summary>
    /// ISO 15118-<b>2</b> contract provisioning (§7.9.2.4): the exchange by which a car that has only its
    /// factory OEM certificate ends up holding a contract — and the renewal that keeps it holding one.
    /// </summary>
    /// <remarks>
    /// Driven through <c>Handle</c> rather than over a socket, which is this project's house style and
    /// enough for everything that is decided here. The full car-against-station run lives with the OCPP
    /// bridge, where the loopback plumbing already exists.
    /// </remarks>
    [TestFixture]
    public class Iso2ContractProvisioningTests
    {

        #region The key transport

        [Test]
        public void The_wrapped_key_comes_back_out_the_way_it_went_in()
        {

            using var receiver = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            using var contract = ECDsa.Create(ECCurve.NamedCurves.nistP256);

            var (dhPublicKey, wrapped) = ContractProvisioning.EncryptContractKey(receiver.PublicKey, contract);
            using var recovered = ContractProvisioning.RecoverContractKey(receiver, dhPublicKey, wrapped);

            Assert.Multiple(() => {

                // The schema's field widths, and the reason they are asserted rather than assumed: an
                // off-by-one here is invisible in a round trip against ourselves and fatal against anyone else.
                Assert.That(dhPublicKey, Has.Length.EqualTo(65), "uncompressed P-256 point");
                Assert.That(dhPublicKey[0], Is.EqualTo(0x04));
                Assert.That(wrapped, Has.Length.EqualTo(48), "IV(16) + ciphertext(32), no padding");

                Assert.That(ContractProvisioning.Matches(recovered, contract), Is.True);

            });

        }

        [Test]
        public void A_key_unwrapped_with_the_wrong_certificate_is_detected_rather_than_used()
        {

            // The whole reason Matches exists. -2 wraps with AES-CBC, which authenticates nothing, so
            // decryption with the wrong key does not fail — it yields 32 bytes of nonsense that make a
            // perfectly valid private key belonging to nobody. -20's GCM would have thrown here.
            using var receiver = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            using var stranger = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            using var contract = ECDsa.Create(ECCurve.NamedCurves.nistP256);

            var (dhPublicKey, wrapped) = ContractProvisioning.EncryptContractKey(receiver.PublicKey, contract);

            using var wrong = ContractProvisioning.RecoverContractKey(stranger, dhPublicKey, wrapped);

            Assert.That(ContractProvisioning.Matches(wrong, contract), Is.False,
                        "an unwrap with the wrong key must not pass for the real one");

        }

        [Test]
        public void A_field_of_the_wrong_width_is_refused_before_anything_is_decrypted()
        {

            using var receiver = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);

            Assert.Multiple(() => {

                Assert.That(() => ContractProvisioning.RecoverContractKey(receiver, new Byte[64], new Byte[48]),
                            Throws.TypeOf<CryptographicException>());

                Assert.That(() => ContractProvisioning.RecoverContractKey(receiver, UncompressedPoint(receiver), new Byte[47]),
                            Throws.TypeOf<CryptographicException>());

            });

            static Byte[] UncompressedPoint(ECDiffieHellman key)
            {
                var q = key.ExportParameters(false).Q;
                var point = new Byte[65];
                point[0] = 0x04;
                q.X!.CopyTo(point, 1);
                q.Y!.CopyTo(point, 33);
                return point;
            }

        }

        #endregion

        #region The exchange

        private static V2G_Message Request(Secc2 secc, BodyBaseType body, SignatureType? signature = null)
            => new (new MessageHeaderType(secc.SessionId, Notification: null, Signature: signature),
                    new BodyType(body));

        /// <summary>An OEM provisioning credential: a P-256 leaf whose CN stands in for the PCID, with the
        /// key usages .NET needs to hand out both an ECDsa and an ECDH view of it.</summary>
        private static (Byte[] Der, ECDsa SignKey, ECDiffieHellman Agreement) OemCredential()
        {

            var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var request = new CertificateRequest("CN=WMIVIN0000000042", key, HashAlgorithmName.SHA256);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyAgreement, true));

            using var leaf = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(4));

            var parameters = key.ExportParameters(includePrivateParameters: true);
            return (leaf.RawData, ECDsa.Create(parameters), ECDiffieHellman.Create(parameters));

        }

        /// <summary>A station run up to the point where a provisioning message is due: the service offered,
        /// asked about, and selected.</summary>
        private static Secc2 SeccAtProvisioning(out ServiceDiscoveryResType discovery)
        {

            var secc = new Secc2(PowerMode.Ac, TimeSpan.FromSeconds(30), TimeProvider.System)
            {
                OfferCertificateService = true,
            };

            secc.Handle(Request(secc, new SessionSetupReqType(EVCCID: new Byte[] { 0xAB, 0xCD, 0xEF, 0x01, 0x02, 0x03 })));

            discovery = (ServiceDiscoveryResType) secc.Handle(
                            Request(secc, new ServiceDiscoveryReqType(ServiceScope: null, ServiceCategory: null)))
                        .Body.BodyElement!;

            secc.Handle(Request(secc, new ServiceDetailReqType(Secc2.CertificateServiceId)));

            secc.Handle(Request(secc, new PaymentServiceSelectionReqType(
                            PaymentOption.Contract,
                            new SelectedServiceListType(new[]
                            {
                                new SelectedServiceType(ServiceID: 1, ParameterSetID: null),
                                new SelectedServiceType(Secc2.CertificateServiceId, Secc2.CertificateInstallationParameterSetId),
                            }))));

            return secc;

        }

        private static V2G_Message SignedInstallation(Secc2 secc, Byte[] oemDer, ECDsa signKey)
        {

            var request  = new CertificateInstallationReqType("id1", oemDer,
                               new ListOfRootCertificateIDsType(new[] { new X509IssuerSerialType("CN=V2GRootCA (dev)", 1) }));

            var fragment = new Byte[4096];
            if (!Iso2Codec.EncodeFragment_CertificateInstallationReq(request, fragment, out Int32 n))
                throw new InvalidOperationException("fragment encode failed.");

            return Request(secc, request, XmlDsigInterop2.Sign("id1", fragment.AsSpan(0, n), signKey));

        }

        [Test]
        public void A_station_offering_nothing_says_so_and_a_station_offering_it_lists_it()
        {

            var quiet = new Secc2(PowerMode.Ac, TimeSpan.FromSeconds(30), TimeProvider.System);
            quiet.Handle(Request(quiet, new SessionSetupReqType(EVCCID: new Byte[] { 1, 2, 3, 4, 5, 6 })));
            var quietDiscovery = (ServiceDiscoveryResType) quiet.Handle(
                                     Request(quiet, new ServiceDiscoveryReqType(null, null))).Body.BodyElement!;

            var offering = SeccAtProvisioning(out var offeringDiscovery);

            Assert.Multiple(() => {

                // Off by default, and the absence is the point: every -2 session this station answered
                // before carries no ServiceList, and none of them has to be re-recorded.
                Assert.That(quietDiscovery.ServiceList, Is.Null);

                Assert.That(offeringDiscovery.ServiceList!.Service, Has.Count.EqualTo(1));
                Assert.That(offeringDiscovery.ServiceList.Service[0].ServiceCategory,
                            Is.EqualTo(ServiceCategory.ContractCertificate));
                Assert.That(offering.CertificateServiceSelected, Is.True);

            });

        }

        [Test]
        public void The_service_details_name_both_parameter_sets()
        {

            var secc = new Secc2(PowerMode.Ac, TimeSpan.FromSeconds(30), TimeProvider.System) { OfferCertificateService = true };
            secc.Handle(Request(secc, new SessionSetupReqType(EVCCID: new Byte[] { 1, 2, 3, 4, 5, 6 })));
            secc.Handle(Request(secc, new ServiceDiscoveryReqType(null, null)));

            var detail = (ServiceDetailResType) secc.Handle(
                             Request(secc, new ServiceDetailReqType(Secc2.CertificateServiceId))).Body.BodyElement!;

            Assert.Multiple(() => {

                Assert.That(detail.ResponseCode, Is.EqualTo(ResponseCode.OK));
                Assert.That(detail.ServiceParameterList!.ParameterSet.Select(p => p.ParameterSetID),
                            Is.EquivalentTo(new[] { Secc2.CertificateInstallationParameterSetId,
                                                    Secc2.CertificateUpdateParameterSetId }));

                // An id the station never listed is not a service it has details for.
                var unknown = (ServiceDetailResType) secc.Handle(
                                  Request(secc, new ServiceDetailReqType(99))).Body.BodyElement!;
                Assert.That(unknown.ResponseCode, Is.EqualTo(ResponseCode.FAILED_ServiceIDInvalid));

            });

        }

        [Test]
        public void An_installation_is_answered_with_a_contract_wrapped_for_the_car()
        {

            var (oemDer, signKey, agreement) = OemCredential();
            using (signKey)
            using (agreement)
            {

                var secc = SeccAtProvisioning(out _);
                var response = secc.Handle(SignedInstallation(secc, oemDer, signKey));
                var install = (CertificateInstallationResType) response.Body.BodyElement!;

                using var recovered = ContractProvisioning.RecoverContractKey(
                                          agreement, install.DHpublickey.Value,
                                          install.ContractSignatureEncryptedPrivateKey.Value);

                using var issued = X509CertificateLoader.LoadCertificate(install.ContractSignatureCertChain.Certificate);
                using var issuedPublicKey = issued.GetECDsaPublicKey()!;

                Assert.Multiple(() => {

                    Assert.That(install.ResponseCode, Is.EqualTo(ResponseCode.OK));
                    Assert.That(secc.CertInstall!.SignatureOk, Is.True, "the car's own signature over its request");
                    Assert.That(secc.CertInstall.DigestOk, Is.True);
                    Assert.That(secc.CertInstall.EncryptedForReceiver, Is.True);
                    Assert.That(secc.CertInstall.IsUpdate, Is.False);
                    Assert.That(secc.CertInstall.ReceiverSubject, Does.Contain("WMIVIN0000000042"));

                    // The four references §7.9.2.4.2 asks for, where every other signed -2 message has one.
                    Assert.That(response.Header.Signature!.SignedInfo.Reference, Has.Count.EqualTo(4));
                    Assert.That(response.Header.Signature.SignedInfo.Reference.Select(r => r.URI),
                                Is.EquivalentTo(new[] { "#id1", "#id2", "#id3", "#id4" }));

                    // And the car can actually use what it got.
                    Assert.That(ContractProvisioning.Matches(recovered, issuedPublicKey), Is.True);
                    Assert.That(install.EMAID.Value, Is.Not.Empty);

                });

            }

        }

        [Test]
        public void A_forged_request_is_answered_but_recorded_as_unverified()
        {

            // -2 cannot refuse in a CertificateInstallationRes — the schema makes the chain, the key, the
            // DH point and the eMAID all mandatory — so the station answers and records what it saw. What
            // decides whether that answer is a real contract is the layer above; here the record is the
            // deliverable, and it has to be truthful.
            var (oemDer, _, agreement) = OemCredential();
            using var wrongKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using (agreement)
            {

                var secc = SeccAtProvisioning(out _);
                secc.Handle(SignedInstallation(secc, oemDer, wrongKey));

                Assert.That(secc.CertInstall!.SignatureOk, Is.False);

            }

        }

        [Test]
        public void An_update_is_wrapped_for_the_contract_it_replaces()
        {

            // The security argument for an update in one assertion: the answer is encrypted for the
            // expiring contract's key, so only the car that already held that contract can open it.
            var (oldDer, signKey, agreement) = OemCredential();
            using (signKey)
            using (agreement)
            {

                var secc = new Secc2(PowerMode.Ac, TimeSpan.FromSeconds(30), TimeProvider.System) { OfferCertificateService = true };
                secc.Handle(Request(secc, new SessionSetupReqType(EVCCID: new Byte[] { 1, 2, 3, 4, 5, 6 })));
                secc.Handle(Request(secc, new ServiceDiscoveryReqType(null, null)));
                secc.Handle(Request(secc, new PaymentServiceSelectionReqType(
                                PaymentOption.Contract,
                                new SelectedServiceListType(new[]
                                {
                                    new SelectedServiceType(1, null),
                                    new SelectedServiceType(Secc2.CertificateServiceId, Secc2.CertificateUpdateParameterSetId),
                                }))));

                var request = new CertificateUpdateReqType("id1",
                                  new CertificateChainType(Id: null, oldDer, SubCertificates: null),
                                  "DE-VAN-C00000009-7",
                                  new ListOfRootCertificateIDsType(new[] { new X509IssuerSerialType("CN=V2GRootCA (dev)", 1) }));

                var fragment = new Byte[4096];
                Assert.That(Iso2Codec.EncodeFragment_CertificateUpdateReq(request, fragment, out Int32 n), Is.True);

                var response = secc.Handle(Request(secc, request, XmlDsigInterop2.Sign("id1", fragment.AsSpan(0, n), signKey)));
                var update = (CertificateUpdateResType) response.Body.BodyElement!;

                using var recovered = ContractProvisioning.RecoverContractKey(
                                          agreement, update.DHpublickey.Value,
                                          update.ContractSignatureEncryptedPrivateKey.Value);

                using var issued = X509CertificateLoader.LoadCertificate(update.ContractSignatureCertChain.Certificate);
                using var issuedPublicKey = issued.GetECDsaPublicKey()!;

                Assert.Multiple(() => {

                    Assert.That(update.ResponseCode, Is.EqualTo(ResponseCode.OK));
                    Assert.That(secc.CertInstall!.IsUpdate, Is.True);
                    Assert.That(secc.CertInstall.SignatureOk, Is.True);

                    // The eMAID is the car's, carried over: a renewal renews a contract, it does not issue
                    // a different one.
                    Assert.That(update.EMAID.Value, Is.EqualTo("DE-VAN-C00000009-7"));
                    Assert.That(ContractProvisioning.Matches(recovered, issuedPublicKey), Is.True);

                });

            }

        }

        [Test]
        public void A_station_that_offers_no_certificate_service_ends_the_session_over_one()
        {

            var (oemDer, signKey, agreement) = OemCredential();
            using (signKey)
            using (agreement)
            {

                var secc = new Secc2(PowerMode.Ac, TimeSpan.FromSeconds(30), TimeProvider.System);   // not offering
                secc.Handle(Request(secc, new SessionSetupReqType(EVCCID: new Byte[] { 1, 2, 3, 4, 5, 6 })));
                secc.Handle(Request(secc, new ServiceDiscoveryReqType(null, null)));
                secc.Handle(Request(secc, new PaymentServiceSelectionReqType(
                                PaymentOption.Contract,
                                new SelectedServiceListType(new[] { new SelectedServiceType(1, null) }))));

                // There is no CertificateInstallationRes that says no, so the only truthful answer is none.
                Assert.That(() => secc.Handle(SignedInstallation(secc, oemDer, signKey)),
                            Throws.TypeOf<SessionAborted>());

            }

        }

        [Test]
        public void A_car_that_selected_the_service_may_still_skip_it()
        {

            // [V2G2-680] leaves the exchange optional even once the service is selected — a car that
            // changes its mind is not out of sequence, and being refused for it would cost it the session.
            var secc = SeccAtProvisioning(out _);

            var details = secc.Handle(Request(secc, new PaymentDetailsReqType(
                              "DE-VAN-C00000001-6",
                              new CertificateChainType(Id: null, SelfSigned(), SubCertificates: null))));

            Assert.That(((PaymentDetailsResType) details.Body.BodyElement!).ResponseCode, Is.EqualTo(ResponseCode.OK));
            Assert.That(secc.SequenceErrorAt, Is.Null);

            static Byte[] SelfSigned()
            {
                using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                using var cert = new CertificateRequest("CN=DE-VAN-C00000001-6", key, HashAlgorithmName.SHA256)
                                     .CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
                return cert.RawData;
            }

        }

        #endregion

    }

}
