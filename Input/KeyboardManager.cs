using System;
using System.Linq;
using Microsoft.Xna.Framework.Input;

namespace ClassicUO.Input
{
    public static class KeyboardManager
    {
        private static KeyboardState _prevKeyboardState = Keyboard.GetState();


        public static void Update()
        {
            var current = Keyboard.GetState();

            var oldkeys = _prevKeyboardState.GetPressedKeys();
            var newkeys = current.GetPressedKeys();

            foreach (var k in newkeys)
                if (current.IsKeyDown(k))
                {
                    var old = oldkeys.FirstOrDefault(s => s == k);
                    if (!_prevKeyboardState.IsKeyDown(old))
                    {
                        // pressed 1st time: FIRE!
                        var arg = new KeyboardEventArgs(k, KeyState.Down);
                        KeyDown?.Invoke(null, arg);
                    }
                    else
                    {
                        var arg = new KeyboardEventArgs(k, KeyState.Down);
                        KeyPressed?.Invoke(null, arg);
                    }
                }

            foreach (var k in oldkeys)
                if (current.IsKeyUp(k))
                {
                    var old = oldkeys.FirstOrDefault(s => s == k);
                    if (!_prevKeyboardState.IsKeyUp(old))
                    {
                        // released 1st time: FIRE!
                        var arg = new KeyboardEventArgs(k, KeyState.Up);
                        KeyUp?.Invoke(null, arg);
                    }
                }

            _prevKeyboardState = current;
        }


        public static event EventHandler<KeyboardEventArgs> KeyDown, KeyUp, KeyPressed;
    }
}