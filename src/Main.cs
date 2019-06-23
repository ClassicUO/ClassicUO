﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ClassicUO.Configuration;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO
{
    static class Bootstrap
    {
        static void Main(string[] args)
        {
            Engine.Configure();

            if (CheckUpdate(args))
                return;

            Engine.Run(args);
        }


        private static bool CheckUpdate(string[] args)
        {
            string currentPath = Environment.CurrentDirectory;

            string path = string.Empty;
            string action = string.Empty;
            int pid = -1;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--source" && i < args.Length - 1)
                    path = args[i + 1];
                else if (args[i] == "--action" && i < args.Length - 1)
                    action = args[i + 1];
                else if (args[i] == "--pid" && i < args.Length - 1) pid = int.Parse(args[i + 1]);
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

                foreach (string file in Directory.EnumerateFiles(currentPath, "*", SearchOption.AllDirectories))
                {
                    string sub = Path.Combine(file, file.Replace(currentPath, path));
                    File.Copy(file, sub, true);
                    Console.WriteLine("COPIED {0} over {1}", file, sub);
                }

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
