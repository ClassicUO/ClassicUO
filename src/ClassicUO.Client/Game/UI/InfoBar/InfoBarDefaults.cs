// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Resources;

namespace ClassicUO.Game.UI.InfoBar
{
    /// <summary>
    /// Concrete <see cref="IInfoBarDefaults"/>. Seeds an
    /// <see cref="IInfoBarStore"/> with the stock info bar items used
    /// when a profile has no <c>infobar.xml</c> yet.
    /// </summary>
    internal sealed class InfoBarDefaults : IInfoBarDefaults
    {
        public void CreateDefault(IInfoBarStore store)
        {
            store.Clear();

            store.Add(new InfoBarItem("", InfoBarVars.NameNotoriety, 0x3D2));
            store.Add(new InfoBarItem(ResGeneral.Hits, InfoBarVars.HP, 0x1B6));
            store.Add(new InfoBarItem(ResGeneral.Mana, InfoBarVars.Mana, 0x1ED));
            store.Add(new InfoBarItem(ResGeneral.Stam, InfoBarVars.Stamina, 0x22E));
            store.Add(new InfoBarItem(ResGeneral.Weight, InfoBarVars.Weight, 0x3D2));
        }
    }
}
