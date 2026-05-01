/*
 * Copyright (c) 2014-2025 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

namespace cloud.charging.open.protocols.ISO15118.SLAC.Messages
{

    /// <summary>
    /// Outcome of a CM_VALIDATE round.
    /// </summary>
    public enum ValidateResult : byte
    {

        /// <summary>
        /// EVSE is ready to start counting toggles, EV should begin toggling.
        /// </summary>
        Ready    = 0x00,

        /// <summary>
        /// Counter values agreed; validation passed.
        /// </summary>
        Success  = 0x01,

        /// <summary>
        /// Counter values disagreed or timeout exceeded; validation failed.
        /// </summary>
        Failure  = 0x02,

        /// <summary>
        /// EVSE is not yet ready (still configuring); EV should wait.
        /// </summary>
        NotReady = 0x03

    }

}
