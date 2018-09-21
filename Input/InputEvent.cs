using static SDL2.SDL;

namespace ClassicUO.Input
{
    public class InputEvent
    {
        public InputEvent(SDL_Keymod modifiers) => Mod = modifiers;

        protected InputEvent(InputEvent parent) => Mod = parent.Mod;

        public SDL_Keymod Mod { get; }

        public bool IsHandled { get; set; }
        public bool Alt => (Mod & SDL_Keymod.KMOD_LALT) == SDL_Keymod.KMOD_LALT;
        public bool Control => (Mod & SDL_Keymod.KMOD_LCTRL) == SDL_Keymod.KMOD_LCTRL;
        public bool Shift => (Mod & SDL_Keymod.KMOD_LSHIFT) == SDL_Keymod.KMOD_LSHIFT;
    }
}