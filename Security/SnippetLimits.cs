namespace CopyPastaNative.Security
{
    public static class SnippetLimits
    {
        public const long MaxImportFileBytes = 10L * 1024 * 1024;
        public const long MaxDatabaseFileBytes = MaxImportFileBytes;
        public const int MaxSnippetsPerImport = 5_000;
        public const int MaxSnippetsInDatabase = MaxSnippetsPerImport;
        public const int MaxTitleLength = 200;
        public const int MaxLanguageLength = 64;
        public const int MaxTagCount = 20;
        public const int MaxTagLength = 50;
        public const int MaxCodeLength = 512 * 1024;
        public const int MaxJsonDepth = 8;

        public const double DuplicateSimilarityThreshold = 0.70;
        public const int MaxEditDistanceCodeLength = 8 * 1024;
        public const int LargeSnippetAffixLength = 256;
    }
}
