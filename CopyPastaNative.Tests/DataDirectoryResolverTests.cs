using CopyPastaNative.Security;

namespace CopyPastaNative.Tests
{
    public class DataDirectoryResolverTests : IDisposable
    {
        private readonly string _root;

        public DataDirectoryResolverTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "CopyPastaTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [Fact]
        public void NeitherExists_ReturnsLocal()
        {
            string local = Path.Combine(_root, "local");
            string roaming = Path.Combine(_root, "roaming");

            string resolved = DataDirectoryResolver.Resolve(local, roaming);

            Assert.Equal(local, resolved);
            Assert.False(File.Exists(Path.Combine(local, "snippets.json")));
        }

        [Fact]
        public void LocalExists_IsPreferredOverRoaming()
        {
            string local = Path.Combine(_root, "local");
            string roaming = Path.Combine(_root, "roaming");
            Directory.CreateDirectory(local);
            Directory.CreateDirectory(roaming);
            File.WriteAllText(Path.Combine(local, "snippets.json"), "[\"local\"]");
            File.WriteAllText(Path.Combine(roaming, "snippets.json"), "[\"roaming\"]");

            string resolved = DataDirectoryResolver.Resolve(local, roaming);

            Assert.Equal(local, resolved);
            Assert.Equal("[\"local\"]", File.ReadAllText(Path.Combine(local, "snippets.json")));
            Assert.Equal("[\"roaming\"]", File.ReadAllText(Path.Combine(roaming, "snippets.json")));
        }

        [Fact]
        public void MigratesRoamingWhenLocalMissing_LeavesOriginalInPlace()
        {
            string local = Path.Combine(_root, "local");
            string roaming = Path.Combine(_root, "roaming");
            Directory.CreateDirectory(roaming);
            File.WriteAllText(Path.Combine(roaming, "snippets.json"), "[\"migrated\"]");
            File.WriteAllText(Path.Combine(roaming, "snippets.json.bak"), "[\"backup\"]");
            File.WriteAllText(Path.Combine(roaming, "settings.json"), "{\"clipboardClearSeconds\":15}");

            string resolved = DataDirectoryResolver.Resolve(local, roaming);

            Assert.Equal(local, resolved);
            Assert.Equal("[\"migrated\"]", File.ReadAllText(Path.Combine(local, "snippets.json")));
            Assert.Equal("[\"backup\"]", File.ReadAllText(Path.Combine(local, "snippets.json.bak")));
            Assert.Equal("{\"clipboardClearSeconds\":15}", File.ReadAllText(Path.Combine(local, "settings.json")));
            Assert.True(File.Exists(Path.Combine(roaming, "snippets.json")));
            Assert.True(File.Exists(Path.Combine(roaming, "snippets.json.bak")));
            Assert.True(File.Exists(Path.Combine(roaming, "settings.json")));
        }

        [Fact]
        public void DoesNotOverwriteExistingLocalDatabase()
        {
            string local = Path.Combine(_root, "local");
            string roaming = Path.Combine(_root, "roaming");
            Directory.CreateDirectory(local);
            Directory.CreateDirectory(roaming);
            File.WriteAllText(Path.Combine(local, "snippets.json"), "[\"newer-local\"]");
            File.WriteAllText(Path.Combine(roaming, "snippets.json"), "[\"older-roaming\"]");

            DataDirectoryResolver.Resolve(local, roaming);

            Assert.Equal("[\"newer-local\"]", File.ReadAllText(Path.Combine(local, "snippets.json")));
        }

        [Fact]
        public void MigrationFailure_KeepsUsingRoaming()
        {
            string localAsFile = Path.Combine(_root, "local-is-a-file");
            File.WriteAllText(localAsFile, "not-a-directory");
            string roaming = Path.Combine(_root, "roaming");
            Directory.CreateDirectory(roaming);
            File.WriteAllText(Path.Combine(roaming, "snippets.json"), "[\"stay\"]");

            string resolved = DataDirectoryResolver.Resolve(localAsFile, roaming);

            Assert.Equal(roaming, resolved);
            Assert.Equal("[\"stay\"]", File.ReadAllText(Path.Combine(roaming, "snippets.json")));
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_root))
                    Directory.Delete(_root, recursive: true);
            }
            catch
            {
                // temp cleanup is best-effort
            }
        }
    }
}
