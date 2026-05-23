// SPDX-License-Identifier: BSD-2-Clause

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
                    return;
                }
            }

            _eol = _Size;
        }

        private void SkipToData()
        {
            while (_pos < _eol && IsDelimiter(_string[_pos]))
                _pos++;
        }

        private bool IsDelimiter(char c)
        {
            foreach (char d in _delimiters)
                if (c == d)
                    return true;
            return false;
        }

        private bool IsComment()
        {
            foreach (char c in _comments)
                if (_string[_pos] == c)
                    return true;
            return false;
        }

        private bool TryGetQuotePair(out char startQuote, out char endQuote)
        {
            for (int i = 0; i + 1 < _quotes.Length; i += 2)
            {
                if (_string[_pos] == _quotes[i])
                {
                    startQuote = _quotes[i];
                    endQuote = _quotes[i + 1];
                    return true;
                }
            }

            startQuote = endQuote = '\0';
            return false;
        }

        private void ObtainQuotedData(char endQuote, bool areTheSame)
        {
            _pos++; // skip opening quote

            while (_pos < _eol)
            {
                if (_string[_pos] == endQuote)
                {
                    if (!areTheSame)
                        _pos++; // skip end quote
                    return;
                }

                _sb.Append(_string[_pos]);
                _pos++;
            }
        }

        private void ObtainUnquotedData()
        {
            while (_pos < _eol)
            {
                if (IsDelimiter(_string[_pos]) || IsComment() || TryGetQuotePair(out _, out _))
                    return;

                _sb.Append(_string[_pos]);
                _pos++;
            }
        }

        public List<string> ReadTokens(bool trim = true)
        {
            _trim = trim;
            List<string> result = new List<string>();

            if (_pos >= _Size)
                return result;

            GetEOL(); // sets _eol to the end of the current line
            SkipToData();

            while (_pos < _eol)
            {
                if (IsComment())
                    break;

                if (TryGetQuotePair(out char start, out char end))
                {
                    ObtainQuotedData(end, start == end);
                }
                else
                {
                    ObtainUnquotedData();
                }

                if (_sb.Length > 0)
                {
                    string token = _sb.ToString();
                    if (trim)
                        token = token.Trim();

                    if (token.Length > 0)
                        result.Add(token);

                    _sb.Clear();
                }

                SkipToData();
            }

            _pos = _eol + 1; // move to next line
            return result;
        }

        public List<string> GetTokens(string str, bool trim = true)
        {
            _string = str;
            _Size = str.Length;
            _pos = 0;

            List<string> result = new List<string>();

            while (_pos < _Size)
            {
                List<string> lineTokens = ReadTokens(trim);
                result.AddRange(lineTokens);
            }

            return result;
        }

    }
}
