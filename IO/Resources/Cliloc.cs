using ClassicUO.IO;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ClassicUO.IO.Resources
{
    public static class Cliloc
    {
        private static Dictionary<int, StringEntry> _entries;


        public static void Load()
        {
            _entries = new Dictionary<int, StringEntry>();

            string path = Path.Combine(FileManager.UoFolderPath, "Cliloc.ENU");
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
                    string text = Encoding.UTF8.GetString(buffer, 0, length);

                    StringEntry entry = new StringEntry(number, text);
                    _entries[number] = entry;
                }
            }
        }

        public static string GetString(int number)
        {
            StringEntry e = GetEntry(number);
            return e.Text;
        }

        public static StringEntry GetEntry(int number)
        {
            _entries.TryGetValue(number, out StringEntry res);
            return res;
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


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct ClilocEntry
    {
        public readonly int Number;
        public readonly byte Flag;
        public readonly ushort Length;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public readonly char[] Name;
    }
}