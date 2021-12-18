using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Text.Json;
using System.IO;
using System;

namespace GenshinLyrePlayer
{
    public partial class Settings : Window
    {
        private Config cfg;

        private string startKeyBackup;
        private string stopKeyBackup;
        private bool listeningForStartKey = false;
        private bool listeningForStopKey = false;

        public Settings(Config cfg)
        {
            this.cfg = cfg;
            InitializeComponent();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            foreach (KeyboardLayout value in Enum.GetValues(typeof(KeyboardLayout)))
            {
                var item = new ComboBoxItem();
                item.Tag = value;
                item.Content = value.ToString();
                layoutComboBox.Items.Add(item);
            }

            autoRootCheckbox.IsChecked = cfg.useAutoRoot;

            if (!cfg.useAutoRoot)
            {
                customRootInput.Text = cfg.customRoot.ToString();
            }

            layoutComboBox.Text = cfg.keyboardLayout; // this doesn't just change the text value but will select the right ComboBoxItem matching

            startKeyButton.Content = cfg.startKey;
            stopKeyButton.Content = cfg.stopKey;
        }

        private void onStartButtonClick(object sender, RoutedEventArgs e)
        {
            if (!listeningForStartKey)
            {
                startKeyBackup = (string)startKeyButton.Content;
                startKeyButton.Content = "PRESS A KEY";
                
            }
            else
            {
                startKeyButton.Content = startKeyBackup;
            }
            listeningForStartKey = !listeningForStartKey;
        }

        private void onStartButtonKeyDown(object sender, KeyEventArgs e)
        {
            if (listeningForStartKey)
            {
                cfg.startKey = e.Key.ToString();
                startKeyButton.Content = e.Key.ToString();
                listeningForStartKey = false;
                save();
            }
        }

        private void onStopButtonClick(object sender, RoutedEventArgs e)
        {
            if (!listeningForStopKey)
            {
                stopKeyBackup = (string)stopKeyButton.Content;
                stopKeyButton.Content = "PRESS A KEY";
            }
            else
            {
                stopKeyButton.Content = stopKeyBackup;
            }
            listeningForStopKey = !listeningForStopKey;
        }

        private void onStopButtonKeyDown(object sender, KeyEventArgs e)
        {
            if (listeningForStopKey)
            {
                cfg.stopKey = e.Key.ToString();
                stopKeyButton.Content = e.Key.ToString();
                listeningForStopKey = false;
                save();
            }
        }

        private void onLayoutChanged(object sender, SelectionChangedEventArgs e)
        {
            cfg.keyboardLayout = (string)((ListBoxItem)layoutComboBox.SelectedItem).Content;
            save();
        }

        private void onCustomNoteChanged(object sender, TextChangedEventArgs e)
        {
            cfg.customRoot = customRootInput.Text.Length > 0 ? int.Parse(customRootInput.Text) : null;
            save();
        }

        private void onAutoRootChecked(object sender, RoutedEventArgs e)
        {
            if (customRootInput != null)
            {
                cfg.useAutoRoot = true;
                customRootInput.IsEnabled = false;
                customRootInput.Text = "";
                save();
            }
        }

        private void onAutoRootUnchecked(object sender, RoutedEventArgs e)
        {
            cfg.useAutoRoot = false;
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
            var jsonData = JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("config.json", jsonData);
        }
    }
}
