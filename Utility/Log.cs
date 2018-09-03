using System;
using System.Collections.Generic;

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

    public static class Log
    {
        private static readonly Dictionary<LogTypes, string> _logMsgFormat = new Dictionary<LogTypes, string> { { LogTypes.None, string.Empty }, { LogTypes.Trace, "  Trace   " }, { LogTypes.Info, "  Info    " }, { LogTypes.Warning, "  Warning " }, { LogTypes.Error, "  Error   " } };

        private static readonly Dictionary<LogTypes, ConsoleColor> _logMsgColor = new Dictionary<LogTypes, ConsoleColor> { { LogTypes.None, ConsoleColor.White }, { LogTypes.Trace, ConsoleColor.Green }, { LogTypes.Info, ConsoleColor.Cyan }, { LogTypes.Warning, ConsoleColor.Yellow }, { LogTypes.Error, ConsoleColor.Red } };


        public static void Message(in LogTypes type, in string msg)
        {
            Console.Write(DateTime.Now.ToString("HH:mm:ss") + " | ");

            ConsoleColor prev = Console.ForegroundColor;
            Console.ForegroundColor = _logMsgColor[type];
            Console.Write(_logMsgFormat[type]);
            Console.ForegroundColor = prev;

            Console.WriteLine(" | " + msg);
        }
    }
}