using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using System.Threading.Tasks;

namespace GenshinLyrePlayer
{
    public partial class MainWindow : Window
    {
        // the int used as a key is the offset from the root note in semitones
        private LowLevelKeyboardListener listener = new LowLevelKeyboardListener();
        private List<FileStream> midiFiles = new List<FileStream>();

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
        }

        private void onKeyPressed(object sender, KeyPressedArgs e)
        {
            Debug.WriteLine("PRESSED " + e.KeyPressed.ToString());

            if (e.KeyPressed == Key.F6 && MidiFilesList.Items.Count > 0)
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

                    Task.Run(() =>
                    {
                        var output = new MIDIToKeyboardConverter();
                        output.ConfigureFor(midiFile);
                        listener.UnHookKeyboard(); // mandatory or the hook will slow down the first few notes played for some reason... fuck it
                        midiFile.Play(output);
                        listener.HookKeyboard();
                        Debug.WriteLine("finished playing that");
                    });
                }
            }
        }
    }
}