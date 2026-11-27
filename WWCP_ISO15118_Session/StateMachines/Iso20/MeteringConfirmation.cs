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

using cloud.charging.open.protocols.ISO15118_20.CommonMessages;
using cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated;

using Ac20 = cloud.charging.open.protocols.ISO15118_20.AC.Generated;
using Dc20 = cloud.charging.open.protocols.ISO15118_20.DC.Generated;

namespace cloud.charging.open.protocols.ISO15118.StateMachines.Iso20
{

    /// <summary>
    /// One <c>MeteringConfirmationReq</c> the vehicle signed and sent, and what the station said to it.
    /// </summary>
    /// <param name="MeterId">The meter the confirmed reading came from.</param>
    /// <param name="ChargedWh">The reading the vehicle confirmed it had received.</param>
    /// <param name="VehicleWh">What the vehicle's own meter had counted at the same iteration.</param>
    /// <param name="WithReceipt">Whether the station's receipt was confirmed with it ([V2G20-2236]).</param>
    /// <param name="ResponseCode">The station's answer.</param>
    public sealed record MeteringConfirmationSent(String        MeterId,
                                                  UInt64        ChargedWh,
                                                  Double        VehicleWh,
                                                  Boolean       WithReceipt,
                                                  ResponseCode  ResponseCode);


    /// <summary>
    /// What a station found in one <c>MeteringConfirmationReq</c>: each of the things the vehicle's
    /// signature is meant to bind, checked separately, so a run can say which one failed.
    /// </summary>
    /// <param name="SessionOk">The signed SessionID is this session's.</param>
    /// <param name="ReadingOk">The signed MeterInfo is the one this station sent with its notification, field
    /// for field ([V2G20-1990] calls a reading that does not fit a reason to refuse).</param>
    /// <param name="ReceiptOk">The signed Receipt is the one this station sent - none, from this station.</param>
    /// <param name="ControlModeOk">The control-mode arm is the session's, and a Scheduled one names the
    /// schedule tuple the vehicle selected at PowerDelivery ([V2G20-776]).</param>
    /// <param name="DigestOk">The reference digest matches the re-encoded request fragment.</param>
    /// <param name="SignatureOk">The SignedInfo verifies against the contract certificate presented at
    /// AuthorizationReq ([V2G20-904], [V2G20-1808]).</param>
    /// <param name="Grammar">Which SignedInfo encoding verified, as <see cref="PnCAuthResult.SignatureGrammar"/>.</param>
    /// <param name="ChargedWh">The reading that was confirmed.</param>
    /// <param name="ResponseCode">What this station answered.</param>
    public sealed record MeteringConfirmationCheck(Boolean       SessionOk,
                                                   Boolean       ReadingOk,
                                                   Boolean       ReceiptOk,
                                                   Boolean       ControlModeOk,
                                                   Boolean       DigestOk,
                                                   Boolean       SignatureOk,
                                                   String        Grammar,
                                                   UInt64        ChargedWh,
                                                   ResponseCode  ResponseCode)
    {

        /// <summary>Whether every check held.</summary>
        public Boolean Valid
            => SessionOk && ReadingOk && ReceiptOk && ControlModeOk && DigestOk && SignatureOk;

    }


    /// <summary>
    /// The parts of the -20 <c>MeteringConfirmation</c> exchange both ends need: the side-stream payload
    /// type, the octets the vehicle signs, and the translation of a reading from the AC or DC message set,
    /// where the charge loop carries it, into CommonMessages, where the confirmation carries it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The exchange.</b> The station asks by putting <c>MeterInfo</c> into a charge-loop response and
    /// setting <c>EVSENotification</c> to <c>MeteringConfirmation</c> ([V2G20-1083]), never in the first
    /// one ([V2G20-1622]) and only when the vehicle authorised with Plug &amp; Charge ([V2G20-2704]) - the
    /// signature is made with the contract key ([V2G20-1808]), and an EIM vehicle has none. The vehicle
    /// answers on the side stream, V2GTP payload type <see cref="PayloadType"/> ([V2G20-1084]), within
    /// <c>NotificationMaxDelay</c> ([V2G20-1621]), and the station answers on the same type ([V2G20-1085]).
    /// </para>
    /// <para>
    /// <b>The signed octets.</b> The reference names the <c>Id</c> of <c>SignedMeteringData</c>, and what is
    /// digested is the EXI fragment of the whole <c>MeteringConfirmationReq</c> with an empty signature
    /// slot - the fragment cbV2G's <c>iso20_exiFragment</c> carries for this message set, and the one this
    /// codec is held byte-exact to (<c>Iso15118_20FragmentTests.MeteringConfirmationReq_Fragment_MatchesCbV2G</c>).
    /// No independent stack signs or verifies a MeteringConfirmationReq today, so that choice is the
    /// reference encoder's and has not been met on the wire.
    /// </para>
    /// <para>
    /// <b>Sequential multiplexing.</b> The side stream runs over the same TLS connection. This vehicle sends
    /// its confirmation between two charge-loop iterations, never with a main-stream request outstanding,
    /// which is one legal interleaving among many; the station answers every frame in the order it arrives
    /// and frames each answer with its request's payload type, so it also serves a vehicle that sends the
    /// two back to back.
    /// </para>
    /// </remarks>
    public static class MeteringConfirmation
    {

        /// <summary>The Reference Id the vehicle gives <c>SignedMeteringData</c>.</summary>
        public const String ReferenceId = "id1";

        /// <summary>V2GTP payload type <c>MeteringConfirmationPayloadID</c> (Table 14).</summary>
        public const UInt16 PayloadType = 0x8102;

        /// <summary>How long a station gives the vehicle to answer, in seconds, as NotificationMaxDelay.</summary>
        public const UInt16 DefaultMaxDelaySeconds = 5;


        #region SignedOctets(Header, Data)

        /// <summary>
        /// The EXI fragment a <c>MeteringConfirmationReq</c> is signed over: the request with
        /// <paramref name="Header"/>'s signature slot empty, whatever it holds.
        /// </summary>
        public static Byte[] SignedOctets(MessageHeaderType Header, SignedMeteringDataType Data)
        {

            var buffer = new Byte[4096];

            if (!CommonMessagesCodec.EncodeFragment_MeteringConfirmationReq(new MeteringConfirmationReqType(Header with { Signature = null }, Data),
                                                                            buffer,
                                                                            out var length))
                throw new InvalidOperationException("MeteringConfirmationReq: EXI fragment encode failed.");

            return buffer.AsSpan(0, length).ToArray();

        }

        #endregion

        #region Sign(Header, Data, ContractKey)

        /// <summary>
        /// The request, signed with the contract key in the form this vehicle signs its
        /// <c>AuthorizationReq</c> with (<see cref="XmlDsigInteropSign"/>).
        /// </summary>
        public static MeteringConfirmationReq Sign(MessageHeaderType Header, SignedMeteringDataType Data, ECDsa ContractKey)

            => new (Header with { Signature = XmlDsigInteropSign.Sign(Data.Id, SignedOctets(Header, Data), ContractKey) },
                    Data);

        #endregion


        #region SameReading(A, B)

        /// <summary>Whether two readings are the same, field for field - the meter signature by value.</summary>
        public static Boolean SameReading(MeterInfoType A, MeterInfoType B)

            => A.MeterID                        == B.MeterID                        &&
               A.ChargedEnergyReadingWh         == B.ChargedEnergyReadingWh         &&
               A.BPT_DischargedEnergyReadingWh  == B.BPT_DischargedEnergyReadingWh  &&
               A.CapacitiveEnergyReadingVARh    == B.CapacitiveEnergyReadingVARh    &&
               A.BPT_InductiveEnergyReadingVARh == B.BPT_InductiveEnergyReadingVARh &&
               A.MeterStatus                    == B.MeterStatus                    &&
               A.MeterTimestamp                 == B.MeterTimestamp                 &&
               (A.MeterSignature ?? []).AsSpan().SequenceEqual(B.MeterSignature ?? []) &&
               (A.MeterSignature is null) == (B.MeterSignature is null);

        #endregion


        #region ToCommon(...)

        /// <summary>A DC charge-loop reading, as CommonMessages carries it.</summary>
        public static MeterInfoType ToCommon(Dc20.MeterInfoType M)
            => new (M.MeterID, M.ChargedEnergyReadingWh, M.BPT_DischargedEnergyReadingWh, M.CapacitiveEnergyReadingVARh,
                    M.BPT_InductiveEnergyReadingVARh, M.MeterSignature, M.MeterStatus, M.MeterTimestamp);

        /// <summary>An AC charge-loop reading, as CommonMessages carries it.</summary>
        public static MeterInfoType ToCommon(Ac20.MeterInfoType M)
            => new (M.MeterID, M.ChargedEnergyReadingWh, M.BPT_DischargedEnergyReadingWh, M.CapacitiveEnergyReadingVARh,
                    M.BPT_InductiveEnergyReadingVARh, M.MeterSignature, M.MeterStatus, M.MeterTimestamp);

        /// <summary>A DC charge-loop receipt, as CommonMessages carries it.</summary>
        public static ReceiptType ToCommon(Dc20.ReceiptType R)
            => new (R.TimeAnchor, Cost(R.EnergyCosts), Cost(R.OccupancyCosts), Cost(R.AdditionalServicesCosts), Cost(R.OverstayCosts),
                    R.TaxCosts.Select(tax => new DetailedTaxType(tax.TaxRuleID, Rational(tax.Amount.Exponent, tax.Amount.Value))).ToArray());

        /// <summary>An AC charge-loop receipt, as CommonMessages carries it.</summary>
        public static ReceiptType ToCommon(Ac20.ReceiptType R)
            => new (R.TimeAnchor, Cost(R.EnergyCosts), Cost(R.OccupancyCosts), Cost(R.AdditionalServicesCosts), Cost(R.OverstayCosts),
                    R.TaxCosts.Select(tax => new DetailedTaxType(tax.TaxRuleID, Rational(tax.Amount.Exponent, tax.Amount.Value))).ToArray());

        private static DetailedCostType? Cost(Dc20.DetailedCostType? C)
            => C is null ? null : new (Rational(C.Amount.Exponent, C.Amount.Value), Rational(C.CostPerUnit.Exponent, C.CostPerUnit.Value));

        private static DetailedCostType? Cost(Ac20.DetailedCostType? C)
            => C is null ? null : new (Rational(C.Amount.Exponent, C.Amount.Value), Rational(C.CostPerUnit.Exponent, C.CostPerUnit.Value));

        private static RationalNumberType Rational(SByte Exponent, Int16 Value)
            => new (Exponent, Value);

        #endregion

    }

}
