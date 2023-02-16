#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

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
                            entries.Add(new SpeechEntry(id, string.Intern(Encoding.UTF8.GetString((byte*) file.PositionAddress, length))));

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
            //string[] words = input.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

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
                    if (input.IndexOf(split[i], input.Length - split[i].Length, StringComparison.InvariantCultureIgnoreCase) == -1)
                    {
                        continue;
                    }
                }

                int idx = input.IndexOf(split[i], StringComparison.InvariantCultureIgnoreCase);
                while (idx >= 0)
                {
                    // "bank" or " bank" or "bank " or " bank " or "!bank" or "bank!"
                    if ((idx - 1 < 0 || char.IsWhiteSpace(input[idx - 1]) || !char.IsLetter(input[idx - 1])) && 
                        (idx + split[i].Length >= input.Length || char.IsWhiteSpace(input[idx + split[i].Length]) || !char.IsLetter(input[idx + split[i].Length]) ))
                    {
                        return true;
                    }

                    

                    idx = input.IndexOf(split[i], idx + 1, StringComparison.InvariantCultureIgnoreCase);
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
                },
                StringSplitOptions.RemoveEmptyEntries
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