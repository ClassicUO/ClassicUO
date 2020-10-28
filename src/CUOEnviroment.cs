using System;
using System.IO;
using System.Reflection;
using System.Threading;

namespace ClassicUO
{
    internal static class CUOEnviroment
    {
        public static Thread GameThread;
        public static float DPIScaleFactor = 1.0f;
        public static bool NoSound;
        public static string[] Args;
        public static string[] Plugins;
        public static bool Debug;
        public static bool Profiler;
        public static bool IsHighDPI;
        public static uint CurrentRefreshRate;
        public static bool SkipLoginScreen;
        public static bool IsOutlands;

        public static readonly bool IsUnix = Environment.OSVersion.Platform != PlatformID.Win32NT &&
                                             Environment.OSVersion.Platform != PlatformID.Win32Windows &&
                                             Environment.OSVersion.Platform != PlatformID.Win32S &&
                                             Environment.OSVersion.Platform != PlatformID.WinCE;

        public static readonly Version Version = Assembly.GetExecutingAssembly().GetName().Version;
        public static readonly string ExecutablePath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
    }
}