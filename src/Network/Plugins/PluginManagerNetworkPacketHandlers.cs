using ClassicUO.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Network.Plugins
{
    static unsafe partial class PluginManager
    {
        private enum PluginNetworkPacketType
        {
        }

        public static void ParsePacket(ref StackDataReader reader)
        {
            PluginNetworkPacketType type = (PluginNetworkPacketType) reader.ReadUInt16BE();
            
            // TODO
        }
    }
}
