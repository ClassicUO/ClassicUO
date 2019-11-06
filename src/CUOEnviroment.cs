using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClassicUO
{
    static class CUOEnviroment
    {
        public static Thread GameThread;
        public static PlatformID Platform;
        public static uint RefreshRate = 60;
        public static float DPIScaleFactor = 1.0f;
        public static bool NoSound;
        public static string Args = string.Empty;
        public static string[] Plugins;
        public static bool Debug;
        public static string Version;
    }
}
