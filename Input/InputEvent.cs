using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;
using static SDL2.SDL;

namespace ClassicUO.Input
{
    public class InputEvent
    {
        private readonly SDL_Keymod _modifiers;


        public InputEvent(SDL_Keymod modifiers)
        {
            _modifiers = modifiers;
        }

        protected InputEvent(InputEvent parent)
        {
            _modifiers = parent._modifiers;
        }

        public SDL_Keymod Mod => _modifiers;

        public bool IsHandled { get; set; }
        public bool Alt => (_modifiers & SDL_Keymod.KMOD_ALT) == SDL_Keymod.KMOD_ALT;
        public bool Control => (_modifiers & SDL_Keymod.KMOD_CTRL) == SDL_Keymod.KMOD_CTRL;
        public bool Shift => (_modifiers & SDL_Keymod.KMOD_SHIFT) == SDL_Keymod.KMOD_SHIFT;
    }
}
