using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            updater.Check();
#endif

            if (CheckUpdate(args))
                return;
            ParseAdditionalArgs(args);

            Engine.Run(args);
        }

        public static bool StartMinimized { get; set; }
        public static bool StartInLittleWindow { get; set; }


        private static void ParseAdditionalArgs(string[] args)
        {
            int count = args.Length - 1;

            for (int i = 0; i <= count; i++)
            {
                switch (args[i])
                {
                    case "-minimized":
                        StartMinimized = true;
                        break;
                    case "-littlewindow":
                        StartInLittleWindow = true;
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

            Console.WriteLine("CURRENT PATH: {0}", currentPath);
            Console.WriteLine("Args: \tpath={0}\taction={1}\tpid={2}", path, action, pid);

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
                catch (Exception e)
                {
                }

                Log.Message(LogTypes.Trace, "ClassicUO updated successful!", ConsoleColor.Green);
            }

            return false;
        }
    }
}
