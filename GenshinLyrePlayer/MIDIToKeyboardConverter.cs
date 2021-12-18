using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WindowsInput;
using WindowsInput.Native;

namespace GenshinLyrePlayer
{
    public sealed class MIDIToKeyboardConverter : IOutputDevice
    {
        public event EventHandler<MidiEventSentEventArgs> EventSent;

        private readonly SevenBitNumber rootNoteNumber;
        private static readonly IKeyboardSimulator keyboard = new InputSimulator().Keyboard;
        private readonly Dictionary<int, VirtualKeyCode> converter;

        // MIDI notes are coded with 7 bits, so they range from 0 to 127
        // As there are 12 semitones in one octave, there are a total of 11 octaves (with the latest one missing the 4 upper values)
        // Because we can only play in a range of 3 octaves in Genshin Impact, we have to choose the interval of 3 octaves we want to keep among the 11
        // The choice made here is to select the interval of notes between the fifth and eighth octaves as it covers the middle notes, and in general most of the notes will be in this interval
        private static readonly int fifthOctaveStart = 48;
        private static readonly int eighthOctaveStart = 84;

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

        private static readonly List<VirtualKeyCode> azertyLyreKeys = new()
        {
            // F-clef
            VirtualKeyCode.VK_W,
            VirtualKeyCode.VK_X,
            VirtualKeyCode.VK_C,
            VirtualKeyCode.VK_V,
            VirtualKeyCode.VK_B,
            VirtualKeyCode.VK_N,
            VirtualKeyCode.OEM_COMMA,

            // C-clef
            VirtualKeyCode.VK_Q,
            VirtualKeyCode.VK_S,
            VirtualKeyCode.VK_D,
            VirtualKeyCode.VK_F,
            VirtualKeyCode.VK_G,
            VirtualKeyCode.VK_H,
            VirtualKeyCode.VK_J,

            // G-clef
            VirtualKeyCode.VK_A,
            VirtualKeyCode.VK_Z,
            VirtualKeyCode.VK_E,
            VirtualKeyCode.VK_R,
            VirtualKeyCode.VK_T,
            VirtualKeyCode.VK_Y,
            VirtualKeyCode.VK_U
        };

        private static readonly List<VirtualKeyCode> qwertzLyreKeys = new()
        {
            // F-clef
            VirtualKeyCode.VK_Y,
            VirtualKeyCode.VK_X,
            VirtualKeyCode.VK_C,
            VirtualKeyCode.VK_V,
            VirtualKeyCode.VK_B,
            VirtualKeyCode.VK_N,
            VirtualKeyCode.VK_M,

            // C-clef
            VirtualKeyCode.VK_A,
            VirtualKeyCode.VK_S,
            VirtualKeyCode.VK_D,
            VirtualKeyCode.VK_F,
            VirtualKeyCode.VK_G,
            VirtualKeyCode.VK_H,
            VirtualKeyCode.VK_J,

            // G-clef
            VirtualKeyCode.VK_Q,
            VirtualKeyCode.VK_W,
            VirtualKeyCode.VK_E,
            VirtualKeyCode.VK_R,
            VirtualKeyCode.VK_T,
            VirtualKeyCode.VK_Z,
            VirtualKeyCode.VK_U,
        };

        private static readonly List<VirtualKeyCode> qwertyLyreKeys = new()
        {
            // F-clef
            VirtualKeyCode.VK_Z,
            VirtualKeyCode.VK_X,
            VirtualKeyCode.VK_C,
            VirtualKeyCode.VK_V,
            VirtualKeyCode.VK_B,
            VirtualKeyCode.VK_N,
            VirtualKeyCode.VK_M,
            
            // C-clef
            VirtualKeyCode.VK_A,
            VirtualKeyCode.VK_S,
            VirtualKeyCode.VK_D,
            VirtualKeyCode.VK_F,
            VirtualKeyCode.VK_G,
            VirtualKeyCode.VK_H,
            VirtualKeyCode.VK_J,
            
            // G-clef
            VirtualKeyCode.VK_Q,
            VirtualKeyCode.VK_W,
            VirtualKeyCode.VK_E,
            VirtualKeyCode.VK_R,
            VirtualKeyCode.VK_T,
            VirtualKeyCode.VK_Y,
            VirtualKeyCode.VK_U,
        };

        public MIDIToKeyboardConverter(MidiFile midiFile, KeyboardLayout layout, int rootNote = -1)
        {
            List<VirtualKeyCode> lyreKeys;

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

            rootNoteNumber = (SevenBitNumber)(rootNote == -1 ? GetBestRootNode(midiFile.GetNotes()).Item1 : rootNote);
            Debug.WriteLine("Root note number: " + rootNoteNumber);
        }

        public void PrepareForEventsSending() {}

        public void SendEvent(MidiEvent midiEvent)
        {
            if (midiEvent is NoteOnEvent @event)
            {
                var note = @event;

                if (converter.ContainsKey(note.NoteNumber - rootNoteNumber))
                {
                    VirtualKeyCode key = converter[note.NoteNumber - rootNoteNumber];
                    keyboard.KeyPress(key);
                }
            }
        }

        private (SevenBitNumber, int) GetBestRootNode(IEnumerable<Note> notes)
        {
            var sortedNotesNumbers = notes.Select(note => note.NoteNumber).OrderBy(note => note);
            var bestRoot = sortedNotesNumbers.First();
            var notesCount = sortedNotesNumbers.GroupBy(note => note).ToDictionary(group => group.Key, group => group.Count());
            var bestHits = 0;
            var bestTotal = 0;

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
                    bestTotal = total;
                }
            }

            Debug.WriteLine(bestHits + " in " + bestTotal + " | best root note is " + ((int)bestRoot).ToString());

            return (bestRoot, bestHits);
        }
    }
}
