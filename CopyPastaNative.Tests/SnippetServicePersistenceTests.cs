using CopyPastaNative.Models;
using CopyPastaNative.Security;
using CopyPastaNative.Services;

namespace CopyPastaNative.Tests
{
    public class SnippetServicePersistenceTests : IDisposable
    {
        private readonly string _dir;

        public SnippetServicePersistenceTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "CopyPastaTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [Fact]
        public async Task Save_CreatesBackupOfPreviousFile()
        {
            var service = new SnippetService(_dir);
            await service.AddSnippetAsync(new Snippet("one", "txt", new List<string>(), "a"));
            await service.AddSnippetAsync(new Snippet("two", "txt", new List<string>(), "b"));

            Assert.True(File.Exists(service.SnippetsFilePath));
            Assert.True(File.Exists(service.BackupFilePath));
            var backup = await File.ReadAllTextAsync(service.BackupFilePath);
            Assert.Contains("\"one\"", backup);
        }

        [Fact]
        public async Task Load_CorruptFile_DoesNotOverwriteOriginal()
        {
            var path = Path.Combine(_dir, "snippets.json");
            var original = "NOT JSON AT ALL";
            await File.WriteAllTextAsync(path, original);

            var service = new SnippetService(_dir);
            var loaded = await service.GetAllSnippetsAsync();

            Assert.Empty(loaded);
            Assert.False(string.IsNullOrEmpty(service.LastLoadWarning));
            Assert.Equal(original, await File.ReadAllTextAsync(path));
        }

        [Fact]
        public async Task RestoreBackup_ReplacesCorruptFile()
        {
            var service = new SnippetService(_dir);
            await service.AddSnippetAsync(new Snippet("keep-me", "txt", new List<string>(), "body"));
            await service.AddSnippetAsync(new Snippet("also", "txt", new List<string>(), "body2"));

            await File.WriteAllTextAsync(service.SnippetsFilePath, "{ broken");

            var restoreService = new SnippetService(_dir);
            await restoreService.GetAllSnippetsAsync();
            Assert.True(restoreService.BackupExists);

            Assert.True(await restoreService.RestoreBackupAsync());
            var restored = await restoreService.GetAllSnippetsAsync();
            Assert.Contains(restored, s => s.Title == "keep-me");
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_dir))
                    Directory.Delete(_dir, recursive: true);
            }
            catch
            {
                // temp cleanup is best-effort
            }
        }
    }
}
