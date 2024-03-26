using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using WindowsInput.Events;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Interaction;

namespace GenshinLyrePlayer
{
    public sealed class MIDIToKeyboardConverter : IOutputDevice
    {
        public event EventHandler<MidiEventSentEventArgs> EventSent;

        private readonly MidiFile midiFile;
        public readonly SevenBitNumber rootNoteNumber;
        private readonly Dictionary<int, KeyCode> converter;

        // MIDI notes are coded with 7 bits, so they range from 0 to 127
        // As there are 12 semitones in one octave, there are a total of 11 octaves (with the latest one missing the 4 upper values)
        // Because we can only play in a range of 3 octaves in Genshin Impact, we have to choose the interval of 3 octaves we want to keep among the 11
        // The choice made here is to select the interval of notes between the fifth and eighth octaves as it covers the middle notes, and in general most of the notes will be in this interval
        private static readonly int fifthOctaveStart = 48;
        private static readonly int eighthOctaveStart = 84;

        private ProgressBar progressBar;

        private static readonly List<int> semiTones = new()
        {
            // first octave
            0,
            2,
            4,
            5,
            7,
            9,
            11,

            // second octave
            12,
            14,
            16,
            17,
            19,
            21,
            23,

            // third octave
            24,
            26,
            28,
            29,
            31,
            33,
            35
        };

        private static readonly List<KeyCode> azertyLyreKeys = new()
        {
            // F-clef
            KeyCode.W,
            KeyCode.X,
            KeyCode.C,
            KeyCode.V,
            KeyCode.B,
            KeyCode.N,
            KeyCode.Oemcomma,

            // C-clef
            KeyCode.Q,
            KeyCode.S,
            KeyCode.D,
            KeyCode.F,
            KeyCode.G,
            KeyCode.H,
            KeyCode.J,

            // G-clef
            KeyCode.A,
            KeyCode.Z,
            KeyCode.E,
            KeyCode.R,
            KeyCode.T,
            KeyCode.Y,
            KeyCode.U
        };

        private static readonly List<KeyCode> qwertzLyreKeys = new()
        {
            // F-clef
            KeyCode.Y,
            KeyCode.X,
            KeyCode.C,
            KeyCode.V,
            KeyCode.B,
            KeyCode.N,
            KeyCode.M,

            // C-clef
            KeyCode.A,
            KeyCode.S,
            KeyCode.D,
            KeyCode.F,
            KeyCode.G,
            KeyCode.H,
            KeyCode.J,

            // G-clef
            KeyCode.Q,
            KeyCode.W,
            KeyCode.E,
            KeyCode.R,
            KeyCode.T,
            KeyCode.Z,
            KeyCode.U,
        };

        private static readonly List<KeyCode> qwertyLyreKeys = new()
        {
            // F-clef
            KeyCode.Z,
            KeyCode.X,
            KeyCode.C,
            KeyCode.V,
            KeyCode.B,
            KeyCode.N,
            KeyCode.M,
            
            // C-clef
            KeyCode.A,
            KeyCode.S,
            KeyCode.D,
            KeyCode.F,
            KeyCode.G,
            KeyCode.H,
            KeyCode.J,
            
            // G-clef
            KeyCode.Q,
            KeyCode.W,
            KeyCode.E,
            KeyCode.R,
            KeyCode.T,
            KeyCode.Y,
            KeyCode.U,
        };

        public MIDIToKeyboardConverter(MidiFile midiFile, KeyboardLayout layout, int? rootNote, ProgressBar progressBar)
        {
            this.midiFile = midiFile;
            this.progressBar = progressBar;
            List<KeyCode> lyreKeys;

            if (layout == KeyboardLayout.QWERTY)
            {
                lyreKeys = qwertyLyreKeys;
            }
            else if (layout == KeyboardLayout.QWERTZ)
            {
                lyreKeys = qwertzLyreKeys;
            }
            else
            {
                lyreKeys = azertyLyreKeys;
            }

            converter = semiTones.Zip(lyreKeys, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);

            rootNoteNumber = (SevenBitNumber)(rootNote == null ? GetBestRootNode() : rootNote);
        }

        public void PrepareForEventsSending() {}

        public async void SendEvent(MidiEvent midiEvent)
        {
            if (midiEvent is NoteOnEvent @event)
            {
                var note = @event;
                progressBar.Dispatcher.Invoke(() => progressBar.Value++);

                if (converter.ContainsKey(note.NoteNumber - rootNoteNumber))
                {
                    KeyCode key = converter[note.NoteNumber - rootNoteNumber];
                    await WindowsInput.Simulate.Events().Click(key).Invoke();
                }
            }
        }

        public int HitsForRootNote()
        {
            int hits = 0;
            foreach (var note in midiFile.GetNotes())
            {
                if (converter.ContainsKey(note.NoteNumber - rootNoteNumber))
                {
                    hits++;
                }
            }
            return hits;
        }

        public int NotesCountInIntervalOfRoot()
        {
            int hits = 0;

            var notes = converter.Keys.ToList();
            notes.Sort();
            var minNote = notes.First();
            var maxNote = notes.Last();

            foreach (var note in midiFile.GetNotes())
            {
                if (minNote <= note.NoteNumber - rootNoteNumber && maxNote >= note.NoteNumber - rootNoteNumber)
                {
                    hits++;
                }
            }
            return hits;
        }

        private SevenBitNumber GetBestRootNode()
        {
            var sortedNotesNumbers = midiFile.GetNotes().Select(note => note.NoteNumber).OrderBy(note => note);
            var bestRoot = sortedNotesNumbers.First();
            var notesCount = sortedNotesNumbers.GroupBy(note => note).ToDictionary(group => group.Key, group => group.Count());
            var bestHits = 0;

            foreach (var root in Enumerable.Range(sortedNotesNumbers.First() - 24, sortedNotesNumbers.Last() + 25))
            {
                var hits = 0;
                var total = 0;
                foreach (var entry in notesCount)
                {
                     var note = entry.Key;
                     var count = entry.Value;

                     if (fifthOctaveStart <= note && note <= eighthOctaveStart)
                     {

                        if (converter.ContainsKey(note - root)) {
                            hits += count;
                        }
                        total += count;
                     }
                }
                if (hits > bestHits)
                {
                    bestHits = hits;
                    bestRoot = (SevenBitNumber)root;
                }
            }

            return bestRoot;
        }

        public void Dispose()
        {
        }
    }
}
