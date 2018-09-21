using static SDL2.SDL;

namespace ClassicUO.Input
{
    public class InputEvent
    {
        public InputEvent(SDL_Keymod modifiers) => Mod = modifiers;

        protected InputEvent(InputEvent parent) => Mod = parent.Mod;

        public SDL_Keymod Mod { get; }

        public bool IsHandled { get; set; }
        public bool Alt => (Mod & SDL_Keymod.KMOD_ALT) == SDL_Keymod.KMOD_ALT;
        public bool Control => (Mod & SDL_Keymod.KMOD_CTRL) == SDL_Keymod.KMOD_CTRL;
        public bool Shift => (Mod & SDL_Keymod.KMOD_SHIFT) == SDL_Keymod.KMOD_SHIFT;
    }
}