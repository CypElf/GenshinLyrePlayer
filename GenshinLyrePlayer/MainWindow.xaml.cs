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
using System.Text.Json;
using Melanchall.DryWetMidi.Interaction;

namespace GenshinLyrePlayer
{
    public partial class MainWindow : Window
    {
        // the int used as a key is the offset from the root note in semitones
        private LowLevelKeyboardListener listener = new LowLevelKeyboardListener();
        private List<FileStream> midiFiles = new List<FileStream>();
        private Playback playback;

        private bool disableInputs = false;

        private Config cfg;

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
                    if (midiFileName.EndsWith(".mid"))
                    {
                        midiFiles.Add(File.OpenRead(midiFileName));
                    }
                }
            }

            foreach (var midiFile in midiFiles)
            {
                var listItem = new ListBoxItem();
                listItem.Content = string.Join(".", midiFile.Name.Split("\\").Last().Split(".").SkipLast(1));
                listItem.FontSize = 14;
                MidiFilesList.Items.Add(listItem);
            }

            listener = new LowLevelKeyboardListener();
            listener.OnKeyPressed += onKeyPressed;
            listener.HookKeyboard();

            loadSave();
        }

        private void onKeyPressed(object sender, KeyPressedArgs e)
        {
            if (!disableInputs)
            {
                if (e.KeyPressed == (Key)Enum.Parse(typeof(Key), (string)cfg.stopKey) && playback != null && playback.IsRunning)
                {
                    playback.Stop();
                    setIdle();
                }

                if (e.KeyPressed == (Key)Enum.Parse(typeof(Key), (string)cfg.startKey) && MidiFilesList.Items.Count > 0 && (playback == null || !playback.IsRunning))
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

                        var layout = (KeyboardLayout)Enum.Parse(typeof(KeyboardLayout), cfg.keyboardLayout);

                        playingTextBlock.Text = "Playing " + ((ListBoxItem)MidiFilesList.SelectedItem).Content;
                        progressBar.Visibility = Visibility.Visible;
                        progressBar.Value = 0;
                        progressBar.Maximum = midiFile.GetNotes().Count;

                        var player = new MIDIToKeyboardConverter(midiFile, layout, cfg.useAutoRoot ? null : cfg.customRoot, progressBar);

                        var hits = player.hitsForRootNote(player.rootNoteNumber);
                        var notesCount = midiFile.GetNotes().Count;

                        infoTextBlock.Text = $"using root note {player.rootNoteNumber} with a restitution of {hits}/{notesCount} notes ({Math.Round((double)hits / notesCount * 100, 2)}%)";
                        infoTextBlock.Visibility = Visibility.Visible;

                        playback = midiFile.GetPlayback(player);
                        playback.Start();

                        playback.Finished += (_, _) => Dispatcher.Invoke(() => setIdle());
                    }
                }
            }
        }

        private void OnSettingsClick(object sender, RoutedEventArgs e)
        {
            Settings settingsWindow = new Settings(cfg);
            disableInputs = true;
            settingsWindow.Show();
            settingsWindow.Closed += (_, _) => disableInputs = false;
        }

        private void OnHelpClick(object sender, RoutedEventArgs e)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.FileName = "https://github.com/CypElf/GenshinLyrePlayer";
            p.Start();
        }

        private void setIdle()
        {
            playingTextBlock.Text = "IDLE";
            progressBar.Visibility = Visibility.Hidden;
            infoTextBlock.Visibility = Visibility.Hidden;
        }

        private void loadSave()
        {
            try
            {
                cfg = JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json"));

                // check if the stored enum fields are valid by trying to convert them to their enum variant

                try
                {
                    Enum.Parse(typeof(KeyboardLayout), cfg.keyboardLayout);
                }
                catch {
                    cfg.keyboardLayout = "QWERTY";
                }

                try
                {
                    Enum.Parse(typeof(Key), cfg.startKey);
                }
                catch {
                    cfg.startKey = "F6";
                }

                try
                {
                    Enum.Parse(typeof(Key), cfg.stopKey);
                }
                catch
                {
                    cfg.stopKey = "F7";
                }
            }
            catch {}
        }
    }
}