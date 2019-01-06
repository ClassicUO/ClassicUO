using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Utility;

namespace ClassicUO.IO.Resources
{
    class ClilocLoader : ResourceLoader
    {
        private readonly Dictionary<int, StringEntry> _entries = new Dictionary<int, StringEntry>();

        public override void Load()
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

        protected override void CleanResources()
        {
            throw new NotImplementedException();
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

        public string Translate(int baseCliloc, string arg = null, bool capitalize = false)
        {
            return Translate(GetString(baseCliloc), arg, capitalize);
        }

        public string Translate(string baseCliloc, string arg = null, bool capitalize = false)
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
