/* SPDX-License-Identifier: Apache-2.0 */
/*
 * cbv2g-iso2 — reference EXI encoder for a fixed set of ISO 15118-2 test messages,
 * built on EVerest's libcbv2g. It emits wire-conformant EXI hex (including the 0x80
 * header, excluding the V2GTP header) for named vectors that mirror the C# round-trip
 * fixtures, so the generated codec can be diffed against it.
 *
 * Development tool only; `dotnet test` never runs it. Regenerated hex is checked into
 * Vanaheimr.V2G.Exi.Tests/Vectors/Iso15118_2.vectors.json.
 *
 * Usage:  cbv2g_iso2 <VectorName>   ->  space-separated lowercase hex on stdout.
 */

#include <stdio.h>
#include <string.h>
#include <stdint.h>

#include "cbv2g/common/exi_bitstream.h"
#include "cbv2g/common/exi_basetypes.h"
#include "cbv2g/iso_2/iso2_msgDefDatatypes.h"
#include "cbv2g/iso_2/iso2_msgDefEncoder.h"

#define OUT_BUF_SIZE 4096

/* Every vector uses an all-zero 8-byte SessionID and an otherwise empty header. */
static void set_header(struct iso2_MessageHeaderType* h) {
    memset(h->SessionID.bytes, 0, iso2_sessionIDType_BYTES_SIZE);
    h->SessionID.bytesLen  = iso2_sessionIDType_BYTES_SIZE;
    h->Notification_isUsed = 0u;
    h->Signature_isUsed    = 0u;
}

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

/* Shared sub-structs, filled with the same values as the C# fixtures. */
static void set_pv(struct iso2_PhysicalValueType* p, int8_t mult, iso2_unitSymbolType unit, int16_t val) {
    p->Multiplier = mult;
    p->Unit       = unit;
    p->Value      = val;
}
static void set_dc_evstatus(struct iso2_DC_EVStatusType* s) {
    s->EVReady    = 1;
    s->EVErrorCode = iso2_DC_EVErrorCodeType_NO_ERROR;
    s->EVRESSSOC  = 50;
}
static void set_dc_evsestatus(struct iso2_DC_EVSEStatusType* s) {
    s->NotificationMaxDelay      = 0;
    s->EVSENotification          = iso2_EVSENotificationType_None;
    s->EVSEIsolationStatus_isUsed = 0u;
    s->EVSEStatusCode            = iso2_DC_EVSEStatusCodeType_EVSE_Ready;
}
static void set_ac_evsestatus(struct iso2_AC_EVSEStatusType* s) {
    s->NotificationMaxDelay = 0;
    s->EVSENotification     = iso2_EVSENotificationType_None;
    s->RCD                  = 0;
}
/* A ListOfRootCertificateIDs with a single X509 root cert id (CN=Root CA, serial 12345). */
static void set_rootcerts(struct iso2_ListOfRootCertificateIDsType* l) {
    l->RootCertificateID.arrayLen = 1;
    struct iso2_X509IssuerSerialType* x = &l->RootCertificateID.array[0];
    set_str(x->X509IssuerName.characters, &x->X509IssuerName.charactersLen, "CN=Root CA");
    /* exi_signed_t data.octets are RAW big-endian value bytes; the encoder re-groups them into EXI
     * 7-bit octets. (The convert_to_* helpers pre-flag and must NOT be used here.) 12345 = 0x3039. */
    x->X509SerialNumber.is_negative = 0u;
    x->X509SerialNumber.data.octets[0] = 0x30;
    x->X509SerialNumber.data.octets[1] = 0x39;
    x->X509SerialNumber.data.octets_count = 2;
}
/* A certificate chain: Certificate {30 82 01 02}, and optionally one SubCertificate {30 82 03 04}.
 * The chain's own Id attribute stays absent. */
static void set_certchain(struct iso2_CertificateChainType* c, int with_sub) {
    static const uint8_t cert[4] = { 0x30, 0x82, 0x01, 0x02 };
    c->Id_isUsed = 0u;
    memcpy(c->Certificate.bytes, cert, 4);
    c->Certificate.bytesLen = 4;
    if (with_sub) {
        static const uint8_t sub[4] = { 0x30, 0x82, 0x03, 0x04 };
        c->SubCertificates_isUsed = 1u;
        c->SubCertificates.Certificate.arrayLen = 1;
        memcpy(c->SubCertificates.Certificate.array[0].bytes, sub, 4);
        c->SubCertificates.Certificate.array[0].bytesLen = 4;
    } else {
        c->SubCertificates_isUsed = 0u;
    }
}
/* The shared tail of both certificate Res messages: encrypted private key, DH public key and eMAID
 * (each with a fixed Id and fixed content). */
static void set_encprivkey(struct iso2_ContractSignatureEncryptedPrivateKeyType* k) {
    set_str(k->Id.characters, &k->Id.charactersLen, "ID2");
    for (int i = 0; i < 16; i++) k->CONTENT.bytes[i] = (uint8_t)(0xA0 + i);
    k->CONTENT.bytesLen = 16;
}
static void set_dhkey(struct iso2_DiffieHellmanPublickeyType* d) {
    set_str(d->Id.characters, &d->Id.charactersLen, "ID3");
    for (int i = 0; i < 8; i++) d->CONTENT.bytes[i] = (uint8_t)(0xB0 + i);
    d->CONTENT.bytesLen = 8;
}
static void set_emaid_ct(struct iso2_EMAIDType* e) {
    set_str(e->Id.characters, &e->Id.charactersLen, "ID4");
    set_str(e->CONTENT.characters, &e->CONTENT.charactersLen, "DEAAA0001234567");
}

/* A fixed SignedInfo (EXI-C14N, ECDSA-SHA256, one Reference URI="#ID1" over a 32-byte digest),
 * mirroring the C# V2GSignature fixtures. */
static void set_test_signedinfo(struct iso2_SignedInfoType* s) {
    s->Id_isUsed = 0u;
    set_str(s->CanonicalizationMethod.Algorithm.characters,
            &s->CanonicalizationMethod.Algorithm.charactersLen,
            "http://www.w3.org/TR/canonical-exi/");
    s->CanonicalizationMethod.ANY_isUsed = 0u;
    set_str(s->SignatureMethod.Algorithm.characters,
            &s->SignatureMethod.Algorithm.charactersLen,
            "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha256");
    s->SignatureMethod.HMACOutputLength_isUsed = 0u;
    s->SignatureMethod.ANY_isUsed             = 0u;
    s->Reference.arrayLen = 1;
    struct iso2_ReferenceType* ref = &s->Reference.array[0];
    ref->Id_isUsed   = 0u;
    ref->Type_isUsed = 0u;
    set_str(ref->URI.characters, &ref->URI.charactersLen, "#ID1");
    ref->URI_isUsed        = 1u;
    ref->Transforms_isUsed = 0u;
    set_str(ref->DigestMethod.Algorithm.characters,
            &ref->DigestMethod.Algorithm.charactersLen,
            "http://www.w3.org/2001/04/xmlenc#sha256");
    ref->DigestMethod.ANY_isUsed = 0u;
    for (int i = 0; i < 32; i++) ref->DigestValue.bytes[i] = (uint8_t)(i + 1);
    ref->DigestValue.bytesLen = 32;
}

/* Encodes a signable element as a standalone EXI fragment (encode_iso2_exiFragment), matching the
 * generated EncodeFragment_<Element>. Content mirrors the body fixtures. */
static int do_fragment(const char* elem) {
    struct iso2_exiFragment frag;
    memset(&frag, 0, sizeof(frag));

    if (strcmp(elem, "AuthorizationReq") == 0) {
        frag.AuthorizationReq_isUsed = 1u;
        frag.AuthorizationReq.Id_isUsed = 0u;
        for (int i = 0; i < 16; i++) frag.AuthorizationReq.GenChallenge.bytes[i] = (uint8_t)(i + 1);
        frag.AuthorizationReq.GenChallenge.bytesLen = 16;
        frag.AuthorizationReq.GenChallenge_isUsed   = 1u;

    } else if (strcmp(elem, "MeteringReceiptReq") == 0) {
        frag.MeteringReceiptReq_isUsed = 1u;
        struct iso2_MeteringReceiptReqType* q = &frag.MeteringReceiptReq;
        q->Id_isUsed = 0u;
        memset(q->SessionID.bytes, 0, iso2_sessionIDType_BYTES_SIZE);
        q->SessionID.bytesLen = iso2_sessionIDType_BYTES_SIZE;
        q->SAScheduleTupleID        = 1;
        q->SAScheduleTupleID_isUsed = 1u;
        set_str(q->MeterInfo.MeterID.characters, &q->MeterInfo.MeterID.charactersLen, "M1");
        q->MeterInfo.MeterReading_isUsed    = 0u;
        q->MeterInfo.SigMeterReading_isUsed = 0u;
        q->MeterInfo.MeterStatus_isUsed     = 0u;
        q->MeterInfo.TMeter_isUsed          = 0u;

    } else if (strcmp(elem, "SalesTariff") == 0) {
        frag.SalesTariff_isUsed = 1u;
        struct iso2_SalesTariffType* t = &frag.SalesTariff;
        t->Id_isUsed = 0u;
        t->SalesTariffID = 1;
        t->SalesTariffDescription_isUsed = 0u;
        t->NumEPriceLevels_isUsed        = 0u;
        t->SalesTariffEntry.arrayLen = 1;
        struct iso2_SalesTariffEntryType* e = &t->SalesTariffEntry.array[0];
        e->RelativeTimeInterval_isUsed = 1u;
        e->RelativeTimeInterval.start          = 0;
        e->RelativeTimeInterval.duration_isUsed = 0u;
        e->TimeInterval_isUsed  = 0u;
        e->EPriceLevel_isUsed   = 0u;
        e->ConsumptionCost.arrayLen = 0;

    } else if (strcmp(elem, "SignedInfo") == 0) {
        frag.SignedInfo_isUsed = 1u;
        set_test_signedinfo(&frag.SignedInfo);

    } else {
        fprintf(stderr, "cbv2g-iso2: unknown fragment element '%s'\n", elem);
        return 1;
    }

    uint8_t out[OUT_BUF_SIZE];
    exi_bitstream_t stream;
    exi_bitstream_init(&stream, out, sizeof(out), 0, NULL);
    int error = encode_iso2_exiFragment(&stream, &frag);
    if (error != 0) {
        fprintf(stderr, "cbv2g-iso2: fragment encode failed with error %d\n", error);
        return 3;
    }
    print_hex(out, exi_bitstream_get_length(&stream));
    return 0;
}

int main(int argc, char** argv) {
    if (argc != 2) {
        fprintf(stderr, "usage: %s <vector>\n", argv[0]);
        return 1;
    }
    const char* v = argv[1];

    if (strncmp(v, "Fragment_", 9) == 0)
        return do_fragment(v + 9);

    struct iso2_exiDocument doc;
    init_iso2_exiDocument(&doc);
    set_header(&doc.V2G_Message.Header);
    struct iso2_BodyType* body = &doc.V2G_Message.Body;

    if (strcmp(v, "SessionSetupReq") == 0) {
        body->SessionSetupReq_isUsed = 1u;
        static const uint8_t evcc[6] = { 0xAB, 0xCD, 0xEF, 0x01, 0x02, 0x03 };
        memcpy(body->SessionSetupReq.EVCCID.bytes, evcc, sizeof(evcc));
        body->SessionSetupReq.EVCCID.bytesLen = (uint16_t)sizeof(evcc);

    } else if (strcmp(v, "SessionSetupRes_ts") == 0) {
        body->SessionSetupRes_isUsed = 1u;
        body->SessionSetupRes.ResponseCode = iso2_responseCodeType_OK_NewSessionEstablished;
        set_str(body->SessionSetupRes.EVSEID.characters, &body->SessionSetupRes.EVSEID.charactersLen, "DE*ABC*E12345*1");
        body->SessionSetupRes.EVSETimeStamp        = 1600000000;
        body->SessionSetupRes.EVSETimeStamp_isUsed = 1u;

    } else if (strcmp(v, "SessionSetupRes_nots") == 0) {
        body->SessionSetupRes_isUsed = 1u;
        body->SessionSetupRes.ResponseCode = iso2_responseCodeType_OK;
        set_str(body->SessionSetupRes.EVSEID.characters, &body->SessionSetupRes.EVSEID.charactersLen, "EVSE1");
        body->SessionSetupRes.EVSETimeStamp_isUsed = 0u;

    } else if (strcmp(v, "ServiceDiscoveryReq_absent") == 0) {
        body->ServiceDiscoveryReq_isUsed = 1u;
        body->ServiceDiscoveryReq.ServiceScope_isUsed    = 0u;
        body->ServiceDiscoveryReq.ServiceCategory_isUsed = 0u;

    } else if (strcmp(v, "ServiceDiscoveryReq_present") == 0) {
        body->ServiceDiscoveryReq_isUsed = 1u;
        set_str(body->ServiceDiscoveryReq.ServiceScope.characters, &body->ServiceDiscoveryReq.ServiceScope.charactersLen, "urn:scope:test");
        body->ServiceDiscoveryReq.ServiceScope_isUsed    = 1u;
        body->ServiceDiscoveryReq.ServiceCategory        = iso2_serviceCategoryType_EVCharging;
        body->ServiceDiscoveryReq.ServiceCategory_isUsed = 1u;

    } else if (strcmp(v, "ServiceDiscoveryRes") == 0) {
        body->ServiceDiscoveryRes_isUsed = 1u;
        struct iso2_ServiceDiscoveryResType* r = &body->ServiceDiscoveryRes;
        r->ResponseCode = iso2_responseCodeType_OK;
        r->PaymentOptionList.PaymentOption.arrayLen  = 2;
        r->PaymentOptionList.PaymentOption.array[0]  = iso2_paymentOptionType_Contract;
        r->PaymentOptionList.PaymentOption.array[1]  = iso2_paymentOptionType_ExternalPayment;
        r->ChargeService.ServiceID = 1;
        set_str(r->ChargeService.ServiceName.characters, &r->ChargeService.ServiceName.charactersLen, "AC");
        r->ChargeService.ServiceName_isUsed  = 1u;
        r->ChargeService.ServiceCategory     = iso2_serviceCategoryType_EVCharging;
        r->ChargeService.ServiceScope_isUsed = 0u;
        r->ChargeService.FreeService         = 1; /* true */
        r->ChargeService.SupportedEnergyTransferMode.EnergyTransferMode.arrayLen = 2;
        r->ChargeService.SupportedEnergyTransferMode.EnergyTransferMode.array[0] = iso2_EnergyTransferModeType_AC_single_phase_core;
        r->ChargeService.SupportedEnergyTransferMode.EnergyTransferMode.array[1] = iso2_EnergyTransferModeType_AC_three_phase_core;
        r->ServiceList_isUsed = 0u;

    } else if (strcmp(v, "SessionStopReq") == 0) {
        body->SessionStopReq_isUsed = 1u;
        body->SessionStopReq.ChargingSession = iso2_chargingSessionType_Terminate;

    } else if (strcmp(v, "SessionStopRes") == 0) {
        body->SessionStopRes_isUsed = 1u;
        body->SessionStopRes.ResponseCode = iso2_responseCodeType_OK;

    } else if (strcmp(v, "CableCheckReq") == 0) {
        body->CableCheckReq_isUsed = 1u;
        set_dc_evstatus(&body->CableCheckReq.DC_EVStatus);

    } else if (strcmp(v, "CableCheckRes") == 0) {
        body->CableCheckRes_isUsed = 1u;
        body->CableCheckRes.ResponseCode = iso2_responseCodeType_OK;
        set_dc_evsestatus(&body->CableCheckRes.DC_EVSEStatus);
        body->CableCheckRes.EVSEProcessing = iso2_EVSEProcessingType_Ongoing;

    } else if (strcmp(v, "PreChargeReq") == 0) {
        body->PreChargeReq_isUsed = 1u;
        set_dc_evstatus(&body->PreChargeReq.DC_EVStatus);
        set_pv(&body->PreChargeReq.EVTargetVoltage, 0, iso2_unitSymbolType_V, 400);
        set_pv(&body->PreChargeReq.EVTargetCurrent, 0, iso2_unitSymbolType_A, 10);

    } else if (strcmp(v, "PreChargeRes") == 0) {
        body->PreChargeRes_isUsed = 1u;
        body->PreChargeRes.ResponseCode = iso2_responseCodeType_OK;
        set_dc_evsestatus(&body->PreChargeRes.DC_EVSEStatus);
        set_pv(&body->PreChargeRes.EVSEPresentVoltage, 0, iso2_unitSymbolType_V, 395);

    } else if (strcmp(v, "WeldingDetectionReq") == 0) {
        body->WeldingDetectionReq_isUsed = 1u;
        set_dc_evstatus(&body->WeldingDetectionReq.DC_EVStatus);

    } else if (strcmp(v, "WeldingDetectionRes") == 0) {
        body->WeldingDetectionRes_isUsed = 1u;
        body->WeldingDetectionRes.ResponseCode = iso2_responseCodeType_OK;
        set_dc_evsestatus(&body->WeldingDetectionRes.DC_EVSEStatus);
        set_pv(&body->WeldingDetectionRes.EVSEPresentVoltage, 0, iso2_unitSymbolType_V, 400);

    } else if (strcmp(v, "PowerDeliveryReq") == 0) {
        body->PowerDeliveryReq_isUsed = 1u;
        body->PowerDeliveryReq.ChargeProgress    = iso2_chargeProgressType_Start;
        body->PowerDeliveryReq.SAScheduleTupleID = 1;
        body->PowerDeliveryReq.ChargingProfile_isUsed             = 0u;
        body->PowerDeliveryReq.DC_EVPowerDeliveryParameter_isUsed = 0u;
        body->PowerDeliveryReq.EVPowerDeliveryParameter_isUsed    = 0u;

    } else if (strcmp(v, "PowerDeliveryRes") == 0) {
        body->PowerDeliveryRes_isUsed = 1u;
        body->PowerDeliveryRes.ResponseCode = iso2_responseCodeType_OK;
        body->PowerDeliveryRes.AC_EVSEStatus_isUsed = 0u;
        body->PowerDeliveryRes.EVSEStatus_isUsed    = 0u;
        body->PowerDeliveryRes.DC_EVSEStatus_isUsed = 1u;
        set_dc_evsestatus(&body->PowerDeliveryRes.DC_EVSEStatus);

    } else if (strcmp(v, "ChargingStatusRes") == 0) {
        body->ChargingStatusRes_isUsed = 1u;
        struct iso2_ChargingStatusResType* r = &body->ChargingStatusRes;
        r->ResponseCode = iso2_responseCodeType_OK;
        set_str(r->EVSEID.characters, &r->EVSEID.charactersLen, "EVSE1");
        r->SAScheduleTupleID    = 1;
        r->EVSEMaxCurrent_isUsed  = 0u;
        r->MeterInfo_isUsed       = 0u;
        r->ReceiptRequired_isUsed = 0u;
        set_ac_evsestatus(&r->AC_EVSEStatus);

    } else if (strcmp(v, "CurrentDemandReq") == 0) {
        body->CurrentDemandReq_isUsed = 1u;
        struct iso2_CurrentDemandReqType* q = &body->CurrentDemandReq;
        set_dc_evstatus(&q->DC_EVStatus);
        set_pv(&q->EVTargetCurrent, 0, iso2_unitSymbolType_A, 10);
        q->EVMaximumVoltageLimit_isUsed  = 0u;
        q->EVMaximumCurrentLimit_isUsed  = 0u;
        q->EVMaximumPowerLimit_isUsed    = 0u;
        q->BulkChargingComplete_isUsed   = 0u;
        q->ChargingComplete              = 0;
        q->RemainingTimeToFullSoC_isUsed = 0u;
        q->RemainingTimeToBulkSoC_isUsed = 0u;
        set_pv(&q->EVTargetVoltage, 0, iso2_unitSymbolType_V, 400);

    } else if (strcmp(v, "CurrentDemandRes") == 0) {
        body->CurrentDemandRes_isUsed = 1u;
        struct iso2_CurrentDemandResType* r = &body->CurrentDemandRes;
        r->ResponseCode = iso2_responseCodeType_OK;
        set_dc_evsestatus(&r->DC_EVSEStatus);
        set_pv(&r->EVSEPresentVoltage, 0, iso2_unitSymbolType_V, 395);
        set_pv(&r->EVSEPresentCurrent, 0, iso2_unitSymbolType_A, 10);
        r->EVSECurrentLimitAchieved = 0;
        r->EVSEVoltageLimitAchieved = 0;
        r->EVSEPowerLimitAchieved   = 0;
        r->EVSEMaximumVoltageLimit_isUsed = 0u;
        r->EVSEMaximumCurrentLimit_isUsed = 0u;
        r->EVSEMaximumPowerLimit_isUsed   = 0u;
        set_str(r->EVSEID.characters, &r->EVSEID.charactersLen, "EVSE1");
        r->SAScheduleTupleID    = 1;
        r->MeterInfo_isUsed       = 0u;
        r->ReceiptRequired_isUsed = 0u;

    } else if (strcmp(v, "ChargeParameterDiscoveryReq") == 0) {
        body->ChargeParameterDiscoveryReq_isUsed = 1u;
        struct iso2_ChargeParameterDiscoveryReqType* q = &body->ChargeParameterDiscoveryReq;
        q->MaxEntriesSAScheduleTuple_isUsed = 0u;
        q->RequestedEnergyTransferMode      = iso2_EnergyTransferModeType_AC_single_phase_core;
        q->DC_EVChargeParameter_isUsed = 0u;
        q->EVChargeParameter_isUsed    = 0u;
        q->AC_EVChargeParameter_isUsed = 1u;
        q->AC_EVChargeParameter.DepartureTime_isUsed = 0u;
        set_pv(&q->AC_EVChargeParameter.EAmount,      0, iso2_unitSymbolType_Wh, 1000);
        set_pv(&q->AC_EVChargeParameter.EVMaxVoltage, 0, iso2_unitSymbolType_V, 400);
        set_pv(&q->AC_EVChargeParameter.EVMaxCurrent, 0, iso2_unitSymbolType_A, 16);
        set_pv(&q->AC_EVChargeParameter.EVMinCurrent, 0, iso2_unitSymbolType_A, 2);

    } else if (strcmp(v, "ChargeParameterDiscoveryRes") == 0) {
        body->ChargeParameterDiscoveryRes_isUsed = 1u;
        struct iso2_ChargeParameterDiscoveryResType* r = &body->ChargeParameterDiscoveryRes;
        r->ResponseCode   = iso2_responseCodeType_OK;
        r->EVSEProcessing = iso2_EVSEProcessingType_Finished;
        r->SAScheduleList_isUsed = 0u;
        r->SASchedules_isUsed    = 0u;
        r->DC_EVSEChargeParameter_isUsed = 0u;
        r->EVSEChargeParameter_isUsed    = 0u;
        r->AC_EVSEChargeParameter_isUsed = 1u;
        set_ac_evsestatus(&r->AC_EVSEChargeParameter.AC_EVSEStatus);
        set_pv(&r->AC_EVSEChargeParameter.EVSENominalVoltage, 0, iso2_unitSymbolType_V, 230);
        set_pv(&r->AC_EVSEChargeParameter.EVSEMaxCurrent,     0, iso2_unitSymbolType_A, 32);

    } else if (strcmp(v, "ChargeParameterDiscoveryReq_DC") == 0) {
        body->ChargeParameterDiscoveryReq_isUsed = 1u;
        struct iso2_ChargeParameterDiscoveryReqType* q = &body->ChargeParameterDiscoveryReq;
        q->MaxEntriesSAScheduleTuple_isUsed = 0u;
        q->RequestedEnergyTransferMode      = iso2_EnergyTransferModeType_DC_extended;
        q->AC_EVChargeParameter_isUsed = 0u;
        q->EVChargeParameter_isUsed    = 0u;
        q->DC_EVChargeParameter_isUsed = 1u;
        struct iso2_DC_EVChargeParameterType* p = &q->DC_EVChargeParameter;
        p->DepartureTime_isUsed = 0u;
        set_dc_evstatus(&p->DC_EVStatus);
        set_pv(&p->EVMaximumCurrentLimit, 0, iso2_unitSymbolType_A, 200);
        p->EVMaximumPowerLimit_isUsed = 0u;
        set_pv(&p->EVMaximumVoltageLimit, 0, iso2_unitSymbolType_V, 500);
        p->EVEnergyCapacity_isUsed = 0u;
        p->EVEnergyRequest_isUsed  = 0u;
        p->FullSOC = 100; p->FullSOC_isUsed = 1u;
        p->BulkSOC = 80;  p->BulkSOC_isUsed = 1u;

    } else if (strcmp(v, "ChargeParameterDiscoveryRes_DC") == 0) {
        body->ChargeParameterDiscoveryRes_isUsed = 1u;
        struct iso2_ChargeParameterDiscoveryResType* r = &body->ChargeParameterDiscoveryRes;
        r->ResponseCode   = iso2_responseCodeType_OK;
        r->EVSEProcessing = iso2_EVSEProcessingType_Finished;
        r->SAScheduleList_isUsed = 0u;
        r->SASchedules_isUsed    = 0u;
        r->AC_EVSEChargeParameter_isUsed = 0u;
        r->EVSEChargeParameter_isUsed    = 0u;
        r->DC_EVSEChargeParameter_isUsed = 1u;
        struct iso2_DC_EVSEChargeParameterType* p = &r->DC_EVSEChargeParameter;
        set_dc_evsestatus(&p->DC_EVSEStatus);
        set_pv(&p->EVSEMaximumCurrentLimit, 0, iso2_unitSymbolType_A, 200);
        set_pv(&p->EVSEMaximumPowerLimit,   1, iso2_unitSymbolType_W, 15000);
        set_pv(&p->EVSEMaximumVoltageLimit, 0, iso2_unitSymbolType_V, 500);
        set_pv(&p->EVSEMinimumCurrentLimit, 0, iso2_unitSymbolType_A, 0);
        set_pv(&p->EVSEMinimumVoltageLimit, 0, iso2_unitSymbolType_V, 200);
        p->EVSECurrentRegulationTolerance_isUsed = 0u;
        set_pv(&p->EVSEPeakCurrentRipple, 0, iso2_unitSymbolType_A, 1);
        p->EVSEEnergyToBeDelivered_isUsed = 0u;

    } else if (strcmp(v, "ChargingStatusReq") == 0) {
        body->ChargingStatusReq_isUsed = 1u;   /* empty body */

    } else if (strcmp(v, "ServiceDetailReq") == 0) {
        body->ServiceDetailReq_isUsed = 1u;
        body->ServiceDetailReq.ServiceID = 2;

    } else if (strcmp(v, "ServiceDetailRes") == 0) {
        body->ServiceDetailRes_isUsed = 1u;
        body->ServiceDetailRes.ResponseCode = iso2_responseCodeType_OK;
        body->ServiceDetailRes.ServiceID    = 2;
        body->ServiceDetailRes.ServiceParameterList_isUsed = 0u;

    } else if (strcmp(v, "PaymentServiceSelectionReq") == 0) {
        body->PaymentServiceSelectionReq_isUsed = 1u;
        struct iso2_PaymentServiceSelectionReqType* q = &body->PaymentServiceSelectionReq;
        q->SelectedPaymentOption = iso2_paymentOptionType_Contract;
        q->SelectedServiceList.SelectedService.arrayLen = 1;
        q->SelectedServiceList.SelectedService.array[0].ServiceID = 1;
        q->SelectedServiceList.SelectedService.array[0].ParameterSetID_isUsed = 0u;

    } else if (strcmp(v, "PaymentServiceSelectionRes") == 0) {
        body->PaymentServiceSelectionRes_isUsed = 1u;
        body->PaymentServiceSelectionRes.ResponseCode = iso2_responseCodeType_OK;

    } else if (strcmp(v, "PaymentDetailsReq") == 0) {
        body->PaymentDetailsReq_isUsed = 1u;
        struct iso2_PaymentDetailsReqType* q = &body->PaymentDetailsReq;
        set_str(q->eMAID.characters, &q->eMAID.charactersLen, "DEAAA0001234567");
        q->ContractSignatureCertChain.Id_isUsed = 0u;
        static const uint8_t cert[4] = { 0x30, 0x82, 0x01, 0x02 };
        memcpy(q->ContractSignatureCertChain.Certificate.bytes, cert, sizeof(cert));
        q->ContractSignatureCertChain.Certificate.bytesLen = (uint16_t)sizeof(cert);
        q->ContractSignatureCertChain.SubCertificates_isUsed = 0u;

    } else if (strcmp(v, "PaymentDetailsRes") == 0) {
        body->PaymentDetailsRes_isUsed = 1u;
        struct iso2_PaymentDetailsResType* r = &body->PaymentDetailsRes;
        r->ResponseCode = iso2_responseCodeType_OK;
        for (int i = 0; i < 16; i++) r->GenChallenge.bytes[i] = (uint8_t)(i + 1);
        r->GenChallenge.bytesLen = 16;
        r->EVSETimeStamp = 1600000000;

    } else if (strcmp(v, "AuthorizationReq") == 0) {
        body->AuthorizationReq_isUsed = 1u;
        body->AuthorizationReq.Id_isUsed = 0u;
        for (int i = 0; i < 16; i++) body->AuthorizationReq.GenChallenge.bytes[i] = (uint8_t)(i + 1);
        body->AuthorizationReq.GenChallenge.bytesLen  = 16;
        body->AuthorizationReq.GenChallenge_isUsed    = 1u;

    } else if (strcmp(v, "AuthorizationReq_Signed") == 0) {
        // Full V2G_Message with a header signature: SignedInfo + a 64-byte (r‖s) SignatureValue,
        // KeyInfo/Object absent. Exercises SignatureType grammar 121/123/124 (all absent) end to end.
        struct iso2_MessageHeaderType* h = &doc.V2G_Message.Header;
        h->Signature_isUsed = 1u;
        h->Signature.Id_isUsed = 0u;
        set_test_signedinfo(&h->Signature.SignedInfo);
        h->Signature.SignatureValue.Id_isUsed = 0u;
        for (int i = 0; i < 64; i++) h->Signature.SignatureValue.CONTENT.bytes[i] = (uint8_t)(i + 1);
        h->Signature.SignatureValue.CONTENT.bytesLen = 64;
        h->Signature.KeyInfo_isUsed = 0u;
        h->Signature.Object_isUsed  = 0u;
        body->AuthorizationReq_isUsed = 1u;
        body->AuthorizationReq.Id_isUsed = 0u;
        for (int i = 0; i < 16; i++) body->AuthorizationReq.GenChallenge.bytes[i] = (uint8_t)(i + 1);
        body->AuthorizationReq.GenChallenge.bytesLen  = 16;
        body->AuthorizationReq.GenChallenge_isUsed    = 1u;

    } else if (strcmp(v, "AuthorizationRes") == 0) {
        body->AuthorizationRes_isUsed = 1u;
        body->AuthorizationRes.ResponseCode   = iso2_responseCodeType_OK;
        body->AuthorizationRes.EVSEProcessing = iso2_EVSEProcessingType_Finished;

    } else if (strcmp(v, "MeteringReceiptReq") == 0) {
        body->MeteringReceiptReq_isUsed = 1u;
        struct iso2_MeteringReceiptReqType* q = &body->MeteringReceiptReq;
        q->Id_isUsed = 0u;
        memset(q->SessionID.bytes, 0, iso2_sessionIDType_BYTES_SIZE);
        q->SessionID.bytesLen = iso2_sessionIDType_BYTES_SIZE;
        q->SAScheduleTupleID        = 1;
        q->SAScheduleTupleID_isUsed = 1u;
        set_str(q->MeterInfo.MeterID.characters, &q->MeterInfo.MeterID.charactersLen, "M1");
        q->MeterInfo.MeterReading_isUsed    = 0u;
        q->MeterInfo.SigMeterReading_isUsed = 0u;
        q->MeterInfo.MeterStatus_isUsed     = 0u;
        q->MeterInfo.TMeter_isUsed          = 0u;

    } else if (strcmp(v, "MeteringReceiptRes") == 0) {
        body->MeteringReceiptRes_isUsed = 1u;
        body->MeteringReceiptRes.ResponseCode = iso2_responseCodeType_OK;
        body->MeteringReceiptRes.AC_EVSEStatus_isUsed = 0u;
        body->MeteringReceiptRes.EVSEStatus_isUsed    = 0u;
        body->MeteringReceiptRes.DC_EVSEStatus_isUsed = 1u;
        set_dc_evsestatus(&body->MeteringReceiptRes.DC_EVSEStatus);

    } else if (strcmp(v, "CertificateInstallationReq") == 0) {
        body->CertificateInstallationReq_isUsed = 1u;
        struct iso2_CertificateInstallationReqType* q = &body->CertificateInstallationReq;
        set_str(q->Id.characters, &q->Id.charactersLen, "ID1");
        static const uint8_t oem[4] = { 0x30, 0x82, 0x02, 0x03 };
        memcpy(q->OEMProvisioningCert.bytes, oem, 4);
        q->OEMProvisioningCert.bytesLen = 4;
        set_rootcerts(&q->ListOfRootCertificateIDs);

    } else if (strcmp(v, "CertificateInstallationRes") == 0) {
        body->CertificateInstallationRes_isUsed = 1u;
        struct iso2_CertificateInstallationResType* r = &body->CertificateInstallationRes;
        r->ResponseCode = iso2_responseCodeType_OK;
        set_certchain(&r->SAProvisioningCertificateChain, 0);
        set_certchain(&r->ContractSignatureCertChain, 1);
        set_encprivkey(&r->ContractSignatureEncryptedPrivateKey);
        set_dhkey(&r->DHpublickey);
        set_emaid_ct(&r->eMAID);

    } else if (strcmp(v, "CertificateUpdateReq") == 0) {
        body->CertificateUpdateReq_isUsed = 1u;
        struct iso2_CertificateUpdateReqType* q = &body->CertificateUpdateReq;
        set_str(q->Id.characters, &q->Id.charactersLen, "ID1");
        set_certchain(&q->ContractSignatureCertChain, 0);
        set_str(q->eMAID.characters, &q->eMAID.charactersLen, "DEAAA0001234567");
        set_rootcerts(&q->ListOfRootCertificateIDs);

    } else if (strcmp(v, "CertificateUpdateRes") == 0) {
        body->CertificateUpdateRes_isUsed = 1u;
        struct iso2_CertificateUpdateResType* r = &body->CertificateUpdateRes;
        r->ResponseCode = iso2_responseCodeType_OK;
        set_certchain(&r->SAProvisioningCertificateChain, 0);
        set_certchain(&r->ContractSignatureCertChain, 1);
        set_encprivkey(&r->ContractSignatureEncryptedPrivateKey);
        set_dhkey(&r->DHpublickey);
        set_emaid_ct(&r->eMAID);
        r->RetryCounter        = 3;
        r->RetryCounter_isUsed = 1u;

    } else {
        fprintf(stderr, "cbv2g-iso2: unknown vector '%s'\n", v);
        return 1;
    }

    uint8_t out[OUT_BUF_SIZE];
    exi_bitstream_t stream;
    exi_bitstream_init(&stream, out, sizeof(out), 0, NULL);

    int error = encode_iso2_exiDocument(&stream, &doc);
    if (error != 0) {
        fprintf(stderr, "cbv2g-iso2: encode failed with libcbv2g error %d\n", error);
        return 3;
    }

    print_hex(out, exi_bitstream_get_length(&stream));
    return 0;
}
