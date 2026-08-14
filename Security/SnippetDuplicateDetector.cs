using System;
using CopyPastaNative.Models;

namespace CopyPastaNative.Security
{
    public static class SnippetDuplicateDetector
    {
        public static double CalculateSimilarity(Snippet snippet1, Snippet snippet2)
        {
            double totalSimilarity = 0.0;
            int checks = 0;

            if (!string.IsNullOrWhiteSpace(snippet1.Title) && !string.IsNullOrWhiteSpace(snippet2.Title))
            {
                totalSimilarity += TitleSimilarity(snippet1.Title, snippet2.Title) * 0.4;
                checks++;
            }

            if (!string.IsNullOrWhiteSpace(snippet1.Code) && !string.IsNullOrWhiteSpace(snippet2.Code))
            {
                totalSimilarity += CodeSimilarity(snippet1.Code, snippet2.Code) * 0.6;
                checks++;
            }

            return checks > 0 ? totalSimilarity : 0.0;
        }

        public static double TitleSimilarity(string title1, string title2)
        {
            return BoundedEditSimilarity(title1, title2, allowEditDistance: true);
        }

        public static double CodeSimilarity(string code1, string code2)
        {
            if (string.Equals(code1, code2, StringComparison.OrdinalIgnoreCase))
                return 1.0;

            int maxLength = Math.Max(code1.Length, code2.Length);
            if (maxLength == 0)
                return 1.0;

            double lengthRatio = (double)Math.Min(code1.Length, code2.Length) / maxLength;
            if (lengthRatio < SnippetLimits.DuplicateSimilarityThreshold)
                return 0.0;

            bool smallEnough =
                code1.Length <= SnippetLimits.MaxEditDistanceCodeLength &&
                code2.Length <= SnippetLimits.MaxEditDistanceCodeLength;

            if (smallEnough)
                return BoundedEditSimilarity(code1, code2, allowEditDistance: true);

            return LargeCodeSimilarity(code1, code2);
        }

        private static double LargeCodeSimilarity(string code1, string code2)
        {
            int affix = SnippetLimits.LargeSnippetAffixLength;
            ReadOnlySpan<char> prefix1 = code1.AsSpan(0, Math.Min(affix, code1.Length));
            ReadOnlySpan<char> prefix2 = code2.AsSpan(0, Math.Min(affix, code2.Length));
            ReadOnlySpan<char> suffix1 = code1.AsSpan(Math.Max(0, code1.Length - affix));
            ReadOnlySpan<char> suffix2 = code2.AsSpan(Math.Max(0, code2.Length - affix));

            bool prefixEqual = MemoryExtensions.Equals(prefix1, prefix2, StringComparison.Ordinal);
            bool suffixEqual = MemoryExtensions.Equals(suffix1, suffix2, StringComparison.Ordinal);
            double lengthRatio = (double)Math.Min(code1.Length, code2.Length) / Math.Max(code1.Length, code2.Length);

            if (prefixEqual && suffixEqual)
                return Math.Max(0.85, lengthRatio);

            if (prefixEqual || suffixEqual)
                return 0.5 * lengthRatio;

            return 0.0;
        }

        private static double BoundedEditSimilarity(string left, string right, bool allowEditDistance)
        {
            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
                return 0.0;

            if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
                return 1.0;

            if (!allowEditDistance)
                return 0.0;

            string a = left.ToLowerInvariant();
            string b = right.ToLowerInvariant();
            int maxLength = Math.Max(a.Length, b.Length);
            if (maxLength == 0)
                return 1.0;

            int maxDistance = (int)Math.Floor((1.0 - SnippetLimits.DuplicateSimilarityThreshold) * maxLength);
            int distance = TwoRowLevenshtein(a, b, maxDistance);
            if (distance > maxDistance)
                return 0.0;

            return Math.Max(0.0, 1.0 - ((double)distance / maxLength));
        }

        public static int TwoRowLevenshtein(string a, string b, int maxDistance)
        {
            if (a.Length > b.Length)
                (a, b) = (b, a);

            int n = a.Length;
            int m = b.Length;

            if (m - n > maxDistance)
                return maxDistance + 1;

            if (n == 0)
                return m;

            int[] prev = new int[n + 1];
            int[] curr = new int[n + 1];

            for (int i = 0; i <= n; i++)
                prev[i] = i;

            for (int j = 1; j <= m; j++)
            {
                curr[0] = j;
                int rowMin = curr[0];

                for (int i = 1; i <= n; i++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    curr[i] = Math.Min(
                        Math.Min(curr[i - 1] + 1, prev[i] + 1),
                        prev[i - 1] + cost);
                    if (curr[i] < rowMin)
                        rowMin = curr[i];
                }

                if (rowMin > maxDistance)
                    return maxDistance + 1;

                (prev, curr) = (curr, prev);
            }

            return prev[n];
        }
    }
}
