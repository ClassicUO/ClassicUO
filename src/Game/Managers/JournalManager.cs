#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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

using ClassicUO.Configuration;
using ClassicUO.Utility;
using ClassicUO.Utility.Collections;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    internal class JournalManager
    {
        private StreamWriter _fileWriter;
        private bool _writerHasException;

        public static Deque<JournalEntry> Entries { get; } = new Deque<JournalEntry>(Constants.MAX_JOURNAL_HISTORY_COUNT);

        public event EventHandler<JournalEntry> EntryAdded;


        public void Add(string text, ushort hue, string name, bool isunicode = true)
        {
            if (Entries.Count >= Constants.MAX_JOURNAL_HISTORY_COUNT)
                Entries.RemoveFromFront();

            byte font = (byte) (isunicode ? 0 : 9);

            if (ProfileManager.Current != null && ProfileManager.Current.OverrideAllFonts)
            {
                font = ProfileManager.Current.ChatFont;
                isunicode = ProfileManager.Current.OverrideAllFontsIsUnicode;
            }

            DateTime timeNow = DateTime.Now;

            JournalEntry entry = new JournalEntry(text, font, hue, name, isunicode, timeNow);

            if (ProfileManager.Current != null && ProfileManager.Current.ForceUnicodeJournal)
            {
                entry.Font = 0;
                entry.IsUnicode = true;
            }

            Entries.AddToBack(entry);
            EntryAdded.Raise(entry);

            if (_fileWriter == null && !_writerHasException)
            {
                CreateWriter();
            }

            _fileWriter?.WriteLine($"[{timeNow:g}]  {name}: {text}");
        }

        private void CreateWriter()
        {
            if (_fileWriter == null && ProfileManager.Current.SaveJournalToFile)
            {
                try
                {
                    string path = FileSystemHelper.CreateFolderIfNotExists(Path.Combine(CUOEnviroment.ExecutablePath, "Data"), "Client", "JournalLogs");
                    _fileWriter = new StreamWriter(File.Open(Path.Combine(path, $"{DateTime.Now:yyyy_MM_dd_HH_mm_ss}_journal.txt"), FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        AutoFlush = true
                    };
                    try
                    {
                        string[] files = Directory.GetFiles(path, "*_journal.txt");
                        Array.Sort<string>(files);
                        for (int i = files.Length - 1; i >= 100; --i)
                            File.Delete(files[i]);
                    }
                    catch { }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    // we don't want to wast time.
                    _writerHasException = true;
                }
            }
        }

        public void CloseWriter()
        {
            _fileWriter?.Flush();
            _fileWriter?.Dispose();
            _fileWriter = null;
        }

        public void Clear()
        {
            //Entries.Clear();
            CloseWriter();
        }
    }

    internal class JournalEntry
    {
        public byte Font;
        public ushort Hue;

        public bool IsUnicode;
        public string Name;
        public string Text;
        public DateTime Time;

        public JournalEntry(string text, byte font, ushort hue, string name, bool isunicode, DateTime time)
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