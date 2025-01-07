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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonTypes
{

    public abstract class ADynamic_CLResControlMode : ACLResControlMode
    {

        public DateTime?      DepartureTime    { get; }
        public PercentValue?  MinimumSOC       { get; }
        public PercentValue?  TargetSOC        { get; }
        public UInt16?        AckMaxDelay      { get; }


        #region Documentation

        // <xs:complexType name="Dynamic_CLResControlModeType" abstract="true">
        //     <xs:complexContent>
        //         <xs:extension base="CLResControlModeType">
        //             <xs:sequence>
        //                 <xs:element name="DepartureTime" type="xs:unsignedInt"   minOccurs="0"/>
        //                 <xs:element name="MinimumSOC"    type="percentValueType" minOccurs="0"/>
        //                 <xs:element name="TargetSOC"     type="percentValueType" minOccurs="0"/>
        //                 <xs:element name="AckMaxDelay"   type="xs:unsignedShort" minOccurs="0"/>
        //             </xs:sequence>
        //         </xs:extension>
        //     </xs:complexContent>
        // </xs:complexType>

        #endregion

    }

}
