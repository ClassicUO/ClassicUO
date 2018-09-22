#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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

namespace ClassicUO.IO.Resources
{
    public static class SpecialKeywords
    {
        private static UOFile _file;
        private static readonly List<KeywordEntry> _keywords = new List<KeywordEntry>();


        public static IReadOnlyList<KeywordEntry> Keywords => _keywords;

        public static void Load()
        {
            if (_keywords.Count > 0)
                return;

            string path = Path.Combine(FileManager.UoFolderPath, "speech.mul");
            if (!File.Exists(path))
                throw new FileNotFoundException();

            _file = new UOFileMul(path);

            while (_file.Position < _file.Length)
            {
                ushort id = (ushort) ((_file.ReadByte() << 8) | _file.ReadByte());
                ushort length = (ushort) ((_file.ReadByte() << 8) | _file.ReadByte());

                if (length > 128)
                    length = 128;

                _keywords.Add(new KeywordEntry
                    {Code = id, Text = Encoding.UTF8.GetString(_file.ReadArray<byte>(length))});
            }
        }
    }

    //[StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct KeywordEntry
    {
        public ushort Code;
        public string Text;
    }
}