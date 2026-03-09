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
//    documentation and/or other materials distributed with the distribution.
// 3. Neither the name of the copyright holder nor the names of its
//    contributors may be used to endorse or promote products derived from
//    this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED.
//
#endregion

using System.Text.RegularExpressions;

namespace ClassicUO.Assets
{
    public static class HtmlTextHelper
    {
        public static string ConvertHtmlToPlain(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            string finalString = Regex.Replace(text, "<basefont\\s+color\\s*=\\s*[\"']?(?<color>[^\"'>\\s]+)[\"']?>", " /c[${color}]", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            finalString = Regex.Replace(finalString, "<Bodytextcolor\"?'?(?<color>.*?)\"?'?>", " /c[${color}]", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            finalString = finalString.Replace("</basefont>", "/cd").Replace("</BASEFONT>", "/cd").Replace("\n", "\n/cd");
            finalString = finalString.Replace("<br>", "\n").Replace("<BR>", "\n");
            finalString = finalString.Replace("<left>", string.Empty).Replace("</left>", string.Empty);
            finalString = finalString.Replace("<b>", string.Empty).Replace("</b>", string.Empty);
            finalString = finalString.Replace("</font>", string.Empty).Replace("<h2>", string.Empty);
            finalString = finalString.Replace("<BODY>", string.Empty).Replace("<body>", string.Empty);
            finalString = finalString.Replace("</BODY>", string.Empty).Replace("</body>", string.Empty);
            finalString = finalString.Replace("</p>", string.Empty).Replace("<p>", string.Empty);
            finalString = finalString.Replace("</BIG>", string.Empty).Replace("<BIG>", string.Empty);
            finalString = finalString.Replace("</big>", string.Empty).Replace("<big>", string.Empty);
            finalString = finalString.Replace("<basefont>", string.Empty).Replace("<BASEFONT>", string.Empty);
            return finalString;
        }

        public static string StripColorCodes(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            string result = Regex.Replace(text, @"/c\[[^\]]*\]", "", RegexOptions.IgnoreCase);
            result = result.Replace("/cd", "");
            return result;
        }

        public static string ConvertUoColorCodesToHtml(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            string result = Regex.Replace(text, @"/c\[([^\]]*)\]", "<basefont color=\"$1\">", RegexOptions.IgnoreCase);
            result = result.Replace("/cd", "<basefont color=\"#FFFFFFFF\">");
            return result;
        }
    }
}
