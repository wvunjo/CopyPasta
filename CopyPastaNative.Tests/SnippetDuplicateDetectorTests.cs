using CopyPastaNative.Models;
using CopyPastaNative.Security;

namespace CopyPastaNative.Tests
{
    public class SnippetDuplicateDetectorTests
    {
        [Fact]
        public void ExactDuplicate_SmallSnippets()
        {
            var a = new Snippet("Hello", "txt", new List<string>(), "Console.WriteLine();");
            var b = new Snippet("Hello", "txt", new List<string>(), "Console.WriteLine();");

            Assert.Equal(1.0, SnippetDuplicateDetector.CalculateSimilarity(a, b));
        }

        [Fact]
        public void ExactDuplicate_LargeSnippets_CompletesWithBoundedMemory()
        {
            string body = new string('A', SnippetLimits.MaxCodeLength);
            var a = new Snippet("Large", "txt", new List<string>(), body);
            var b = new Snippet("Large", "txt", new List<string>(), body);

            GC.Collect();
            long before = GC.GetTotalMemory(true);
            double similarity = SnippetDuplicateDetector.CalculateSimilarity(a, b);
            GC.Collect();
            long after = GC.GetTotalMemory(true);

            Assert.Equal(1.0, similarity);
            Assert.True(after - before < 32L * 1024 * 1024, "Large exact comparison must not allocate an n×m matrix.");
        }

        [Fact]
        public void ClearlyDifferent_LargeSnippets()
        {
            string left = new string('A', SnippetLimits.MaxCodeLength);
            string right = new string('B', SnippetLimits.MaxCodeLength);
            var a = new Snippet("A", "txt", new List<string>(), left);
            var b = new Snippet("B", "txt", new List<string>(), right);

            Assert.True(SnippetDuplicateDetector.CalculateSimilarity(a, b) < SnippetLimits.DuplicateSimilarityThreshold);
        }

        [Fact]
        public void NearDuplicate_SmallSnippets()
        {
            var a = new Snippet("Sort array", "js", new List<string>(), "function sort(items) { return items.sort(); }");
            var b = new Snippet("Sort array", "js", new List<string>(), "function sort(items) { return items.sort(); } ");

            Assert.True(SnippetDuplicateDetector.CalculateSimilarity(a, b) >= SnippetLimits.DuplicateSimilarityThreshold);
        }

        [Fact]
        public void MaximumSize_ValidSnippetComparison_DifferentMiddles()
        {
            int affix = SnippetLimits.LargeSnippetAffixLength;
            string prefix = new string('P', affix);
            string suffix = new string('S', affix);
            string midA = new string('A', SnippetLimits.MaxCodeLength - (affix * 2));
            string midB = new string('B', SnippetLimits.MaxCodeLength - (affix * 2));

            double similarity = SnippetDuplicateDetector.CodeSimilarity(prefix + midA + suffix, prefix + midB + suffix);

            Assert.True(similarity >= 0.85);
        }

        [Fact]
        public void VeryDifferentLengths_AreNotSimilar()
        {
            string large = new string('A', SnippetLimits.MaxCodeLength);
            string tiny = "A";

            Assert.Equal(0.0, SnippetDuplicateDetector.CodeSimilarity(large, tiny));
        }

        [Fact]
        public void EmptyStrings_AreExact()
        {
            Assert.Equal(1.0, SnippetDuplicateDetector.CodeSimilarity(string.Empty, string.Empty));
        }

        [Fact]
        public void WhitespaceOnly_Bodies_AreSkippedInOverallScore()
        {
            var a = new Snippet("Title", "txt", new List<string>(), "   ");
            var b = new Snippet("Title", "txt", new List<string>(), "\t");

            Assert.Equal(0.4, SnippetDuplicateDetector.CalculateSimilarity(a, b), 5);
        }

        [Fact]
        public void UnicodeContent_ExactMatch()
        {
            const string code = "console.log('こんにちは 🎉 café');";
            var a = new Snippet("挨拶", "js", new List<string>(), code);
            var b = new Snippet("挨拶", "js", new List<string>(), code);

            Assert.Equal(1.0, SnippetDuplicateDetector.CalculateSimilarity(a, b));
        }

        [Fact]
        public void ExactCode_DifferentTitles_ScoresBelowThreshold()
        {
            var a = new Snippet("Alpha", "txt", new List<string>(), "identical-body");
            var b = new Snippet("Omega", "txt", new List<string>(), "identical-body");

            double similarity = SnippetDuplicateDetector.CalculateSimilarity(a, b);
            Assert.Equal(1.0, SnippetDuplicateDetector.CodeSimilarity(a.Code, b.Code));
            Assert.True(similarity < SnippetLimits.DuplicateSimilarityThreshold);
        }

        [Fact]
        public void TwoRowLevenshtein_KnownDistance()
        {
            Assert.Equal(3, SnippetDuplicateDetector.TwoRowLevenshtein("kitten", "sitting", 10));
        }

        [Fact]
        public void TwoRowLevenshtein_EarlyExitWhenBeyondThreshold()
        {
            int distance = SnippetDuplicateDetector.TwoRowLevenshtein("aaaaaaaa", "bbbbbbbb", 1);
            Assert.True(distance > 1);
        }
    }
}
