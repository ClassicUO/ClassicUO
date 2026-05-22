// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;

namespace ClassicUO.Game.Input.Hotkeys
{
    /// <summary>
    /// Catalog of <see cref="HotkeyAction"/> -&gt; <see cref="Action"/> handlers.
    /// Single responsibility: own the default action table and resolve a
    /// <see cref="HotkeyAction"/> back to an executable delegate. No bindings,
    /// no dispatch.
    /// </summary>
    internal interface IHotkeyActionRegistry
    {
        bool TryGetAction(HotkeyAction action, out Action handler);
        Dictionary<HotkeyAction, Action> GetValues();
    }
}
