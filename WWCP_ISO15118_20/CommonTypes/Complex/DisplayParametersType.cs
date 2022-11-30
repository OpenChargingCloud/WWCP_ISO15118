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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.CommonTypes
{

    public class DisplayParametersType
    {

        public PercentValueType?    PresentSOC                   { get; }
        public PercentValueType?    MinimumSOC                   { get; }
        public PercentValueType?    TargetSOC                    { get; }
        public PercentValueType?    MaximumSOC                   { get; }
        public UInt32?              RemainingTimeToMinimumSOC    { get; }
        public UInt32?              RemainingTimeToTargetSOC     { get; }
        public UInt32?              RemainingTimeToMaximumSOC    { get; }
        public Boolean?             ChargingComplete             { get; }
        public RationalNumberType?  BatteryEnergyCapacity        { get; }
        public Boolean?             InletHot                     { get; }


    }

}
