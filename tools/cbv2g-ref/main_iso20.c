/* SPDX-License-Identifier: Apache-2.0 */
/*
 * cbv2g-iso20 — reference EXI encoder for a fixed set of ISO 15118-20 test messages across all
 * three Phase-4 message sets (CommonMessages, DC, AC), built on EVerest's libcbv2g.
 *
 * Development tool only; `dotnet test` never invokes it. Regenerated hex is checked into
 * Vanaheimr.V2G.Exi.Tests/Vectors/Iso15118_20.*.vectors.json.
 *
 * Usage:  cbv2g_iso20 <Set>_<VectorName>   ->  space-separated lowercase hex on stdout.
 *         <Set> is one of: Common, DC, AC.
 */

#include <stdio.h>
#include <string.h>
#include <stdint.h>

#include "cbv2g/common/exi_bitstream.h"
#include "cbv2g/iso_20/iso20_CommonMessages_Datatypes.h"
#include "cbv2g/iso_20/iso20_CommonMessages_Encoder.h"
#include "cbv2g/iso_20/iso20_DC_Datatypes.h"
#include "cbv2g/iso_20/iso20_DC_Encoder.h"
#include "cbv2g/iso_20/iso20_AC_Datatypes.h"
#include "cbv2g/iso_20/iso20_AC_Encoder.h"
#include "cbv2g/iso_20/iso20_WPT_Datatypes.h"
#include "cbv2g/iso_20/iso20_WPT_Encoder.h"
#include "cbv2g/iso_20/iso20_ACDP_Datatypes.h"
#include "cbv2g/iso_20/iso20_ACDP_Encoder.h"

#define OUT_BUF_SIZE 4096

static void set_str(char* dst, uint16_t* len, const char* s) {
    size_t n = strlen(s);
    memcpy(dst, s, n);
    dst[n] = '\0';
    *len = (uint16_t)n;
}

static void print_hex(const uint8_t* data, size_t len) {
    for (size_t i = 0; i < len; i++)
        printf(i == 0 ? "%02x" : " %02x", data[i]);
    printf("\n");
}

/* Every vector uses an all-zero 8-byte SessionID, a fixed TimeStamp and no header signature. */
static void set_header(struct iso20_MessageHeaderType* h) {
    memset(h->SessionID.bytes, 0, iso20_sessionIDType_BYTES_SIZE);
    h->SessionID.bytesLen = iso20_sessionIDType_BYTES_SIZE;
    h->TimeStamp = 1700000000ULL;
    h->Signature_isUsed = 0u;
}
static void set_header_dc(struct iso20_dc_MessageHeaderType* h) {
    memset(h->SessionID.bytes, 0, iso20_dc_sessionIDType_BYTES_SIZE);
    h->SessionID.bytesLen = iso20_dc_sessionIDType_BYTES_SIZE;
    h->TimeStamp = 1700000000ULL;
    h->Signature_isUsed = 0u;
}
static void set_header_ac(struct iso20_ac_MessageHeaderType* h) {
    memset(h->SessionID.bytes, 0, iso20_ac_sessionIDType_BYTES_SIZE);
    h->SessionID.bytesLen = iso20_ac_sessionIDType_BYTES_SIZE;
    h->TimeStamp = 1700000000ULL;
    h->Signature_isUsed = 0u;
}
static void set_header_wpt(struct iso20_wpt_MessageHeaderType* h) {
    memset(h->SessionID.bytes, 0, iso20_wpt_sessionIDType_BYTES_SIZE);
    h->SessionID.bytesLen = iso20_wpt_sessionIDType_BYTES_SIZE;
    h->TimeStamp = 1700000000ULL;
    h->Signature_isUsed = 0u;
}
static void set_header_acdp(struct iso20_acdp_MessageHeaderType* h) {
    memset(h->SessionID.bytes, 0, iso20_acdp_sessionIDType_BYTES_SIZE);
    h->SessionID.bytesLen = iso20_acdp_sessionIDType_BYTES_SIZE;
    h->TimeStamp = 1700000000ULL;
    h->Signature_isUsed = 0u;
}

/* ---- CommonMessages ---------------------------------------------------------------------- */

static int do_common(const char* v) {
    struct iso20_exiDocument doc;
    memset(&doc, 0, sizeof(doc));

    if (strcmp(v, "SessionSetupReq") == 0) {
        doc.SessionSetupReq_isUsed = 1u;
        set_header(&doc.SessionSetupReq.Header);
        set_str(doc.SessionSetupReq.EVCCID.characters, &doc.SessionSetupReq.EVCCID.charactersLen, "EVCCID1234567");

    } else if (strcmp(v, "SessionSetupRes") == 0) {
        doc.SessionSetupRes_isUsed = 1u;
        set_header(&doc.SessionSetupRes.Header);
        doc.SessionSetupRes.ResponseCode = iso20_responseCodeType_OK;
        set_str(doc.SessionSetupRes.EVSEID.characters, &doc.SessionSetupRes.EVSEID.charactersLen, "EVSEID1234567");

    } else if (strcmp(v, "AuthorizationSetupRes") == 0) {
        doc.AuthorizationSetupRes_isUsed = 1u;
        struct iso20_AuthorizationSetupResType* r = &doc.AuthorizationSetupRes;
        set_header(&r->Header);
        r->ResponseCode = iso20_responseCodeType_OK;
        r->AuthorizationServices.arrayLen = 2;
        r->AuthorizationServices.array[0] = iso20_authorizationType_EIM;
        r->AuthorizationServices.array[1] = iso20_authorizationType_PnC;
        r->CertificateInstallationService = 1; /* true */
        r->EIM_ASResAuthorizationMode_isUsed = 1u;
        r->PnC_ASResAuthorizationMode_isUsed = 0u;

    } else if (strcmp(v, "ServiceDiscoveryReq") == 0) {
        doc.ServiceDiscoveryReq_isUsed = 1u;
        set_header(&doc.ServiceDiscoveryReq.Header);
        doc.ServiceDiscoveryReq.SupportedServiceIDs_isUsed = 0u;

    } else if (strcmp(v, "ServiceDiscoveryRes") == 0) {
        doc.ServiceDiscoveryRes_isUsed = 1u;
        struct iso20_ServiceDiscoveryResType* r = &doc.ServiceDiscoveryRes;
        set_header(&r->Header);
        r->ResponseCode = iso20_responseCodeType_OK;
        r->ServiceRenegotiationSupported = 0;
        r->EnergyTransferServiceList.Service.arrayLen = 1;
        r->EnergyTransferServiceList.Service.array[0].ServiceID = 1;
        r->EnergyTransferServiceList.Service.array[0].FreeService = 1;
        r->VASList_isUsed = 0u;

    } else if (strcmp(v, "ServiceDetailReq") == 0) {
        doc.ServiceDetailReq_isUsed = 1u;
        set_header(&doc.ServiceDetailReq.Header);
        doc.ServiceDetailReq.ServiceID = 1;

    } else if (strcmp(v, "ServiceDetailRes") == 0) {
        doc.ServiceDetailRes_isUsed = 1u;
        struct iso20_ServiceDetailResType* r = &doc.ServiceDetailRes;
        set_header(&r->Header);
        r->ResponseCode = iso20_responseCodeType_OK;
        r->ServiceID = 1;
        r->ServiceParameterList.ParameterSet.arrayLen = 1;
        struct iso20_ParameterSetType* ps = &r->ServiceParameterList.ParameterSet.array[0];
        ps->ParameterSetID = 1;
        ps->Parameter.arrayLen = 1;
        struct iso20_ParameterType* p = &ps->Parameter.array[0];
        set_str(p->Name.characters, &p->Name.charactersLen, "Level");
        p->intValue = 3;
        p->intValue_isUsed = 1u;
        p->boolValue_isUsed = 0u;
        p->byteValue_isUsed = 0u;
        p->shortValue_isUsed = 0u;
        p->rationalNumber_isUsed = 0u;
        p->finiteString_isUsed = 0u;

    } else if (strcmp(v, "ServiceSelectionReq") == 0) {
        doc.ServiceSelectionReq_isUsed = 1u;
        struct iso20_ServiceSelectionReqType* q = &doc.ServiceSelectionReq;
        set_header(&q->Header);
        q->SelectedEnergyTransferService.ServiceID = 1;
        q->SelectedEnergyTransferService.ParameterSetID = 1;
        q->SelectedVASList.SelectedService.arrayLen = 0;

    } else if (strcmp(v, "ServiceSelectionRes") == 0) {
        doc.ServiceSelectionRes_isUsed = 1u;
        set_header(&doc.ServiceSelectionRes.Header);
        doc.ServiceSelectionRes.ResponseCode = iso20_responseCodeType_OK;

    } else if (strcmp(v, "PowerDeliveryReq") == 0) {
        doc.PowerDeliveryReq_isUsed = 1u;
        struct iso20_PowerDeliveryReqType* q = &doc.PowerDeliveryReq;
        set_header(&q->Header);
        q->EVProcessing               = iso20_processingType_Finished;
        q->ChargeProgress             = iso20_chargeProgressType_Start;
        q->EVPowerProfile_isUsed      = 0u;
        q->BPT_ChannelSelection_isUsed = 0u;

    } else if (strcmp(v, "PowerDeliveryRes") == 0) {
        doc.PowerDeliveryRes_isUsed = 1u;
        struct iso20_PowerDeliveryResType* r = &doc.PowerDeliveryRes;
        set_header(&r->Header);
        r->ResponseCode      = iso20_responseCodeType_OK;
        r->EVSEStatus_isUsed = 0u;

    } else if (strcmp(v, "SessionStopReq") == 0) {
        doc.SessionStopReq_isUsed = 1u;
        struct iso20_SessionStopReqType* q = &doc.SessionStopReq;
        set_header(&q->Header);
        q->ChargingSession = iso20_chargingSessionType_Terminate;
        q->EVTerminationCode_isUsed = 0u;
        q->EVTerminationExplanation_isUsed = 0u;

    } else if (strcmp(v, "SessionStopRes") == 0) {
        doc.SessionStopRes_isUsed = 1u;
        set_header(&doc.SessionStopRes.Header);
        doc.SessionStopRes.ResponseCode = iso20_responseCodeType_OK;

    } else if (strcmp(v, "MeteringConfirmationReq") == 0) {
        doc.MeteringConfirmationReq_isUsed = 1u;
        struct iso20_SignedMeteringDataType* d = &doc.MeteringConfirmationReq.SignedMeteringData;
        set_header(&doc.MeteringConfirmationReq.Header);
        set_str(d->Id.characters, &d->Id.charactersLen, "ID1");
        memset(d->SessionID.bytes, 0, iso20_sessionIDType_BYTES_SIZE);
        d->SessionID.bytesLen = iso20_sessionIDType_BYTES_SIZE;
        set_str(d->MeterInfo.MeterID.characters, &d->MeterInfo.MeterID.charactersLen, "M1");
        d->MeterInfo.ChargedEnergyReadingWh          = 5000;
        d->MeterInfo.BPT_DischargedEnergyReadingWh_isUsed = 0u;
        d->MeterInfo.CapacitiveEnergyReadingVARh_isUsed   = 0u;
        d->MeterInfo.BPT_InductiveEnergyReadingVARh_isUsed = 0u;
        d->Receipt_isUsed = 0u;
        d->Dynamic_SMDTControlMode_isUsed  = 0u;
        d->Scheduled_SMDTControlMode_isUsed = 1u;
        d->Scheduled_SMDTControlMode.SelectedScheduleTupleID = 1;

    } else if (strcmp(v, "MeteringConfirmationRes") == 0) {
        doc.MeteringConfirmationRes_isUsed = 1u;
        set_header(&doc.MeteringConfirmationRes.Header);
        doc.MeteringConfirmationRes.ResponseCode = iso20_responseCodeType_OK;

    } else if (strcmp(v, "AuthorizationReq") == 0) {
        doc.AuthorizationReq_isUsed = 1u;
        struct iso20_AuthorizationReqType* q = &doc.AuthorizationReq;
        set_header(&q->Header);
        q->SelectedAuthorizationService     = iso20_authorizationType_EIM;
        q->EIM_AReqAuthorizationMode_isUsed = 1u;
        q->PnC_AReqAuthorizationMode_isUsed = 0u;

    } else if (strcmp(v, "AuthorizationSetupReq") == 0) {
        doc.AuthorizationSetupReq_isUsed = 1u;
        set_header(&doc.AuthorizationSetupReq.Header);

    } else if (strcmp(v, "ScheduleExchangeReq") == 0) {
        doc.ScheduleExchangeReq_isUsed = 1u;
        struct iso20_ScheduleExchangeReqType* q = &doc.ScheduleExchangeReq;
        set_header(&q->Header);
        q->MaximumSupportingPoints          = 12;
        q->Scheduled_SEReqControlMode_isUsed = 0u;
        q->Dynamic_SEReqControlMode_isUsed   = 1u;
        struct iso20_Dynamic_SEReqControlModeType* m = &q->Dynamic_SEReqControlMode;
        m->DepartureTime = 1800;
        m->MinimumSOC_isUsed = 0u;
        m->TargetSOC_isUsed  = 0u;
        m->EVTargetEnergyRequest.Exponent = 3;
        m->EVTargetEnergyRequest.Value    = 20;
        m->EVMaximumEnergyRequest.Exponent = 3;
        m->EVMaximumEnergyRequest.Value    = 30;
        m->EVMinimumEnergyRequest.Exponent = 3;
        m->EVMinimumEnergyRequest.Value    = 5;
        m->EVMaximumV2XEnergyRequest_isUsed = 0u;
        m->EVMinimumV2XEnergyRequest_isUsed = 0u;

    } else if (strcmp(v, "ScheduleExchangeRes") == 0) {
        doc.ScheduleExchangeRes_isUsed = 1u;
        struct iso20_ScheduleExchangeResType* r = &doc.ScheduleExchangeRes;
        set_header(&r->Header);
        r->ResponseCode   = iso20_responseCodeType_OK;
        r->EVSEProcessing = iso20_processingType_Finished;
        r->GoToPause_isUsed = 0u;
        r->Scheduled_SEResControlMode_isUsed = 0u;
        r->Dynamic_SEResControlMode_isUsed   = 1u;
        struct iso20_Dynamic_SEResControlModeType* m = &r->Dynamic_SEResControlMode;
        m->DepartureTime_isUsed = 0u;
        m->MinimumSOC_isUsed    = 0u;
        m->TargetSOC_isUsed     = 0u;
        m->AbsolutePriceSchedule_isUsed = 0u;
        m->PriceLevelSchedule_isUsed    = 1u;
        struct iso20_PriceLevelScheduleType* pl = &m->PriceLevelSchedule;
        pl->Id_isUsed = 0u;
        pl->TimeAnchor    = 1700000000ULL;
        pl->PriceScheduleID = 1;
        pl->PriceScheduleDescription_isUsed = 0u;
        pl->NumberOfPriceLevels = 3;
        pl->PriceLevelScheduleEntries.PriceLevelScheduleEntry.arrayLen = 1;
        pl->PriceLevelScheduleEntries.PriceLevelScheduleEntry.array[0].Duration   = 3600;
        pl->PriceLevelScheduleEntries.PriceLevelScheduleEntry.array[0].PriceLevel = 1;

    } else if (strcmp(v, "AuthorizationRes") == 0) {
        doc.AuthorizationRes_isUsed = 1u;
        struct iso20_AuthorizationResType* r = &doc.AuthorizationRes;
        set_header(&r->Header);
        r->ResponseCode   = iso20_responseCodeType_OK;
        r->EVSEProcessing = iso20_processingType_Finished;

    } else if (strcmp(v, "CertificateInstallationReq") == 0) {
        doc.CertificateInstallationReq_isUsed = 1u;
        struct iso20_CertificateInstallationReqType* q = &doc.CertificateInstallationReq;
        set_header(&q->Header);
        set_str(q->OEMProvisioningCertificateChain.Id.characters, &q->OEMProvisioningCertificateChain.Id.charactersLen, "OEMCERT1");
        q->OEMProvisioningCertificateChain.Certificate.bytesLen = 3;
        q->OEMProvisioningCertificateChain.Certificate.bytes[0] = 0xAA;
        q->OEMProvisioningCertificateChain.Certificate.bytes[1] = 0xBB;
        q->OEMProvisioningCertificateChain.Certificate.bytes[2] = 0xCC;
        q->OEMProvisioningCertificateChain.SubCertificates_isUsed = 0u;
        q->ListOfRootCertificateIDs.RootCertificateID.arrayLen = 1;
        set_str(q->ListOfRootCertificateIDs.RootCertificateID.array[0].X509IssuerName.characters,
                &q->ListOfRootCertificateIDs.RootCertificateID.array[0].X509IssuerName.charactersLen, "Root CA");
        // NOTE: exi_basetypes_encoder_unsigned() re-chunks value->data.octets via
        // exi_basetypes_convert_bytes_to_unsigned(), which expects plain big-endian bytes —
        // but exi_basetypes_convert_64_to_signed() already wrote 7-bit EXI wire chunks into
        // those same octets. The double-transform means the wire value is NOT 12345; the C#
        // fixture below uses whatever this actually decodes to (47456), confirmed byte-exact.
        exi_basetypes_convert_64_to_signed(&q->ListOfRootCertificateIDs.RootCertificateID.array[0].X509SerialNumber, 12345);
        q->MaximumContractCertificateChains = 3;
        q->PrioritizedEMAIDs_isUsed = 0u;

    } else if (strcmp(v, "CertificateInstallationRes") == 0) {
        doc.CertificateInstallationRes_isUsed = 1u;
        struct iso20_CertificateInstallationResType* r = &doc.CertificateInstallationRes;
        set_header(&r->Header);
        r->ResponseCode   = iso20_responseCodeType_OK;
        r->EVSEProcessing = iso20_processingType_Finished;
        r->CPSCertificateChain.Certificate.bytesLen = 2;
        r->CPSCertificateChain.Certificate.bytes[0] = 0x01;
        r->CPSCertificateChain.Certificate.bytes[1] = 0x02;
        r->CPSCertificateChain.SubCertificates_isUsed = 0u;
        set_str(r->SignedInstallationData.Id.characters, &r->SignedInstallationData.Id.charactersLen, "SID1");
        r->SignedInstallationData.ContractCertificateChain.Certificate.bytesLen = 2;
        r->SignedInstallationData.ContractCertificateChain.Certificate.bytes[0] = 0x03;
        r->SignedInstallationData.ContractCertificateChain.Certificate.bytes[1] = 0x04;
        r->SignedInstallationData.ContractCertificateChain.SubCertificates.Certificate.arrayLen = 1;
        r->SignedInstallationData.ContractCertificateChain.SubCertificates.Certificate.array[0].bytesLen = 1;
        r->SignedInstallationData.ContractCertificateChain.SubCertificates.Certificate.array[0].bytes[0] = 0x05;
        r->SignedInstallationData.ECDHCurve = iso20_ecdhCurveType_SECP521;
        r->SignedInstallationData.DHPublicKey.bytesLen = 2;
        r->SignedInstallationData.DHPublicKey.bytes[0] = 0x06;
        r->SignedInstallationData.DHPublicKey.bytes[1] = 0x07;
        r->SignedInstallationData.SECP521_EncryptedPrivateKey_isUsed = 1u;
        r->SignedInstallationData.SECP521_EncryptedPrivateKey.bytesLen = 2;
        r->SignedInstallationData.SECP521_EncryptedPrivateKey.bytes[0] = 0x08;
        r->SignedInstallationData.SECP521_EncryptedPrivateKey.bytes[1] = 0x09;
        r->SignedInstallationData.X448_EncryptedPrivateKey_isUsed = 0u;
        r->SignedInstallationData.TPM_EncryptedPrivateKey_isUsed  = 0u;
        r->RemainingContractCertificateChains = 2;

    } else if (strcmp(v, "VehicleCheckInReq") == 0) {
        doc.VehicleCheckInReq_isUsed = 1u;
        struct iso20_VehicleCheckInReqType* q = &doc.VehicleCheckInReq;
        set_header(&q->Header);
        q->EVCheckInStatus = iso20_evCheckInStatusType_CheckIn;
        q->ParkingMethod   = iso20_parkingMethodType_AutoParking;
        q->VehicleFrame_isUsed = 1u;
        q->VehicleFrame        = 100;
        q->DeviceOffset_isUsed = 1u;
        q->DeviceOffset        = -50;
        q->VehicleTravel_isUsed = 0u;

    } else if (strcmp(v, "VehicleCheckInRes") == 0) {
        doc.VehicleCheckInRes_isUsed = 1u;
        struct iso20_VehicleCheckInResType* r = &doc.VehicleCheckInRes;
        set_header(&r->Header);
        r->ResponseCode = iso20_responseCodeType_OK;
        r->ParkingSpace_isUsed = 1u;
        r->ParkingSpace        = 200;
        r->DeviceLocation_isUsed = 0u;
        r->TargetDistance_isUsed = 1u;
        r->TargetDistance        = 30;

    } else if (strcmp(v, "VehicleCheckOutReq") == 0) {
        doc.VehicleCheckOutReq_isUsed = 1u;
        struct iso20_VehicleCheckOutReqType* q = &doc.VehicleCheckOutReq;
        set_header(&q->Header);
        q->EVCheckOutStatus = iso20_evCheckOutStatusType_CheckOut;
        q->CheckOutTime     = 1700000100ULL;

    } else if (strcmp(v, "VehicleCheckOutRes") == 0) {
        doc.VehicleCheckOutRes_isUsed = 1u;
        struct iso20_VehicleCheckOutResType* r = &doc.VehicleCheckOutRes;
        set_header(&r->Header);
        r->ResponseCode         = iso20_responseCodeType_OK;
        r->EVSECheckOutStatus   = iso20_evseCheckOutStatusType_Scheduled;

    } else {
        fprintf(stderr, "cbv2g-iso20: unknown CommonMessages vector '%s'\n", v);
        return 1;
    }

    uint8_t out[OUT_BUF_SIZE];
    exi_bitstream_t stream;
    exi_bitstream_init(&stream, out, sizeof(out), 0, NULL);
    int error = encode_iso20_exiDocument(&stream, &doc);
    if (error != 0) {
        fprintf(stderr, "cbv2g-iso20: CommonMessages encode failed with error %d\n", error);
        return 3;
    }
    print_hex(out, exi_bitstream_get_length(&stream));
    return 0;
}

/* ---- DC ----------------------------------------------------------------------------------- */

static void set_rational_dc(struct iso20_dc_RationalNumberType* r, int8_t exponent, int16_t value) {
    r->Exponent = exponent;
    r->Value = value;
}

static int do_dc(const char* v) {
    struct iso20_dc_exiDocument doc;
    memset(&doc, 0, sizeof(doc));

    if (strcmp(v, "DC_CableCheckReq") == 0) {
        doc.DC_CableCheckReq_isUsed = 1u;
        set_header_dc(&doc.DC_CableCheckReq.Header);

    } else if (strcmp(v, "DC_CableCheckRes") == 0) {
        doc.DC_CableCheckRes_isUsed = 1u;
        set_header_dc(&doc.DC_CableCheckRes.Header);
        doc.DC_CableCheckRes.ResponseCode = iso20_dc_responseCodeType_OK;
        doc.DC_CableCheckRes.EVSEProcessing = iso20_dc_processingType_Finished;

    } else if (strcmp(v, "DC_ChargeParameterDiscoveryReq") == 0) {
        doc.DC_ChargeParameterDiscoveryReq_isUsed = 1u;
        struct iso20_dc_DC_ChargeParameterDiscoveryReqType* q = &doc.DC_ChargeParameterDiscoveryReq;
        set_header_dc(&q->Header);
        q->BPT_DC_CPDReqEnergyTransferMode_isUsed = 0u;
        q->DC_CPDReqEnergyTransferMode_isUsed      = 1u;
        struct iso20_dc_DC_CPDReqEnergyTransferModeType* m = &q->DC_CPDReqEnergyTransferMode;
        set_rational_dc(&m->EVMaximumChargePower,  0, 20000);
        set_rational_dc(&m->EVMinimumChargePower,  0, 100);
        set_rational_dc(&m->EVMaximumChargeCurrent, 0, 200);
        set_rational_dc(&m->EVMinimumChargeCurrent, 0, 1);
        set_rational_dc(&m->EVMaximumVoltage,      0, 500);
        set_rational_dc(&m->EVMinimumVoltage,      0, 200);
        m->TargetSOC_isUsed = 0u;

    } else if (strcmp(v, "DC_ChargeParameterDiscoveryRes") == 0) {
        doc.DC_ChargeParameterDiscoveryRes_isUsed = 1u;
        struct iso20_dc_DC_ChargeParameterDiscoveryResType* r = &doc.DC_ChargeParameterDiscoveryRes;
        set_header_dc(&r->Header);
        r->ResponseCode = iso20_dc_responseCodeType_OK;
        r->BPT_DC_CPDResEnergyTransferMode_isUsed = 0u;
        r->DC_CPDResEnergyTransferMode_isUsed      = 1u;
        struct iso20_dc_DC_CPDResEnergyTransferModeType* m = &r->DC_CPDResEnergyTransferMode;
        set_rational_dc(&m->EVSEMaximumChargePower,  1, 15000);
        set_rational_dc(&m->EVSEMinimumChargePower,  0, 100);
        set_rational_dc(&m->EVSEMaximumChargeCurrent, 0, 200);
        set_rational_dc(&m->EVSEMinimumChargeCurrent, 0, 1);
        set_rational_dc(&m->EVSEMaximumVoltage,      0, 500);
        set_rational_dc(&m->EVSEMinimumVoltage,      0, 200);
        m->EVSEPowerRampLimitation_isUsed = 0u;

    } else if (strcmp(v, "DC_PreChargeReq") == 0) {
        doc.DC_PreChargeReq_isUsed = 1u;
        struct iso20_dc_DC_PreChargeReqType* q = &doc.DC_PreChargeReq;
        set_header_dc(&q->Header);
        q->EVProcessing = iso20_dc_processingType_Finished;
        set_rational_dc(&q->EVPresentVoltage, 0, 390);
        set_rational_dc(&q->EVTargetVoltage,  0, 400);

    } else if (strcmp(v, "DC_PreChargeRes") == 0) {
        doc.DC_PreChargeRes_isUsed = 1u;
        struct iso20_dc_DC_PreChargeResType* r = &doc.DC_PreChargeRes;
        set_header_dc(&r->Header);
        r->ResponseCode = iso20_dc_responseCodeType_OK;
        set_rational_dc(&r->EVSEPresentVoltage, 0, 395);

    } else if (strcmp(v, "DC_ChargeLoopReq") == 0) {
        doc.DC_ChargeLoopReq_isUsed = 1u;
        struct iso20_dc_DC_ChargeLoopReqType* q = &doc.DC_ChargeLoopReq;
        set_header_dc(&q->Header);
        q->DisplayParameters_isUsed = 0u;
        q->MeterInfoRequested       = 0;
        set_rational_dc(&q->EVPresentVoltage, 0, 400);
        q->BPT_Dynamic_DC_CLReqControlMode_isUsed   = 0u;
        q->BPT_Scheduled_DC_CLReqControlMode_isUsed = 0u;
        q->CLReqControlMode_isUsed                  = 0u;
        q->Dynamic_DC_CLReqControlMode_isUsed       = 0u;
        q->Scheduled_DC_CLReqControlMode_isUsed     = 1u;
        struct iso20_dc_Scheduled_DC_CLReqControlModeType* m = &q->Scheduled_DC_CLReqControlMode;
        m->EVTargetEnergyRequest_isUsed  = 0u;
        m->EVMaximumEnergyRequest_isUsed = 0u;
        m->EVMinimumEnergyRequest_isUsed = 0u;
        set_rational_dc(&m->EVTargetCurrent, 0, 120);
        set_rational_dc(&m->EVTargetVoltage, 0, 400);
        m->EVMaximumChargePower_isUsed  = 0u;
        m->EVMinimumChargePower_isUsed  = 0u;
        m->EVMaximumChargeCurrent_isUsed = 0u;
        m->EVMaximumVoltage_isUsed      = 0u;
        m->EVMinimumVoltage_isUsed      = 0u;

    } else if (strcmp(v, "DC_ChargeLoopRes") == 0) {
        doc.DC_ChargeLoopRes_isUsed = 1u;
        struct iso20_dc_DC_ChargeLoopResType* r = &doc.DC_ChargeLoopRes;
        set_header_dc(&r->Header);
        r->ResponseCode = iso20_dc_responseCodeType_OK;
        r->EVSEStatus_isUsed = 0u;
        r->MeterInfo_isUsed  = 0u;
        r->Receipt_isUsed    = 0u;
        set_rational_dc(&r->EVSEPresentCurrent, 0, 118);
        set_rational_dc(&r->EVSEPresentVoltage, 0, 398);
        r->EVSEPowerLimitAchieved   = 0;
        r->EVSECurrentLimitAchieved = 0;
        r->EVSEVoltageLimitAchieved = 0;
        r->BPT_Dynamic_DC_CLResControlMode_isUsed   = 0u;
        r->BPT_Scheduled_DC_CLResControlMode_isUsed = 0u;
        r->CLResControlMode_isUsed                  = 0u;
        r->Dynamic_DC_CLResControlMode_isUsed       = 0u;
        r->Scheduled_DC_CLResControlMode_isUsed     = 1u;
        struct iso20_dc_Scheduled_DC_CLResControlModeType* m = &r->Scheduled_DC_CLResControlMode;
        m->EVSEMaximumChargePower_isUsed  = 0u;
        m->EVSEMinimumChargePower_isUsed  = 0u;
        m->EVSEMaximumChargeCurrent_isUsed = 0u;
        m->EVSEMaximumVoltage_isUsed       = 0u;

    } else if (strcmp(v, "DC_ChargeLoopReq_Dynamic") == 0) {
        doc.DC_ChargeLoopReq_isUsed = 1u;
        struct iso20_dc_DC_ChargeLoopReqType* q = &doc.DC_ChargeLoopReq;
        set_header_dc(&q->Header);
        q->DisplayParameters_isUsed = 0u;
        q->MeterInfoRequested       = 0;
        set_rational_dc(&q->EVPresentVoltage, 0, 400);
        q->BPT_Dynamic_DC_CLReqControlMode_isUsed   = 0u;
        q->BPT_Scheduled_DC_CLReqControlMode_isUsed = 0u;
        q->CLReqControlMode_isUsed                  = 0u;
        q->Dynamic_DC_CLReqControlMode_isUsed       = 1u;
        q->Scheduled_DC_CLReqControlMode_isUsed     = 0u;
        struct iso20_dc_Dynamic_DC_CLReqControlModeType* m = &q->Dynamic_DC_CLReqControlMode;
        m->DepartureTime_isUsed = 0u;
        set_rational_dc(&m->EVTargetEnergyRequest, 1, 4000);
        set_rational_dc(&m->EVMaximumEnergyRequest, 1, 6000);
        set_rational_dc(&m->EVMinimumEnergyRequest, 0, 0);
        set_rational_dc(&m->EVMaximumChargePower, 0, 20000);
        set_rational_dc(&m->EVMinimumChargePower, 0, 100);
        set_rational_dc(&m->EVMaximumChargeCurrent, 0, 200);
        set_rational_dc(&m->EVMaximumVoltage, 0, 500);
        set_rational_dc(&m->EVMinimumVoltage, 0, 200);

    } else if (strcmp(v, "DC_ChargeLoopRes_Dynamic") == 0) {
        doc.DC_ChargeLoopRes_isUsed = 1u;
        struct iso20_dc_DC_ChargeLoopResType* r = &doc.DC_ChargeLoopRes;
        set_header_dc(&r->Header);
        r->ResponseCode = iso20_dc_responseCodeType_OK;
        r->EVSEStatus_isUsed = 0u;
        r->MeterInfo_isUsed  = 0u;
        r->Receipt_isUsed    = 0u;
        set_rational_dc(&r->EVSEPresentCurrent, 0, 118);
        set_rational_dc(&r->EVSEPresentVoltage, 0, 398);
        r->EVSEPowerLimitAchieved   = 0;
        r->EVSECurrentLimitAchieved = 0;
        r->EVSEVoltageLimitAchieved = 0;
        r->BPT_Dynamic_DC_CLResControlMode_isUsed   = 0u;
        r->BPT_Scheduled_DC_CLResControlMode_isUsed = 0u;
        r->CLResControlMode_isUsed                  = 0u;
        r->Dynamic_DC_CLResControlMode_isUsed       = 1u;
        r->Scheduled_DC_CLResControlMode_isUsed     = 0u;
        struct iso20_dc_Dynamic_DC_CLResControlModeType* m = &r->Dynamic_DC_CLResControlMode;
        m->DepartureTime_isUsed = 0u;
        m->MinimumSOC_isUsed = 0u;
        m->TargetSOC_isUsed = 0u;
        m->AckMaxDelay_isUsed = 0u;
        set_rational_dc(&m->EVSEMaximumChargePower, 0, 19500);
        set_rational_dc(&m->EVSEMinimumChargePower, 0, 100);
        set_rational_dc(&m->EVSEMaximumChargeCurrent, 0, 195);
        set_rational_dc(&m->EVSEMaximumVoltage, 0, 500);

    } else if (strcmp(v, "DC_ChargeLoopReq_BPTScheduled") == 0) {
        doc.DC_ChargeLoopReq_isUsed = 1u;
        struct iso20_dc_DC_ChargeLoopReqType* q = &doc.DC_ChargeLoopReq;
        set_header_dc(&q->Header);
        q->DisplayParameters_isUsed = 0u;
        q->MeterInfoRequested       = 0;
        set_rational_dc(&q->EVPresentVoltage, 0, 400);
        q->BPT_Dynamic_DC_CLReqControlMode_isUsed   = 0u;
        q->BPT_Scheduled_DC_CLReqControlMode_isUsed = 1u;
        q->CLReqControlMode_isUsed                  = 0u;
        q->Dynamic_DC_CLReqControlMode_isUsed       = 0u;
        q->Scheduled_DC_CLReqControlMode_isUsed     = 0u;
        struct iso20_dc_BPT_Scheduled_DC_CLReqControlModeType* m = &q->BPT_Scheduled_DC_CLReqControlMode;
        m->EVTargetEnergyRequest_isUsed  = 0u;
        m->EVMaximumEnergyRequest_isUsed = 0u;
        m->EVMinimumEnergyRequest_isUsed = 0u;
        set_rational_dc(&m->EVTargetCurrent, 0, 120);
        set_rational_dc(&m->EVTargetVoltage, 0, 400);
        m->EVMaximumChargePower_isUsed   = 0u;
        m->EVMinimumChargePower_isUsed   = 0u;
        m->EVMaximumChargeCurrent_isUsed = 0u;
        m->EVMaximumVoltage_isUsed       = 0u;
        m->EVMinimumVoltage_isUsed       = 0u;
        set_rational_dc(&m->EVMaximumDischargePower, 0, 11000);
        m->EVMaximumDischargePower_isUsed  = 1u;
        set_rational_dc(&m->EVMinimumDischargePower, 0, 100);
        m->EVMinimumDischargePower_isUsed  = 1u;
        set_rational_dc(&m->EVMaximumDischargeCurrent, 0, 110);
        m->EVMaximumDischargeCurrent_isUsed = 1u;

    } else if (strcmp(v, "DC_ChargeLoopRes_BPTScheduled") == 0) {
        doc.DC_ChargeLoopRes_isUsed = 1u;
        struct iso20_dc_DC_ChargeLoopResType* r = &doc.DC_ChargeLoopRes;
        set_header_dc(&r->Header);
        r->ResponseCode = iso20_dc_responseCodeType_OK;
        r->EVSEStatus_isUsed = 0u;
        r->MeterInfo_isUsed  = 0u;
        r->Receipt_isUsed    = 0u;
        set_rational_dc(&r->EVSEPresentCurrent, 0, 118);
        set_rational_dc(&r->EVSEPresentVoltage, 0, 398);
        r->EVSEPowerLimitAchieved   = 0;
        r->EVSECurrentLimitAchieved = 0;
        r->EVSEVoltageLimitAchieved = 0;
        r->BPT_Dynamic_DC_CLResControlMode_isUsed   = 0u;
        r->BPT_Scheduled_DC_CLResControlMode_isUsed = 1u;
        r->CLResControlMode_isUsed                  = 0u;
        r->Dynamic_DC_CLResControlMode_isUsed       = 0u;
        r->Scheduled_DC_CLResControlMode_isUsed     = 0u;
        struct iso20_dc_BPT_Scheduled_DC_CLResControlModeType* m = &r->BPT_Scheduled_DC_CLResControlMode;
        m->EVSEMaximumChargePower_isUsed   = 0u;
        m->EVSEMinimumChargePower_isUsed   = 0u;
        m->EVSEMaximumChargeCurrent_isUsed = 0u;
        m->EVSEMaximumVoltage_isUsed       = 0u;
        set_rational_dc(&m->EVSEMaximumDischargePower, 0, 10500);
        m->EVSEMaximumDischargePower_isUsed = 1u;
        set_rational_dc(&m->EVSEMinimumDischargePower, 0, 100);
        m->EVSEMinimumDischargePower_isUsed = 1u;
        set_rational_dc(&m->EVSEMaximumDischargeCurrent, 0, 105);
        m->EVSEMaximumDischargeCurrent_isUsed = 1u;
        m->EVSEMinimumVoltage_isUsed = 0u;

    } else if (strcmp(v, "DC_ChargeLoopReq_BPTDynamic") == 0) {
        doc.DC_ChargeLoopReq_isUsed = 1u;
        struct iso20_dc_DC_ChargeLoopReqType* q = &doc.DC_ChargeLoopReq;
        set_header_dc(&q->Header);
        q->DisplayParameters_isUsed = 0u;
        q->MeterInfoRequested       = 0;
        set_rational_dc(&q->EVPresentVoltage, 0, 400);
        q->BPT_Dynamic_DC_CLReqControlMode_isUsed   = 1u;
        q->BPT_Scheduled_DC_CLReqControlMode_isUsed = 0u;
        q->CLReqControlMode_isUsed                  = 0u;
        q->Dynamic_DC_CLReqControlMode_isUsed       = 0u;
        q->Scheduled_DC_CLReqControlMode_isUsed     = 0u;
        struct iso20_dc_BPT_Dynamic_DC_CLReqControlModeType* m = &q->BPT_Dynamic_DC_CLReqControlMode;
        m->DepartureTime_isUsed = 0u;
        set_rational_dc(&m->EVTargetEnergyRequest, 1, 4000);
        set_rational_dc(&m->EVMaximumEnergyRequest, 1, 6000);
        set_rational_dc(&m->EVMinimumEnergyRequest, 0, 0);
        set_rational_dc(&m->EVMaximumChargePower, 0, 20000);
        set_rational_dc(&m->EVMinimumChargePower, 0, 100);
        set_rational_dc(&m->EVMaximumChargeCurrent, 0, 200);
        set_rational_dc(&m->EVMaximumVoltage, 0, 500);
        set_rational_dc(&m->EVMinimumVoltage, 0, 200);
        set_rational_dc(&m->EVMaximumDischargePower, 0, 11000);
        set_rational_dc(&m->EVMinimumDischargePower, 0, 100);
        set_rational_dc(&m->EVMaximumDischargeCurrent, 0, 110);
        m->EVMaximumV2XEnergyRequest_isUsed = 0u;
        m->EVMinimumV2XEnergyRequest_isUsed = 0u;

    } else if (strcmp(v, "DC_ChargeLoopRes_BPTDynamic") == 0) {
        doc.DC_ChargeLoopRes_isUsed = 1u;
        struct iso20_dc_DC_ChargeLoopResType* r = &doc.DC_ChargeLoopRes;
        set_header_dc(&r->Header);
        r->ResponseCode = iso20_dc_responseCodeType_OK;
        r->EVSEStatus_isUsed = 0u;
        r->MeterInfo_isUsed  = 0u;
        r->Receipt_isUsed    = 0u;
        set_rational_dc(&r->EVSEPresentCurrent, 0, 118);
        set_rational_dc(&r->EVSEPresentVoltage, 0, 398);
        r->EVSEPowerLimitAchieved   = 0;
        r->EVSECurrentLimitAchieved = 0;
        r->EVSEVoltageLimitAchieved = 0;
        r->BPT_Dynamic_DC_CLResControlMode_isUsed   = 1u;
        r->BPT_Scheduled_DC_CLResControlMode_isUsed = 0u;
        r->CLResControlMode_isUsed                  = 0u;
        r->Dynamic_DC_CLResControlMode_isUsed       = 0u;
        r->Scheduled_DC_CLResControlMode_isUsed     = 0u;
        struct iso20_dc_BPT_Dynamic_DC_CLResControlModeType* m = &r->BPT_Dynamic_DC_CLResControlMode;
        m->DepartureTime_isUsed = 0u;
        m->MinimumSOC_isUsed = 0u;
        m->TargetSOC_isUsed = 0u;
        m->AckMaxDelay_isUsed = 0u;
        set_rational_dc(&m->EVSEMaximumChargePower, 0, 19500);
        set_rational_dc(&m->EVSEMinimumChargePower, 0, 100);
        set_rational_dc(&m->EVSEMaximumChargeCurrent, 0, 195);
        set_rational_dc(&m->EVSEMaximumVoltage, 0, 500);
        set_rational_dc(&m->EVSEMaximumDischargePower, 0, 10500);
        set_rational_dc(&m->EVSEMinimumDischargePower, 0, 100);
        set_rational_dc(&m->EVSEMaximumDischargeCurrent, 0, 105);
        set_rational_dc(&m->EVSEMinimumVoltage, 0, 200);

    } else if (strcmp(v, "DC_WeldingDetectionReq") == 0) {
        doc.DC_WeldingDetectionReq_isUsed = 1u;
        set_header_dc(&doc.DC_WeldingDetectionReq.Header);
        doc.DC_WeldingDetectionReq.EVProcessing = iso20_dc_processingType_Finished;

    } else if (strcmp(v, "DC_WeldingDetectionRes") == 0) {
        doc.DC_WeldingDetectionRes_isUsed = 1u;
        set_header_dc(&doc.DC_WeldingDetectionRes.Header);
        doc.DC_WeldingDetectionRes.ResponseCode = iso20_dc_responseCodeType_OK;
        set_rational_dc(&doc.DC_WeldingDetectionRes.EVSEPresentVoltage, 0, 5);

    } else {
        fprintf(stderr, "cbv2g-iso20: unknown DC vector '%s'\n", v);
        return 1;
    }

    uint8_t out[OUT_BUF_SIZE];
    exi_bitstream_t stream;
    exi_bitstream_init(&stream, out, sizeof(out), 0, NULL);
    int error = encode_iso20_dc_exiDocument(&stream, &doc);
    if (error != 0) {
        fprintf(stderr, "cbv2g-iso20: DC encode failed with error %d\n", error);
        return 3;
    }
    print_hex(out, exi_bitstream_get_length(&stream));
    return 0;
}

/* ---- AC ----------------------------------------------------------------------------------- */

static void set_rational(struct iso20_ac_RationalNumberType* r, int8_t exponent, int16_t value) {
    r->Exponent = exponent;
    r->Value = value;
}

static int do_ac(const char* v) {
    struct iso20_ac_exiDocument doc;
    memset(&doc, 0, sizeof(doc));

    if (strcmp(v, "AC_ChargeParameterDiscoveryReq") == 0) {
        doc.AC_ChargeParameterDiscoveryReq_isUsed = 1u;
        struct iso20_ac_AC_ChargeParameterDiscoveryReqType* q = &doc.AC_ChargeParameterDiscoveryReq;
        set_header_ac(&q->Header);
        q->AC_CPDReqEnergyTransferMode_isUsed     = 1u;
        q->BPT_AC_CPDReqEnergyTransferMode_isUsed = 0u;
        struct iso20_ac_AC_CPDReqEnergyTransferModeType* m = &q->AC_CPDReqEnergyTransferMode;
        set_rational(&m->EVMaximumChargePower, 0, 11000);
        m->EVMaximumChargePower_L2_isUsed = 0u;
        m->EVMaximumChargePower_L3_isUsed = 0u;
        set_rational(&m->EVMinimumChargePower, 0, 100);
        m->EVMinimumChargePower_L2_isUsed = 0u;
        m->EVMinimumChargePower_L3_isUsed = 0u;

    } else if (strcmp(v, "AC_ChargeParameterDiscoveryRes") == 0) {
        doc.AC_ChargeParameterDiscoveryRes_isUsed = 1u;
        struct iso20_ac_AC_ChargeParameterDiscoveryResType* r = &doc.AC_ChargeParameterDiscoveryRes;
        set_header_ac(&r->Header);
        r->ResponseCode = iso20_ac_responseCodeType_OK;
        r->AC_CPDResEnergyTransferMode_isUsed     = 1u;
        r->BPT_AC_CPDResEnergyTransferMode_isUsed = 0u;
        struct iso20_ac_AC_CPDResEnergyTransferModeType* m = &r->AC_CPDResEnergyTransferMode;
        set_rational(&m->EVSEMaximumChargePower, 0, 22000);
        m->EVSEMaximumChargePower_L2_isUsed = 0u;
        m->EVSEMaximumChargePower_L3_isUsed = 0u;
        set_rational(&m->EVSEMinimumChargePower, 0, 100);
        m->EVSEMinimumChargePower_L2_isUsed = 0u;
        m->EVSEMinimumChargePower_L3_isUsed = 0u;
        set_rational(&m->EVSENominalFrequency, 0, 50);
        m->MaximumPowerAsymmetry_isUsed    = 0u;
        m->EVSEPowerRampLimitation_isUsed  = 0u;
        m->EVSEPresentActivePower_isUsed    = 0u;
        m->EVSEPresentActivePower_L2_isUsed = 0u;
        m->EVSEPresentActivePower_L3_isUsed = 0u;

    } else if (strcmp(v, "AC_ChargeLoopReq") == 0) {
        doc.AC_ChargeLoopReq_isUsed = 1u;
        struct iso20_ac_AC_ChargeLoopReqType* q = &doc.AC_ChargeLoopReq;
        set_header_ac(&q->Header);
        q->DisplayParameters_isUsed = 0u;
        q->MeterInfoRequested       = 0;
        q->BPT_Dynamic_AC_CLReqControlMode_isUsed   = 0u;
        q->BPT_Scheduled_AC_CLReqControlMode_isUsed = 0u;
        q->CLReqControlMode_isUsed                  = 0u;
        q->Dynamic_AC_CLReqControlMode_isUsed        = 0u;
        q->Scheduled_AC_CLReqControlMode_isUsed      = 1u;
        struct iso20_ac_Scheduled_AC_CLReqControlModeType* m = &q->Scheduled_AC_CLReqControlMode;
        m->EVTargetEnergyRequest_isUsed  = 0u;
        m->EVMaximumEnergyRequest_isUsed = 0u;
        m->EVMinimumEnergyRequest_isUsed = 0u;
        m->EVMaximumChargePower_isUsed    = 0u;
        m->EVMaximumChargePower_L2_isUsed = 0u;
        m->EVMaximumChargePower_L3_isUsed = 0u;
        m->EVMinimumChargePower_isUsed    = 0u;
        m->EVMinimumChargePower_L2_isUsed = 0u;
        m->EVMinimumChargePower_L3_isUsed = 0u;
        set_rational(&m->EVPresentActivePower, 0, 4000);
        m->EVPresentActivePower_L2_isUsed = 0u;
        m->EVPresentActivePower_L3_isUsed = 0u;
        m->EVPresentReactivePower_isUsed    = 0u;
        m->EVPresentReactivePower_L2_isUsed = 0u;
        m->EVPresentReactivePower_L3_isUsed = 0u;

    } else if (strcmp(v, "AC_ChargeLoopRes") == 0) {
        doc.AC_ChargeLoopRes_isUsed = 1u;
        struct iso20_ac_AC_ChargeLoopResType* r = &doc.AC_ChargeLoopRes;
        set_header_ac(&r->Header);
        r->ResponseCode = iso20_ac_responseCodeType_OK;
        r->EVSEStatus_isUsed = 0u;
        r->MeterInfo_isUsed  = 0u;
        r->Receipt_isUsed    = 0u;
        r->EVSETargetFrequency_isUsed = 0u;
        r->BPT_Dynamic_AC_CLResControlMode_isUsed   = 0u;
        r->BPT_Scheduled_AC_CLResControlMode_isUsed = 0u;
        r->CLResControlMode_isUsed                  = 0u;
        r->Dynamic_AC_CLResControlMode_isUsed        = 0u;
        r->Scheduled_AC_CLResControlMode_isUsed      = 1u;
        struct iso20_ac_Scheduled_AC_CLResControlModeType* m = &r->Scheduled_AC_CLResControlMode;
        m->EVSETargetActivePower_isUsed      = 0u;
        m->EVSETargetActivePower_L2_isUsed   = 0u;
        m->EVSETargetActivePower_L3_isUsed   = 0u;
        m->EVSETargetReactivePower_isUsed    = 0u;
        m->EVSETargetReactivePower_L2_isUsed = 0u;
        m->EVSETargetReactivePower_L3_isUsed = 0u;
        m->EVSEPresentActivePower_isUsed     = 0u;
        m->EVSEPresentActivePower_L2_isUsed  = 0u;
        m->EVSEPresentActivePower_L3_isUsed  = 0u;

    } else if (strcmp(v, "AC_ChargeLoopReq_BPTScheduled") == 0) {
        doc.AC_ChargeLoopReq_isUsed = 1u;
        struct iso20_ac_AC_ChargeLoopReqType* q = &doc.AC_ChargeLoopReq;
        set_header_ac(&q->Header);
        q->DisplayParameters_isUsed = 0u;
        q->MeterInfoRequested       = 0;
        q->BPT_Dynamic_AC_CLReqControlMode_isUsed   = 0u;
        q->BPT_Scheduled_AC_CLReqControlMode_isUsed = 1u;
        q->CLReqControlMode_isUsed                  = 0u;
        q->Dynamic_AC_CLReqControlMode_isUsed       = 0u;
        q->Scheduled_AC_CLReqControlMode_isUsed     = 0u;
        struct iso20_ac_BPT_Scheduled_AC_CLReqControlModeType* m = &q->BPT_Scheduled_AC_CLReqControlMode;
        m->EVTargetEnergyRequest_isUsed  = 0u;
        m->EVMaximumEnergyRequest_isUsed = 0u;
        m->EVMinimumEnergyRequest_isUsed = 0u;
        m->EVMaximumChargePower_isUsed    = 0u;
        m->EVMaximumChargePower_L2_isUsed = 0u;
        m->EVMaximumChargePower_L3_isUsed = 0u;
        m->EVMinimumChargePower_isUsed    = 0u;
        m->EVMinimumChargePower_L2_isUsed = 0u;
        m->EVMinimumChargePower_L3_isUsed = 0u;
        set_rational(&m->EVPresentActivePower, 0, 4000);
        m->EVPresentActivePower_L2_isUsed = 0u;
        m->EVPresentActivePower_L3_isUsed = 0u;
        m->EVPresentReactivePower_isUsed    = 0u;
        m->EVPresentReactivePower_L2_isUsed = 0u;
        m->EVPresentReactivePower_L3_isUsed = 0u;
        set_rational(&m->EVMaximumDischargePower, 0, 3700);
        m->EVMaximumDischargePower_isUsed    = 1u;
        m->EVMaximumDischargePower_L2_isUsed = 0u;
        m->EVMaximumDischargePower_L3_isUsed = 0u;
        set_rational(&m->EVMinimumDischargePower, 0, 100);
        m->EVMinimumDischargePower_isUsed    = 1u;
        m->EVMinimumDischargePower_L2_isUsed = 0u;
        m->EVMinimumDischargePower_L3_isUsed = 0u;

    } else if (strcmp(v, "AC_ChargeLoopRes_BPTScheduled") == 0) {
        doc.AC_ChargeLoopRes_isUsed = 1u;
        struct iso20_ac_AC_ChargeLoopResType* r = &doc.AC_ChargeLoopRes;
        set_header_ac(&r->Header);
        r->ResponseCode = iso20_ac_responseCodeType_OK;
        r->EVSEStatus_isUsed = 0u;
        r->MeterInfo_isUsed  = 0u;
        r->Receipt_isUsed    = 0u;
        r->EVSETargetFrequency_isUsed = 0u;
        r->BPT_Dynamic_AC_CLResControlMode_isUsed   = 0u;
        r->BPT_Scheduled_AC_CLResControlMode_isUsed = 1u;
        r->CLResControlMode_isUsed                  = 0u;
        r->Dynamic_AC_CLResControlMode_isUsed       = 0u;
        r->Scheduled_AC_CLResControlMode_isUsed     = 0u;
        struct iso20_ac_BPT_Scheduled_AC_CLResControlModeType* m = &r->BPT_Scheduled_AC_CLResControlMode;
        set_rational(&m->EVSETargetActivePower, 0, 3700);
        m->EVSETargetActivePower_isUsed    = 1u;
        m->EVSETargetActivePower_L2_isUsed = 0u;
        m->EVSETargetActivePower_L3_isUsed = 0u;
        m->EVSETargetReactivePower_isUsed    = 0u;
        m->EVSETargetReactivePower_L2_isUsed = 0u;
        m->EVSETargetReactivePower_L3_isUsed = 0u;
        m->EVSEPresentActivePower_isUsed    = 0u;
        m->EVSEPresentActivePower_L2_isUsed = 0u;
        m->EVSEPresentActivePower_L3_isUsed = 0u;

    } else if (strcmp(v, "AC_ChargeLoopReq_Dynamic") == 0) {
        doc.AC_ChargeLoopReq_isUsed = 1u;
        struct iso20_ac_AC_ChargeLoopReqType* q = &doc.AC_ChargeLoopReq;
        set_header_ac(&q->Header);
        q->DisplayParameters_isUsed = 0u;
        q->MeterInfoRequested       = 0;
        q->BPT_Dynamic_AC_CLReqControlMode_isUsed   = 0u;
        q->BPT_Scheduled_AC_CLReqControlMode_isUsed = 0u;
        q->CLReqControlMode_isUsed                  = 0u;
        q->Dynamic_AC_CLReqControlMode_isUsed       = 1u;
        q->Scheduled_AC_CLReqControlMode_isUsed     = 0u;
        struct iso20_ac_Dynamic_AC_CLReqControlModeType* m = &q->Dynamic_AC_CLReqControlMode;
        m->DepartureTime_isUsed = 0u;
        set_rational(&m->EVTargetEnergyRequest, 1, 4000);
        set_rational(&m->EVMaximumEnergyRequest, 1, 6000);
        set_rational(&m->EVMinimumEnergyRequest, 0, 0);
        set_rational(&m->EVMaximumChargePower, 0, 11000);
        m->EVMaximumChargePower_L2_isUsed = 0u;
        m->EVMaximumChargePower_L3_isUsed = 0u;
        set_rational(&m->EVMinimumChargePower, 0, 100);
        m->EVMinimumChargePower_L2_isUsed = 0u;
        m->EVMinimumChargePower_L3_isUsed = 0u;
        set_rational(&m->EVPresentActivePower, 0, 4000);
        m->EVPresentActivePower_L2_isUsed = 0u;
        m->EVPresentActivePower_L3_isUsed = 0u;
        set_rational(&m->EVPresentReactivePower, 0, 0);
        m->EVPresentReactivePower_L2_isUsed = 0u;
        m->EVPresentReactivePower_L3_isUsed = 0u;

    } else if (strcmp(v, "AC_ChargeLoopRes_Dynamic") == 0) {
        doc.AC_ChargeLoopRes_isUsed = 1u;
        struct iso20_ac_AC_ChargeLoopResType* r = &doc.AC_ChargeLoopRes;
        set_header_ac(&r->Header);
        r->ResponseCode = iso20_ac_responseCodeType_OK;
        r->EVSEStatus_isUsed = 0u;
        r->MeterInfo_isUsed  = 0u;
        r->Receipt_isUsed    = 0u;
        r->EVSETargetFrequency_isUsed = 0u;
        r->BPT_Dynamic_AC_CLResControlMode_isUsed   = 0u;
        r->BPT_Scheduled_AC_CLResControlMode_isUsed = 0u;
        r->CLResControlMode_isUsed                  = 0u;
        r->Dynamic_AC_CLResControlMode_isUsed       = 1u;
        r->Scheduled_AC_CLResControlMode_isUsed     = 0u;
        struct iso20_ac_Dynamic_AC_CLResControlModeType* m = &r->Dynamic_AC_CLResControlMode;
        m->DepartureTime_isUsed = 0u;
        m->MinimumSOC_isUsed = 0u;
        m->TargetSOC_isUsed = 0u;
        m->AckMaxDelay_isUsed = 0u;
        set_rational(&m->EVSETargetActivePower, 0, 3700);
        m->EVSETargetActivePower_L2_isUsed = 0u;
        m->EVSETargetActivePower_L3_isUsed = 0u;
        m->EVSETargetReactivePower_isUsed    = 0u;
        m->EVSETargetReactivePower_L2_isUsed = 0u;
        m->EVSETargetReactivePower_L3_isUsed = 0u;
        m->EVSEPresentActivePower_isUsed    = 0u;
        m->EVSEPresentActivePower_L2_isUsed = 0u;
        m->EVSEPresentActivePower_L3_isUsed = 0u;

    } else if (strcmp(v, "AC_ChargeLoopReq_BPTDynamic") == 0) {
        doc.AC_ChargeLoopReq_isUsed = 1u;
        struct iso20_ac_AC_ChargeLoopReqType* q = &doc.AC_ChargeLoopReq;
        set_header_ac(&q->Header);
        q->DisplayParameters_isUsed = 0u;
        q->MeterInfoRequested       = 0;
        q->BPT_Dynamic_AC_CLReqControlMode_isUsed   = 1u;
        q->BPT_Scheduled_AC_CLReqControlMode_isUsed = 0u;
        q->CLReqControlMode_isUsed                  = 0u;
        q->Dynamic_AC_CLReqControlMode_isUsed       = 0u;
        q->Scheduled_AC_CLReqControlMode_isUsed     = 0u;
        struct iso20_ac_BPT_Dynamic_AC_CLReqControlModeType* m = &q->BPT_Dynamic_AC_CLReqControlMode;
        m->DepartureTime_isUsed = 0u;
        set_rational(&m->EVTargetEnergyRequest, 1, 4000);
        set_rational(&m->EVMaximumEnergyRequest, 1, 6000);
        set_rational(&m->EVMinimumEnergyRequest, 0, 0);
        set_rational(&m->EVMaximumChargePower, 0, 11000);
        m->EVMaximumChargePower_L2_isUsed = 0u;
        m->EVMaximumChargePower_L3_isUsed = 0u;
        set_rational(&m->EVMinimumChargePower, 0, 100);
        m->EVMinimumChargePower_L2_isUsed = 0u;
        m->EVMinimumChargePower_L3_isUsed = 0u;
        set_rational(&m->EVPresentActivePower, 0, 4000);
        m->EVPresentActivePower_L2_isUsed = 0u;
        m->EVPresentActivePower_L3_isUsed = 0u;
        set_rational(&m->EVPresentReactivePower, 0, 0);
        m->EVPresentReactivePower_L2_isUsed = 0u;
        m->EVPresentReactivePower_L3_isUsed = 0u;
        set_rational(&m->EVMaximumDischargePower, 0, 3700);
        m->EVMaximumDischargePower_L2_isUsed = 0u;
        m->EVMaximumDischargePower_L3_isUsed = 0u;
        set_rational(&m->EVMinimumDischargePower, 0, 100);
        m->EVMinimumDischargePower_L2_isUsed = 0u;
        m->EVMinimumDischargePower_L3_isUsed = 0u;
        m->EVMaximumV2XEnergyRequest_isUsed = 0u;
        m->EVMinimumV2XEnergyRequest_isUsed = 0u;

    } else if (strcmp(v, "AC_ChargeLoopRes_BPTDynamic") == 0) {
        doc.AC_ChargeLoopRes_isUsed = 1u;
        struct iso20_ac_AC_ChargeLoopResType* r = &doc.AC_ChargeLoopRes;
        set_header_ac(&r->Header);
        r->ResponseCode = iso20_ac_responseCodeType_OK;
        r->EVSEStatus_isUsed = 0u;
        r->MeterInfo_isUsed  = 0u;
        r->Receipt_isUsed    = 0u;
        r->EVSETargetFrequency_isUsed = 0u;
        r->BPT_Dynamic_AC_CLResControlMode_isUsed   = 1u;
        r->BPT_Scheduled_AC_CLResControlMode_isUsed = 0u;
        r->CLResControlMode_isUsed                  = 0u;
        r->Dynamic_AC_CLResControlMode_isUsed       = 0u;
        r->Scheduled_AC_CLResControlMode_isUsed     = 0u;
        struct iso20_ac_BPT_Dynamic_AC_CLResControlModeType* m = &r->BPT_Dynamic_AC_CLResControlMode;
        m->DepartureTime_isUsed = 0u;
        m->MinimumSOC_isUsed = 0u;
        m->TargetSOC_isUsed = 0u;
        m->AckMaxDelay_isUsed = 0u;
        set_rational(&m->EVSETargetActivePower, 0, 3700);
        m->EVSETargetActivePower_L2_isUsed = 0u;
        m->EVSETargetActivePower_L3_isUsed = 0u;
        m->EVSETargetReactivePower_isUsed    = 0u;
        m->EVSETargetReactivePower_L2_isUsed = 0u;
        m->EVSETargetReactivePower_L3_isUsed = 0u;
        m->EVSEPresentActivePower_isUsed    = 0u;
        m->EVSEPresentActivePower_L2_isUsed = 0u;
        m->EVSEPresentActivePower_L3_isUsed = 0u;

    } else {
        fprintf(stderr, "cbv2g-iso20: unknown AC vector '%s'\n", v);
        return 1;
    }

    uint8_t out[OUT_BUF_SIZE];
    exi_bitstream_t stream;
    exi_bitstream_init(&stream, out, sizeof(out), 0, NULL);
    int error = encode_iso20_ac_exiDocument(&stream, &doc);
    if (error != 0) {
        fprintf(stderr, "cbv2g-iso20: AC encode failed with error %d\n", error);
        return 3;
    }
    print_hex(out, exi_bitstream_get_length(&stream));
    return 0;
}

/* ---- CommonMessages EXI fragments (XMLDSig, §8.5.3) ---------------------------------------- */

/* A fixed SignedInfo (EXI-C14N, ECDSA-SHA512, one Reference URI="#ID1" over a 64-byte digest),
 * mirroring -2's set_test_signedinfo but with the -20 secp521r1/SHA-512 suite and a wider digest. */
static void set_test_signedinfo(struct iso20_SignedInfoType* s) {
    s->Id_isUsed = 0u;
    set_str(s->CanonicalizationMethod.Algorithm.characters,
            &s->CanonicalizationMethod.Algorithm.charactersLen,
            "http://www.w3.org/TR/canonical-exi/");
    s->CanonicalizationMethod.ANY_isUsed = 0u;
    set_str(s->SignatureMethod.Algorithm.characters,
            &s->SignatureMethod.Algorithm.charactersLen,
            "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha512");
    s->SignatureMethod.HMACOutputLength_isUsed = 0u;
    s->SignatureMethod.ANY_isUsed             = 0u;
    s->Reference.arrayLen = 1;
    struct iso20_ReferenceType* ref = &s->Reference.array[0];
    ref->Id_isUsed   = 0u;
    ref->Type_isUsed = 0u;
    set_str(ref->URI.characters, &ref->URI.charactersLen, "#ID1");
    ref->URI_isUsed        = 1u;
    ref->Transforms_isUsed = 0u;
    set_str(ref->DigestMethod.Algorithm.characters,
            &ref->DigestMethod.Algorithm.charactersLen,
            "http://www.w3.org/2001/04/xmlenc#sha512");
    ref->DigestMethod.ANY_isUsed = 0u;
    for (int i = 0; i < 64; i++) ref->DigestValue.bytes[i] = (uint8_t)(i + 1);
    ref->DigestValue.bytesLen = 64;
}

/* Encodes a signable CommonMessages element as a standalone EXI fragment
 * (encode_iso20_exiFragment), matching the generated EncodeFragment_<Element>. */
static int do_fragment(const char* elem) {
    struct iso20_exiFragment frag;
    memset(&frag, 0, sizeof(frag));

    if (strcmp(elem, "SignedInfo") == 0) {
        frag.SignedInfo_isUsed = 1u;
        set_test_signedinfo(&frag.SignedInfo);

    } else if (strcmp(elem, "MeteringConfirmationReq") == 0) {
        frag.MeteringConfirmationReq_isUsed = 1u;
        struct iso20_MeteringConfirmationReqType* q = &frag.MeteringConfirmationReq;
        set_header(&q->Header);
        struct iso20_SignedMeteringDataType* d = &q->SignedMeteringData;
        set_str(d->Id.characters, &d->Id.charactersLen, "ID1");
        memset(d->SessionID.bytes, 0, iso20_sessionIDType_BYTES_SIZE);
        d->SessionID.bytesLen = iso20_sessionIDType_BYTES_SIZE;
        set_str(d->MeterInfo.MeterID.characters, &d->MeterInfo.MeterID.charactersLen, "M1");
        d->MeterInfo.ChargedEnergyReadingWh               = 5000;
        d->MeterInfo.BPT_DischargedEnergyReadingWh_isUsed = 0u;
        d->MeterInfo.CapacitiveEnergyReadingVARh_isUsed   = 0u;
        d->MeterInfo.BPT_InductiveEnergyReadingVARh_isUsed = 0u;
        d->Receipt_isUsed = 0u;
        d->Dynamic_SMDTControlMode_isUsed  = 0u;
        d->Scheduled_SMDTControlMode_isUsed = 1u;
        d->Scheduled_SMDTControlMode.SelectedScheduleTupleID = 1;

    } else if (strcmp(elem, "CertificateInstallationReq") == 0) {
        frag.CertificateInstallationReq_isUsed = 1u;
        struct iso20_CertificateInstallationReqType* q = &frag.CertificateInstallationReq;
        set_header(&q->Header);
        set_str(q->OEMProvisioningCertificateChain.Id.characters,
                &q->OEMProvisioningCertificateChain.Id.charactersLen, "OEMCERT1");
        q->OEMProvisioningCertificateChain.Certificate.bytesLen = 3;
        q->OEMProvisioningCertificateChain.Certificate.bytes[0] = 0xAA;
        q->OEMProvisioningCertificateChain.Certificate.bytes[1] = 0xBB;
        q->OEMProvisioningCertificateChain.Certificate.bytes[2] = 0xCC;
        q->OEMProvisioningCertificateChain.SubCertificates_isUsed = 0u;
        q->ListOfRootCertificateIDs.RootCertificateID.arrayLen = 1;
        set_str(q->ListOfRootCertificateIDs.RootCertificateID.array[0].X509IssuerName.characters,
                &q->ListOfRootCertificateIDs.RootCertificateID.array[0].X509IssuerName.charactersLen, "Root CA");
        exi_basetypes_convert_64_to_signed(&q->ListOfRootCertificateIDs.RootCertificateID.array[0].X509SerialNumber, 12345);
        q->MaximumContractCertificateChains = 3;
        q->PrioritizedEMAIDs_isUsed = 0u;

    } else if (strcmp(elem, "PnC_AReqAuthorizationMode") == 0) {
        frag.PnC_AReqAuthorizationMode_isUsed = 1u;
        struct iso20_PnC_AReqAuthorizationModeType* m = &frag.PnC_AReqAuthorizationMode;
        set_str(m->Id.characters, &m->Id.charactersLen, "ID1");
        for (int i = 0; i < 16; i++) m->GenChallenge.bytes[i] = (uint8_t)(i + 1);
        m->GenChallenge.bytesLen = 16;
        m->ContractCertificateChain.Certificate.bytesLen = 2;
        m->ContractCertificateChain.Certificate.bytes[0] = 0x03;
        m->ContractCertificateChain.Certificate.bytes[1] = 0x04;
        m->ContractCertificateChain.SubCertificates.Certificate.arrayLen = 1;
        m->ContractCertificateChain.SubCertificates.Certificate.array[0].bytesLen = 1;
        m->ContractCertificateChain.SubCertificates.Certificate.array[0].bytes[0] = 0x05;

    } else if (strcmp(elem, "SignedInstallationData") == 0) {
        frag.SignedInstallationData_isUsed = 1u;
        struct iso20_SignedInstallationDataType* d = &frag.SignedInstallationData;
        set_str(d->Id.characters, &d->Id.charactersLen, "SID1");
        d->ContractCertificateChain.Certificate.bytesLen = 2;
        d->ContractCertificateChain.Certificate.bytes[0] = 0x03;
        d->ContractCertificateChain.Certificate.bytes[1] = 0x04;
        d->ContractCertificateChain.SubCertificates.Certificate.arrayLen = 1;
        d->ContractCertificateChain.SubCertificates.Certificate.array[0].bytesLen = 1;
        d->ContractCertificateChain.SubCertificates.Certificate.array[0].bytes[0] = 0x05;
        d->ECDHCurve = iso20_ecdhCurveType_SECP521;
        d->DHPublicKey.bytesLen = 2;
        d->DHPublicKey.bytes[0] = 0x06;
        d->DHPublicKey.bytes[1] = 0x07;
        d->SECP521_EncryptedPrivateKey_isUsed = 1u;
        d->SECP521_EncryptedPrivateKey.bytesLen = 2;
        d->SECP521_EncryptedPrivateKey.bytes[0] = 0x08;
        d->SECP521_EncryptedPrivateKey.bytes[1] = 0x09;
        d->X448_EncryptedPrivateKey_isUsed = 0u;
        d->TPM_EncryptedPrivateKey_isUsed  = 0u;

    } else if (strcmp(elem, "AbsolutePriceSchedule") == 0) {
        frag.AbsolutePriceSchedule_isUsed = 1u;
        struct iso20_AbsolutePriceScheduleType* p = &frag.AbsolutePriceSchedule;
        p->Id_isUsed = 0u;
        p->TimeAnchor      = 1700000000ULL;
        p->PriceScheduleID = 1;
        p->PriceScheduleDescription_isUsed = 0u;
        set_str(p->Currency.characters, &p->Currency.charactersLen, "EUR");
        set_str(p->Language.characters, &p->Language.charactersLen, "EN");
        set_str(p->PriceAlgorithm.characters, &p->PriceAlgorithm.charactersLen, "Alg1");
        p->MinimumCost_isUsed = 0u;
        p->MaximumCost_isUsed = 0u;
        p->TaxRules_isUsed    = 0u;
        p->PriceRuleStacks.PriceRuleStack.arrayLen = 1;
        struct iso20_PriceRuleStackType* stack = &p->PriceRuleStacks.PriceRuleStack.array[0];
        stack->Duration = 3600;
        stack->PriceRule.arrayLen = 1;
        struct iso20_PriceRuleType* rule = &stack->PriceRule.array[0];
        rule->EnergyFee.Exponent = 0;
        rule->EnergyFee.Value    = 30;
        rule->ParkingFee_isUsed  = 0u;
        rule->ParkingFeePeriod_isUsed = 0u;
        rule->CarbonDioxideEmission_isUsed = 0u;
        rule->RenewableGenerationPercentage_isUsed = 0u;
        rule->PowerRangeStart.Exponent = 0;
        rule->PowerRangeStart.Value    = 0;
        p->OverstayRules_isUsed = 0u;
        p->AdditionalSelectedServices_isUsed = 0u;

    } else {
        fprintf(stderr, "cbv2g-iso20: unknown fragment element '%s'\n", elem);
        return 1;
    }

    uint8_t out[OUT_BUF_SIZE];
    exi_bitstream_t stream;
    exi_bitstream_init(&stream, out, sizeof(out), 0, NULL);
    int error = encode_iso20_exiFragment(&stream, &frag);
    if (error != 0) {
        fprintf(stderr, "cbv2g-iso20: fragment encode failed with error %d\n", error);
        return 3;
    }
    print_hex(out, exi_bitstream_get_length(&stream));
    return 0;
}

/* DC's iso20_dc_exiFragment carries exactly DC_ChargeParameterDiscoveryRes + SignedInfo
 * (include/cbv2g/iso_20/iso20_DC_Datatypes.h) — the CLI strips the "DC_" set prefix before
 * calling this, so `elem` is "ChargeParameterDiscoveryRes" or "SignedInfo". */
static int do_fragment_dc(const char* elem) {
    struct iso20_dc_exiFragment frag;
    memset(&frag, 0, sizeof(frag));

    if (strcmp(elem, "SignedInfo") == 0) {
        frag.SignedInfo_isUsed = 1u;
        struct iso20_dc_SignedInfoType* s = &frag.SignedInfo;
        s->Id_isUsed = 0u;
        set_str(s->CanonicalizationMethod.Algorithm.characters,
                &s->CanonicalizationMethod.Algorithm.charactersLen,
                "http://www.w3.org/TR/canonical-exi/");
        s->CanonicalizationMethod.ANY_isUsed = 0u;
        set_str(s->SignatureMethod.Algorithm.characters,
                &s->SignatureMethod.Algorithm.charactersLen,
                "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha512");
        s->SignatureMethod.HMACOutputLength_isUsed = 0u;
        s->SignatureMethod.ANY_isUsed             = 0u;
        s->Reference.arrayLen = 1;
        struct iso20_dc_ReferenceType* ref = &s->Reference.array[0];
        ref->Id_isUsed   = 0u;
        ref->Type_isUsed = 0u;
        set_str(ref->URI.characters, &ref->URI.charactersLen, "#ID1");
        ref->URI_isUsed        = 1u;
        ref->Transforms_isUsed = 0u;
        set_str(ref->DigestMethod.Algorithm.characters,
                &ref->DigestMethod.Algorithm.charactersLen,
                "http://www.w3.org/2001/04/xmlenc#sha512");
        ref->DigestMethod.ANY_isUsed = 0u;
        for (int i = 0; i < 64; i++) ref->DigestValue.bytes[i] = (uint8_t)(i + 1);
        ref->DigestValue.bytesLen = 64;

    } else if (strcmp(elem, "ChargeParameterDiscoveryRes") == 0) {
        frag.DC_ChargeParameterDiscoveryRes_isUsed = 1u;
        struct iso20_dc_DC_ChargeParameterDiscoveryResType* r = &frag.DC_ChargeParameterDiscoveryRes;
        set_header_dc(&r->Header);
        r->ResponseCode = iso20_dc_responseCodeType_OK;
        r->BPT_DC_CPDResEnergyTransferMode_isUsed = 0u;
        r->DC_CPDResEnergyTransferMode_isUsed      = 1u;
        struct iso20_dc_DC_CPDResEnergyTransferModeType* m = &r->DC_CPDResEnergyTransferMode;
        set_rational_dc(&m->EVSEMaximumChargePower,  1, 15000);
        set_rational_dc(&m->EVSEMinimumChargePower,  0, 100);
        set_rational_dc(&m->EVSEMaximumChargeCurrent, 0, 200);
        set_rational_dc(&m->EVSEMinimumChargeCurrent, 0, 1);
        set_rational_dc(&m->EVSEMaximumVoltage,      0, 500);
        set_rational_dc(&m->EVSEMinimumVoltage,      0, 200);
        m->EVSEPowerRampLimitation_isUsed = 0u;

    } else {
        fprintf(stderr, "cbv2g-iso20: unknown DC fragment element '%s'\n", elem);
        return 1;
    }

    uint8_t out[OUT_BUF_SIZE];
    exi_bitstream_t stream;
    exi_bitstream_init(&stream, out, sizeof(out), 0, NULL);
    int error = encode_iso20_dc_exiFragment(&stream, &frag);
    if (error != 0) {
        fprintf(stderr, "cbv2g-iso20: DC fragment encode failed with error %d\n", error);
        return 3;
    }
    print_hex(out, exi_bitstream_get_length(&stream));
    return 0;
}

/* AC's iso20_ac_exiFragment carries exactly AC_ChargeParameterDiscoveryRes + SignedInfo
 * (include/cbv2g/iso_20/iso20_AC_Datatypes.h). */
static int do_fragment_ac(const char* elem) {
    struct iso20_ac_exiFragment frag;
    memset(&frag, 0, sizeof(frag));

    if (strcmp(elem, "SignedInfo") == 0) {
        frag.SignedInfo_isUsed = 1u;
        struct iso20_ac_SignedInfoType* s = &frag.SignedInfo;
        s->Id_isUsed = 0u;
        set_str(s->CanonicalizationMethod.Algorithm.characters,
                &s->CanonicalizationMethod.Algorithm.charactersLen,
                "http://www.w3.org/TR/canonical-exi/");
        s->CanonicalizationMethod.ANY_isUsed = 0u;
        set_str(s->SignatureMethod.Algorithm.characters,
                &s->SignatureMethod.Algorithm.charactersLen,
                "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha512");
        s->SignatureMethod.HMACOutputLength_isUsed = 0u;
        s->SignatureMethod.ANY_isUsed             = 0u;
        s->Reference.arrayLen = 1;
        struct iso20_ac_ReferenceType* ref = &s->Reference.array[0];
        ref->Id_isUsed   = 0u;
        ref->Type_isUsed = 0u;
        set_str(ref->URI.characters, &ref->URI.charactersLen, "#ID1");
        ref->URI_isUsed        = 1u;
        ref->Transforms_isUsed = 0u;
        set_str(ref->DigestMethod.Algorithm.characters,
                &ref->DigestMethod.Algorithm.charactersLen,
                "http://www.w3.org/2001/04/xmlenc#sha512");
        ref->DigestMethod.ANY_isUsed = 0u;
        for (int i = 0; i < 64; i++) ref->DigestValue.bytes[i] = (uint8_t)(i + 1);
        ref->DigestValue.bytesLen = 64;

    } else if (strcmp(elem, "ChargeParameterDiscoveryRes") == 0) {
        frag.AC_ChargeParameterDiscoveryRes_isUsed = 1u;
        struct iso20_ac_AC_ChargeParameterDiscoveryResType* r = &frag.AC_ChargeParameterDiscoveryRes;
        set_header_ac(&r->Header);
        r->ResponseCode = iso20_ac_responseCodeType_OK;
        r->AC_CPDResEnergyTransferMode_isUsed     = 1u;
        r->BPT_AC_CPDResEnergyTransferMode_isUsed = 0u;
        struct iso20_ac_AC_CPDResEnergyTransferModeType* m = &r->AC_CPDResEnergyTransferMode;
        set_rational(&m->EVSEMaximumChargePower, 0, 22000);
        m->EVSEMaximumChargePower_L2_isUsed = 0u;
        m->EVSEMaximumChargePower_L3_isUsed = 0u;
        set_rational(&m->EVSEMinimumChargePower, 0, 100);
        m->EVSEMinimumChargePower_L2_isUsed = 0u;
        m->EVSEMinimumChargePower_L3_isUsed = 0u;
        set_rational(&m->EVSENominalFrequency, 0, 50);
        m->MaximumPowerAsymmetry_isUsed    = 0u;
        m->EVSEPowerRampLimitation_isUsed  = 0u;
        m->EVSEPresentActivePower_isUsed    = 0u;
        m->EVSEPresentActivePower_L2_isUsed = 0u;
        m->EVSEPresentActivePower_L3_isUsed = 0u;

    } else {
        fprintf(stderr, "cbv2g-iso20: unknown AC fragment element '%s'\n", elem);
        return 1;
    }

    uint8_t out[OUT_BUF_SIZE];
    exi_bitstream_t stream;
    exi_bitstream_init(&stream, out, sizeof(out), 0, NULL);
    int error = encode_iso20_ac_exiFragment(&stream, &frag);
    if (error != 0) {
        fprintf(stderr, "cbv2g-iso20: AC fragment encode failed with error %d\n", error);
        return 3;
    }
    print_hex(out, exi_bitstream_get_length(&stream));
    return 0;
}

/* ---- WPT ------------------------------------------------------------------------------------
 * Baseline coverage only: VendorSpecificDataContainer/ManufacturerSpecificDataContainer empty,
 * WPT_LF_DataPackageList and LF_SystemSetupData absent. Those two fields touch grammar shapes this
 * repo's generator had to design independently (no working cbV2G reference — see
 * docs/xsd-inventory-15118-20.md) and are covered by self-consistency roundtrip tests instead. */

static void set_rational_wpt(struct iso20_wpt_RationalNumberType* r, int8_t exponent, int16_t value) {
    r->Exponent = exponent;
    r->Value = value;
}

static int do_wpt(const char* v) {
    struct iso20_wpt_exiDocument doc;
    memset(&doc, 0, sizeof(doc));

    if (strcmp(v, "WPT_FinePositioningSetupReq") == 0) {
        doc.WPT_FinePositioningSetupReq_isUsed = 1u;
        struct iso20_wpt_WPT_FinePositioningSetupReqType* q = &doc.WPT_FinePositioningSetupReq;
        set_header_wpt(&q->Header);
        q->EVProcessing = iso20_wpt_processingType_Finished;
        q->EVDeviceFinePositioningMethodList.WPT_FinePositioningMethod.arrayLen = 1;
        q->EVDeviceFinePositioningMethodList.WPT_FinePositioningMethod.array[0] = iso20_wpt_WPT_FinePositioningMethodType_Manual;
        q->EVDevicePairingMethodList.WPT_PairingMethod.arrayLen = 1;
        q->EVDevicePairingMethodList.WPT_PairingMethod.array[0] = iso20_wpt_WPT_PairingMethodType_LPE;
        q->EVDeviceAlignmentCheckMethodList.WPT_AlignmentCheckMethod.arrayLen = 1;
        q->EVDeviceAlignmentCheckMethodList.WPT_AlignmentCheckMethod.array[0] = iso20_wpt_WPT_AlignmentCheckMethodType_PowerCheck;
        q->NaturalOffset = 0;
        q->VendorSpecificDataContainer.arrayLen = 0;
        q->LF_SystemSetupData_isUsed = 0u;

    } else if (strcmp(v, "WPT_FinePositioningSetupRes") == 0) {
        doc.WPT_FinePositioningSetupRes_isUsed = 1u;
        struct iso20_wpt_WPT_FinePositioningSetupResType* r = &doc.WPT_FinePositioningSetupRes;
        set_header_wpt(&r->Header);
        r->ResponseCode = iso20_wpt_responseCodeType_OK;
        r->PrimaryDeviceFinePositioningMethodList.WPT_FinePositioningMethod.arrayLen = 1;
        r->PrimaryDeviceFinePositioningMethodList.WPT_FinePositioningMethod.array[0] = iso20_wpt_WPT_FinePositioningMethodType_Manual;
        r->PrimaryDevicePairingMethodList.WPT_PairingMethod.arrayLen = 1;
        r->PrimaryDevicePairingMethodList.WPT_PairingMethod.array[0] = iso20_wpt_WPT_PairingMethodType_LPE;
        r->PrimaryDeviceAlignmentCheckMethodList.WPT_AlignmentCheckMethod.arrayLen = 1;
        r->PrimaryDeviceAlignmentCheckMethodList.WPT_AlignmentCheckMethod.array[0] = iso20_wpt_WPT_AlignmentCheckMethodType_PowerCheck;
        r->NaturalOffset = 0;
        r->VendorSpecificDataContainer.arrayLen = 0;
        r->LF_SystemSetupData_isUsed = 0u;

    } else if (strcmp(v, "WPT_FinePositioningReq") == 0) {
        doc.WPT_FinePositioningReq_isUsed = 1u;
        struct iso20_wpt_WPT_FinePositioningReqType* q = &doc.WPT_FinePositioningReq;
        set_header_wpt(&q->Header);
        q->EVProcessing = iso20_wpt_processingType_Finished;
        q->EVResultCode = iso20_wpt_WPT_EVResultType_EVResultSuccess;
        q->VendorSpecificDataContainer.arrayLen = 0;
        q->VendorSpecificDataContainer_isUsed = 0u;
        q->WPT_LF_DataPackageList_isUsed = 0u;

    } else if (strcmp(v, "WPT_FinePositioningRes") == 0) {
        doc.WPT_FinePositioningRes_isUsed = 1u;
        struct iso20_wpt_WPT_FinePositioningResType* r = &doc.WPT_FinePositioningRes;
        set_header_wpt(&r->Header);
        r->ResponseCode = iso20_wpt_responseCodeType_OK;
        r->EVSEProcessing = iso20_wpt_processingType_Finished;
        r->VendorSpecificDataContainer.arrayLen = 0;
        r->VendorSpecificDataContainer_isUsed = 0u;
        r->WPT_LF_DataPackageList_isUsed = 0u;

    } else if (strcmp(v, "WPT_PairingReq") == 0) {
        doc.WPT_PairingReq_isUsed = 1u;
        struct iso20_wpt_WPT_PairingReqType* q = &doc.WPT_PairingReq;
        set_header_wpt(&q->Header);
        q->EVProcessing = iso20_wpt_processingType_Finished;
        q->ObservedIDCode_isUsed = 0u;
        q->EVResultCode = iso20_wpt_WPT_EVResultType_EVResultSuccess;
        q->VendorSpecificDataContainer.arrayLen = 0;
        q->VendorSpecificDataContainer_isUsed = 0u;

    } else if (strcmp(v, "WPT_PairingRes") == 0) {
        doc.WPT_PairingRes_isUsed = 1u;
        struct iso20_wpt_WPT_PairingResType* r = &doc.WPT_PairingRes;
        set_header_wpt(&r->Header);
        r->ResponseCode = iso20_wpt_responseCodeType_OK;
        r->EVSEProcessing = iso20_wpt_processingType_Finished;
        r->ObservedIDCode_isUsed = 0u;
        r->AlternativeSECCList_isUsed = 0u;
        r->VendorSpecificDataContainer.arrayLen = 0;
        r->VendorSpecificDataContainer_isUsed = 0u;

    } else if (strcmp(v, "WPT_ChargeParameterDiscoveryReq") == 0) {
        doc.WPT_ChargeParameterDiscoveryReq_isUsed = 1u;
        struct iso20_wpt_WPT_ChargeParameterDiscoveryReqType* q = &doc.WPT_ChargeParameterDiscoveryReq;
        set_header_wpt(&q->Header);
        set_rational_wpt(&q->EVPCMaxReceivablePower, 0, 11000);
        q->SDMaxGroundClearence = 300;
        q->SDMinGroundClearence = 100;
        set_rational_wpt(&q->EVPCNaturalFrequency, 0, 85);
        q->EVPCDeviceLocalControl = 0;
        q->VendorSpecificDataContainer.arrayLen = 0;
        q->VendorSpecificDataContainer_isUsed = 0u;

    } else if (strcmp(v, "WPT_ChargeParameterDiscoveryRes") == 0) {
        doc.WPT_ChargeParameterDiscoveryRes_isUsed = 1u;
        struct iso20_wpt_WPT_ChargeParameterDiscoveryResType* r = &doc.WPT_ChargeParameterDiscoveryRes;
        set_header_wpt(&r->Header);
        r->ResponseCode = iso20_wpt_responseCodeType_OK;
        r->PDInputPowerClass = iso20_wpt_WPT_PowerClassType_MF_WPT1;
        set_rational_wpt(&r->SDMinOutputPower, 0, 100);
        set_rational_wpt(&r->SDMaxOutputPower, 0, 11000);
        r->SDMaxGroundClearanceSupport = 300;
        r->SDMinGroundClearanceSupport = 100;
        set_rational_wpt(&r->PDMinCoilCurrent, 0, 1);
        set_rational_wpt(&r->PDMaxCoilCurrent, 0, 200);
        r->SDManufacturerSpecificDataContainer.arrayLen = 0;
        r->SDManufacturerSpecificDataContainer_isUsed = 0u;

    } else if (strcmp(v, "WPT_AlignmentCheckReq") == 0) {
        doc.WPT_AlignmentCheckReq_isUsed = 1u;
        struct iso20_wpt_WPT_AlignmentCheckReqType* q = &doc.WPT_AlignmentCheckReq;
        set_header_wpt(&q->Header);
        q->EVProcessing = iso20_wpt_processingType_Finished;
        q->TargetCoilCurrent_isUsed = 0u;
        q->EVResultCode = iso20_wpt_WPT_EVResultType_EVResultSuccess;
        q->VendorSpecificDataContainer.arrayLen = 0;
        q->VendorSpecificDataContainer_isUsed = 0u;

    } else if (strcmp(v, "WPT_AlignmentCheckRes") == 0) {
        doc.WPT_AlignmentCheckRes_isUsed = 1u;
        struct iso20_wpt_WPT_AlignmentCheckResType* r = &doc.WPT_AlignmentCheckRes;
        set_header_wpt(&r->Header);
        r->ResponseCode = iso20_wpt_responseCodeType_OK;
        r->EVSEProcessing = iso20_wpt_processingType_Finished;
        r->PowerTransmitted_isUsed = 0u;
        r->SupplyDeviceCurrent_isUsed = 0u;
        r->VendorSpecificDataContainer.arrayLen = 0;
        r->VendorSpecificDataContainer_isUsed = 0u;

    } else if (strcmp(v, "WPT_ChargeLoopReq") == 0) {
        doc.WPT_ChargeLoopReq_isUsed = 1u;
        struct iso20_wpt_WPT_ChargeLoopReqType* q = &doc.WPT_ChargeLoopReq;
        set_header_wpt(&q->Header);
        q->DisplayParameters_isUsed = 0u;
        q->MeterInfoRequested = 0;
        set_rational_wpt(&q->EVPCPowerRequest, 0, 3700);
        set_rational_wpt(&q->EVPCPowerOutput, 0, 3700);
        q->EVPCChargeDiagnostics = iso20_wpt_WPT_EVPCChargeDiagnosticsType_EVPCNoIssue;
        q->EVPCOperatingFrequency_isUsed = 0u;
        q->EVPCPowerControlParameter_isUsed = 0u;
        q->ManufacturerSpecificDataContainer.arrayLen = 0;
        q->ManufacturerSpecificDataContainer_isUsed = 0u;

    } else if (strcmp(v, "WPT_ChargeLoopRes") == 0) {
        doc.WPT_ChargeLoopRes_isUsed = 1u;
        struct iso20_wpt_WPT_ChargeLoopResType* r = &doc.WPT_ChargeLoopRes;
        set_header_wpt(&r->Header);
        r->ResponseCode = iso20_wpt_responseCodeType_OK;
        r->EVSEStatus_isUsed = 0u;
        r->MeterInfo_isUsed = 0u;
        r->Receipt_isUsed = 0u;
        set_rational_wpt(&r->EVPCPowerRequest, 0, 3700);
        r->SDPowerInput_isUsed = 0u;
        set_rational_wpt(&r->SPCMaxOutputPowerLimit, 0, 3700);
        set_rational_wpt(&r->SPCMinOutputPowerLimit, 0, 0);
        r->SPCChargeDiagnostics = iso20_wpt_WPT_SPCChargeDiagnosticsType_SPCNoIssue;
        r->SPCOperatingFrequency_isUsed = 0u;
        r->SPCPowerControlParameter_isUsed = 0u;
        r->ManufacturerSpecificDataContainer.arrayLen = 0;
        r->ManufacturerSpecificDataContainer_isUsed = 0u;

    } else {
        fprintf(stderr, "cbv2g-iso20: unknown WPT vector '%s'\n", v);
        return 1;
    }

    uint8_t out[OUT_BUF_SIZE];
    exi_bitstream_t stream;
    exi_bitstream_init(&stream, out, sizeof(out), 0, NULL);
    int error = encode_iso20_wpt_exiDocument(&stream, &doc);
    if (error != 0) {
        fprintf(stderr, "cbv2g-iso20: WPT encode failed with error %d\n", error);
        return 3;
    }
    print_hex(out, exi_bitstream_get_length(&stream));
    return 0;
}

/* ---- ACDP ------------------------------------------------------------------------------------ */

static int do_acdp(const char* v) {
    struct iso20_acdp_exiDocument doc;
    memset(&doc, 0, sizeof(doc));

    if (strcmp(v, "ACDP_VehiclePositioningReq") == 0) {
        doc.ACDP_VehiclePositioningReq_isUsed = 1u;
        struct iso20_acdp_ACDP_VehiclePositioningReqType* q = &doc.ACDP_VehiclePositioningReq;
        set_header_acdp(&q->Header);
        q->EVMobilityStatus = 1;
        q->EVPositioningSupport = 1;

    } else if (strcmp(v, "ACDP_VehiclePositioningRes") == 0) {
        doc.ACDP_VehiclePositioningRes_isUsed = 1u;
        struct iso20_acdp_ACDP_VehiclePositioningResType* r = &doc.ACDP_VehiclePositioningRes;
        set_header_acdp(&r->Header);
        r->ResponseCode = iso20_acdp_responseCodeType_OK;
        r->EVSEProcessing = iso20_acdp_processingType_Finished;
        r->EVSEPositioningSupport = 1;
        r->EVRelativeXDeviation = 10;
        r->EVRelativeYDeviation = -5;
        r->ContactWindowXc = 100;
        r->ContactWindowYc = 50;
        r->EVInChargePosition = 0;

    } else if (strcmp(v, "ACDP_ConnectReq") == 0) {
        doc.ACDP_ConnectReq_isUsed = 1u;
        struct iso20_acdp_ACDP_ConnectReqType* q = &doc.ACDP_ConnectReq;
        set_header_acdp(&q->Header);
        q->EVElectricalChargingDeviceStatus = iso20_acdp_electricalChargingDeviceStatusType_State_B;

    } else if (strcmp(v, "ACDP_ConnectRes") == 0) {
        doc.ACDP_ConnectRes_isUsed = 1u;
        struct iso20_acdp_ACDP_ConnectResType* r = &doc.ACDP_ConnectRes;
        set_header_acdp(&r->Header);
        r->ResponseCode = iso20_acdp_responseCodeType_OK;
        r->EVSEProcessing = iso20_acdp_processingType_Finished;
        r->EVSEElectricalChargingDeviceStatus = iso20_acdp_electricalChargingDeviceStatusType_State_C;
        r->EVSEMechanicalChargingDeviceStatus = iso20_acdp_mechanicalChargingDeviceStatusType_EndPosition;

    } else if (strcmp(v, "ACDP_DisconnectReq") == 0) {
        doc.ACDP_DisconnectReq_isUsed = 1u;
        struct iso20_acdp_ACDP_ConnectReqType* q = &doc.ACDP_DisconnectReq;
        set_header_acdp(&q->Header);
        q->EVElectricalChargingDeviceStatus = iso20_acdp_electricalChargingDeviceStatusType_State_A;

    } else if (strcmp(v, "ACDP_DisconnectRes") == 0) {
        doc.ACDP_DisconnectRes_isUsed = 1u;
        struct iso20_acdp_ACDP_ConnectResType* r = &doc.ACDP_DisconnectRes;
        set_header_acdp(&r->Header);
        r->ResponseCode = iso20_acdp_responseCodeType_OK;
        r->EVSEProcessing = iso20_acdp_processingType_Finished;
        r->EVSEElectricalChargingDeviceStatus = iso20_acdp_electricalChargingDeviceStatusType_State_A;
        r->EVSEMechanicalChargingDeviceStatus = iso20_acdp_mechanicalChargingDeviceStatusType_Home;

    } else if (strcmp(v, "ACDP_SystemStatusReq") == 0) {
        doc.ACDP_SystemStatusReq_isUsed = 1u;
        struct iso20_acdp_ACDP_SystemStatusReqType* q = &doc.ACDP_SystemStatusReq;
        set_header_acdp(&q->Header);
        q->EVTechnicalStatus.EVReadyToCharge = 1;
        q->EVTechnicalStatus.EVImmobilizationRequest = 0;
        q->EVTechnicalStatus.EVImmobilized_isUsed = 0u;
        q->EVTechnicalStatus.EVWLANStrength_isUsed = 0u;
        q->EVTechnicalStatus.EVCPStatus_isUsed = 0u;
        q->EVTechnicalStatus.EVSOC_isUsed = 0u;
        q->EVTechnicalStatus.EVErrorCode_isUsed = 0u;
        q->EVTechnicalStatus.EVTimeout_isUsed = 0u;

    } else if (strcmp(v, "ACDP_SystemStatusRes") == 0) {
        doc.ACDP_SystemStatusRes_isUsed = 1u;
        struct iso20_acdp_ACDP_SystemStatusResType* r = &doc.ACDP_SystemStatusRes;
        set_header_acdp(&r->Header);
        r->ResponseCode = iso20_acdp_responseCodeType_OK;
        r->EVSEMechanicalChargingDeviceStatus = iso20_acdp_mechanicalChargingDeviceStatusType_EndPosition;
        r->EVSEReadyToCharge = 1;
        r->EVSEIsolationStatus = iso20_acdp_isolationStatusType_Safe;
        r->EVSEDisabled = 0;
        r->EVSEUtilityInterruptEvent = 0;
        r->EVSEEmergencyShutdown = 0;
        r->EVSEMalfunction = 0;
        r->EVInChargePosition = 1;
        r->EVAssociationStatus = 1;

    } else {
        fprintf(stderr, "cbv2g-iso20: unknown ACDP vector '%s'\n", v);
        return 1;
    }

    uint8_t out[OUT_BUF_SIZE];
    exi_bitstream_t stream;
    exi_bitstream_init(&stream, out, sizeof(out), 0, NULL);
    int error = encode_iso20_acdp_exiDocument(&stream, &doc);
    if (error != 0) {
        fprintf(stderr, "cbv2g-iso20: ACDP encode failed with error %d\n", error);
        return 3;
    }
    print_hex(out, exi_bitstream_get_length(&stream));
    return 0;
}

int main(int argc, char** argv) {
    if (argc != 2) {
        fprintf(stderr, "usage: %s <Set>_<vector>  (Set: Common, DC, AC)\n", argv[0]);
        return 1;
    }
    const char* arg = argv[1];

    if (strncmp(arg, "Fragment_", 9) == 0) {
        const char* elem = arg + 9;
        if (strncmp(elem, "DC_", 3) == 0) return do_fragment_dc(elem + 3);
        if (strncmp(elem, "AC_", 3) == 0) return do_fragment_ac(elem + 3);
        return do_fragment(elem);
    }
    if (strncmp(arg, "Common_", 7) == 0) return do_common(arg + 7);
    if (strncmp(arg, "DC_", 3) == 0)     return do_dc(arg);       /* DC vector names already start with DC_ */
    if (strncmp(arg, "AC_", 3) == 0)     return do_ac(arg);       /* AC vector names already start with AC_ */
    if (strncmp(arg, "WPT_", 4) == 0)    return do_wpt(arg);      /* WPT vector names already start with WPT_ */
    if (strncmp(arg, "ACDP_", 5) == 0)   return do_acdp(arg);     /* ACDP vector names already start with ACDP_ */

    fprintf(stderr, "cbv2g-iso20: vector name must be prefixed Common_/DC_/AC_/WPT_/ACDP_\n");
    return 1;
}
