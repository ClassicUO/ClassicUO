// SPDX-License-Identifier: BSD-2-Clause

using System;

namespace ClassicUO.Game.Events.Outgoing
{
    /// <summary>0xB1 Generic gump response sent.</summary>
    internal readonly record struct GumpResponseSentArgs(uint Local, uint Server, int Button, uint[] Switches, Tuple<ushort, string>[] Entries);

    /// <summary>0xB1 Virtue gump response sent.</summary>
    internal readonly record struct VirtueGumpResponseSentArgs(uint Serial, uint Code);

    /// <summary>0x7D Menu response sent.</summary>
    internal readonly record struct MenuResponseSentArgs(uint Serial, ushort Graphic, int Code, ushort ItemGraphic, ushort ItemHue);

    /// <summary>0x7D Gray menu response sent.</summary>
    internal readonly record struct GrayMenuResponseSentArgs(uint Serial, ushort Graphic, ushort Code);

    /// <summary>0xBF/0x13 Request popup menu sent.</summary>
    internal readonly record struct RequestPopupMenuSentArgs(uint Serial);

    /// <summary>0xBF/0x15 Popup menu selection sent.</summary>
    internal readonly record struct PopupMenuSelectionSentArgs(uint Serial, ushort MenuId);

    /// <summary>0xAC Text entry dialog response sent.</summary>
    internal readonly record struct TextEntryDialogResponseSentArgs(uint Serial, byte ParentId, byte Button, string Text, bool Code);
}
