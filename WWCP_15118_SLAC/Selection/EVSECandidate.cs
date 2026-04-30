/*
 * Copyright (c) 2021-2025 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP ISO/IEC 15118 <https://github.com/OpenChargingCloud/WWCP_ISO15118>
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

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

using cloud.charging.open.protocols.ISO15118.SLAC.Messages;

#endregion

namespace cloud.charging.open.protocols.ISO15118.SLAC.Selection
{

    /// <summary>
    /// Information collected by the EV about one candidate EVSE during a SLAC
    /// matching attempt. A candidate is created when the EV receives a
    /// CM_SLAC_PARM.CNF from that EVSE; its <see cref="AttenChar"/> is filled in
    /// later when (and if) the corresponding CM_ATTEN_CHAR.IND arrives.
    /// </summary>
    /// <param name="EVSEMACAddress">L2 source MAC of this EVSE.</param>
    /// <param name="ParmCnf">The CM_SLAC_PARM.CNF this EVSE replied with.</param>
    /// <param name="AttenChar">
    /// The CM_ATTEN_CHAR.IND this EVSE broadcast (after sounding). Null if this
    /// EVSE did not produce an attenuation profile within the collection window.
    /// </param>
    public sealed record EVSECandidate(MACAddress     EVSEMACAddress,
                                       SLACParmCnf    ParmCnf,
                                       AttenCharInd?  AttenChar)
    {

        /// <summary>
        /// The 17-byte EVSE_ID, taken from the AttenChar's SourceId, or null.
        /// </summary>
        public Byte[]?  EvseId
            => AttenChar?.SourceId;

        /// <summary>
        /// True if this candidate produced an attenuation profile.
        /// </summary>
        public Boolean  HasAttenuationProfile
            => AttenChar is not null;

        /// <summary>
        /// The attenuation profile, or null if the EVSE did not respond with one.
        /// </summary>
        public Byte[]? AttenuationProfile
            => AttenChar?.AttenProfile;

        /// <summary>
        /// Average attenuation in dB across all carrier groups, or null if no profile
        /// has been received. Lower values mean the EV's signals reach this EVSE with
        /// less attenuation — i.e. this is the EVSE the EV is most likely physically
        /// plugged into.
        /// </summary>
        public Double? AverageAttenuation
        {
            get
            {

                if (AttenChar is null || AttenChar.AttenProfile.Length == 0)
                    return null;

                Int64 sum = 0;
                foreach (var b in AttenChar.AttenProfile)
                    sum += b;

                return (Double) sum / AttenChar.AttenProfile.Length;

            }
        }

    }

}
