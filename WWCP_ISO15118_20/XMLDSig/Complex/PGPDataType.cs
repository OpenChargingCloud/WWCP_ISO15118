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

using System.Xml.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.ISO15118_20.XMLDSig
{

    public abstract class APGPDataType
    {

    }


    public class PGPDataType1 : APGPDataType
    {

        [Mandatory]
        public Byte[]                 PGPKeyID        { get; }

        [Optional]
        public Byte[]?                PGPKeyPacket    { get; }

        [Optional]
        public IEnumerable<XElement>  AnyElement      { get; }


        // <?xml version="1.0" encoding="utf-8"?>
        // <ds:PGPData xmlns:ds           = "http://www.w3.org/2000/09/xmldsig#"
        //             xmlns:xsi          = "http://www.w3.org/2001/XMLSchema-instance"
        //             xsi:schemaLocation = "http://www.w3.org/2000/09/xmldsig# file://xmldsig-core-schema.xsd">
        //
        //     <ds:PGPKeyID>YTM0NZomIzI2OTsmIzM0NTueYQ==</ds:PGPKeyID>
        //     <ds:PGPKeyPacket>YTM0NZomIzI2OTsmIzM0NTueYQ==</ds:PGPKeyPacket>
        //
        // </ds:PGPData>

    }

    public class PGPDataType2 : APGPDataType
    {

        [Mandatory]
        public Byte[]                 PGPKeyPacket    { get; }

        [Optional]
        public IEnumerable<XElement>  AnyElement      { get; }

    }







}
