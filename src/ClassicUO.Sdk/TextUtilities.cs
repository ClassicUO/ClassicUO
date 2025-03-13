// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Sdk.IO;

namespace ClassicUO.Sdk;

public static class TextUtilities
{
    public static string CapitalizeAllWords(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return str;

        var sb = new StringBuilder(str.Length);
        bool newWord = true;

        foreach (char c in str)
        {
            if (char.IsWhiteSpace(c) || char.IsPunctuation(c))
            {
                newWord = true;
                sb.Append(c);
            }
            else
            {
                if (newWord)
                {
                    sb.Append(char.ToUpper(c));
                    newWord = false;
                }
                else
                {
                    sb.Append(c);
                }
            }
        }

        return sb.ToString();
    }

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
}