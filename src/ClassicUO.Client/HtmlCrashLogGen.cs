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

using ClassicUO.Utility.Logging;
using ClassicUO.Utility.Platforms;
using System;
using System.IO;

namespace ClassicUO
{
    public static class HtmlCrashLogGen
    {
        private static string SafeHtmlEncode(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            try
            {
                return System.Net.WebUtility.HtmlEncode(text);
            }
            catch
            {
                return text
                    .Replace("&", "&amp;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;")
                    .Replace("\"", "&quot;")
                    .Replace("'", "&#39;");
            }
        }

        public static void Generate(
            string stackTrace,
            string title = "Dust765 Crash Report",
            string description = "Oh no! Dust765 crashed."
        )
        {
            const string TEMPLATE = @"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"" />
  <title>[TITLE]</title>
  <style>
    body {
      margin: 0;
      font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;
      background-color: #1a1a1a;
      color: #e0e0e0;
      padding: 2rem;
    }

    h1 {
      font-size: 1.8rem;
      color: #750303;
      margin-bottom: 1rem;
    }

    a {
      font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;
      color: #750303;
    }

    p {
      margin-bottom: 1rem;
    }

    pre {
      background-color: #2b2b2b;
      color: #f8f8f2;
      border: 1px solid #750303;
      padding: 1rem;
      border-radius: 8px;
      white-space: pre-wrap;
      word-break: break-word;
      overflow-x: auto;
    }

    button {
      margin-top: 1rem;
      padding: 0.5rem 1.2rem;
      font-size: 1rem;
      border: none;
      border-radius: 5px;
      background-color: #750303;
      color: #fff;
      cursor: pointer;
      border: 1px solid #750303;
      transition: background-color 0.2s, border-color 0.2s;
    }

    button:hover {
      background-color: #750303;
      color: #000;
      border-color: #750303;
    }
  </style>
</head>
<body>
  <h1>[TITLE]</h1>
  <p>[DESCRIPTION]<br>If you'd like support for this please copy and send this to our <a href=""https://github.com/dust765/ClassicUO/issues"">GitHub</a> or <a href=""https://discord.gg/9Vh7aqqX"">Discord</a>:</p>
  <pre id=""stackTrace"">[STACK TRACE]</pre>
  <button onclick=""copyStack()"">Copy to Clipboard</button>

  <script>
    function copyStack() {
      const text = document.getElementById('stackTrace').textContent;

      if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(text)
          .then(() => alert('Stack trace copied to clipboard'))
          .catch(() => fallbackCopy(text));
      } else {
        fallbackCopy(text);
      }
    }

    function fallbackCopy(text) {
      const textArea = document.createElement('textarea');
      textArea.value = text;
      textArea.style.position = 'fixed';
      textArea.style.opacity = '0';
      document.body.appendChild(textArea);
      textArea.select();

      try {
        document.execCommand('copy');
        alert('Stack trace copied to clipboard');
      } catch (err) {
        alert('Failed to copy. Please select the text and copy manually (Ctrl+C).');
      }

      document.body.removeChild(textArea);
    }
  </script>
</body>
</html>";

            stackTrace = (stackTrace ?? string.Empty).Trim();

            string encodedStackTrace = SafeHtmlEncode(stackTrace);
            string encodedTitle = SafeHtmlEncode(title);
            string encodedDescription = SafeHtmlEncode(description);

            string html = TEMPLATE.Replace("[STACK TRACE]", encodedStackTrace);
            html = html.Replace("[TITLE]", encodedTitle);
            html = html.Replace("[DESCRIPTION]", encodedDescription);

            try
            {
                Log.Trace("Generating HTML crash report...");

                string filePath = Path.Combine(Path.GetTempPath(), $"Dust765_Crash_{Guid.NewGuid():N}.html");
                File.WriteAllText(filePath, html);
                PlatformHelper.LaunchBrowser(filePath);

                Log.Trace($"Crash report saved to: {filePath}");
            }
            catch (Exception e)
            {
                Log.Error($"Failed to generate HTML crash report: {e}");

                try
                {
                    Console.Error.WriteLine("===== CRASH REPORT =====");
                    Console.Error.WriteLine(stackTrace);
                    Console.Error.WriteLine("========================");
                }
                catch
                {
                }
            }
        }
    }
}
