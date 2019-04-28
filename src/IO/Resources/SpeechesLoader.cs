using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ClassicUO.IO.Resources
{
    internal class SpeechesLoader : ResourceLoader
    {
        private SpeechEntry[] _speech;

        public override unsafe void Load()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "speech.mul");

            if (!File.Exists(path))
                throw new FileNotFoundException();

            UOFileMul file = new UOFileMul(path, false);
            List<SpeechEntry> entries = new List<SpeechEntry>();

            while (file.Position < file.Length)
            {
                int id = (file.ReadByte() << 8) | file.ReadByte();
                int length = (file.ReadByte() << 8) | file.ReadByte();

                if (length > 0)
                {
                    entries.Add(new SpeechEntry(id, string.Intern(Encoding.UTF8.GetString((byte*) file.PositionAddress, length))));
                    file.Skip(length);
                }
            }

            _speech = entries.ToArray();
            file.Dispose();
        }

        protected override void CleanResources()
        {
            throw new NotImplementedException();
        }

        public bool IsMatch(string input, in SpeechEntry entry)
        {
            string[] split = entry.Keywords;

            for (int i = 0; i < split.Length; i++)
            {
                if (split[i].Length > 0 && split[i].Length <= input.Length)
                {
                    if (!entry.CheckStart)
                    {
                        if (input.IndexOf(split[i], 0) < 0)
                            continue;
                    }

                    if (!entry.CheckEnd)
                    {
                        if (input.IndexOf(split[i], input.Length - split[i].Length) < 0)
                            continue;
                    }

                    if (input.IndexOf(split[i]) >= 0)
                        return true;
                }
            }

            return false;
        }

        public SpeechEntry[] GetKeywords(string text)
        {
            if (FileManager.ClientVersion < ClientVersions.CV_305D)
            {
                return new SpeechEntry[0]
                {
                };
            }

            text = text.ToLower();
            List<SpeechEntry> list = new List<SpeechEntry>();

            for (int i = 0; i < _speech.Length; i++)
            {
                SpeechEntry entry = _speech[i];

                if (IsMatch(text, entry))
                    list.Add(entry);
            }

            list.Sort();

            return list.ToArray();
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