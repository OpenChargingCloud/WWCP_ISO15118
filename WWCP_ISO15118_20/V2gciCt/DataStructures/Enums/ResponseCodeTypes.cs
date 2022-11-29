/*
 * Copyright (c) 2021-2022 GraphDefined GmbH
 * This file is part of WWCP ISO 15118-20 <https://github.com/OpenChargingCloud/WWCP_ISO15118_20>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace cloud.charging.open.protocols.ISO15118_20.V2gciCt
{
    public enum ResponseCodeTypes
    {

        OK,
        OK_CertificateExpiresSoon,
        OK_NewSessionEstablished,
        OK_OldSessionJoined,
        OK_PowerToleranceConfirmed,

        WARNING_AuthorizationSelectionInvalid,
        WARNING_CertificateExpired,
        WARNING_CertificateNotYetValid,
        WARNING_CertificateRevoked,
        WARNING_CertificateValidationError,
        WARNING_ChallengeInvalid,
        WARNING_EIMAuthorizationFailure,
        WARNING_EMSPUnknown,
        WARNING_EVPowerProfileViolation,
        WARNING_GeneralPnCAuthorizationError,
        WARNING_NoCertificateAvailable,
        WARNING_NoContractMatchingPCIDFound,
        WARNING_PowerToleranceNotConfirmed,
        WARNING_ScheduleRenegotiationFailed,
        WARNING_StandbyNotAllowed,
        WARNING_WPT,

        FAILED,
        FAILED_AssociationError,
        FAILED_ContactorError,
        FAILED_EVPowerProfileInvalid,
        FAILED_EVPowerProfileViolation,
        FAILED_MeteringSignatureNotValid,
        FAILED_NoEnergyTransferServiceSelected,
        FAILED_NoServiceRenegotiationSupported,
        FAILED_PauseNotAllowed,
        FAILED_PowerDeliveryNotApplied,
        FAILED_PowerToleranceNotConfirmed,
        FAILED_ScheduleRenegotiation,
        FAILED_ScheduleSelectionInvalid,
        FAILED_SequenceError,
        FAILED_ServiceIDInvalid,
        FAILED_ServiceSelectionInvalid,
        FAILED_SignatureError,
        FAILED_UnknownSession,
        FAILED_WrongChargeParameter

    }

}
