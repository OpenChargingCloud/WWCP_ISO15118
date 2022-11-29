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

using cloud.charging.open.protocols.ISO15118_20.V2gciCt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.TnsC
{

    public class SignedInstallationDataType
    {

        public Byte[]? SECP521_EncryptedPrivateKey    { get; }
        public Byte[]? X448_EncryptedPrivateKey       { get; }
        public Byte[]? TPM_EncryptedPrivateKey        { get; }

        //<?xml version="1.0" encoding="utf-8"?>
        //<ns:SignedInstallationData xmlns:v2gci_ct="urn:iso:std:iso:15118:-20:CommonTypes"
        //                           xmlns:xmlsig="http://www.w3.org/2000/09/xmldsig#"
        //                           xmlns:ns="urn:iso:std:iso:15118:-20:CommonMessages"
        //                           xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
        //                           xsi:schemaLocation="urn:iso:std:iso:15118:-20:CommonMessages file:///V2G_CI_CommonMessages.xsd" ns:Id="AAAAA">
        //
        //    <ns:ContractCertificateChain>
        //        <ns:Certificate>YTM0NZomIzI2OTsmIzM0NTueYQ==</ns:Certificate>
        //        <ns:SubCertificates>
        //            <ns:Certificate>YTM0NZomIzI2OTsmIzM0NTueYQ==</ns:Certificate>
        //        </ns:SubCertificates>
        //    </ns:ContractCertificateChain>
        //
        //    <ns:ECDHCurve>SECP521</ns:ECDHCurve>
        //    <ns:DHPublicKey>YTM0NZomIzI2OTsmIzM0NTueYQ==</ns:DHPublicKey>
        //    <ns:X448_EncryptedPrivateKey>YTM0NZomIzI2OTsmIzM0NTueYQ==</ns:X448_EncryptedPrivateKey>
        //
        //</ns:SignedInstallationData>

    }

}
