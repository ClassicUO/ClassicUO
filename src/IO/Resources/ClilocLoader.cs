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

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Utility;

namespace ClassicUO.IO.Resources
{
    internal class ClilocLoader : ResourceLoader
    {
        private readonly Dictionary<int, StringEntry> _entries = new Dictionary<int, StringEntry>();
        private string _cliloc;

        public Task Load(string cliloc)
        {
            _cliloc = cliloc;

            if (!File.Exists(Path.Combine(FileManager.UoFolderPath, cliloc)))
                _cliloc = "Cliloc.enu";

            return Load();
        }

        public override Task Load()
        {
            return Task.Run(() => {
                if (string.IsNullOrEmpty(_cliloc))
                    _cliloc = "Cliloc.enu";

                string path = Path.Combine(FileManager.UoFolderPath, _cliloc);

                if (!File.Exists(path))
                    return;

                using (BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open, FileAccess.Read)))
                {
                    reader.ReadInt32();
                    reader.ReadInt16();
                    byte[] buffer = new byte[1024];

                    while (reader.BaseStream.Length != reader.BaseStream.Position)
                    {
                        int number = reader.ReadInt32();
                        byte flag = reader.ReadByte();
                        int length = reader.ReadInt16();

                        if (length > buffer.Length)
                            buffer = new byte[(length + 1023) & ~1023];
                        reader.Read(buffer, 0, length);
                        string text = string.Intern(Encoding.UTF8.GetString(buffer, 0, length));

                        _entries[number] = new StringEntry(number, text);
                    }
                }
            });
          
        }

        public override void CleanResources()
        {
        }

        public string GetString(int number)
        {
            return GetEntry(number).Text;
        }

        public StringEntry GetEntry(int number)
        {
            _entries.TryGetValue(number, out StringEntry res);

            return res;
        }

        public string Translate(int baseCliloc, string arg = "", bool capitalize = false)
        {
            return Translate(GetString(baseCliloc), arg, capitalize);
        }

        public string Translate(string baseCliloc, string arg = "", bool capitalize = false)
        {
            if (baseCliloc == null)
                return null;


            while (arg.Length != 0 && arg[0] == '\t')
                arg = arg.Remove(0, 1);

            List<string> arguments = new List<string>();

            while (true)
            {
                int pos = arg.IndexOf('\t');

                if (pos != -1)
                {
                    arguments.Add(arg.Substring(0, pos));
                    arg = arg.Substring(pos + 1);
                }
                else
                {
                    arguments.Add(arg);

                    break;
                }
            }

            for (int i = 0; i < arguments.Count; i++)
            {
                int pos = baseCliloc.IndexOf('~');

                if (pos == -1)
                    break;

                int pos2 = baseCliloc.IndexOf('~', pos + 1);

                if (pos2 == -1)
                    break;

                string a = arguments[i];

                if (a.Length > 1 && a[0] == '#')
                {
                    if (int.TryParse(a.Substring(1), out int id1))
                        arguments[i] = GetString(id1) ?? string.Empty;
                    else
                        arguments[i] = a;
                }

                baseCliloc = baseCliloc.Remove(pos, pos2 - pos + 1).Insert(pos, arguments[i]);
            }

            if (capitalize)
                baseCliloc = StringHelper.CapitalizeAllWords(baseCliloc);

            return baseCliloc;
        }
    }

    internal readonly struct StringEntry
    {
        public StringEntry(int num, string text)
        {
            Number = num;
            Text = text;
        }

        public readonly int Number;
        public readonly string Text;
    }
}