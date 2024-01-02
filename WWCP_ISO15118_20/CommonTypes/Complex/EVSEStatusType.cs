/*
 * Copyright (c) 2021-2024 GraphDefined GmbH <achim.friedland@graphdefined.com>
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
    /// The EVSE status.
    /// </summary>
    public class EVSEStatusType
    {

        #region Properties

        /// <summary>
        /// The notification max delay.
        /// </summary>
        [Mandatory]
        public TimeSpan           NotificationMaxDelay    { get; }

        /// <summary>
        /// The EVSE notification.
        /// </summary>
        [Mandatory]
        public EVSENotifications  EVSENotification        { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new EVSE status.
        /// </summary>
        /// <param name="NotificationMaxDelay">A notification max delay.</param>
        /// <param name="EVSENotification">An EVSE notification.</param>
        public EVSEStatusType(TimeSpan           NotificationMaxDelay,
                              EVSENotifications  EVSENotification)
        {

            this.NotificationMaxDelay  = NotificationMaxDelay;
            this.EVSENotification      = EVSENotification;

        }

        #endregion


        #region Documentation

        // <xs:complexType name="EVSEStatusType">
        //     <xs:sequence>
        //         <xs:element name="NotificationMaxDelay" type="xs:unsignedShort"/>
        //         <xs:element name="EVSENotification"     type="evseNotificationType"/>
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

        #region (static) Parse   (JSON, CustomEVSEStatusTypeParser = null)

        /// <summary>
        /// Parse the given JSON representation of an EVSE status chain.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="CustomEVSEStatusTypeParser">A delegate to parse custom EVSE status chains.</param>
        public static EVSEStatusType Parse(JObject                                       JSON,
                                           CustomJObjectParserDelegate<EVSEStatusType>?  CustomEVSEStatusTypeParser   = null)
        {

            if (TryParse(JSON,
                         out var evseStatusType,
                         out var errorResponse,
                         CustomEVSEStatusTypeParser))
            {
                return evseStatusType!;
            }

            throw new ArgumentException("The given JSON representation of an EVSE status chain is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out EVSEStatusType, out ErrorResponse, CustomEVSEStatusTypeParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of an EVSE status chain.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EVSEStatusType">The parsed EVSE status chain.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject              JSON,
                                       out EVSEStatusType?  EVSEStatusType,
                                       out String?          ErrorResponse)

            => TryParse(JSON,
                        out EVSEStatusType,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an EVSE status chain.
        /// </summary>
        /// <param name="JSON">The JSON to be parsed.</param>
        /// <param name="EVSEStatusType">The parsed EVSE status chain.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomEVSEStatusTypeParser">A delegate to parse custom EVSE statuss.</param>
        public static Boolean TryParse(JObject                                       JSON,
                                       out EVSEStatusType?                           EVSEStatusType,
                                       out String?                                   ErrorResponse,
                                       CustomJObjectParserDelegate<EVSEStatusType>?  CustomEVSEStatusTypeParser)
        {

            try
            {

                EVSEStatusType = null;

                #region NotificationMaxDelay    [mandatory]

                if (!JSON.ParseMandatory("notificationMaxDelay",
                                         "notification max delay",
                                         out UInt64 notificationMaxDelay,
                                         out ErrorResponse))
                {
                    return false;
                }

                var NotificationMaxDelay = TimeSpan.FromSeconds(notificationMaxDelay);

                #endregion

                #region EVSENotification        [mandatory]

                if (!JSON.ParseMandatory("evseNotification",
                                         "EVSE notification",
                                         EVSENotificationsExtensions.TryParse,
                                         out EVSENotifications EVSENotification,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion


                EVSEStatusType = new EVSEStatusType(NotificationMaxDelay,
                                                    EVSENotification);

                if (CustomEVSEStatusTypeParser is not null)
                    EVSEStatusType = CustomEVSEStatusTypeParser(JSON,
                                                                EVSEStatusType);

                return true;

            }
            catch (Exception e)
            {
                EVSEStatusType  = null;
                ErrorResponse   = "The given JSON representation of an EVSE status chain is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(CustomEVSEStatusTypeSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomEVSEStatusTypeSerializer">A delegate to serialize custom EVSE status chains.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<EVSEStatusType>? CustomEVSEStatusTypeSerializer = null)
        {

            var json = JSONObject.Create(

                           new JProperty("notificationMaxDelay",  (UInt64) Math.Round(NotificationMaxDelay.TotalSeconds, 0)),
                           new JProperty("evseNotification",      EVSENotification.AsText())

                       );

            return CustomEVSEStatusTypeSerializer is not null
                       ? CustomEVSEStatusTypeSerializer(this, json)
                       : json;

        }

        #endregion


        #region Operator overloading

        #region Operator == (EVSEStatusType1, EVSEStatusType2)

        /// <summary>
        /// Compares two EVSE status chains for equality.
        /// </summary>
        /// <param name="EVSEStatusType1">An EVSE status chain.</param>
        /// <param name="EVSEStatusType2">Another EVSE status chain.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EVSEStatusType? EVSEStatusType1,
                                           EVSEStatusType? EVSEStatusType2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(EVSEStatusType1, EVSEStatusType2))
                return true;

            // If one is null, but not both, return false.
            if (EVSEStatusType1 is null || EVSEStatusType2 is null)
                return false;

            return EVSEStatusType1.Equals(EVSEStatusType2);

        }

        #endregion

        #region Operator != (EVSEStatusType1, EVSEStatusType2)

        /// <summary>
        /// Compares two EVSE status chains for inequality.
        /// </summary>
        /// <param name="EVSEStatusType1">An EVSE status chain.</param>
        /// <param name="EVSEStatusType2">Another EVSE status chain.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EVSEStatusType? EVSEStatusType1,
                                           EVSEStatusType? EVSEStatusType2)

            => !(EVSEStatusType1 == EVSEStatusType2);

        #endregion

        #endregion

        #region IEquatable<EVSEStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two EVSE status chains for equality.
        /// </summary>
        /// <param name="Object">An EVSE status chain to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EVSEStatusType evseStatusType &&
                   Equals(evseStatusType);

        #endregion

        #region Equals(EVSEStatusType)

        /// <summary>
        /// Compares two EVSE status chains for equality.
        /// </summary>
        /// <param name="EVSEStatusType">An EVSE status chain to compare with.</param>
        public Boolean Equals(EVSEStatusType? EVSEStatusType)

            => EVSEStatusType is not null &&

               NotificationMaxDelay.Equals(EVSEStatusType.NotificationMaxDelay) &&
               EVSENotification.    Equals(EVSEStatusType.EVSENotification);

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

                return NotificationMaxDelay.GetHashCode() * 5 ^
                       EVSENotification.    GetHashCode() * 3 ^

                       base.                GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   NotificationMaxDelay.TotalSeconds,
                   " second(s), ",

                   EVSENotification.AsText()

               );

        #endregion

    }

}
