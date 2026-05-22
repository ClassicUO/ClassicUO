// SPDX-License-Identifier: BSD-2-Clause

using SDL3;

namespace ClassicUO.Game.Input.Hotkeys
{
    /// <summary>
    /// Owns the list of (key, modifier) -&gt; <see cref="HotkeyAction"/>
    /// bindings. Single responsibility: add / remove / find. No action
    /// resolution, no dispatch.
    /// </summary>
    internal interface IHotkeyBindingStore
    {
        bool Bind(HotkeyAction action, SDL.SDL_Keycode key, SDL.SDL_Keymod mod);
        void UnBind(HotkeyAction action);
        bool TryFind(SDL.SDL_Keycode key, SDL.SDL_Keymod mod, out HotkeyAction action);
    }
}
