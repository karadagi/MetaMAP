using System;
using System.IO;
using System.Net.Http;
using MetaMap;
using Xunit;

namespace MetaMAP.Tests
{
    public class TestHooksTests
    {
        [Fact]
        public void CreateHttpClient_UsesOverride()
        {
            var expected = new HttpClient();
            try
            {
                TestHooks.HttpClientFactoryOverride = () => expected;
                var client = TestHooks.CreateHttpClient();
                Assert.Same(expected, client);
            }
            finally
            {
                expected.Dispose();
                ResetHooks();
            }
        }

        [Fact]
        public void CreateHttpClient_DefaultCreatesClient()
        {
            try
            {
                TestHooks.HttpClientFactoryOverride = null;
                using var client = TestHooks.CreateHttpClient();
                Assert.NotNull(client);
            }
            finally
            {
                ResetHooks();
            }
        }

        [Fact]
        public void Sleep_UsesOverride()
        {
            var called = false;
            try
            {
                TestHooks.SleepOverride = _ => called = true;
                TestHooks.Sleep(1);
                Assert.True(called);
            }
            finally
            {
                ResetHooks();
            }
        }

        [Fact]
        public void Sleep_DefaultUsesThreadSleep()
        {
            try
            {
                TestHooks.SleepOverride = null;
                TestHooks.Sleep(0);
            }
            finally
            {
                ResetHooks();
            }
        }

        [Fact]
        public void GetTempFileName_UsesOverride()
        {
            try
            {
                TestHooks.TempFileNameOverride = () => "temp_override.tmp";
                var name = TestHooks.GetTempFileName();
                Assert.Equal("temp_override.tmp", name);
            }
            finally
            {
                ResetHooks();
            }
        }

        [Fact]
        public void GetTempFileName_DefaultCreatesFile()
        {
            try
            {
                TestHooks.TempFileNameOverride = null;
                var name = TestHooks.GetTempFileName();
                Assert.True(File.Exists(name));
                File.Delete(name);
            }
            finally
            {
                ResetHooks();
            }
        }

        [Fact]
        public void GetAssemblyLocation_UsesOverride()
        {
            try
            {
                TestHooks.AssemblyLocationOverride = () => "fake_location.dll";
                var location = TestHooks.GetAssemblyLocation();
                Assert.Equal("fake_location.dll", location);
            }
            finally
            {
                ResetHooks();
            }
        }

        [Fact]
        public void GetAssemblyLocation_DefaultReturnsValue()
        {
            try
            {
                TestHooks.AssemblyLocationOverride = null;
                var location = TestHooks.GetAssemblyLocation();
                Assert.False(string.IsNullOrWhiteSpace(location));
            }
            finally
            {
                ResetHooks();
            }
        }

        private static void ResetHooks()
        {
            TestHooks.HttpClientFactoryOverride = null;
            TestHooks.SleepOverride = null;
            TestHooks.InvokeOnUiThreadOverride = null;
            TestHooks.TempFileNameOverride = null;
            TestHooks.AssemblyLocationOverride = null;
            TestHooks.IsOSPlatformOverride = null;
            TestHooks.ForceExtrusionFailure = false;
            TestHooks.ForceDelaunayFailure = false;
        }
    }
}
