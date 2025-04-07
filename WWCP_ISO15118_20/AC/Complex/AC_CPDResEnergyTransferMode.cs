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

using cloud.charging.open.protocols.ISO15118_20.CommonTypes;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.AC
{

    /// <summary>
    /// The AC CPD response energy transfer mode.
    /// </summary>
    public class AC_CPDResEnergyTransferMode
    {

        #region Properties

        /// <summary>
        /// The EVSE maximum charge power.
        /// </summary>
        [Mandatory]
        public RationalNumber   EVSEMaximumChargePower      { get; }

        /// <summary>
        /// The optional EVSE maximum charge power on phase 2.
        /// </summary>
        [Optional]
        public RationalNumber?  EVSEMaximumChargePowerL2    { get; }

        /// <summary>
        /// The optional EVSE maximum charge power on phase 3.
        /// </summary>
        [Optional]
        public RationalNumber?  EVSEMaximumChargePowerL3    { get; }


        /// <summary>
        /// The EVSE minimum charge power.
        /// </summary>
        [Mandatory]
        public RationalNumber   EVSEMinimumChargePower      { get; }

        /// <summary>
        /// The optional EVSE minimum charge power on phase 2.
        /// </summary>
        [Optional]
        public RationalNumber?  EVSEMinimumChargePowerL2    { get; }

        /// <summary>
        /// The optional EVSE minimum charge power on phase 3.
        /// </summary>
        [Optional]
        public RationalNumber?  EVSEMinimumChargePowerL3    { get; }


        /// <summary>
        /// The optional EVSE present active power.
        /// </summary>
        [Optional]
        public RationalNumber?  EVSEPresentActivePower      { get; }

        /// <summary>
        /// The optional EVSE present active power on phase 2.
        /// </summary>
        [Optional]
        public RationalNumber?  EVSEPresentActivePowerL2    { get; }

        /// <summary>
        /// The optional EVSE present active power on phase 3.
        /// </summary>
        [Optional]
        public RationalNumber?  EVSEPresentActivePowerL3    { get; }


        /// <summary>
        /// The EVSE nominal frequency.
        /// </summary>
        [Mandatory]
        public RationalNumber   EVSENominalFrequency        { get; }

        /// <summary>
        /// The optional maximum power asymmetry.
        /// </summary>
        [Optional]
        public RationalNumber?  MaximumPowerAsymmetry       { get; }

        /// <summary>
        /// The optional EVSE power ramp limitation.
        /// </summary>
        [Optional]
        public RationalNumber?  EVSEPowerRampLimitation     { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new AC CPD response energy transfer mode.
        /// </summary>
        /// <param name="EVSEMaximumChargePower">An EVSE maximum charge power.</param>
        /// <param name="EVSEMinimumChargePower">An EVSE minimum charge power.</param>
        /// <param name="EVSENominalFrequency">An EVSE nominal frequency.</param>
        /// 
        /// <param name="EVSEMaximumChargePowerL2">An optional EVSE maximum charge power on phase 2.</param>
        /// <param name="EVSEMaximumChargePowerL3">An optional EVSE maximum charge power on phase 3.</param>
        /// 
        /// <param name="EVSEMinimumChargePowerL2">An optional EVSE minimum charge power on phase 2.</param>
        /// <param name="EVSEMinimumChargePowerL3">An optional EVSE minimum charge power on phase 3.</param>
        /// 
        /// <param name="EVSEPresentActivePower">An optional EVSE present active power.</param>
        /// <param name="EVSEPresentActivePowerL2">An optional EVSE present active power on phase 2.</param>
        /// <param name="EVSEPresentActivePowerL3">An optional EVSE present active power on phase 3.</param>
        /// 
        /// <param name="MaximumPowerAsymmetry">An optional maximum power asymmetry.</param>
        /// <param name="EVSEPowerRampLimitation">An optional EVSE power ramp limitation.</param>
        public AC_CPDResEnergyTransferMode(RationalNumber   EVSEMaximumChargePower,
                                           RationalNumber   EVSEMinimumChargePower,
                                           RationalNumber   EVSENominalFrequency,

                                           RationalNumber?  EVSEMaximumChargePowerL2   = null,
                                           RationalNumber?  EVSEMaximumChargePowerL3   = null,

                                           RationalNumber?  EVSEMinimumChargePowerL2   = null,
                                           RationalNumber?  EVSEMinimumChargePowerL3   = null,

                                           RationalNumber?  EVSEPresentActivePower     = null,
                                           RationalNumber?  EVSEPresentActivePowerL2   = null,
                                           RationalNumber?  EVSEPresentActivePowerL3   = null,

                                           RationalNumber?  MaximumPowerAsymmetry      = null,
                                           RationalNumber?  EVSEPowerRampLimitation    = null)

        {

            this.EVSEMaximumChargePower     = EVSEMaximumChargePower;
            this.EVSEMinimumChargePower     = EVSEMinimumChargePower;
            this.EVSENominalFrequency       = EVSENominalFrequency;

            this.EVSEMaximumChargePowerL2  = EVSEMaximumChargePowerL2;
            this.EVSEMaximumChargePowerL3  = EVSEMaximumChargePowerL3;

            this.EVSEMinimumChargePowerL2  = EVSEMinimumChargePowerL2;
            this.EVSEMinimumChargePowerL3  = EVSEMinimumChargePowerL3;

            this.EVSEPresentActivePower     = EVSEPresentActivePower;
            this.EVSEPresentActivePowerL2  = EVSEPresentActivePowerL2;
            this.EVSEPresentActivePowerL3  = EVSEPresentActivePowerL3;

            this.MaximumPowerAsymmetry      = MaximumPowerAsymmetry;
            this.EVSEPowerRampLimitation    = EVSEPowerRampLimitation;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="AC_CPDResEnergyTransferModeType">
        //     <xs:sequence>
        //         <xs:element name="EVSEMaximumChargePower"    type="v2gci_ct:RationalNumberType"/>
        //         <xs:element name="EVSEMaximumChargePower_L2" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //         <xs:element name="EVSEMaximumChargePower_L3" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //
        //         <xs:element name="EVSEMinimumChargePower"    type="v2gci_ct:RationalNumberType"/>
        //         <xs:element name="EVSEMinimumChargePower_L2" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //         <xs:element name="EVSEMinimumChargePower_L3" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //
        //         <xs:element name="EVSENominalFrequency"      type="v2gci_ct:RationalNumberType"/>
        //         <xs:element name="MaximumPowerAsymmetry"     type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //         <xs:element name="EVSEPowerRampLimitation"   type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //
        //         <xs:element name="EVSEPresentActivePower"    type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //         <xs:element name="EVSEPresentActivePower_L2" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //         <xs:element name="EVSEPresentActivePower_L3" type="v2gci_ct:RationalNumberType" minOccurs="0"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomAC_CPDResEnergyTransferModeParser = null)

        /// <summary>
        /// Parse the given JSON representation of an AC CPD response energy transfer mode.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomAC_CPDResEnergyTransferModeParser">An optional delegate to parse custom AC CPD response energy transfer modes.</param>
        public static AC_CPDResEnergyTransferMode Parse(JObject                                                    JSON,
                                                        CustomJObjectParserDelegate<AC_CPDResEnergyTransferMode>?  CustomAC_CPDResEnergyTransferModeParser   = null)
        {

            if (TryParse(JSON,
                         out var ac_CPDResEnergyTransferMode,
                         out var errorResponse,
                         CustomAC_CPDResEnergyTransferModeParser))
            {
                return ac_CPDResEnergyTransferMode!;
            }

            throw new ArgumentException("The given JSON representation of an AC CPD response energy transfer mode is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out AC_CPDResEnergyTransferMode, out ErrorResponse, CustomAC_CPDResEnergyTransferModeParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of an AC CPD response energy transfer mode.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="AC_CPDResEnergyTransferMode">The parsed AC CPD response energy transfer mode.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                           JSON,
                                       out AC_CPDResEnergyTransferMode?  AC_CPDResEnergyTransferMode,
                                       out String?                       ErrorResponse)

            => TryParse(JSON,
                        out AC_CPDResEnergyTransferMode,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an AC CPD response energy transfer mode.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="AC_CPDResEnergyTransferMode">The parsed AC CPD response energy transfer mode.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomAC_CPDResEnergyTransferModeParser">An optional delegate to parse custom contract certificates.</param>
        public static Boolean TryParse(JObject                                                    JSON,
                                       out AC_CPDResEnergyTransferMode?                           AC_CPDResEnergyTransferMode,
                                       out String?                                                ErrorResponse,
                                       CustomJObjectParserDelegate<AC_CPDResEnergyTransferMode>?  CustomAC_CPDResEnergyTransferModeParser)
        {

            try
            {

                AC_CPDResEnergyTransferMode = null;

                #region EVSEMaximumChargePower      [mandatory]

                if (!JSON.ParseMandatoryJSON("evseMaximumChargePower",
                                             "EVSE maximum charge power",
                                             RationalNumber.TryParse,
                                             out RationalNumber EVSEMaximumChargePower,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region EVSEMinimumChargePower      [mandatory]

                if (!JSON.ParseMandatoryJSON("evseMinimumChargePower",
                                             "EVSE minimum charge power",
                                             RationalNumber.TryParse,
                                             out RationalNumber EVSEMinimumChargePower,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region EVSENominalFrequency        [mandatory]

                if (!JSON.ParseMandatoryJSON("evseNominalFrequency",
                                             "EVSE nominal frequency",
                                             RationalNumber.TryParse,
                                             out RationalNumber EVSENominalFrequency,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region EVSEMaximumChargePowerL2    [optional]

                if (JSON.ParseOptionalJSON("evseMaximumChargePowerL2",
                                           "EVSE maximum charge power phase 2",
                                           RationalNumber.TryParse,
                                           out RationalNumber? EVSEMaximumChargePowerL2,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region EVSEMaximumChargePowerL3    [optional]

                if (JSON.ParseOptionalJSON("evseMaximumChargePowerL3",
                                           "EVSE maximum charge power phase 3",
                                           RationalNumber.TryParse,
                                           out RationalNumber? EVSEMaximumChargePowerL3,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region EVSEMinimumChargePowerL2    [optional]

                if (JSON.ParseOptionalJSON("evseMinimumChargePowerL2",
                                           "EVSE minimum charge power phase 2",
                                           RationalNumber.TryParse,
                                           out RationalNumber? EVSEMinimumChargePowerL2,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region EVSEMinimumChargePowerL3    [optional]

                if (JSON.ParseOptionalJSON("evseMinimumChargePowerL3",
                                           "EVSE minimum charge power phase 3",
                                           RationalNumber.TryParse,
                                           out RationalNumber? EVSEMinimumChargePowerL3,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region EVSEPresentActivePower      [optional]

                if (JSON.ParseOptionalJSON("evsePresentActivePower",
                                           "EVSE present active power",
                                           RationalNumber.TryParse,
                                           out RationalNumber? EVSEPresentActivePower,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region EVSEPresentActivePowerL2    [optional]

                if (JSON.ParseOptionalJSON("evsePresentActivePower",
                                           "EVSE present active power phase 2",
                                           RationalNumber.TryParse,
                                           out RationalNumber? EVSEPresentActivePowerL2,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region EVSEPresentActivePowerL3    [optional]

                if (JSON.ParseOptionalJSON("evsePresentActivePower",
                                           "EVSE present active power phase 3",
                                           RationalNumber.TryParse,
                                           out RationalNumber? EVSEPresentActivePowerL3,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region MaximumPowerAsymmetry       [optional]

                if (JSON.ParseOptionalJSON("maximumPowerAsymmetry",
                                           "maximum power asymmetry",
                                           RationalNumber.TryParse,
                                           out RationalNumber? MaximumPowerAsymmetry,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region EVSEPowerRampLimitation     [optional]

                if (JSON.ParseOptionalJSON("evsePowerRampLimitation",
                                           "EVSE power ramp limitation",
                                           RationalNumber.TryParse,
                                           out RationalNumber? EVSEPowerRampLimitation,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                AC_CPDResEnergyTransferMode = new AC_CPDResEnergyTransferMode(EVSEMaximumChargePower,
                                                                              EVSEMinimumChargePower,
                                                                              EVSENominalFrequency,
                                                                              EVSEMaximumChargePowerL2,
                                                                              EVSEMaximumChargePowerL3,
                                                                              EVSEMinimumChargePowerL2,
                                                                              EVSEMinimumChargePowerL3,
                                                                              EVSEPresentActivePower,
                                                                              EVSEPresentActivePowerL2,
                                                                              EVSEPresentActivePowerL3,
                                                                              MaximumPowerAsymmetry,
                                                                              EVSEPowerRampLimitation);

                if (CustomAC_CPDResEnergyTransferModeParser is not null)
                    AC_CPDResEnergyTransferMode = CustomAC_CPDResEnergyTransferModeParser(JSON,
                                                                                          AC_CPDResEnergyTransferMode);

                return true;

            }
            catch (Exception e)
            {
                AC_CPDResEnergyTransferMode  = null;
                ErrorResponse                = "The given JSON representation of an AC CPD response energy transfer mode is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomAC_CPDResEnergyTransferModeSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomAC_CPDResEnergyTransferModeSerializer">A delegate to serialize custom AC CPD response energy transfer modes.</param>
        /// <param name="CustomRationalNumberSerializer">A delegate to serialize custom rational numbers.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<AC_CPDResEnergyTransferMode>?  CustomAC_CPDResEnergyTransferModeSerializer   = null,
                              CustomJObjectSerializerDelegate<RationalNumber>?               CustomRationalNumberSerializer                = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("evseMaximumChargePower",    EVSEMaximumChargePower.        ToJSON(CustomRationalNumberSerializer)),
                                 new JProperty("evseMinimumChargePower",    EVSEMinimumChargePower.        ToJSON(CustomRationalNumberSerializer)),
                                 new JProperty("evseNominalFrequency",      EVSENominalFrequency.          ToJSON(CustomRationalNumberSerializer)),


                           EVSEMaximumChargePowerL2.HasValue
                               ? new JProperty("evseMaximumChargePowerL2",  EVSEMaximumChargePowerL2.Value.ToJSON(CustomRationalNumberSerializer))
                               : null,

                           EVSEMaximumChargePowerL3.HasValue
                               ? new JProperty("evseMaximumChargePowerL3",  EVSEMaximumChargePowerL3.Value.ToJSON(CustomRationalNumberSerializer))
                               : null,


                           EVSEMinimumChargePowerL2.HasValue
                               ? new JProperty("EVSEMinimumChargePowerL2",  EVSEMinimumChargePowerL2.Value.ToJSON(CustomRationalNumberSerializer))
                               : null,

                           EVSEMinimumChargePowerL3.HasValue
                               ? new JProperty("evseMinimumChargePowerL3",  EVSEMinimumChargePowerL3.Value.ToJSON(CustomRationalNumberSerializer))
                               : null,


                           EVSEPresentActivePower.HasValue
                               ? new JProperty("evsePresentActivePower",    EVSEPresentActivePower.  Value.ToJSON(CustomRationalNumberSerializer))
                               : null,

                           EVSEPresentActivePowerL2.HasValue
                               ? new JProperty("EVSEPresentActivePowerL2",  EVSEPresentActivePowerL2.Value.ToJSON(CustomRationalNumberSerializer))
                               : null,

                           EVSEPresentActivePowerL3.HasValue
                               ? new JProperty("evsePresentActivePowerL3",  EVSEPresentActivePowerL3.Value.ToJSON(CustomRationalNumberSerializer))
                               : null,


                           MaximumPowerAsymmetry.HasValue
                               ? new JProperty("maximumPowerAsymmetry",     MaximumPowerAsymmetry.   Value.ToJSON(CustomRationalNumberSerializer))
                               : null,

                           EVSEPowerRampLimitation.HasValue
                               ? new JProperty("evsePowerRampLimitation",   EVSEPowerRampLimitation. Value.ToJSON(CustomRationalNumberSerializer))
                               : null

                       );

            return CustomAC_CPDResEnergyTransferModeSerializer is not null
                       ? CustomAC_CPDResEnergyTransferModeSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (AC_CPDResEnergyTransferMode1, AC_CPDResEnergyTransferMode2)

        /// <summary>
        /// Compares two AC CPD response energy transfer modes for equality.
        /// </summary>
        /// <param name="AC_CPDResEnergyTransferMode1">An AC CPD response energy transfer mode.</param>
        /// <param name="AC_CPDResEnergyTransferMode2">Another AC CPD response energy transfer mode.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (AC_CPDResEnergyTransferMode? AC_CPDResEnergyTransferMode1,
                                           AC_CPDResEnergyTransferMode? AC_CPDResEnergyTransferMode2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(AC_CPDResEnergyTransferMode1, AC_CPDResEnergyTransferMode2))
                return true;

            // If one is null, but not both, return false.
            if (AC_CPDResEnergyTransferMode1 is null || AC_CPDResEnergyTransferMode2 is null)
                return false;

            return AC_CPDResEnergyTransferMode1.Equals(AC_CPDResEnergyTransferMode2);

        }

        #endregion

        #region Operator != (AC_CPDResEnergyTransferMode1, AC_CPDResEnergyTransferMode2)

        /// <summary>
        /// Compares two AC CPD response energy transfer modes for inequality.
        /// </summary>
        /// <param name="AC_CPDResEnergyTransferMode1">An AC CPD response energy transfer mode.</param>
        /// <param name="AC_CPDResEnergyTransferMode2">Another AC CPD response energy transfer mode.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (AC_CPDResEnergyTransferMode? AC_CPDResEnergyTransferMode1,
                                           AC_CPDResEnergyTransferMode? AC_CPDResEnergyTransferMode2)

            => !(AC_CPDResEnergyTransferMode1 == AC_CPDResEnergyTransferMode2);

        #endregion

        #endregion

        #region IEquatable<AC_CPDResEnergyTransferMode> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two AC CPD response energy transfer modes for equality.
        /// </summary>
        /// <param name="Object">An AC CPD response energy transfer mode to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is AC_CPDResEnergyTransferMode ac_CPDResEnergyTransferMode &&
                   Equals(ac_CPDResEnergyTransferMode);

        #endregion

        #region Equals(AC_CPDResEnergyTransferMode)

        /// <summary>
        /// Compares two AC CPD response energy transfer modes for equality.
        /// </summary>
        /// <param name="AC_CPDResEnergyTransferMode">An AC CPD response energy transfer mode to compare with.</param>
        public Boolean Equals(AC_CPDResEnergyTransferMode? AC_CPDResEnergyTransferMode)

            => AC_CPDResEnergyTransferMode is not null &&

               EVSEMaximumChargePower.Equals(AC_CPDResEnergyTransferMode.EVSEMaximumChargePower) &&
               EVSEMinimumChargePower.Equals(AC_CPDResEnergyTransferMode.EVSEMinimumChargePower) &&
               EVSENominalFrequency.  Equals(AC_CPDResEnergyTransferMode.EVSENominalFrequency);




               // EVSEMaximumChargePower,
               // EVSEMinimumChargePower,
               // EVSENominalFrequency,
               // 
               // EVSEMaximumChargePower_L2
               // EVSEMaximumChargePower_L3
               // 
               // EVSEMinimumChargePower_L2
               // EVSEMinimumChargePower_L3
               // 
               // EVSEPresentActivePower   
               // EVSEPresentActivePower_L2
               // EVSEPresentActivePower_L3
               // 
               // MaximumPowerAsymmetry    
               // EVSEPowerRampLimitation  





               //SubCertificates.Count().Equals(AC_CPDResEnergyTransferMode.SubCertificates.Count()) &&
               //SubCertificates.All(subCertificate => AC_CPDResEnergyTransferMode.SubCertificates.Contains(subCertificate));

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

                return //Certificate.    GetHashCode()  * 5 ^
                       //SubCertificates.CalcHashCode() * 3 ^

                       base.           GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   //Certificate.ToString().SubstringMax(30),
                   ", ",

                   //SubCertificates.Count(),
                   " sub certificate(s)"

               );

        #endregion



    }

}
