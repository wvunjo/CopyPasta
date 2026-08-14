using CopyPastaNative.Models;
using CopyPastaNative.Security;
using Newtonsoft.Json;

namespace CopyPastaNative.Tests
{
    public class SnippetValidatorTests
    {
        [Fact]
        public void SafeJsonSettings_DisableTypeNameHandling()
        {
            Assert.Equal(TypeNameHandling.None, SnippetJson.Settings.TypeNameHandling);
            Assert.Equal(SnippetLimits.MaxJsonDepth, SnippetJson.Settings.MaxDepth);
        }

        [Fact]
        public void Parse_ValidSnippetList_Succeeds()
        {
            var json = JsonConvert.SerializeObject(new[]
            {
                new Snippet("Hello", "csharp", new List<string> { "demo" }, "Console.WriteLine();")
            }, SnippetJson.Settings);

            var result = SnippetValidator.ParseAndValidateImport(json);

            Assert.True(result.Success);
            Assert.Single(result.Snippets);
            Assert.Equal(0, result.RejectedCount);
        }

        [Fact]
        public void Parse_MalformedJson_FailsCleanly()
        {
            var result = SnippetValidator.ParseAndValidateImport("{ not a snippet list");

            Assert.False(result.Success);
            Assert.Contains("not valid CopyPasta JSON", result.Error);
            Assert.Empty(result.Snippets);
        }

        [Fact]
        public void Parse_ObjectInsteadOfArray_Fails()
        {
            var result = SnippetValidator.ParseAndValidateImport("{\"title\":\"x\"}");

            Assert.False(result.Success);
        }

        [Fact]
        public void Parse_TooManySnippets_Fails()
        {
            var snippets = Enumerable.Range(0, SnippetLimits.MaxSnippetsPerImport + 1)
                .Select(i => new Snippet($"t{i}", "txt", new List<string>(), "code"))
                .ToList();
            var json = JsonConvert.SerializeObject(snippets, SnippetJson.Settings);

            var result = SnippetValidator.ParseAndValidateImport(json);

            Assert.False(result.Success);
            Assert.Contains("Maximum allowed", result.Error);
        }

        [Fact]
        public void Parse_OversizedFileLength_FailsWithoutParsing()
        {
            var result = SnippetValidator.ParseAndValidateImport("[]", SnippetLimits.MaxImportFileBytes + 1);

            Assert.False(result.Success);
            Assert.Contains("too large", result.Error);
        }

        [Fact]
        public void Parse_OverlongTitle_IsRejected()
        {
            var json = JsonConvert.SerializeObject(new[]
            {
                new Snippet(new string('A', SnippetLimits.MaxTitleLength + 1), "txt", new List<string>(), "code")
            }, SnippetJson.Settings);

            var result = SnippetValidator.ParseAndValidateImport(json);

            Assert.False(result.Success);
            Assert.Empty(result.Snippets);
        }

        [Fact]
        public void Parse_TypeNamePayload_DoesNotInstantiateArbitraryTypes()
        {
            var json = """
                [
                  {
                    "$type": "System.IO.FileInfo, System.Runtime",
                    "title": "ok",
                    "language": "txt",
                    "tags": [],
                    "code": "hi"
                  }
                ]
                """;

            var result = SnippetValidator.ParseAndValidateImport(json);

            Assert.True(result.Success);
            Assert.Single(result.Snippets);
            Assert.IsType<Snippet>(result.Snippets[0]);
        }
    }
}
