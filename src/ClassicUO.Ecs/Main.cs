// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Resources;
using ClassicUO.Ecs.Logging;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using ClassicUO.Utility.Platforms;
using Microsoft.Extensions.Logging;
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
using TinyEcs.Bevy;

namespace ClassicUO
{
    internal static class Bootstrap
    {
        [STAThread]
        public static void Main(string[] args) => Boot(args);


        public static void Boot(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            var logger = CuoLog.Create();
            PacketLogger.Default.Logger = logger;

            DllMap.Init(logger);

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

                logger.LogCritical("unhandled exception: {Exception}", e.ExceptionObject);
                string path = Path.Combine(CUOEnviroment.ExecutablePath, "Logs");

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                using (LogFile crashfile = new LogFile(path, "crash.txt"))
                {
                    crashfile.WriteAsync(sb.ToString()).RunSynchronously();
                }
            };
#endif
            ReadSettingsFromArgs(args, logger);

            if (CUOEnviroment.IsHighDPI)
            {
                Environment.SetEnvironmentVariable("FNA_GRAPHICS_ENABLE_HIGHDPI", "1");
            }

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
                    Settings.GlobalSettings.Save(logger);
                }
            }

            Settings.GlobalSettings = ConfigurationResolver.Load(globalSettingsPath, SettingsJsonContext.RealDefault.Settings, logger);

            ReadSettingsFromArgs(args, logger);

            // still invalid, cannot load settings
            if (Settings.GlobalSettings == null)
            {
                Settings.GlobalSettings = new Settings();
                Settings.GlobalSettings.Save(logger);
            }

            if (string.IsNullOrWhiteSpace(Settings.GlobalSettings.Language))
            {
                logger.LogTrace("language is not set. Trying to get the OS language.");
                try
                {
                    Settings.GlobalSettings.Language = CultureInfo.InstalledUICulture.ThreeLetterWindowsLanguageName;

                    if (string.IsNullOrWhiteSpace(Settings.GlobalSettings.Language))
                    {
                        logger.LogWarning("cannot read the OS language. Rolled back to ENU");

                        Settings.GlobalSettings.Language = "ENU";
                    }

                    logger.LogTrace("language set: '{Language}'", Settings.GlobalSettings.Language);
                }
                catch
                {
                    logger.LogWarning("cannot read the OS language. Rolled back to ENU");

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
                logger.LogWarning("Client version [{Version}] is invalid, let's try to read the client.exe", clientVersionText);

                // mmm something bad happened, try to load from client.exe [windows only]
                if (!ClientVersionHelper.TryParseFromFile(Path.Combine(Settings.GlobalSettings.UltimaOnlineDirectory, "client.exe"), out clientVersionText) || !ClientVersionHelper.IsClientVersionValid(clientVersionText, out clientVersion))
                {
                    logger.LogError("Invalid client version: {Version}", clientVersionText);

                    flags |= INVALID_UO_VERSION;
                }
                else
                {
                    logger.LogTrace($"Found a valid client.exe [{clientVersionText} - {clientVersion}]");

                    // update the wrong/missing client version in settings.json
                    Settings.GlobalSettings.ClientVersion = clientVersionText;
                }
            }

            if (flags != 0)
            {
                if ((flags & INVALID_UO_DIRECTORY) != 0)
                {
                    logger.LogError(ResGeneral.YourUODirectoryIsInvalid);
                }
                else if ((flags & INVALID_UO_VERSION) != 0)
                {
                    logger.LogError(ResGeneral.YourUOClientVersionIsInvalid);
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

                logger.LogTrace("FNA3D_FORCE_DRIVER: {Value}", Environment.GetEnvironmentVariable("FNA3D_FORCE_DRIVER"));

                using var ecs = new TinyEcs.World();
                var app = new App(ecs, ThreadingMode.Auto);

                app.AddResource<ILogger>(logger);
                app.AddPlugin<Ecs.CuoPlugin>();

                while (true)
                    app.Update();

                // scheduler.Run(() => false);

                // Client.Run(pluginHost);
            }

            logger.LogTrace("Closing...");
        }

        private static void ReadSettingsFromArgs(string[] args, ILogger logger)
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

                logger.LogTrace("ARG: {Cmd}, VALUE: {Value}", cmd, value);

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
