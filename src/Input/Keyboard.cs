using SDL2;

namespace ClassicUO.Input
{
    internal static class Keyboard
    {
        static Keyboard()
        {
            Engine.Input.KeyDown += InputOnKeyDown;
            Engine.Input.KeyUp += InputOnKeyUp;
        }


        public static SDL.SDL_Keymod IgnoreKeyMod { get; } = SDL.SDL_Keymod.KMOD_CAPS | SDL.SDL_Keymod.KMOD_NUM | SDL.SDL_Keymod.KMOD_MODE | SDL.SDL_Keymod.KMOD_RESERVED;

        public static bool Alt { get; private set; }
        public static bool Shift { get; private set; }
        public static bool Ctrl { get; private set; }


        public static bool IsModPressed(SDL.SDL_Keymod mod, SDL.SDL_Keymod tocheck)
        {
            mod ^= mod & IgnoreKeyMod;

            return tocheck == mod || mod != SDL.SDL_Keymod.KMOD_NONE && (mod & tocheck) != 0;
        }



        private static void InputOnKeyUp(object sender, SDL.SDL_KeyboardEvent e)
        {
            Shift = (e.keysym.mod & SDL.SDL_Keymod.KMOD_SHIFT) != SDL.SDL_Keymod.KMOD_NONE;
            Alt = (e.keysym.mod & SDL.SDL_Keymod.KMOD_ALT) != SDL.SDL_Keymod.KMOD_NONE;
            Ctrl = (e.keysym.mod & SDL.SDL_Keymod.KMOD_CTRL) != SDL.SDL_Keymod.KMOD_NONE;
        }

        private static void InputOnKeyDown(object sender, SDL.SDL_KeyboardEvent e)
        {
            Shift = (e.keysym.mod & SDL.SDL_Keymod.KMOD_SHIFT) != SDL.SDL_Keymod.KMOD_NONE;
            Alt = (e.keysym.mod & SDL.SDL_Keymod.KMOD_ALT) != SDL.SDL_Keymod.KMOD_NONE;
            Ctrl = (e.keysym.mod & SDL.SDL_Keymod.KMOD_CTRL) != SDL.SDL_Keymod.KMOD_NONE;
        }
    }
}