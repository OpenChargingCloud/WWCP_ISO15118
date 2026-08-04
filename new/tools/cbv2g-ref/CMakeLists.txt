# cbv2g-ref — reference EXI encoder/decoder harness for the SupportedAppProtocol
# (SAP) handshake, built on EVerest's libcbv2g.
#
# Purpose: produce wire-conformant "expectedHex" bytes for the SAP test vectors in
# Vanaheimr.V2G.Exi.Tests/Vectors/AppProtocol.vectors.json. This is a *tool*, not
# part of the product build: `dotnet test` never invokes it. It is only used to
# (re)generate the checked-in vectors — see ../regenerate-appprotocol-vectors.ps1.
#
# libcbv2g is pulled at a pinned commit via FetchContent and is NOT checked into
# this repo. We compile only the handful of sources the SAP codec needs and skip
# libcbv2g's own CMake (which pulls everest-cmake and its tooling) for robustness.

cmake_minimum_required(VERSION 3.14)
project(cbv2g_ref LANGUAGES C)

set(CMAKE_C_STANDARD 99)
set(CMAKE_C_STANDARD_REQUIRED ON)

# --- pinned reference ---------------------------------------------------------
# EVerest/libcbv2g. Bump deliberately; the vectors' `referenceEncoder.commit`
# field must be kept in sync with this SHA.
set(CBV2G_GIT_REPOSITORY "https://github.com/EVerest/libcbv2g.git")
set(CBV2G_GIT_TAG        "03350be048b35b179905129005a97144a4bdcf93")

include(FetchContent)
FetchContent_Declare(
    libcbv2g
    GIT_REPOSITORY ${CBV2G_GIT_REPOSITORY}
    GIT_TAG        ${CBV2G_GIT_TAG}
)
# Only fetch the sources; do not run libcbv2g's own CMake.
FetchContent_GetProperties(libcbv2g)
if(NOT libcbv2g_POPULATED)
    FetchContent_Populate(libcbv2g)
endif()

set(CBV2G_SRC "${libcbv2g_SOURCE_DIR}")

# The self-contained subset required for the app_handshake (SAP) codec.
add_executable(cbv2g_ref
    main.c
    "${CBV2G_SRC}/lib/cbv2g/common/exi_basetypes.c"
    "${CBV2G_SRC}/lib/cbv2g/common/exi_basetypes_decoder.c"
    "${CBV2G_SRC}/lib/cbv2g/common/exi_basetypes_encoder.c"
    "${CBV2G_SRC}/lib/cbv2g/common/exi_bitstream.c"
    "${CBV2G_SRC}/lib/cbv2g/common/exi_header.c"
    "${CBV2G_SRC}/lib/cbv2g/common/exi_types_decoder.c"
    "${CBV2G_SRC}/lib/cbv2g/app_handshake/appHand_Datatypes.c"
    "${CBV2G_SRC}/lib/cbv2g/app_handshake/appHand_Decoder.c"
    "${CBV2G_SRC}/lib/cbv2g/app_handshake/appHand_Encoder.c"
)

target_include_directories(cbv2g_ref PRIVATE "${CBV2G_SRC}/include")

# --- ISO 15118-2 reference encoder --------------------------------------------
# A second tool for the -2 message vectors (SessionSetup / ServiceDiscovery).
add_executable(cbv2g_iso2
    main_iso2.c
    "${CBV2G_SRC}/lib/cbv2g/common/exi_basetypes.c"
    "${CBV2G_SRC}/lib/cbv2g/common/exi_basetypes_decoder.c"
    "${CBV2G_SRC}/lib/cbv2g/common/exi_basetypes_encoder.c"
    "${CBV2G_SRC}/lib/cbv2g/common/exi_bitstream.c"
    "${CBV2G_SRC}/lib/cbv2g/common/exi_header.c"
    "${CBV2G_SRC}/lib/cbv2g/common/exi_types_decoder.c"
    "${CBV2G_SRC}/lib/cbv2g/iso_2/iso2_msgDefDatatypes.c"
    "${CBV2G_SRC}/lib/cbv2g/iso_2/iso2_msgDefEncoder.c"
    "${CBV2G_SRC}/lib/cbv2g/iso_2/iso2_msgDefDecoder.c"
)

target_include_directories(cbv2g_iso2 PRIVATE "${CBV2G_SRC}/include")

# --- ISO 15118-20 reference encoder ------------------------------------------
# A third tool for the -20 message vectors, covering all three Phase-4 message
# sets (CommonMessages, DC, AC) from one binary — main_iso20.c dispatches on
# the vector name (e.g. "DC_CableCheckReq") to the right set's encoder.
add_executable(cbv2g_iso20
    main_iso20.c
    "${CBV2G_SRC}/lib/cbv2g/common/exi_basetypes.c"
    "${CBV2G_SRC}/lib/cbv2g/common/exi_basetypes_decoder.c"
    "${CBV2G_SRC}/lib/cbv2g/common/exi_basetypes_encoder.c"
    "${CBV2G_SRC}/lib/cbv2g/common/exi_bitstream.c"
    "${CBV2G_SRC}/lib/cbv2g/common/exi_header.c"
    "${CBV2G_SRC}/lib/cbv2g/common/exi_types_decoder.c"
    "${CBV2G_SRC}/lib/cbv2g/iso_20/iso20_CommonMessages_Datatypes.c"
    "${CBV2G_SRC}/lib/cbv2g/iso_20/iso20_CommonMessages_Encoder.c"
    "${CBV2G_SRC}/lib/cbv2g/iso_20/iso20_CommonMessages_Decoder.c"
    "${CBV2G_SRC}/lib/cbv2g/iso_20/iso20_DC_Datatypes.c"
    "${CBV2G_SRC}/lib/cbv2g/iso_20/iso20_DC_Encoder.c"
    "${CBV2G_SRC}/lib/cbv2g/iso_20/iso20_DC_Decoder.c"
    "${CBV2G_SRC}/lib/cbv2g/iso_20/iso20_AC_Datatypes.c"
    "${CBV2G_SRC}/lib/cbv2g/iso_20/iso20_AC_Encoder.c"
    "${CBV2G_SRC}/lib/cbv2g/iso_20/iso20_AC_Decoder.c"
    "${CBV2G_SRC}/lib/cbv2g/iso_20/iso20_WPT_Datatypes.c"
    "${CBV2G_SRC}/lib/cbv2g/iso_20/iso20_WPT_Encoder.c"
    "${CBV2G_SRC}/lib/cbv2g/iso_20/iso20_WPT_Decoder.c"
    "${CBV2G_SRC}/lib/cbv2g/iso_20/iso20_ACDP_Datatypes.c"
    "${CBV2G_SRC}/lib/cbv2g/iso_20/iso20_ACDP_Encoder.c"
    "${CBV2G_SRC}/lib/cbv2g/iso_20/iso20_ACDP_Decoder.c"
)

target_include_directories(cbv2g_iso20 PRIVATE "${CBV2G_SRC}/include")
