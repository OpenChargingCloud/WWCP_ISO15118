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

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{

    public class Dynamic_SEResControlModeType : ScheduleExchangeResponse,
                                                IEquatable<Dynamic_SEResControlModeType>
    {

        #region Properties

        public DateTime?               DepartureTime            { get; }
        public PercentValue?           MinimumSOC               { get; }
        public PercentValue?           TargetSOC                { get; }


        // Choose one of the following...
        public AbsolutePriceSchedule?  AbsolutePriceSchedule    { get; }
        public PriceLevelSchedule?     PriceLevelSchedule       { get; }

        #endregion

        #region Constructor(s)

        #region (private) Dynamic_SEResControlModeType(DepartureTime, MinimumSOC, TargetSOC, AbsolutePriceSchedule, PriceLevelSchedule)

        private Dynamic_SEResControlModeType(DateTime?               DepartureTime,
                                             PercentValue?           MinimumSOC,
                                             PercentValue?           TargetSOC,
                                             AbsolutePriceSchedule?  AbsolutePriceSchedule,
                                             PriceLevelSchedule?     PriceLevelSchedule)
        {

            this.DepartureTime          = DepartureTime;
            this.MinimumSOC             = MinimumSOC;
            this.TargetSOC              = TargetSOC;
            this.AbsolutePriceSchedule  = AbsolutePriceSchedule;
            this.PriceLevelSchedule     = PriceLevelSchedule;

        }

        #endregion

        #region (public) Dynamic_SEResControlModeType(DepartureTime = null, MinimumSOC = null, TargetSOC = null)

        public Dynamic_SEResControlModeType(DateTime?      DepartureTime   = null,
                                            PercentValue?  MinimumSOC      = null,
                                            PercentValue?  TargetSOC       = null)
        {

            this.DepartureTime  = DepartureTime;
            this.MinimumSOC     = MinimumSOC;
            this.TargetSOC      = TargetSOC;

        }

        #endregion

        #region (public) Dynamic_SEResControlModeType(DepartureTime, MinimumSOC, TargetSOC, AbsolutePriceSchedule)

        public Dynamic_SEResControlModeType(AbsolutePriceSchedule  AbsolutePriceSchedule,
                                            DateTime?              DepartureTime   = null,
                                            PercentValue?          MinimumSOC      = null,
                                            PercentValue?          TargetSOC       = null)
        {

            this.DepartureTime          = DepartureTime;
            this.MinimumSOC             = MinimumSOC;
            this.TargetSOC              = TargetSOC;
            this.AbsolutePriceSchedule  = AbsolutePriceSchedule;

        }

        public Dynamic_SEResControlModeType(PriceLevelSchedule  PriceLevelSchedule,
                                            DateTime?           DepartureTime   = null,
                                            PercentValue?       MinimumSOC      = null,
                                            PercentValue?       TargetSOC       = null)
        {

            this.DepartureTime          = DepartureTime;
            this.MinimumSOC             = MinimumSOC;
            this.TargetSOC              = TargetSOC;
            this.PriceLevelSchedule     = PriceLevelSchedule;

        }


        #endregion

        #endregion


        #region Documentation

        // <xs:complexType name="Dynamic_SEResControlModeType">
        //     <xs:sequence>
        //
        //         <xs:element name="DepartureTime" type="xs:unsignedInt"            minOccurs="0"/>
        //         <xs:element name="MinimumSOC"    type="v2gci_ct:percentValueType" minOccurs="0"/>
        //         <xs:element name="TargetSOC"     type="v2gci_ct:percentValueType" minOccurs="0"/>
        //
        //         <xs:choice minOccurs="0">
        //             <xs:element name="AbsolutePriceSchedule" type="AbsolutePriceScheduleType"/>
        //             <xs:element name="PriceLevelSchedule" type="PriceLevelScheduleType"/>
        //         </xs:choice>
        //
        //     </xs:sequence>
        // </xs:complexType>

        #endregion

    }

}
