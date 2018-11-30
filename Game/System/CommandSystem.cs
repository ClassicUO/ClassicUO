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
using System.Collections.Generic;

using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.System
{
    public static class CommandSystem
    {
        private static readonly Dictionary<string, EventHandler> _commandDictionary = new Dictionary<string, EventHandler>();

        public static void Register(string commandName, EventHandler commandHandler)
        {
            if (_commandDictionary.ContainsKey(commandName))
                Log.Message(LogTypes.Error, string.Format($"Attempted to register command: '{0}' twice."));
            else
                _commandDictionary.Add(commandName, commandHandler);
        }

        public static void TriggerCommandHandler(string commandName)
        {
            if (_commandDictionary.TryGetValue(commandName, out EventHandler commandHandler)) commandHandler.Raise();
        }
    }
}