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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using ClassicUO.Configuration;
using ClassicUO.Data;
using ClassicUO.Game;
using ClassicUO.IO;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using ClassicUO.Utility.Platforms;
using SDL2;

namespace ClassicUO
{
    internal static class Bootstrap
    {
        private static bool _skipUpdates;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetDllDirectory(string lpPathName);

        [STAThread]
        public static void Main(string[] args)
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

#if DEV_BUILD
            if (!_skipUpdates)
            {
                Network.Updater updater = new Network.Updater();
                if (updater.Check())
                    return;
            }
#endif

            if (!_skipUpdates)
            {
                if (CheckUpdate(args))
                {
                    return;
                }
            }

            if (CUOEnviroment.IsHighDPI)
            {
                Environment.SetEnvironmentVariable("FNA_GRAPHICS_ENABLE_HIGHDPI", "1");
            }

            Environment.SetEnvironmentVariable("FNA3D_BACKBUFFER_SCALE_NEAREST", "1");
            Environment.SetEnvironmentVariable("FNA3D_OPENGL_FORCE_COMPATIBILITY_PROFILE", "1");
            Environment.SetEnvironmentVariable(SDL.SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");

            Environment.SetEnvironmentVariable
            (
                "PATH",
                Environment.GetEnvironmentVariable("PATH") + ";" + Path.Combine
                    (CUOEnviroment.ExecutablePath, "Data", "Plugins")
            );

            string globalSettingsPath = Settings.GetSettingsFilepath();

            if (!Directory.Exists(Path.GetDirectoryName(globalSettingsPath)) || !File.Exists(globalSettingsPath))
            {
                // settings specified in path does not exists, make new one
                {
                    // TODO: 
                    Settings.GlobalSettings.Save();
                }
            }

            Settings.GlobalSettings = ConfigurationResolver.Load<Settings>(globalSettingsPath);
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
                string libsPath = Path.Combine
                    (CUOEnviroment.ExecutablePath, Environment.Is64BitProcess ? "x64" : "x86");

                SetDllDirectory(libsPath);
            }


            if (string.IsNullOrWhiteSpace(Settings.GlobalSettings.UltimaOnlineDirectory))
            {
                Settings.GlobalSettings.UltimaOnlineDirectory = CUOEnviroment.ExecutablePath;
            }

            const uint INVALID_UO_DIRECTORY = 0x100;
            const uint INVALID_UO_VERSION = 0x200;

            uint flags = 0;

            if (!Directory.Exists(Settings.GlobalSettings.UltimaOnlineDirectory) ||
                !File.Exists(UOFileManager.GetUOFilePath("tiledata.mul")))
            {
                flags |= INVALID_UO_DIRECTORY;
            }

            string clientVersionText = Settings.GlobalSettings.ClientVersion;

            if (!ClientVersionHelper.IsClientVersionValid
                (Settings.GlobalSettings.ClientVersion, out ClientVersion clientVersion))
            {
                Log.Warn($"Client version [{clientVersionText}] is invalid, let's try to read the client.exe");

                // mmm something bad happened, try to load from client.exe [windows only]
                if (!ClientVersionHelper.TryParseFromFile
                (
                    Path.Combine(Settings.GlobalSettings.UltimaOnlineDirectory, "client.exe"), out clientVersionText
                ) || !ClientVersionHelper.IsClientVersionValid(clientVersionText, out clientVersion))
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

                Client.Run();
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

                    case "skipupdate":
                        _skipUpdates = true;

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

                    case "password_enc"
                        : // Non-standard setting, similar to `password` but for already encrypted password
                        Settings.GlobalSettings.Password = value;

                        break;

                    case "ip":
                        Settings.GlobalSettings.IP = value;

                        break;

                    case "port":
                        Settings.GlobalSettings.Port = ushort.Parse(value);

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
                        Settings.GlobalSettings.LastCharacterName = value;

                        break;

                    case "lastservernum":
                        Settings.GlobalSettings.LastServerNum = ushort.Parse(value);

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
						// ## BEGIN - END ## //
					    //CUOEnviroment.Debug = true;
						// ## BEGIN - END ## //
                        CUOEnviroment.Debug = bool.Parse(value); 
						// ## BEGIN - END ## //

                        break;

                    case "profiler":
                        CUOEnviroment.Profiler = bool.Parse(value);

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
                        Settings.GlobalSettings.Plugins = string.IsNullOrEmpty
                            (value) ?
                            new string[0] :
                            value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

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
                        
                        CUOEnviroment.PacketLog = true;

                        break;
                }
            }
        }

        private static bool CheckUpdate(string[] args)
        {
            string currentPath = CUOEnviroment.ExecutablePath;

            string path = string.Empty;
            string action = string.Empty;
            int processId = -1;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--source" && i < args.Length - 1)
                {
                    path = args[i + 1];
                }
                else if (args[i] == "--action" && i < args.Length - 1)
                {
                    action = args[i + 1];
                }
                else if (args[i] == "--pid" && i < args.Length - 1)
                {
                    processId = int.Parse(args[i + 1]);
                }
            }

            if (action != string.Empty)
            {
                Console.WriteLine("[CheckUpdate] CURRENT PATH: {0}", currentPath);
                Console.WriteLine("[CheckUpdate] Args: \tpath={0}\taction={1}\tpid={2}", path, action, processId);
            }

            if (action == "update")
            {
                Log.Trace("ClassicUO Updating...");

                try
                {
                    Process processToBeKilled = Process.GetProcessById(processId);
                    processToBeKilled.Kill();
                    processToBeKilled.WaitForExit(5000);
                }
                catch
                {
                }

                //File.SetAttributes(Path.GetDirectoryName(path), FileAttributes.Normal);

                //foreach (string file in Directory.EnumerateFiles(currentPath, "*", SearchOption.AllDirectories))
                //{
                //    string sub = Path.Combine(file, file.Replace(currentPath, path));
                //    Console.WriteLine("COPIED {0} over {1}", file, sub);
                //    File.Copy(file, sub, true);
                //}

                DirectoryInfo dd = new DirectoryInfo(currentPath);
                dd.CopyAllTo(new DirectoryInfo(path));

                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = path,
                    UseShellExecute = false
                };

                if (CUOEnviroment.IsUnix)
                {
                    processStartInfo.FileName = "mono";

                    processStartInfo.Arguments =
                        $"\"{Path.Combine(path, "ClassicUO.exe")}\" --source \"{currentPath}\" --pid {Process.GetCurrentProcess().Id} --action cleanup";
                }
                else
                {
                    processStartInfo.FileName = Path.Combine(path, "ClassicUO.exe");

                    processStartInfo.Arguments =
                        $"--source \"{currentPath}\" --pid {Process.GetCurrentProcess().Id} --action cleanup";
                }

                Process.Start(processStartInfo);

                return true;
            }

            if (action == "cleanup")
            {
                try
                {
                    Process.GetProcessById(processId);
                    Thread.Sleep(1000);

                    Process.GetProcessById(processId).Kill();
                }
                catch
                {
                }

                try
                {
                    Directory.Delete(path, true);
                }
                catch (Exception)
                {
                }

                Log.Trace("ClassicUO updated successfully!");
            }

            return false;
        }
    }
}