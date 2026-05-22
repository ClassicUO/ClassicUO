// SPDX-License-Identifier: BSD-2-Clause

using System;
using SDL3;

namespace ClassicUO.Game.Input.Hotkeys
{
    /// <summary>
    /// Resolves an input (key + modifier) to an executable
    /// <see cref="Action"/> by consulting the binding store and the action
    /// registry. Single responsibility: input -&gt; binding -&gt; action.
    /// </summary>
    internal interface IHotkeyDispatcher
    {
        bool TryExecuteIfBinded(SDL.SDL_Keycode key, SDL.SDL_Keymod mod, out Action action);
    }
}
