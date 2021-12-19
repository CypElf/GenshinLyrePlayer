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
using System.Threading.Tasks;

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
                var listItem = new ListBoxItem
                {
                    Content = string.Join(".", midiFile.Name.Split("\\").Last().Split(".").SkipLast(1)),
                    FontSize = 14
                };
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
                if (e.KeyPressed == (Key)Enum.Parse(typeof(Key), cfg.stopKey) && playback != null && playback.IsRunning)
                {
                    playback.Stop();
                    setIdle();
                }

                if (e.KeyPressed == (Key)Enum.Parse(typeof(Key), cfg.startKey) && MidiFilesList.Items.Count > 0 && (playback == null || !playback.IsRunning))
                {
                    {
                        Debug.WriteLine(MidiFilesList.SelectedIndex);
                        MidiFile midiFile;
                        if (MidiFilesList.SelectedIndex >= 0)
                        {
                            try
                            {
                                midiFile = MidiFile.Read(midiFiles[MidiFilesList.SelectedIndex]);
                            }
                            catch
                            {
                                MessageBox.Show("The file you're trying to play is not a valid MIDI file or is corrupted.", "Invalid file", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                        }
                        else
                        {
                            MessageBox.Show("Please select the MIDI file you want to play first.", "No file selected", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        var layout = (KeyboardLayout)Enum.Parse(typeof(KeyboardLayout), cfg.keyboardLayout);

                        playingTextBlock.Margin = new Thickness(267, 108, 0, 0);
                        playingTextBlock.Text = "Playing " + ((ListBoxItem)MidiFilesList.SelectedItem).Content;
                        progressBar.Visibility = Visibility.Visible;
                        progressBar.Value = 0;
                        progressBar.Maximum = midiFile.GetNotes().Count;

                        var player = new MIDIToKeyboardConverter(midiFile, layout, cfg.useAutoRoot ? null : cfg.customRoot, progressBar);

                        var hits = player.HitsForRootNote();

                        var totalNotesCount = midiFile.GetNotes().Count;
                        var intervalNotesCount = player.NotesCountInIntervalOfRoot();

                        var metricDuration = (MetricTimeSpan)midiFile.GetDuration(TimeSpanType.Metric);

                        infoTextBlock.Text = $"using root note {player.rootNoteNumber}\nrestitution of {hits}/{totalNotesCount} of all notes ({Math.Round((double)hits / totalNotesCount * 100, 2)}%)\n{hits}/{intervalNotesCount} notes in the interval can be played ({Math.Round((double)hits / intervalNotesCount * 100, 2)}%)";

                        infoTextBlock.Visibility = Visibility.Visible;
                        progressInfoTextBlock.Visibility = Visibility.Visible;

                        var task = Task.Run(async () =>
                        {
                            Dispatcher.Invoke(() => progressInfoTextBlock.Text = $"0:00/{metricDuration.Minutes}:{metricDuration.Seconds:00}");
                            var elapsedTime = 0;
                            var songDuration = metricDuration.Hours * 3600 + metricDuration.Minutes * 60 + metricDuration.Seconds;
                            while (elapsedTime < songDuration && progressInfoTextBlock.Visibility == Visibility.Visible)
                            {
                                await Task.Delay(1000);
                                elapsedTime++;
                                Dispatcher.Invoke(() => progressInfoTextBlock.Text = $"{elapsedTime / 60}:{elapsedTime % 60:00}/{metricDuration.Minutes}:{metricDuration.Seconds:00}");
                            }
                        });

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
            playingTextBlock.Margin = new Thickness(268, 172, 0, 0);
            playingTextBlock.Text = "IDLE";
            progressBar.Visibility = Visibility.Hidden;
            infoTextBlock.Visibility = Visibility.Hidden;
            progressInfoTextBlock.Visibility = Visibility.Hidden;
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