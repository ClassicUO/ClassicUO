// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using SDL2;

namespace ClassicUO.Utility
{
    public static class StringHelper
    {
        private static readonly char[] _dots = { '.', ',', ';', '!' };

        public static IEnumerable<byte> StringToCp1252Bytes(string s, int length = -1)
        {
            length = length > 0 ? Math.Min(length, s.Length) : s.Length;

            for (int i = 0; i < length; i += char.IsSurrogatePair(s, i) ? 2 : 1)
            {
                yield return UnicodeToCp1252(char.ConvertToUtf32(s, i));
            }
        }

        public static string Cp1252ToString(ReadOnlySpan<byte> strCp1252)
        {
            var sb = new ValueStringBuilder(strCp1252.Length);

            for (int i = 0; i < strCp1252.Length; ++i)
            {
                sb.Append(char.ConvertFromUtf32(Cp1252ToUnicode(strCp1252[i])));
            }

            var str = sb.ToString();

            sb.Dispose();

            return str;
        }

        /// <summary>
        /// Converts a unicode code point into a cp1252 code point
        /// </summary>
        private static byte UnicodeToCp1252(int codepoint)
        {
            if (codepoint >= 0x80 && codepoint <= 0x9f)
                return (byte)'?';
            else if (codepoint <= 0xff)
                return (byte)codepoint;
            else
            {
                switch (codepoint)
                {
                    case 0x20AC: return 128; //€
                    case 0x201A: return 130; //‚
                    case 0x0192: return 131; //ƒ
                    case 0x201E: return 132; //„
                    case 0x2026: return 133; //…
                    case 0x2020: return 134; //†
                    case 0x2021: return 135; //‡
                    case 0x02C6: return 136; //ˆ
                    case 0x2030: return 137; //‰
                    case 0x0160: return 138; //Š
                    case 0x2039: return 139; //‹
                    case 0x0152: return 140; //Œ
                    case 0x017D: return 142; //Ž
                    case 0x2018: return 145; //‘
                    case 0x2019: return 146; //’
                    case 0x201C: return 147; //“
                    case 0x201D: return 148; //”
                    case 0x2022: return 149; //•
                    case 0x2013: return 150; //–
                    case 0x2014: return 151; //—
                    case 0x02DC: return 152; //˜
                    case 0x2122: return 153; //™
                    case 0x0161: return 154; //š
                    case 0x203A: return 155; //›
                    case 0x0153: return 156; //œ
                    case 0x017E: return 158; //ž
                    case 0x0178: return 159; //Ÿ
                    default: return (byte)'?';
                }
            }
        }

        /// <summary>
        /// Converts a cp1252 code point into a unicode code point
        /// </summary>
        private static int Cp1252ToUnicode(byte codepoint)
        {
            switch (codepoint)
            {
                case 128: return 0x20AC; //€
                case 130: return 0x201A; //‚
                case 131: return 0x0192; //ƒ
                case 132: return 0x201E; //„
                case 133: return 0x2026; //…
                case 134: return 0x2020; //†
                case 135: return 0x2021; //‡
                case 136: return 0x02C6; //ˆ
                case 137: return 0x2030; //‰
                case 138: return 0x0160; //Š
                case 139: return 0x2039; //‹
                case 140: return 0x0152; //Œ
                case 142: return 0x017D; //Ž
                case 145: return 0x2018; //‘
                case 146: return 0x2019; //’
                case 147: return 0x201C; //“
                case 148: return 0x201D; //”
                case 149: return 0x2022; //•
                case 150: return 0x2013; //–
                case 151: return 0x2014; //—
                case 152: return 0x02DC; //˜
                case 153: return 0x2122; //™
                case 154: return 0x0161; //š
                case 155: return 0x203A; //›
                case 156: return 0x0153; //œ
                case 158: return 0x017E; //ž
                case 159: return 0x0178; //Ÿ
                default: return codepoint;
            }
        }

        public static string CapitalizeFirstCharacter(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            if (str.Length == 1)
            {
                return char.ToUpper(str[0]).ToString();
            }

            return char.ToUpper(str[0]) + str.Substring(1);
        }


        public static string CapitalizeAllWords(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            Span<char> span = stackalloc char[str.Length];
            ValueStringBuilder sb = new ValueStringBuilder(span);
            bool capitalizeNext = true;

            for (int i = 0; i < str.Length; i++)
            {
                sb.Append(capitalizeNext ? char.ToUpper(str[i]) : str[i]);

                if (!char.IsWhiteSpace(str[i]))
                {
                    capitalizeNext = i + 1 < str.Length && char.IsWhiteSpace(str[i + 1]);
                }
            }

            string ss = sb.ToString();

            sb.Dispose();

            return ss;
        }

        public static string CapitalizeWordsByLimitator(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            Span<char> span = stackalloc char[str.Length];
            ValueStringBuilder sb = new ValueStringBuilder(span);

            bool capitalizeNext = true;

            for (int i = 0; i < str.Length; i++)
            {
                sb.Append(capitalizeNext ? char.ToUpper(str[i]) : str[i]);
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

            string ss = sb.ToString();

            sb.Dispose();

            return ss;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSafeChar(int c)
        {
            return c >= 0x20 && c < 0xFFFE;
        }

        public static void AddSpaceBeforeCapital(string[] str, bool checkAcronyms = true)
        {
            for (int i = 0; i < str.Length; i++)
            {
                str[i] = AddSpaceBeforeCapital(str[i], checkAcronyms);
            }
        }

        public static string AddSpaceBeforeCapital(string str, bool checkAcronyms = true)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return "";
            }

            ValueStringBuilder sb = new ValueStringBuilder(str.Length * 2);
            sb.Append(str[0]);

            for (int i = 1, len = str.Length - 1; i <= len; i++)
            {
                if (char.IsUpper(str[i]))
                {
                    if (str[i - 1] != ' ' && !char.IsUpper(str[i - 1]) || checkAcronyms && char.IsUpper(str[i - 1]) && i < len && !char.IsUpper(str[i + 1]))
                    {
                        sb.Append(' ');
                    }
                }

                sb.Append(str[i]);
            }

            string s = sb.ToString();

            sb.Dispose();

            return s;
        }

        public static string RemoveUpperLowerChars(string str, bool removelower = true)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return "";
            }

            Span<char> span = stackalloc char[str.Length];
            ValueStringBuilder sb = new ValueStringBuilder(span);

            for (int i = 0; i < str.Length; i++)
            {
                if (char.IsUpper(str[i]) == removelower || str[i] == ' ')
                {
                    sb.Append(str[i]);
                }
            }

            string ss = sb.ToString();

            sb.Dispose();

            return ss;
        }

        public static string IntToAbbreviatedString(int num)
        {
            if (num > 999999)
            {
                return string.Format("{0}M+", num / 1000000);
            }

            if (num > 999)
            {
                return string.Format("{0}K+", num / 1000);
            }

            return num.ToString();
        }

        public static string GetClipboardText(bool multiline)
        {
            if (SDL.SDL_HasClipboardText() != SDL.SDL_bool.SDL_FALSE)
            {
                string s = multiline ? SDL.SDL_GetClipboardText() : SDL.SDL_GetClipboardText()?.Replace('\n', ' ') ?? null;

                if (!string.IsNullOrEmpty(s))
                {
                    if (s.IndexOf('\r') >= 0)
                    {
                        s = s.Replace("\r", "");
                    }

                    if (s.IndexOf('\t') >= 0)
                    {
                        return s.Replace("\t", "   ");
                    }

                    return s;
                }
            }

            return null;
        }

        public static string GetPluralAdjustedString(string str, bool plural = false)
        {
            if (str.Contains("%"))
            {
                string[] parts = str.Split(new[] { '%' }, System.StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 2)
                {
                    return str;
                }

                Span<char> span = stackalloc char[str.Length];
                ValueStringBuilder sb = new ValueStringBuilder(span);

                sb.Append(parts[0]);

                if (parts[1].Contains("/"))
                {
                    string[] pluralparts = parts[1].Split('/');

                    if (plural)
                    {
                        sb.Append(pluralparts[0]);
                    }
                    else if (pluralparts.Length > 1)
                    {
                        sb.Append(pluralparts[1]);
                    }
                }
                else if (plural)
                {
                    sb.Append(parts[1]);
                }

                if (parts.Length == 3)
                {
                    sb.Append(parts[2]);
                }

                string ss = sb.ToString();

                sb.Dispose();

                return ss;
            }

            return str;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool UnsafeCompare(char* buffer, string str, int length)
        {
            for (int i = 0; i < length && i < str.Length; ++i)
            {
                var c0 = char.IsLetter(buffer[i]) ? char.ToLowerInvariant(buffer[i]) : buffer[i];
                var c1 = char.IsLetter(str[i]) ? char.ToLowerInvariant(str[i]) : str[i];

                if (c0 != c1)
                {
                    return false;
                }
            }

            return true;
        }
    }
}