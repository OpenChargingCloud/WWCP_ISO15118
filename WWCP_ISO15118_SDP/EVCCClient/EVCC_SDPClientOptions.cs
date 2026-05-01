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

#region Usings

using cloud.charging.open.protocols.ISO15118.SDP.Messages;
using cloud.charging.open.protocols.ISO15118.NetworkInterfaces;

#endregion

namespace cloud.charging.open.protocols.ISO15118.SDP.Client
{

    /// <summary>
    /// Configuration for <see cref="EVCC_SDPClient"/>.
    /// </summary>
    public sealed record EVCC_SDPClientOptions
    {

        /// <summary>
        /// The PLC interface to send the SDP_Request from.
        /// </summary>
        public required V2GNetworkInterface Interface { get; init; }


        /// <summary>
        /// What the EVCC requests in the SDP_Request. ISO 15118-20 mandates
        /// <see cref="SDP_Security.TLS"/>; -2 allows downgrade.
        /// </summary>
        public SDP_Security                  RequestedSecurity             { get; init; } = SDP_Security.TLS;

        /// <summary>Always TCP per the standard – exposed for pentest crafted requests.</summary>
        public SDP_TransportProtocol         RequestedTransport            { get; init; } = SDP_TransportProtocol.TCP;

        // ----- Retry / timeout knobs (ISO 15118-2 §8.4.2 timing) -----------

        /// <summary>
        /// Per ISO 15118-2 the EVCC waits ≤ <c>SDP_Request_Timeout</c> for a
        /// response and may then re-send. This library uses 250 ms by default,
        /// matching the spec's typical value.
        /// </summary>
        public TimeSpan                     PerAttemptTimeout              { get; init; } = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// Maximum number of SDP_Request emissions (initial + retries). The
        /// standard caps the total discovery phase by V2G_SECC_Sequence_Timeout
        /// (60 s) – this knob is the per-attempt counter.
        /// </summary>
        public Int32                        MaxRetries                     { get; init; } = 50;

        /// <summary>
        /// Hard upper bound on the entire discovery operation, covering all
        /// retries. Default 60 s mirrors V2G_SECC_Sequence_Timeout from -2.
        /// </summary>
        public TimeSpan                     TotalDeadline                  { get; init; } = TimeSpan.FromSeconds(60);



        // ----- Acceptance policy --------------------------------------------

        /// <summary>
        /// If <c>true</c>, the client will reject SDP_Response messages that
        /// advertise <see cref="SDP_Security.NoTLS"/>. CRA/NIS2 default.
        /// </summary>
        public Boolean                      RejectNoTlsResponses           { get; init; } = true;

        /// <summary>
        /// If <c>true</c>, the client will reject responses whose
        /// <c>SeccIPAddress</c> is not link-local. Defends against trivially
        /// spoofed responses pointing to off-link addresses.
        /// </summary>
        public Boolean                      RequireLinkLocalSeccAddress    { get; init; } = true;

        /// <summary>
        /// What to do when multiple SDP_Responses arrive for the same request.
        /// <see cref="DuplicateResponseStrategy.AcceptFirst"/> mirrors normal
        /// EVCC behaviour; the others are useful for debugging spoof scenarios.
        /// </summary>
        public DuplicateResponseStrategy    DuplicateStrategy              { get; init; } = DuplicateResponseStrategy.AcceptFirst;



        // ----- Pentest hooks ------------------------------------------------

        /// <summary>
        /// If set, called instead of the normal payload serializer to allow
        /// fuzzed/malformed SDP_Request bytes.
        /// </summary>
        public Func<SDP_Request, Byte[]>?    RawPayloadSerializer          { get; init; }

        /// <summary>
        /// If set, called for every received SDP_Response. Returning <c>false</c>
        /// makes the client ignore that response and continue waiting.
        /// </summary>
        public Func<SDP_Response, Boolean>?  ResponseFilter                { get; init; }

    }

}
