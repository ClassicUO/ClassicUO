using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Scripting.Hosting;

namespace ClassicUO.LegionScripting
{
    internal static class Python
    {
        private static ScriptEngine _engine;
        private static readonly object _lock = new object();

        public static ScriptEngine CreateEngine()
        {
            if (_engine != null) return _engine;
            lock (_lock)
            {
                if (_engine != null) return _engine;
                _engine = IronPython.Hosting.Python.CreateEngine();
                return _engine;
            }
        }
    }
}
