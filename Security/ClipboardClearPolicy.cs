namespace CopyPastaNative.Security
{
    public static class ClipboardClearPolicy
    {
        public static bool ShouldClear(string? originallyCopied, string? currentClipboard)
        {
            return !string.IsNullOrEmpty(originallyCopied)
                && originallyCopied == currentClipboard;
        }
    }
}
