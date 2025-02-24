// SPDX-License-Identifier: BSD-2-Clause

using SDL2;

namespace ClassicUO.Input
{
    internal static class Keyboard
    {
        private static SDL.SDL_Keycode _code;


        public static SDL.SDL_Keymod IgnoreKeyMod { get; } = SDL.SDL_Keymod.KMOD_CAPS | SDL.SDL_Keymod.KMOD_NUM | SDL.SDL_Keymod.KMOD_MODE | SDL.SDL_Keymod.KMOD_RESERVED;

        public static bool Alt { get; private set; }
        public static bool Shift { get; private set; }
        public static bool Ctrl { get; private set; }


        //public static bool IsKeyPressed(SDL.SDL_Keycode code)
        //{
        //    return code != SDL.SDL_Keycode.SDLK_UNKNOWN && _code == code;
        //}

        //public static bool IsModPressed(SDL.SDL_Keymod mod, SDL.SDL_Keymod tocheck)
        //{
        //    mod ^= mod & IgnoreKeyMod;

        //    return tocheck == mod || mod != SDL.SDL_Keymod.KMOD_NONE && (mod & tocheck) != 0;
        //}

        public static void OnKeyUp(SDL.SDL_KeyboardEvent e)
        {
            SDL.SDL_Keymod mod = e.keysym.mod & ~IgnoreKeyMod;

            if ((mod & (SDL.SDL_Keymod.KMOD_RALT | SDL.SDL_Keymod.KMOD_LCTRL)) == (SDL.SDL_Keymod.KMOD_RALT | SDL.SDL_Keymod.KMOD_LCTRL))
            {
                e.keysym.sym = SDL.SDL_Keycode.SDLK_UNKNOWN;
                e.keysym.mod = SDL.SDL_Keymod.KMOD_NONE;
            }

            Shift = (e.keysym.mod & SDL.SDL_Keymod.KMOD_SHIFT) != SDL.SDL_Keymod.KMOD_NONE;
            Alt = (e.keysym.mod & SDL.SDL_Keymod.KMOD_ALT) != SDL.SDL_Keymod.KMOD_NONE;
            Ctrl = (e.keysym.mod & SDL.SDL_Keymod.KMOD_CTRL) != SDL.SDL_Keymod.KMOD_NONE;

            _code = SDL.SDL_Keycode.SDLK_UNKNOWN;
        }

        public static void OnKeyDown(SDL.SDL_KeyboardEvent e)
        {
            SDL.SDL_Keymod mod = e.keysym.mod & ~IgnoreKeyMod;

            if ((mod & (SDL.SDL_Keymod.KMOD_RALT | SDL.SDL_Keymod.KMOD_LCTRL)) == (SDL.SDL_Keymod.KMOD_RALT | SDL.SDL_Keymod.KMOD_LCTRL))
            {
                e.keysym.sym = SDL.SDL_Keycode.SDLK_UNKNOWN;
                e.keysym.mod = SDL.SDL_Keymod.KMOD_NONE;
            }

            Shift = (e.keysym.mod & SDL.SDL_Keymod.KMOD_SHIFT) != SDL.SDL_Keymod.KMOD_NONE;
            Alt = (e.keysym.mod & SDL.SDL_Keymod.KMOD_ALT) != SDL.SDL_Keymod.KMOD_NONE;
            Ctrl = (e.keysym.mod & SDL.SDL_Keymod.KMOD_CTRL) != SDL.SDL_Keymod.KMOD_NONE;

            if (e.keysym.sym != SDL.SDL_Keycode.SDLK_UNKNOWN)
            {
                _code = e.keysym.sym;
            }
        }
    }
}