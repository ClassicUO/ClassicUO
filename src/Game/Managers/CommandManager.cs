#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
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

using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    internal static class CommandManager
    {
        private static readonly Dictionary<string, Action<string[]>> _commands = new Dictionary<string, Action<string[]>>();


        public static void Initialize()
        {
            Register("info", s =>
            {
                if (!TargetManager.IsTargeting)
                    TargetManager.SetTargeting(CursorTarget.SetTargetClientSide, CursorType.Target, TargetType.Neutral);
                else
                    TargetManager.CancelTarget();
            });

            Register("focus", s => { Engine.DebugFocus = !Engine.DebugFocus; });
            Register("datetime", s =>
            {
                if(World.Player != null)
                {
                    GameActions.Print($"Current DateTime.Now is {DateTime.Now}");
                    GameActions.Print($"Current CurrDateTime is {Engine.CurrDateTime}");
                }
            });
            Register("hue", s =>
            {
                if (!TargetManager.IsTargeting)
                    TargetManager.SetTargeting(CursorTarget.HueCommandTarget, CursorType.Target, TargetType.Neutral);
                else
                    TargetManager.CancelTarget();
            });
        }


        public static void Register(string name, Action<string[]> callback)
        {
            name = name.ToLower();

            if (!_commands.ContainsKey(name))
                _commands.Add(name, callback);
            else
                Log.Message(LogTypes.Error, string.Format($"Attempted to register command: '{0}' twice.", name));
        }

        public static void UnRegister(string name)
        {
            name = name.ToLower();

            if (_commands.ContainsKey(name))
                _commands.Remove(name);
        }

        public static void UnRegisterAll()
        {
            _commands.Clear();
        }

        public static void Execute(string name, params string[] args)
        {
            name = name.ToLower();

            if (_commands.TryGetValue(name, out var action))
                action.Invoke(args);
            else
                Log.Message(LogTypes.Warning, $"Commad: '{name}' not exists");
        }

        public static void OnHueTarget(Entity entity)
        {
            TargetManager.TargetGameObject(entity);
            Mouse.LastLeftButtonClickTime = 0;
            GameActions.Print($"Item ID: {entity.Graphic}\nHue: {entity.Hue}");
        }
    }
}
