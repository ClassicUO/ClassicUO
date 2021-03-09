﻿#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

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
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Renderer.Batching;
using ClassicUO.Utility.Logging;
using ClassicUO.Utility.Platforms;
using CUO_API;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL2;

namespace ClassicUO.Network
{
    internal unsafe class Plugin
    {
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnCastSpell _castSpell;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnDrawCmdList _draw_cmd_list;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnGetCliloc _get_cliloc;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnGetStaticData _get_static_data;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnGetTileData _get_tile_data;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnGetPacketLength _getPacketLength;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnGetPlayerPosition _getPlayerPosition;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnGetStaticImage _getStaticImage;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnGetUOFilePath _getUoFilePath;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnWndProc _on_wnd_proc;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnClientClose _onClientClose;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnConnected _onConnected;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnDisconnected _onDisconnected;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnFocusGained _onFocusGained;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnFocusLost _onFocusLost;

        [MarshalAs(UnmanagedType.FunctionPtr)] private OnHotkey _onHotkeyPressed;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnInitialize _onInitialize;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnMouse _onMouse;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnPacketSendRecv_new _onRecv_new, _onSend_new;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnUpdatePlayerPosition _onUpdatePlayerPosition;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnPacketSendRecv _recv, _send, _onRecv, _onSend;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnPacketSendRecv_new_intptr _recv_new, _send_new;
        [MarshalAs(UnmanagedType.FunctionPtr)] private RequestMove _requestMove;
        private readonly Dictionary<IntPtr, GraphicsResource> _resources = new Dictionary<IntPtr, GraphicsResource>();
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnSetTitle _setTitle;
        [MarshalAs(UnmanagedType.FunctionPtr)] private OnTick _tick;

        private Plugin(string path)
        {
            PluginPath = path;
        }

        public static List<Plugin> Plugins { get; } = new List<Plugin>();

        public string PluginPath { get; }

        public bool IsValid { get; private set; }


        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteFile(string name);


        public static Plugin Create(string path)
        {
            path = Path.GetFullPath(Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Plugins", path));

            if (!File.Exists(path))
            {
                Log.Error($"Plugin '{path}' not found.");

                return null;
            }

            Log.Trace($"Loading plugin: {path}");

            Plugin p = new Plugin(path);
            p.Load();

            if (!p.IsValid)
            {
                Log.Warn($"Invalid plugin: {path}");

                return null;
            }

            Log.Trace($"Plugin: {path} loaded.");
            Plugins.Add(p);

            return p;
        }


        public void Load()
        {
            _recv = OnPluginRecv;
            _send = OnPluginSend;
            _recv_new = OnPluginRecv_new;
            _send_new = OnPluginSend_new;
            _getPacketLength = PacketsTable.GetPacketLength;
            _getPlayerPosition = GetPlayerPosition;
            _castSpell = GameActions.CastSpell;
            _getStaticImage = GetStaticImage;
            _getUoFilePath = GetUOFilePath;
            _requestMove = RequestMove;
            _setTitle = SetWindowTitle;
            _get_static_data = GetStaticData;
            _get_tile_data = GetTileData;
            _get_cliloc = GetCliloc;

            SDL.SDL_SysWMinfo info = new SDL.SDL_SysWMinfo();
            SDL.SDL_VERSION(out info.version);
            SDL.SDL_GetWindowWMInfo(Client.Game.Window.Handle, ref info);

            IntPtr hwnd = IntPtr.Zero;

            if (info.subsystem == SDL.SDL_SYSWM_TYPE.SDL_SYSWM_WINDOWS)
            {
                hwnd = info.info.win.window;
            }

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
                SetTitle = Marshal.GetFunctionPointerForDelegate(_setTitle),
                Recv_new = Marshal.GetFunctionPointerForDelegate(_recv_new),
                Send_new = Marshal.GetFunctionPointerForDelegate(_send_new),

                SDL_Window = Client.Game.Window.Handle,
                GetStaticData = Marshal.GetFunctionPointerForDelegate(_get_static_data),
                GetTileData = Marshal.GetFunctionPointerForDelegate(_get_tile_data),
                GetCliloc = Marshal.GetFunctionPointerForDelegate(_get_cliloc)
            };

            void* func = &header;

            if (Environment.OSVersion.Platform != PlatformID.Unix && Environment.OSVersion.Platform != PlatformID.MacOSX)
            {
                UnblockPath(Path.GetDirectoryName(PluginPath));
            }

            try
            {
                IntPtr assptr = Native.LoadLibrary(PluginPath);

                Log.Trace($"assembly: {assptr}");

                if (assptr == IntPtr.Zero)
                {
                    throw new Exception("Invalid Assembly, Attempting managed load.");
                }

                Log.Trace($"Searching for 'Install' entry point  -  {assptr}");

                IntPtr installPtr = Native.GetProcessAddress(assptr, "Install");

                Log.Trace($"Entry point: {installPtr}");

                if (installPtr == IntPtr.Zero)
                {
                    throw new Exception("Invalid Entry Point, Attempting managed load.");
                }

                Marshal.GetDelegateForFunctionPointer<OnInstall>(installPtr)(func);

                Console.WriteLine(">>> ADDRESS {0}", header.OnInitialize);
            }
            catch
            {
                try
                {
                    Assembly asm = Assembly.LoadFile(PluginPath);
                    Type type = asm.GetType("Assistant.Engine");

                    if (type == null)
                    {
                        Log.Error("Unable to find Plugin Type, API requires the public class Engine in namespace Assistant.");

                        return;
                    }

                    MethodInfo meth = type.GetMethod("Install", BindingFlags.Public | BindingFlags.Static);

                    if (meth == null)
                    {
                        Log.Error("Engine class missing public static Install method Needs 'public static unsafe void Install(PluginHeader *plugin)' ");

                        return;
                    }

                    meth.Invoke(null, new object[] { (IntPtr) func });
                }
                catch (Exception err)
                {
                    Log.Error($"Plugin threw an error during Initialization. {err.Message} {err.StackTrace} {err.InnerException?.Message} {err.InnerException?.StackTrace}");

                    return;
                }
            }


            if (header.OnRecv != IntPtr.Zero)
            {
                _onRecv = Marshal.GetDelegateForFunctionPointer<OnPacketSendRecv>(header.OnRecv);
            }

            if (header.OnSend != IntPtr.Zero)
            {
                _onSend = Marshal.GetDelegateForFunctionPointer<OnPacketSendRecv>(header.OnSend);
            }

            if (header.OnHotkeyPressed != IntPtr.Zero)
            {
                _onHotkeyPressed = Marshal.GetDelegateForFunctionPointer<OnHotkey>(header.OnHotkeyPressed);
            }

            if (header.OnMouse != IntPtr.Zero)
            {
                _onMouse = Marshal.GetDelegateForFunctionPointer<OnMouse>(header.OnMouse);
            }

            if (header.OnPlayerPositionChanged != IntPtr.Zero)
            {
                _onUpdatePlayerPosition = Marshal.GetDelegateForFunctionPointer<OnUpdatePlayerPosition>(header.OnPlayerPositionChanged);
            }

            if (header.OnClientClosing != IntPtr.Zero)
            {
                _onClientClose = Marshal.GetDelegateForFunctionPointer<OnClientClose>(header.OnClientClosing);
            }

            if (header.OnInitialize != IntPtr.Zero)
            {
                _onInitialize = Marshal.GetDelegateForFunctionPointer<OnInitialize>(header.OnInitialize);
            }

            if (header.OnConnected != IntPtr.Zero)
            {
                _onConnected = Marshal.GetDelegateForFunctionPointer<OnConnected>(header.OnConnected);
            }

            if (header.OnDisconnected != IntPtr.Zero)
            {
                _onDisconnected = Marshal.GetDelegateForFunctionPointer<OnDisconnected>(header.OnDisconnected);
            }

            if (header.OnFocusGained != IntPtr.Zero)
            {
                _onFocusGained = Marshal.GetDelegateForFunctionPointer<OnFocusGained>(header.OnFocusGained);
            }

            if (header.OnFocusLost != IntPtr.Zero)
            {
                _onFocusLost = Marshal.GetDelegateForFunctionPointer<OnFocusLost>(header.OnFocusLost);
            }

            if (header.Tick != IntPtr.Zero)
            {
                _tick = Marshal.GetDelegateForFunctionPointer<OnTick>(header.Tick);
            }


            if (header.OnRecv_new != IntPtr.Zero)
            {
                _onRecv_new = Marshal.GetDelegateForFunctionPointer<OnPacketSendRecv_new>(header.OnRecv_new);
            }

            if (header.OnSend_new != IntPtr.Zero)
            {
                _onSend_new = Marshal.GetDelegateForFunctionPointer<OnPacketSendRecv_new>(header.OnSend_new);
            }

            if (header.OnDrawCmdList != IntPtr.Zero)
            {
                _draw_cmd_list = Marshal.GetDelegateForFunctionPointer<OnDrawCmdList>(header.OnDrawCmdList);
            }

            if (header.OnWndProc != IntPtr.Zero)
            {
                _on_wnd_proc = Marshal.GetDelegateForFunctionPointer<OnWndProc>(header.OnWndProc);
            }


            IsValid = true;

            _onInitialize?.Invoke();
        }

        private static string GetUOFilePath()
        {
            return Settings.GlobalSettings.UltimaOnlineDirectory;
        }

        private static void SetWindowTitle(string str)
        {
            Client.Game.SetWindowTitle(str);
        }

        private static bool GetStaticData
        (
            int index,
            ref ulong flags,
            ref byte weight,
            ref byte layer,
            ref int count,
            ref ushort animid,
            ref ushort lightidx,
            ref byte height,
            ref string name
        )
        {
            if (index >= 0 && index < Constants.MAX_STATIC_DATA_INDEX_COUNT)
            {
                ref StaticTiles st = ref TileDataLoader.Instance.StaticData[index];

                flags = (ulong) st.Flags;
                weight = st.Weight;
                layer = st.Layer;
                count = st.Count;
                animid = st.AnimID;
                lightidx = st.LightIndex;
                height = st.Height;
                name = st.Name;

                return true;
            }

            return false;
        }

        private static bool GetTileData(int index, ref ulong flags, ref ushort textid, ref string name)
        {
            if (index >= 0 && index < Constants.MAX_STATIC_DATA_INDEX_COUNT)
            {
                ref LandTiles st = ref TileDataLoader.Instance.LandData[index];

                flags = (ulong) st.Flags;
                textid = st.TexID;
                name = st.Name;

                return true;
            }

            return false;
        }

        private static bool GetCliloc(int cliloc, string args, bool capitalize, out string buffer)
        {
            buffer = ClilocLoader.Instance.Translate(cliloc, args, capitalize);

            return buffer != null;
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
            foreach (Plugin t in Plugins)
            {
                t._tick?.Invoke();
            }
        }


        internal static bool ProcessRecvPacket(ref byte[] data, ref int length)
        {
            bool result = true;

            foreach (Plugin plugin in Plugins)
            {
                if (plugin._onRecv_new != null)
                {
                    if (!plugin._onRecv_new(data, ref length))
                    {
                        result = false;
                    }
                }
                else if (plugin._onRecv != null && !plugin._onRecv(ref data, ref length))
                {
                    result = false;
                }
            }

            return result;
        }

        internal static bool ProcessSendPacket(ref byte[] data, ref int length)
        {
            bool result = true;

            foreach (Plugin plugin in Plugins)
            {
                if (plugin._onSend_new != null)
                {
                    if (!plugin._onSend_new(data, ref length))
                    {
                        result = false;
                    }
                }
                else if (plugin._onSend != null && !plugin._onSend(ref data, ref length))
                {
                    result = false;
                }
            }

            return result;
        }

        internal static void OnClosing()
        {
            for (int i = 0; i < Plugins.Count; i++)
            {
                Plugins[i]._onClientClose?.Invoke();

                Plugins.RemoveAt(i--);
            }
        }

        internal static void OnFocusGained()
        {
            foreach (Plugin t in Plugins)
            {
                t._onFocusGained?.Invoke();
            }
        }

        internal static void OnFocusLost()
        {
            foreach (Plugin t in Plugins)
            {
                t._onFocusLost?.Invoke();
            }
        }


        internal static void OnConnected()
        {
            foreach (Plugin t in Plugins)
            {
                t._onConnected?.Invoke();
            }
        }

        internal static void OnDisconnected()
        {
            foreach (Plugin t in Plugins)
            {
                t._onDisconnected?.Invoke();
            }
        }

        internal static bool ProcessHotkeys(int key, int mod, bool ispressed)
        {
            if (!World.InGame || UIManager.SystemChat != null && (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.ActivateChatAfterEnter && UIManager.SystemChat.IsActive || UIManager.KeyboardFocusControl != UIManager.SystemChat.TextBoxControl))
            {
                return true;
            }

            bool result = true;

            foreach (Plugin plugin in Plugins)
            {
                if (plugin._onHotkeyPressed != null && !plugin._onHotkeyPressed(key, mod, ispressed))
                {
                    result = false;
                }
            }

            return result;
        }

        internal static void ProcessMouse(int button, int wheel)
        {
            foreach (Plugin plugin in Plugins)
            {
                plugin._onMouse?.Invoke(button, wheel);
            }
        }

        internal static void ProcessDrawCmdList(GraphicsDevice device)
        {
            foreach (Plugin plugin in Plugins)
            {
                if (plugin._draw_cmd_list != null)
                {
                    int cmd_count = 0;
                    plugin._draw_cmd_list.Invoke(out IntPtr cmdlist, ref cmd_count);

                    if (cmd_count != 0 && cmdlist != IntPtr.Zero)
                    {
                        plugin.HandleCmdList(device, cmdlist, cmd_count, plugin._resources);
                    }
                }
            }
        }

        internal static int ProcessWndProc(SDL.SDL_Event* e)
        {
            int result = 0;

            foreach (Plugin plugin in Plugins)
            {
                result |= plugin._on_wnd_proc?.Invoke(e) ?? 0;
            }

            return result;
        }

        internal static void UpdatePlayerPosition(int x, int y, int z)
        {
            foreach (Plugin plugin in Plugins)
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
                    Log.Error("Plugin initialization failed, please re login");
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
            //if (data != null && data.Length != 0)
            //{
            //    // horrible workaround to avoid ghosting item when a plugin sends drag request item
            //    switch (data[0])
            //    {
            //        case 0x07:
            //            ItemHold.Clear();
            //            break;
            //    }
            //}


            if (NetClient.LoginSocket.IsDisposed && NetClient.Socket.IsConnected)
            {
                NetClient.Socket.Send(data, length, true);
            }
            else if (NetClient.Socket.IsDisposed && NetClient.LoginSocket.IsConnected)
            {
                NetClient.LoginSocket.Send(data, length, true);
            }

            return true;
        }

        private static bool OnPluginRecv_new(IntPtr buffer, ref int length)
        {
            byte[] data = new byte[length];
            Marshal.Copy(buffer, data, 0, length);

            return OnPluginRecv(ref data, ref length);
        }

        private static bool OnPluginSend_new(IntPtr buffer, ref int length)
        {
            byte[] data = new byte[length];
            Marshal.Copy(buffer, data, 0, length);

            return OnPluginSend(ref data, ref length);
        }


        //Code from https://stackoverflow.com/questions/6374673/unblock-file-from-within-net-4-c-sharp
        private static void UnblockPath(string path)
        {
            string[] files = Directory.GetFiles(path);
            string[] dirs = Directory.GetDirectories(path);

            foreach (string file in files)
            {
                if (file.EndsWith("dll") || file.EndsWith("exe"))
                {
                    UnblockFile(file);
                }
            }

            foreach (string dir in dirs)
            {
                UnblockPath(dir);
            }
        }

        private static bool UnblockFile(string fileName)
        {
            return DeleteFile(fileName + ":Zone.Identifier");
        }

        private void HandleCmdList(GraphicsDevice device, IntPtr ptr, int length, IDictionary<IntPtr, GraphicsResource> resources)
        {
            if (ptr == IntPtr.Zero || length <= 0)
            {
                return;
            }

            const int CMD_VIEWPORT = 0;
            const int CMD_SCISSOR = 1;
            const int CMD_BLEND_STATE = 2;
            const int CMD_RASTERIZE_STATE = 3;
            const int CMD_STENCIL_STATE = 4;
            const int CMD_SAMPLER_STATE = 5;
            const int CMD_SET_VERTEX_BUFFER = 6;
            const int CMD_SET_INDEX_BUFFER = 7;
            const int CMD_CREATE_VERTEX_BUFFER = 8;
            const int CMD_CREATE_INDEX_BUFFER = 9;
            const int CMD_CREATE_EFFECT = 10;
            const int CMD_CREATE_TEXTURE_2D = 11;
            const int CMD_SET_TEXTURE_DATA_2D = 12;
            const int CMD_INDEXED_PRIMITIVE_DATA = 13;
            const int CMD_CREATE_BASIC_EFFECT = 14;
            const int CMD_SET_VERTEX_DATA = 15;
            const int CMD_SET_INDEX_DATA = 16;
            const int CMD_DESTROY_RESOURCE = 17;
            const int CMD_BLEND_FACTOR = 18;
            const int CMD_NEW_BLEND_STATE = 19;
            const int CMD_NEW_RASTERIZE_STATE = 20;
            const int CMD_NEW_STENCIL_STATE = 21;
            const int CMD_NEW_SAMPLER_STATE = 22;


            Effect current_effect = null;


            Viewport lastViewport = device.Viewport;
            Rectangle lastScissorBox = device.ScissorRectangle;

            Color lastBlendFactor = device.BlendFactor;
            BlendState lastBlendState = device.BlendState;
            RasterizerState lastRasterizeState = device.RasterizerState;
            DepthStencilState lastDepthStencilState = device.DepthStencilState;
            SamplerState lastsampler = device.SamplerStates[0];


            //var blend_snap_AlphaBlendFunction = device.BlendState.AlphaBlendFunction;
            //var blend_snap_AlphaDestinationBlend = device.BlendState.AlphaDestinationBlend;
            //var blend_snap_AlphaSourceBlend = device.BlendState.AlphaSourceBlend;
            //var blend_snap_ColorBlendFunction = device.BlendState.ColorBlendFunction;
            //var blend_snap_ColorDestinationBlend = device.BlendState.ColorDestinationBlend;
            //var blend_snap_ColorSourceBlend = device.BlendState.ColorSourceBlend;
            //var blend_snap_ColorWriteChannels = device.BlendState.ColorWriteChannels;
            //var blend_snap_ColorWriteChannels1 = device.BlendState.ColorWriteChannels1;
            //var blend_snap_ColorWriteChannels2 = device.BlendState.ColorWriteChannels2;
            //var blend_snap_ColorWriteChannels3 = device.BlendState.ColorWriteChannels3;
            //var blend_snap_BlendFactor = device.BlendState.BlendFactor;
            //var blend_snap_MultiSampleMask = device.BlendState.MultiSampleMask;

            //var rasterize_snap_CullMode = device.RasterizerState.CullMode;
            //var rasterize_snap_DepthBias = device.RasterizerState.DepthBias;
            //var rasterize_snap_FillMode = device.RasterizerState.FillMode;
            //var rasterize_snap_MultiSampleAntiAlias = device.RasterizerState.MultiSampleAntiAlias;
            //var rasterize_snap_ScissorTestEnable = device.RasterizerState.ScissorTestEnable;
            //var rasterize_snap_SlopeScaleDepthBias = device.RasterizerState.SlopeScaleDepthBias;

            //var stencil_snap_DepthBufferEnable = device.DepthStencilState.DepthBufferEnable;
            //var stencil_snap_DepthBufferWriteEnable = device.DepthStencilState.DepthBufferWriteEnable;
            //var stencil_snap_DepthBufferFunction = device.DepthStencilState.DepthBufferFunction;
            //var stencil_snap_StencilEnable = device.DepthStencilState.StencilEnable;
            //var stencil_snap_StencilFunction = device.DepthStencilState.StencilFunction;
            //var stencil_snap_StencilPass = device.DepthStencilState.StencilPass;
            //var stencil_snap_StencilFail = device.DepthStencilState.StencilFail;
            //var stencil_snap_StencilDepthBufferFail = device.DepthStencilState.StencilDepthBufferFail;
            //var stencil_snap_TwoSidedStencilMode = device.DepthStencilState.TwoSidedStencilMode;
            //var stencil_snap_CounterClockwiseStencilFunction = device.DepthStencilState.CounterClockwiseStencilFunction;
            //var stencil_snap_CounterClockwiseStencilFail = device.DepthStencilState.CounterClockwiseStencilFail;
            //var stencil_snap_CounterClockwiseStencilPass = device.DepthStencilState.CounterClockwiseStencilPass;
            //var stencil_snap_CounterClockwiseStencilDepthBufferFail = device.DepthStencilState.CounterClockwiseStencilDepthBufferFail;
            //var stencil_snap_StencilMask = device.DepthStencilState.StencilMask;
            //var stencil_snap_StencilWriteMask = device.DepthStencilState.StencilWriteMask;
            //var stencil_snap_ReferenceStencil = device.DepthStencilState.ReferenceStencil;


            for (int i = 0; i < length; i++)
            {
                BatchCommand command = ((BatchCommand*) ptr)[i];

                switch (command.type)
                {
                    case CMD_VIEWPORT:
                        ref ViewportCommand viewportCommand = ref command.ViewportCommand;

                        device.Viewport = new Viewport(viewportCommand.X, viewportCommand.y, viewportCommand.w, viewportCommand.h);

                        break;

                    case CMD_SCISSOR:
                        ref ScissorCommand scissorCommand = ref command.ScissorCommand;

                        device.ScissorRectangle = new Rectangle(scissorCommand.x, scissorCommand.y, scissorCommand.w, scissorCommand.h);

                        break;

                    case CMD_BLEND_FACTOR:

                        ref BlendFactorCommand blendFactorCommand = ref command.NewBlendFactorCommand;

                        device.BlendFactor = blendFactorCommand.color;

                        break;

                    case CMD_NEW_BLEND_STATE:
                        ref CreateBlendStateCommand createBlend = ref command.NewCreateBlendStateCommand;

                        resources[createBlend.id] = new BlendState
                        {
                            AlphaBlendFunction = createBlend.AlphaBlendFunc,
                            AlphaDestinationBlend = createBlend.AlphaDestBlend,
                            AlphaSourceBlend = createBlend.AlphaSrcBlend,
                            ColorBlendFunction = createBlend.ColorBlendFunc,
                            ColorDestinationBlend = createBlend.ColorDestBlend,
                            ColorSourceBlend = createBlend.ColorSrcBlend,
                            ColorWriteChannels = createBlend.ColorWriteChannels0,
                            ColorWriteChannels1 = createBlend.ColorWriteChannels1,
                            ColorWriteChannels2 = createBlend.ColorWriteChannels2,
                            ColorWriteChannels3 = createBlend.ColorWriteChannels3,
                            BlendFactor = createBlend.BlendFactor,
                            MultiSampleMask = createBlend.MultipleSampleMask
                        };

                        break;

                    case CMD_NEW_RASTERIZE_STATE:

                        ref CreateRasterizerStateCommand rasterize = ref command.NewRasterizeStateCommand;

                        resources[rasterize.id] = new RasterizerState
                        {
                            CullMode = rasterize.CullMode,
                            DepthBias = rasterize.DepthBias,
                            FillMode = rasterize.FillMode,
                            MultiSampleAntiAlias = rasterize.MultiSample,
                            ScissorTestEnable = rasterize.ScissorTestEnabled,
                            SlopeScaleDepthBias = rasterize.SlopeScaleDepthBias
                        };

                        break;

                    case CMD_NEW_STENCIL_STATE:

                        ref CreateStencilStateCommand createStencil = ref command.NewCreateStencilStateCommand;

                        resources[createStencil.id] = new DepthStencilState
                        {
                            DepthBufferEnable = createStencil.DepthBufferEnabled,
                            DepthBufferWriteEnable = createStencil.DepthBufferWriteEnabled,
                            DepthBufferFunction = createStencil.DepthBufferFunc,
                            StencilEnable = createStencil.StencilEnabled,
                            StencilFunction = createStencil.StencilFunc,
                            StencilPass = createStencil.StencilPass,
                            StencilFail = createStencil.StencilFail,
                            StencilDepthBufferFail = createStencil.StencilDepthBufferFail,
                            TwoSidedStencilMode = createStencil.TwoSidedStencilMode,
                            CounterClockwiseStencilFunction = createStencil.CounterClockwiseStencilFunc,
                            CounterClockwiseStencilFail = createStencil.CounterClockwiseStencilFail,
                            CounterClockwiseStencilPass = createStencil.CounterClockwiseStencilPass,
                            CounterClockwiseStencilDepthBufferFail = createStencil.CounterClockwiseStencilDepthBufferFail,
                            StencilMask = createStencil.StencilMask,
                            StencilWriteMask = createStencil.StencilWriteMask,
                            ReferenceStencil = createStencil.ReferenceStencil
                        };


                        break;

                    case CMD_NEW_SAMPLER_STATE:

                        ref CreateSamplerStateCommand createSampler = ref command.NewCreateSamplerStateCommand;

                        resources[createSampler.id] = new SamplerState
                        {
                            AddressU = createSampler.AddressU,
                            AddressV = createSampler.AddressV,
                            AddressW = createSampler.AddressW,
                            Filter = createSampler.TextureFilter,
                            MaxAnisotropy = createSampler.MaxAnisotropy,
                            MaxMipLevel = createSampler.MaxMipLevel,
                            MipMapLevelOfDetailBias = createSampler.MipMapLevelOfDetailBias
                        };

                        break;

                    case CMD_BLEND_STATE:

                        device.BlendState = resources[command.SetBlendStateCommand.id] as BlendState;

                        break;

                    case CMD_RASTERIZE_STATE:

                        device.RasterizerState = resources[command.SetRasterizerStateCommand.id] as RasterizerState;

                        break;

                    case CMD_STENCIL_STATE:

                        device.DepthStencilState = resources[command.SetStencilStateCommand.id] as DepthStencilState;

                        break;

                    case CMD_SAMPLER_STATE:

                        device.SamplerStates[command.SetSamplerStateCommand.index] = resources[command.SetSamplerStateCommand.id] as SamplerState;

                        break;

                    case CMD_SET_VERTEX_DATA:

                        ref SetVertexDataCommand setVertexDataCommand = ref command.SetVertexDataCommand;

                        VertexBuffer vertex_buffer = resources[setVertexDataCommand.id] as VertexBuffer;

                        vertex_buffer?.SetDataPointerEXT(0, setVertexDataCommand.vertex_buffer_ptr, setVertexDataCommand.vertex_buffer_length, SetDataOptions.None);

                        break;

                    case CMD_SET_INDEX_DATA:

                        ref SetIndexDataCommand setIndexDataCommand = ref command.SetIndexDataCommand;

                        IndexBuffer index_buffer = resources[setIndexDataCommand.id] as IndexBuffer;

                        index_buffer?.SetDataPointerEXT(0, setIndexDataCommand.indices_buffer_ptr, setIndexDataCommand.indices_buffer_length, SetDataOptions.None);

                        break;

                    case CMD_CREATE_VERTEX_BUFFER:

                        ref CreateVertexBufferCommand createVertexBufferCommand = ref command.CreateVertexBufferCommand;

                        VertexElement[] elements = new VertexElement[createVertexBufferCommand.DeclarationCount];

                        for (int j = 0; j < elements.Length; j++)
                        {
                            elements[j] = ((VertexElement*) createVertexBufferCommand.Declarations)[j];
                        }

                        VertexBuffer vb = createVertexBufferCommand.IsDynamic ? new DynamicVertexBuffer(device, new VertexDeclaration(createVertexBufferCommand.Size, elements), createVertexBufferCommand.VertexElementsCount, createVertexBufferCommand.BufferUsage) : new VertexBuffer(device, new VertexDeclaration(createVertexBufferCommand.Size, elements), createVertexBufferCommand.VertexElementsCount, createVertexBufferCommand.BufferUsage);

                        resources[createVertexBufferCommand.id] = vb;

                        break;

                    case CMD_CREATE_INDEX_BUFFER:

                        ref CreateIndexBufferCommand createIndexBufferCommand = ref command.CreateIndexBufferCommand;

                        IndexBuffer ib = createIndexBufferCommand.IsDynamic ? new DynamicIndexBuffer(device, createIndexBufferCommand.IndexElementSize, createIndexBufferCommand.IndexCount, createIndexBufferCommand.BufferUsage) : new IndexBuffer(device, createIndexBufferCommand.IndexElementSize, createIndexBufferCommand.IndexCount, createIndexBufferCommand.BufferUsage);

                        resources[createIndexBufferCommand.id] = ib;

                        break;

                    case CMD_SET_VERTEX_BUFFER:

                        ref SetVertexBufferCommand setVertexBufferCommand = ref command.SetVertexBufferCommand;

                        vb = resources[setVertexBufferCommand.id] as VertexBuffer;

                        device.SetVertexBuffer(vb);

                        break;

                    case CMD_SET_INDEX_BUFFER:

                        ref SetIndexBufferCommand setIndexBufferCommand = ref command.SetIndexBufferCommand;

                        ib = resources[setIndexBufferCommand.id] as IndexBuffer;

                        device.Indices = ib;

                        break;

                    case CMD_CREATE_EFFECT:

                        ref CreateEffectCommand createEffectCommand = ref command.CreateEffectCommand;

                        break;

                    case CMD_CREATE_BASIC_EFFECT:

                        ref CreateBasicEffectCommand createBasicEffectCommand = ref command.CreateBasicEffectCommand;

                        if (!resources.TryGetValue(createBasicEffectCommand.id, out GraphicsResource res))
                        {
                            res = new BasicEffect(device);
                            resources[createBasicEffectCommand.id] = res;
                        }
                        else
                        {
                            BasicEffect be = res as BasicEffect;
                            be.World = createBasicEffectCommand.world;
                            be.View = createBasicEffectCommand.view;
                            be.Projection = createBasicEffectCommand.projection;
                            be.TextureEnabled = createBasicEffectCommand.texture_enabled;
                            be.Texture = resources[createBasicEffectCommand.texture_id] as Texture2D;
                            be.VertexColorEnabled = createBasicEffectCommand.vertex_color_enabled;

                            current_effect = be;
                        }

                        break;

                    case CMD_CREATE_TEXTURE_2D:

                        ref CreateTexture2DCommand createTexture2DCommand = ref command.CreateTexture2DCommand;

                        Texture2D texture;

                        if (createTexture2DCommand.IsRenderTarget)
                        {
                            texture = new RenderTarget2D
                            (
                                device,
                                createTexture2DCommand.Width,
                                createTexture2DCommand.Height,
                                false,
                                createTexture2DCommand.Format,
                                DepthFormat.Depth24Stencil8
                            );
                        }
                        else
                        {
                            texture = new Texture2D
                            (
                                device,
                                createTexture2DCommand.Width,
                                createTexture2DCommand.Height,
                                false,
                                createTexture2DCommand.Format
                            );
                        }


                        resources[createTexture2DCommand.id] = texture;

                        break;

                    case CMD_SET_TEXTURE_DATA_2D:

                        ref SetTexture2DDataCommand setTexture2DDataCommand = ref command.SetTexture2DDataCommand;

                        texture = resources[setTexture2DDataCommand.id] as Texture2D;

                        texture?.SetDataPointerEXT(setTexture2DDataCommand.level, new Rectangle(setTexture2DDataCommand.x, setTexture2DDataCommand.y, setTexture2DDataCommand.width, setTexture2DDataCommand.height), setTexture2DDataCommand.data, setTexture2DDataCommand.data_length);

                        break;

                    case CMD_INDEXED_PRIMITIVE_DATA:

                        ref IndexedPrimitiveDataCommand indexedPrimitiveDataCommand = ref command.IndexedPrimitiveDataCommand;

                        //device.Textures[0] = resources[indexedPrimitiveDataCommand.texture_id] as Texture;

                        if (current_effect != null)
                        {
                            foreach (EffectPass pass in current_effect.CurrentTechnique.Passes)
                            {
                                pass.Apply();

                                device.DrawIndexedPrimitives
                                (
                                    indexedPrimitiveDataCommand.PrimitiveType,
                                    indexedPrimitiveDataCommand.BaseVertex,
                                    indexedPrimitiveDataCommand.MinVertexIndex,
                                    indexedPrimitiveDataCommand.NumVertices,
                                    indexedPrimitiveDataCommand.StartIndex,
                                    indexedPrimitiveDataCommand.PrimitiveCount
                                );
                            }
                        }
                        else
                        {
                            device.DrawIndexedPrimitives
                            (
                                indexedPrimitiveDataCommand.PrimitiveType,
                                indexedPrimitiveDataCommand.BaseVertex,
                                indexedPrimitiveDataCommand.MinVertexIndex,
                                indexedPrimitiveDataCommand.NumVertices,
                                indexedPrimitiveDataCommand.StartIndex,
                                indexedPrimitiveDataCommand.PrimitiveCount
                            );
                        }

                        break;

                    case CMD_DESTROY_RESOURCE:

                        ref DestroyResourceCommand destroyResourceCommand = ref command.DestroyResourceCommand;

                        resources[destroyResourceCommand.id]?.Dispose();

                        resources.Remove(destroyResourceCommand.id);

                        break;
                }
            }


            device.Viewport = lastViewport;
            device.ScissorRectangle = lastScissorBox;
            device.BlendFactor = lastBlendFactor;
            device.BlendState = lastBlendState;
            device.RasterizerState = lastRasterizeState;
            device.DepthStencilState = lastDepthStencilState;
            device.SamplerStates[0] = lastsampler;
        }


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void OnInstall(void* header);

        [return: MarshalAs(UnmanagedType.I1)]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool OnPacketSendRecv_new(byte[] data, ref int length);

        [return: MarshalAs(UnmanagedType.I1)]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool OnPacketSendRecv_new_intptr(IntPtr data, ref int length);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int OnDrawCmdList([Out] out IntPtr cmdlist, ref int size);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int OnWndProc(SDL.SDL_Event* ev);

        [return: MarshalAs(UnmanagedType.I1)]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool OnGetStaticData
        (
            int index,
            ref ulong flags,
            ref byte weight,
            ref byte layer,
            ref int count,
            ref ushort animid,
            ref ushort lightidx,
            ref byte height,
            ref string name
        );

        [return: MarshalAs(UnmanagedType.I1)]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool OnGetTileData(int index, ref ulong flags, ref ushort textid, ref string name);

        [return: MarshalAs(UnmanagedType.I1)]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool OnGetCliloc(int cliloc, [MarshalAs(UnmanagedType.LPStr)] string args, bool capitalize, [Out] [MarshalAs(UnmanagedType.LPStr)] out string buffer);


        private struct PluginHeader
        {
            public int ClientVersion;
            public IntPtr HWND;
            public IntPtr OnRecv;
            public IntPtr OnSend;
            public IntPtr OnHotkeyPressed;
            public IntPtr OnMouse;
            public IntPtr OnPlayerPositionChanged;
            public IntPtr OnClientClosing;
            public IntPtr OnInitialize;
            public IntPtr OnConnected;
            public IntPtr OnDisconnected;
            public IntPtr OnFocusGained;
            public IntPtr OnFocusLost;
            public IntPtr GetUOFilePath;
            public IntPtr Recv;
            public IntPtr Send;
            public IntPtr GetPacketLength;
            public IntPtr GetPlayerPosition;
            public IntPtr CastSpell;
            public IntPtr GetStaticImage;
            public IntPtr Tick;
            public IntPtr RequestMove;
            public IntPtr SetTitle;

            public IntPtr OnRecv_new, OnSend_new, Recv_new, Send_new;

            public IntPtr OnDrawCmdList;
            public IntPtr SDL_Window;
            public IntPtr OnWndProc;
            public IntPtr GetStaticData;
            public IntPtr GetTileData;
            public IntPtr GetCliloc;
        }
    }
}