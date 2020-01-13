#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using System.Text;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace ClassicUO.Utility
{
    internal static class StringHelper
    {
        private static readonly char[] _dots = {'.', ',', ';', '!'};
        private static readonly StringBuilder _sb = new StringBuilder();

        public static string CapitalizeFirstCharacter(string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            if (str.Length == 1)
                return char.ToUpper(str[0]).ToString();

            return char.ToUpper(str[0]) + str.Substring(1);
        }


        public static string CapitalizeAllWords(string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            if (str.Length == 1)
                return char.ToUpper(str[0]).ToString();

            _sb.Clear();

            bool capitalizeNext = true;

            for (int i = 0; i < str.Length; i++)
            {
                _sb.Append(capitalizeNext ? char.ToUpper(str[i]) : str[i]);
                if (!char.IsWhiteSpace(str[i]))
                    capitalizeNext = i + 1 < str.Length && char.IsWhiteSpace(str[i + 1]);
            }

            return _sb.ToString();
        }

        public static string CapitalizeWordsByLimitator(string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            if (str.Length == 1)
                return char.ToUpper(str[0]).ToString();

            _sb.Clear();

            bool capitalizeNext = true;

            for (int i = 0; i < str.Length; i++)
            {
                _sb.Append(capitalizeNext ? char.ToUpper(str[i]) : str[i]);
                capitalizeNext = false;

                for (int j = 0; j < _dots.Length; j++)
                {
                    if (str[i] == _dots[j])
                    {
                        capitalizeNext = true;

                        break;
                    }
                }
            }

            return _sb.ToString();
        }

        public static unsafe string ReadUTF8(byte* data)
        {
            byte* ptr = data;

            while (*ptr != 0)
                ptr++;

            return Encoding.UTF8.GetString(data, (int) (ptr - data));
        }

        [MethodImpl(256)]
        public static bool IsSafeChar(int c)
        {
            return c >= 0x20 && c < 0xFFFE;
        }

        public static void AddSpaceBeforeCapital(string[] str, bool checkAcronyms = true)
        {
            for (int i = 0; i < str.Length; i++) str[i] = AddSpaceBeforeCapital(str[i], checkAcronyms);
        }

        public static string AddSpaceBeforeCapital(string str, bool checkAcronyms = true)
        {
            if (string.IsNullOrWhiteSpace(str))
                return "";

            _sb.Clear();
            _sb.Append(str[0]);

            for (int i = 1, len = str.Length - 1; i <= len; i++)
            {
                if (char.IsUpper(str[i]))
                {
                    if (str[i - 1] != ' ' && !char.IsUpper(str[i - 1]) ||
                        checkAcronyms && char.IsUpper(str[i - 1]) && i < len && !char.IsUpper(str[i + 1]))
                        _sb.Append(' ');
                }

                _sb.Append(str[i]);
            }

            return _sb.ToString();
        }

        public static string RemoveUpperLowerChars(string str, bool removelower = true)
        {
            if (string.IsNullOrWhiteSpace(str))
                return "";

            _sb.Clear();

            for (int i = 0; i < str.Length; i++)
            {
                if (char.IsUpper(str[i]) == removelower || str[i] == ' ')
                    _sb.Append(str[i]);
            }

            return _sb.ToString();
        }

        public static string IntToAbbreviatedString(int num)
        {
            if (num > 999999)
            {
                return string.Format("{0}M+", num / 1000000);
            }
            else if (num > 999)
            {
                return string.Format("{0}K+", num / 1000);
            }
            else
            {
                return num.ToString();
            }
        }
    }
}