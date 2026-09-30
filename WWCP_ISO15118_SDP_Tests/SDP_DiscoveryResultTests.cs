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

using System.Net;

using NUnit.Framework;

using cloud.charging.open.protocols.ISO15118.SDP.Client;
using cloud.charging.open.protocols.ISO15118.SDP.Messages;

#endregion

namespace cloud.charging.open.protocols.ISO15118.SDP.Tests
{

    /// <summary>
    /// Every answer a discovery heard keeps where it came from, and not only
    /// the first one.
    /// </summary>
    /// <remarks>
    /// The answers are handed to the client's Found and Refused as it would
    /// have collected them, rather than heard on a socket: nothing in this
    /// suite binds one - see <see cref="SDP_MulticastLoopbackTests"/>.
    /// </remarks>
    [TestFixture]
    public class SDP_DiscoveryResultTests
    {

        #region Data

        private static readonly SDP_Response  first       = new (IPAddress.Parse("fe80::1"), 64109, SDP_Security.TLS,   SDP_TransportProtocol.TCP);
        private static readonly SDP_Response  second      = new (IPAddress.Parse("fe80::2"), 64110, SDP_Security.TLS,   SDP_TransportProtocol.TCP);
        private static readonly SDP_Response  third       = new (IPAddress.Parse("fe80::3"), 64111, SDP_Security.NoTLS, SDP_TransportProtocol.TCP);

        private static readonly IPEndPoint    fromFirst   = new (IPAddress.Parse("fe80::a"), 15118);
        private static readonly IPEndPoint    fromSecond  = new (IPAddress.Parse("fe80::b"), 15118);
        private static readonly IPEndPoint    fromThird   = new (IPAddress.Parse("fe80::c"), 15118);

        private const           String        noTLS       = "no-TLS response rejected by policy (RejectNoTlsResponses=true)";
        private const           String        filtered    = "rejected by ResponseFilter";

        #endregion


        #region EveryOtherStationKeepsWhereItsAnswerCameFrom()

        /// <summary>
        /// The first answer is the one to use; the others are the other
        /// stations on the link, and each of them is said with its sender, in
        /// the order they answered.
        /// </summary>
        [Test]
        public void EveryOtherStationKeepsWhereItsAnswerCameFrom()
        {

            var found = EVCC_SDPClient.Found([ (first, fromFirst), (second, fromSecond), (third, fromThird) ],
                                             1,
                                             TimeSpan.FromMilliseconds(250));

            Assert.Multiple(() => {
                Assert.That(found.Response,                   Is.EqualTo(first));
                Assert.That(found.RemoteEndpoint,             Is.EqualTo(fromFirst));
                Assert.That(found.AdditionalResponses,        Is.EqualTo(new[] { second,     third     }));
                Assert.That(found.AdditionalRemoteEndpoints,  Is.EqualTo(new[] { fromSecond, fromThird }));
                Assert.That(found.Attempts,                   Is.EqualTo(1));
            });

        }

        #endregion

        #region ASingleStationHasNoOthers()

        [Test]
        public void ASingleStationHasNoOthers()
        {

            var found = EVCC_SDPClient.Found([ (first, fromFirst) ],
                                             1,
                                             TimeSpan.FromMilliseconds(250));

            Assert.Multiple(() => {
                Assert.That(found.RemoteEndpoint,             Is.EqualTo(fromFirst));
                Assert.That(found.AdditionalResponses,        Is.Empty);
                Assert.That(found.AdditionalRemoteEndpoints,  Is.Empty);
            });

        }

        #endregion

        #region EveryRefusedAnswerKeepsWhereItCameFrom()

        /// <summary>
        /// Answers that were all refused say why, and who sent them - the one
        /// thing that tells two misconfigured stations on one link apart.
        /// </summary>
        [Test]
        public void EveryRefusedAnswerKeepsWhereItCameFrom()
        {

            var refused = EVCC_SDPClient.Refused([ (third, fromThird, noTLS),
                                                   (first, fromFirst, filtered) ],
                                                 3,
                                                 TimeSpan.FromMilliseconds(750));

            Assert.Multiple(() => {
                Assert.That(refused.RejectedResponses,        Is.EqualTo(new[] { (third, noTLS), (first, filtered) }));
                Assert.That(refused.RejectedRemoteEndpoints,  Is.EqualTo(new[] { fromThird, fromFirst }));
                Assert.That(refused.Attempts,                 Is.EqualTo(3));
            });

        }

        #endregion

        #region AStationAskedAgainIsRefusedOnce()

        /// <summary>
        /// A station that answers every request with what is refused is one
        /// refused answer, not one per request - said where it was first heard.
        /// </summary>
        [Test]
        public void AStationAskedAgainIsRefusedOnce()
        {

            var refused = EVCC_SDPClient.Refused([ (third, fromThird, noTLS),
                                                   (third, fromThird, noTLS),
                                                   (first, fromFirst, filtered),
                                                   (third, fromThird, noTLS) ],
                                                 15,
                                                 TimeSpan.FromMilliseconds(3883));

            Assert.Multiple(() => {
                Assert.That(refused.RejectedResponses,        Is.EqualTo(new[] { (third, noTLS), (first, filtered) }));
                Assert.That(refused.RejectedRemoteEndpoints,  Is.EqualTo(new[] { fromThird, fromFirst }));
                Assert.That(refused.Attempts,                 Is.EqualTo(15));
            });

        }

        #endregion

        #region WhatDiffersIsKept()

        /// <summary>
        /// Only the same answer from the same sender for the same reason is
        /// one. The same answer from another sender is another station, another
        /// answer from the same sender is something else it said, and another
        /// reason for the same answer says something new.
        /// </summary>
        [Test]
        public void WhatDiffersIsKept()
        {

            var refused = EVCC_SDPClient.Refused([ (third, fromThird,  noTLS),
                                                   (third, fromSecond, noTLS),
                                                   (first, fromThird,  filtered),
                                                   (third, fromThird,  filtered) ],
                                                 2,
                                                 TimeSpan.FromMilliseconds(500));

            Assert.Multiple(() => {
                Assert.That(refused.RejectedResponses,        Is.EqualTo(new[] { (third, noTLS), (third, noTLS), (first, filtered), (third, filtered) }));
                Assert.That(refused.RejectedRemoteEndpoints,  Is.EqualTo(new[] { fromThird, fromSecond, fromThird, fromThird }));
            });

        }

        #endregion

        #region AResultMadeElsewhereSaysNoSenders()

        /// <summary>
        /// Somebody who makes a result of their own, as they could before the
        /// senders were kept, still can - and says none.
        /// </summary>
        [Test]
        public void AResultMadeElsewhereSaysNoSenders()
        {

            var found    = new SDP_DiscoverySuccess {
                               Response             = first,
                               RemoteEndpoint       = fromFirst,
                               Attempts             = 1,
                               Elapsed              = TimeSpan.Zero,
                               AdditionalResponses  = [ second ]
                           };

            var refused  = new SDP_DiscoveryRejected {
                               Attempts           = 1,
                               Elapsed            = TimeSpan.Zero,
                               RejectedResponses  = [ (second, "SECC port is zero") ]
                           };

            Assert.Multiple(() => {
                Assert.That(found.  AdditionalRemoteEndpoints,  Is.Empty);
                Assert.That(refused.RejectedRemoteEndpoints,    Is.Empty);
            });

        }

        #endregion

    }

}
