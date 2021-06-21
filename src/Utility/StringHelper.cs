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
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using SDL2;

namespace ClassicUO.Utility
{
    internal static class StringHelper
    {
        private static readonly char[] _dots = { '.', ',', ';', '!' };

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
                if (buffer[i] != str[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}