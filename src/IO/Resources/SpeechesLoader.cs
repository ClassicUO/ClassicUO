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

using ClassicUO.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.IO.Resources
{
    internal class SpeechesLoader : ResourceLoader
    {
        private SpeechEntry[] _speech;

        public override unsafe Task Load()
        {
            return Task.Run(() =>
            {
                string path = Path.Combine(FileManager.UoFolderPath, "speech.mul");

                if (!File.Exists(path))
                {
                    _speech = Array.Empty<SpeechEntry>();
                    return;
                }

                UOFileMul file = new UOFileMul(path);
                List<SpeechEntry> entries = new List<SpeechEntry>();

                while (file.Position < file.Length)
                {
                    int id = file.ReadUShortReversed();
                    int length = file.ReadUShortReversed();

                    if (length > 0)
                    {
                        entries.Add(new SpeechEntry(id, string.Intern(Encoding.UTF8.GetString((byte*) file.PositionAddress, length))));
                        file.Skip(length);
                    }
                }

                _speech = entries.ToArray();
                file.Dispose();
            });
        }

        public override void CleanResources()
        {
        }

        public bool IsMatch(string input, in SpeechEntry entry)
        {
            string[] split = entry.Keywords;

            for (int i = 0; i < split.Length; i++)
            {
                if (split[i].Length > input.Length || split[i].Length == 0)
                    continue;

                if (!entry.CheckStart)
                {
                    if (input.IndexOf(split[i], 0, split[i].Length) == -1)
                        continue;
                }

                if (!entry.CheckEnd)
                {
                    if (input.IndexOf(split[i], input.Length - split[i].Length) == -1)
                        continue;
                }

                if (input.IndexOf(split[i]) != -1)
                    return true;
            }

            return false;
        }

        public List<SpeechEntry> GetKeywords(string text)
        {
            List<SpeechEntry> list = new List<SpeechEntry>();

            if (FileManager.ClientVersion < ClientVersions.CV_305D) return list;

            text = text.ToLower().TrimStart(' ').TrimEnd(' ');

            for (int i = 0; i < _speech.Length; i++)
            {
                SpeechEntry entry = _speech[i];

                if (IsMatch(text, entry)) list.Add(entry);
            }

            list.Sort();

            return list;
        }
    }

    internal readonly struct SpeechEntry : IComparable<SpeechEntry>
    {
        public SpeechEntry(int id, string keyword)
        {
            KeywordID = (short) id;

            Keywords = keyword.Split(new[]
            {
                '*'
            }, StringSplitOptions.RemoveEmptyEntries);

            CheckStart = keyword.Length > 0 && keyword[0] == '*';
            CheckEnd = keyword.Length > 0 && keyword[keyword.Length - 1] == '*';
        }

        public string[] Keywords { get; }

        public short KeywordID { get; }

        public bool CheckStart { get; }

        public bool CheckEnd { get; }

        public int CompareTo(SpeechEntry obj)
        {
            if (KeywordID < obj.KeywordID)
                return -1;

            return KeywordID > obj.KeywordID ? 1 : 0;
        }
    }
}