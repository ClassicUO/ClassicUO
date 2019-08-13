using SDL2;

namespace ClassicUO.Input
{
    public static class Keyboard
    {
        public static SDL2.SDL.SDL_Keymod IgnoreKeyMod { get; } = SDL2.SDL.SDL_Keymod.KMOD_CAPS | SDL2.SDL.SDL_Keymod.KMOD_NUM | SDL2.SDL.SDL_Keymod.KMOD_MODE | SDL2.SDL.SDL_Keymod.KMOD_RESERVED;

        public static bool IsModPressed(SDL.SDL_Keymod mod, SDL.SDL_Keymod tocheck)
        {
            mod ^= mod & IgnoreKeyMod;
            return tocheck == mod || (mod != SDL.SDL_Keymod.KMOD_NONE && (mod & tocheck) != 0);
        }

        public static bool IgnoreNextTextInput { get; set; }
    }
}
