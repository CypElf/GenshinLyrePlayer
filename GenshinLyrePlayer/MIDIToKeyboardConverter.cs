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

        private bool firstNote = true;
        private SevenBitNumber rootNoteNumber = (SevenBitNumber)0;
        private LowLevelKeyboardListener hook;
        private KeyboardLayout layout;
        private static IKeyboardSimulator keyboard = new InputSimulator().Keyboard;
        private static readonly Dictionary<int, VirtualKeyCode> notesConverterAZERTY = new Dictionary<int, VirtualKeyCode>
        {
            // AZERTY layout

            // F-clef
            { 0, VirtualKeyCode.VK_W },
            { 2, VirtualKeyCode.VK_X },
            { 4, VirtualKeyCode.VK_C },
            { 5, VirtualKeyCode.VK_V },
            { 7, VirtualKeyCode.VK_B },
            { 9, VirtualKeyCode.VK_N },
            { 11, VirtualKeyCode.OEM_COMMA },

            // C-clef
            { 12, VirtualKeyCode.VK_Q },
            { 14, VirtualKeyCode.VK_S },
            { 16, VirtualKeyCode.VK_D },
            { 17, VirtualKeyCode.VK_F },
            { 19, VirtualKeyCode.VK_G },
            { 21, VirtualKeyCode.VK_H },
            { 23, VirtualKeyCode.VK_J },

            // G-clef
            { 24, VirtualKeyCode.VK_A },
            { 26, VirtualKeyCode.VK_Z },
            { 28, VirtualKeyCode.VK_E },
            { 29, VirtualKeyCode.VK_R },
            { 31, VirtualKeyCode.VK_T },
            { 33, VirtualKeyCode.VK_Y },
            { 35, VirtualKeyCode.VK_U },
        };
        private static readonly Dictionary<int, VirtualKeyCode> notesConverterQWERTY = new Dictionary<int, VirtualKeyCode>
        {
            // QWERTY layout

            // F-clef
            { 0, VirtualKeyCode.VK_Z },
            { 2, VirtualKeyCode.VK_X },
            { 4, VirtualKeyCode.VK_C },
            { 5, VirtualKeyCode.VK_V },
            { 7, VirtualKeyCode.VK_B },
            { 9, VirtualKeyCode.VK_N },
            { 11, VirtualKeyCode.VK_M },

            // C-clef
            { 12, VirtualKeyCode.VK_A },
            { 14, VirtualKeyCode.VK_S },
            { 16, VirtualKeyCode.VK_D },
            { 17, VirtualKeyCode.VK_F },
            { 19, VirtualKeyCode.VK_G },
            { 21, VirtualKeyCode.VK_H },
            { 23, VirtualKeyCode.VK_J },

            // G-clef
            { 24, VirtualKeyCode.VK_Q },
            { 26, VirtualKeyCode.VK_W },
            { 28, VirtualKeyCode.VK_E },
            { 29, VirtualKeyCode.VK_R },
            { 31, VirtualKeyCode.VK_T },
            { 33, VirtualKeyCode.VK_Y },
            { 35, VirtualKeyCode.VK_U },
        };

        public MIDIToKeyboardConverter(MidiFile midiFile, LowLevelKeyboardListener hook, KeyboardLayout layout)
        {
            this.hook = hook;
            this.layout = layout;
            if (layout == KeyboardLayout.QWERTY)
                rootNoteNumber = GetBestRootNode(midiFile.GetNotes(), notesConverterQWERTY).Item1;
            else
                rootNoteNumber = GetBestRootNode(midiFile.GetNotes(), notesConverterAZERTY).Item1;
        }

        public void PrepareForEventsSending()
        {
            Debug.WriteLine("preparing...");
            rootNoteNumber = (SevenBitNumber)49;
            hook.UnHookKeyboard(); // disable the keyboard hook because if it's enabled while the music begins to play, the first notes are buggy (the first few are played slowly out of sync, and then a ton are played at the same time before the music starts to play correctly after a few seconds)
        }

        public void SendEvent(MidiEvent midiEvent)
        {
            if (midiEvent is NoteOnEvent)
            {
                if (firstNote)
                {
                    hook.HookKeyboard(); // re enable the keyboard hook after the song has begun to play
                    firstNote = false;
                }
                var note = (NoteOnEvent)midiEvent;

                var converter = layout == KeyboardLayout.AZERTY ? notesConverterAZERTY : notesConverterQWERTY;

                if (converter.ContainsKey(note.NoteNumber - rootNoteNumber))
                {
                    VirtualKeyCode key = converter[note.NoteNumber - rootNoteNumber];
                    keyboard.KeyPress(key);
                }

            }
        }

        private static (SevenBitNumber, int) GetBestRootNode(IEnumerable<Note> notes, Dictionary<int, VirtualKeyCode> notesConverter)
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
