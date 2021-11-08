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
using Melanchall.DryWetMidi.Multimedia;

namespace GenshinLyrePlayer
{
    public partial class MainWindow : Window
    {
        // the int used as a key is the offset from the root note in semitones
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

            if (e.KeyPressed == Key.F7 || e.KeyPressed == Key.F6)
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

                if (e.KeyPressed == Key.F7 && MidiFilesList.Items.Count > 0)
                {
                    var output = new MIDIToKeyboardConverter();
                    output.ConfigureFor(midiFile);
                    midiFile.Play(output);
                    Debug.WriteLine("finished playing that");
                }

                if (e.KeyPressed == Key.F6 && MidiFilesList.Items.Count > 0)
                {
                    /* var tempo = midiFile.GetTempoMap();
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
                    } */
                }
            }
        }
    }
}