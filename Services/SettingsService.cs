using System;
using System.IO;
using CopyPastaNative.Security;
using Newtonsoft.Json;

namespace CopyPastaNative.Services
{
    public sealed class SettingsService
    {
        private readonly string _filePath;

        public SettingsService(string dataDirectory)
        {
            _filePath = Path.Combine(dataDirectory, "settings.json");
        }

        public AppSettings Load()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return new AppSettings();

                var json = File.ReadAllText(_filePath);
                var settings = JsonConvert.DeserializeObject<AppSettings>(json, SnippetJson.Settings);
                if (settings == null)
                    return new AppSettings();

                if (settings.ClipboardClearSeconds < 0)
                    settings.ClipboardClearSeconds = 0;

                return settings;
            }
            catch (Exception)
            {
                return new AppSettings();
            }
        }

        public void Save(AppSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings, SnippetJson.Settings);
            var tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, json);
            if (File.Exists(_filePath))
            {
                File.Replace(tempPath, _filePath, null);
            }
            else
            {
                File.Move(tempPath, _filePath);
            }
        }
    }
}
