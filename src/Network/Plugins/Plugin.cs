using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Network.Plugins
{
    class Plugin
    {
        public Plugin(string name, string path, PluginFlags flags)
        {
            Name = name;
            Path = path;
            Flags = flags;
        }

        public string Name { get; }
        public string Path { get; }
        public PluginFlags Flags { get; }

        public bool CanSendPackets => (Flags & PluginFlags.CanSendPackets) != 0;
        public bool CanRecvPackets => (Flags & PluginFlags.CanRecvPackets) != 0;
        public bool CanDraw => (Flags & PluginFlags.CanDraw) != 0;
    }
}
