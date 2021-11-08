using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Interaction;
using WindowsInput.Native;
using WindowsInput;
using System.Threading;
using System;

namespace GenshinLyrePlayer
{
    public partial class MainWindow : Window
    {
        // the int used as a key is the offset from the root note in semitones
        private static Dictionary<int, VirtualKeyCode> notesConverter = new Dictionary<int, VirtualKeyCode>
        {
            // AZERTY layout

            // F-clef
            { 0, VirtualKeyCode.VK_W },
            { 2, VirtualKeyCode.VK_X },
            { 4, VirtualKeyCode.VK_X },
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
        private LowLevelKeyboardListener listener;
        private List<FileStream> midiFiles;
        private InputSimulator simulator;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            listener.UnHookKeyboard();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            midiFiles = new List<FileStream>();

            if (Directory.Exists("songs"))
            {
                var midiFilesNames = Directory.GetFiles("songs");
                foreach (var midiFileName in midiFilesNames)
                {
                    midiFiles.Add(File.OpenRead(midiFileName));
                }
            }

            foreach (var midiFile in midiFiles)
            {
                var listItem = new ListBoxItem();
                listItem.Content = midiFile.Name.Split("\\").Last();
                listItem.FontSize = 14;
                MidiFilesList.Items.Add(listItem);
            }

            listener = new LowLevelKeyboardListener();
            listener.OnKeyPressed += onKeyPressed;
            listener.HookKeyboard();

            simulator = new InputSimulator();
        }

        private void onKeyPressed(object sender, KeyPressedArgs e)
        {
            Debug.WriteLine("PRESSED " + e.KeyPressed.ToString());

            if (e.KeyPressed == Key.F6 && MidiFilesList.Items.Count > 0)
            {
                MidiFile midiFile;
                try
                {
                    midiFile = MidiFile.Read(midiFiles[MidiFilesList.SelectedIndex]);
                }
                catch
                {
                    MessageBox.Show("Unable to parse this file as a MIDI file");
                    return;
                }

                var tempo = midiFile.GetTempoMap();
                IEnumerable<Note> notes = midiFile.GetNotes();
                
                var (rootNode, hits) = GetBestRootNode(notes, notesConverter);

                Debug.WriteLine("Best root found is " + rootNode + " with " + hits + " hits in " + notes.Count() + " notes (" + ((double)hits / notes.Count() * 100).ToString("F") + "%)");

                foreach (var note in notes)
                {
                    VirtualKeyCode? key = null;
                    if (notesConverter.ContainsKey(note.NoteNumber - rootNode))
                    {
                        key = notesConverter[note.NoteNumber - rootNode];
                    }
                    Debug.WriteLine("Note: " + note.NoteNumber);
                    Debug.WriteLine(key.ToString());

                    if (key != null)
                    {
                        simulator.Keyboard.KeyDown((VirtualKeyCode)key);

                        var length = TimeConverter.ConvertTo<MetricTimeSpan>(note.Length, tempo);
                        Debug.WriteLine("I'll wait for " + length.Milliseconds + " ms");
                        Thread.Sleep(length.Milliseconds);

                        simulator.Keyboard.KeyUp((VirtualKeyCode)key);
                    }
                }
            }
        }

        private static (int, int) GetBestRootNode(IEnumerable<Note> notes, Dictionary<int, VirtualKeyCode> notesConverter)
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