using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using CopyPastaNative.Services;

namespace CopyPastaNative
{
    public partial class AboutWindow : Window
    {
        private static readonly Dictionary<string, int> ClearOptions = new()
        {
            ["Off (recommended)"] = 0,
            ["15 seconds"] = 15,
            ["30 seconds"] = 30,
            ["60 seconds"] = 60,
            ["5 minutes"] = 300
        };

        private readonly SettingsService _settingsService;
        private readonly AppSettings _settings;

        public AboutWindow(string dataDirectory, SettingsService settingsService, AppSettings settings)
        {
            InitializeComponent();
            _settingsService = settingsService;
            _settings = settings;

            var version = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? "0.3.1";

            VersionText.Text = $"Version {version}";
            DataPathText.Text = $"Local data folder: {dataDirectory}";

            foreach (var option in ClearOptions.Keys)
            {
                ClipboardClearComboBox.Items.Add(option);
            }

            ClipboardClearComboBox.SelectedItem = FindLabel(_settings.ClipboardClearSeconds);
            ClipboardClearComboBox.SelectionChanged += ClipboardClearComboBox_SelectionChanged;
        }

        private static string FindLabel(int seconds)
        {
            foreach (var pair in ClearOptions)
            {
                if (pair.Value == seconds)
                    return pair.Key;
            }
            return "Off (recommended)";
        }

        private void ClipboardClearComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ClipboardClearComboBox.SelectedItem is string label &&
                ClearOptions.TryGetValue(label, out var seconds))
            {
                _settings.ClipboardClearSeconds = seconds;
                _settingsService.Save(_settings);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
