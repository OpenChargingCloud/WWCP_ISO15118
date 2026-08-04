/* SPDX-License-Identifier: Apache-2.0 */
/*
 * cbv2g-ref — a thin CLI around EVerest's libcbv2g app_handshake (SAP) codec.
 *
 * It is the *reference oracle* for the SupportedAppProtocol test vectors: it turns
 * a simple line-based description of a SAP message into wire-conformant EXI hex
 * (encode) and back (decode). The JSON<->line translation lives in the PowerShell
 * driver (regenerate-appprotocol-vectors.ps1); keeping JSON out of C keeps this
 * harness tiny and dependency-free.
 *
 * The EXI stream produced/consumed here INCLUDES the 0x80 EXI header byte (libcbv2g
 * writes/reads it itself) but NOT the 8-byte V2GTP header — matching the vector
 * file convention.
 *
 * ---- line protocol -------------------------------------------------------------
 * encode (stdin -> stdout hex):
 *     req
 *     <major> <minor> <schemaId> <priority> <namespace...>   (one line per AppProtocol)
 *     ...
 *   or
 *     res
 *     <codeIndex> <schemaId|->        (codeIndex: 0=OK, 1=OK_minor, 2=Failed)
 *
 * decode (stdin hex -> stdout lines): emits the same shape as encode input, so the
 * driver can assert a bidirectional round-trip.
 * --------------------------------------------------------------------------------
 */

#define _GNU_SOURCE  /* getline() under -std=c99 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdint.h>

#include "cbv2g/common/exi_bitstream.h"
#include "cbv2g/app_handshake/appHand_Datatypes.h"
#include "cbv2g/app_handshake/appHand_Encoder.h"
#include "cbv2g/app_handshake/appHand_Decoder.h"

#define OUT_BUF_SIZE 4096

/* cbV2G ResponseCode enum values (declaration order in appHand_Datatypes.h). */
static appHand_responseCodeType code_from_index(int idx) {
    switch (idx) {
        case 0: return appHand_responseCodeType_OK_SuccessfulNegotiation;
        case 1: return appHand_responseCodeType_OK_SuccessfulNegotiationWithMinorDeviation;
        case 2: return appHand_responseCodeType_Failed_NoNegotiation;
        default: fprintf(stderr, "cbv2g-ref: invalid response code index %d\n", idx); exit(2);
    }
}

static int index_from_code(appHand_responseCodeType c) {
    switch (c) {
        case appHand_responseCodeType_OK_SuccessfulNegotiation:                   return 0;
        case appHand_responseCodeType_OK_SuccessfulNegotiationWithMinorDeviation: return 1;
        case appHand_responseCodeType_Failed_NoNegotiation:                       return 2;
        default: return -1;
    }
}

static void print_hex(const uint8_t* data, size_t len) {
    for (size_t i = 0; i < len; i++)
        printf(i == 0 ? "%02x" : " %02x", data[i]);
    printf("\n");
}

static void die(const char* what, int error) {
    fprintf(stderr, "cbv2g-ref: %s failed with libcbv2g error %d\n", what, error);
    exit(3);
}

/* ---- encode ---------------------------------------------------------------- */

static int do_encode(void) {
    char*   line = NULL;
    size_t  cap  = 0;
    ssize_t n;

    if ((n = getline(&line, &cap, stdin)) < 0) {
        fprintf(stderr, "cbv2g-ref: empty encode input\n");
        return 1;
    }
    /* strip trailing newline(s) */
    while (n > 0 && (line[n - 1] == '\n' || line[n - 1] == '\r')) line[--n] = '\0';

    struct appHand_exiDocument doc;
    init_appHand_exiDocument(&doc);

    if (strcmp(line, "req") == 0) {
        doc.supportedAppProtocolReq_isUsed = 1;
        init_appHand_supportedAppProtocolReq(&doc.supportedAppProtocolReq);
        uint16_t count = 0;

        while ((n = getline(&line, &cap, stdin)) >= 0) {
            while (n > 0 && (line[n - 1] == '\n' || line[n - 1] == '\r')) line[--n] = '\0';
            if (n == 0) continue;

            if (count >= appHand_AppProtocolType_5_ARRAY_SIZE) {
                fprintf(stderr, "cbv2g-ref: libcbv2g caps AppProtocol at %d entries\n",
                        appHand_AppProtocolType_5_ARRAY_SIZE);
                free(line);
                return 4;
            }

            struct appHand_AppProtocolType* ap = &doc.supportedAppProtocolReq.AppProtocol.array[count];
            init_appHand_AppProtocolType(ap);

            unsigned int major, minor, schemaId, priority;
            int off = 0;
            if (sscanf(line, "%u %u %u %u %n", &major, &minor, &schemaId, &priority, &off) < 4) {
                fprintf(stderr, "cbv2g-ref: malformed ap line: %s\n", line);
                free(line);
                return 1;
            }
            const char* ns = line + off;               /* rest of line = namespace bytes */
            size_t ns_len  = strlen(ns);
            if (ns_len >= appHand_ProtocolNamespace_CHARACTER_SIZE) {
                fprintf(stderr, "cbv2g-ref: namespace too long (%zu)\n", ns_len);
                free(line);
                return 1;
            }
            ap->VersionNumberMajor = major;
            ap->VersionNumberMinor = minor;
            ap->SchemaID           = (uint8_t)schemaId;
            ap->Priority           = (uint8_t)priority;
            memcpy(ap->ProtocolNamespace.characters, ns, ns_len);
            ap->ProtocolNamespace.characters[ns_len] = '\0';
            ap->ProtocolNamespace.charactersLen = (uint16_t)ns_len;
            count++;
        }
        doc.supportedAppProtocolReq.AppProtocol.arrayLen = count;

    } else if (strcmp(line, "res") == 0) {
        doc.supportedAppProtocolRes_isUsed = 1;
        init_appHand_supportedAppProtocolRes(&doc.supportedAppProtocolRes);

        if ((n = getline(&line, &cap, stdin)) < 0) {
            fprintf(stderr, "cbv2g-ref: res needs a second line\n");
            free(line);
            return 1;
        }
        int codeIdx;
        char schema[64];
        if (sscanf(line, "%d %63s", &codeIdx, schema) < 2) {
            fprintf(stderr, "cbv2g-ref: malformed res line: %s\n", line);
            free(line);
            return 1;
        }
        doc.supportedAppProtocolRes.ResponseCode = code_from_index(codeIdx);
        if (strcmp(schema, "-") == 0) {
            doc.supportedAppProtocolRes.SchemaID_isUsed = 0;
        } else {
            doc.supportedAppProtocolRes.SchemaID_isUsed = 1;
            doc.supportedAppProtocolRes.SchemaID = (uint8_t)atoi(schema);
        }
    } else {
        fprintf(stderr, "cbv2g-ref: first line must be 'req' or 'res', got: %s\n", line);
        free(line);
        return 1;
    }
    free(line);

    uint8_t out[OUT_BUF_SIZE];
    exi_bitstream_t stream;
    exi_bitstream_init(&stream, out, sizeof(out), 0, NULL);

    int error = encode_appHand_exiDocument(&stream, &doc);
    if (error != 0) die("encode", error);

    print_hex(out, exi_bitstream_get_length(&stream));
    return 0;
}

/* ---- decode ---------------------------------------------------------------- */

static int do_decode(void) {
    uint8_t buf[OUT_BUF_SIZE];
    size_t  len = 0;
    unsigned int byte;

    /* read whitespace-separated hex bytes from stdin */
    while (scanf("%2x", &byte) == 1) {
        if (len >= sizeof(buf)) { fprintf(stderr, "cbv2g-ref: input too long\n"); return 1; }
        buf[len++] = (uint8_t)byte;
    }
    if (len == 0) { fprintf(stderr, "cbv2g-ref: empty decode input\n"); return 1; }

    struct appHand_exiDocument doc;
    init_appHand_exiDocument(&doc);

    exi_bitstream_t stream;
    exi_bitstream_init(&stream, buf, len, 0, NULL);

    int error = decode_appHand_exiDocument(&stream, &doc);
    if (error != 0) die("decode", error);

    if (doc.supportedAppProtocolReq_isUsed) {
        printf("req\n");
        for (uint16_t i = 0; i < doc.supportedAppProtocolReq.AppProtocol.arrayLen; i++) {
            const struct appHand_AppProtocolType* ap = &doc.supportedAppProtocolReq.AppProtocol.array[i];
            printf("%u %u %u %u %s\n",
                   ap->VersionNumberMajor, ap->VersionNumberMinor,
                   ap->SchemaID, ap->Priority, ap->ProtocolNamespace.characters);
        }
    } else if (doc.supportedAppProtocolRes_isUsed) {
        printf("res\n");
        if (doc.supportedAppProtocolRes.SchemaID_isUsed)
            printf("%d %u\n", index_from_code(doc.supportedAppProtocolRes.ResponseCode),
                   doc.supportedAppProtocolRes.SchemaID);
        else
            printf("%d -\n", index_from_code(doc.supportedAppProtocolRes.ResponseCode));
    } else {
        fprintf(stderr, "cbv2g-ref: decoded neither req nor res\n");
        return 1;
    }
    return 0;
}

int main(int argc, char** argv) {
    if (argc != 2) {
        fprintf(stderr, "usage: %s <encode|decode>\n", argv[0]);
        return 1;
    }
    if (strcmp(argv[1], "encode") == 0) return do_encode();
    if (strcmp(argv[1], "decode") == 0) return do_decode();
    fprintf(stderr, "usage: %s <encode|decode>\n", argv[0]);
    return 1;
}
