using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClassicUO
{
    static class CUOEnviroment
    {
        public static Thread GameThread;
        public static float DPIScaleFactor = 1.0f;
        public static bool NoSound;
        public static string[] Args;
        public static string[] Plugins;
        public static bool Debug;
        public static bool IsHighDPI;
        public static uint CurrentRefreshRate;

        public static readonly bool IsUnix = Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX;
        public static readonly Version Version = Assembly.GetExecutingAssembly().GetName().Version;
        public static readonly string ExecutablePath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);

        public static GameController Client;
    }
}
