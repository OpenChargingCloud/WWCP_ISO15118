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

using cloud.charging.open.protocols.ISO15118.SLAC.Util;

#endregion

namespace cloud.charging.open.protocols.ISO15118.SLAC.Tests
{

    /// <summary>
    /// The 8-byte RunID that ties every frame of one matching attempt together. The EV
    /// generates it and the EVSE echoes it; a stack that compares them wrongly will happily
    /// mix two concurrent matching attempts on a shared supply line, so the equality
    /// behaviour is load-bearing rather than decorative.
    ///
    /// The byte order is deliberately not asserted against a fixed constant: RunId stores a
    /// UInt64 through BitConverter and reads it back the same way, so the round-trip holds on
    /// either endianness, and a RunID is an opaque token rather than a number anyone reads.
    /// </summary>
    [TestFixture]
    public class SLAC_RunIdTests
    {

        #region Constructor_RejectsAnythingButEightBytes(...)

        [TestCase(0)]
        [TestCase(7)]
        [TestCase(9)]
        [TestCase(16)]
        public void Constructor_RejectsAnythingButEightBytes(Int32 Length)
        {

            Assert.Throws<ArgumentException>(() => _ = new RunId(new Byte[Length]));

        }

        #endregion

        #region RunIdLength_IsEight()

        [Test]
        public void RunIdLength_IsEight()
        {

            Assert.That(SLACConstants.RunIdLength, Is.EqualTo(8));

        }

        #endregion

        #region ToArray_RoundTripsThroughTheConstructor()

        [Test]
        public void ToArray_RoundTripsThroughTheConstructor()
        {

            var bytes  = new Byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
            var runId  = new RunId(bytes);

            Assert.Multiple(() => {
                Assert.That(runId.ToArray(),               Is.EqualTo(bytes));
                Assert.That(new RunId(runId.ToArray()),    Is.EqualTo(runId));
            });

        }

        #endregion

        #region ToArray_ReturnsAFreshCopyEachTime()

        [Test]
        public void ToArray_ReturnsAFreshCopyEachTime()
        {

            var runId  = new RunId([ 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 ]);
            var first  = runId.ToArray();

            first[0] = 0xFF;

            Assert.That(runId.ToArray()[0], Is.EqualTo(0x01));

        }

        #endregion

        #region CopyTo_WritesEightBytes()

        [Test]
        public void CopyTo_WritesEightBytes()
        {

            var runId        = new RunId([ 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 ]);
            var destination  = new Byte[12];

            runId.CopyTo(destination.AsSpan(2, 10));

            Assert.Multiple(() => {
                Assert.That(destination[0..2],   Is.EqualTo(new Byte[2]));           // untouched
                Assert.That(destination[2..10],  Is.EqualTo(runId.ToArray()));
                Assert.That(destination[10..12], Is.EqualTo(new Byte[2]));           // untouched
            });

        }

        #endregion

        #region CopyTo_RejectsAnUndersizedDestination()

        [Test]
        public void CopyTo_RejectsAnUndersizedDestination()
        {

            var runId = new RunId([ 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 ]);

            Assert.Throws<ArgumentException>(() => {
                var tooSmall = new Byte[7];
                runId.CopyTo(tooSmall);
            });

        }

        #endregion

        #region Equality_IsByValue()

        [Test]
        public void Equality_IsByValue()
        {

            var bytes  = new Byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
            var a      = new RunId(bytes);
            var b      = new RunId((Byte[]) bytes.Clone());
            var c      = new RunId([ 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01 ]);

            Assert.Multiple(() => {

                Assert.That(a.Equals(b),        Is.True);
                Assert.That(a == b,             Is.True);
                Assert.That(a != b,             Is.False);
                Assert.That(a.GetHashCode(),    Is.EqualTo(b.GetHashCode()));

                Assert.That(a.Equals(c),        Is.False);
                Assert.That(a == c,             Is.False);
                Assert.That(a != c,             Is.True);

                Assert.That(a.Equals((Object) b),      Is.True);
                Assert.That(a.Equals((Object?) null),  Is.False);
                Assert.That(a.Equals("not a RunId"),   Is.False);

            });

        }

        #endregion

        #region OneBitDifference_IsNotEqual()

        /// <summary>
        /// A single flipped bit anywhere in the eight octets must break equality — otherwise
        /// two concurrent matching attempts could be conflated.
        /// </summary>
        [Test]
        public void OneBitDifference_IsNotEqual()
        {

            var baseline = new RunId([ 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 ]);

            Assert.Multiple(() => {

                for (var octet = 0; octet < SLACConstants.RunIdLength; octet++)
                {
                    for (var bit = 0; bit < 8; bit++)
                    {

                        var bytes     = new Byte[SLACConstants.RunIdLength];
                        bytes[octet]  = (Byte) (1 << bit);

                        Assert.That(new RunId(bytes), Is.Not.EqualTo(baseline),
                                    $"flipping bit {bit} of octet {octet} did not change the RunID");

                    }
                }

            });

        }

        #endregion

        #region NewRandom_ProducesDistinctValues()

        [Test]
        public void NewRandom_ProducesDistinctValues()
        {

            var runIds = new HashSet<RunId>();

            for (var i = 0; i < 256; i++)
                runIds.Add(RunId.NewRandom());

            // A collision among 256 draws from 2^64 is not a thing that happens; if this ever
            // trips, the generator is not random.
            Assert.Multiple(() => {
                Assert.That(runIds,                     Has.Count.EqualTo(256));
                Assert.That(RunId.NewRandom().ToArray(), Has.Length.EqualTo(SLACConstants.RunIdLength));
            });

        }

        #endregion

        #region ToString_IsSixteenHexDigits()

        [Test]
        public void ToString_IsSixteenHexDigits()
        {

            var text = new RunId([ 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 ]).ToString();

            Assert.Multiple(() => {
                Assert.That(text,       Does.StartWith("0x"));
                Assert.That(text,       Has.Length.EqualTo(18));
                Assert.That(text[2..],  Does.Match("^[0-9A-F]{16}$"));
            });

        }

        #endregion

    }


    /// <summary>
    /// The hex helpers. They exist for pentest logs and packet dumps, which means they are read
    /// by people trying to work out what went wrong on a link — so the offsets and the gutter
    /// alignment matter more than they would in ordinary debug output.
    /// </summary>
    [TestFixture]
    public class SLAC_HexTests
    {

        #region Compact_IsSpaceSeparatedUppercaseOctets()

        [Test]
        public void Compact_IsSpaceSeparatedUppercaseOctets()
        {

            Assert.Multiple(() => {
                Assert.That(Hex.Compact([ 0x00, 0x0F, 0xA0, 0xFF ]), Is.EqualTo("00 0F A0 FF"));
                Assert.That(Hex.Compact([ 0x2A ]),                   Is.EqualTo("2A"));
                Assert.That(Hex.Compact([]),                         Is.Empty);
            });

        }

        #endregion

        #region Dump_ShowsOffsetHexAndAsciiGutter()

        [Test]
        public void Dump_ShowsOffsetHexAndAsciiGutter()
        {

            var dump = Hex.Dump(System.Text.Encoding.ASCII.GetBytes("Hello"));

            Assert.Multiple(() => {
                Assert.That(dump, Does.StartWith("  0000  "));
                Assert.That(dump, Does.Contain("48 65 6C 6C 6F"));
                Assert.That(dump, Does.Contain("Hello"));
            });

        }

        #endregion

        #region Dump_ReplacesNonPrintableBytesWithDots()

        [Test]
        public void Dump_ReplacesNonPrintableBytesWithDots()
        {

            var dump = Hex.Dump([ 0x00, 0x1F, 0x41, 0x7F, 0xFF ]);

            Assert.Multiple(() => {
                Assert.That(dump, Does.Contain("..A.."));
                Assert.That(dump, Does.Contain("00 1F 41 7F FF"));
            });

        }

        #endregion

        #region Dump_BreaksLinesAndAdvancesTheOffset()

        [Test]
        public void Dump_BreaksLinesAndAdvancesTheOffset()
        {

            var lines = Hex.Dump(new Byte[33]).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            Assert.Multiple(() => {
                Assert.That(lines,     Has.Length.EqualTo(3));      // 16 + 16 + 1
                Assert.That(lines[0],  Does.StartWith("  0000  "));
                Assert.That(lines[1],  Does.StartWith("  0010  "));
                Assert.That(lines[2],  Does.StartWith("  0020  "));
            });

        }

        #endregion

        #region Dump_PadsTheLastLineSoTheGutterStaysAligned()

        [Test]
        public void Dump_PadsTheLastLineSoTheGutterStaysAligned()
        {

            var lines = Hex.Dump(new Byte[17]).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            // A full line and a one-byte line must reach the ASCII gutter at the same column.
            Assert.That(lines[1].IndexOf('.'), Is.EqualTo(lines[0].IndexOf('.')));

        }

        #endregion

        #region Dump_OfNothingIsEmpty()

        [Test]
        public void Dump_OfNothingIsEmpty()
        {

            Assert.That(Hex.Dump([]), Is.Empty);

        }

        #endregion

    }

}
