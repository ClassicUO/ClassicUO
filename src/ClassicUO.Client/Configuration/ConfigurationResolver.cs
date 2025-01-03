#region license

// BSD 2-Clause License
//
// Copyright (c) 2025, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//
// - Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer.
//
// - Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Configuration
{
    internal static class ConfigurationResolver
    {
        public static T Load<T>(string file, JsonTypeInfo<T> ctx) where T : class
        {
            if (!File.Exists(file))
            {
                Log.Warn(file + " not found.");

                return null;
            }

            var text = File.ReadAllText(file);

            text = Regex.Replace
            (
                text,
                @"(?<!\\)  # lookbehind: Check that previous character isn't a \
                                                \\         # match a \
                                                (?!\\)     # lookahead: Check that the following character isn't a \",
                @"\\",
                RegexOptions.IgnorePatternWhitespace
            );

            return JsonSerializer.Deserialize(text, ctx);
        }

        public static void Save<T>(T obj, string file, JsonTypeInfo<T> ctx) where T : class
        {
            // this try catch is necessary when multiples cuo instances points to this file.
            try
            {
                var fileInfo = new FileInfo(file);

                if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
                {
                    fileInfo.Directory.Create();
                }

                var json = JsonSerializer.Serialize(obj, ctx);
                File.WriteAllText(file, json);
            }
            catch (IOException e)
            {
                Log.Error(e.ToString());
            }
        }
    }
}