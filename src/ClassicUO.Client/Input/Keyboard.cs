#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

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