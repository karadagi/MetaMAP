using Rhino;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace MetaMap
{
    internal static class TestHooks
    {
        internal static Func<HttpClient> HttpClientFactoryOverride;
        internal static Action<int> SleepOverride;
        internal static Action<Action> InvokeOnUiThreadOverride;
        internal static Func<string> TempFileNameOverride;
        internal static Func<string> AssemblyLocationOverride;
        internal static Func<OSPlatform, bool> IsOSPlatformOverride;
        internal static bool ForceExtrusionFailure;
        internal static bool ForceDelaunayFailure;

        internal static HttpClient CreateHttpClient()
        {
            return HttpClientFactoryOverride != null ? HttpClientFactoryOverride() : new HttpClient();
        }

        internal static void Sleep(int milliseconds)
        {
            if (SleepOverride != null)
            {
                SleepOverride(milliseconds);
                return;
            }

            Thread.Sleep(milliseconds);
        }

        [ExcludeFromCodeCoverage]
        internal static void InvokeOnUiThread(Action action)
        {
            if (InvokeOnUiThreadOverride != null)
            {
                InvokeOnUiThreadOverride(action);
                return;
            }

            RhinoApp.InvokeOnUiThread(action);
        }

        internal static string GetTempFileName()
        {
            return TempFileNameOverride != null ? TempFileNameOverride() : Path.GetTempFileName();
        }

        internal static string GetAssemblyLocation()
        {
            return AssemblyLocationOverride != null ? AssemblyLocationOverride() : Assembly.GetExecutingAssembly().Location;
        }
    }
}
