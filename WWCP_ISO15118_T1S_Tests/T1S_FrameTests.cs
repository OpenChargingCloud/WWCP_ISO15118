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

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

using cloud.charging.open.protocols.ISO15118.T1S.Messages;

#endregion

namespace cloud.charging.open.protocols.ISO15118.T1S.Tests
{

    /// <summary>
    /// The Ethernet framing and the message codec: every message survives the
    /// wire unchanged, and nothing malformed gets past
    /// <see cref="T1SCodec.TryDecode(ReadOnlySpan{Byte})"/> as anything but
    /// null - which matters more than the happy path, because every datagram
    /// off a shared medium meets it first.
    /// </summary>
    [TestFixture]
    public class T1S_FrameTests
    {

        #region (private static) Data

        private static readonly MACAddress  evse  = MACAddress.Parse("02:71:50:00:00:01");
        private static readonly MACAddress  ev    = MACAddress.Parse("02:71:50:00:00:02");

        private static IT1SMessage RoundTrip(IT1SMessage Message)
        {

            var frame    = T1SCodec.Frame(ev, evse, Message);
            var bytes    = frame.Encode();
            var decoded  = EthernetFrame.TryDecode(bytes);

            Assert.That(decoded, Is.Not.Null, "The frame did not decode.");
            Assert.That(decoded!.Destination, Is.EqualTo(ev));
            Assert.That(decoded.Source,       Is.EqualTo(evse));
            Assert.That(decoded.EtherType,    Is.EqualTo(T1SConstants.EtherType));

            var message = T1SCodec.TryDecode(decoded);

            Assert.That(message, Is.Not.Null, $"The {Message.Type} did not decode.");

            return message!;

        }

        #endregion


        #region TheHeaderIsEthernetII()

        [Test]
        public void TheHeaderIsEthernetII()
        {

            var bytes = T1SCodec.Frame(ev, evse, new Beacon(1, 2)).Encode();

            Assert.Multiple(() => {
                Assert.That(bytes.Length,           Is.EqualTo(EthernetFrame.HeaderLength + 6));
                Assert.That(bytes[..6],             Is.EqualTo(ev.  GetBytes()), "destination first");
                Assert.That(bytes[6..12],           Is.EqualTo(evse.GetBytes()), "then source");
                Assert.That(bytes[12],              Is.EqualTo(0x88),            "EtherType big-endian");
                Assert.That(bytes[13],              Is.EqualTo(0xB5));
                Assert.That(bytes[14],              Is.EqualTo((Byte) T1SMessageType.Beacon));
            });

        }

        #endregion

        #region EveryMessageSurvivesTheWire()

        [Test]
        public void EveryMessageSurvivesTheWire()
        {

            Assert.Multiple(() => {

                Assert.That(RoundTrip(new Beacon(0xDEADBEEF, 7)),
                            Is.EqualTo(new Beacon(0xDEADBEEF, 7)));

                Assert.That(RoundTrip(new TransmitOpportunity(42, 3, 5)),
                            Is.EqualTo(new TransmitOpportunity(42, 3, 5)));

                Assert.That(RoundTrip(new TransmitOpportunity(42, 6, T1SConstants.UnassignedNodeId)),
                            Is.EqualTo(new TransmitOpportunity(42, 6, 255)));

                Assert.That(RoundTrip(new Yield(42, 5)),
                            Is.EqualTo(new Yield(42, 5)));

                Assert.That(RoundTrip(new Join(T1SNodeRole.Vehicle, 0x01020304, 3, "Truck")),
                            Is.EqualTo(new Join(T1SNodeRole.Vehicle, 0x01020304, 3, "Truck")));

                Assert.That(RoundTrip(new Assign(0x01020304, 1, T1SNodeRole.Vehicle, 3)),
                            Is.EqualTo(new Assign(0x01020304, 1, T1SNodeRole.Vehicle, 3)));

                Assert.That(RoundTrip(new Announce(2, T1SNodeRole.TemperatureSensor, "DC+ pin")),
                            Is.EqualTo(new Announce(2, T1SNodeRole.TemperatureSensor, "DC+ pin")));

                Assert.That(RoundTrip(new Leave(2)),
                            Is.EqualTo(new Leave(2)));

                Assert.That(RoundTrip(SensorReading.Temperature(2, 17, 91.37, SensorFlags.Alarm | SensorFlags.Warning)),
                            Is.EqualTo(new SensorReading(2, SensorKind.Temperature, 17, 9137, SensorFlags.Alarm | SensorFlags.Warning)));

                Assert.That(RoundTrip(SensorReading.Temperature(2, 18, -12.5)),
                            Is.EqualTo(new SensorReading(2, SensorKind.Temperature, 18, -1250, SensorFlags.None)));

            });

        }

        #endregion

        #region AReadingIsHundredthsOfTheUnit()

        [Test]
        public void AReadingIsHundredthsOfTheUnit()
        {

            var reading = SensorReading.Temperature(1, 0, 23.456);

            Assert.Multiple(() => {
                Assert.That(reading.Value,    Is.EqualTo(2346), "rounded, not cut");
                Assert.That(reading.AsDouble, Is.EqualTo(23.46).Within(1e-9));
            });

            // Past what two bytes hold: pinned, not wrapped. A pin at 400 °C
            // reading as -255 °C would be a pin nobody worried about.
            Assert.That(SensorReading.Temperature(1, 0, 400).Value, Is.EqualTo(Int16.MaxValue));

        }

        #endregion

        #region DataIsCarriedUntouched()

        [Test]
        public void DataIsCarriedUntouched()
        {

            var payload = Enumerable.Range(0, 300).Select(i => (Byte) i).ToArray();
            var back    = RoundTrip(new Data(payload)) as Data;

            Assert.That(back,          Is.Not.Null);
            Assert.That(back!.Payload, Is.EqualTo(payload));

        }

        #endregion

        #region ANameIsCutToWhatFits()

        [Test]
        public void ANameIsCutToWhatFits()
        {

            var name  = new String('x', 40) + "é";
            var back  = RoundTrip(new Join(T1SNodeRole.Sensor, 1, 1, name)) as Join;

            Assert.That(back,              Is.Not.Null);
            Assert.That(back!.Name,        Is.EqualTo(new String('x', T1SConstants.MaxNameLength)));
            Assert.That(back.Name.Length,  Is.LessThanOrEqualTo(T1SConstants.MaxNameLength));

        }

        #endregion

        #region NothingMalformedDecodes()

        [Test]
        public void NothingMalformedDecodes()
        {

            Assert.Multiple(() => {

                // Not even a header.
                Assert.That(EthernetFrame.TryDecode([ 1, 2, 3 ]), Is.Null);

                // A frame, but an empty payload.
                Assert.That(T1SCodec.TryDecode(ReadOnlySpan<Byte>.Empty), Is.Null);

                // The wrong EtherType is somebody else's conversation.
                Assert.That(T1SCodec.TryDecode(new EthernetFrame(ev, evse, 0x88E1, T1SCodec.Encode(new Beacon(1, 1)))), Is.Null);

                // A type nobody knows.
                Assert.That(T1SCodec.TryDecode([ 0x7F, 0, 0 ]), Is.Null);

                // Every fixed layout, one byte short and one byte long.
                Assert.That(T1SCodec.TryDecode(T1SCodec.Encode(new Beacon(1, 1))[..^1]),                                     Is.Null);
                Assert.That(T1SCodec.TryDecode([.. T1SCodec.Encode(new Beacon(1, 1)), 0]),                                    Is.Null);
                Assert.That(T1SCodec.TryDecode(T1SCodec.Encode(new TransmitOpportunity(1, 1, 1))[..^1]),                      Is.Null);
                Assert.That(T1SCodec.TryDecode(T1SCodec.Encode(new Yield(1, 1))[..^1]),                                      Is.Null);
                Assert.That(T1SCodec.TryDecode(T1SCodec.Encode(new Assign(1, 1, T1SNodeRole.Vehicle, 1))[..^1]),             Is.Null);
                Assert.That(T1SCodec.TryDecode(T1SCodec.Encode(new Leave(1))[..^1]),                                         Is.Null);
                Assert.That(T1SCodec.TryDecode(T1SCodec.Encode(SensorReading.Temperature(1, 1, 1))[..^1]),                   Is.Null);

                // A name that claims more bytes than follow, and one that
                // claims fewer.
                Assert.That(T1SCodec.TryDecode([ (Byte) T1SMessageType.Announce, 1, (Byte) T1SNodeRole.Sensor, 5, (Byte) 'a', (Byte) 'b' ]),   Is.Null);
                Assert.That(T1SCodec.TryDecode([ (Byte) T1SMessageType.Announce, 1, (Byte) T1SNodeRole.Sensor, 1, (Byte) 'a', (Byte) 'b' ]),   Is.Null);

                // A role nobody has.
                Assert.That(T1SCodec.TryDecode([ (Byte) T1SMessageType.Announce, 1, 0xEE, 1, (Byte) 'a' ]),                                     Is.Null);

                // A name that is not UTF-8.
                Assert.That(T1SCodec.TryDecode([ (Byte) T1SMessageType.Announce, 1, (Byte) T1SNodeRole.Sensor, 2, 0xFF, 0xFE ]),               Is.Null);

            });

        }

        #endregion

    }

}
