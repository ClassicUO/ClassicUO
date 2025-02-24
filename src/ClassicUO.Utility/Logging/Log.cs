// SPDX-License-Identifier: BSD-2-Clause

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