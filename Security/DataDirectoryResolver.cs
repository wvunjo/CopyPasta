using System;
using System.IO;

namespace CopyPastaNative.Security
{
    public static class DataDirectoryResolver
    {
        public const string FolderName = "CopyPasta";

        public static string Resolve()
        {
            return Resolve(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FolderName),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), FolderName));
        }

        public static string Resolve(string localDirectory, string roamingDirectory)
        {
            string localDb = Path.Combine(localDirectory, "snippets.json");
            string roamingDb = Path.Combine(roamingDirectory, "snippets.json");

            if (File.Exists(localDb))
                return localDirectory;

            if (!File.Exists(roamingDb))
                return localDirectory;

            try
            {
                Directory.CreateDirectory(localDirectory);
                File.Copy(roamingDb, localDb, overwrite: false);
                CopyIfPresent(Path.Combine(roamingDirectory, "snippets.json.bak"), Path.Combine(localDirectory, "snippets.json.bak"));
                CopyIfPresent(Path.Combine(roamingDirectory, "settings.json"), Path.Combine(localDirectory, "settings.json"));
                return localDirectory;
            }
            catch (Exception)
            {
                return roamingDirectory;
            }
        }

        private static void CopyIfPresent(string source, string destination)
        {
            if (File.Exists(source) && !File.Exists(destination))
                File.Copy(source, destination, overwrite: false);
        }
    }
}
