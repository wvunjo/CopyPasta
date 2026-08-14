using CopyPastaNative.Security;

namespace CopyPastaNative.Tests
{
    public class ClipboardClearPolicyTests
    {
        [Fact]
        public void Clears_OnlyExactMatch()
        {
            Assert.True(ClipboardClearPolicy.ShouldClear("abc", "abc"));
            Assert.False(ClipboardClearPolicy.ShouldClear("abc", "xyz"));
            Assert.False(ClipboardClearPolicy.ShouldClear("abc", null));
            Assert.False(ClipboardClearPolicy.ShouldClear(null, "abc"));
            Assert.False(ClipboardClearPolicy.ShouldClear("", ""));
        }
    }
}
