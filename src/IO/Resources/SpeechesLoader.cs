using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Data;

namespace ClassicUO.IO.Resources
{
    internal class SpeechesLoader : UOFileLoader
    {
        private static SpeechesLoader _instance;
        private SpeechEntry[] _speech;

        private SpeechesLoader()
        {
        }

        public static SpeechesLoader Instance => _instance ?? (_instance = new SpeechesLoader());

        public override unsafe Task Load()
        {
            return Task.Run
            (
                () =>
                {
                    string path = UOFileManager.GetUOFilePath("speech.mul");

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
                            entries.Add
                            (
                                new SpeechEntry
                                    (id, string.Intern(Encoding.UTF8.GetString((byte*) file.PositionAddress, length)))
                            );

                            file.Skip(length);
                        }
                    }

                    _speech = entries.ToArray();
                    file.Dispose();
                }
            );
        }

        public bool IsMatch(string input, in SpeechEntry entry)
        {
            string[] split = entry.Keywords;

            for (int i = 0; i < split.Length; i++)
            {
                if (split[i].Length > input.Length || split[i].Length == 0)
                {
                    continue;
                }

                if (!entry.CheckStart)
                {
                    if (input.IndexOf(split[i], 0, split[i].Length, StringComparison.InvariantCultureIgnoreCase) == -1)
                    {
                        continue;
                    }
                }

                if (!entry.CheckEnd)
                {
                    if (input.IndexOf
                        (split[i], input.Length - split[i].Length, StringComparison.InvariantCultureIgnoreCase) == -1)
                    {
                        continue;
                    }
                }

                if (input.IndexOf(split[i], StringComparison.InvariantCultureIgnoreCase) != -1)
                {
                    return true;
                }
            }

            return false;
        }

        public List<SpeechEntry> GetKeywords(string text)
        {
            List<SpeechEntry> list = new List<SpeechEntry>();

            if (Client.Version < ClientVersion.CV_305D)
            {
                return list;
            }

            text = text.TrimStart(' ').TrimEnd(' ');

            for (int i = 0; i < _speech.Length; i++)
            {
                SpeechEntry entry = _speech[i];

                if (IsMatch(text, in entry))
                {
                    list.Add(entry);
                }
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

            Keywords = keyword.Split
            (
                new[]
                {
                    '*'
                }, StringSplitOptions.RemoveEmptyEntries
            );

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
            {
                return -1;
            }

            return KeywordID > obj.KeywordID ? 1 : 0;
        }
    }
}