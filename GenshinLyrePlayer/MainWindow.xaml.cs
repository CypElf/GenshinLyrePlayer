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
using System.Text.RegularExpressions;
using System.Text.Json;

namespace GenshinLyrePlayer
{
    public partial class MainWindow : Window
    {
        // the int used as a key is the offset from the root note in semitones
        private LowLevelKeyboardListener listener = new LowLevelKeyboardListener();
        private List<FileStream> midiFiles = new List<FileStream>();
        private Playback playback;
        private bool useAutoRoot;

        private bool configLoaded = false;

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

            foreach (KeyboardLayout value in Enum.GetValues(typeof(KeyboardLayout)))
            {
                var item = new ComboBoxItem();
                item.Tag = value;
                item.Content = value.ToString();
                if (value == KeyboardLayout.QWERTY) item.IsSelected = true;
                layoutComboBox.Items.Add(item);
            }

            useAutoRoot = autoRootCheckbox.IsChecked ?? false;

            listener = new LowLevelKeyboardListener();
            listener.OnKeyPressed += onKeyPressed;
            listener.HookKeyboard();

            loadSave();
        }

        private void onKeyPressed(object sender, KeyPressedArgs e)
        {
            Debug.WriteLine("PRESSED " + e.KeyPressed.ToString());

            if (e.KeyPressed == Key.F7 && playback != null && playback.IsRunning)
            {
                playback.Stop();
                playingTextBlock.Text = "IDLE";
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

                    var player = new MIDIToKeyboardConverter(midiFile, layout, useAutoRoot ? -1 : int.Parse(customRootInput.Text));

                    playback = midiFile.GetPlayback(player);
                    playback.Start();

                    playingTextBlock.Text = "Playing: " + ((ListBoxItem)MidiFilesList.SelectedItem).Content;
                }
            }
        }

        private void onLayoutChanged(object sender, SelectionChangedEventArgs e)
        {
            save();
        }

        private void onCustomNoteChanged(object sender, TextChangedEventArgs e)
        {
            save();
        }

        private void onAutoRootChecked(object sender, RoutedEventArgs e)
        {
            useAutoRoot = true;
            if (customRootInput != null)
            {
                customRootInput.IsEnabled = false;
                customRootInput.Text = "";
            }
            save();
        }

        private void onAutoRootUnchecked(object sender, RoutedEventArgs e)
        {
            useAutoRoot = false;
            customRootInput.IsEnabled = true;
            save();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            var regex = new Regex("[0-9]+");
            var isMatch = regex.IsMatch(e.Text);
            if (isMatch)
            {
                var number = int.Parse(((TextBox)sender).Text + e.Text);
                var isOk = number >= 0 && number < 128;
                e.Handled = !isOk;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void save()
        {
            if (configLoaded && layoutComboBox != null && autoRootCheckbox != null && customRootInput != null)
            {
                var config = new Config();
                config.keyboardLayout = (string)((ComboBoxItem)layoutComboBox.SelectedItem).Content;
                config.useAutoRoot = (bool)autoRootCheckbox.IsChecked;
                if (!config.useAutoRoot)
                {
                    config.customRoot = customRootInput.Text.Length > 0 ? int.Parse(customRootInput.Text) : null;
                }
                else
                {
                    config.customRoot = null;
                }
                var jsonData = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText("config.json", jsonData);
            }
        }

        private void loadSave()
        {
            var config = JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json"));
            layoutComboBox.Text = config.keyboardLayout; // this doesn't just change the text value but will select the right ComboBoxItem matching
            autoRootCheckbox.IsChecked = config.useAutoRoot;
            if (!config.useAutoRoot)
            {
                customRootInput.Text = config.customRoot.ToString();
            }
            configLoaded = true;
        }
    }
}