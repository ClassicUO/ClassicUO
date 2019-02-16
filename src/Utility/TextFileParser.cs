using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using ClassicUO.IO;

namespace ClassicUO.Utility
{
    internal class TextFileParser
    {
        private readonly char[] _Delimiters, _Comments, _Quotes;
        private string _String;
        private readonly long _Size;
        private bool _Trim;
        private int _Pos = 0;
        private int _EOL;

        public TextFileParser(FileInfo file, char[] delimiters, char[] comments, char[] quotes)
        {
            _Delimiters = delimiters;
            _Comments = comments;
            _Quotes = quotes;
            _Size = file.Length;
            _String = File.ReadAllText(file.FullName);
        }

        internal void Restart()
        {
            _Pos = 0;
        }

        internal bool IsDelimiter()
        {
            bool result = false;

            for (int i = 0; i < _Delimiters.Length && !result; i++)
                result = _String[_Pos] == _Delimiters[i];
            return result;
        }

        private void GetEOL()
        {
            for(int i = _Pos; i < _String.Length; i++)
            {
                if (_String[i] == '\n')
                    _EOL = i;
            }
        }

        private void SkipToData()
        {
            while (_Pos < _EOL && IsDelimiter())
                _Pos++;
        }

        private bool IsComment()
        {
            bool result = _String[_Pos] == '\n';

            for (int i = 0; i < _Comments.Length && !result; i++)
            {
                result = _String[_Pos] == _Comments[i];

                if (result && i + 1 < _Comments.Length && _Comments[i] == _Comments[i + 1] && _Pos + 1 < _EOL)
                {
                    result = _String[_Pos] == _String[_Pos + 1];
                    i++;
                }
            }

            return result;
        }

        private bool IsQuote()
        {
            bool result = _String[_Pos] == '\n';

            for (int i = 0; i < _Quotes.Length && !result; i += 2)
            {
                if (_String[_Pos] == _Quotes[i] || (i + 1 < _Quotes.Length && _String[_Pos] == _Quotes[i + 1]))
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        private bool IsSecondQuote()
        {
            bool result = _String[_Pos] == '\n';

            for (int i = 0; i + 1 < _Quotes.Length && !result; i += 2)
            {
                if (_String[_Pos] == _Quotes[i + 1])
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

            while (_Pos < _String.Length && _String[_Pos] != '\n')
            {
                if (IsDelimiter())
                    break;

                else if (IsComment())
                {
                    _Pos = _EOL;
                    break;
                }

                if (_String[_Pos] != '\r' && (!_Trim || (_String[_Pos] != ' ' && _String[_Pos] != '\t')))
                    result.Append(_String[_Pos]);

                _Pos++;
            }

            return result.ToString();
        }

        private string ObtainQuotedData()
        {
            bool exit = false;
            string result = "";

            for (int i = 0; i < _Quotes.Length; i += 2)
            {
                if (_String[_Pos] == _Quotes[i])
                {
                    char endQuote = _Quotes[i + 1];
                    exit = true;

                    _Pos++;
                    int pos = _Pos;

                    while (pos < _EOL && _String[pos] != '\n' && _String[pos] != endQuote)
                        pos++;

                    int size = pos - _Pos;

                    if (size > 0)
                    {
                        result = _String.Substring(_Pos, size).TrimEnd('\r', '\n');
                        _Pos = pos;

                        if (_Pos < _EOL && _String[_Pos] == endQuote)
                            _Pos++;
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
            int size = _EOL - _Pos;

            if (size > 0)
                RawLine = _String.Substring(_Pos, size).TrimEnd('\r', '\n');
            else
                RawLine = "";
        }

        internal List<string> ReadTokens(bool trim = true)
        {
            _Trim = trim;
            List<string> result = new List<string>();

            if (_Pos < _String.Length)
            {
                GetEOL();

                SaveRawLine();

                while (_Pos < _EOL)
                {
                    SkipToData();

                    if (IsComment())
                        break;

                    string buf = ObtainQuotedData();

                    if (buf.Length > 0)
                        result.Add(buf);
                    else if (IsSecondQuote())
                        _Pos++;
                }

                _Pos = _EOL + 1;
            }

            return result;
        }
    }
}
