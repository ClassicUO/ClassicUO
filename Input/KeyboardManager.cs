using ClassicUO.Utility;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClassicUO.Input
{
    public static class KeyboardManager
    {
        private static KeyboardState _prevKeyboardState = Keyboard.GetState();

        public static event EventHandler<KeyboardEventArgs> KeyDown, KeyUp;

        public static void Update()
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
                        var arg = new KeyboardEventArgs(k, KeyState.Down);
                        KeyDown.Raise(arg);
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
                        var arg = new KeyboardEventArgs(k, KeyState.Up);
                        KeyUp.Raise(arg);
                    }
                }
            }

            _prevKeyboardState = current;
        }
    }
}
