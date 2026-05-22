// SPDX-License-Identifier: BSD-2-Clause

using System;
using SDL3;

namespace ClassicUO.Game.Input.Hotkeys
{
    /// <summary>
    /// Combines the binding store and the action registry: looks up the
    /// <see cref="HotkeyAction"/> bound to (key, mod) and resolves it to an
    /// executable delegate. Caller is responsible for actually invoking the
    /// returned <see cref="Action"/>.
    /// </summary>
    internal sealed class HotkeyDispatcher : IHotkeyDispatcher
    {
        private readonly IHotkeyBindingStore _bindings;
        private readonly IHotkeyActionRegistry _registry;

        public HotkeyDispatcher(IHotkeyBindingStore bindings, IHotkeyActionRegistry registry)
        {
            _bindings = bindings;
            _registry = registry;
        }

        public bool TryExecuteIfBinded(SDL.SDL_Keycode key, SDL.SDL_Keymod mod, out Action action)
        {
            if (_bindings.TryFind(key, mod, out HotkeyAction hotkeyAction)
                && _registry.TryGetAction(hotkeyAction, out action))
            {
                return true;
            }

            action = null;
            return false;
        }
    }
}
