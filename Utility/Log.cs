#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClassicUO.Utility
{
    public enum LogTypes
    {
        None,
        Trace,
        Info,
        Warning,
        Error
    }

    public class Log
    {
        private static readonly Dictionary<LogTypes, string> LogMsgFormat = new Dictionary<LogTypes, string>
        {
            {LogTypes.None, string.Empty}, {LogTypes.Trace, "  Trace   "}, {LogTypes.Info, "  Info    "},
            {LogTypes.Warning, "  Warning "}, {LogTypes.Error, "  Error   "}
        };

        private static readonly Dictionary<LogTypes, ConsoleColor> LogMsgColor = new Dictionary<LogTypes, ConsoleColor>
        {
            {LogTypes.None, ConsoleColor.White}, {LogTypes.Trace, ConsoleColor.Green},
            {LogTypes.Info, ConsoleColor.Cyan}, {LogTypes.Warning, ConsoleColor.Yellow},
            {LogTypes.Error, ConsoleColor.Red}
        };

        private static readonly BlockingCollection<Tuple<LogTypes, string, string, bool>> _loqQueue = new BlockingCollection<Tuple<LogTypes, string, string, bool>>();

        public static bool IsLogging { get; private set; }

        //public Log(string file)
        //{
        //    _file = new LogFile();
        //}

        private LogFile _file;

        static Log()
        {
            Thread logThread = new Thread(async () =>
            {
                using (_loqQueue)
                {
                    IsLogging = true;

                    while (IsLogging)
                    {
                        Thread.Sleep(1);

                        if (!_loqQueue.TryTake(out Tuple<LogTypes, string, string, bool> log))
                            continue;

                        LogTypes type = log.Item1;
                        string time = log.Item2;
                        string text = log.Item3;
                        bool newline = log.Item4;

                        if (type != LogTypes.None)
                            Console.Write(time);

                        ConsoleColor prev = Console.ForegroundColor;
                        Console.ForegroundColor = LogMsgColor[type];
                        Console.Write(LogMsgFormat[type]);
                        Console.ForegroundColor = prev;

                        if (newline)
                            Console.WriteLine(type == LogTypes.None ? text : " |  " + text);
                        else
                            Console.Write(type == LogTypes.None ? text : " |  " + text);
                    }
                }

            }){ IsBackground = true};

            logThread.Start();
            IsLogging = logThread.ThreadState == ThreadState.Running || logThread.ThreadState == ThreadState.Background;
        }

        public static void Message(LogTypes type, string msg, bool newline = true)
        {
            _loqQueue.Add(Tuple.Create(type, DateTime.Now.ToString("HH:mm:ss") + " | ", msg, newline));
        }
    }


    public sealed class LogFile : IDisposable
    {
        private readonly FileStream logStream;

        public LogFile(string directory, string file)
        {
            logStream = new FileStream($"{directory}/{DateTime.Now:yyyy-MM-dd_hh-mm-ss}_{file}", FileMode.Append, FileAccess.Write, FileShare.ReadWrite, 4096, true);
        }

        public void Dispose()
        {
            logStream.Close();
        }

        public async Task WriteAsync(string logMessage)
        {
            var logBytes = Encoding.UTF8.GetBytes($"{logMessage}\n");

            await logStream.WriteAsync(logBytes, 0, logBytes.Length);
            await logStream.FlushAsync();
        }
    }
}