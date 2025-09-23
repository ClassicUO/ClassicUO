// SPDX-License-Identifier: BSD-2-Clause

using SDL3;

namespace ClassicUO.Input
{
    internal static class Keyboard
    {
        private static SDL.SDL_Keycode _code;


        public static SDL.SDL_Keymod IgnoreKeyMod { get; } = SDL.SDL_Keymod.SDL_KMOD_CAPS | SDL.SDL_Keymod.SDL_KMOD_NUM | SDL.SDL_Keymod.SDL_KMOD_MODE | SDL.SDL_Keymod.SDL_KMOD_SCROLL;

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
            SDL.SDL_Keymod mod = e.mod & ~IgnoreKeyMod;

            if ((mod & (SDL.SDL_Keymod.SDL_KMOD_RALT | SDL.SDL_Keymod.SDL_KMOD_LCTRL)) == (SDL.SDL_Keymod.SDL_KMOD_RALT | SDL.SDL_Keymod.SDL_KMOD_LCTRL))
            {
                e.key = (uint)SDL.SDL_Keycode.SDLK_UNKNOWN;
                e.mod = SDL.SDL_Keymod.SDL_KMOD_NONE;
            }

            Shift = (e.mod & SDL.SDL_Keymod.SDL_KMOD_SHIFT) != SDL.SDL_Keymod.SDL_KMOD_NONE;
            Alt = (e.mod & SDL.SDL_Keymod.SDL_KMOD_ALT) != SDL.SDL_Keymod.SDL_KMOD_NONE;
            Ctrl = (e.mod & SDL.SDL_Keymod.SDL_KMOD_CTRL) != SDL.SDL_Keymod.SDL_KMOD_NONE;

            _code = SDL.SDL_Keycode.SDLK_UNKNOWN;
        }

        public static void OnKeyDown(SDL.SDL_KeyboardEvent e)
        {
            SDL.SDL_Keymod mod = e.mod & ~IgnoreKeyMod;

            if ((mod & (SDL.SDL_Keymod.SDL_KMOD_RALT | SDL.SDL_Keymod.SDL_KMOD_LCTRL)) == (SDL.SDL_Keymod.SDL_KMOD_RALT | SDL.SDL_Keymod.SDL_KMOD_LCTRL))
            {
                e.key = (uint)SDL.SDL_Keycode.SDLK_UNKNOWN;
                e.mod = SDL.SDL_Keymod.SDL_KMOD_NONE;
            }

            Shift = (e.mod & SDL.SDL_Keymod.SDL_KMOD_SHIFT) != SDL.SDL_Keymod.SDL_KMOD_NONE;
            Alt = (e.mod & SDL.SDL_Keymod.SDL_KMOD_ALT) != SDL.SDL_Keymod.SDL_KMOD_NONE;
            Ctrl = (e.mod & SDL.SDL_Keymod.SDL_KMOD_CTRL) != SDL.SDL_Keymod.SDL_KMOD_NONE;

            if ((SDL.SDL_Keycode)e.key != SDL.SDL_Keycode.SDLK_UNKNOWN)
            {
                _code = (SDL.SDL_Keycode)e.key;
            }
        }
    }
}