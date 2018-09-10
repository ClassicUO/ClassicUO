using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;
using static SDL2.SDL;

namespace ClassicUO.Input
{
    public class InputKeyboardEvent : InputEvent
    {
        private readonly int _keyDataExtra;

        public InputKeyboardEvent(KeyboardEvent eventtype, SDL_Keycode keycode, int key, SDL_Keymod modifiers) : base(modifiers)
        {
            EventType = eventtype;
            KeyCode = keycode;
            _keyDataExtra = key;
        }

        public InputKeyboardEvent(KeyboardEvent eventtype, InputKeyboardEvent parent) : base(parent)
        {
            EventType = eventtype;
            KeyCode = parent.KeyCode;
            _keyDataExtra = parent._keyDataExtra;
        }

        public KeyboardEvent EventType { get; }
        public SDL_Keycode KeyCode { get; }
        public bool IsChar => KeyChar != '\0';
        public char KeyChar { get; set; }
       
    }
}
