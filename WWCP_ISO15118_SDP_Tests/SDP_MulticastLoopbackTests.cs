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

#region Usings

using NUnit.Framework;

using cloud.charging.open.protocols.ISO15118.SDP.Client;
using cloud.charging.open.protocols.ISO15118.SDP.Server;

#endregion

namespace cloud.charging.open.protocols.ISO15118.SDP.Tests
{

    /// <summary>
    /// Hearing yourself is off on both sides, and stays off.
    /// </summary>
    /// <remarks>
    /// One assertion per side, and it is the one that matters: a SECC that
    /// accepted multicast from its own machine by default would answer the
    /// simulator somebody left running on the same host, and an EVCC that
    /// looped its own requests back would find whatever is on its own bench
    /// before it found the station it is plugged into. Both defaults are the
    /// real-hardware case, where the two are separate nodes.
    ///
    /// What cannot be pinned here is the other half - that turning it on makes
    /// a single-host bench work. That needs two sockets, a real interface and
    /// a multicast group, which is not a thing to do in a test suite that
    /// otherwise binds nothing at all; nothing else in this repository does.
    /// It was measured by hand instead, on Windows 11 over ff02::1, and the
    /// numbers are in the remark on SECC_SDPServerOptions.MulticastLoopback -
    /// including the part that makes this an option rather than a constant:
    /// the two platforms disagree about which socket the switch belongs to.
    /// </remarks>
    [TestFixture]
    public class SDP_MulticastLoopbackTests
    {

        #region ASECCDoesNotHearItselfUnlessAsked()

        [Test]
        public void ASECCDoesNotHearItselfUnlessAsked()
        {
            Assert.That(new SECC_SDPServerOptions {
                            Interface  = null!,
                            SeccPort   = 15118
                        }.MulticastLoopback,
                        Is.False,
                        "A SECC would answer anything running on its own machine.");
        }

        #endregion

        #region AnEVCCDoesNotHearItselfUnlessAsked()

        [Test]
        public void AnEVCCDoesNotHearItselfUnlessAsked()
        {
            Assert.That(new EVCC_SDPClientOptions {
                            Interface  = null!
                        }.MulticastLoopback,
                        Is.False,
                        "A vehicle would discover whatever is on its own bench.");
        }

        #endregion

        #region AskingForItIsWhatTurnsItOn()

        /// <summary>
        /// And it is the same word on both sides, because a bench has to set
        /// both: which of the two actually decides depends on the platform.
        /// </summary>
        [Test]
        public void AskingForItIsWhatTurnsItOn()
        {

            Assert.Multiple(() => {

                Assert.That(new SECC_SDPServerOptions {
                                Interface          = null!,
                                SeccPort           = 15118,
                                MulticastLoopback  = true
                            }.MulticastLoopback,
                            Is.True);

                Assert.That(new EVCC_SDPClientOptions {
                                Interface          = null!,
                                MulticastLoopback  = true
                            }.MulticastLoopback,
                            Is.True);

            });

        }

        #endregion

    }

}
