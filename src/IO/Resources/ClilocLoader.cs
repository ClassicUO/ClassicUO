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

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Utility;

namespace ClassicUO.IO.Resources
{
    internal class ClilocLoader : UOFileLoader
    {
        private static ClilocLoader _instance;
        private string _cliloc;
        private readonly Dictionary<int, string> _entries = new Dictionary<int, string>();

        private ClilocLoader()
        {
        }

        public static ClilocLoader Instance => _instance ?? (_instance = new ClilocLoader());

        public Task Load(string cliloc)
        {
            _cliloc = cliloc;

            if (!File.Exists(UOFileManager.GetUOFilePath(cliloc)))
            {
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
                        return;
                    }

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
                            {
                                buffer = new byte[(length + 1023) & ~1023];
                            }

                            reader.Read(buffer, 0, length);
                            string text = string.Intern(Encoding.UTF8.GetString(buffer, 0, length));

                            _entries[number] = text;
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

        public string Translate(int clilocNum, string arg = "", bool capitalize = false)
        {
            string baseCliloc = GetString(clilocNum);

            if (baseCliloc == null)
            {
                return null;
            }

            List<string> arguments = new List<string>();

            if (arg == null)
            {
                arg = "";
            }

            while (arg.Length != 0 && arg[0] == '\t')
            {
                arg = arg.Remove(0, 1);
            }

            for (int i = 0; i < arg.Length; i++)
            {
                if (arg[i] == '\t')
                {
                    arguments.Add(arg.Substring(0, i));
                    arg = arg.Substring(i + 1);
                    i = 0;
                }
            }

            bool has_arguments = arguments.Count != 0;

            arguments.Add(arg);

            //while (true)
            //{
            //    int pos = arg.IndexOf('\t');

            //    if (pos != -1)
            //    {
            //        arguments.Add(arg.Substring(0, pos));
            //        arg = arg.Substring(pos + 1);
            //    }
            //    else
            //    {
            //        arguments.Add(arg);

            //        break;
            //    }
            //}

            int index, pos = 0;

            while (pos < baseCliloc.Length)
            {
                pos = baseCliloc.IndexOf('~', pos);

                if (pos == -1)
                {
                    break;
                }

                int pos2 = baseCliloc.IndexOf('~', pos + 1);

                if (pos2 == -1) //non valid arg
                {
                    break;
                }

                index = baseCliloc.IndexOf('_', pos + 1, pos2 - (pos + 1));

                if (index <= pos)
                {
                    //there is no underscore inside the bounds, so we use all the part to get the number of argument
                    index = pos2;
                }

                int start = pos + 1;
                int max = index - start;
                int count = 0;

                for (; count < max; count++)
                {
                    if (!char.IsNumber(baseCliloc[start + count]))
                    {
                        break;
                    }
                }

                if (!int.TryParse(baseCliloc.Substring(start, count), out index))
                {
                    return $"MegaCliloc: error for {clilocNum}";
                }

                --index;

                string a = index < 0 || index >= arguments.Count ? string.Empty : arguments[index];

                if (a.Length > 1)
                {
                    if (a[0] == '#')
                    {
                        if (int.TryParse(a.Substring(1), out int id1))
                        {
                            arguments[index] = GetString(id1) ?? string.Empty;
                        }
                        else
                        {
                            arguments[index] = a;
                        }
                    }
                    else if (has_arguments && int.TryParse(a, out int clil))
                    {
                        if (_entries.TryGetValue(clil, out string value) && !string.IsNullOrEmpty(value))
                        {
                            arguments[index] = value;
                        }
                    }
                }

                baseCliloc = baseCliloc.Remove(pos, pos2 - pos + 1).Insert(pos, index >= arguments.Count ? string.Empty : arguments[index]);

                if (index >= 0 && index < arguments.Count)
                {
                    pos += arguments[index].Length;
                }
            }

            //for (int i = 0; i < arguments.Count; i++)
            //{
            //    int pos = baseCliloc.IndexOf('~');

            //    if (pos == -1)
            //        break;

            //    int pos2 = baseCliloc.IndexOf('~', pos + 1);

            //    if (pos2 == -1)
            //        break;

            //    string a = arguments[i];

            //    if (a.Length > 1 && a[0] == '#')
            //    {
            //        if (int.TryParse(a.Substring(1), out int id1))
            //            arguments[i] = GetString(id1) ?? string.Empty;
            //        else
            //            arguments[i] = a;
            //    }

            //    baseCliloc = baseCliloc.Remove(pos, pos2 - pos + 1).Insert(pos, arguments[i]);
            //}

            if (capitalize)
            {
                baseCliloc = StringHelper.CapitalizeAllWords(baseCliloc);
            }

            return baseCliloc;
        }
    }
}