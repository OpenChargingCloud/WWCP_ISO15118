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

using cloud.charging.open.protocols.ISO15118_20.CommonTypes;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.WPT
{
    public class WPT_ChargeParameterDiscoveryResponse : AChargeParameterDiscoveryResponse
    {

        public WPT_PowerClassTypes  PDInputPowerClass                      { get; }
        public RationalNumber       SDMinOutputPower                       { get; }
        public RationalNumber       SDMaxOutputPower                       { get; }
        public UInt16               SDMaxGroundClearanceSupport            { get; }
        public UInt16               SDMinGroundClearanceSupport            { get; }
        public RationalNumber       PDMinCoilCurrent                       { get; }
        public RationalNumber       PDMaxCoilCurrent                       { get; }
        public IEnumerable<Byte[]>  SDManufacturerSpecificDataContainer    { get; }


        public WPT_ChargeParameterDiscoveryResponse(AChargeParameterDiscoveryRequest  Request,
                                                    MessageHeader                     MessageHeader,
                                                    ResponseCodes                     ResponseCode,

                                                    WPT_PowerClassTypes               PDInputPowerClass,
                                                    RationalNumber                    SDMinOutputPower,
                                                    RationalNumber                    SDMaxOutputPower,
                                                    UInt16                            SDMaxGroundClearanceSupport,
                                                    UInt16                            SDMinGroundClearanceSupport,
                                                    RationalNumber                    PDMinCoilCurrent,
                                                    RationalNumber                    PDMaxCoilCurrent,
                                                    IEnumerable<Byte[]>               SDManufacturerSpecificDataContainer)

            : base (Request,
                    MessageHeader,
                    ResponseCode)

        {

            this.PDInputPowerClass                    = PDInputPowerClass;
            this.SDMinOutputPower                     = SDMinOutputPower;
            this.SDMaxOutputPower                     = SDMaxOutputPower;
            this.SDMaxGroundClearanceSupport          = SDMaxGroundClearanceSupport;
            this.SDMinGroundClearanceSupport          = SDMinGroundClearanceSupport;
            this.PDMinCoilCurrent                     = PDMinCoilCurrent;
            this.PDMaxCoilCurrent                     = PDMaxCoilCurrent;
            this.SDManufacturerSpecificDataContainer  = SDManufacturerSpecificDataContainer;

        }

        public override Boolean Equals(AChargeParameterDiscoveryResponse? AV2GResponse)
        {
            throw new NotImplementedException();
        }

    }

}
