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

namespace cloud.charging.open.protocols.ISO15118.SLAC.Validation;

/// <summary>
/// Simulated CP-toggle channel. Stands in for the physical CP pilot signal
/// in the demo: the EV's <see cref="IToggleSource"/> increments its count
/// whenever <see cref="Trigger"/> is called; the EVSE's
/// <see cref="IToggleObserver"/> sees the same increment on the same shared
/// channel.
/// </summary>
/// <remarks>
/// In production the EV and EVSE never share state — they observe a real
/// physical signal. Here we model the signal as a single counter with two
/// views (source / observer) so that one toggle in the demo corresponds to
/// one toggle on both sides, exactly the way real wiring would work.
/// </remarks>
public sealed class SimulatedToggleChannel
{
    private int  _absoluteCount;
    private int  _sourceBaseline;
    private int  _observerBaseline;

    public IToggleSource    Source   { get; }
    public IToggleObserver  Observer { get; }

    public SimulatedToggleChannel()
    {
        Source   = new SourceView(this);
        Observer = new ObserverView(this);
    }

    /// <summary>Trigger one CP-toggle event on the simulated wire.</summary>
    public void Trigger() => Interlocked.Increment(ref _absoluteCount);

    /// <summary>Trigger several CP-toggle events at once.</summary>
    public void Trigger(int count)
    {
        for (var i = 0; i < count; i++) Trigger();
    }

    private sealed class SourceView(SimulatedToggleChannel ch) : IToggleSource
    {
        public void Reset() => ch._sourceBaseline = Volatile.Read(ref ch._absoluteCount);
        public byte GetCount()
        {
            var d = Volatile.Read(ref ch._absoluteCount) - ch._sourceBaseline;
            return (byte) Math.Clamp(d, 0, 255);
        }
    }

    private sealed class ObserverView(SimulatedToggleChannel ch) : IToggleObserver
    {
        public void Reset() => ch._observerBaseline = Volatile.Read(ref ch._absoluteCount);
        public byte GetCount()
        {
            var d = Volatile.Read(ref ch._absoluteCount) - ch._observerBaseline;
            return (byte) Math.Clamp(d, 0, 255);
        }
    }
}

/// <summary>
/// Stand-alone source that produces a fixed number of toggles automatically
/// on first use. Useful when the EV's state machine runs in a different
/// process from the EVSE's, and a shared <see cref="SimulatedToggleChannel"/>
/// isn't available.
/// </summary>
public sealed class AutoToggleSource(byte targetCount) : IToggleSource
{
    private byte _produced;

    public void Reset() => _produced = 0;
    public byte GetCount()
    {
        // Each call advances toward the target so multiple polls converge
        // on the expected value within a couple of CM_VALIDATE rounds.
        if (_produced < targetCount) _produced++;
        return _produced;
    }
}

/// <summary>
/// Stand-alone observer that reports a steadily growing count, capped at
/// <paramref name="targetCount"/>. The mirror image of
/// <see cref="AutoToggleSource"/> for the EVSE side.
/// </summary>
public sealed class AutoToggleObserver(byte targetCount) : IToggleObserver
{
    private byte _seen;

    public void Reset() => _seen = 0;
    public byte GetCount()
    {
        if (_seen < targetCount) _seen++;
        return _seen;
    }
}
