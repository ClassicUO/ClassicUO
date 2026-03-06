#region license

// Copyright (c) 2021, andreakarasho
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

        public static void OnKeyUp(SDL.SDL_KeyboardEvent e)
        {
            ApplyFilter(ref e);
            UpdateModifiers(e.mod);
            _code = SDL.SDL_Keycode.SDLK_UNKNOWN;
        }

        public static void OnKeyDown(SDL.SDL_KeyboardEvent e)
        {
            ApplyFilter(ref e);
            UpdateModifiers(e.mod);
            if ((SDL.SDL_Keycode)e.key != SDL.SDL_Keycode.SDLK_UNKNOWN)
                _code = (SDL.SDL_Keycode)e.key;
        }

        private static void ApplyFilter(ref SDL.SDL_KeyboardEvent e)
        {
            if ((e.mod & (SDL.SDL_Keymod.SDL_KMOD_RALT | SDL.SDL_Keymod.SDL_KMOD_LCTRL)) == (SDL.SDL_Keymod.SDL_KMOD_RALT | SDL.SDL_Keymod.SDL_KMOD_LCTRL))
            {
                e.key = (uint)SDL.SDL_Keycode.SDLK_UNKNOWN;
                e.mod = SDL.SDL_Keymod.SDL_KMOD_NONE;
            }
        }

        private static void UpdateModifiers(SDL.SDL_Keymod mod)
        {
            SDL.SDL_Keymod m = mod & ~IgnoreKeyMod;
            if ((m & (SDL.SDL_Keymod.SDL_KMOD_RALT | SDL.SDL_Keymod.SDL_KMOD_LCTRL)) == (SDL.SDL_Keymod.SDL_KMOD_RALT | SDL.SDL_Keymod.SDL_KMOD_LCTRL))
                m = SDL.SDL_Keymod.SDL_KMOD_NONE;
            Shift = (m & SDL.SDL_Keymod.SDL_KMOD_SHIFT) != SDL.SDL_Keymod.SDL_KMOD_NONE;
            Alt = (m & SDL.SDL_Keymod.SDL_KMOD_ALT) != SDL.SDL_Keymod.SDL_KMOD_NONE;
            Ctrl = (m & SDL.SDL_Keymod.SDL_KMOD_CTRL) != SDL.SDL_Keymod.SDL_KMOD_NONE;
        }

        public static void Refresh()
        {
            UpdateModifiers(SDL.SDL_GetModState());
        }
    }
}