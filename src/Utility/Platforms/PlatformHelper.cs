using System;

namespace ClassicUO.Utility.Platforms
{
    internal static class PlatformHelper
    {
        public static readonly bool IsMonoRuntime = Type.GetType("Mono.Runtime") != null;
    }
}