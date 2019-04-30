﻿using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Utility
{
    internal class TextFileParser
    {
        private readonly char[] _delimiters, _comments, _quotes;
        private int _eol;
        private int _pos;
        private int _Size;
        private string _string;
        private bool _trim;

        private string RawLine;

        public TextFileParser(string str, char[] delimiters, char[] comments, char[] quotes)
        {
            _delimiters = delimiters;
            _comments = comments;
            _quotes = quotes;
            _Size = str.Length;
            _string = str;
        }

        internal void Restart()
        {
            _pos = 0;
        }

        internal bool IsDelimiter()
        {
            bool result = false;

            for (int i = 0; i < _delimiters.Length && !result; i++)
                result = _string[_pos] == _delimiters[i];

            return result;
        }

        internal bool IsEOF()
        {
            return _pos >= _Size;
        }

        private void GetEOL()
        {
            for (int i = _pos; i < _Size; i++)
            {
                if (_string[i] == '\n' || i + 1 >= _Size)
                {
                    _eol = i;

                    break;
                }
            }
        }

        private void SkipToData()
        {
            while (_pos < _eol && IsDelimiter())
                _pos++;
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

        private string ObtainData()
        {
            StringBuilder result = new StringBuilder();

            while (_pos < _Size && _string[_pos] != '\n')
            {
                if (IsDelimiter())
                    break;

                if (IsComment())
                {
                    _pos = _eol;

                    break;
                }

                if (_string[_pos] != '\r' && (!_trim || _string[_pos] != ' ' && _string[_pos] != '\t'))
                    result.Append(_string[_pos]);

                _pos++;
            }

            return result.ToString();
        }

        private string ObtainQuotedData()
        {
            bool exit = false;
            string result = "";

            for (int i = 0; i < _quotes.Length; i += 2)
            {
                if (_string[_pos] == _quotes[i])
                {
                    char endQuote = _quotes[i + 1];
                    exit = true;

                    _pos++;
                    int pos = _pos;

                    while (pos < _eol && _string[pos] != '\n' && _string[pos] != endQuote)
                        pos++;

                    int size = pos - _pos;

                    if (size > 0)
                    {
                        result = _string.Substring(_pos, size).TrimEnd('\r', '\n');
                        _pos = pos;

                        if (_pos < _eol && _string[_pos] == endQuote)
                            _pos++;
                    }

                    break;
                }
            }

            if (!exit)
                result = ObtainData();

            return result;
        }

        private void SaveRawLine()
        {
            int size = _eol - _pos;

            if (size > 0)
                RawLine = _string.Substring(_pos, size).TrimEnd('\r', '\n');
            else
                RawLine = "";
        }

        internal List<string> ReadTokens(bool trim = true)
        {
            _trim = trim;
            List<string> result = new List<string>();

            if (_pos < _Size)
            {
                GetEOL();

                SaveRawLine();

                while (_pos < _eol)
                {
                    SkipToData();

                    if (IsComment())
                        break;

                    string buf = ObtainQuotedData();

                    if (buf.Length > 0)
                        result.Add(buf);
                    else if (IsSecondQuote())
                        _pos++;
                }

                _pos = _eol + 1;
            }

            return result;
        }

        internal List<string> GetTokens(string str, bool trim = true)
        {
            _trim = trim;
            List<string> result = new List<string>();

            _pos = 0;
            _string = str;
            _Size = str.Length;
            _eol = _Size - 1;

            SaveRawLine();

            while (_pos < _eol)
            {
                SkipToData();

                if (IsComment())
                    break;

                string buf = ObtainQuotedData();

                if (buf.Length > 0)
                    result.Add(buf);
            }

            return result;
        }
    }
}