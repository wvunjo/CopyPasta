using System;
using System.Collections.Generic;
using System.IO;
using CopyPastaNative.Models;
using Newtonsoft.Json;

namespace CopyPastaNative.Security
{
    public sealed class SnippetImportResult
    {
        public bool Success { get; init; }
        public string Error { get; init; } = string.Empty;
        public List<Snippet> Snippets { get; init; } = new();
        public int RejectedCount { get; init; }
    }

    public static class SnippetValidator
    {
        public static string? ValidateFieldLengths(string? title, string? language, IList<string>? tags, string? code)
        {
            if (string.IsNullOrWhiteSpace(title))
                return "Snippet title is required.";
            if (title.Length > SnippetLimits.MaxTitleLength)
                return $"Snippet title exceeds {SnippetLimits.MaxTitleLength} characters.";
            if (!string.IsNullOrEmpty(language) && language.Length > SnippetLimits.MaxLanguageLength)
                return $"Language exceeds {SnippetLimits.MaxLanguageLength} characters.";
            if (string.IsNullOrWhiteSpace(code))
                return "Snippet content is required.";
            if (code.Length > SnippetLimits.MaxCodeLength)
                return $"Snippet content exceeds {SnippetLimits.MaxCodeLength} characters.";

            if (tags != null)
            {
                if (tags.Count > SnippetLimits.MaxTagCount)
                    return $"A snippet cannot have more than {SnippetLimits.MaxTagCount} tags.";
                foreach (var tag in tags)
                {
                    if (tag != null && tag.Length > SnippetLimits.MaxTagLength)
                        return $"Tag exceeds {SnippetLimits.MaxTagLength} characters.";
                }
            }

            return null;
        }

        public static bool TryValidateSnippet(Snippet? snippet, out string? error)
        {
            error = null;
            if (snippet == null)
            {
                error = "Snippet object is missing.";
                return false;
            }

            error = ValidateFieldLengths(snippet.Title, snippet.Language, snippet.Tags, snippet.Code);
            return error == null;
        }

        public static SnippetImportResult ParseAndValidateImport(string json, long? fileLengthBytes = null)
        {
            if (fileLengthBytes.HasValue && fileLengthBytes.Value > SnippetLimits.MaxImportFileBytes)
            {
                return Fail($"Import file is too large. Maximum size is {SnippetLimits.MaxImportFileBytes / (1024 * 1024)} MB.");
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                return Fail("The selected file is empty.");
            }

            List<Snippet>? imported;
            try
            {
                imported = JsonConvert.DeserializeObject<List<Snippet>>(json, SnippetJson.Settings);
            }
            catch (JsonException)
            {
                return Fail("The selected file is not valid CopyPasta JSON. Import was cancelled and your existing snippets were not changed.");
            }
            catch (Exception)
            {
                return Fail("The selected file could not be read as CopyPasta JSON.");
            }

            if (imported == null)
            {
                return Fail("The selected file does not contain a snippet list.");
            }

            if (imported.Count > SnippetLimits.MaxSnippetsPerImport)
            {
                return Fail($"Import contains {imported.Count} snippets. Maximum allowed per import is {SnippetLimits.MaxSnippetsPerImport}.");
            }

            var accepted = new List<Snippet>();
            var rejected = 0;
            foreach (var snippet in imported)
            {
                if (TryValidateSnippet(snippet, out _))
                {
                    snippet.Tags ??= new List<string>();
                    accepted.Add(snippet);
                }
                else
                {
                    rejected++;
                }
            }

            if (accepted.Count == 0)
            {
                return Fail(imported.Count == 0
                    ? "No snippets found in the selected file."
                    : "No valid snippets were found. Invalid entries were not imported.");
            }

            return new SnippetImportResult
            {
                Success = true,
                Snippets = accepted,
                RejectedCount = rejected
            };
        }

        public static SnippetImportResult ParseAndValidateImportFile(string path)
        {
            FileInfo info;
            try
            {
                info = new FileInfo(path);
            }
            catch (Exception)
            {
                return Fail("The selected file could not be accessed.");
            }

            if (!info.Exists)
            {
                return Fail("The selected file does not exist.");
            }

            if (info.Length > SnippetLimits.MaxImportFileBytes)
            {
                return Fail($"Import file is too large. Maximum size is {SnippetLimits.MaxImportFileBytes / (1024 * 1024)} MB.");
            }

            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception)
            {
                return Fail("The selected file could not be read.");
            }

            return ParseAndValidateImport(json, info.Length);
        }

        private static SnippetImportResult Fail(string error) => new()
        {
            Success = false,
            Error = error
        };
    }
}
