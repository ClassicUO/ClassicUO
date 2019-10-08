using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ClassicUO.Configuration;
using ClassicUO.Network;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO
{
    static class Bootstrap
    {
        static void Main(string[] args)
        {
            Engine.Configure();

#if DEV_BUILD
            Updater updater = new Updater();
            if (updater.Check())
                return;
#endif
            ParseMainArgs(args);

            if (!SkipUpdate)
                if (CheckUpdate(args))
                    return;

            Engine.Run(args);
        }

        public static bool StartMinimized { get; set; }
        public static bool StartInLittleWindow { get; set; }
        public static bool SkipUpdate { get; set; }

        private static void ParseMainArgs(string[] args)
        {
            for (int i = 0; i <= args.Length - 1; i++)
            {
                string cmd = args[i].ToLower();

                // NOTE: Command-line option name should start with "-" character
                if (cmd.Length == 0 || cmd[0] != '-')
                    continue;

                cmd = cmd.Remove(0, 1);
                string value = (i < args.Length - 1) ? args[i + 1] : null;

                Log.Message(LogTypes.Trace, $"ARG: {cmd}, VALUE: {value}");

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
                }
            }
        }

        private static bool CheckUpdate(string[] args)
        {
            string currentPath = Engine.ExePath;

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
                Log.Message(LogTypes.Trace, "ClassicUO Updating...", ConsoleColor.Yellow);

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

                Log.Message(LogTypes.Trace, "ClassicUO updated successfully!", ConsoleColor.Green);
            }

            return false;
        }
    }
}
