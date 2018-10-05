#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ClassicUO.IO.Resources
{
    public static class SpecialKeywords
    {
        //private static UOFile _file;
        //private static readonly List<KeywordEntry> _keywords = new List<KeywordEntry>();


        //public static IReadOnlyList<KeywordEntry> Keywords => _keywords;


        private static readonly List<Dictionary<int, List<Regex>>> _speeches = new List<Dictionary<int, List<Regex>>>();


        public static void Load()
        {
            if (_speeches.Count > 0)
                return;

            string path = Path.Combine(FileManager.UoFolderPath, "speech.mul");
            if (!File.Exists(path))
                throw new FileNotFoundException();

            UOFileMul file = new UOFileMul(path);

            Dictionary<int, List<Regex>> table = null;
            int lastIndex = -1;

            while (file.Position < file.Length)
            {
                ushort id = (ushort) ((file.ReadByte() << 8) | file.ReadByte());
                ushort length = (ushort) ((file.ReadByte() << 8) | file.ReadByte());

                if (length > 128)
                    length = 128;

                string text = Encoding.UTF8.GetString(file.ReadArray<byte>(length)).Trim();

                if (text.Length == 0)
                    continue;

                if (table == null || lastIndex > id)
                {
                    if (id == 0 && text == "*withdraw*")
                        _speeches.Insert(0, table = new Dictionary<int, List<Regex>>());
                    else
                    {
                        _speeches.Add(table = new Dictionary<int, List<Regex>>());
                    }
                }

                lastIndex = id;

                table.TryGetValue(id, out List<Regex> regex);

                if (regex == null)
                    table[id] = regex = new List<Regex>();

                regex.Add(new Regex(text.Replace("*", @".*"), RegexOptions.IgnoreCase));


                //_keywords.Add(new KeywordEntry
                //    {Code = id, Text = Encoding.UTF8.GetString(_file.ReadArray<byte>(length)).Trim()});
            }

            file.Unload();
        }

        public static void GetSpeechTriggers(string text, string lang, out int count, out int[] triggers)
        {
            List<int> t = new List<int>();
            int speechTable = 0;


            foreach (KeyValuePair<int, List<Regex>> e in _speeches[speechTable])
            {
                for (int i = 0; i < e.Value.Count; i++)
                {
                    if (e.Value[i].IsMatch(text) && !t.Contains(e.Key))
                    {
                        t.Add(e.Key);
                    }
                }
            }

            count = t.Count;
            triggers = t.ToArray();
        }
    }

    //[StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct KeywordEntry
    {
        public ushort Code;
        public string Text;
    }
}