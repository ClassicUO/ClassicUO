using System;
using System.IO;
using System.Collections.Generic;

using ClassicUO.Utility;

namespace ClassicUO.Game.Managers
{
    internal class JournalManager
    {
        private readonly Deque<JournalEntry> _entries = new Deque<JournalEntry>();

        public Deque<JournalEntry> Entries => _entries;

        public event EventHandler<JournalEntry> EntryAdded;

        public void Add(string text, Hue hue, string name, bool isunicode = true)
        {
            if (_entries.Count >= 100)
                _entries.RemoveFromFront();

            JournalEntry entry = new JournalEntry(text, (byte) (isunicode ? 0 : 9), hue, name, isunicode);
            _entries.AddToBack(entry);
            EntryAdded.Raise(entry);
            _fileWriter?.WriteLineAsync($"{name}: {text}");
            _fileWriter?.FlushAsync();
        }

        public void CreateWriter(bool create)
        {
            if (create)
            {
                try
                {
                    FileInfo info = new FileInfo($"{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}_journal.txt");
                    _fileWriter = info.CreateText();
                }
                catch { }
            }
            else
            {
                _fileWriter?.Flush();
                _fileWriter?.Dispose();
                _fileWriter = null;
            }
        }

        private StreamWriter _fileWriter;
        public void Clear()
        {
            _entries.Clear();
            CreateWriter(false);
        }
    }

    internal readonly struct JournalEntry
    {
        public JournalEntry(string text, byte font, Hue hue, string name, bool isunicode)
        {
            IsUnicode = isunicode;
            Font = font;
            Hue = hue;
            Name = name;
            Text = text;
        }

        public readonly bool IsUnicode;
        public readonly byte Font;
        public readonly Hue Hue;
        public readonly string Name;
        public readonly string Text;
    }
}