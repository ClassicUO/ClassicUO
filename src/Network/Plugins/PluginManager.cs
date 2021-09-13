using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ClassicUO.Renderer.Batching;
using ClassicUO.Utility.Platforms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using static SDL2.SDL;
using static ClassicUO.Utility.Logging.Log;
using ClassicUO.Configuration;

namespace ClassicUO.Network.Plugins
{
    static unsafe partial class PluginManager
    {
        private enum PluginEventType
        {
            Initialize,
            Close,
            Connect,
            Disconnect,
            Tick,
            Draw,
            PacketSend,
            PacketRecv,
            SendPlayerPosition,
        }


#if NETFRAMEWORK
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void dOnInstall(void* header);
#endif



        private static readonly List<Plugin> _plugins = new List<Plugin>();
        private static readonly Dictionary<IntPtr, GraphicsResource> _resources = new Dictionary<IntPtr, GraphicsResource>();



        public static void LoadPlugins(string[] plugins)
        {
            foreach (string pluginPath in plugins)
            {
                string path = Path.GetFullPath(Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Plugins", pluginPath));

                Trace("loading plugin: " + path);

                if (!File.Exists(path))
                {
                    Error("plugin does not exist --> " + path);

                    continue;
                }

                IntPtr lib = Native.LoadLibrary(path);

                if (lib == IntPtr.Zero)
                {
                    Error("invalid plugin!");

                    continue;
                }

                Trace("library loaded");

                Trace("calling 'Install' function");
                IntPtr installPtr = Native.GetProcessAddress(lib, "Install");

                if (installPtr == IntPtr.Zero)
                {
                    Error("invalid 'Install' plugin function entry");

                    continue;
                }


                PluginDescriptor pluginDescriptor = new PluginDescriptor();
                pluginDescriptor.PluginVersion = 1;
                pluginDescriptor.ClientVersion = (uint) Client.Version;
                pluginDescriptor.Features = PluginFlags.All;
                pluginDescriptor.SDLWindowHandle = Client.Game.Window.Handle;
            
                
                try
                {
#if NETFRAMEWORK
                    dOnInstall install = Marshal.GetDelegateForFunctionPointer<dOnInstall>(installPtr);
                    install(&pluginDescriptor);
#else
                    ((delegate* unmanaged[Cdecl]<void*, void>)installPtr)(&pluginDescriptor);
#endif

                    Plugin plugin = new Plugin(Path.GetFileNameWithoutExtension(path), path, pluginDescriptor.Features);

                    _plugins.Add(plugin);
                }
                catch (Exception ex)
                {
                    Error($"error while loading plugin: '{path}'\n{ex}");
                }
            }

            if (_plugins.Count != 0)
            {
                Trace("start plugins initialization");
                Initialize();
                Trace("plugins initialization done");
            }
        }
 
        private static int SendCommand(PluginEventType id, IntPtr data1, out bool result)
        {
            SDL_Event ev = new SDL_Event();
            ev.type = SDL_EventType.SDL_USEREVENT;

            result = false;
            ref SDL_UserEvent uev = ref ev.user;
            uev.type = (uint)ev.type;
            uev.code = (int)id;
            uev.data1 = data1;
            uev.data2 = (IntPtr)Unsafe.AsPointer(ref result);

            return SDL_PushEvent(ref ev);
        }
    }
}
