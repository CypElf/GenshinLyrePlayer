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

        private SevenBitNumber rootNoteNumber;
        private static readonly IKeyboardSimulator keyboard = new InputSimulator().Keyboard;
        private readonly Dictionary<int, VirtualKeyCode> converter;
        private static readonly List<int> semiTones = new()
        {
            0,
            2,
            4,
            5,
            7,
            9,
            11,

            12,
            14,
            16,
            17,
            19,
            21,
            23,

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

        public MIDIToKeyboardConverter(MidiFile midiFile, KeyboardLayout layout)
        {
            converter = semiTones.Zip(layout == KeyboardLayout.QWERTY ? qwertyLyreKeys : azertyLyreKeys, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);

            rootNoteNumber = GetBestRootNode(midiFile.GetNotes()).Item1;
        }

        public void PrepareForEventsSending()
        {
            Debug.WriteLine("preparing...");
            rootNoteNumber = (SevenBitNumber)47;
        }

        public void SendEvent(MidiEvent midiEvent)
        {
            if (midiEvent is NoteOnEvent)
            {
                var note = (NoteOnEvent)midiEvent;

                if (converter.ContainsKey(note.NoteNumber - rootNoteNumber))
                {
                    VirtualKeyCode key = converter[note.NoteNumber - rootNoteNumber];
                    keyboard.KeyPress(key);
                }

            }
        }

        private static (SevenBitNumber, int) GetBestRootNode(IEnumerable<Note> notes)
        {
            var sortedNotesNumbers = notes.Select(note => note.NoteNumber).OrderBy(note => note);
            var bestRoot = sortedNotesNumbers.First();
            var notesCount = sortedNotesNumbers.GroupBy(note => note).ToDictionary(group => group.Key, group => group.Count());
            var bestHits = 0;

            Debug.WriteLine(notesCount.Select(note => note.Value).Sum());
            Debug.WriteLine(sortedNotesNumbers.Count());

            foreach (var root in Enumerable.Range(sortedNotesNumbers.First(), sortedNotesNumbers.Last() - 35))
            {
                var higher = root + 35;
                var hits = 0;
                foreach (var noteNumber in notesCount.Keys)
                {
                    if (root <= noteNumber && noteNumber <= higher)
                    {
                        hits += notesCount[noteNumber];
                    }
                }
                if (hits > bestHits)
                {
                    bestHits = hits;
                    bestRoot = (SevenBitNumber)root;
                }
            }

            return (bestRoot, bestHits);
        }
    }
}
