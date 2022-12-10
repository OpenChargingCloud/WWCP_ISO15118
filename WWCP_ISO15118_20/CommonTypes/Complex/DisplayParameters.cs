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

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonTypes
{

    /// <summary>
    /// The display parameters.
    /// </summary>
    public class DisplayParameters
    {

        #region Properties

        /// <summary>
        /// The optional present state-of-charge.
        /// </summary>
        [Optional]
        public PercentValue?    PresentSOC                   { get; }

        /// <summary>
        /// The optional minimum state-of-charge.
        /// </summary>
        [Optional]
        public PercentValue?    MinimumSOC                   { get; }

        /// <summary>
        /// The optional target state-of-charge.
        /// </summary>
        [Optional]
        public PercentValue?    TargetSOC                    { get; }

        /// <summary>
        /// The optional maximum state-of-charge.
        /// </summary>
        [Optional]
        public PercentValue?    MaximumSOC                   { get; }


        /// <summary>
        /// The optional remaining time to minimum state-of-charge.
        /// </summary>
        [Optional]
        public UInt32?          RemainingTimeToMinimumSOC    { get; }

        /// <summary>
        /// The optional remaining time to target state-of-charge.
        /// </summary>
        [Optional]
        public UInt32?          RemainingTimeToTargetSOC     { get; }

        /// <summary>
        /// The optional remaining time to maximum state-of-charge.
        /// </summary>
        [Optional]
        public UInt32?          RemainingTimeToMaximumSOC    { get; }


        /// <summary>
        /// The optional indication whether charging is completed.
        /// </summary>
        public Boolean?         ChargingComplete             { get; }

        /// <summary>
        /// The optional battery energy capacity.
        /// </summary>
        public RationalNumber?  BatteryEnergyCapacity        { get; }

        /// <summary>
        /// The optional indication whether the inlet is hot.
        /// </summary>
        public Boolean?         InletHot                     { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create new display parameters.
        /// </summary>
        /// <param name="PresentSOC">An optional present state-of-charge.</param>
        /// <param name="MinimumSOC">An optional minimum state-of-charge.</param>
        /// <param name="TargetSOC">An optional target state-of-charge.</param>
        /// <param name="MaximumSOC">An optional maximum state-of-charge.</param>
        /// 
        /// <param name="RemainingTimeToMinimumSOC">An optional remaining time to minimum state-of-charge.</param>
        /// <param name="RemainingTimeToTargetSOC">An optional remaining time to target state-of-charge.</param>
        /// <param name="RemainingTimeToMaximumSOC">An optional remaining time to maximum state-of-charge.</param>
        /// 
        /// <param name="ChargingComplete">An optional indication whether charging is completed.</param>
        /// <param name="BatteryEnergyCapacity">An optional battery energy capacity.</param>
        /// <param name="InletHot">An optional indication whether the inlet is hot.</param>
        public DisplayParameters(PercentValue?    PresentSOC,
                                 PercentValue?    MinimumSOC,
                                 PercentValue?    TargetSOC,
                                 PercentValue?    MaximumSOC,

                                 UInt32?          RemainingTimeToMinimumSOC,
                                 UInt32?          RemainingTimeToTargetSOC,
                                 UInt32?          RemainingTimeToMaximumSOC,

                                 Boolean?         ChargingComplete,
                                 RationalNumber?  BatteryEnergyCapacity,
                                 Boolean?         InletHot)

        {

            this.PresentSOC                 = PresentSOC;
            this.MinimumSOC                 = MinimumSOC;
            this.TargetSOC                  = TargetSOC;
            this.MaximumSOC                 = MaximumSOC;

            this.RemainingTimeToMinimumSOC  = RemainingTimeToMinimumSOC;
            this.RemainingTimeToTargetSOC   = RemainingTimeToTargetSOC;
            this.RemainingTimeToMaximumSOC  = RemainingTimeToMaximumSOC;

            this.ChargingComplete           = ChargingComplete;
            this.BatteryEnergyCapacity      = BatteryEnergyCapacity;
            this.InletHot                   = InletHot;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="DisplayParametersType">
        //     <xs:sequence>
        //         <xs:element name="PresentSOC"                type="percentValueType"   minOccurs="0"/>
        //         <xs:element name="MinimumSOC"                type="percentValueType"   minOccurs="0"/>
        //         <xs:element name="TargetSOC"                 type="percentValueType"   minOccurs="0"/>
        //         <xs:element name="MaximumSOC"                type="percentValueType"   minOccurs="0"/>
        //         <xs:element name="RemainingTimeToMinimumSOC" type="xs:unsignedInt"     minOccurs="0"/>
        //         <xs:element name="RemainingTimeToTargetSOC"  type="xs:unsignedInt"     minOccurs="0"/>
        //         <xs:element name="RemainingTimeToMaximumSOC" type="xs:unsignedInt"     minOccurs="0"/>
        //         <xs:element name="ChargingComplete"          type="xs:boolean"         minOccurs="0"/>
        //         <xs:element name="BatteryEnergyCapacity"     type="RationalNumberType" minOccurs="0"/>
        //         <xs:element name="InletHot"                  type="xs:boolean"         minOccurs="0"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomDisplayParametersParser = null)

        /// <summary>
        /// Parse the given JSON representation of display parameters.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomDisplayParametersParser">A delegate to parse custom display parameterss.</param>
        public static DisplayParameters Parse(JObject                                          JSON,
                                              CustomJObjectParserDelegate<DisplayParameters>?  CustomDisplayParametersParser   = null)
        {

            if (TryParse(JSON,
                         out var displayParameters,
                         out var errorResponse,
                         CustomDisplayParametersParser))
            {
                return displayParameters!;
            }

            throw new ArgumentException("The given JSON representation of display parameters is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out DisplayParameters, out ErrorResponse, CustomDisplayParametersParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of display parameters.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DisplayParameters">The parsed display parameters.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                 JSON,
                                       out DisplayParameters?  DisplayParameters,
                                       out String?             ErrorResponse)

            => TryParse(JSON,
                        out DisplayParameters,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of display parameters.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="DisplayParameters">The parsed display parameters.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomDisplayParametersParser">A delegate to parse custom contract certificates.</param>
        public static Boolean TryParse(JObject                                          JSON,
                                       out DisplayParameters?                           DisplayParameters,
                                       out String?                                      ErrorResponse,
                                       CustomJObjectParserDelegate<DisplayParameters>?  CustomDisplayParametersParser)
        {

            try
            {

                DisplayParameters = null;

                #region PresentSOC                   [optional]

                if (JSON.ParseOptional("presentSOC",
                                       "present state-of-charge",
                                       PercentValue.TryParse,
                                       out PercentValue? PresentSOC,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region MinimumSOC                   [optional]

                if (JSON.ParseOptional("minimumSOC",
                                       "minimum state-of-charge",
                                       PercentValue.TryParse,
                                       out PercentValue? MinimumSOC,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region TargetSOC                    [optional]

                if (JSON.ParseOptional("targetSOC",
                                       "target state-of-charge",
                                       PercentValue.TryParse,
                                       out PercentValue? TargetSOC,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region MaximumSOC                   [optional]

                if (JSON.ParseOptional("maximumSOC",
                                       "maximum state-of-charge",
                                       PercentValue.TryParse,
                                       out PercentValue? MaximumSOC,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                #region RemainingTimeToMinimumSOC    [optional]

                if (JSON.ParseOptional("remainingTimeToMinimumSOC",
                                       "remaining time to minimum state-of-charge",
                                       out UInt32? RemainingTimeToMinimumSOC,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region RemainingTimeToTargetSOC     [optional]

                if (JSON.ParseOptional("remainingTimeToTargetSOC",
                                       "remaining time to target state-of-charge",
                                       out UInt32? RemainingTimeToTargetSOC,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region RemainingTimeToMaximumSOC    [optional]

                if (JSON.ParseOptional("remainingTimeToMaximumSOC",
                                       "remaining time to maximum state-of-charge",
                                       out UInt32? RemainingTimeToMaximumSOC,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                #region ChargingComplete             [optional]

                if (JSON.ParseOptional("chargingComplete",
                                       "charging complete",
                                       out Boolean? ChargingComplete,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region BatteryEnergyCapacity        [optional]

                if (JSON.ParseOptionalJSON("batteryEnergyCapacity",
                                           "battery energy capacity",
                                           RationalNumber.TryParse,
                                           out RationalNumber? BatteryEnergyCapacity,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region InletHot                     [optional]

                if (JSON.ParseOptional("inletHot",
                                       "inlet hot",
                                       out Boolean? InletHot,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                DisplayParameters = new DisplayParameters(PresentSOC,
                                                          MinimumSOC,
                                                          TargetSOC,
                                                          MaximumSOC,

                                                          RemainingTimeToMinimumSOC,
                                                          RemainingTimeToTargetSOC,
                                                          RemainingTimeToMaximumSOC,

                                                          ChargingComplete,
                                                          BatteryEnergyCapacity,
                                                          InletHot);

                if (CustomDisplayParametersParser is not null)
                    DisplayParameters = CustomDisplayParametersParser(JSON,
                                                                      DisplayParameters);

                return true;

            }
            catch (Exception e)
            {
                DisplayParameters  = null;
                ErrorResponse      = "The given JSON representation of display parameters is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomDisplayParametersSerializer = null, CustomRationalNumberSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomDisplayParametersSerializer">A delegate to serialize custom display parameterss.</param>
        /// <param name="CustomRationalNumberSerializer">A delegate to serialize custom rational numbers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<DisplayParameters>?  CustomDisplayParametersSerializer   = null,
                              CustomJObjectSerializerDelegate<RationalNumber>?     CustomRationalNumberSerializer      = null)
        {

            var json = JSONObject.Create(

                           PresentSOC.HasValue
                               ? new JProperty("presentSOC",                  PresentSOC.               Value.Value)
                               : null,

                           MinimumSOC.HasValue
                               ? new JProperty("minimumSOC",                  MinimumSOC.               Value.Value)
                               : null,

                           TargetSOC.HasValue
                               ? new JProperty("targetSOC",                   TargetSOC.                Value. Value)
                               : null,

                           MaximumSOC.HasValue
                               ? new JProperty("maximumSOC",                  MaximumSOC.               Value.Value)
                               : null,


                           RemainingTimeToMinimumSOC.HasValue
                               ? new JProperty("remainingTimeToMinimumSOC",   RemainingTimeToMinimumSOC.Value)
                               : null,

                           RemainingTimeToTargetSOC.HasValue
                               ? new JProperty("remainingTimeToTargetSOC",    RemainingTimeToTargetSOC. Value)
                               : null,

                           RemainingTimeToMaximumSOC.HasValue
                               ? new JProperty("remainingTimeToMaximumSOC",   RemainingTimeToMaximumSOC.Value)
                               : null,


                           ChargingComplete.HasValue
                               ? new JProperty("chargingComplete",            ChargingComplete.         Value)
                               : null,

                           BatteryEnergyCapacity.HasValue
                               ? new JProperty("batteryEnergyCapacity",       BatteryEnergyCapacity.    Value.ToJSON(CustomRationalNumberSerializer))
                               : null,

                           InletHot.HasValue
                               ? new JProperty("inletHot",                    InletHot.                 Value)
                               : null

                       );

            return CustomDisplayParametersSerializer is not null
                       ? CustomDisplayParametersSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (DisplayParameters1, DisplayParameters2)

        /// <summary>
        /// Compares two display parameterss for equality.
        /// </summary>
        /// <param name="DisplayParameters1">Display parameters.</param>
        /// <param name="DisplayParameters2">Other display parameters.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (DisplayParameters? DisplayParameters1,
                                           DisplayParameters? DisplayParameters2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(DisplayParameters1, DisplayParameters2))
                return true;

            // If one is null, but not both, return false.
            if (DisplayParameters1 is null || DisplayParameters2 is null)
                return false;

            return DisplayParameters1.Equals(DisplayParameters2);

        }

        #endregion

        #region Operator != (DisplayParameters1, DisplayParameters2)

        /// <summary>
        /// Compares two display parameterss for inequality.
        /// </summary>
        /// <param name="DisplayParameters1">Display parameters.</param>
        /// <param name="DisplayParameters2">Other display parameters.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (DisplayParameters? DisplayParameters1,
                                           DisplayParameters? DisplayParameters2)

            => !(DisplayParameters1 == DisplayParameters2);

        #endregion

        #endregion

        #region IEquatable<DisplayParameters> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two display parameterss for equality.
        /// </summary>
        /// <param name="Object">Display parameters to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is DisplayParameters displayParameters &&
                   Equals(displayParameters);

        #endregion

        #region Equals(DisplayParameters)

        /// <summary>
        /// Compares two display parameterss for equality.
        /// </summary>
        /// <param name="DisplayParameters">Display parameters to compare with.</param>
        public Boolean Equals(DisplayParameters? DisplayParameters)

            => DisplayParameters is not null &&

            ((!PresentSOC.               HasValue && !DisplayParameters.PresentSOC.               HasValue) ||
              (PresentSOC.               HasValue &&  DisplayParameters.PresentSOC.               HasValue    && PresentSOC.            Value.Equals(DisplayParameters.PresentSOC.               Value))) &&

            ((!MinimumSOC.               HasValue && !DisplayParameters.MinimumSOC.               HasValue) ||
              (MinimumSOC.               HasValue &&  DisplayParameters.MinimumSOC.               HasValue    && MinimumSOC.            Value.Equals(DisplayParameters.MinimumSOC.               Value))) &&

            ((!TargetSOC.                HasValue && !DisplayParameters.TargetSOC.                HasValue) ||
              (TargetSOC.                HasValue &&  DisplayParameters.TargetSOC.                HasValue    && TargetSOC.             Value.Equals(DisplayParameters.TargetSOC.                Value))) &&

            ((!MaximumSOC.               HasValue && !DisplayParameters.MaximumSOC.               HasValue) ||
              (MaximumSOC.               HasValue &&  DisplayParameters.MaximumSOC.               HasValue    && MaximumSOC.            Value.Equals(DisplayParameters.MaximumSOC.               Value))) &&


            ((!RemainingTimeToMinimumSOC.HasValue && !DisplayParameters.RemainingTimeToMinimumSOC.HasValue) ||
              (RemainingTimeToMinimumSOC.HasValue &&  DisplayParameters.RemainingTimeToMinimumSOC.HasValue && RemainingTimeToMinimumSOC.Value.Equals(DisplayParameters.RemainingTimeToMinimumSOC.Value))) &&

            ((!RemainingTimeToTargetSOC. HasValue && !DisplayParameters.RemainingTimeToTargetSOC. HasValue) ||
              (RemainingTimeToTargetSOC. HasValue &&  DisplayParameters.RemainingTimeToTargetSOC. HasValue && RemainingTimeToTargetSOC. Value.Equals(DisplayParameters.RemainingTimeToTargetSOC. Value))) &&

            ((!RemainingTimeToMaximumSOC.HasValue && !DisplayParameters.RemainingTimeToMaximumSOC.HasValue) ||
              (RemainingTimeToMaximumSOC.HasValue &&  DisplayParameters.RemainingTimeToMaximumSOC.HasValue && RemainingTimeToMaximumSOC.Value.Equals(DisplayParameters.RemainingTimeToMaximumSOC.Value))) &&


            ((!ChargingComplete.         HasValue && !DisplayParameters.ChargingComplete.         HasValue) ||
              (ChargingComplete.         HasValue &&  DisplayParameters.ChargingComplete.         HasValue && ChargingComplete.         Value.Equals(DisplayParameters.ChargingComplete.         Value))) &&

            ((!BatteryEnergyCapacity.    HasValue && !DisplayParameters.BatteryEnergyCapacity.    HasValue) ||
              (BatteryEnergyCapacity.    HasValue &&  DisplayParameters.BatteryEnergyCapacity.    HasValue && BatteryEnergyCapacity.    Value.Equals(DisplayParameters.BatteryEnergyCapacity.    Value))) &&

            ((!InletHot.                 HasValue && !DisplayParameters.InletHot.                 HasValue) ||
              (InletHot.                 HasValue &&  DisplayParameters.InletHot.                 HasValue && InletHot.                 Value.Equals(DisplayParameters.InletHot.                 Value)));

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

                return (PresentSOC?.               GetHashCode() ?? 0) * 31 ^
                       (MinimumSOC?.               GetHashCode() ?? 0) * 29 ^
                       (TargetSOC?.                GetHashCode() ?? 0) * 23 ^
                       (MaximumSOC?.               GetHashCode() ?? 0) * 19 ^

                       (RemainingTimeToMinimumSOC?.GetHashCode() ?? 0) * 17 ^
                       (RemainingTimeToTargetSOC?. GetHashCode() ?? 0) * 13 ^
                       (RemainingTimeToMaximumSOC?.GetHashCode() ?? 0) * 11 ^

                       (ChargingComplete?.         GetHashCode() ?? 0) *  7 ^
                       (BatteryEnergyCapacity?.    GetHashCode() ?? 0) *  5 ^
                       (InletHot?.                 GetHashCode() ?? 0) *  3 ^

                       base.                       GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => new String[] {

                   PresentSOC.                HasValue
                       ? "present SOC: "             + PresentSOC.               Value.ToString()
                       : "",

                   MinimumSOC.                HasValue
                       ? "minimum SOC: "             + MinimumSOC.               Value.ToString()
                       : "",

                   TargetSOC.                 HasValue
                       ? "target SOC: "              + TargetSOC.                Value.ToString()
                       : "",

                   MaximumSOC.                HasValue
                       ? "maximum SOC: "             + MaximumSOC.               Value.ToString()
                       : "",


                   RemainingTimeToMinimumSOC. HasValue
                       ? "till minimum SOC: "        + RemainingTimeToMinimumSOC.Value.ToString()
                       : "",

                   RemainingTimeToTargetSOC.  HasValue
                       ? "till target SOC: "         + RemainingTimeToTargetSOC. Value.ToString()
                       : "",

                   RemainingTimeToMaximumSOC. HasValue
                       ? "till maximum SOC: "        + RemainingTimeToMaximumSOC.Value.ToString()
                       : "",


                   ChargingComplete.          HasValue
                       ? "charging complete: "       + ChargingComplete.         Value.ToString()
                       : "",

                   BatteryEnergyCapacity.     HasValue
                       ? "battery energy capacity: " + BatteryEnergyCapacity.    Value.ToString()
                       : "",

                   InletHot.                  HasValue
                       ? "inlet hot: "               + InletHot.                 Value.ToString()
                       : ""

            }.AggregateWith(", ");

        #endregion

    }

}
