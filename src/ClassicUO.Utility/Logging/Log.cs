#region license

// Copyright (c) 2021, andreakarasho
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

using System.Diagnostics;

namespace ClassicUO.Utility.Logging
{
    public class Log
    {
        private static Logger _logger;

        public static void Start(LogTypes logTypes, LogFile logFile = null)
        {
            _logger = _logger ?? new Logger
            {
                LogTypes = logTypes
            };

            _logger.Start(logFile);
        }

        public static void Stop()
        {
            _logger?.Stop();
            _logger = null;
        }

        public static void Resume(LogTypes logTypes)
        {
            if (_logger != null)
                _logger.LogTypes = logTypes;
        }

        public static void Pause()
        {
            if (_logger != null)
                _logger.LogTypes = LogTypes.None;
        }

        [Conditional("DEBUG")]
        public static void Debug(string text)
        {
            _logger?.Message(LogTypes.Debug, text);
        }

        public static void Info(string text)
        {
            _logger?.Message(LogTypes.Info, text);
        }

        public static void Trace(string text)
        {
            _logger?.Message(LogTypes.Trace, text);
        }

        public static void Warn(string text)
        {
            _logger?.Message(LogTypes.Warning, text);
        }

        public static void Error(string text)
        {
            _logger?.Message(LogTypes.Error, text);
        }

        public static void Panic(string text)
        {
            _logger?.Message(LogTypes.Error, text);
        }

        public static void NewLine()
        {
            _logger?.NewLine();
        }

        public static void Clear()
        {
            _logger?.Clear();
        }

        public static void PushIndent()
        {
            _logger?.PushIndent();
        }

        public static void PopIndent()
        {
            _logger?.PopIndent();
        }
    }
}