#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Utility.Logging;
using ClassicUO.Utility.Platforms;

using CUO_API;

using SDL2;

namespace ClassicUO.Network
{
    internal unsafe class Plugin
    {
        private static readonly List<Plugin> _plugins = new List<Plugin>();
        public static List<Plugin> Plugins => _plugins;
        private readonly string _path;
        public string PluginPath => _path;

        [MarshalAs(UnmanagedType.FunctionPtr)] private OnCastSpell _castSpell;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnGetPacketLength _getPacketLength;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnGetPlayerPosition _getPlayerPosition;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnGetStaticImage _getStaticImage;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnGetUOFilePath _getUoFilePath;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnClientClose _onClientClose;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnConnected _onConnected;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnDisconnected _onDisconnected;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnFocusGained _onFocusGained;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnFocusLost _onFocusLost;

        [MarshalAs(UnmanagedType.FunctionPtr)] private OnHotkey _onHotkeyPressed;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnInitialize _onInitialize;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnMouse _onMouse;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnUpdatePlayerPosition _onUpdatePlayerPosition;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnPacketSendRecv _recv, _send, _onRecv, _onSend;
        [MarshalAs(UnmanagedType.FunctionPtr)] private RequestMove _requestMove;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnSetTitle _setTitle;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnTick _tick;



        private Plugin(string path)
        {
            _path = path;
        }

        public bool IsValid { get; private set; }


        public static Plugin Create(string path)
        {
            path = Path.GetFullPath(Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Plugins", path));

            if (!File.Exists(path))
            {
                Log.Error( $"Plugin '{path}' not found.");

                return null;
            }

            Log.Trace( $"Loading plugin: {path}");

            Plugin p = new Plugin(path);
            p.Load();

            if (!p.IsValid)
            {
                Log.Warn( $"Invalid plugin: {path}");

                return null;
            }

            Log.Trace( $"Plugin: {path} loaded.");
            _plugins.Add(p);

            return p;
        }


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

            SDL.SDL_SysWMinfo info = new SDL.SDL_SysWMinfo();
            SDL.SDL_VERSION(out info.version);
            SDL.SDL_GetWindowWMInfo(SDL.SDL_GL_GetCurrentWindow(), ref info);

            IntPtr hwnd = IntPtr.Zero;

            if (info.subsystem == SDL.SDL_SYSWM_TYPE.SDL_SYSWM_WINDOWS)
                hwnd = info.info.win.window;

            PluginHeader header = new PluginHeader
            {
                ClientVersion = (int) Client.Version,
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
                IntPtr assptr = Native.LoadLibrary(_path);

                Log.Trace( $"assembly: {assptr}");

                if (assptr == IntPtr.Zero) throw new Exception("Invalid Assembly, Attempting managed load.");

                Log.Trace( $"Searching for 'Install' entry point  -  {assptr}");

                IntPtr installPtr = Native.GetProcessAddress(assptr, "Install");

                Log.Trace( $"Entry point: {installPtr}");

                if (installPtr == IntPtr.Zero) throw new Exception("Invalid Entry Point, Attempting managed load.");

                Marshal.GetDelegateForFunctionPointer<OnInstall>(installPtr)(func);

                Console.WriteLine(">>> ADDRESS {0}", header.OnInitialize);
            }
            catch
            {
                try
                {
                    var asm = Assembly.LoadFile(_path);
                    var type = asm.GetType("Assistant.Engine");

                    if (type == null)
                    {
                        Log.Error(
                                    "Unable to find Plugin Type, API requires the public class Engine in namespace Assistant.");

                        return;
                    }

                    var meth = type.GetMethod("Install", BindingFlags.Public | BindingFlags.Static);

                    if (meth == null)
                    {
                        Log.Error( "Engine class missing public static Install method Needs 'public static unsafe void Install(PluginHeader *plugin)' ");

                        return;
                    }

                    meth.Invoke(null, new object[] {(IntPtr) func});
                }
                catch (Exception err)
                {
                    Log.Error(
                                $"Plugin threw an error during Initialization. {err.Message} {err.StackTrace} {err.InnerException?.Message} {err.InnerException?.StackTrace}");

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


            _onInitialize?.Invoke();
        }

        private static string GetUOFilePath()
        {
            return Settings.GlobalSettings.UltimaOnlineDirectory;
        }

        private static void SetWindowTitle(string str)
        {
            if (string.IsNullOrEmpty(str))
                CUOEnviroment.DisableUpdateWindowCaption = false;
            else
            {
                CUOEnviroment.DisableUpdateWindowCaption = true;
                Client.Game.Window.Title = str;
            }
        }

        private static void GetStaticImage(ushort g, ref ArtInfo info)
        {
            ArtLoader.Instance.TryGetEntryInfo(g, out long address, out long size, out long compressedsize);
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
            foreach (Plugin t in _plugins)
                t._tick?.Invoke();
        }


        internal static bool ProcessRecvPacket(ref byte[] data, ref int length)
        {
            bool result = true;

            foreach (Plugin plugin in _plugins)
            {
                if (plugin._onRecv != null && !plugin._onRecv(ref data, ref length))
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
            foreach (Plugin t in _plugins)
                t._onFocusGained?.Invoke();
        }

        internal static void OnFocusLost()
        {
            foreach (Plugin t in _plugins)
                t._onFocusLost?.Invoke();
        }


        internal static void OnConnected()
        {
            foreach (Plugin t in _plugins)
                t._onConnected?.Invoke();
        }

        internal static void OnDisconnected()
        {
            foreach (Plugin t in _plugins)
                t._onDisconnected?.Invoke();
        }

        internal static bool ProcessSendPacket(ref byte[] data, ref int length)
        {
            bool result = true;

            foreach (Plugin plugin in _plugins)
            {
                if (plugin._onSend != null && !plugin._onSend(ref data, ref length))
                    result = false;
            }

            return result;
        }

        internal static bool ProcessHotkeys(int key, int mod, bool ispressed)
        {
            bool result = true;


            if (!World.InGame || 
                (ProfileManager.Current != null && 
                ProfileManager.Current.ActivateChatAfterEnter && 
                UIManager.SystemChat?.IsActive == true) ||
                UIManager.KeyboardFocusControl != UIManager.SystemChat.TextBoxControl)
            {
                return result;
            }

            foreach (Plugin plugin in _plugins)
            {
                if (plugin._onHotkeyPressed != null && !plugin._onHotkeyPressed(key, mod, ispressed))
                    result = false;
            }

            return result;
        }

        internal static void ProcessMouse(int button, int wheel)
        {
            foreach (Plugin plugin in _plugins) plugin._onMouse?.Invoke(button, wheel);
        }

        internal static void UpdatePlayerPosition(int x, int y, int z)
        {
            foreach (Plugin plugin in _plugins)
            {
                try
                {
                    // TODO: need fixed on razor side
                    // if you quick entry (0.5-1 sec after start, without razor window loaded) - breaks CUO.
                    // With this fix - the razor does not work, but client does not crashed.
                    plugin._onUpdatePlayerPosition?.Invoke(x, y, z);
                }
                catch
                {
                    Log.Error( "Plugin initialization failed, please re login");
                }
            }
        }

        private static bool OnPluginRecv(ref byte[] data, ref int length)
        {
            NetClient.EnqueuePacketFromPlugin(data, length);

            return true;
        }

        private static bool OnPluginSend(ref byte[] data, ref int length)
        {
            if (NetClient.LoginSocket.IsDisposed && NetClient.Socket.IsConnected)
                NetClient.Socket.Send(data, true);
            else if (NetClient.Socket.IsDisposed && NetClient.LoginSocket.IsConnected)
                NetClient.LoginSocket.Send(data, true);

            return true;
        }

        private delegate void OnInstall(void* header);
    }
}