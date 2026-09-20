/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP ISO/IEC 15118 <https://github.com/OpenChargingCloud/WWCP_ISO15118>
 *
 * Licensed under the Affero GPL license, Version 3.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.gnu.org/licenses/agpl.html
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace cloud.charging.open.protocols.ISO15118.PKI;

/// <summary>
/// How many trust anchors a hierarchy has above its branches.
/// </summary>
/// <remarks>
/// The difference matters to whoever keeps the roots apart by what they vouch
/// for. A vehicle that believes one bag of roots cannot tell an OEM root
/// vouching for a contract from an MO root doing so; a vehicle that keeps a
/// v2gRoot, an moRoot and an oemRoot each in its own slot can, and needs a
/// hierarchy that has the three to fill them with.
/// </remarks>
public enum V2GRootLayout
{

    /// <summary>
    /// One V2G Root CA above every branch: CPO, MO, OEM, Vehicle and CPS. The
    /// ISO 15118-2 Annex H picture, and the default.
    /// </summary>
    SingleRoot,

    /// <summary>
    /// A V2G Root CA above the CPO and CPS branches, an MO Root CA above the
    /// MO branch, and an OEM Root CA above the OEM and Vehicle branches - the
    /// three anchors ISO 15118-20 Annex C names, each self-signed and each
    /// vouching for one thing.
    /// </summary>
    SeparateRoots

}
