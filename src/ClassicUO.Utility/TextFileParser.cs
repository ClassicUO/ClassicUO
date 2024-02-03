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

using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Utility
{
    public class TextFileParser
    {
        private readonly char[] _delimiters, _comments, _quotes;
        private int _eol;
        private int _pos;
        private readonly StringBuilder _sb = new StringBuilder();
        private int _Size;
        private string _string;
        private bool _trim;

        public TextFileParser(string str, char[] delimiters, char[] comments, char[] quotes)
        {
            _delimiters = delimiters;
            _comments = comments;
            _quotes = quotes;
            _Size = str.Length;
            _string = str;
        }

        public void Restart()
        {
            _pos = 0;
        }

        public bool IsDelimiter()
        {
            bool result = false;

            for (int i = 0; i < _delimiters.Length && !result; i++)
            {
                result = _string[_pos] == _delimiters[i];
            }

            return result;
        }

        public bool IsEOF()
        {
            return _pos >= _Size;
        }

        private void GetEOL()
        {
            for (int i = _pos; i < _Size; i++)
            {
                if (_string[i] == '\n')
                {
                    _eol = i;

                    break;
                }
                else if (i + 1 >= _Size)
                {
                    _eol = i + 1;

                    break;
                }
            }
        }

        private void SkipToData()
        {
            while (_pos < _eol && IsDelimiter())
            {
                _pos++;
            }
        }

        private bool IsComment()
        {
            bool result = _string[_pos] == '\n';

            for (int i = 0; i < _comments.Length && !result; i++)
            {
                result = _string[_pos] == _comments[i];

                if (result && i + 1 < _comments.Length && _comments[i] == _comments[i + 1] && _pos + 1 < _eol)
                {
                    result = _string[_pos] == _string[_pos + 1];
                    i++;
                }
            }

            return result;
        }

        private bool IsQuote()
        {
            bool result = _string[_pos] == '\n';

            for (int i = 0; i < _quotes.Length && !result; i += 2)
            {
                if (_string[_pos] == _quotes[i] || i + 1 < _quotes.Length && _string[_pos] == _quotes[i + 1])
                {
                    result = true;

                    break;
                }
            }

            return result;
        }

        private bool IsSecondQuote()
        {
            bool result = _string[_pos] == '\n';

            for (int i = 0; i + 1 < _quotes.Length && !result; i += 2)
            {
                if (_string[_pos] == _quotes[i + 1])
                {
                    result = true;

                    break;
                }
            }

            return result;
        }

        private void ObtainData()
        {
            while (_pos < _Size && _string[_pos] != '\n')
            {
                if (IsDelimiter())
                {
                    break;
                }

                if (IsComment())
                {
                    _pos = _eol;

                    break;
                }

                if (_string[_pos] != '\r' && (!_trim || _string[_pos] != ' ' && _string[_pos] != '\t'))
                {
                    for (int i = 0; i < _quotes.Length; i++)
                    {
                        if (_string[_pos] == _quotes[i])
                        {
                            return;
                        }
                    }

                    _sb.Append(_string[_pos]);
                }

                _pos++;
            }
        }

        private void ObtainQuotedData(bool save = true)
        {
            bool exit = false;

            for (int i = 0; i < _quotes.Length; i += 2)
            {
                if (_string[_pos] == _quotes[i])
                {
                    char endQuote = _quotes[i + 1];
                    exit = true;

                    int pos = _pos + 1;
                    int start = pos;

                    while (pos < _eol && _string[pos] != '\n' && _string[pos] != endQuote)
                    {
                        if (_string[pos] == _quotes[i]) // another {
                        {
                            _pos = pos;
                            ObtainQuotedData(false); // skip
                            pos = _pos;
                        }

                        pos++;
                    }

                    _pos++;
                    int size = pos - start;

                    if (size > 0)
                    {
                        if (save)
                        {
                            ReadOnlySpan<char> span = _string.AsSpan(start, size);
                            int idx0 = span.IndexOf('\r');
                            int idx1 = span.IndexOf('\n');

                            if (idx0 >= 0)
                            {
                                span = span.Slice(start, idx0);
                            }
                            else if (idx1 >= 0)
                            {
                                span = span.Slice(start, idx1);
                            }

                            unsafe
                            {
                                fixed (char* ptr = span)
                                {
                                    _sb.Append(ptr, span.Length);
                                }
                            }
                        }

                        _pos = pos;

                        if (_pos < _eol && _string[_pos] == endQuote)
                        {
                            _pos++;
                        }
                    }

                    break;
                }
            }

            if (!exit)
            {
                ObtainData();
            }
        }

        public List<string> ReadTokens(bool trim = true)
        {
            _trim = trim;
            List<string> result = new List<string>();

            if (_pos < _Size)
            {
                GetEOL();

                while (_pos < _eol)
                {
                    SkipToData();

                    if (_pos >= _eol)
                    {
                        break;
                    }

                    if (IsComment())
                    {
                        break;
                    }

                    ObtainQuotedData();

                    if (_sb.Length > 0)
                    {
                        result.Add(_sb.ToString());
                        _sb.Clear();
                    }
                    else if (IsSecondQuote())
                    {
                        _pos++;
                    }
                }

                _pos = _eol + 1;
            }

            return result;
        }

        public List<string> GetTokens(string str, bool trim = true)
        {
            _trim = trim;
            List<string> result = new List<string>();

            _pos = 0;
            _string = str;
            _eol = _Size = str.Length;

            while (_pos < _eol)
            {
                SkipToData();

                if (_pos >= _eol)
                {
                    break;
                }

                if (IsComment())
                {
                    break;
                }

                ObtainQuotedData();

                if (_sb.Length > 0)
                {
                    result.Add(_sb.ToString());
                    _sb.Clear();
                }
            }

            return result;
        }
    }
}