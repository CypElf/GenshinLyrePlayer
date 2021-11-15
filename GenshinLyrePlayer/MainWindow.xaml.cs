using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using System;

namespace GenshinLyrePlayer
{
    public partial class MainWindow : Window
    {
        // the int used as a key is the offset from the root note in semitones
        private LowLevelKeyboardListener listener = new LowLevelKeyboardListener();
        private List<FileStream> midiFiles = new List<FileStream>();
        private Playback playback;

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

            foreach (KeyboardLayout value in Enum.GetValues(typeof(KeyboardLayout))) {
                var item = new ComboBoxItem();
                item.Tag = value;
                item.Content = value.ToString();
                if (value == KeyboardLayout.QWERTY) item.IsSelected = true;
                layoutComboBox.Items.Add(item);
            }

            listener = new LowLevelKeyboardListener();
            listener.OnKeyPressed += onKeyPressed;
            listener.HookKeyboard();
        }

        private void onKeyPressed(object sender, KeyPressedArgs e)
        {
            Debug.WriteLine("PRESSED " + e.KeyPressed.ToString());

            if (e.KeyPressed == Key.F7 && playback != null && playback.IsRunning)
            {
                playback.Stop();
            }

            if (e.KeyPressed == Key.F6 && MidiFilesList.Items.Count > 0 && (playback == null || !playback.IsRunning))
            {
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

                    var layout = (KeyboardLayout)((ComboBoxItem)layoutComboBox.SelectedItem).Tag;

                    var player = new MIDIToKeyboardConverter(midiFile, layout);

                    playback = midiFile.GetPlayback(player);
                    playback.Start();
                }
            }
        }
    }
}