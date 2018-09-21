#region license

//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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

    public class Log
    {
        private static readonly Dictionary<LogTypes, string> _logMsgFormat = new Dictionary<LogTypes, string>
        {
            {LogTypes.None, string.Empty}, {LogTypes.Trace, "  Trace   "}, {LogTypes.Info, "  Info    "},
            {LogTypes.Warning, "  Warning "}, {LogTypes.Error, "  Error   "}
        };

        private static readonly Dictionary<LogTypes, ConsoleColor> _logMsgColor = new Dictionary<LogTypes, ConsoleColor>
        {
            {LogTypes.None, ConsoleColor.White}, {LogTypes.Trace, ConsoleColor.Green},
            {LogTypes.Info, ConsoleColor.Cyan}, {LogTypes.Warning, ConsoleColor.Yellow},
            {LogTypes.Error, ConsoleColor.Red}
        };


        public void Message(LogTypes type, string msg, bool newline = true)
        {
            if (type != LogTypes.None)
                Console.Write(DateTime.Now.ToString("HH:mm:ss") + " | ");

            ConsoleColor prev = Console.ForegroundColor;
            Console.ForegroundColor = _logMsgColor[type];
            Console.Write(_logMsgFormat[type]);
            Console.ForegroundColor = prev;

            if (newline)
                Console.WriteLine(type == LogTypes.None ? string.Empty + msg : " |  " + msg);
            else
                Console.Write(type == LogTypes.None ? string.Empty + msg : " |  " + msg);
        }
    }
}