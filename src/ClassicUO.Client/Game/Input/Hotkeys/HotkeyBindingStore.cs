// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using SDL3;

namespace ClassicUO.Game.Input.Hotkeys
{
    /// <summary>
    /// In-memory list of <see cref="HotKeyCombination"/> bindings. Bind
    /// refuses duplicates (same key+mod). UnBind removes by action.
    /// </summary>
    internal sealed class HotkeyBindingStore : IHotkeyBindingStore
    {
        private readonly List<HotKeyCombination> _hotkeys = new List<HotKeyCombination>();

        public bool Bind(HotkeyAction action, SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            foreach (HotKeyCombination h in _hotkeys)
            {
                if (h.Key == key && h.Mod == mod)
                {
                    return false;
                }
            }

            _hotkeys.Add
            (
                new HotKeyCombination
                {
                    Key = key,
                    Mod = mod,
                    KeyAction = action
                }
            );

            return true;
        }

        public void UnBind(HotkeyAction action)
        {
            for (int i = 0; i < _hotkeys.Count; i++)
            {
                HotKeyCombination h = _hotkeys[i];

                if (h.KeyAction == action)
                {
                    _hotkeys.RemoveAt(i);

                    break;
                }
            }
        }

        public bool TryFind(SDL.SDL_Keycode key, SDL.SDL_Keymod mod, out HotkeyAction action)
        {
            for (int i = 0; i < _hotkeys.Count; i++)
            {
                HotKeyCombination h = _hotkeys[i];

                if (h.Key == key && h.Mod == mod)
                {
                    action = h.KeyAction;
                    return true;
                }
            }

            action = HotkeyAction.None;
            return false;
        }
    }
}
