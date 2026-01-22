using MetaMap;
using Xunit;

namespace MetaMAP.Tests
{
    public class PlatformUtilsTests
    {
        [Fact]
        public void PlatformChecks_ReportWindowsByDefault()
        {
            ResetHooks();
            Assert.True(PlatformUtils.IsWindows());
            Assert.False(PlatformUtils.IsMacOS());
            Assert.False(PlatformUtils.IsLinux());
            Assert.Equal("Windows", PlatformUtils.GetCurrentPlatform());
        }

        [Fact]
        public void PlatformChecks_ReportMacOSWhenOverridden()
        {
            TestHooks.IsOSPlatformOverride = platform => platform == System.Runtime.InteropServices.OSPlatform.OSX;
            Assert.True(PlatformUtils.IsMacOS());
            Assert.False(PlatformUtils.IsWindows());
            Assert.False(PlatformUtils.IsLinux());
            Assert.Equal("macOS", PlatformUtils.GetCurrentPlatform());
            ResetHooks();
        }

        [Fact]
        public void PlatformChecks_ReportLinuxWhenOverridden()
        {
            TestHooks.IsOSPlatformOverride = platform => platform == System.Runtime.InteropServices.OSPlatform.Linux;
            Assert.True(PlatformUtils.IsLinux());
            Assert.False(PlatformUtils.IsWindows());
            Assert.False(PlatformUtils.IsMacOS());
            Assert.Equal("Linux", PlatformUtils.GetCurrentPlatform());
            ResetHooks();
        }

        [Fact]
        public void PlatformChecks_ReportUnknownWhenNoMatch()
        {
            TestHooks.IsOSPlatformOverride = _ => false;
            Assert.Equal("Unknown", PlatformUtils.GetCurrentPlatform());
            ResetHooks();
        }

        private static void ResetHooks()
        {
            TestHooks.IsOSPlatformOverride = null;
        }
    }
}
