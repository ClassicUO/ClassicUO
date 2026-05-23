// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Input;
using SDL3;

namespace ClassicUO.Game.Macros
{
    /// <summary>
    /// Hotkey + name lookup over the macro list owned by a
    /// <see cref="MacroManager"/>. Walks the host's
    /// <see cref="LinkedObject"/> chain and returns the first
    /// <see cref="Macro"/> whose binding matches the supplied input — keyboard
    /// keycode, mouse button, mouse wheel direction, or display name.
    /// Pure read-only — no mutation, no I/O.
    /// </summary>
    internal interface IMacroLookup
    {
        /// <summary>Find the macro bound to the given keyboard keycode + modifier set, or <c>null</c> if none.</summary>
        Macro FindMacro(MacroManager host, SDL.SDL_Keycode key, bool alt, bool ctrl, bool shift);

        /// <summary>Find the macro bound to the given mouse button + modifier set, or <c>null</c> if none.</summary>
        Macro FindMacro(MacroManager host, MouseButtonType button, bool alt, bool ctrl, bool shift);

        /// <summary>Find the macro bound to the given mouse-wheel direction + modifier set, or <c>null</c> if none.</summary>
        Macro FindMacro(MacroManager host, bool wheelUp, bool alt, bool ctrl, bool shift);

        /// <summary>Find the macro whose <see cref="Macro.Name"/> equals <paramref name="name"/>, or <c>null</c> if none.</summary>
        Macro FindMacro(MacroManager host, string name);
    }
}
