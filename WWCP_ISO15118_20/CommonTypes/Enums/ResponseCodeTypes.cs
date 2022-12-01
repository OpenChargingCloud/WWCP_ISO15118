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

namespace cloud.charging.open.protocols.ISO15118_20.CommonTypes
{

    /// <summary>
    /// Extensions methods for response codes.
    /// </summary>
    public static class ResponseCodesExtensions
    {

        #region Parse   (Text)

        /// <summary>
        /// Parse the given text as a response code.
        /// </summary>
        /// <param name="Text">A text representation of a response code.</param>
        public static ResponseCodes Parse(String Text)
        {

            if (TryParse(Text, out var code))
                return code;

            return ResponseCodes.Unknown;

        }

        #endregion

        #region TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a response code.
        /// </summary>
        /// <param name="Text">A text representation of a response code.</param>
        public static ResponseCodes? TryParse(String Text)
        {

            if (TryParse(Text, out var code))
                return code;

            return null;

        }

        #endregion

        #region TryParse(Text, out ResponseCode)

        /// <summary>
        /// Try to parse the given text as a response code.
        /// </summary>
        /// <param name="Text">A text representation of a response code.</param>
        /// <param name="ResponseCode">The parsed response codes.</param>
        public static Boolean TryParse(String Text, out ResponseCodes ResponseCode)
        {
            switch (Text.Trim())
            {

                case "OK":
                    ResponseCode = ResponseCodes.OK;
                    return true;

                case "OK_CertificateExpiresSoon":
                    ResponseCode = ResponseCodes.OK_CertificateExpiresSoon;
                    return true;

                case "OK_NewSessionEstablished":
                    ResponseCode = ResponseCodes.OK_NewSessionEstablished;
                    return true;

                case "OK_OldSessionJoined":
                    ResponseCode = ResponseCodes.OK_OldSessionJoined;
                    return true;

                case "OK_PowerToleranceConfirmed":
                    ResponseCode = ResponseCodes.OK_PowerToleranceConfirmed;
                    return true;


                case "WARNING_AuthorizationSelectionInvalid":
                    ResponseCode = ResponseCodes.WARNING_AuthorizationSelectionInvalid;
                    return true;

                case "WARNING_CertificateExpired":
                    ResponseCode = ResponseCodes.WARNING_CertificateExpired;
                    return true;

                case "WARNING_CertificateNotYetValid":
                    ResponseCode = ResponseCodes.WARNING_CertificateNotYetValid;
                    return true;

                case "WARNING_CertificateRevoked":
                    ResponseCode = ResponseCodes.WARNING_CertificateRevoked;
                    return true;

                case "WARNING_CertificateValidationError":
                    ResponseCode = ResponseCodes.WARNING_CertificateValidationError;
                    return true;

                case "WARNING_ChallengeInvalid":
                    ResponseCode = ResponseCodes.WARNING_ChallengeInvalid;
                    return true;

                case "WARNING_EIMAuthorizationFailure":
                    ResponseCode = ResponseCodes.WARNING_EIMAuthorizationFailure;
                    return true;

                case "WARNING_EMSPUnknown":
                    ResponseCode = ResponseCodes.WARNING_EMSPUnknown;
                    return true;

                case "WARNING_EVPowerProfileViolation":
                    ResponseCode = ResponseCodes.WARNING_EVPowerProfileViolation;
                    return true;

                case "WARNING_GeneralPnCAuthorizationError":
                    ResponseCode = ResponseCodes.WARNING_GeneralPnCAuthorizationError;
                    return true;

                case "WARNING_NoCertificateAvailable":
                    ResponseCode = ResponseCodes.WARNING_NoCertificateAvailable;
                    return true;

                case "WARNING_NoContractMatchingPCIDFound":
                    ResponseCode = ResponseCodes.WARNING_NoContractMatchingPCIDFound;
                    return true;

                case "WARNING_PowerToleranceNotConfirmed":
                    ResponseCode = ResponseCodes.WARNING_PowerToleranceNotConfirmed;
                    return true;

                case "WARNING_ScheduleRenegotiationFailed":
                    ResponseCode = ResponseCodes.WARNING_ScheduleRenegotiationFailed;
                    return true;

                case "WARNING_StandbyNotAllowed":
                    ResponseCode = ResponseCodes.WARNING_StandbyNotAllowed;
                    return true;

                case "WARNING_WPT":
                    ResponseCode = ResponseCodes.WARNING_WPT;
                    return true;


                case "FAILED":
                    ResponseCode = ResponseCodes.FAILED;
                    return true;

                case "FAILED_AssociationError":
                    ResponseCode = ResponseCodes.FAILED_AssociationError;
                    return true;

                case "FAILED_ContactorError":
                    ResponseCode = ResponseCodes.FAILED_ContactorError;
                    return true;

                case "FAILED_EVPowerProfileInvalid":
                    ResponseCode = ResponseCodes.FAILED_EVPowerProfileInvalid;
                    return true;

                case "FAILED_EVPowerProfileViolation":
                    ResponseCode = ResponseCodes.FAILED_EVPowerProfileViolation;
                    return true;

                case "FAILED_MeteringSignatureNotValid":
                    ResponseCode = ResponseCodes.FAILED_MeteringSignatureNotValid;
                    return true;

                case "FAILED_NoEnergyTransferServiceSelected":
                    ResponseCode = ResponseCodes.FAILED_NoEnergyTransferServiceSelected;
                    return true;

                case "FAILED_NoServiceRenegotiationSupported":
                    ResponseCode = ResponseCodes.FAILED_NoServiceRenegotiationSupported;
                    return true;

                case "FAILED_PauseNotAllowed":
                    ResponseCode = ResponseCodes.FAILED_PauseNotAllowed;
                    return true;

                case "FAILED_PowerDeliveryNotApplied":
                    ResponseCode = ResponseCodes.FAILED_PowerDeliveryNotApplied;
                    return true;

                case "FAILED_PowerToleranceNotConfirmed":
                    ResponseCode = ResponseCodes.FAILED_PowerToleranceNotConfirmed;
                    return true;

                case "FAILED_ScheduleRenegotiation":
                    ResponseCode = ResponseCodes.FAILED_ScheduleRenegotiation;
                    return true;

                case "FAILED_ScheduleSelectionInvalid":
                    ResponseCode = ResponseCodes.FAILED_ScheduleSelectionInvalid;
                    return true;

                case "FAILED_SequenceError":
                    ResponseCode = ResponseCodes.FAILED_SequenceError;
                    return true;

                case "FAILED_ServiceIDInvalid":
                    ResponseCode = ResponseCodes.FAILED_ServiceIDInvalid;
                    return true;

                case "FAILED_ServiceSelectionInvalid":
                    ResponseCode = ResponseCodes.FAILED_ServiceSelectionInvalid;
                    return true;

                case "FAILED_SignatureError":
                    ResponseCode = ResponseCodes.FAILED_SignatureError;
                    return true;

                case "FAILED_UnknownSession":
                    ResponseCode = ResponseCodes.FAILED_UnknownSession;
                    return true;

                case "FAILED_WrongChargeParameter":
                    ResponseCode = ResponseCodes.FAILED_WrongChargeParameter;
                    return true;


                default:
                    ResponseCode = ResponseCodes.Unknown;
                    return false;

            }
        }

        #endregion

        #region AsText  (this ResponseCode)

        public static String AsText(this ResponseCodes ResponseCode)

            => ResponseCode switch {

                   ResponseCodes.OK                                       => "OK",
                   ResponseCodes.OK_CertificateExpiresSoon                => "OK_CertificateExpiresSoon",
                   ResponseCodes.OK_NewSessionEstablished                 => "OK_NewSessionEstablished",
                   ResponseCodes.OK_OldSessionJoined                      => "OK_OldSessionJoined",
                   ResponseCodes.OK_PowerToleranceConfirmed               => "OK_PowerToleranceConfirmed",

                   ResponseCodes.WARNING_AuthorizationSelectionInvalid    => "WARNING_AuthorizationSelectionInvalid",
                   ResponseCodes.WARNING_CertificateExpired               => "WARNING_CertificateExpired",
                   ResponseCodes.WARNING_CertificateNotYetValid           => "WARNING_CertificateNotYetValid",
                   ResponseCodes.WARNING_CertificateRevoked               => "WARNING_CertificateRevoked",
                   ResponseCodes.WARNING_CertificateValidationError       => "WARNING_CertificateValidationError",
                   ResponseCodes.WARNING_ChallengeInvalid                 => "WARNING_ChallengeInvalid",
                   ResponseCodes.WARNING_EIMAuthorizationFailure          => "WARNING_EIMAuthorizationFailure",
                   ResponseCodes.WARNING_EMSPUnknown                      => "WARNING_EMSPUnknown",
                   ResponseCodes.WARNING_EVPowerProfileViolation          => "WARNING_EVPowerProfileViolation",
                   ResponseCodes.WARNING_GeneralPnCAuthorizationError     => "WARNING_GeneralPnCAuthorizationError",
                   ResponseCodes.WARNING_NoCertificateAvailable           => "WARNING_NoCertificateAvailable",
                   ResponseCodes.WARNING_NoContractMatchingPCIDFound      => "WARNING_NoContractMatchingPCIDFound",
                   ResponseCodes.WARNING_PowerToleranceNotConfirmed       => "WARNING_PowerToleranceNotConfirmed",
                   ResponseCodes.WARNING_ScheduleRenegotiationFailed      => "WARNING_ScheduleRenegotiationFailed",
                   ResponseCodes.WARNING_StandbyNotAllowed                => "WARNING_StandbyNotAllowed",
                   ResponseCodes.WARNING_WPT                              => "WARNING_WPT",

                   ResponseCodes.FAILED                                   => "FAILED",
                   ResponseCodes.FAILED_AssociationError                  => "FAILED_AssociationError",
                   ResponseCodes.FAILED_ContactorError                    => "FAILED_ContactorError",
                   ResponseCodes.FAILED_EVPowerProfileInvalid             => "FAILED_EVPowerProfileInvalid",
                   ResponseCodes.FAILED_EVPowerProfileViolation           => "FAILED_EVPowerProfileViolation",
                   ResponseCodes.FAILED_MeteringSignatureNotValid         => "FAILED_MeteringSignatureNotValid",
                   ResponseCodes.FAILED_NoEnergyTransferServiceSelected   => "FAILED_NoEnergyTransferServiceSelected",
                   ResponseCodes.FAILED_NoServiceRenegotiationSupported   => "FAILED_NoServiceRenegotiationSupported",
                   ResponseCodes.FAILED_PauseNotAllowed                   => "FAILED_PauseNotAllowed",
                   ResponseCodes.FAILED_PowerDeliveryNotApplied           => "FAILED_PowerDeliveryNotApplied",
                   ResponseCodes.FAILED_PowerToleranceNotConfirmed        => "FAILED_PowerToleranceNotConfirmed",
                   ResponseCodes.FAILED_ScheduleRenegotiation             => "FAILED_ScheduleRenegotiation",
                   ResponseCodes.FAILED_ScheduleSelectionInvalid          => "FAILED_ScheduleSelectionInvalid",
                   ResponseCodes.FAILED_SequenceError                     => "FAILED_SequenceError",
                   ResponseCodes.FAILED_ServiceIDInvalid                  => "FAILED_ServiceIDInvalid",
                   ResponseCodes.FAILED_ServiceSelectionInvalid           => "FAILED_ServiceSelectionInvalid",
                   ResponseCodes.FAILED_SignatureError                    => "FAILED_SignatureError",
                   ResponseCodes.FAILED_UnknownSession                    => "FAILED_UnknownSession",
                   ResponseCodes.FAILED_WrongChargeParameter              => "FAILED_WrongChargeParameter",

                   _                                                      => "Unknown"

               };

        #endregion

    }


    public enum ResponseCodes
    {

        Unknown,

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
