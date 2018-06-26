using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ClassicUO.Assets
{
    public static class BodyDef
    {
        private static Dictionary<int, BodyTableEntry> _entries;

        public static BodyTableEntry Get(int i)
        {
            _entries.TryGetValue(i, out var r);
            return r;
        }

        public static void Load()
        {
            _entries = new Dictionary<int, BodyTableEntry>();

            string path = Path.Combine(FileManager.UoFolderPath, "body.def");
            if (!File.Exists(path))
                return;

            using (StreamReader reader = new StreamReader(path))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Length == 0 || line[0] == '#')
                        continue;

                    int index1 = line.IndexOf("{");
                    int index2 = line.IndexOf("}");

                    string origBody = line.Substring(0, index1);
                    string newBody = line.Substring(index1 + 1, index2 - index1 - 1);
                    string newHue = line.Substring(index2 + 1);

                    int indexOf = newBody.IndexOf(',');
                    if (indexOf > -1)
                        newBody = newBody.Substring(0, indexOf).Trim();

                    int iParam1 = Convert.ToInt32(origBody);
                    int iParam2 = Convert.ToInt32(newBody);
                    int iParam3 = Convert.ToInt32(newHue);

                    _entries[iParam1] = new BodyTableEntry()
                    {
                        OldBody = iParam1, NewBody = iParam2, NewHue = iParam3
                    };
                }
            }
        }

        public static bool Translate(ref int body, ref int hue)
        {
            if (_entries.TryGetValue(body, out var value))
            {
                body = value.NewBody;
                if (hue == 0)
                    hue = value.NewHue;
                return true;
            }
            return false;
        }
    }

    public struct BodyTableEntry
    {
        public int OldBody { get; set; }
        public int NewBody { get; set; }
        public int NewHue { get; set; }
    }
}
