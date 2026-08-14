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
            Assert.Contains(path, service.LastLoadWarning);
            Assert.Equal(original, await File.ReadAllTextAsync(path));
        }

        [Fact]
        public async Task Load_OversizedFile_FailsClosed_DoesNotReadOrOverwrite()
        {
            var path = Path.Combine(_dir, "snippets.json");
            await using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.SetLength(SnippetLimits.MaxDatabaseFileBytes + 1);
            }

            var service = new SnippetService(_dir);
            var loaded = await service.GetAllSnippetsAsync();

            Assert.Empty(loaded);
            Assert.Contains("larger than", service.LastLoadWarning);
            Assert.Contains(path, service.LastLoadWarning);
            Assert.Equal(SnippetLimits.MaxDatabaseFileBytes + 1, new FileInfo(path).Length);
        }

        [Fact]
        public async Task Load_TooManySnippets_FailsClosed_DoesNotOverwrite()
        {
            var path = Path.Combine(_dir, "snippets.json");
            var original = BuildSnippetArrayJson(SnippetLimits.MaxSnippetsInDatabase + 1);
            await File.WriteAllTextAsync(path, original);

            var service = new SnippetService(_dir);
            var loaded = await service.GetAllSnippetsAsync();

            Assert.Empty(loaded);
            Assert.Contains("exceeds the maximum", service.LastLoadWarning);
            Assert.Contains(path, service.LastLoadWarning);
            Assert.Equal(original, await File.ReadAllTextAsync(path));
        }

        [Fact]
        public async Task Load_InvalidSnippetAmongValid_FailsClosed_DoesNotKeepPartialData()
        {
            var path = Path.Combine(_dir, "snippets.json");
            var original =
                "[{\"title\":\"ok\",\"language\":\"txt\",\"tags\":[],\"code\":\"body\"}," +
                "{\"title\":\"" + new string('X', SnippetLimits.MaxTitleLength + 1) +
                "\",\"language\":\"txt\",\"tags\":[],\"code\":\"body\"}]";
            await File.WriteAllTextAsync(path, original);

            var service = new SnippetService(_dir);
            var loaded = await service.GetAllSnippetsAsync();

            Assert.Empty(loaded);
            Assert.Contains("invalid snippet data", service.LastLoadWarning);
            Assert.Contains(path, service.LastLoadWarning);
            Assert.Equal(original, await File.ReadAllTextAsync(path));
        }

        private static string BuildSnippetArrayJson(int count)
        {
            var builder = new System.Text.StringBuilder();
            builder.Append('[');
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                    builder.Append(',');
                builder.Append("{\"title\":\"t").Append(i).Append("\",\"language\":\"txt\",\"tags\":[],\"code\":\"x\"}");
            }

            builder.Append(']');
            return builder.ToString();
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
