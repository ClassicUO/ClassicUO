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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using ClassicUO.Utility;

namespace ClassicUO.IO.Resources
{
    public static class Cliloc
    {
        private static readonly Dictionary<int, StringEntry> _entries = new Dictionary<int, StringEntry>();

        public static void Load()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "Cliloc.enu");

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
        }

        public static string GetString(int number)
        {
            return GetEntry(number).Text;
        }

        public static StringEntry GetEntry(int number)
        {
            _entries.TryGetValue(number, out StringEntry res);

            return res;
        }

        public static string Translate(int baseCliloc, string arg = null, bool capitalize = false)
        {
            return Translate(GetString(baseCliloc), arg, capitalize);
        }

        public static string Translate(string baseCliloc, string arg = null, bool capitalize = false)
        {
            if (string.IsNullOrEmpty(baseCliloc))
                return string.Empty;

            if (string.IsNullOrEmpty(arg))
                return capitalize ? StringHelper.CapitalizeFirstCharacter(baseCliloc) : baseCliloc;

            string[] args = arg.Split(new[]
            {
               '\t'
            }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Length > 0 && args[i][0] == '#')
                {
                    args[i] = GetString(int.Parse(args[i].Substring(1)));
                }
            }

            string construct = baseCliloc;

            for (int i = 0; i < args.Length; i++)
            {
                int begin = construct.IndexOf('~', 0);
                int end = construct.IndexOf('~', begin + 1);

                if (begin != -1 && end != -1)
                    construct = construct.Substring(0, begin) + args[i] + construct.Substring(end + 1, construct.Length - end - 1);
                else
                    construct = baseCliloc;
            }

            construct = construct.Trim(' ');

            return capitalize ? StringHelper.CapitalizeFirstCharacter(construct) : construct;
        }
    }

    public readonly struct StringEntry
    {
        public StringEntry(int num, string text)
        {
            Number = num;
            Text = text;
        }

        public readonly int Number;
        public readonly string Text;
    }

    //[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4 + 1 + 2 + 20)]
    //internal unsafe readonly struct ClilocEntry
    //{
    //    public readonly int Number;
    //    public readonly byte Flag;
    //    public readonly ushort Length;
    //    //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    //    //public readonly char[] Name;
    //    public readonly char* Name;
    //}
}