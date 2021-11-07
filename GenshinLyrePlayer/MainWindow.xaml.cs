using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using WindowsInput.Native;
using WindowsInput;
using System.Threading;

namespace GenshinLyrePlayer
{
    public partial class MainWindow : Window
    {
        private static Dictionary<int, Key> notesConverter = new Dictionary<int, Key>
        {
            { 0, Key.A }
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

            if (e.KeyPressed == Key.F6)
            {
                simulator.Keyboard.KeyDown(VirtualKeyCode.VK_A);
                simulator.Keyboard.KeyUp(VirtualKeyCode.VK_A);
                Thread.Sleep(1000);
                simulator.Keyboard.KeyDown(VirtualKeyCode.VK_A);
                simulator.Keyboard.KeyUp(VirtualKeyCode.VK_A);
                Thread.Sleep(1000);
                simulator.Keyboard.KeyDown(VirtualKeyCode.VK_A);
                simulator.Keyboard.KeyUp(VirtualKeyCode.VK_A);
                Thread.Sleep(1000);
                simulator.Keyboard.KeyDown(VirtualKeyCode.VK_A);
                simulator.Keyboard.KeyUp(VirtualKeyCode.VK_A);
                Thread.Sleep(1000);
                simulator.Keyboard.KeyDown(VirtualKeyCode.VK_A);
                simulator.Keyboard.KeyUp(VirtualKeyCode.VK_A);
                Thread.Sleep(1000);



                /* var midiFile = MidiFile.Read(midiFiles[0]);

                IEnumerable<Note> notes = midiFile.GetNotes();

                foreach (var note in notes)
                {
                    Debug.WriteLine(note.NoteNumber);
                } */
            }
        }
    }
}