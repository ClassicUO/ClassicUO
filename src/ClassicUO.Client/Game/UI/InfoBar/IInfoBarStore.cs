// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;

namespace ClassicUO.Game.UI.InfoBar
{
    /// <summary>
    /// Owns the in-memory <see cref="InfoBarItem"/> collection used by an
    /// <see cref="InfoBarManager"/> and the XML persistence layer in
    /// <c>infobar.xml</c>. Responsible for add / remove / clear semantics
    /// and for reading / writing the XML file in the active profile directory.
    /// </summary>
    internal interface IInfoBarStore
    {
        /// <summary>Backing list of info bar items. Exposed for read-only iteration by gumps.</summary>
        List<InfoBarItem> Items { get; }

        /// <summary>Append an item to the end of the list.</summary>
        void Add(InfoBarItem item);

        /// <summary>Remove an item from the list.</summary>
        void Remove(InfoBarItem item);

        /// <summary>Empty the list.</summary>
        void Clear();

        /// <summary>Load <c>infobar.xml</c> from the active profile. Falls back to <paramref name="defaults"/> (then persists) when the file is missing.</summary>
        void Load(IInfoBarDefaults defaults);

        /// <summary>Serialise the current list to <c>infobar.xml</c> in the active profile.</summary>
        void Save();
    }
}
