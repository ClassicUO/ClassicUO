using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Utility.Platforms
{
    static class PlatformHelper
    {
        public static readonly bool IsMonoRuntime = Type.GetType("Mono.Runtime") != null;
    }
}
