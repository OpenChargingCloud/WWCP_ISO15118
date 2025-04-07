/*
 * Copyright (c) 2021-2025 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonTypes
{

    /// <summary>
    /// The energy meter information.
    /// </summary>
    public class MeterInfo
    {

        #region Properties

        /// <summary>
        /// The unique identification of the energy meter.
        /// </summary>
        [Mandatory]
        public Meter_Id         MeterId                    { get; }

        /// <summary>
        /// The charged energy reading [Wh].
        /// </summary>
        [Mandatory]
        public UInt64           ChargedEnergyReading       { get; }

        /// <summary>
        /// The optional discharged energy reading [Wh].
        /// </summary>
        [Optional]
        public UInt64?          DischargedEnergyReading    { get; }

        /// <summary>
        /// The optional capacitive energy reading [VARh].
        /// </summary>
        [Optional]
        public UInt64?          CapacitiveEnergyReading    { get; }

        /// <summary>
        /// The optional bidirectional power transfer inductive energy reading [VARh].
        /// </summary>
        [Optional]
        public UInt64?          InductiveEnergyReading     { get; }

        /// <summary>
        /// The optional energy meter signature.
        /// </summary>
        [Optional]
        public MeterSignature?  MeterSignature             { get; }

        /// <summary>
        /// The optional meter status.
        /// </summary>
        [Optional]
        public Int16?           MeterStatus                { get; }

        /// <summary>
        /// The optional meter timestamp.
        /// </summary>
        [Optional]
        public DateTime?        MeterTimestamp             { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create new energy meter information.
        /// </summary>
        /// <param name="MeterId">An unique identification of the energy meter.</param>
        /// <param name="ChargedEnergyReading">The charged energy reading [Wh].</param>
        /// <param name="DischargedEnergyReading">An optional discharged energy reading [Wh].</param>
        /// <param name="CapacitiveEnergyReading">An optional capacitive energy reading [VARh].</param>
        /// <param name="InductiveEnergyReading">An optional bidirectional power transfer inductive energy reading [VARh].</param>
        /// <param name="MeterSignature">An optional energy meter signature.</param>
        /// <param name="MeterStatus">An optional meter status.</param>
        /// <param name="MeterTimestamp">An optional meter timestamp.</param>
        public MeterInfo(Meter_Id         MeterId,
                         UInt64           ChargedEnergyReading,
                         UInt64?          DischargedEnergyReading   = null,
                         UInt64?          CapacitiveEnergyReading   = null,
                         UInt64?          InductiveEnergyReading    = null,
                         MeterSignature?  MeterSignature            = null,
                         Int16?           MeterStatus               = null,
                         DateTime?        MeterTimestamp            = null)
        {

            this.MeterId                  = MeterId;
            this.ChargedEnergyReading     = ChargedEnergyReading;
            this.DischargedEnergyReading  = DischargedEnergyReading;
            this.CapacitiveEnergyReading  = CapacitiveEnergyReading;
            this.InductiveEnergyReading   = InductiveEnergyReading;
            this.MeterSignature           = MeterSignature;
            this.MeterStatus              = MeterStatus;
            this.MeterTimestamp           = MeterTimestamp;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="MeterInfoType">
        //     <xs:sequence>
        //         <xs:element name="MeterID"                        type="meterIDType"/>
        //         <xs:element name="ChargedEnergyReadingWh"         type="xs:unsignedLong"/>
        //         <xs:element name="BPT_DischargedEnergyReadingWh"  type="xs:unsignedLong"    minOccurs="0"/>
        //         <xs:element name="CapacitiveEnergyReadingVARh"    type="xs:unsignedLong"    minOccurs="0"/>
        //         <xs:element name="BPT_InductiveEnergyReadingVARh" type="xs:unsignedLong"    minOccurs="0"/>
        //         <xs:element name="MeterSignature"                 type="meterSignatureType" minOccurs="0"/>
        //         <xs:element name="MeterStatus"                    type="xs:short"           minOccurs="0"/>
        //         <xs:element name="MeterTimestamp"                 type="xs:unsignedLong"    minOccurs="0"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomMeterInfoParser = null)

        /// <summary>
        /// Parse the given JSON representation of energy meter information.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomMeterInfoParser">An optional delegate to parse custom energy meter information.</param>
        public static MeterInfo Parse(JObject                                  JSON,
                                      CustomJObjectParserDelegate<MeterInfo>?  CustomMeterInfoParser   = null)
        {

            if (TryParse(JSON,
                         out var meterInfo,
                         out var errorResponse,
                         CustomMeterInfoParser))
            {
                return meterInfo!;
            }

            throw new ArgumentException("The given JSON representation of energy meter information is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out MeterInfo, out ErrorResponse, CustomMeterInfoParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of energy meter information.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="MeterInfo">The parsed energy meter information.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject         JSON,
                                       out MeterInfo?  MeterInfo,
                                       out String?     ErrorResponse)

            => TryParse(JSON,
                        out MeterInfo,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of energy meter information.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="MeterInfo">The parsed energy meter information.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomMeterInfoParser">An optional delegate to parse custom contract certificates.</param>
        public static Boolean TryParse(JObject                                  JSON,
                                       out MeterInfo?                           MeterInfo,
                                       out String?                              ErrorResponse,
                                       CustomJObjectParserDelegate<MeterInfo>?  CustomMeterInfoParser)
        {

            try
            {

                MeterInfo = null;

                #region MeterId                    [mandatory]

                if (!JSON.ParseMandatory("meterId",
                                         "energy meter identification",
                                         Meter_Id.TryParse,
                                         out Meter_Id MeterId,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region ChargedEnergyReading       [mandatory]

                if (!JSON.ParseMandatory("chargedEnergyReading",
                                         "charged energy reading",
                                         out UInt64 ChargedEnergyReading,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region DischargedEnergyReading    [optional]

                if (JSON.ParseOptional("dischargedEnergyReading",
                                       "discharged energy reading",
                                       out UInt64? DischargedEnergyReading,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region CapacitiveEnergyReading    [optional]

                if (JSON.ParseOptional("capacitiveEnergyReading",
                                       "capacitive energy reading",
                                       out UInt64? CapacitiveEnergyReading,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region InductiveEnergyReading     [optional]

                if (JSON.ParseOptional("inductiveEnergyReading",
                                       "inductive energy reading",
                                       out UInt64? InductiveEnergyReading,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region MeterSignature             [optional]

                if (JSON.ParseOptional("dischargedEnergyReading",
                                       "discharged energy reading",
                                       CommonTypes.MeterSignature.TryParse,
                                       out MeterSignature? MeterSignature,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region MeterStatus                [optional]

                if (JSON.ParseOptional("meterStatus",
                                       "meter status",
                                       out Int16? MeterStatus,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region MeterTimestamp             [optional]

                if (JSON.ParseOptional("meterTimestamp",
                                       "meter  timestamp",
                                       out DateTime? MeterTimestamp,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                MeterInfo = new MeterInfo(MeterId,
                                          ChargedEnergyReading,
                                          DischargedEnergyReading,
                                          CapacitiveEnergyReading,
                                          InductiveEnergyReading,
                                          MeterSignature,
                                          MeterStatus,
                                          MeterTimestamp);

                if (CustomMeterInfoParser is not null)
                    MeterInfo = CustomMeterInfoParser(JSON,
                                                      MeterInfo);

                return true;

            }
            catch (Exception e)
            {
                MeterInfo      = null;
                ErrorResponse  = "The given JSON representation of energy meter information is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomMeterInfoSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomMeterInfoSerializer">A delegate to serialize custom energy meter information.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<MeterInfo>? CustomMeterInfoSerializer = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("meterId",                   MeterId.                      ToString()),
                                 new JProperty("chargedEnergyReading",      ChargedEnergyReading),

                           DischargedEnergyReading.HasValue
                               ? new JProperty("dischargedEnergyReading",   DischargedEnergyReading.Value)
                               : null,

                           CapacitiveEnergyReading.HasValue
                               ? new JProperty("capacitiveEnergyReading",   CapacitiveEnergyReading.Value)
                               : null,

                           InductiveEnergyReading.HasValue
                               ? new JProperty("inductiveEnergyReading",    InductiveEnergyReading. Value)
                               : null,

                           MeterSignature.HasValue
                               ? new JProperty("meterSignature",            MeterSignature.         Value.ToString())
                               : null,

                           MeterStatus.HasValue
                               ? new JProperty("MeterStatus",               MeterStatus.            Value)
                               : null,

                           MeterTimestamp.HasValue
                               ? new JProperty("meterTimestamp",            MeterTimestamp.         Value.ToIso8601())
                               : null

                       );

            return CustomMeterInfoSerializer is not null
                       ? CustomMeterInfoSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (MeterInfo1, MeterInfo2)

        /// <summary>
        /// Compares two energy meter information for equality.
        /// </summary>
        /// <param name="MeterInfo1">Energy meter information.</param>
        /// <param name="MeterInfo2">Other energy meter information.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (MeterInfo? MeterInfo1,
                                           MeterInfo? MeterInfo2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(MeterInfo1, MeterInfo2))
                return true;

            // If one is null, but not both, return false.
            if (MeterInfo1 is null || MeterInfo2 is null)
                return false;

            return MeterInfo1.Equals(MeterInfo2);

        }

        #endregion

        #region Operator != (MeterInfo1, MeterInfo2)

        /// <summary>
        /// Compares two energy meter information for inequality.
        /// </summary>
        /// <param name="MeterInfo1">Energy meter information.</param>
        /// <param name="MeterInfo2">Other energy meter information.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (MeterInfo? MeterInfo1,
                                           MeterInfo? MeterInfo2)

            => !(MeterInfo1 == MeterInfo2);

        #endregion

        #endregion

        #region IEquatable<MeterInfo> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two energy meter information for equality.
        /// </summary>
        /// <param name="Object">Energy meter information to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is MeterInfo meterInfo &&
                   Equals(meterInfo);

        #endregion

        #region Equals(MeterInfo)

        /// <summary>
        /// Compares two energy meter information for equality.
        /// </summary>
        /// <param name="MeterInfo">Energy meter information to compare with.</param>
        public Boolean Equals(MeterInfo? MeterInfo)

            => MeterInfo is not null &&

               MeterId.             Equals(MeterInfo.MeterId)              &&
               ChargedEnergyReading.Equals(MeterInfo.ChargedEnergyReading) &&

            ((!DischargedEnergyReading.HasValue && !MeterInfo.DischargedEnergyReading.HasValue) ||
              (DischargedEnergyReading.HasValue &&  MeterInfo.DischargedEnergyReading.HasValue && DischargedEnergyReading.Value.Equals(MeterInfo.DischargedEnergyReading.Value))) &&

            ((!CapacitiveEnergyReading.HasValue && !MeterInfo.CapacitiveEnergyReading.HasValue) ||
              (CapacitiveEnergyReading.HasValue &&  MeterInfo.CapacitiveEnergyReading.HasValue && CapacitiveEnergyReading.Value.Equals(MeterInfo.CapacitiveEnergyReading.Value))) &&

            ((!InductiveEnergyReading. HasValue && !MeterInfo.InductiveEnergyReading. HasValue) ||
              (InductiveEnergyReading. HasValue &&  MeterInfo.InductiveEnergyReading. HasValue && InductiveEnergyReading. Value.Equals(MeterInfo.InductiveEnergyReading. Value))) &&

            ((!MeterSignature.         HasValue && !MeterInfo.MeterSignature.         HasValue) ||
              (MeterSignature.         HasValue &&  MeterInfo.MeterSignature.         HasValue && MeterSignature.         Value.Equals(MeterInfo.MeterSignature.         Value))) &&

            ((!MeterStatus.            HasValue && !MeterInfo.MeterStatus.            HasValue) ||
              (MeterStatus.            HasValue &&  MeterInfo.MeterStatus.            HasValue && MeterStatus.            Value.Equals(MeterInfo.MeterStatus.            Value))) &&

            ((!MeterTimestamp.         HasValue && !MeterInfo.MeterTimestamp.         HasValue) ||
              (MeterTimestamp.         HasValue &&  MeterInfo.MeterTimestamp.         HasValue && MeterTimestamp.         Value.Equals(MeterInfo.MeterTimestamp.         Value)));

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the HashCode of this object.
        /// </summary>
        public override Int32 GetHashCode()
        {
            unchecked
            {

                return MeterId.                 GetHashCode()       * 5 ^
                       ChargedEnergyReading.    GetHashCode()       * 3 ^
                      (DischargedEnergyReading?.GetHashCode() ?? 0) * 3 ^
                      (CapacitiveEnergyReading?.GetHashCode() ?? 0) * 3 ^
                      (InductiveEnergyReading?. GetHashCode() ?? 0) * 3 ^
                      (MeterSignature?.         GetHashCode() ?? 0) * 3 ^
                      (MeterStatus?.            GetHashCode() ?? 0) * 3 ^
                      (MeterTimestamp?.         GetHashCode() ?? 0) * 3 ^

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

                   MeterId,
                   ": ",
                   ChargedEnergyReading,
                   " Wh",

                   DischargedEnergyReading.HasValue
                       ? ", discharged energy: " + DischargedEnergyReading.Value + " Wh"
                       : "",

                   CapacitiveEnergyReading.HasValue
                       ? ", capacitive energy: " + CapacitiveEnergyReading.Value + " VARh"
                       : "",

                   InductiveEnergyReading.HasValue
                       ? ", inductive energy: " +  InductiveEnergyReading. Value + " VARh"
                       : "",

                   MeterSignature.HasValue
                       ? ", meter signature: " +   MeterSignature.         Value
                       : "",

                   MeterStatus.HasValue
                       ? ", meter status: " +      MeterStatus.            Value
                       : "",

                   MeterTimestamp.HasValue
                       ? ", meter timestamp: " +   MeterTimestamp.         Value.ToIso8601()
                       : ""

               );

        #endregion

    }

}
