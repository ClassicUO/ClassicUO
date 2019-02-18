using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using ClassicUO.IO;

namespace ClassicUO.Utility
{
    internal class TextFileParser
    {
        private readonly char[] _delimiters, _comments, _quotes;
        private readonly string _string;
        private readonly long _Size;
        private bool _trim;
        private int _pos = 0;
        private int _eol;

        public TextFileParser(FileInfo file, char[] delimiters, char[] comments, char[] quotes)
        {
            if (file.Length > 0x100000)//1megabyte limit of string file
                throw new InternalBufferOverflowException($"{file.FullName} exceeds the maximum 1Megabyte allowed size for a string text file, please, check that the file is correct and not corrupted -> {file.Length} file size");

            _delimiters = delimiters;
            _comments = comments;
            _quotes = quotes;
            _Size = file.Length;
            _string = File.ReadAllText(file.FullName);
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
            return _pos >= _string.Length;
        }

        private void GetEOL()
        {
            for(int i = _pos; i < _string.Length; i++)
            {
                if (_string[i] == '\n')
                    _eol = i;
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
                if (_string[_pos] == _quotes[i] || (i + 1 < _quotes.Length && _string[_pos] == _quotes[i + 1]))
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

            while (_pos < _string.Length && _string[_pos] != '\n')
            {
                if (IsDelimiter())
                    break;

                else if (IsComment())
                {
                    _pos = _eol;
                    break;
                }

                if (_string[_pos] != '\r' && (!_trim || (_string[_pos] != ' ' && _string[_pos] != '\t')))
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

        private string RawLine;
        void SaveRawLine()
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

            if (_pos < _string.Length)
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
    }
}
