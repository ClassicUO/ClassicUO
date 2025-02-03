﻿#region license

// BSD 2-Clause License
//
// Copyright (c) 2025, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//
// - Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer.
//
// - Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;

namespace ClassicUO.Utility.Logging
{
    public class Logger
    {
        private static readonly Dictionary<LogTypes, Tuple<ConsoleColor, string>> _logTypesInfo = new Dictionary<LogTypes, Tuple<ConsoleColor, string>>
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

        private int _indent;

        private bool _isLogging;
        private readonly object _syncObject = new object();

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
            lock (_syncObject)
            {
                SetLogger(logType, text);
            }
        }

        public void NewLine()
        {
            lock (_syncObject)
            {
                SetLogger(LogTypes.None, string.Empty);
            }
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
            {
                _indent = 0;
            }
        }

        private void SetLogger(LogTypes type, string text)
        {
            if (!_isLogging)
            {
                return;
            }

            if ((LogTypes & type) == type)
            {
                if (type == LogTypes.None)
                {
                    if (_indent > 0)
                    {
                        Console.Write(new string('\t', _indent * 2));
                    }

                    Console.WriteLine(text);
                }
                else
                {
                    Console.Write(DateTime.UtcNow);
                    Console.Write(" | ");
                    ConsoleColor temp = Console.ForegroundColor;

                    Console.ForegroundColor = _logTypesInfo[type].Item1;
                    Console.Write(_logTypesInfo[type].Item2);
                    Console.ForegroundColor = temp;
                    Console.Write(" | ");

                    if (_indent > 0)
                    {
                        Console.Write(new string('\t', _indent * 2));
                    }

                    Console.WriteLine(text);
                }
            }
        }
    }
}