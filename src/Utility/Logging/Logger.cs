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
using System.Threading;

namespace ClassicUO.Utility.Logging
{
    internal class Logger
    {
        private int _indent;

        public static readonly Dictionary<LogTypes, Tuple<ConsoleColor, string>> LogTypeInfo = new Dictionary<LogTypes, Tuple<ConsoleColor, string>>
        {
            {
                LogTypes.None, Tuple.Create(ConsoleColor.White, "")
            },
            {
                LogTypes.Info, Tuple.Create(ConsoleColor.Green, "  Info    ")
            },
            {
                LogTypes.Debug, Tuple.Create(ConsoleColor.DarkGreen, "  Debug   ")
            },
            {
                LogTypes.Trace, Tuple.Create(ConsoleColor.Green, "  Trace   ")
            },
            {
                LogTypes.Warning, Tuple.Create(ConsoleColor.Yellow, "  Warning ")
            },
            {
                LogTypes.Error, Tuple.Create(ConsoleColor.Red, "  Error   ")
            },
            {
                LogTypes.Panic, Tuple.Create(ConsoleColor.Red, "  Panic   ")
            }
        };
        private bool _isLogging;

        // No volatile support for properties, let's use a private backing field.
        public LogTypes LogTypes { get; set; }

        public void Start(LogFile logFile = null)
        {
            _isLogging = true;
        }

        public void Stop()
        {
            _isLogging = false;
        }

        public void Message(LogTypes logType, string text)
        {
            SetLogger(logType, text);
        }

        public void NewLine()
        {
            SetLogger(LogTypes.None, string.Empty);
        }

        public void Clear()
        {
            Console.Clear();
        }

        public void PushIndent()
        {
            _indent++;
        }

        public void PopIndent()
        {
            _indent--;

            if (_indent < 0)
                _indent = 0;
        }

        private void SetLogger(LogTypes type, string text)
        {
            if (!_isLogging)
                return;

            if ((LogTypes & type) == type)
            {
                string time = type == LogTypes.None ? string.Empty : DateTime.Now.ToString("T");


                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"{time} |");
                Console.ForegroundColor = LogTypeInfo[type].Item1;
                Console.Write(LogTypeInfo[type].Item2);
                Console.ForegroundColor = ConsoleColor.White;

                if (_indent > 0)
                {
                    Console.Write("| ");
                    Console.Write(new string('\t', _indent * 2));
                    Console.WriteLine(text);
                }
                else
                    Console.WriteLine($"| {text}");
            }
        }
    }
}