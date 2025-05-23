﻿/*
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

using cloud.charging.open.protocols.ISO15118_20.CommonTypes;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.WPT
{

    public class WPT_ChargeLoopRequest : AChargeLoopRequest
    {

        public WPT_EVResultTypes                   EVResultCode                         { get; }
        public RationalNumber                      EVPCPowerOutput                      { get; }
        public WPT_EVPCChargeDiagnosticsTypes      EVPCChargeDiagnostics                { get; }
        public RationalNumber?                     EVPCOperatingFrequency               { get; }

        public WPT_EVPCPowerControlParameterType?  EVPCPowerControlParameter            { get; }

        public IEnumerable<Byte[]>                 ManufacturerSpecificDataContainer    { get; }


        public WPT_ChargeLoopRequest(MessageHeader                       MessageHeader,
                                     Boolean                             MeterInfoRequested,
                                     DisplayParameters?                  DisplayParameters,

                                     WPT_EVResultTypes                   EVResultCode,
                                     RationalNumber                      EVPCPowerOutput,
                                     WPT_EVPCChargeDiagnosticsTypes      EVPCChargeDiagnostics,
                                     RationalNumber?                     EVPCOperatingFrequency,
                                     WPT_EVPCPowerControlParameterType?  EVPCPowerControlParameter,
                                     IEnumerable<Byte[]>                 ManufacturerSpecificDataContainer)

            : base(MessageHeader,
                   MeterInfoRequested,
                   DisplayParameters)

        {

            this.EVResultCode                       = EVResultCode;
            this.EVPCPowerOutput                    = EVPCPowerOutput;
            this.EVPCChargeDiagnostics              = EVPCChargeDiagnostics;
            this.EVPCOperatingFrequency             = EVPCOperatingFrequency;
            this.EVPCPowerControlParameter          = EVPCPowerControlParameter;
            this.ManufacturerSpecificDataContainer  = ManufacturerSpecificDataContainer;

        }

    }

}
