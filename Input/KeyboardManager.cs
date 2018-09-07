#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2015 ClassicUO Development Team)
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
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;

namespace ClassicUO.Input
{
    public class KeyboardManager
    {
        private static KeyboardState _prevKeyboardState;

        public KeyboardManager()
        {
            _prevKeyboardState = Keyboard.GetState();
        }


        public void Update()
        {
            KeyboardState current = Keyboard.GetState();

            Keys[] oldkeys = _prevKeyboardState.GetPressedKeys();
            Keys[] newkeys = current.GetPressedKeys();

            foreach (Keys k in newkeys)
            {
                if (current.IsKeyDown(k))
                {
                    Keys old = oldkeys.FirstOrDefault(s => s == k);
                    if (!_prevKeyboardState.IsKeyDown(old))
                    {
                        // pressed 1st time: FIRE!
                        KeyboardEventArgs arg = new KeyboardEventArgs(k, KeyState.Down);
                        KeyDown?.Invoke(null, arg);
                    }
                    else
                    {
                        KeyboardEventArgs arg = new KeyboardEventArgs(k, KeyState.Down);
                        KeyPressed?.Invoke(null, arg);
                    }
                }
            }

            foreach (Keys k in oldkeys)
            {
                if (current.IsKeyUp(k))
                {
                    Keys old = oldkeys.FirstOrDefault(s => s == k);
                    if (!_prevKeyboardState.IsKeyUp(old))
                    {
                        // released 1st time: FIRE!
                        KeyboardEventArgs arg = new KeyboardEventArgs(k, KeyState.Up);
                        KeyUp?.Invoke(null, arg);
                    }
                }
            }

            _prevKeyboardState = current;
        }


        public event EventHandler<KeyboardEventArgs> KeyDown, KeyUp, KeyPressed;
    }
}