using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Network.Plugins
{
    static unsafe partial class PluginManager
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BufferDescriptor
        {
            public IntPtr Buffer;
            public IntPtr LengthPtr;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct PlayerPosition
        {
            public int X, Y, Z;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct PluginDescriptor
        {
            public uint PluginVersion;
            public uint ClientVersion;
            public PluginFlags Features;
            public IntPtr SDLWindowHandle;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct ClientFunctions
        {
            public IntPtr SendPacketToClient;
            public IntPtr SendPacketToServer;
            public IntPtr MovePlayer;
            public IntPtr SetGameWindowTitle;
            public IntPtr GetCliloc;
            public IntPtr GetUltimaOnlinePath;
            public IntPtr GetPacketLength;
        }
    }
}
