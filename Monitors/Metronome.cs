using Blastula.Sounds;
using Blastula.VirtualVariables;
using Godot;
using System;

namespace HellSyncer
{
    /// <summary>
    /// Monitor to filter and emit signals based on the MIDI's rhythm.
    /// This emits two kinds of signals: one for when the measure begins,
    /// and one for when an "interval" in that measure begins, which has the duration of a customizable number of quarter notes.
    /// </summary>
    /// <remarks>
    /// Naturally, AudioStreamSynced.main must exist and be currently playing for this to emit any signals.
    /// </remarks>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/metronome.png")]
    public partial class Metronome : Node
    {
        /// <summary>
        /// The length of an interval, which is a number of quarter notes.
        /// </summary>
        /// <example>0.25: an interval is one sixteenth note long.</example>
        /// <example>2: an interval is one half note long.</example>
        /// <example>1.5: an interval is one dotted quarter note long. (Note: a new interval begins every measure, so depending on time signature, the last measure's final interval may be cut short.)</example>
        /// <example>0.333: an interval is one triplet eighth note long.</example>
        /// <remarks>
        /// If this is extremely small (think 64th notes), it's possible that an interval is shorter than one frame.
        /// When this happens, only one OnInterval signal is sent per frame, and the metronome will lag behind.
        /// So avoid extremely small intervals if you can.
        /// </remarks>
        [Export] public float intervalQuarterNotes = 1f;
        /// <summary>
        /// Sound ID to play when a measure begins. Useful for testing a rhythm.
        /// </summary>
        [ExportGroup("Debug")]
        [Export] public string debugMeasureSound = "";
        /// <summary>
        /// Sound ID to play when an interval begins. Useful for testing a rhythm.
        /// </summary>
        [Export] public string debugBeatSound = "";

        public const int PROCESS_PRIORITY = AudioStreamSynced.PROCESS_PRIORITY + 1;

        private ulong lastMeasure = ulong.MaxValue;
        private float lastBeat = -1f;
        private float targetBeat = 0f;

        [Signal] public delegate void OnMeasureEventHandler(ulong measureNumber);
        [Signal] public delegate void OnIntervalEventHandler(ulong measureNumber, float beat);

        public override void _Ready()
        {
            _Process(0);
        }

        public override void _Process(double delta)
        {
            if (AudioStreamSynced.main == null) { return; }
            if (Session.main.paused || Blastula.Debug.GameFlow.frozen) { return; }
            (ulong currMeasure, float currBeat) = AudioStreamSynced.main.GetBeatAndMeasure();
            if (currMeasure != lastMeasure)
            {
                EmitSignal(SignalName.OnMeasure, currMeasure);
                EmitSignal(SignalName.OnInterval, currMeasure, 0);
                if (debugMeasureSound != null && debugMeasureSound != "")
                {
                    CommonSFXManager.PlayByName(debugMeasureSound, 1, 1, default, true);
                }
                targetBeat = intervalQuarterNotes;
            }
            else if (currBeat >= targetBeat)
            {
                EmitSignal(SignalName.OnInterval, currMeasure, targetBeat);
                if (debugBeatSound != null && debugBeatSound != "")
                {
                    CommonSFXManager.PlayByName(debugBeatSound, 1, 1, default, true);
                }
                targetBeat += intervalQuarterNotes;
            }
            (lastMeasure, lastBeat) = (currMeasure, currBeat);
        }
    }
}

