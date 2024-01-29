using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ClassicUO.Utility.Logging;
using ClassicUO.Utility.Platforms;

namespace ClassicUO.Network
{
    enum PluginCommand
    {
        Setup,

        SniffPacketToClient,
        SniffPacketToServer,

        SendPacketToClient,
        SendPacketToServer,

        Hotkey,
    }



    sealed unsafe class Plugin2
    {
        private delegate* <PluginCommand, byte*, int, int> _pluginFn;

        public void Initialize(FileInfo pluginLib)
        {
            if (!pluginLib.Exists)
                return;

            var ptr = Native.LoadLibrary(pluginLib.FullName);
            if (ptr == IntPtr.Zero)
                return;

            var installFn = (delegate* unmanaged[Cdecl]<void*, void>) Native.GetProcessAddress(ptr, "Install");
            if (installFn == null)
                return;

            try
            {
                var init = new PluginInitialization()
                {
                    HandlerClient = &OnPluginStuff,
                    HandlerPlugin = null, // this must be set plugin side!
                };

                installFn(&init);

                if (init.HandlerPlugin == null)
                {
                    // error ?
                }

                _pluginFn = init.HandlerPlugin;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }


        static int OnPluginStuff(PluginCommand cmd, byte* data, int size)
        {
            Console.WriteLine("Plugin sent this cmd: {0}", cmd);

            return 0;
        }

        public bool SendHotkeyCommand(int key, int mod)
        {
            if (_pluginFn == null)
                return true;

            var cmd = new HotkeyCommand()
            {
                Key = key,
                Mod = mod
            };

            var res = _pluginFn(PluginCommand.Hotkey, (byte*)Unsafe.AsPointer(ref cmd), sizeof(HotkeyCommand));

            return res == 0;
        }

        public void SendClientInfo(UltimaOnline uo)
        {
            if (_pluginFn == null)
                return;

            var path = Encoding.UTF8.GetBytes(uo.ClientPath);
            fixed (byte* ptr = path)
            {
                var cmd = new SetupCommand()
                {
                    UOPath = ptr,
                    UOPathLen = path.Length,
                    ClientVersion = (uint)uo.Version
                };

                var res = _pluginFn(PluginCommand.Setup, (byte*)Unsafe.AsPointer(ref cmd), sizeof(SetupCommand));
            }
        }


        [StructLayout(LayoutKind.Sequential)]
        struct SetupCommand
        {
            public byte* UOPath;
            public int UOPathLen;
            public uint ClientVersion;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct HotkeyCommand
        {
            public int Key;
            public int Mod;
        }

        unsafe struct PluginInitialization
        {
            // cmd | cmd_data | cmd_data_len | return 0 != 0 -> error
            public delegate* <PluginCommand, byte*, int, int> HandlerClient, HandlerPlugin;
        }
    }
}