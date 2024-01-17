#region license

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

using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using ClassicUO.Utility.Platforms;
using Microsoft.Xna.Framework.Graphics;
using SDL2;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace ClassicUO
{
    internal static class Bootstrap
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetDllDirectory(string lpPathName);


        [UnmanagedCallersOnly(EntryPoint = "Initialize", CallConvs = new Type[] { typeof(CallConvCdecl) })]
        static unsafe void Initialize(IntPtr* argv, int argc, HostSetup* hostSetup)
        {
            Console.WriteLine("run!");

            var args = new string[argc];
            for (int i = 0; i < argc; i++)
            {
                args[i] = Marshal.PtrToStringAnsi(argv[i]);
            }

            var host = new UnmanagedAssistantHost(hostSetup);
            Boot(host, args);
        }

        private static void PatchEnvVars()
        {
            // Patch necessary for .NET 5
            foreach (System.Collections.DictionaryEntry envs in Environment.GetEnvironmentVariables())
            {
                SDL2.SDL.SDL_SetHint(envs.Key.ToString(), envs.Value.ToString());
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct HostSetup
        {
            public IntPtr InitializeFn;
            public IntPtr LoadPluginFn;
            public IntPtr TickFn;
            public IntPtr ClosingFn;
            public IntPtr FocusGainedFn;
            public IntPtr FocusLostFn;
            public IntPtr ConnectedFn;
            public IntPtr DisconnectedFn;
            public IntPtr /*delegate*<int, int, bool, bool>*/ HotkeyFn;
            public IntPtr /*delegate*<int, int, void>*/ MouseFn;
            public IntPtr /*delegate*<IntPtr, ref int, void>*/ CmdListFn;
            public IntPtr /*delegate*<IntPtr, int>*/ SdlEventFn;
            public IntPtr /*delegate*<int, int, int, void>*/ UpdatePlayerPosFn;
            public IntPtr PacketInFn;
            public IntPtr PacketOutFn;
        }

        [StructLayout(LayoutKind.Sequential)]
        unsafe struct CuoHostSetup
        {
            public IntPtr /*delegate*<IntPtr, ref int, bool>*/ PluginRecvFn;
            public IntPtr /*delegate*<IntPtr, ref int, bool>*/ PluginSendFn;
            public IntPtr /*delegate*<int, short>*/ PacketLengthFn;
            public IntPtr /*delegate*<int, void>*/ CastSpellFn;
            public IntPtr /*delegate*<IntPtr>*/ SetWindowTitleFn;
            public IntPtr /*delegate*<int, IntPtr, IntPtr, bool, bool>*/ GetClilocFn;
            public IntPtr /*delegate*<int, bool, bool>*/ RequestMoveFn;
            public IntPtr /*delegate*<ref int, ref int, ref int, bool>*/ GetPlayerPositionFn;
            public IntPtr ReflectionCmdFn;
        }

        internal unsafe sealed class UnmanagedAssistantHost : IPluginHost
        {
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate void dOnPluginBindCuoFunctions(IntPtr exportedFuncs);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            private readonly dOnPluginBindCuoFunctions _initialize;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate void dOnPluginLoad(IntPtr pluginPathPtr, uint clientVersion, IntPtr assetsPathPtr, IntPtr sdlWindow);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            private readonly dOnPluginLoad _loadPlugin;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate void dOnPluginTick();
            [MarshalAs(UnmanagedType.FunctionPtr)]
            private readonly dOnPluginTick _tick;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate void dOnPluginClose();
            [MarshalAs(UnmanagedType.FunctionPtr)]
            private readonly dOnPluginClose _close;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate void dOnPluginConnection();
            [MarshalAs(UnmanagedType.FunctionPtr)]
            private readonly dOnPluginConnection _connected, _disconnected;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate bool dOnPluginPacketInOut(IntPtr data, ref int length);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            private readonly dOnPluginPacketInOut _packetIn, _packetOut;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate bool dOnHotkey(int key, int mod, bool pressed);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            private readonly dOnHotkey _hotkey;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate void dOnMouse(int button, int wheel);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            private readonly dOnMouse _mouse;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate bool dOnUpdatePlayerPosition(int x, int y, int z);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            private readonly dOnUpdatePlayerPosition _updatePlayerPos;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate void dOnPluginFocusWindow();
            [MarshalAs(UnmanagedType.FunctionPtr)]
            private readonly dOnPluginFocusWindow _focusGained, _focusLost;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate int dOnPluginSdlEvent(IntPtr ev);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            private readonly dOnPluginSdlEvent _sdlEvent;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate void dOnPluginCommandList(out IntPtr list, out int len);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            private readonly dOnPluginCommandList _cmdList;


            public UnmanagedAssistantHost(HostSetup* setup) 
            {
                _initialize = Marshal.GetDelegateForFunctionPointer<dOnPluginBindCuoFunctions>(setup->InitializeFn);
                _loadPlugin = Marshal.GetDelegateForFunctionPointer<dOnPluginLoad>(setup->LoadPluginFn);
                _tick = Marshal.GetDelegateForFunctionPointer<dOnPluginTick>(setup->TickFn);
                _close = Marshal.GetDelegateForFunctionPointer<dOnPluginClose>(setup->ClosingFn);
                _packetIn = Marshal.GetDelegateForFunctionPointer<dOnPluginPacketInOut>(setup->PacketInFn);
                _packetOut = Marshal.GetDelegateForFunctionPointer<dOnPluginPacketInOut>(setup->PacketOutFn);
                _hotkey = Marshal.GetDelegateForFunctionPointer<dOnHotkey>(setup->HotkeyFn);
                _mouse = Marshal.GetDelegateForFunctionPointer<dOnMouse>(setup->MouseFn);
                _updatePlayerPos = Marshal.GetDelegateForFunctionPointer<dOnUpdatePlayerPosition>(setup->UpdatePlayerPosFn);
                _focusGained = Marshal.GetDelegateForFunctionPointer<dOnPluginFocusWindow>(setup->FocusGainedFn);
                _focusLost = Marshal.GetDelegateForFunctionPointer<dOnPluginFocusWindow>(setup->FocusLostFn);
                _sdlEvent = Marshal.GetDelegateForFunctionPointer<dOnPluginSdlEvent>(setup->SdlEventFn);
                _connected = Marshal.GetDelegateForFunctionPointer<dOnPluginConnection>(setup->ConnectedFn);
                _disconnected = Marshal.GetDelegateForFunctionPointer<dOnPluginConnection>(setup->DisconnectedFn);
                _cmdList = Marshal.GetDelegateForFunctionPointer<dOnPluginCommandList>(setup->CmdListFn);
            }

            public Dictionary<IntPtr, GraphicsResource> GfxResources { get; } = new Dictionary<nint, GraphicsResource>();

            public void Closing()
            {
                _close?.Invoke();
            }

            public void GetCommandList(out IntPtr listPtr, out int listCount)
            {
                listPtr = IntPtr.Zero;
                listCount = 0;
                _cmdList?.Invoke(out listPtr, out  listCount);
            }

            public void Connected()
            {
                _connected?.Invoke();
            }

            public void Disconnected()
            {
                _disconnected?.Invoke();
            }

            public void FocusGained()
            {
                _focusGained?.Invoke();
            }

            public void FocusLost()
            {
                _focusLost?.Invoke();
            }

            public bool Hotkey(int key, int mod, bool pressed)
            {
                return _hotkey == null || _hotkey(key, mod, pressed);
            }

            public void Mouse(int button, int wheel)
            {
                _mouse?.Invoke(button, wheel);
            }


            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate void dCastSpell(int index);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            private readonly dCastSpell _castSpell = GameActions.CastSpell;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate IntPtr dGetCliloc(int cliloc, IntPtr args, bool capitalize);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            private readonly dGetCliloc _getCliloc;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate short dGetPacketLength(int id);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            private readonly dGetPacketLength _packetLength = PacketsTable.GetPacketLength;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate bool dGetPlayerPosition(out int x, out int y, out int z);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            private readonly dGetPlayerPosition _getPlayerPosition = Plugin.GetPlayerPosition;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate bool dRequestMove(int dir, bool run);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            private readonly dRequestMove _requestMove = Plugin.RequestMove;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate bool dPacketRecvSend(IntPtr data, ref int length);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            private readonly dPacketRecvSend _sendToClient = Plugin.OnPluginRecv_new;
            [MarshalAs(UnmanagedType.FunctionPtr)]
            private readonly dPacketRecvSend _sendToServer = Plugin.OnPluginSend_new;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate void dSetWindowTitle(IntPtr textPtr);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            private readonly dSetWindowTitle _setWindowTitle = setWindowTitle;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate void dOnPluginReflectionCommand(IntPtr cmdPtr);
            [MarshalAs(UnmanagedType.FunctionPtr)]
            private readonly dOnPluginReflectionCommand _reflectionCmd = reflectionCmd;


            static void setWindowTitle(IntPtr ptr)
            {
                var title = SDL2.SDL.UTF8_ToManaged(ptr);
                Client.Game.SetWindowTitle(title);
            }

            static void reflectionCmd(IntPtr cmd)
            {
                Console.WriteLine("called reflection cmd {0}", cmd);
            }

            public void Initialize()
            {
                if (_initialize == null)
                    return;

                var mem = NativeMemory.AllocZeroed((nuint)sizeof(CuoHostSetup));
                ref var cuoHost = ref Unsafe.AsRef<CuoHostSetup>(mem);
                cuoHost.PacketLengthFn = Marshal.GetFunctionPointerForDelegate(_packetLength);
                cuoHost.CastSpellFn = Marshal.GetFunctionPointerForDelegate(_castSpell);
                cuoHost.SetWindowTitleFn = Marshal.GetFunctionPointerForDelegate(_setWindowTitle);
                cuoHost.PluginRecvFn = Marshal.GetFunctionPointerForDelegate(_sendToClient);
                cuoHost.PluginSendFn = Marshal.GetFunctionPointerForDelegate(_sendToServer);
                cuoHost.RequestMoveFn = Marshal.GetFunctionPointerForDelegate(_requestMove);
                cuoHost.GetPlayerPositionFn = Marshal.GetFunctionPointerForDelegate(_getPlayerPosition);
                cuoHost.ReflectionCmdFn = Marshal.GetFunctionPointerForDelegate(_reflectionCmd);

                _initialize((IntPtr)mem);
            }

            public void LoadPlugin(string pluginPath)
            {
                if (_loadPlugin == null)
                    return;

                var pluginPathPtr = Marshal.StringToHGlobalAnsi(pluginPath);
                var uoAssetsPtr = Marshal.StringToHGlobalAnsi(Settings.GlobalSettings.UltimaOnlineDirectory);

                _loadPlugin
                (
                    pluginPathPtr,
                    (uint)Client.Game.UO.Version,
                    uoAssetsPtr,
                    Client.Game.Window.Handle
                );

                if (pluginPathPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(pluginPathPtr);

                if (uoAssetsPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(uoAssetsPtr);
            }

            public bool PacketIn(ArraySegment<byte> buffer)
            {
                if (_packetIn == null || buffer.Array == null || buffer.Count <= 0)
                    return true;

                var len = buffer.Count;
                fixed (byte* ptr = buffer.Array)
                    return _packetIn((IntPtr)ptr, ref len);
            }

            public bool PacketOut(Span<byte> buffer)
            {
                if (_packetOut == null || buffer.IsEmpty)
                    return true;

                var len = buffer.Length;
                fixed (byte* ptr = buffer)
                    return _packetOut((IntPtr)ptr, ref len);
            }

            public unsafe int SdlEvent(SDL.SDL_Event* ev)
            {
                return _sdlEvent != null ? _sdlEvent((IntPtr)ev) : 0;
            }

            public void Tick()
            {
                _tick?.Invoke();
            }

            public void UpdatePlayerPosition(int x, int y, int z)
            {
                _updatePlayerPos?.Invoke(x, y, z);
            }
        }

        [STAThread]
        public static void Main(string[] args) => Boot(null, args);


        public static void Boot(UnmanagedAssistantHost pluginHost, string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

#if !NETFRAMEWORK
            //DllMap.Initialise();
            DllMap.Init(Assembly.GetExecutingAssembly());
            DllMap.Init(typeof(Microsoft.Xna.Framework.Point).Assembly);
            PatchEnvVars();
#endif

            Log.Start(LogTypes.All);

            CUOEnviroment.GameThread = Thread.CurrentThread;
            CUOEnviroment.GameThread.Name = "CUO_MAIN_THREAD";

#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("######################## [START LOG] ########################");

#if DEV_BUILD
                sb.AppendLine($"ClassicUO [DEV_BUILD] - {CUOEnviroment.Version} - {DateTime.Now}");
#else
                sb.AppendLine($"ClassicUO [STANDARD_BUILD] - {CUOEnviroment.Version} - {DateTime.Now}");
#endif

                sb.AppendLine
                    ($"OS: {Environment.OSVersion.Platform} {(Environment.Is64BitOperatingSystem ? "x64" : "x86")}");

                sb.AppendLine($"Thread: {Thread.CurrentThread.Name}");
                sb.AppendLine();

                if (Settings.GlobalSettings != null)
                {
                    sb.AppendLine($"Shard: {Settings.GlobalSettings.IP}");
                    sb.AppendLine($"ClientVersion: {Settings.GlobalSettings.ClientVersion}");
                    sb.AppendLine();
                }

                sb.AppendFormat("Exception:\n{0}\n", e.ExceptionObject);
                sb.AppendLine("######################## [END LOG] ########################");
                sb.AppendLine();
                sb.AppendLine();

                Log.Panic(e.ExceptionObject.ToString());
                string path = Path.Combine(CUOEnviroment.ExecutablePath, "Logs");

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                using (LogFile crashfile = new LogFile(path, "crash.txt"))
                {
                    crashfile.WriteAsync(sb.ToString()).RunSynchronously();
                }
            };
#endif
            ReadSettingsFromArgs(args);

            if (CUOEnviroment.IsHighDPI)
            {
                Environment.SetEnvironmentVariable("FNA_GRAPHICS_ENABLE_HIGHDPI", "1");
            }

            Environment.SetEnvironmentVariable("FNA3D_BACKBUFFER_SCALE_NEAREST", "1");
            Environment.SetEnvironmentVariable("FNA3D_OPENGL_FORCE_COMPATIBILITY_PROFILE", "1");
            Environment.SetEnvironmentVariable(SDL.SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");

            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Plugins"));

            string globalSettingsPath = Settings.GetSettingsFilepath();

            if (!Directory.Exists(Path.GetDirectoryName(globalSettingsPath)) || !File.Exists(globalSettingsPath))
            {
                // settings specified in path does not exists, make new one
                {
                    // TODO: 
                    Settings.GlobalSettings.Save();
                }
            }

            Settings.GlobalSettings = ConfigurationResolver.Load<Settings>(globalSettingsPath, SettingsJsonContext.Default);
            CUOEnviroment.IsOutlands = Settings.GlobalSettings.ShardType == 2;

            ReadSettingsFromArgs(args);

            // still invalid, cannot load settings
            if (Settings.GlobalSettings == null)
            {
                Settings.GlobalSettings = new Settings();
                Settings.GlobalSettings.Save();
            }

            if (!CUOEnviroment.IsUnix)
            {
                string libsPath = Path.Combine(CUOEnviroment.ExecutablePath, Environment.Is64BitProcess ? "x64" : "x86");

                SetDllDirectory(libsPath);
            }

            if (string.IsNullOrWhiteSpace(Settings.GlobalSettings.Language))
            {
                Log.Trace("language is not set. Trying to get the OS language.");
                try
                {
                    Settings.GlobalSettings.Language = CultureInfo.InstalledUICulture.ThreeLetterWindowsLanguageName;

                    if (string.IsNullOrWhiteSpace(Settings.GlobalSettings.Language))
                    {
                        Log.Warn("cannot read the OS language. Rolled back to ENU");

                        Settings.GlobalSettings.Language = "ENU";
                    }

                    Log.Trace($"language set: '{Settings.GlobalSettings.Language}'");
                }
                catch
                {
                    Log.Warn("cannot read the OS language. Rolled back to ENU");

                    Settings.GlobalSettings.Language = "ENU";
                }
            }

            if (string.IsNullOrWhiteSpace(Settings.GlobalSettings.UltimaOnlineDirectory))
            {
                Settings.GlobalSettings.UltimaOnlineDirectory = CUOEnviroment.ExecutablePath;
            }

            const uint INVALID_UO_DIRECTORY = 0x100;
            const uint INVALID_UO_VERSION = 0x200;

            uint flags = 0;

            if (!Directory.Exists(Settings.GlobalSettings.UltimaOnlineDirectory) || !File.Exists(Path.Combine(Settings.GlobalSettings.UltimaOnlineDirectory, "tiledata.mul")))
            {
                flags |= INVALID_UO_DIRECTORY;
            }

            string clientVersionText = Settings.GlobalSettings.ClientVersion;

            if (!ClientVersionHelper.IsClientVersionValid(Settings.GlobalSettings.ClientVersion, out ClientVersion clientVersion))
            {
                Log.Warn($"Client version [{clientVersionText}] is invalid, let's try to read the client.exe");

                // mmm something bad happened, try to load from client.exe [windows only]
                if (!ClientVersionHelper.TryParseFromFile(Path.Combine(Settings.GlobalSettings.UltimaOnlineDirectory, "client.exe"), out clientVersionText) || !ClientVersionHelper.IsClientVersionValid(clientVersionText, out clientVersion))
                {
                    Log.Error("Invalid client version: " + clientVersionText);

                    flags |= INVALID_UO_VERSION;
                }
                else
                {
                    Log.Trace($"Found a valid client.exe [{clientVersionText} - {clientVersion}]");

                    // update the wrong/missing client version in settings.json
                    Settings.GlobalSettings.ClientVersion = clientVersionText;
                }
            }

            if (flags != 0)
            {
                if ((flags & INVALID_UO_DIRECTORY) != 0)
                {
                    Client.ShowErrorMessage(ResGeneral.YourUODirectoryIsInvalid);
                }
                else if ((flags & INVALID_UO_VERSION) != 0)
                {
                    Client.ShowErrorMessage(ResGeneral.YourUOClientVersionIsInvalid);
                }

                PlatformHelper.LaunchBrowser(ResGeneral.ClassicUOLink);
            }
            else
            {
                switch (Settings.GlobalSettings.ForceDriver)
                {
                    case 1: // OpenGL
                        Environment.SetEnvironmentVariable("FNA3D_FORCE_DRIVER", "OpenGL");

                        break;

                    case 2: // Vulkan
                        Environment.SetEnvironmentVariable("FNA3D_FORCE_DRIVER", "Vulkan");

                        break;
                }

                Client.Run(pluginHost);
            }

            Log.Trace("Closing...");
        }

        private static void ReadSettingsFromArgs(string[] args)
        {
            for (int i = 0; i <= args.Length - 1; i++)
            {
                string cmd = args[i].ToLower();

                // NOTE: Command-line option name should start with "-" character
                if (cmd.Length == 0 || cmd[0] != '-')
                {
                    continue;
                }

                cmd = cmd.Remove(0, 1);
                string value = string.Empty;

                if (i < args.Length - 1)
                {
                    if (!string.IsNullOrWhiteSpace(args[i + 1]) && !args[i + 1].StartsWith("-"))
                    {
                        value = args[++i];
                    }
                }

                Log.Trace($"ARG: {cmd}, VALUE: {value}");

                switch (cmd)
                {
                    // Here we have it! Using `-settings` option we can now set the filepath that will be used 
                    // to load and save ClassicUO main settings instead of default `./settings.json`
                    // NOTE: All individual settings like `username`, `password`, etc passed in command-line options
                    // will override and overwrite those in the settings file because they have higher priority
                    case "settings":
                        Settings.CustomSettingsFilepath = value;

                        break;

                    case "highdpi":
                        CUOEnviroment.IsHighDPI = true;

                        break;

                    case "username":
                        Settings.GlobalSettings.Username = value;

                        break;

                    case "password":
                        Settings.GlobalSettings.Password = Crypter.Encrypt(value);

                        break;

                    case "password_enc": // Non-standard setting, similar to `password` but for already encrypted password
                        Settings.GlobalSettings.Password = value;

                        break;

                    case "ip":
                        Settings.GlobalSettings.IP = value;

                        break;

                    case "port":
                        Settings.GlobalSettings.Port = ushort.Parse(value);

                        break;

                    case "filesoverride":
                    case "uofilesoverride":
                        UOFilesOverrideMap.OverrideFile = value;

                        break;

                    case "ultimaonlinedirectory":
                    case "uopath":
                        Settings.GlobalSettings.UltimaOnlineDirectory = value;

                        break;

                    case "profilespath":
                        Settings.GlobalSettings.ProfilesPath = value;

                        break;

                    case "clientversion":
                        Settings.GlobalSettings.ClientVersion = value;

                        break;

                    case "lastcharactername":
                    case "lastcharname":
                        LastCharacterManager.OverrideLastCharacter(value);

                        break;

                    case "lastservernum":
                        Settings.GlobalSettings.LastServerNum = ushort.Parse(value);

                        break;

                    case "last_server_name":
                        Settings.GlobalSettings.LastServerName = value;
                        break;

                    case "fps":
                        int v = int.Parse(value);

                        if (v < Constants.MIN_FPS)
                        {
                            v = Constants.MIN_FPS;
                        }
                        else if (v > Constants.MAX_FPS)
                        {
                            v = Constants.MAX_FPS;
                        }

                        Settings.GlobalSettings.FPS = v;

                        break;

                    case "debug":
                        CUOEnviroment.Debug = true;

                        break;

                    case "profiler":
                        Profiler.Enabled = bool.Parse(value);

                        break;

                    case "saveaccount":
                        Settings.GlobalSettings.SaveAccount = bool.Parse(value);

                        break;

                    case "autologin":
                        Settings.GlobalSettings.AutoLogin = bool.Parse(value);

                        break;

                    case "reconnect":
                        Settings.GlobalSettings.Reconnect = bool.Parse(value);

                        break;

                    case "reconnect_time":

                        if (!int.TryParse(value, out int reconnectTime) || reconnectTime < 1000)
                        {
                            reconnectTime = 1000;
                        }

                        Settings.GlobalSettings.ReconnectTime = reconnectTime;

                        break;

                    case "login_music":
                    case "music":
                        Settings.GlobalSettings.LoginMusic = bool.Parse(value);

                        break;

                    case "login_music_volume":
                    case "music_volume":
                        Settings.GlobalSettings.LoginMusicVolume = int.Parse(value);

                        break;

                    // ======= [SHARD_TYPE_FIX] =======
                    // TODO old. maintain it for retrocompatibility
                    case "shard_type":
                    case "shard":
                        Settings.GlobalSettings.ShardType = int.Parse(value);

                        break;
                    // ================================

                    case "outlands":
                        CUOEnviroment.IsOutlands = true;

                        break;

                    case "fixed_time_step":
                        Settings.GlobalSettings.FixedTimeStep = bool.Parse(value);

                        break;

                    case "skiploginscreen":
                        CUOEnviroment.SkipLoginScreen = true;

                        break;

                    case "plugins":
                        Settings.GlobalSettings.Plugins = string.IsNullOrEmpty(value) ? new string[0] : value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        break;

                    case "use_verdata":
                        Settings.GlobalSettings.UseVerdata = bool.Parse(value);

                        break;

                    case "maps_layouts":

                        Settings.GlobalSettings.MapsLayouts = value;

                        break;

                    case "encryption":
                        Settings.GlobalSettings.Encryption = byte.Parse(value);

                        break;

                    case "force_driver":
                        if (byte.TryParse(value, out byte res))
                        {
                            switch (res)
                            {
                                case 1: // OpenGL
                                    Settings.GlobalSettings.ForceDriver = 1;

                                    break;

                                case 2: // Vulkan
                                    Settings.GlobalSettings.ForceDriver = 2;

                                    break;

                                default: // use default
                                    Settings.GlobalSettings.ForceDriver = 0;

                                    break;
                            }
                        }
                        else
                        {
                            Settings.GlobalSettings.ForceDriver = 0;
                        }

                        break;

                    case "packetlog":

                        PacketLogger.Default.Enabled = true;
                        PacketLogger.Default.CreateFile();

                        break;

                    case "language":

                        switch (value?.ToUpperInvariant())
                        {
                            case "RUS": Settings.GlobalSettings.Language = "RUS"; break;
                            case "FRA": Settings.GlobalSettings.Language = "FRA"; break;
                            case "DEU": Settings.GlobalSettings.Language = "DEU"; break;
                            case "ESP": Settings.GlobalSettings.Language = "ESP"; break;
                            case "JPN": Settings.GlobalSettings.Language = "JPN"; break;
                            case "KOR": Settings.GlobalSettings.Language = "KOR"; break;
                            case "PTB": Settings.GlobalSettings.Language = "PTB"; break;
                            case "ITA": Settings.GlobalSettings.Language = "ITA"; break;
                            case "CHT": Settings.GlobalSettings.Language = "CHT"; break;
                            default:

                                Settings.GlobalSettings.Language = "ENU";
                                break;

                        }

                        break;

                    case "no_server_ping":

                        CUOEnviroment.NoServerPing = true;

                        break;
                }
            }
        }
    }
}