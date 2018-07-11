using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClassicUO.Input
{
    public sealed class KeyboardManager : GameComponent
    {
        private KeyboardState _prevKeyboardState = Keyboard.GetState();

        public KeyboardManager(Microsoft.Xna.Framework.Game game): base(game)
        {
          
        }

        public override void Update(GameTime gameTime)
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
                    else
                    {
                        var arg = new KeyboardEventArgs(k, KeyState.Down);
                        KeyPressed.Raise(arg);
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

            base.Update(gameTime);
        }


        public event EventHandler<KeyboardEventArgs> KeyDown, KeyUp, KeyPressed;
    }
}
