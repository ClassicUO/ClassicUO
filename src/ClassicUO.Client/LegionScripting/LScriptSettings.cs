using System.Collections.Generic;

namespace ClassicUO.LegionScripting
{
    internal class LScriptSettings
    {
        public List<string> GlobalAutoStartScripts { get; set; } = new List<string>();
        public Dictionary<string, List<string>> CharAutoStartScripts { get; set; } = new Dictionary<string, List<string>>();
        public Dictionary<string, bool> GroupCollapsed { get; set; } = new Dictionary<string, bool>();
    }
}
