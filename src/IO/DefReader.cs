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
using System.Globalization;
using System.IO;
using System.Linq;
using ClassicUO.Utility.Logging;

namespace ClassicUO.IO
{
    internal class DefReader : IDisposable
    {
        private const char COMMENT = '#';

        private static readonly char[] _tokens =
        {
            '\t', ' '
        };

        private static readonly char[] _tokensGroup =
        {
            ',', ' ', '{', '}'
        };
        private readonly string _file;
        private List<string[]> _groups = new List<string[]>();

        private readonly int _minSize;
        private List<string[]> _parts = new List<string[]>();

        private StreamReader _reader;

        public DefReader(string file, int minsize = 2)
        {
            _file = file;
            _reader = new StreamReader(File.OpenRead(file));
            Line = -1;
            Position = 0;
            _minSize = minsize;
            Parse();
        }

        public int Line { get; private set; }
        public int Position { get; private set; }
        public int LinesCount => _parts.Count;

        public int PartsCount => _parts[Line].Length;

        private bool IsEOF => Line + 1 >= LinesCount;


        public void Dispose()
        {
            if (_reader == null)
            {
                return;
            }

            _reader.Dispose();
            _reader = null;
            _parts = null;
            _groups = null;
        }

        public bool Next()
        {
            if (!IsEOF)
            {
                Line++;
                Position = 0;

                return true;
            }

            return false;
        }

        private void Parse()
        {
            if (_parts.Count > 0)
            {
                _parts.Clear();
            }

            if (_groups.Count > 0)
            {
                _groups.Clear();
            }

            string line;

            while ((line = _reader.ReadLine()) != null)
            {
                line = line.Trim();

                if (line.Length <= 0 || line[0] == COMMENT || !char.IsNumber(line[0]))
                {
                    continue;
                }

                int comment = line.IndexOf('#');

                if (comment >= 0)
                {
                    line = line.Substring(0, comment);
                }

                int groupStart = line.IndexOf('{');
                int groupEnd = line.IndexOf('}');

                string[] p;

                if (groupStart >= 0 && groupEnd >= 0)
                {
                    string[] firstPart = line.Substring(0, groupStart).Split(_tokens, StringSplitOptions.RemoveEmptyEntries);

                    string group = line.Substring(groupStart, groupEnd - groupStart + 1);

                    string[] lastPart = line.Substring(groupEnd + 1, line.Length - groupEnd - 1).Split(_tokens, StringSplitOptions.RemoveEmptyEntries);

                    p = firstPart.Concat(new[] { group }).Concat(lastPart).ToArray();
                }
                else
                {
                    p = line.Split(_tokens, StringSplitOptions.RemoveEmptyEntries);
                }

                if (p.Length >= _minSize)
                {
                    _parts.Add(p);
                }
            }
        }

        private string[] GetTokensAtLine(int line)
        {
            if (line >= _parts.Count || line < 0)
            {
                Log.Error($"Index out of range [Line: {line}]. Returned '0'");

                return new[] { "0" };
            }

            return _parts[line];
        }


        private string TokenAt(int line, int index)
        {
            string[] p = GetTokensAtLine(line);

            if (index >= p.Length || index < 0)
            {
                Log.Error($"Index out of range [Line: {line}]. Returned '0'");

                return "0";
            }

            return p[index];
        }

        public int ReadInt()
        {
            return ReadInt(Line, Position++);
        }

        public int ReadGroupInt(int index = 0)
        {
            if (!TryReadGroup(TokenAt(Line, Position++), out string[] group))
            {
                throw new Exception("It's not a group");
            }

            if (index >= group.Length)
            {
                throw new IndexOutOfRangeException();
            }

            SanitizeStringNumber(ref group[index]);

            return int.Parse(group[index]);
        }

        public int[] ReadGroup()
        {
            string s = TokenAt(Line, Position++);

            if (s.Length > 0)
            {
                if (s[0] == '{')
                {
                    if (s[s.Length - 1] == '}')
                    {
                        List<int> results = new List<int>();

                        string[] splitRes = s.Split(_tokensGroup, StringSplitOptions.RemoveEmptyEntries);

                        for (int i = 0; i < splitRes.Length; i++)
                        {
                            if (!string.IsNullOrEmpty(splitRes[i]) && char.IsNumber(splitRes[i][0]))
                            {
                                NumberStyles style = NumberStyles.Any;

                                if (splitRes[i].Length > 1 && splitRes[i][0] == '0' && splitRes[i][1] == 'x')
                                {
                                    style = NumberStyles.HexNumber;
                                }

                                if (int.TryParse(splitRes[i], style, null, out int res))
                                {
                                    results.Add(res);
                                }
                            }
                        }

                        return results.ToArray();
                    }

                    Log.Error($"Missing }} at line {Line + 1}, in '{_file}'");
                }
            }

            return null;
        }

        private static bool TryReadGroup(string s, out string[] group)
        {
            if (s.Length > 0)
            {
                if (s[0] == '{')
                {
                    if (s[s.Length - 1] == '}')
                    {
                        group = s.Split(_tokensGroup, StringSplitOptions.RemoveEmptyEntries);

                        return true;
                    }
                }
            }

            group = null;

            return false;
        }

        private int ReadInt(int line, int index)
        {
            string token = TokenAt(line, index);

            if (!string.IsNullOrEmpty(token))
            {
                SanitizeStringNumber(ref token);

                return token.StartsWith("0x") ? int.Parse(token.Remove(0, 2), NumberStyles.HexNumber) : int.Parse(token);
            }

            return -1;
        }

        private static void SanitizeStringNumber(ref string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return;
            }

            for (int i = 0; i < token.Length; ++i)
            {
                char c = token[i];

                if (!char.IsNumber(c) && c != '-' && c != '+')
                {
                    token = token.Substring(0, i);

                    break;
                }
            }
        }
    }
}