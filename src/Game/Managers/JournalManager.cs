using System;
using System.Collections.Generic;

using ClassicUO.Utility;

namespace ClassicUO.Game.Managers
{
    internal class JournalManager
    {
        private readonly Deque<JournalEntry> _entries = new Deque<JournalEntry>();

        public IReadOnlyList<JournalEntry> Entries => _entries;

        public event EventHandler<JournalEntry> EntryAdded;

        public void Add(string text, Hue hue, string name, bool isunicode = true)
        {
            if (_entries.Count >= 100)
                _entries.RemoveFromFront();

            JournalEntry entry = new JournalEntry(text, isunicode ? MessageFont.Bold : MessageFont.SmallLight, hue, name, isunicode);
            _entries.AddToBack(entry);
            EntryAdded.Raise(entry);
        }

        public void Clear() => _entries.Clear();
    }

    internal readonly struct JournalEntry
    {
        public JournalEntry(string text, MessageFont font, Hue hue, string name, bool isunicode)
        {
            IsUnicode = isunicode;
            Font = font;
            Hue = hue;
            Name = name;
            Text = text;
        }

        public readonly bool IsUnicode;
        public readonly MessageFont Font;
        public readonly Hue Hue;
        public readonly string Name;
        public readonly string Text;
    }
}
