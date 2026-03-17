// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;

namespace ClassicUO.Game.Managers
{
    internal interface IInfoBarManager
    {
        List<InfoBarItem> GetInfoBars();

        void AddItem(InfoBarItem ibi);

        void RemoveItem(InfoBarItem item);

        void Clear();

        void Save();

        void Load();

        void CreateDefault();
    }
}
