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

#region Usings

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

using cloud.charging.open.protocols.ISO15118_20.CommonTypes;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    /// <summary>
    /// Signed metering data.
    /// </summary>
    public class SignedMeteringData
    {

        #region Properties

        /// <summary>
        /// The unique identification of this signed metering data.
        /// </summary>
        [Mandatory]
        public SignedMeteringData_Id  Id                         { get; }

        /// <summary>
        /// The charging session identification.
        /// </summary>
        [Mandatory]
        public Session_Id             SessionId                  { get; }

        /// <summary>
        /// The metering information.
        /// </summary>
        [Mandatory]
        public MeterInfo              MeterInfo                  { get; }

        /// <summary>
        /// The optional receipt.
        /// </summary>
        [Optional]
        public Receipt?               Receipt                    { get; }

        /// <summary>
        /// An optional selected schedule tuple identification.
        /// </summary>
        [Optional]
        public ScheduleTuple_Id?      SelectedScheduleTupleId    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create new signed metering data.
        /// </summary>
        /// <param name="Id">An unique identification for this signed metering data.</param>
        /// <param name="SessionId">A charging session identification.</param>
        /// <param name="MeterInfo">A metering information.</param>
        /// <param name="Receipt">An optional receipt.</param>
        /// <param name="SelectedScheduleTupleId">An optional selected schedule tuple identification.</param>
        public SignedMeteringData(SignedMeteringData_Id  Id,
                                  Session_Id             SessionId,
                                  MeterInfo              MeterInfo,
                                  Receipt?               Receipt                   = null,
                                  ScheduleTuple_Id?      SelectedScheduleTupleId   = null)
        {

            this.Id                       = Id;
            this.SessionId                = SessionId;
            this.MeterInfo                = MeterInfo;
            this.Receipt                  = Receipt;
            this.SelectedScheduleTupleId  = SelectedScheduleTupleId;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="SignedMeteringDataType">
        //
        //     <xs:sequence>
        //
        //         <xs:element name="SessionID" type="v2gci_ct:sessionIDType"/>
        //         <xs:element name="MeterInfo" type="v2gci_ct:MeterInfoType"/>
        //         <xs:element name="Receipt"   type="v2gci_ct:ReceiptType" minOccurs="0"/>
        //
        //         <xs:choice>
        //             <xs:element name="Dynamic_SMDTControlMode"   type="Dynamic_SMDTControlModeType"/>
        //             <xs:element name="Scheduled_SMDTControlMode" type="Scheduled_SMDTControlModeType"/>
        //         </xs:choice>
        //
        //     </xs:sequence>
        //
        //     <xs:attribute name="Id" type="xs:ID" use="required"/>
        //
        // </xs:complexType>


        // <xs:complexType name="Dynamic_SMDTControlModeType"/>


        // <xs:complexType name="Scheduled_SMDTControlModeType">
        //     <xs:sequence>
        //         <xs:element name="SelectedScheduleTupleID" type="v2gci_ct:numericIDType"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomSignedMeteringDataParser = null)

        /// <summary>
        /// Parse the given JSON representation of signed metering data.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomSignedMeteringDataParser">A delegate to parse custom signed metering datas.</param>
        public static SignedMeteringData Parse(JObject                                           JSON,
                                               CustomJObjectParserDelegate<SignedMeteringData>?  CustomSignedMeteringDataParser   = null)
        {

            if (TryParse(JSON,
                         out var signedMeteringData,
                         out var errorResponse,
                         CustomSignedMeteringDataParser))
            {
                return signedMeteringData!;
            }

            throw new ArgumentException("The given JSON representation of signed metering data is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out SignedMeteringData, out ErrorResponse, CustomSignedMeteringDataParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of signed metering data.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="SignedMeteringData">The parsed signed metering data.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                  JSON,
                                       out SignedMeteringData?  SignedMeteringData,
                                       out String?              ErrorResponse)

            => TryParse(JSON,
                        out SignedMeteringData,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of signed metering data.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="SignedMeteringData">The parsed signed metering data.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomSignedMeteringDataParser">A delegate to parse custom contract certificates.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       out SignedMeteringData?                           SignedMeteringData,
                                       out String?                                       ErrorResponse,
                                       CustomJObjectParserDelegate<SignedMeteringData>?  CustomSignedMeteringDataParser)
        {

            try
            {

                SignedMeteringData = null;

                #region Id                         [mandatory]

                if (!JSON.ParseMandatory("id",
                                         "signed metering data identification",
                                         SignedMeteringData_Id.TryParse,
                                         out SignedMeteringData_Id Id,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region SessionId                  [mandatory]

                if (!JSON.ParseMandatory("sessionId",
                                         "session identification",
                                         Session_Id.TryParse,
                                         out Session_Id SessionId,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region MeterInfo                  [mandatory]

                if (!JSON.ParseMandatoryJSON("MeterInfo",
                                             "meter info",
                                             CommonTypes.MeterInfo.TryParse,
                                             out MeterInfo? MeterInfo,
                                             out ErrorResponse))
                {
                    return false;
                }

                if (MeterInfo is null)
                    return false;

                #endregion

                #region Receipt                    [optional]

                if (!JSON.ParseOptionalJSON("receipt",
                                            "receipt",
                                            CommonTypes.Receipt.TryParse,
                                            out Receipt? Receipt,
                                            out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region SelectedScheduleTupleId    [optional]

                if (!JSON.ParseOptional("selectedScheduleTupleId",
                                        "selected schedule tuple identification",
                                        ScheduleTuple_Id.TryParse,
                                        out ScheduleTuple_Id? SelectedScheduleTupleId,
                                        out ErrorResponse))
                {
                    return false;
                }

                #endregion


                SignedMeteringData = new SignedMeteringData(Id,
                                                            SessionId,
                                                            MeterInfo,
                                                            Receipt,
                                                            SelectedScheduleTupleId);

                if (CustomSignedMeteringDataParser is not null)
                    SignedMeteringData = CustomSignedMeteringDataParser(JSON,
                                                                        SignedMeteringData);

                return true;

            }
            catch (Exception e)
            {
                SignedMeteringData  = null;
                ErrorResponse       = "The given JSON representation of signed metering data is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomSignedMeteringDataSerializer = null, CustomMeterInfoSerializer = null, ...)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomSignedMeteringDataSerializer">A delegate to serialize custom signed metering datas.</param>
        /// <param name="CustomMeterInfoSerializer">A delegate to serialize custom energy meter information.</param>
        /// <param name="CustomReceiptSerializer">A delegate to serialize custom receipts.</param>
        /// <param name="CustomDetailedCostSerializer">A delegate to serialize custom detailed costs.</param>
        /// <param name="CustomRationalNumberSerializer">A delegate to serialize custom rational numbers.</param>
        /// <param name="CustomDetailedTaxSerializer">A delegate to serialize custom detailed taxes.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<SignedMeteringData>?  CustomSignedMeteringDataSerializer   = null,
                              CustomJObjectSerializerDelegate<MeterInfo>?           CustomMeterInfoSerializer            = null,
                              CustomJObjectSerializerDelegate<Receipt>?             CustomReceiptSerializer              = null,
                              CustomJObjectSerializerDelegate<DetailedCost>?        CustomDetailedCostSerializer         = null,
                              CustomJObjectSerializerDelegate<RationalNumber>?      CustomRationalNumberSerializer       = null,
                              CustomJObjectSerializerDelegate<DetailedTax>?         CustomDetailedTaxSerializer          = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("id",                       Id.                     ToString()),
                                 new JProperty("sessionId",                SessionId.              ToString()),
                                 new JProperty("MeterInfo",                MeterInfo.              ToJSON(CustomMeterInfoSerializer)),

                           Receipt is not null
                               ? new JProperty("certificate",              Receipt.                ToJSON(CustomReceiptSerializer,
                                                                                                          CustomDetailedCostSerializer,
                                                                                                          CustomRationalNumberSerializer,
                                                                                                          CustomDetailedTaxSerializer))
                               : null,

                           SelectedScheduleTupleId.HasValue
                               ? new JProperty("selectedScheduleTupleId",  SelectedScheduleTupleId.ToString())
                               : null

                       );

            return CustomSignedMeteringDataSerializer is not null
                       ? CustomSignedMeteringDataSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (SignedMeteringData1, SignedMeteringData2)

        /// <summary>
        /// Compares two signed metering datas for equality.
        /// </summary>
        /// <param name="SignedMeteringData1">Signed metering data.</param>
        /// <param name="SignedMeteringData2">Another signed metering data.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (SignedMeteringData? SignedMeteringData1,
                                           SignedMeteringData? SignedMeteringData2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(SignedMeteringData1, SignedMeteringData2))
                return true;

            // If one is null, but not both, return false.
            if (SignedMeteringData1 is null || SignedMeteringData2 is null)
                return false;

            return SignedMeteringData1.Equals(SignedMeteringData2);

        }

        #endregion

        #region Operator != (SignedMeteringData1, SignedMeteringData2)

        /// <summary>
        /// Compares two signed metering datas for inequality.
        /// </summary>
        /// <param name="SignedMeteringData1">Signed metering data.</param>
        /// <param name="SignedMeteringData2">Another signed metering data.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (SignedMeteringData? SignedMeteringData1,
                                           SignedMeteringData? SignedMeteringData2)

            => !(SignedMeteringData1 == SignedMeteringData2);

        #endregion

        #endregion

        #region IEquatable<SignedMeteringData> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two signed metering datas for equality.
        /// </summary>
        /// <param name="Object">Signed metering data to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is SignedMeteringData signedMeteringData &&
                   Equals(signedMeteringData);

        #endregion

        #region Equals(SignedMeteringData)

        /// <summary>
        /// Compares two signed metering datas for equality.
        /// </summary>
        /// <param name="SignedMeteringData">Signed metering data to compare with.</param>
        public Boolean Equals(SignedMeteringData? SignedMeteringData)

            => SignedMeteringData is not null &&

               Id.       Equals(SignedMeteringData.Id)        &&
               SessionId.Equals(SignedMeteringData.SessionId) &&
               MeterInfo.Equals(SignedMeteringData.MeterInfo) &&

             ((Receipt                 is     null &&  SignedMeteringData.Receipt                 is     null) ||
              (Receipt                 is not null &&  SignedMeteringData.Receipt                 is not null    && Receipt.                      Equals(SignedMeteringData.Receipt))) &&

            ((!SelectedScheduleTupleId.HasValue    && !SignedMeteringData.SelectedScheduleTupleId.HasValue)    ||
              (SelectedScheduleTupleId.HasValue    &&  SignedMeteringData.SelectedScheduleTupleId.HasValue       && SelectedScheduleTupleId.Value.Equals(SignedMeteringData.SelectedScheduleTupleId.Value)));

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the HashCode of this object.
        /// </summary>
        /// <returns>The HashCode of this object.</returns>
        public override Int32 GetHashCode()
        {
            unchecked
            {

                return Id.                      GetHashCode()       * 13 ^
                       SessionId.               GetHashCode()       * 11 ^
                       MeterInfo.               GetHashCode()       *  7 ^
                      (Receipt?.                GetHashCode() ?? 0) *  5 ^
                      (SelectedScheduleTupleId?.GetHashCode() ?? 0) *  3 ^

                       base.                    GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   Id,        ", ",
                   SessionId, ", ",
                   MeterInfo,

                   Receipt is not null
                       ? ", receipt: "                    + Receipt
                       : "",

                   SelectedScheduleTupleId.HasValue
                       ? ", selected schedule tuple id: " + SelectedScheduleTupleId
                       : ""

               );

        #endregion

    }

}
