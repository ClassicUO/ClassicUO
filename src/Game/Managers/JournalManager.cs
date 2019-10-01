#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System;
using System.IO;

using ClassicUO.Utility;
using ClassicUO.Utility.Collections;

namespace ClassicUO.Game.Managers
{
    internal class JournalManager
    {
        private StreamWriter _fileWriter;
        public Deque<JournalEntry> Entries { get; } = new Deque<JournalEntry>();

        public event EventHandler<JournalEntry> EntryAdded;

        public void Add(string text, Hue hue, string name, bool isunicode = true)
        {
            if (Entries.Count >= 100)
                Entries.RemoveFromFront();

            byte font = (byte) (isunicode ? 0 : 9);

            if (Engine.Profile.Current != null && Engine.Profile.Current.OverrideAllFonts)
            {
                font = Engine.Profile.Current.ChatFont;
                isunicode = Engine.Profile.Current.OverrideAllFontsIsUnicode;
            }

            JournalEntry entry = new JournalEntry(text, font, hue, name, isunicode, Engine.CurrDateTime);
            Entries.AddToBack(entry);
            EntryAdded.Raise(entry);
            _fileWriter?.WriteLine($"[{Engine.CurrDateTime:g}]  {name}: {text}");
        }

        public void CreateWriter(bool create)
        {
            if (create)
            {
                try
                {
                    _fileWriter = new StreamWriter(File.Open($"{Engine.CurrDateTime:yyyy_MM_dd_HH_mm_ss}_journal.txt", FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        AutoFlush = true
                    };
                }
                catch
                {
                }
            }
            else
            {
                _fileWriter?.Flush();
                _fileWriter?.Dispose();
                _fileWriter = null;
            }
        }

        public void Clear()
        {
            Entries.Clear();
            CreateWriter(false);
        }
    }

    internal class JournalEntry
    {
        public readonly byte Font;
        public readonly Hue Hue;

        public readonly bool IsUnicode;
        public readonly string Name;
        public readonly string Text;
        public readonly DateTime Time;

        public JournalEntry(string text, byte font, Hue hue, string name, bool isunicode, DateTime time)
        {
            IsUnicode = isunicode;
            Font = font;
            Hue = hue;
            Name = name;
            Text = text;
            Time = time;
        }
    }
}