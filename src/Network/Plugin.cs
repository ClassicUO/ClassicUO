using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

using ClassicUO.Game;
using ClassicUO.IO;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

using CUO_API;

using SDL2;

namespace ClassicUO.Network
{
    internal unsafe class Plugin
    {
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnPacketSendRecv _recv, _send, _onRecv, _onSend;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnGetPacketLength _getPacketLength;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnGetPlayerPosition _getPlayerPosition;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnCastSpell _castSpell;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnGetStaticImage _getStaticImage;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnGetUOFilePath _getUoFilePath;

        [MarshalAs(UnmanagedType.FunctionPtr)] private OnHotkey _onHotkeyPressed;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnMouse _onMouse;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnUpdatePlayerPosition _onUpdatePlayerPosition;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnClientClose _onClientClose;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnInitialize _onInitialize;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnConnected _onConnected;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnDisconnected _onDisconnected;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnFocusGained _onFocusGained;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnFocusLost _onFocusLost;

        private readonly string _path;
        

        private Plugin(string path)
        {
            _path = path;
        }

        public bool IsValid { get; private set; }


        public static Plugin Create(string path)
        {
            path = Path.GetFullPath(Path.Combine(Engine.ExePath, "Data", "Plugins", path));

            if (!File.Exists(path))
            {
                Log.Message(LogTypes.Error, $"Plugin '{path}' not found.");
                return null;
            }

            Log.Message(LogTypes.Trace, $"Loading plugin: {path}");

            Plugin p = new Plugin(path);
            p.Load();

            if (!p.IsValid)
            {
                Log.Message(LogTypes.Warning, $"Invalid plugin: {path}");
                return null;
            }

            Log.Message(LogTypes.Trace, $"Plugin: {path} loaded.");
            _plugins.Add(p);
            return p;
        }

        private static readonly List<Plugin> _plugins = new List<Plugin>();


        private delegate void OnInstall(ref void* header);

        public void Load()
        {
            _recv = OnPluginRecv;
            _send = OnPluginSend;
            _getPacketLength = PacketsTable.GetPacketLength;
            _getPlayerPosition = GetPlayerPosition;
            _castSpell = GameActions.CastSpell;
            _getStaticImage = GetStaticImage;
            _getUoFilePath = GetUOFilePath;



            IntPtr assptr = SDL2EX.SDL_LoadObject(_path);

            Log.Message(LogTypes.Trace, $"assembly: {assptr}");

            if (assptr == IntPtr.Zero)
            {
                Log.Message(LogTypes.Error, "Invalid assemlby.");
                return;
            }

            Log.Message(LogTypes.Trace, $"Searching for 'Install' entry point  -  {assptr}");

            IntPtr installPtr = SDL2EX.SDL_LoadFunction(assptr, "Install");

            Log.Message(LogTypes.Trace, $"Entry point: {installPtr}");

            if (installPtr == IntPtr.Zero)
            {
                Log.Message(LogTypes.Error, "Invalid entry point.");
                return;
            }


            //IntPtr headerPtr = Marshal.AllocHGlobal(4 + 8 * 18); // 256 ?
            //Marshal.WriteInt32(headerPtr, (int)FileManager.ClientVersion);
            //Marshal.WriteIntPtr(headerPtr, Marshal.GetFunctionPointerForDelegate(_recv));
            //Marshal.WriteIntPtr(headerPtr, Marshal.GetFunctionPointerForDelegate(_send));
            //Marshal.WriteIntPtr(headerPtr, Marshal.GetFunctionPointerForDelegate(_getPacketLength));
            //Marshal.WriteIntPtr(headerPtr, Marshal.GetFunctionPointerForDelegate(_getPlayerPosition));
            //Marshal.WriteIntPtr(headerPtr, Marshal.GetFunctionPointerForDelegate(_castSpell));
            //Marshal.WriteIntPtr(headerPtr, Marshal.GetFunctionPointerForDelegate(_getStaticImage));
            //Marshal.WriteIntPtr(headerPtr, SDL.SDL_GL_GetCurrentWindow());
            //Marshal.WriteIntPtr(headerPtr, Marshal.GetFunctionPointerForDelegate(_getUoFilePath));


            PluginHeader header = new PluginHeader
            {
                ClientVersion = (int) FileManager.ClientVersion,
                Recv = Marshal.GetFunctionPointerForDelegate(_recv),
                Send = Marshal.GetFunctionPointerForDelegate(_send),
                GetPacketLength = Marshal.GetFunctionPointerForDelegate(_getPacketLength),
                GetPlayerPosition = Marshal.GetFunctionPointerForDelegate(_getPlayerPosition),
                CastSpell = Marshal.GetFunctionPointerForDelegate(_castSpell),
                GetStaticImage = Marshal.GetFunctionPointerForDelegate(_getStaticImage),
                HWND = SDL.SDL_GL_GetCurrentWindow(),
                GetUOFilePath = Marshal.GetFunctionPointerForDelegate(_getUoFilePath)
            };

            void* func = &header;          
            Marshal.GetDelegateForFunctionPointer<OnInstall>(installPtr)(ref func);

            if (header.OnRecv != IntPtr.Zero)
                _onRecv = Marshal.GetDelegateForFunctionPointer<OnPacketSendRecv>(header.OnRecv);
            if (header.OnSend != IntPtr.Zero)
                _onSend = Marshal.GetDelegateForFunctionPointer<OnPacketSendRecv>(header.OnSend);
            if (header.OnHotkeyPressed != IntPtr.Zero)
                _onHotkeyPressed = Marshal.GetDelegateForFunctionPointer<OnHotkey>(header.OnHotkeyPressed);
            if (header.OnMouse != IntPtr.Zero)
                _onMouse = Marshal.GetDelegateForFunctionPointer<OnMouse>(header.OnMouse);
            if (header.OnPlayerPositionChanged != IntPtr.Zero)
                _onUpdatePlayerPosition = Marshal.GetDelegateForFunctionPointer<OnUpdatePlayerPosition>(header.OnPlayerPositionChanged);
            if (header.OnClientClosing != IntPtr.Zero)
                _onClientClose = Marshal.GetDelegateForFunctionPointer<OnClientClose>(header.OnClientClosing);
            if (header.OnInitialize != IntPtr.Zero)
                _onInitialize = Marshal.GetDelegateForFunctionPointer<OnInitialize>(header.OnInitialize);
            if (header.OnConnected != IntPtr.Zero)
                _onConnected = Marshal.GetDelegateForFunctionPointer<OnConnected>(header.OnConnected);
            if (header.OnDisconnected != IntPtr.Zero)
                _onDisconnected = Marshal.GetDelegateForFunctionPointer<OnDisconnected>(header.OnDisconnected);
            if (header.OnFocusGained != IntPtr.Zero)
                _onFocusGained = Marshal.GetDelegateForFunctionPointer<OnFocusGained>(header.OnFocusGained);
            if (header.OnFocusLost != IntPtr.Zero)
                _onFocusLost = Marshal.GetDelegateForFunctionPointer<OnFocusLost>(header.OnFocusLost);

            IsValid = true;

            //Marshal.FreeHGlobal(headerPtr);

            _onInitialize?.Invoke();
        }

        private static string GetUOFilePath()
            => FileManager.UoFolderPath;

        private static void GetStaticImage(ushort g, ref ArtInfo info)
        {
            FileManager.Art.TryGetEntryInfo(g, out long address, out long size, out long compressedsize);
            info.Address = address;
            info.Size = size;
            info.CompressedSize = compressedsize;
        }

        private static bool GetPlayerPosition(out int x, out int y, out int z)
        {
            if (World.Player != null)
            {
                x = World.Player.X;
                y = World.Player.Y;
                z = World.Player.Z;

                return true;
            }

            x = y = z = 0;

            return false;
        }


        internal static bool ProcessRecvPacket(byte[] data, int length)
        {
            bool result = true;

            for (int i = 0; i < _plugins.Count; i++)
            {
                Plugin plugin = _plugins[i];

                if (plugin._onRecv != null && !plugin._onRecv(data, length))
                    result = false;
            }

            return result;
        }


        internal static void OnClosing()
        {
            for (int i = 0; i < _plugins.Count; i++)
            {
                _plugins[i]._onClientClose?.Invoke();

                _plugins.RemoveAt(i--);
            }
        }

        internal static void OnFocusGained()
        {
            for (int i = 0; i < _plugins.Count; i++)
            {
                _plugins[i]._onFocusGained?.Invoke();
            }
        }

        internal static void OnFocusLost()
        {
            for (int i = 0; i < _plugins.Count; i++)
            {
                _plugins[i]._onFocusLost?.Invoke();

            }
        }


        internal static void OnConnected()
        {
            for (int i = 0; i < _plugins.Count; i++)
            {
                _plugins[i]._onConnected?.Invoke();
            }
        }

        internal static void OnDisconnected()
        {
            for (int i = 0; i < _plugins.Count; i++)
            {
                _plugins[i]._onDisconnected?.Invoke();
            }
        }

        internal static bool ProcessSendPacket(byte[] data, int length)
        {
            bool result = true;

            for (int i = 0; i < _plugins.Count; i++)
            {
                Plugin plugin = _plugins[i];

                if (plugin._onSend != null && !plugin._onSend(data, length))
                    result = false;
            }

            return result;
        }

        internal static bool ProcessHotkeys(int key, int mod, bool ispressed)
        {
            bool result = true;

            for (int i = 0; i < _plugins.Count; i++)
            {
                Plugin plugin = _plugins[i];

                if (plugin._onHotkeyPressed != null && !plugin._onHotkeyPressed(key, mod, ispressed))
                    result = false;
            }

            return result;
        }

        internal static void ProcessMouse(int button, int wheel)
        {
            for (int i = 0; i < _plugins.Count; i++)
            {
                Plugin plugin = _plugins[i];
                plugin._onMouse?.Invoke(button, wheel);
            }
        }

        internal static void UpdatePlayerPosition(int x, int y, int z)
        {
            for (int i = 0; i < _plugins.Count; i++)
            {
                Plugin plugin = _plugins[i];
                plugin._onUpdatePlayerPosition?.Invoke(x, y, z);
            }
        }

        private static bool OnPluginRecv(byte[] data, int length)
        {
            Packet p = new Packet(data, length);
            NetClient.EnqueuePacketFromPlugin(p);
            return true;
        }

        private static bool OnPluginSend(byte[] data, int length)
        {
            if (NetClient.LoginSocket.IsDisposed && NetClient.Socket.IsConnected)
            {
                NetClient.Socket.Send(data);
            }
            else if (NetClient.Socket.IsDisposed && NetClient.LoginSocket.IsConnected)
            {
                NetClient.LoginSocket.Send(data);
            }

            return true;
        }
    }
}