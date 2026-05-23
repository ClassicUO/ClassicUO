// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.UI.InfoBar
{
    /// <summary>
    /// Produces the default <see cref="InfoBarItem"/> set used when no
    /// profile <c>infobar.xml</c> file exists. Seeds the store with the
    /// stock entries (NameNotoriety, HP, Mana, Stamina, Weight).
    /// </summary>
    internal interface IInfoBarDefaults
    {
        /// <summary>Clear <paramref name="store"/> and repopulate it with the default info bar items.</summary>
        void CreateDefault(IInfoBarStore store);
    }
}
