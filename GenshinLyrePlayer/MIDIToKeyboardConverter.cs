using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;

namespace GenshinLyrePlayer
{
    public sealed class MIDIToKeyboardConverter : IOutputDevice
    {
        public event EventHandler<MidiEventSentEventArgs> EventSent;

        private static InputSimulator simulator = new InputSimulator();
        private static Dictionary<int, VirtualKeyCode> notesConverter = new Dictionary<int, VirtualKeyCode>
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
        private SevenBitNumber rootNoteNumber = (SevenBitNumber)0;
        private TempoMap tempoMap;


        public void PrepareForEventsSending()
        {
            Debug.WriteLine("preparing...");
        }

        public async void SendEvent(MidiEvent midiEvent)
        {

            if (midiEvent is NoteOnEvent)
            {
                var note = (NoteOnEvent)midiEvent;
                // var delta = TimeConverter.ConvertTo<MetricTimeSpan>(note.DeltaTime, tempoMap).Milliseconds;

                // Debug.WriteLine("I'll wait for " + delta + " ms (note on)");

                // await Task.Delay(delta);

                VirtualKeyCode? key = null;
                if (notesConverter.ContainsKey(note.NoteNumber - rootNoteNumber))
                {
                    key = notesConverter[note.NoteNumber - rootNoteNumber];
                }

                if (key != null)
                {
                    simulator.Keyboard.KeyDown((VirtualKeyCode)key);
                }
            }
            else if (midiEvent is NoteOffEvent)
            {
                var note = (NoteOffEvent)midiEvent;
                // var delta = TimeConverter.ConvertTo<MetricTimeSpan>(note.DeltaTime, tempoMap).Milliseconds;
                // Debug.WriteLine("I'll wait for " + delta + " ms (note off)");
                // await Task.Delay(delta);

                VirtualKeyCode? key = null;
                if (notesConverter.ContainsKey(note.NoteNumber - rootNoteNumber))
                {
                    key = notesConverter[note.NoteNumber - rootNoteNumber];
                }

                if (key != null)
                {
                    simulator.Keyboard.KeyUp((VirtualKeyCode)key);
                }
            }

            // Debug.WriteLine(midiEvent);
        }

        public void ConfigureFor(MidiFile midiFile)
        {
            rootNoteNumber = GetBestRootNode(midiFile.GetNotes(), notesConverter).Item1;
            tempoMap = midiFile.GetTempoMap();
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
