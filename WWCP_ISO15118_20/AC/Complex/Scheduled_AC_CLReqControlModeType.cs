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

using cloud.charging.open.protocols.ISO15118_20.CommonTypes;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.AC
{

    public class Scheduled_AC_CLReqControlModeType : AScheduled_CLReqControlModeType
    {

        public RationalNumber?  EVMaximumChargePower         { get; }
        public RationalNumber?  EVMaximumChargePower_L2      { get; }
        public RationalNumber?  EVMaximumChargePower_L3      { get; }

        public RationalNumber?  EVMinimumChargePower         { get; }
        public RationalNumber?  EVMinimumChargePower_L2      { get; }
        public RationalNumber?  EVMinimumChargePower_L3      { get; }

        public RationalNumber   EVPresentActivePower         { get; }
        public RationalNumber?  EVPresentActivePower_L2      { get; }
        public RationalNumber?  EVPresentActivePower_L3      { get; }

        public RationalNumber?  EVPresentReactivePower       { get; }
        public RationalNumber?  EVPresentReactivePower_L2    { get; }
        public RationalNumber?  EVPresentReactivePower_L3    { get; }


    }

}
