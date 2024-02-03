#region license

// Copyright (c) 2024, andreakarasho
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

using ClassicUO.IO;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public class ClilocLoader : UOFileLoader
    {
        private static ClilocLoader _instance;
        private string _cliloc;
        private readonly Dictionary<int, string> _entries = new Dictionary<int, string>();

        private ClilocLoader()
        {
        }

        public static ClilocLoader Instance => _instance ?? (_instance = new ClilocLoader());

        public Task Load(string lang)
        {
            if (string.IsNullOrEmpty(lang))
            {
                lang = "enu";
            }

            _cliloc = $"Cliloc.{lang}";
            Log.Trace($"searching for: '{_cliloc}'");

            if (!File.Exists(UOFileManager.GetUOFilePath(_cliloc)))
            {
                Log.Warn($"'{_cliloc}' not found. Rolled back to Cliloc.enu");

                _cliloc = "Cliloc.enu";
            }

            return Load();
        }

        public override Task Load()
        {
            return Task.Run
            (
                () =>
                {
                    if (string.IsNullOrEmpty(_cliloc))
                    {
                        _cliloc = "Cliloc.enu";
                    }

                    string path = UOFileManager.GetUOFilePath(_cliloc);

                    if (!File.Exists(path))
                    {
                        Log.Error($"cliloc not found: '{path}'");
                        return;
                    }

                    if (string.Compare(_cliloc, "cliloc.enu", StringComparison.InvariantCultureIgnoreCase) != 0)
                    { 
                        string enupath = UOFileManager.GetUOFilePath("Cliloc.enu");

                        using (BinaryReader reader = new BinaryReader(new FileStream(enupath, FileMode.Open, FileAccess.Read)))
                        {
                            reader.ReadInt32();
                            reader.ReadInt16();

                            byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(1024);

                            try
                            {
                                while (reader.BaseStream.Length != reader.BaseStream.Position)
                                {
                                    int number = reader.ReadInt32();
                                    byte flag = reader.ReadByte();
                                    int length = reader.ReadInt16();

                                    if (length > buffer.Length)
                                    {
                                        System.Buffers.ArrayPool<byte>.Shared.Return(buffer);

                                        buffer = System.Buffers.ArrayPool<byte>.Shared.Rent((length + 1023) & ~1023);
                                    }

                                    reader.Read(buffer, 0, length);
                                    string text = string.Intern(Encoding.UTF8.GetString(buffer, 0, length));

                                    _entries[number] = text;
                                }
                            }
                            finally
                            {
                                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                            }
                        }
                    }

                    using (BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open, FileAccess.Read)))
                    {
                        reader.ReadInt32();
                        reader.ReadInt16();
                        byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(1024);

                        try
                        {
                            while (reader.BaseStream.Length != reader.BaseStream.Position)
                            {
                                int number = reader.ReadInt32();
                                byte flag = reader.ReadByte();
                                int length = reader.ReadInt16();

                                if (length > buffer.Length)
                                {
                                    System.Buffers.ArrayPool<byte>.Shared.Return(buffer);

                                    buffer = System.Buffers.ArrayPool<byte>.Shared.Rent((length + 1023) & ~1023);
                                }

                                reader.Read(buffer, 0, length);
                                string text = string.Intern(Encoding.UTF8.GetString(buffer, 0, length));

                                _entries[number] = text;
                            }
                        }
                        finally
                        {
                            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                        }
                    }
                }
            );
        }

        public override void ClearResources()
        {
            _entries.Clear();
        }

        public string GetString(int number)
        {
            _entries.TryGetValue(number, out string text);

            return text;
        }

        public string GetString(int number, string replace)
        {
            string s = GetString(number);

            if (string.IsNullOrEmpty(s))
            {
                s = replace;
            }

            return s;
        }

        public string GetString(int number, bool camelcase, string replace = "")
        {
            string s = GetString(number);

            if (string.IsNullOrEmpty(s) && !string.IsNullOrEmpty(replace))
            {
                s = replace;
            }

            if (camelcase && !string.IsNullOrEmpty(s))
            {
                s = StringHelper.CapitalizeAllWords(s);
            }

            return s;
        }

        public unsafe string Translate(int clilocNum, string arg = "", bool capitalize = false)
        {
            string baseCliloc = GetString(clilocNum);

            if (baseCliloc == null)
            {
                return null;
            }

            if (arg == null)
            {
                arg = "";
            }

            var roChars = arg.AsSpan();


            // get count of valid args
            int i = 0;
            int totalArgs = 0;
            int trueStart = -1;

            for (; i < roChars.Length; ++i)
            {
                if (roChars[i] != '\t')
                {
                    if (trueStart == -1)
                    {
                        trueStart = i;
                    }
                }
                else if (trueStart >= 0)
                {
                    ++totalArgs;
                }
            }

            if (trueStart == -1)
            {
                trueStart = 0;
            }

            // store index locations
            Span<(int, int)> locations = stackalloc (int, int)[++totalArgs];
            i = trueStart;
            for (int j = 0; i < roChars.Length; ++i)
            {
                if (roChars[i] == '\t')
                {
                    locations[j].Item1 = trueStart;
                    locations[j].Item2 = i;

                    trueStart = i + 1;

                    ++j;
                }
            }

            bool has_arguments = totalArgs - 1 > 0;

            locations[totalArgs - 1].Item1 = trueStart;
            locations[totalArgs - 1].Item2 = i;

            ValueStringBuilder sb = new ValueStringBuilder(baseCliloc.AsSpan());
            {
                int index, pos = 0;

                while (pos < sb.Length)
                {
                    int poss = pos;
                    pos = sb.RawChars.Slice(pos, sb.Length - pos).IndexOf('~');

                    if (pos == -1)
                    {
                        break;
                    }

                    pos += poss;

                    int pos2 = sb.RawChars.Slice(pos + 1, sb.Length - (pos + 1)).IndexOf('~');

                    if (pos2 == -1) //non valid arg
                    {
                        break;
                    }

                    pos2 += pos + 1;

                    index = sb.RawChars.Slice(pos + 1, pos2 - (pos + 1)).IndexOf('_');

                    if (index == -1)
                    {
                        //there is no underscore inside the bounds, so we use all the part to get the number of argument
                        index = pos2;
                    }
                    else
                    {
                        index += pos + 1;
                    }

                    int start = pos + 1;
                    int max = index - start;
                    int count = 0;

                    for (; count < max; count++)
                    {
                        if (!char.IsNumber(sb.RawChars[start + count]))
                        {
                            break;
                        }
                    }

                    if (!int.TryParse(sb.RawChars.Slice(start, count).ToString(), out index))
                    {
                        return $"MegaCliloc: error for {clilocNum}";
                    }

                    --index;

                    var a = index < 0 || index >= totalArgs ? string.Empty.AsSpan() : arg.AsSpan().Slice(locations[index].Item1, locations[index].Item2 - locations[index].Item1);

                    if (a.Length > 1)
                    {
                        if (a[0] == '#')
                        {
                            if (int.TryParse(a.Slice(1).ToString(), out int id1))
                            {
                                var ss = GetString(id1);

                                if (string.IsNullOrEmpty(ss))
                                {
                                    a = string.Empty.AsSpan();
                                }
                                else
                                {
                                    a = ss.AsSpan();
                                }
                            }
                        }
                        else if (has_arguments && int.TryParse(a.ToString(), out int clil))
                        {
                            if (_entries.TryGetValue(clil, out string value) && !string.IsNullOrEmpty(value))
                            {
                                a = value.AsSpan();
                            }
                        }
                    }

                    sb.Remove(pos, pos2 - pos + 1);
                    sb.Insert(pos, a);

                    if (index >= 0 && index < totalArgs)
                    {
                        pos += a.Length /*locations[index].Y - locations[index].X*/;
                    }
                }

                baseCliloc = sb.ToString();

                sb.Dispose();

                if (capitalize)
                {
                    baseCliloc = StringHelper.CapitalizeAllWords(baseCliloc);
                }

                return baseCliloc;
            }
        }
    }
}
