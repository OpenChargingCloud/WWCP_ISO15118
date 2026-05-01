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

using System.Net;

using cloud.charging.open.protocols.ISO15118.SDP.Messages;
using cloud.charging.open.protocols.ISO15118.NetworkInterfaces;

#endregion

namespace cloud.charging.open.protocols.ISO15118.SDP.Server;

/// <summary>
/// Configuration for <see cref="SECC_SDPServer"/>. All policy knobs are
/// configurable on purpose – this library is intended to support
/// pentest scenarios as well as standards-compliant deployments.
/// </summary>
public sealed record SECC_SDPServerOptions
{

    /// <summary>
    /// The PLC interface to listen on.
    /// </summary>
    public required V2GNetworkInterface Interface { get; init; }



    /// <summary>
    /// IPv6 address the EVCC should connect to. Defaults to the interface's
    /// link-local address. Override for spoof tests / multi-homed setups.
    /// </summary>
    public IPAddress?                              SeccIPAddressOverride    { get; init; }

    /// <summary>TCP port the V2G server (SAP/EXI handler) is listening on.</summary>
    public required UInt16 SeccPort { get; init; }



    // ----- Acceptance policy --------------------------------------------

    /// <summary>
    /// Which 15118 revisions this SECC is willing to discover for. Default:
    /// both -2 and -20. The version is negotiated via SAP later, but for
    /// pentest purposes it is useful to limit which discovery messages the
    /// SECC will respond to.
    /// </summary>
    public IReadOnlySet<SDP_Version>                AcceptedVersions         { get; init; }

        = new HashSet<SDP_Version> {
              SDP_Version.ISO_15118_2,
              SDP_Version.ISO_15118_20
          };

    /// <summary>
    /// What the SECC offers as its security mode. For 15118-20-compliant
    /// operation this must be <see cref="SDP_Security.TLS"/>. For pentest
    /// scenarios the operator may explicitly want to advertise no-TLS.
    /// </summary>
    public SDP_Security                             OfferedSecurity          { get; init; } = SDP_Security.TLS;

    /// <summary>
    /// If <c>true</c>, the SECC silently drops SDP_Request messages that ask
    /// for <see cref="SDP_Security.NoTLS"/>. CRA/NIS2-compliant default.
    /// Set to <c>false</c> to allow downgrade for pentest scenarios.
    /// </summary>
    public Boolean                                 RejectNoTlsRequests      { get; init; } = true;



    // ----- Pentest hooks ------------------------------------------------

    /// <summary>
    /// If set, called for every incoming SDP_Request *before* the server
    /// decides to respond. Returning <c>false</c> drops the request silently.
    /// Useful for scripted pentest scenarios ("only respond on every 3rd
    /// request", "respond only to requests with certain remote addresses").
    /// </summary>
    public Func<IPEndPoint, SDP_Request, Boolean>?  RequestFilter            { get; init; }

    /// <summary>
    /// If set, the server hands the outgoing SDP_Response through this hook
    /// before serialization, so pentest scripts can mutate fields (wrong
    /// address, wrong port, downgrade Security…).
    /// </summary>
    public Func<SDP_Response, SDP_Response>?         ResponseTransformer      { get; init; }

    /// <summary>
    /// If set, called instead of the normal payload serializer. Lets the
    /// caller hand-craft completely arbitrary V2GTP payload bytes
    /// (malformed-frame fuzzing).
    /// </summary>
    public Func<SDP_Response, Byte[]>?              RawPayloadSerializer     { get; init; }

    /// <summary>
    /// Inject artificial response delay – useful to test EVCC retry/timeout
    /// behaviour (V2G_SECC_Sequence_Timeout, SDP_Request retry counter).
    /// </summary>
    public TimeSpan                                ResponseDelay            { get; init; } = TimeSpan.Zero;

    /// <summary>
    /// If <c>true</c>, the server emits N copies of the SDP_Response.
    /// Default 1; increase to test EVCC behaviour against duplicate
    /// responses (race-to-match style attack from a malicious second SECC).
    /// </summary>
    public Int32                                   ResponseDuplicates       { get; init; } = 1;

    /// <summary>
    /// If <c>true</c>, the server will *also* emit responses to non-multicast
    /// SDP_Request packets (i.e. unicast SDP probing). Off by default.
    /// </summary>
    public Boolean                                 AnswerUnicastRequests    { get; init; } = false;

}
