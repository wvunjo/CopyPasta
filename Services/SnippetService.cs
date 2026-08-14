using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CopyPastaNative.Models;
using CopyPastaNative.Security;
using Newtonsoft.Json;

namespace CopyPastaNative.Services
{
    public class SnippetService
    {
        private readonly string _filePath;
        private readonly string _backupPath;
        private List<Snippet> _snippets = new();
        private bool _isLoaded;

        public string DataDirectory { get; }
        public string SnippetsFilePath => _filePath;
        public string BackupFilePath => _backupPath;
        public string? LastLoadWarning { get; private set; }
        public bool BackupExists => File.Exists(_backupPath);

        public SnippetService()
            : this(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CopyPasta"))
        {
        }

        public SnippetService(string dataDirectory)
        {
            if (string.IsNullOrWhiteSpace(dataDirectory))
                throw new ArgumentException("Data directory is required.", nameof(dataDirectory));

            DataDirectory = dataDirectory;
            _filePath = Path.Combine(dataDirectory, "snippets.json");
            _backupPath = Path.Combine(dataDirectory, "snippets.json.bak");

            Directory.CreateDirectory(dataDirectory);
        }

        public async Task<List<Snippet>> GetAllSnippetsAsync()
        {
            if (!_isLoaded)
            {
                await LoadSnippetsAsync();
            }

            System.Diagnostics.Debug.WriteLine($"GetAllSnippetsAsync: Returning {_snippets.Count} snippets");
            return _snippets.ToList();
        }

        public async Task<Snippet?> GetSnippetByIdAsync(string id)
        {
            if (!_isLoaded)
            {
                await LoadSnippetsAsync();
            }
            return _snippets.FirstOrDefault(s => s.Id == id);
        }

        public async Task AddSnippetAsync(Snippet snippet)
        {
            if (!SnippetValidator.TryValidateSnippet(snippet, out var error))
                throw new InvalidOperationException(error);

            snippet.UpdatedAt = DateTime.Now;
            _snippets.Add(snippet);
            await SaveSnippetsAsync();
        }

        public async Task UpdateSnippetAsync(Snippet snippet)
        {
            if (!SnippetValidator.TryValidateSnippet(snippet, out var error))
                throw new InvalidOperationException(error);

            var existing = _snippets.FirstOrDefault(s => s.Id == snippet.Id);
            if (existing != null)
            {
                existing.Title = snippet.Title;
                existing.Language = snippet.Language;
                existing.Tags = snippet.Tags;
                existing.Code = snippet.Code;
                existing.IsFavorite = snippet.IsFavorite;
                existing.UpdatedAt = DateTime.Now;
                await SaveSnippetsAsync();
            }
        }

        public async Task DeleteSnippetAsync(string id)
        {
            var snippet = _snippets.FirstOrDefault(s => s.Id == id);
            if (snippet != null)
            {
                _snippets.Remove(snippet);
                await SaveSnippetsAsync();
            }
        }

        public async Task ReplaceAllSnippetsAsync(IEnumerable<Snippet> snippets)
        {
            var next = snippets.ToList();
            foreach (var snippet in next)
            {
                if (!SnippetValidator.TryValidateSnippet(snippet, out var error))
                    throw new InvalidOperationException(error);
            }

            _snippets = next;
            await SaveSnippetsAsync();
        }

        public async Task<List<Snippet>> SearchSnippetsAsync(string searchTerm)
        {
            if (!_isLoaded)
            {
                await LoadSnippetsAsync();
            }

            if (string.IsNullOrWhiteSpace(searchTerm))
                return _snippets.ToList();

            searchTerm = searchTerm.ToLowerInvariant();
            return _snippets.Where(s =>
                s.Title?.ToLowerInvariant().Contains(searchTerm) == true ||
                s.Code?.ToLowerInvariant().Contains(searchTerm) == true ||
                s.Tags?.Any(tag => tag?.ToLowerInvariant().Contains(searchTerm) == true) == true
            ).ToList();
        }

        public async Task<List<Snippet>> GetSnippetsByTagsAsync(List<string> tags)
        {
            if (!_isLoaded)
            {
                await LoadSnippetsAsync();
            }

            if (tags == null || tags.Count == 0)
                return _snippets.ToList();

            return _snippets.Where(s =>
                s.Tags != null && tags.All(tag => s.Tags.Contains(tag))
            ).ToList();
        }

        public List<string> GetAllTags()
        {
            try
            {
                if (_snippets == null || _snippets.Count == 0)
                    return new List<string>();

                return _snippets
                    .Where(s => s.Tags != null)
                    .SelectMany(s => s.Tags)
                    .Where(tag => !string.IsNullOrEmpty(tag))
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting all tags: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<List<Snippet>> FindPotentialDuplicatesAsync(Snippet newSnippet)
        {
            if (!_isLoaded)
            {
                await LoadSnippetsAsync();
            }

            if (newSnippet == null)
                return new List<Snippet>();

            var potentialDuplicates = new List<Snippet>();

            foreach (var existingSnippet in _snippets)
            {
                if (existingSnippet.Id == newSnippet.Id)
                    continue;

                double similarity = CalculateSimilarity(existingSnippet, newSnippet);

                if (similarity >= 0.70)
                {
                    potentialDuplicates.Add(existingSnippet);
                }
            }

            return potentialDuplicates.OrderByDescending(s =>
                CalculateSimilarity(s, newSnippet)
            ).ToList();
        }

        private double CalculateSimilarity(Snippet snippet1, Snippet snippet2)
        {
            double totalSimilarity = 0.0;
            int checks = 0;

            if (!string.IsNullOrWhiteSpace(snippet1.Title) && !string.IsNullOrWhiteSpace(snippet2.Title))
            {
                double titleSimilarity = CalculateStringSimilarity(
                    snippet1.Title.ToLowerInvariant(),
                    snippet2.Title.ToLowerInvariant()
                );
                totalSimilarity += titleSimilarity * 0.4;
                checks++;
            }

            if (!string.IsNullOrWhiteSpace(snippet1.Code) && !string.IsNullOrWhiteSpace(snippet2.Code))
            {
                double codeSimilarity = CalculateStringSimilarity(
                    snippet1.Code.ToLowerInvariant(),
                    snippet2.Code.ToLowerInvariant()
                );
                totalSimilarity += codeSimilarity * 0.6;
                checks++;
            }

            return checks > 0 ? totalSimilarity : 0.0;
        }

        private double CalculateStringSimilarity(string str1, string str2)
        {
            if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
                return 0.0;

            if (str1.Equals(str2, StringComparison.OrdinalIgnoreCase))
                return 1.0;

            int maxLength = Math.Max(str1.Length, str2.Length);
            if (maxLength == 0)
                return 1.0;

            int distance = LevenshteinDistance(str1, str2);
            double similarity = 1.0 - ((double)distance / maxLength);

            return Math.Max(0.0, similarity);
        }

        private int LevenshteinDistance(string str1, string str2)
        {
            int n = str1.Length;
            int m = str2.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0)
                return m;
            if (m == 0)
                return n;

            for (int i = 0; i <= n; i++)
                d[i, 0] = i;

            for (int j = 0; j <= m; j++)
                d[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (str2[j - 1] == str1[i - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost
                    );
                }
            }

            return d[n, m];
        }

        public async Task ResetToSampleDataAsync()
        {
            _snippets = CreateSampleSnippets();
            await SaveSnippetsAsync();
        }

        public async Task<bool> RestoreBackupAsync()
        {
            if (!File.Exists(_backupPath))
                return false;

            File.Copy(_backupPath, _filePath, overwrite: true);
            _isLoaded = false;
            _snippets.Clear();
            await LoadSnippetsAsync();
            return string.IsNullOrEmpty(LastLoadWarning) || _snippets.Count > 0;
        }

        public async Task LoadSnippetsAsync()
        {
            LastLoadWarning = null;

            try
            {
                if (!File.Exists(_filePath))
                {
                    _snippets = CreateSampleSnippets();
                    _isLoaded = true;
                    await SaveSnippetsAsync();
                    return;
                }

                var json = await File.ReadAllTextAsync(_filePath);
                List<Snippet>? loaded;
                try
                {
                    loaded = JsonConvert.DeserializeObject<List<Snippet>>(json, SnippetJson.Settings);
                }
                catch (JsonException)
                {
                    _snippets = new List<Snippet>();
                    _isLoaded = true;
                    LastLoadWarning = "The snippet database could not be read because it is not valid JSON. Existing data was left unchanged.";
                    return;
                }

                if (loaded == null)
                {
                    _snippets = new List<Snippet>();
                    _isLoaded = true;
                    LastLoadWarning = "The snippet database did not contain a snippet list. Existing data was left unchanged.";
                    return;
                }

                var accepted = new List<Snippet>();
                var rejected = 0;
                foreach (var snippet in loaded)
                {
                    if (SnippetValidator.TryValidateSnippet(snippet, out _))
                    {
                        snippet.Tags ??= new List<string>();
                        accepted.Add(snippet);
                    }
                    else
                    {
                        rejected++;
                    }
                }

                _snippets = accepted;
                _isLoaded = true;
                if (rejected > 0)
                {
                    LastLoadWarning = $"{rejected} snippet(s) in the local database were skipped because they failed validation.";
                }
            }
            catch (Exception ex)
            {
                _snippets = new List<Snippet>();
                _isLoaded = true;
                LastLoadWarning = "The snippet database could not be opened. Existing data was left unchanged.";
                System.Diagnostics.Debug.WriteLine($"LoadSnippetsAsync: {ex.GetType().Name}");
            }
        }

        public async Task SaveSnippetsAsync()
        {
            var json = JsonConvert.SerializeObject(_snippets, SnippetJson.Settings);
            var tempPath = _filePath + ".tmp";

            await File.WriteAllTextAsync(tempPath, json);

            if (File.Exists(_filePath))
            {
                File.Replace(tempPath, _filePath, _backupPath);
            }
            else
            {
                File.Move(tempPath, _filePath);
            }
        }

        private List<Snippet> CreateSampleSnippets()
        {
            return new List<Snippet>
            {
                new Snippet(
                    "React useState Hook",
                    "javascript",
                    new List<string> { "react", "hooks", "state" },
                    "import { useState } from 'react';\n\nfunction Example() {\n  const [count, setCount] = useState(0);\n  \n  return (\n    <div>\n      <p>You clicked {count} times</p>\n      <button onClick={() => setCount(count + 1)}>\n        Click me\n      </button>\n    </div>\n  );\n}"
                ),
                new Snippet(
                    "Python List Comprehension",
                    "python",
                    new List<string> { "python", "list", "comprehension" },
                    "# Basic list comprehension\nsquares = [x**2 for x in range(10)]\n\n# With condition\neven_squares = [x**2 for x in range(10) if x % 2 == 0]\n\n# Nested comprehension\nmatrix = [[i+j for j in range(3)] for i in range(3)]"
                ),
                new Snippet(
                    "CSS Flexbox Center",
                    "css",
                    new List<string> { "css", "flexbox", "layout" },
                    ".container {\n  display: flex;\n  justify-content: center;\n  align-items: center;\n  min-height: 100vh;\n}\n\n.item {\n  /* Your content here */\n}"
                ),
                new Snippet(
                    "PowerShell Get-Process",
                    "powershell",
                    new List<string> { "PS", "powershell", "process" },
                    "# Get all running processes\nGet-Process | Where-Object {$_.CPU -gt 10} | Sort-Object CPU -Descending\n\n# Get specific process by name\nGet-Process -Name 'notepad' -ErrorAction SilentlyContinue\n\n# Get process with custom properties\nGet-Process | Select-Object Name, Id, CPU, WorkingSet | Format-Table -AutoSize"
                ),
                new Snippet(
                    "Java Stream API Example",
                    "java",
                    new List<string> { "java", "stream", "collections" },
                    "import java.util.List;\nimport java.util.stream.Collectors;\n\n// Filter and map using streams\nList<String> names = List.of(\"Alice\", \"Bob\", \"Charlie\", \"David\");\nList<String> filteredNames = names.stream()\n    .filter(name -> name.length() > 4)\n    .map(String::toUpperCase)\n    .collect(Collectors.toList());\n\nSystem.out.println(filteredNames); // [ALICE, CHARLIE, DAVID]"
                )
            };
        }
    }
}
