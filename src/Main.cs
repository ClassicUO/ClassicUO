using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Network;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

using SDL2;

namespace ClassicUO
{
    static class Bootstrap
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetDllDirectory(string lpPathName);


        static void Main(string[] args)
        {
            // - check for update
            // - launcher & user setup
            // - game setup 
            // - game launch
            // - enjoy

            Log.Start(LogTypes.All);

            CUOEnviroment.GameThread = Thread.CurrentThread;
            CUOEnviroment.GameThread.Name = "CUO_MAIN_THREAD";


#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                StringBuilder sb = new StringBuilder();
#if DEV_BUILD
                sb.AppendFormat("ClassicUO [dev] - v{0}\nOS: {1} {2}\nThread: {3}\n\n", CUOEnviroment.Version, Environment.OSVersion.Platform, Environment.Is64BitOperatingSystem ? "x64" : "x86", Thread.CurrentThread.Name);
#else
                sb.AppendFormat("ClassicUO - v{0}\nOS: {1} {2}\nThread: {3}\n\n", CUOEnviroment.Version, Environment.OSVersion.Platform, Environment.Is64BitOperatingSystem ? "x64" : "x86", Thread.CurrentThread.Name);
#endif
                sb.AppendFormat("Exception:\n{0}", e.ExceptionObject);

                Log.Panic(e.ExceptionObject.ToString());
                string path = Path.Combine(CUOEnviroment.ExecutablePath, "Logs");

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                using (LogFile crashfile = new LogFile(path, "crash.txt"))
                {
                    crashfile.WriteAsync(sb.ToString()).RunSynchronously();

                    //SDL.SDL_ShowSimpleMessageBox(
                    //                             SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_INFORMATION,
                    //                             "An error occurred",
                    //                             $"{crashfile}\ncreated in /Logs.",
                    //                             SDL.SDL_GL_GetCurrentWindow()
                    //                            );
                }
            };
#endif

#if DEV_BUILD
            Updater updater = new Updater();
            if (updater.Check())
                return;
#endif
            ReadSettingsFromArgs(args);

            if (!SkipUpdate)
                if (CheckUpdate(args))
                    return;

            //Environment.SetEnvironmentVariable("FNA_GRAPHICS_FORCE_GLDEVICE", "ModernGLDevice");
            Environment.SetEnvironmentVariable("FNA_GRAPHICS_ENABLE_HIGHDPI",  CUOEnviroment.IsHighDPI ? "1" : "0");
            Environment.SetEnvironmentVariable("FNA_OPENGL_BACKBUFFER_SCALE_NEAREST", "1");
            Environment.SetEnvironmentVariable("FNA_OPENGL_FORCE_COMPATIBILITY_PROFILE", "1");
            Environment.SetEnvironmentVariable(SDL.SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");
            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Plugins"));

          
            string globalSettingsPath = Settings.GetSettingsFilepath();

            if ((!Directory.Exists(Path.GetDirectoryName(globalSettingsPath)) ||
                                                       !File.Exists(globalSettingsPath)))
            {
                // settings specified in path does not exists, make new one
                {
                    // TODO: 
                    Settings.GlobalSettings.Save();
                    return;
                }
            }

            Settings.GlobalSettings = ConfigurationResolver.Load<Settings>(globalSettingsPath);

            ReadSettingsFromArgs(args);

            // still invalid, cannot load settings
            if (Settings.GlobalSettings == null || !Settings.GlobalSettings.IsValid())
            {
                // TODO: 
                Settings.GlobalSettings?.Save();
                return;
            }

            if (!CUOEnviroment.IsUnix)
            {
                string libsPath = Path.Combine(CUOEnviroment.ExecutablePath, "libs", Environment.Is64BitProcess ? "x64" : "x86");
                SetDllDirectory(libsPath);
            }


            Client.Run();

            Log.Trace("Closing...");
        }

        public static bool StartMinimized { get; set; }
        public static bool StartInLittleWindow { get; set; }
        public static bool SkipUpdate { get; set; }

        private static void ReadSettingsFromArgs(string[] args)
        {
            for (int i = 0; i <= args.Length - 1; i++)
            {
                string cmd = args[i].ToLower();

                // NOTE: Command-line option name should start with "-" character
                if (cmd.Length == 0 || cmd[0] != '-')
                    continue;

                cmd = cmd.Remove(0, 1);
                string value = (i < args.Length - 1) ? args[i + 1] : null;

                Log.Trace( $"ARG: {cmd}, VALUE: {value}");

                switch (cmd)
                {
                    // Here we have it! Using `-settings` option we can now set the filepath that will be used 
                    // to load and save ClassicUO main settings instead of default `./settings.json`
                    // NOTE: All individual settings like `username`, `password`, etc passed in command-line options
                    // will override and overwrite those in the settings file because they have higher priority
                    case "settings":
                        Settings.CustomSettingsFilepath = value;
                        break;

                    case "minimized":
                        StartMinimized = true;
                        break;

                    case "littlewindow":
                        StartInLittleWindow = true;
                        break;

                    case "skipupdate":
                        SkipUpdate = true;
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

                    case "ultimaonlinedirectory":
                    case "uopath":
                        Settings.GlobalSettings.UltimaOnlineDirectory = value; // Required

                        break;

                    case "clientversion":
                        Settings.GlobalSettings.ClientVersion = value; // Required

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
                            v = Constants.MIN_FPS;
                        else if (v > Constants.MAX_FPS)
                            v = Constants.MAX_FPS;

                        Settings.GlobalSettings.FPS = v;
                        
                        break;

                    case "debug":
                        CUOEnviroment.Debug = Settings.GlobalSettings.Debug = bool.Parse(value);
                        
                        break;

                    case "profiler":
                        Settings.GlobalSettings.Profiler = bool.Parse(value);

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
                        Settings.GlobalSettings.ReconnectTime = int.Parse(value);

                        break;

                    case "login_music":
                    case "music":
                        Settings.GlobalSettings.LoginMusic = bool.Parse(value);

                        break;

                    case "login_music_volume":
                    case "music_volume":
                        Settings.GlobalSettings.LoginMusicVolume = int.Parse(value);

                        break;

                    case "shard_type":
                    case "shard":
                        Settings.GlobalSettings.ShardType = int.Parse(value);

                        break;

                    case "fixed_time_step":
                        Settings.GlobalSettings.FixedTimeStep = bool.Parse(value);

                        break;

                    case "skiploginscreen":
                        CUOEnviroment.SkipLoginScreen = true;
                        break;

                    case "plugins":
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            Settings.GlobalSettings.Plugins = value.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
                        }
                        break;

                }
            }
        }

        private static bool CheckUpdate(string[] args)
        {
            string currentPath = CUOEnviroment.ExecutablePath;

            string path = string.Empty;
            string action = string.Empty;
            int pid = -1;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--source" && i < args.Length - 1)
                    path = args[i + 1];
                else if (args[i] == "--action" && i < args.Length - 1)
                    action = args[i + 1];
                else if (args[i] == "--pid" && i < args.Length - 1)
                    pid = int.Parse(args[i + 1]);
            }

            if (action != string.Empty)
            {
                Console.WriteLine("[CheckUpdate] CURRENT PATH: {0}", currentPath);
                Console.WriteLine("[CheckUpdate] Args: \tpath={0}\taction={1}\tpid={2}", path, action, pid);
            }

            if (action == "update")
            {
                Log.Trace( "ClassicUO Updating...", ConsoleColor.Yellow);

                try
                {
                    Process proc = Process.GetProcessById(pid);
                    proc.Kill();
                    proc.WaitForExit(5000);
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

                string prefix = Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix ? "mono " : string.Empty;

                new Process
                {
                    StartInfo =
                    {
                        WorkingDirectory = path,
                        FileName = prefix + Path.Combine(path, "ClassicUO.exe"),
                        UseShellExecute = false,
                        Arguments =
                            $"--source \"{currentPath}\" --pid {Process.GetCurrentProcess().Id} --action cleanup"
                    }
                }.Start();

                return true;
            }

            if (action == "cleanup")
            {
                try
                {
                    Process.GetProcessById(pid);
                    Thread.Sleep(1000);
                    Process.GetProcessById(pid).Kill();
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

                Log.Trace( "ClassicUO updated successfully!", ConsoleColor.Green);
            }

            return false;
        }
    }
}
