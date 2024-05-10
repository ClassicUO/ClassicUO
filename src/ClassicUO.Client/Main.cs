#region license

// Copyright (c) 2024, andreakarasho
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
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using ClassicUO.Utility.Platforms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SDL2;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using TinyEcs;

namespace ClassicUO
{
    internal static class Bootstrap
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetDllDirectory(string lpPathName);


        [UnmanagedCallersOnly(EntryPoint = "Initialize", CallConvs = new Type[] { typeof(CallConvCdecl) })]
        static unsafe void Initialize(IntPtr* argv, int argc, HostBindings* hostSetup)
        {
            var args = new string[argc];
            for (int i = 0; i < argc; i++)
            {
                args[i] = Marshal.PtrToStringAnsi(argv[i]);
            }

            var host = new UnmanagedAssistantHost(hostSetup);
            Boot(host, args);
        }


        [STAThread]
        public static void Main(string[] args) => Boot(null, args);


        public static void Boot(UnmanagedAssistantHost pluginHost, string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

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

            //Environment.SetEnvironmentVariable("FNA3D_FORCE_DRIVER", "OpenGL");

            // NOTE: this is a workaroud to fix d3d11 on windows 11 + scale windows
            Environment.SetEnvironmentVariable("FNA3D_D3D11_FORCE_BITBLT", "1");

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

            Settings.GlobalSettings = ConfigurationResolver.Load<Settings>(globalSettingsPath, SettingsJsonContext.RealDefault.Settings);
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

                var ecs = new TinyEcs.World();
                var scheduler = new Scheduler(ecs);

                scheduler.AddPlugin<MainPlugin>();
                while (true)
                    scheduler.Run();

                //Client.Run(pluginHost);
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

    struct Renderable
    {
        public Texture2D Texture;
        public Vector2 Position;
        public Vector3 Color;
        public Rectangle UV;
        public float Z;
    }

    struct TileStretched
    {
        public sbyte AvgZ, MinZ;
        public Renderer.UltimaBatcher2D.YOffsets Offset;
        public Vector3 NormalTop, NormalRight, NormalLeft, NormalBottom;
    }

    readonly struct MainPlugin : IPlugin
    {
        public void Build(Scheduler scheduler)
        {
            // center world position x,y
            //scheduler.AddResource(((ushort)1631, (ushort)1233));
            scheduler.AddResource(((ushort)1431, (ushort)1690, Vector2.Zero));

            scheduler.AddPlugin(new FnaPlugin() {
                WindowResizable = true,
                MouseVisible = true,
                VSync = true, // don't kill the gpu
            });

            scheduler.AddPlugin<CuoPlugin>();

            scheduler.AddSystem((EventWriter<OnNewChunkRequest> chunkWriter) => {
                var offset = 8;
                for (var x = -offset; x < offset; x += 1)
                for (var y = -offset; y < offset; y += 1)
                    chunkWriter.Enqueue(new () { X = (1431 / 8) + x, Y = (1690 / 8) + y, Map = 0});
            }, Stages.Startup);
        }
    }

    static class Isometric
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 IsoToScreen(ushort isoX, ushort isoY, sbyte isoZ)
        {
            return new Vector2(
                (isoX - isoY) * 22,
                (isoX + isoY) * 22 - (isoZ << 2)
            );
        }

    }

    struct OnNewChunkRequest { public int Map; public int X; public int Y; }
    struct OnPacketRecv { public byte[] RentedBuffer; public int Length; }

    // TODO: just for test
    readonly struct CuoPlugin : IPlugin
    {
        public unsafe void Build(Scheduler scheduler)
        {
            scheduler.AddEvent<OnNewChunkRequest>();
            scheduler.AddEvent<OnPacketRecv>();
            scheduler.AddResource(new NetClient());

            scheduler.AddSystem(static (Res<GraphicsDevice> device, SchedulerState schedState, TinyEcs.World world) => {
                world.Entity<Renderable>();
                world.Entity<TileStretched>();
                ClientVersionHelper.IsClientVersionValid(Settings.GlobalSettings.ClientVersion, out ClientVersion clientVersion);
                Assets.UOFileManager.Load(clientVersion, Settings.GlobalSettings.UltimaOnlineDirectory, false, "ENU");

                schedState.AddResource(Assets.TileDataLoader.Instance);
                schedState.AddResource(Assets.MapLoader.Instance);
                schedState.AddResource(new Renderer.Arts.Art(device));
                schedState.AddResource(new Renderer.Texmaps.Texmap(device));
                schedState.AddResource(new Renderer.UltimaBatcher2D(device));
                schedState.AddResource(clientVersion);
            }, Stages.Startup);

            scheduler.AddSystem((Res<NetClient> network, Res<ClientVersion> clientVersion) => {
                PacketsTable.AdjustPacketSizeByVersion(clientVersion.Value);
                network.Value.Connect("localhost", 2593);

                var major = (byte) ((uint)clientVersion.Value >> 24);
                var minor = (byte) ((uint)clientVersion.Value >> 16);
                var build = (byte) ((uint)clientVersion.Value >> 8);
                var extra = (byte) clientVersion.Value;

                network.Value.Send_Seed(network.Value.LocalIP, major, minor, build, extra);
                network.Value.Send_FirstLogin(Settings.GlobalSettings.Username, Crypter.Decrypt(Settings.GlobalSettings.Password));
            }, Stages.Startup);

            scheduler.AddSystem(static (
                TinyEcs.World world,
                Res<Assets.MapLoader> mapLoader,
                Res<Assets.TileDataLoader> tiledataLoader,
                Res<Renderer.Arts.Art> arts,
                Res<Renderer.Texmaps.Texmap> textmaps,
                Res<(ushort, ushort, Vector2)> centerWorldPos,
                EventReader<OnNewChunkRequest> chunkRequests
            ) => {
                static float getDepthZ(int x, int y, int priorityZ)
                    => x + y + (127 + priorityZ) * 0.01f;

                foreach (var chunkEv in chunkRequests.Read())
                {
                    ref var im = ref mapLoader.Value.GetIndex(chunkEv.Map, chunkEv.X, chunkEv.Y);

                    if (im.MapAddress == 0)
                        continue;

                    var block = (Assets.MapBlock*) im.MapAddress;
                    var cells = (Assets.MapCells*) &block->Cells;
                    var bx = chunkEv.X << 3;
                    var by = chunkEv.Y << 3;

                    for (int y = 0; y < 8; ++y)
                    {
                        var pos = y << 3;
                        var tileY = (ushort) (by + y);

                        for (int x = 0; x < 8; ++x, ++pos)
                        {
                            var tileID = (ushort) (cells[pos].TileID & 0x3FFF);
                            var z = cells[pos].Z;
                            var tileX = (ushort) (bx + x);

                            var isStretched = tiledataLoader.Value.LandData[tileID].TexID == 0 &&
                                tiledataLoader.Value.LandData[tileID].IsWet;

                            isStretched = ApplyStretch(
                                chunkEv.Map, tiledataLoader.Value.LandData[tileID].TexID,
                                tileX, tileY, z,
                                isStretched,
                                out var avgZ, out var minZ,
                                out var offsets,
                                out var normalTop,
                                out var normalRight,
                                out var normalBottom,
                                out var normalLeft
                            );

                            if (isStretched)
                            {
                                ref readonly var textmapInfo = ref textmaps.Value.GetTexmap(tiledataLoader.Value.LandData[tileID].TexID);

                                var position = Isometric.IsoToScreen(tileX, tileY, z);
                                position.Y += z << 2;

                                world.Entity()
                                    .Set(new Renderable() {
                                        Texture = textmapInfo.Texture,
                                        UV = textmapInfo.UV,
                                        Color = new Vector3(0, Renderer.ShaderHueTranslator.SHADER_LAND, 1f),
                                        Position = position,
                                        Z = getDepthZ(tileX, tileY, avgZ - 2)
                                    })
                                    .Set(new TileStretched() {
                                        NormalTop = normalTop,
                                        NormalRight = normalRight,
                                        NormalBottom = normalBottom,
                                        NormalLeft = normalLeft,
                                        AvgZ = avgZ,
                                        MinZ = minZ,
                                        Offset = offsets
                                    });
                            }
                            else
                            {
                                ref readonly var artInfo = ref arts.Value.GetLand(tileID);

                                world.Entity()
                                    .Set(new Renderable() {
                                        Texture = artInfo.Texture,
                                        UV = artInfo.UV,
                                        Color = Vector3.UnitZ,
                                        Position = Isometric.IsoToScreen(tileX, tileY, z),
                                        Z = getDepthZ(tileX, tileY, z - 2)
                                    });
                            }
                        }
                    }

                    if (im.StaticAddress != 0)
                    {
                        var sb = (Assets.StaticsBlock*) im.StaticAddress;

                        if (sb != null)
                        {
                            for (int i = 0, count = (int) im.StaticCount; i < count; ++i, ++sb)
                            {
                                if (sb->Color != 0 && sb->Color != 0xFFFF)
                                {
                                    int pos = (sb->Y << 3) + sb->X;

                                    if (pos >= 64)
                                    {
                                        continue;
                                    }

                                    var staX = (ushort)(bx + sb->X);
                                    var staY = (ushort)(by + sb->Y);

                                    ref readonly var artInfo = ref arts.Value.GetArt(sb->Color);

                                    var priorityZ = sb->Z;

                                    if (tiledataLoader.Value.StaticData[sb->Color].IsBackground)
                                    {
                                        priorityZ -= 1;
                                    }

                                    if (tiledataLoader.Value.StaticData[sb->Color].Height != 0)
                                    {
                                        priorityZ += 1;
                                    }

                                    if (tiledataLoader.Value.StaticData[sb->Color].IsMultiMovable)
                                    {
                                        priorityZ += 1;
                                    }

                                    var posVec = Isometric.IsoToScreen(staX, staY, sb->Z);
                                    posVec.X -= (short)((artInfo.UV.Width >> 1) - 22);
                                    posVec.Y -= (short)(artInfo.UV.Height - 44);
                                    world.Entity()
                                        .Set(new Renderable() {
                                            Texture = artInfo.Texture,
                                            UV = artInfo.UV,
                                            Color = Renderer.ShaderHueTranslator.GetHueVector(sb->Hue),
                                            Position = posVec,
                                            Z = getDepthZ(staX, staY, priorityZ)
                                        });
                                }
                            }
                        }
                    }
                }
            }, Stages.BeforeUpdate)
            .RunIf((EventReader<OnNewChunkRequest> reader) => !reader.IsEmpty);

            scheduler.AddSystem((Res<NetClient> network, EventWriter<OnPacketRecv> packetWriter) => {
                var availableData = network.Value.CollectAvailableData();
                if (availableData.Count != 0)
                {
                    var sharedBuffer = ArrayPool<byte>.Shared.Rent(availableData.Count);
                    availableData.CopyTo(sharedBuffer);

                    packetWriter.Enqueue(new () { RentedBuffer = sharedBuffer, Length = availableData.Count });
                }

                network.Value.Flush();
            });

            scheduler.AddSystem((EventReader<OnPacketRecv> packetReader, Res<NetClient> network) => {
                foreach (var packet in packetReader.Read())
                {
                    var realBuffer = packet.RentedBuffer.AsSpan(0, packet.Length);
                    try
                    {
                        while (!realBuffer.IsEmpty)
                        {
                            var packetId = realBuffer[0];
                            var packetLen = PacketsTable.GetPacketLength(packetId);
                            var packetBuffer = realBuffer[1..];

                            if (packetLen == -1)
                            {
                                if (realBuffer.Length < 3)
                                    return;

                                packetLen = BinaryPrimitives.ReadInt16BigEndian(packetBuffer);
                                packetBuffer = packetBuffer[2.. (packetLen - 2)];
                            }

                            var reader = new StackDataReader(packetBuffer);

                            switch (packetId)
                            {
                                // server list
                                case 0xA8:
                                    {
                                        var flags = reader.ReadUInt8();
                                        var count = reader.ReadUInt16BE();

                                        for (var i = 0; i < count; ++i)
                                        {
                                            var index = reader.ReadUInt16BE();
                                            var name = reader.ReadASCII(32, true);
                                            var percFull = reader.ReadUInt8();
                                            var timeZone = reader.ReadUInt8();
                                            var address = reader.ReadUInt32BE();

                                            Console.WriteLine("server entry -> {0}", name);

                                            network.Value.Send_SelectServer((byte) index);
                                            break;
                                        }
                                    }
                                    break;

                                // characters list
                                case 0xA9:
                                    {
                                        var charactersCount = reader.ReadUInt8();
                                        var characterNames = new List<string>();
                                        for (var i = 0; i < charactersCount; ++i)
                                        {
                                            characterNames.Add(reader.ReadASCII(30).TrimEnd('\0'));
                                            reader.Skip(30);
                                        }

                                        var cityCount = reader.ReadUInt8();
                                        // bla bla

                                        var protocol = ClientFlags.CF_T2A |
                                            ClientFlags.CF_RE |
                                            ClientFlags.CF_TD |
                                            ClientFlags.CF_LBR |
                                            ClientFlags.CF_AOS |
                                            ClientFlags.CF_SE |
                                            ClientFlags.CF_SA |
                                            ClientFlags.CF_UO3D |
                                            ClientFlags.CF_RESERVED |
                                            ClientFlags.CF_3D;

                                        network.Value.Send_SelectCharacter(0, characterNames[0], network.Value.LocalIP, protocol);
                                    }
                                    break;

                                // server relay
                                case 0x8C:
                                    {
                                        var ip = reader.ReadUInt32LE();
                                        var port = reader.ReadUInt16BE();
                                        var seed = reader.ReadUInt32BE();

                                        network.Value.Disconnect();
                                        network.Value.Connect(new IPAddress(ip).ToString(), port);

                                        if (network.Value.IsConnected)
                                        {
                                            network.Value.EnableCompression();
                                            unsafe {
                                                Span<byte> b = [(byte)(seed >> 24), (byte)(seed >> 16), (byte)(seed >> 8), (byte)seed];
                                                network.Value.Send(b, true, true);
                                            }

                                            network.Value.Send_SecondLogin(Settings.GlobalSettings.Username, Crypter.Decrypt(Settings.GlobalSettings.Password), seed);
                                        }
                                    }
                                    break;

                                // locked features
                                case 0xB9:
                                // light level
                                case 0x4F:
                                default:
                                    break;
                            }

                            realBuffer = realBuffer[packetLen ..];
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(packet.RentedBuffer);
                    }
                }
            });
        }


        static bool ApplyStretch(
            int mapIndex,
            ushort texId, int x, int y, sbyte z,
            bool isStretched, out sbyte avgZ, out sbyte minZ,
            out Renderer.UltimaBatcher2D.YOffsets offsets,
            out Vector3 normalTop,
            out Vector3 normalRight,
            out Vector3 normalBottom,
            out Vector3 normalLeft)
        {
            if (isStretched || Assets.TexmapsLoader.Instance.GetValidRefEntry(texId).Length <= 0)
            {
                isStretched = false;
                avgZ = z;
                minZ = z;
                offsets = new Renderer.UltimaBatcher2D.YOffsets();
                normalTop = normalRight = normalBottom = normalLeft = Vector3.Zero;

                return false;
            }

            /*  _____ _____
             * | top | rig |
             * |_____|_____|
             * | lef | bot |
             * |_____|_____|
             */
            var zTop = z;
            var zRight = GetTileZ(mapIndex, x + 1, y);
            var zLeft = GetTileZ(mapIndex, x, y + 1);
            sbyte zBottom = GetTileZ(mapIndex, x + 1, y + 1);

            offsets.Top = zTop * 4;
            offsets.Right = zRight * 4;
            offsets.Left = zLeft * 4;
            offsets.Bottom = zBottom * 4;

            if (Math.Abs(zTop - zBottom) <= Math.Abs(zLeft - zRight))
            {
                avgZ = (sbyte) ((zTop + zBottom) >> 1);
            }
            else
            {
                avgZ = (sbyte) ((zLeft + zRight) >> 1);
            }

            minZ = Math.Min(zTop, Math.Min(zRight, Math.Min(zLeft, zBottom)));


            /*  _____ _____ _____ _____
             * |     | t10 | t20 |     |
             * |_____|_____|_____|_____|
             * | t01 |  z  | t21 | t31 |
             * |_____|_____|_____|_____|
             * | t02 | t12 | t22 | t32 |
             * |_____|_____|_____|_____|
             * |     | t13 | t23 |     |
             * |_____|_____|_____|_____|
             */
            var t10 = GetTileZ(mapIndex, x, y - 1);
            var t20 = GetTileZ(mapIndex, x + 1, y - 1);
            var t01 = GetTileZ(mapIndex, x - 1, y);
            var t21 = zRight;
            var t31 = GetTileZ(mapIndex, x + 2, y);
            var t02 = GetTileZ(mapIndex, x - 1, y + 1);
            var t12 = zLeft;
            var t22 = zBottom;
            var t32 = GetTileZ(mapIndex, x + 2, y + 1);
            var t13 = GetTileZ(mapIndex, x, y + 2);
            var t23 = GetTileZ(mapIndex, x + 1, y + 2);


            isStretched |= CalculateNormal(z, t10, t21, t12, t01, out normalTop);
            isStretched |= CalculateNormal(t21, t20, t31, t22, z, out normalRight);
            isStretched |= CalculateNormal(t22, t21, t32, t23, t12, out normalBottom);
            isStretched |= CalculateNormal(t12, z, t22, t13, t02, out normalLeft);

            return isStretched;
        }

        private unsafe static sbyte GetTileZ(int mapIndex, int x, int y)
        {
            static ref Assets.IndexMap GetIndex(int mapIndex, int x, int y)
            {
                var block = GetBlock(mapIndex, x, y);
                Assets.MapLoader.Instance.SanitizeMapIndex(ref mapIndex);
                var list = Assets.MapLoader.Instance.BlockData[mapIndex];

                return ref block >= list.Length ? ref Assets.IndexMap.Invalid : ref list[block];

                static int GetBlock(int mapIndex, int blockX, int blockY)
                    => blockX * Assets.MapLoader.Instance.MapBlocksSize[mapIndex, 1] + blockY;
            }


            if (x < 0 || y < 0)
            {
                return -125;
            }

            ref var blockIndex = ref GetIndex(mapIndex, x >> 3, y >> 3);

            if (blockIndex.MapAddress == 0)
            {
                return -125;
            }

            int mx = x % 8;
            int my = y % 8;

            unsafe
            {
                var mp = (Assets.MapBlock*) blockIndex.MapAddress;
                var cells = (Assets.MapCells*) &mp->Cells;

                return cells[(my << 3) + mx].Z;
            }
        }

        private static bool CalculateNormal(sbyte tile, sbyte top, sbyte right, sbyte bottom, sbyte left, out Vector3 normal)
        {
            if (tile == top && tile == right && tile == bottom && tile == left)
            {
                normal.X = 0;
                normal.Y = 0;
                normal.Z = 1f;

                return false;
            }

            Vector3 u = new Vector3();
            Vector3 v = new Vector3();
            Vector3 ret = new Vector3();


            // ==========================
            u.X = -22;
            u.Y = -22;
            u.Z = (left - tile) * 4;

            v.X = -22;
            v.Y = 22;
            v.Z = (bottom - tile) * 4;

            Vector3.Cross(ref v, ref u, out ret);
            // ==========================


            // ==========================
            u.X = -22;
            u.Y = 22;
            u.Z = (bottom - tile) * 4;

            v.X = 22;
            v.Y = 22;
            v.Z = (right - tile) * 4;

            Vector3.Cross(ref v, ref u, out normal);
            Vector3.Add(ref ret, ref normal, out ret);
            // ==========================


            // ==========================
            u.X = 22;
            u.Y = 22;
            u.Z = (right - tile) * 4;

            v.X = 22;
            v.Y = -22;
            v.Z = (top - tile) * 4;

            Vector3.Cross(ref v, ref u, out normal);
            Vector3.Add(ref ret, ref normal, out ret);
            // ==========================


            // ==========================
            u.X = 22;
            u.Y = -22;
            u.Z = (top - tile) * 4;

            v.X = -22;
            v.Y = -22;
            v.Z = (left - tile) * 4;

            Vector3.Cross(ref v, ref u, out normal);
            Vector3.Add(ref ret, ref normal, out ret);
            // ==========================


            Vector3.Normalize(ref ret, out normal);

            return true;
        }
    }

    struct FnaPlugin : IPlugin
    {
        public bool WindowResizable { get; set; }
        public bool MouseVisible { get; set; }
        public bool VSync { get; set; }



        public void Build(Scheduler scheduler)
        {
            var game = new UoGame(MouseVisible, WindowResizable, VSync);
            scheduler.AddResource(game);
            scheduler.AddResource(Keyboard.GetState());
            scheduler.AddResource(Mouse.GetState());
            scheduler.AddEvent<KeyEvent>();
            scheduler.AddEvent<MouseEvent>();
            scheduler.AddEvent<WheelEvent>();

            scheduler.AddSystem((Res<UoGame> game, SchedulerState schedState) => {
                game.Value.BeforeLoop();
                game.Value.RunOneFrame();
                schedState.AddResource(game.Value.GraphicsDevice);
                game.Value.RunApplication = true;
            }, Stages.Startup);

            scheduler.AddSystem((Res<UoGame> game) => {
                game.Value.SuppressDraw();
                game.Value.Tick();

                FrameworkDispatcher.Update();
            }).RunIf((SchedulerState state) => state.ResourceExists<UoGame>());

            scheduler.AddSystem((Res<UoGame> game) => {
                Environment.Exit(0);
            }, Stages.AfterUpdate).RunIf((Res<UoGame> game) => !game.Value.RunApplication);

            scheduler.AddSystem((
                Res<GraphicsDevice> device,
                Res<Renderer.UltimaBatcher2D> batch,
                Res<(ushort CenterX, ushort CenterY, Vector2 Offset)> centerWorldPos,
                Res<MouseState> oldMouseState,
                Query<Renderable, Not<TileStretched>> query,
                Query<(Renderable, TileStretched)> queryTiles
            ) => {
                device.Value.Clear(Color.AliceBlue);

                var center = Isometric.IsoToScreen(centerWorldPos.Value.CenterX, centerWorldPos.Value.CenterY, 0);
                center.X -= device.Value.PresentationParameters.BackBufferWidth / 2;
                center.Y -= device.Value.PresentationParameters.BackBufferHeight / 2;

                var newMouseState = Mouse.GetState();
                if (newMouseState.LeftButton == ButtonState.Pressed)
                {
                    centerWorldPos.Value.Offset.X += newMouseState.X - oldMouseState.Value.X;
                    centerWorldPos.Value.Offset.Y += newMouseState.Y - oldMouseState.Value.Y;
                }

                center -= centerWorldPos.Value.Offset;

                var sb = batch.Value;
                sb.Begin();
                sb.SetBrightlight(1.7f);
                sb.SetStencil(DepthStencilState.Default);
                queryTiles.Each((ref Renderable renderable, ref TileStretched stretched) =>
                    sb.DrawStretchedLand(
                        renderable.Texture,
                        renderable.Position - center,
                        renderable.UV,
                        ref stretched.Offset,
                        ref stretched.NormalTop,
                        ref stretched.NormalRight,
                        ref stretched.NormalLeft,
                        ref stretched.NormalBottom,
                        renderable.Color,
                        renderable.Z
                    )
                );
                query.Each((ref Renderable renderable) =>
                    sb.Draw
                    (
                        renderable.Texture,
                        renderable.Position - center,
                        renderable.UV,
                        renderable.Color,
                        0f,
                        Vector2.Zero,
                        1f,
                        SpriteEffects.None,
                        renderable.Z
                    )
                );
                sb.SetStencil(null);
                sb.End();
                device.Value.Present();
            }, Stages.AfterUpdate).RunIf((SchedulerState state) => state.ResourceExists<GraphicsDevice>());

            scheduler.AddSystem((EventWriter<KeyEvent> writer, Res<KeyboardState> oldState) => {
                var newState = Keyboard.GetState();

                foreach (var key in oldState.Value.GetPressedKeys())
                    if (newState.IsKeyUp(key)) // [pressed] -> [released]
                        writer.Enqueue(new KeyEvent() { Action = 0, Key = key });

                foreach (var key in newState.GetPressedKeys())
                    if (oldState.Value.IsKeyUp(key)) // [released] -> [pressed]
                        writer.Enqueue(new KeyEvent() { Action = 1, Key = key });
                    else if (oldState.Value.IsKeyDown(key))
                        writer.Enqueue(new KeyEvent() { Action = 2, Key = key });

                oldState.Value = newState;
            }, Stages.AfterUpdate);

            scheduler.AddSystem((EventWriter<MouseEvent> writer, EventWriter<WheelEvent> wheelWriter, Res<MouseState> oldState) => {
                var newState = Mouse.GetState();

                if (newState.LeftButton != oldState.Value.LeftButton)
                    writer.Enqueue(new MouseEvent() { Action = newState.LeftButton, Button = Input.MouseButtonType.Left, X = newState.X, Y = newState.Y });
                if (newState.RightButton != oldState.Value.RightButton)
                    writer.Enqueue(new MouseEvent() { Action = newState.RightButton, Button = Input.MouseButtonType.Right, X = newState.X, Y = newState.Y });
                if (newState.MiddleButton != oldState.Value.MiddleButton)
                    writer.Enqueue(new MouseEvent() { Action = newState.MiddleButton, Button = Input.MouseButtonType.Middle, X = newState.X, Y = newState.Y });
                if (newState.XButton1 != oldState.Value.XButton1)
                    writer.Enqueue(new MouseEvent() { Action = newState.XButton1, Button = Input.MouseButtonType.XButton1, X = newState.X, Y = newState.Y });
                if (newState.XButton2 != oldState.Value.XButton2)
                    writer.Enqueue(new MouseEvent() { Action = newState.XButton2, Button = Input.MouseButtonType.XButton2, X = newState.X, Y = newState.Y });

                if (newState.ScrollWheelValue != oldState.Value.ScrollWheelValue)
                    // FNA multiplies for 120 for some reason
                    wheelWriter.Enqueue(new WheelEvent() { Value = (oldState.Value.ScrollWheelValue - newState.ScrollWheelValue) / 120 });

                oldState.Value = newState;
            }, Stages.AfterUpdate);

            scheduler.AddSystem((EventReader<KeyEvent> reader) => {
                foreach (var ev in reader.Read())
                    Console.WriteLine("key {0} is {1}", ev.Key, ev.Action switch {
                        0 => "up",
                        1 => "down",
                        2 => "pressed",
                        _ => "unkown"
                    });
            });

            scheduler.AddSystem((EventReader<MouseEvent> reader) => {
                foreach (var ev in reader.Read())
                    Console.WriteLine("mouse button {0} is {1} at {2},{3}", ev.Button, ev.Action switch {
                        ButtonState.Pressed => "pressed",
                        ButtonState.Released => "released",
                        _ => "unknown"
                    }, ev.X, ev.Y);
            }).RunIf((Res<UoGame> game) => game.Value.IsActive);

            scheduler.AddSystem((EventReader<WheelEvent> reader) => {
                foreach (var ev in reader.Read())
                    Console.WriteLine("wheel value {0}", ev.Value);
            }).RunIf((Res<UoGame> game) => game.Value.IsActive);
        }

        struct KeyEvent
        {
            public byte Action;
            public Keys Key;
        }

        struct MouseEvent
        {
            public ButtonState Action;
            public Input.MouseButtonType Button;
            public int X, Y;
        }

        struct WheelEvent
        {
            public int Value;
        }

        sealed class UoGame : Microsoft.Xna.Framework.Game
        {
            public UoGame(bool mouseVisible, bool allowWindowResizing, bool vSync)
            {
                GraphicManager = new GraphicsDeviceManager(this)
                {
                    SynchronizeWithVerticalRetrace = vSync
                };
                IsFixedTimeStep = false;
                IsMouseVisible = mouseVisible;
                Window.AllowUserResizing = allowWindowResizing;
            }

            public GraphicsDeviceManager GraphicManager { get; }


            protected override void Initialize()
            {
                base.Initialize();
            }

            protected override void LoadContent()
            {
                base.LoadContent();
            }

            protected override void Update(GameTime gameTime)
            {
                // I don't want to update things here, but on ecs systems instead
            }

            protected override void Draw(GameTime gameTime)
            {
                // I don't want to render things here, but on ecs systems instead
            }
        }
    }
}