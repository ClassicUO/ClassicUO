﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Scenes;
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
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnTick _tick;
        [MarshalAs(UnmanagedType.FunctionPtr)] private RequestMove _requestMove;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnSetTitle _setTitle;


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

        public PluginHeader header;

        public void Load()
        {
            _recv = OnPluginRecv;
            _send = OnPluginSend;
            _getPacketLength = PacketsTable.GetPacketLength;
            _getPlayerPosition = GetPlayerPosition;
            _castSpell = GameActions.CastSpell;
            _getStaticImage = GetStaticImage;
            _getUoFilePath = GetUOFilePath;
            _requestMove = RequestMove;
            _setTitle = SetWindowTitle;

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

            SDL.SDL_SysWMinfo info = new SDL.SDL_SysWMinfo();
            SDL.SDL_VERSION(out info.version);
            SDL.SDL_GetWindowWMInfo(SDL.SDL_GL_GetCurrentWindow(), ref info);

            IntPtr hwnd = IntPtr.Zero;
            if (info.subsystem == SDL.SDL_SYSWM_TYPE.SDL_SYSWM_WINDOWS)
                hwnd = info.info.win.window;

            PluginHeader header = new PluginHeader
            {
                ClientVersion = (int) FileManager.ClientVersion,
                Recv = Marshal.GetFunctionPointerForDelegate(_recv),
                Send = Marshal.GetFunctionPointerForDelegate(_send),
                GetPacketLength = Marshal.GetFunctionPointerForDelegate(_getPacketLength),
                GetPlayerPosition = Marshal.GetFunctionPointerForDelegate(_getPlayerPosition),
                CastSpell = Marshal.GetFunctionPointerForDelegate(_castSpell),
                GetStaticImage = Marshal.GetFunctionPointerForDelegate(_getStaticImage),
                HWND = hwnd,
                GetUOFilePath = Marshal.GetFunctionPointerForDelegate(_getUoFilePath),
                RequestMove = Marshal.GetFunctionPointerForDelegate(_requestMove),
                SetTitle = Marshal.GetFunctionPointerForDelegate(_setTitle)
            };

            void* func = &header;
            try
            {
                IntPtr assptr = SDL2EX.SDL_LoadObject(_path);

                Log.Message(LogTypes.Trace, $"assembly: {assptr}");

                if (assptr == IntPtr.Zero)
                {
                    throw new Exception("Invalid Assembly, Attempting managed load.");
                }

                Log.Message(LogTypes.Trace, $"Searching for 'Install' entry point  -  {assptr}");

                IntPtr installPtr = SDL2EX.SDL_LoadFunction(assptr, "Install");

                Log.Message(LogTypes.Trace, $"Entry point: {installPtr}");

                if (installPtr == IntPtr.Zero)
                {
                    throw new Exception("Invalid Entry Point, Attempting managed load.");
                }
                Marshal.GetDelegateForFunctionPointer<OnInstall>(installPtr)(ref func);
            }
            catch
            {
                try
                {
                    var asm = Assembly.LoadFile(_path);
                    var type = asm.GetType("Assistant.Engine");
                    
                    if (type == null)
                    {
                        Log.Message(LogTypes.Error,
                            $"Unable to find Plugin Type, API requires the public class Engine in namespace Assistant.");
                        return;
                    }

                    var meth = type.GetMethod("Install", BindingFlags.Public | BindingFlags.Static);
                    if (meth == null)
                    {
                        Log.Message(LogTypes.Error, $"Engine class missing public static Install method Needs 'public static unsafe void Install(PluginHeader *plugin)' ");
                        return;
                    }

                    meth.Invoke(null, new object[] {(IntPtr) func });
                }
                catch (Exception err)
                {
                    Log.Message(LogTypes.Error,
                        $"Invalid plugin specified. {err.Message} {err.StackTrace}");
                    return;
                }

            }
           

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
            if (header.Tick != IntPtr.Zero)
                _tick = Marshal.GetDelegateForFunctionPointer<OnTick>(header.Tick);
            IsValid = true;

            //Marshal.FreeHGlobal(headerPtr);

            _onInitialize?.Invoke();
        }

        private static string GetUOFilePath()
            => FileManager.UoFolderPath;

        private static void SetWindowTitle(string str)
        {
            Engine.Instance.Window.Title = str;
        }
        private static void GetStaticImage(ushort g, ref ArtInfo info)
        {
            FileManager.Art.TryGetEntryInfo(g, out long address, out long size, out long compressedsize);
            info.Address = address;
            info.Size = size;
            info.CompressedSize = compressedsize;
        }

        private static bool RequestMove(int dir, bool run)
        {
            return World.Player.Walk((Direction) dir, run);
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

        internal static void Tick()
        {
            for (int i = 0; i < _plugins.Count; i++)
            {
                _plugins[i]._tick?.Invoke();

            }

        }


        internal static bool ProcessRecvPacket(Packet p)
        {
            bool result = true;
            if (p.IsAssistPacket)
                return result;
            for (int i = 0; i < _plugins.Count; i++)
            {
                Plugin plugin = _plugins[i];

                if (plugin._onRecv != null && !plugin._onRecv(p.ToArray(), p.Length))
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

            // Activate chat after `Enter` pressing, 
            // If chat active - ignores hotkeys from Razor (Plugins)
            if (World.Player != null && 
                Engine.Profile.Current.ActivateChatAfterEnter &&
                Engine.Profile.Current.ActivateChatIgnoreHotkeysPlugins &&
                Engine.Profile.Current.ActivateChatStatus &&
                Engine.SceneManager.CurrentScene is GameScene gs)
                return true;

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
                try
                {
                    // TODO: need fixed on razor side
                    // if you quick entry (0.5-1 sec after start, without razor window loaded) - breaks CUO.
                    // With this fix - the razor does not work, but client does not crashed.
                    plugin._onUpdatePlayerPosition?.Invoke(x, y, z);
                }
                catch
                {
                    Log.Message(LogTypes.Error, $"Plugin initialization failed, please re login");
                }
            }
        }

        private static bool OnPluginRecv(byte[] data, int length)
        {
            Packet p = new Packet(data, length){IsAssistPacket = true};
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