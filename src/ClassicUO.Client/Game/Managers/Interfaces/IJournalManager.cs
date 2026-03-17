// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game.Data;

namespace ClassicUO.Game.Managers
{
    internal interface IJournalManager
    {
        event EventHandler<JournalEntry> EntryAdded;

        void Add(string text, ushort hue, string name, uint? serial, TextType type, bool isunicode = true, MessageType messageType = MessageType.Regular);

        void CloseWriter();

        void Clear();
    }
}
